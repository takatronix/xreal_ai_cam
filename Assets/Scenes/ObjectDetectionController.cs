using UnityEngine;
using UnityEngine.UI;

namespace Unity.AI.Cam
{
    /// <summary>
    /// XREAL One Pro & XREAL Eye 物体検知オーバーレイコントローラー
    /// AIによる物体認識結果をリアルタイム表示
    /// </summary>
    public class ObjectDetectionController : MonoBehaviour
    {
        [Header("物体検知設定")]
        // Note: レイヤー管理システム導入により、直接参照は不要

        [SerializeField]
        [Tooltip("検知結果表示用UI")]
        RawImage m_DetectionDisplay;

        [SerializeField]
        [Tooltip("検知情報テキスト")]
        Text m_DetectionInfoText;

        [Header("表示制御")]
        [SerializeField]
        [Range(0.0f, 1.0f)]
        [Tooltip("オーバーレイの透過率")]
        float m_OverlayAlpha = 0.6f;

        [SerializeField]
        [Tooltip("検知枠の色")]
        Color m_BoundingBoxColor = Color.green;

        [SerializeField]
        [Tooltip("検知結果の表示時間（秒）")]
        float m_DisplayDuration = 2.0f;

        // 検知結果データ
        private Texture2D m_DetectionTexture;
        private string[] m_DetectedObjects;
        private float m_LastDetectionTime;

        void Start()
        {
            // 検知結果表示用テクスチャ作成
            CreateDetectionTexture();
        }

        void Update()
        {
            // 検知結果の自動非表示
            if (Time.time - m_LastDetectionTime > m_DisplayDuration)
            {
                ClearDetectionDisplay();
            }
        }

        /// <summary>
        /// 検知結果表示用テクスチャ作成
        /// </summary>
        void CreateDetectionTexture()
        {
            // 仮想的な検知結果テクスチャ（実際はAIモデルから取得）
            m_DetectionTexture = new Texture2D(1920, 1080, TextureFormat.RGBA32, false);
            
            // 透明で初期化
            Color[] pixels = new Color[1920 * 1080];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = Color.clear;
            }
            m_DetectionTexture.SetPixels(pixels);
            m_DetectionTexture.Apply();
        }

        /// <summary>
        /// 物体検知結果を表示（AI モデルから呼び出される想定）
        /// </summary>
        /// <param name="objects">検知された物体名</param>
        /// <param name="boundingBoxes">バウンディングボックス座標</param>
        public void DisplayDetectionResult(string[] objects, Rect[] boundingBoxes)
        {
            m_DetectedObjects = objects;
            m_LastDetectionTime = Time.time;

            // バウンディングボックス描画
            DrawBoundingBoxes(boundingBoxes);

            // 検知情報テキスト更新
            UpdateDetectionText();

            // 検知結果表示オーバーレイに反映（レイヤー管理システムで処理）
        }

        /// <summary>
        /// バウンディングボックスをテクスチャに描画
        /// </summary>
        void DrawBoundingBoxes(Rect[] boundingBoxes)
        {
            if (boundingBoxes == null || m_DetectionTexture == null) return;

            // テクスチャをクリア
            Color[] pixels = new Color[m_DetectionTexture.width * m_DetectionTexture.height];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = Color.clear;
            }

            // バウンディングボックス描画
            foreach (var box in boundingBoxes)
            {
                DrawRectangle(pixels, box, m_BoundingBoxColor);
            }

            m_DetectionTexture.SetPixels(pixels);
            m_DetectionTexture.Apply();
        }

        /// <summary>
        /// 矩形をピクセル配列に描画
        /// </summary>
        void DrawRectangle(Color[] pixels, Rect rect, Color color)
        {
            int width = m_DetectionTexture.width;
            int height = m_DetectionTexture.height;

            int x1 = Mathf.RoundToInt(rect.x * width);
            int y1 = Mathf.RoundToInt(rect.y * height);
            int x2 = Mathf.RoundToInt((rect.x + rect.width) * width);
            int y2 = Mathf.RoundToInt((rect.y + rect.height) * height);

            // 境界チェック
            x1 = Mathf.Clamp(x1, 0, width - 1);
            y1 = Mathf.Clamp(y1, 0, height - 1);
            x2 = Mathf.Clamp(x2, 0, width - 1);
            y2 = Mathf.Clamp(y2, 0, height - 1);

            // 矩形の境界線を描画（線の太さ3px）
            for (int thickness = 0; thickness < 3; thickness++)
            {
                // 上下の線
                for (int x = x1; x <= x2; x++)
                {
                    if (y1 + thickness < height)
                        pixels[(y1 + thickness) * width + x] = color;
                    if (y2 - thickness >= 0)
                        pixels[(y2 - thickness) * width + x] = color;
                }

                // 左右の線
                for (int y = y1; y <= y2; y++)
                {
                    if (x1 + thickness < width)
                        pixels[y * width + (x1 + thickness)] = color;
                    if (x2 - thickness >= 0)
                        pixels[y * width + (x2 - thickness)] = color;
                }
            }
        }

        /// <summary>
        /// 検知情報テキストを更新
        /// </summary>
        void UpdateDetectionText()
        {
            if (m_DetectionInfoText != null && m_DetectedObjects != null)
            {
                string info = "検知された物体:\n";
                for (int i = 0; i < m_DetectedObjects.Length; i++)
                {
                    info += $"• {m_DetectedObjects[i]}\n";
                }
                m_DetectionInfoText.text = info;
            }
        }

        /// <summary>
        /// 検知結果表示をクリア
        /// </summary>
        void ClearDetectionDisplay()
        {
            if (m_DetectionTexture != null)
            {
                // テクスチャをクリア
                Color[] pixels = new Color[m_DetectionTexture.width * m_DetectionTexture.height];
                for (int i = 0; i < pixels.Length; i++)
                {
                    pixels[i] = Color.clear;
                }
                m_DetectionTexture.SetPixels(pixels);
                m_DetectionTexture.Apply();

                // オーバーレイクリア（レイヤー管理システムで処理）
            }

            // テキストクリア
            if (m_DetectionInfoText != null)
            {
                m_DetectionInfoText.text = "";
            }
        }

        /// <summary>
        /// サンプル検知実行（テスト用）
        /// </summary>
        [ContextMenu("テスト検知実行")]
        public void TestDetection()
        {
            // サンプルの検知結果
            string[] testObjects = { "人", "車", "建物" };
            Rect[] testBoxes = {
                new Rect(0.2f, 0.3f, 0.3f, 0.4f),
                new Rect(0.6f, 0.5f, 0.2f, 0.2f),
                new Rect(0.1f, 0.1f, 0.4f, 0.3f)
            };

            DisplayDetectionResult(testObjects, testBoxes);
        }
    }
} 