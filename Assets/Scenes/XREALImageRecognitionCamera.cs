using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

namespace Unity.AI.Cam
{
    /// <summary>
    /// XREAL One Pro & XREAL Eye 画像認識カメラコントローラー
    /// リアルタイムカメラフィード表示と物体検知オーバーレイ機能
    /// </summary>
    public class XREALImageRecognitionCamera : MonoBehaviour
    {
        // YUV420テクスチャ（カメラデータ）
        Texture2D m_TextureY;
        Texture2D m_TextureV;
        Texture2D m_TextureU;

        [Header("XREAL カメラ設定")]
        [SerializeField]
        ARCameraManager m_CameraManager;

        [SerializeField]
        RawImage m_RawCameraImage;

        [Header("表示制御")]
        [SerializeField]
        [Range(0.0f, 1.0f)]
        [Tooltip("カメラ映像の透過率 (0=完全透明, 1=完全不透明)")]
        float m_CameraAlpha = 0.8f;

        [Header("物体検知オーバーレイ")]
        [SerializeField]
        [Tooltip("物体検知結果を表示するオーバーレイUI")]
        RawImage m_ObjectDetectionOverlay;

        [SerializeField]
        [Tooltip("検知結果のオーバーレイ色")]
        Color m_OverlayColor = new Color(0, 1, 0, 0.5f); // 緑色半透明

        // 前回の透過率（変更検知用）
        private float m_PreviousAlpha;

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
            // 透過率の変更をリアルタイムで反映
            if (Mathf.Abs(m_CameraAlpha - m_PreviousAlpha) > 0.001f)
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
        /// カメラ映像の透過率をリアルタイム更新
        /// </summary>
        void UpdateCameraAlpha()
        {
            if (m_RawCameraImage != null)
            {
                var color = m_RawCameraImage.color;
                color.a = m_CameraAlpha;
                m_RawCameraImage.color = color;
            }
        }

        /// <summary>
        /// 物体検知結果をオーバーレイ表示
        /// </summary>
        /// <param name="detectionTexture">検知結果のテクスチャ</param>
        public void UpdateObjectDetectionOverlay(Texture2D detectionTexture)
        {
            if (m_ObjectDetectionOverlay != null && detectionTexture != null)
            {
                m_ObjectDetectionOverlay.texture = detectionTexture;
                m_ObjectDetectionOverlay.color = m_OverlayColor;
            }
        }

        /// <summary>
        /// オーバーレイの透過率変更
        /// </summary>
        /// <param name="alpha">透過率 (0-1)</param>
        public void SetOverlayAlpha(float alpha)
        {
            m_OverlayColor.a = Mathf.Clamp01(alpha);
            if (m_ObjectDetectionOverlay != null)
            {
                m_ObjectDetectionOverlay.color = m_OverlayColor;
            }
        }

        /// <summary>
        /// カメラ透過率をプログラムから制御
        /// </summary>
        /// <param name="alpha">透過率 (0-1)</param>
        public void SetCameraAlpha(float alpha)
        {
            m_CameraAlpha = Mathf.Clamp01(alpha);
            UpdateCameraAlpha();
        }
    }
}
