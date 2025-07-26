using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

namespace Unity.AI.Cam
{
    /// <summary>
    /// XREAL One Pro & XREAL Eye 生カメラレイヤー
    /// ARFoundationからの生カメラフィードを表示する底層レイヤー
    /// </summary>
    public class XREALRawCameraLayer : MonoBehaviour
    {
        // YUV420テクスチャ（カメラデータ）
        Texture2D m_TextureY;
        Texture2D m_TextureV;
        Texture2D m_TextureU;

        [Header("XREAL 生カメラ設定")]
        [SerializeField]
        ARCameraManager m_CameraManager;

        [SerializeField]
        RawImage m_RawCameraImage;

        [Header("レイヤー制御")]
        [SerializeField]
        [Tooltip("生カメラレイヤーの表示/非表示")]
        bool m_LayerEnabled = true;

        [SerializeField]
        [Range(0.0f, 1.0f)]
        [Tooltip("生カメラ映像の透過率 (0=完全透明, 1=完全不透明)")]
        float m_CameraAlpha = 0.8f;

        // 前回の設定値（変更検知用）
        private float m_PreviousAlpha;
        private bool m_PreviousEnabled;

        void OnEnable()
        {
            // ARカメラマネージャーのフレーム受信イベントに登録
            if (m_CameraManager != null)
                m_CameraManager.frameReceived += OnCameraFrameReceived;
        }

        void OnDisable()
        {
            // ARカメラマネージャーのフレーム受信イベントから解除
            if (m_CameraManager != null)
                m_CameraManager.frameReceived -= OnCameraFrameReceived;
        }

        void Update()
        {
            // レイヤー表示/非表示の変更をリアルタイムで反映
            if (m_LayerEnabled != m_PreviousEnabled)
            {
                UpdateLayerVisibility();
                m_PreviousEnabled = m_LayerEnabled;
            }

            // 透過率の変更をリアルタイムで反映（レイヤーが有効な場合のみ）
            if (m_LayerEnabled && Mathf.Abs(m_CameraAlpha - m_PreviousAlpha) > 0.001f)
            {
                UpdateCameraAlpha();
                m_PreviousAlpha = m_CameraAlpha;
            }
        }

        /// <summary>
        /// ARカメラフレーム受信時のコールバック（XREAL One Pro & Eye 対応）
        /// </summary>
        void OnCameraFrameReceived(ARCameraFrameEventArgs eventArgs)
        {
            UpdateCameraImage();
        }

        /// <summary>
        /// カメラ画像更新処理（YUV420 → Unity Texture変換）
        /// XREAL One Pro & Eye の高解像度カメラに最適化
        /// </summary>
        void UpdateCameraImage()
        {
            if (m_CameraManager.TryAcquireLatestCpuImage(out XRCpuImage image))
            {
                // 初回のみテクスチャ作成
                if (m_TextureY == null)
                {
                    int Width = image.dimensions.x;
                    int Height = image.dimensions.y;
                    
                    // YUV420形式のテクスチャ作成
                    m_TextureY = new Texture2D(Width, Height, TextureFormat.Alpha8, false);
                    m_TextureU = new Texture2D(Width / 2, Height / 2, TextureFormat.Alpha8, false);
                    m_TextureV = new Texture2D(Width / 2, Height / 2, TextureFormat.Alpha8, false);

                    // メインカメラ表示の設定
                    var material = m_RawCameraImage.material;
                    m_RawCameraImage.texture = m_TextureY;
                    material.SetTexture("_UTex", m_TextureU);
                    material.SetTexture("_VTex", m_TextureV);
                    
                    // 初期透過率適用
                    UpdateCameraAlpha();
                }

                // YUVプレーンデータ取得
                var planeY = image.GetPlane(0);  // 輝度
                var planeV = image.GetPlane(1);  // V色差
                var planeU = image.GetPlane(2);  // U色差

                // テクスチャにデータロード
                m_TextureY.LoadRawTextureData(planeY.data);
                m_TextureV.LoadRawTextureData(planeV.data);
                m_TextureU.LoadRawTextureData(planeU.data);

                // GPU に反映
                m_TextureY.Apply();
                m_TextureU.Apply();
                m_TextureV.Apply();

                // メモリ解放
                image.Dispose();
            }
        }

        /// <summary>
        /// レイヤーの表示/非表示を更新
        /// </summary>
        void UpdateLayerVisibility()
        {
            if (m_RawCameraImage != null)
            {
                m_RawCameraImage.gameObject.SetActive(m_LayerEnabled);
            }
        }

        /// <summary>
        /// 生カメラ映像の透過率をリアルタイム更新
        /// </summary>
        void UpdateCameraAlpha()
        {
            if (m_RawCameraImage != null && m_LayerEnabled)
            {
                var color = m_RawCameraImage.color;
                color.a = m_CameraAlpha;
                m_RawCameraImage.color = color;
            }
        }

        /// <summary>
        /// レイヤーの表示/非表示をプログラムから制御
        /// </summary>
        /// <param name="enabled">表示する場合true</param>
        public void SetLayerEnabled(bool enabled)
        {
            m_LayerEnabled = enabled;
            UpdateLayerVisibility();
        }

        /// <summary>
        /// 生カメラ透過率をプログラムから制御
        /// </summary>
        /// <param name="alpha">透過率 (0-1)</param>
        public void SetCameraAlpha(float alpha)
        {
            m_CameraAlpha = Mathf.Clamp01(alpha);
            if (m_LayerEnabled)
            {
                UpdateCameraAlpha();
            }
        }

        /// <summary>
        /// 現在のカメラテクスチャを取得（他のレイヤーで使用）
        /// </summary>
        /// <returns>Y, U, V テクスチャの配列</returns>
        public Texture2D[] GetCameraTextures()
        {
            if (m_TextureY != null && m_TextureU != null && m_TextureV != null)
            {
                return new Texture2D[] { m_TextureY, m_TextureU, m_TextureV };
            }
            return null;
        }
    }
}
