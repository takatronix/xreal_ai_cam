using UnityEngine;
using UnityEngine.UI;

namespace Unity.AI.Cam
{
    /// <summary>
    /// XREAL One Pro & XREAL Eye 処理済み画像レイヤー（中層）
    /// 生カメラ映像にAI処理やフィルタ処理を適用して表示
    /// </summary>
    public class XREALProcessedImageLayer : MonoBehaviour
    {
        [Header("XREAL 処理済み画像設定")]
        [SerializeField]
        [Tooltip("生カメラレイヤーの参照")]
        XREALRawCameraLayer m_RawCameraLayer;

        [SerializeField]
        [Tooltip("処理済み画像を表示するRawImage")]
        RawImage m_ProcessedImage;

        [Header("レイヤー制御")]
        [SerializeField]
        [Tooltip("処理済み画像レイヤーの表示/非表示")]
        bool m_LayerEnabled = true;

        [SerializeField]
        [Range(0.0f, 1.0f)]
        [Tooltip("処理済み画像の透過率 (0=完全透明, 1=完全不透明)")]
        float m_ImageAlpha = 0.7f;

        [Header("画像処理設定")]
        [SerializeField]
        [Tooltip("処理タイプ")]
        ProcessingType m_ProcessingType = ProcessingType.EdgeDetection;

        [SerializeField]
        [Range(0.0f, 2.0f)]
        [Tooltip("処理強度")]
        float m_ProcessingIntensity = 1.0f;

        [SerializeField]
        [Tooltip("処理カラー")]
        Color m_ProcessingColor = Color.cyan;

        // 処理タイプ列挙
        public enum ProcessingType
        {
            None,           // 処理なし
            EdgeDetection,  // エッジ検出
            Blur,          // ぼかし
            Sharpen,       // シャープ化
            ColorFilter,   // カラーフィルタ
            Sobel,         // ソーベルフィルタ
            AI_Enhancement // AI強化（将来実装）
        }

        // 処理済み画像テクスチャ
        private Texture2D m_ProcessedTexture;
        private RenderTexture m_RenderTexture;
        private Material m_ProcessingMaterial;

        // 前回の設定値（変更検知用）
        private bool m_PreviousEnabled;
        private float m_PreviousAlpha;
        private ProcessingType m_PreviousProcessingType;
        private float m_PreviousIntensity;

        void Start()
        {
            // 初期化
            if (m_RawCameraLayer == null)
            {
                m_RawCameraLayer = FindObjectOfType<XREALRawCameraLayer>();
            }

            // 処理用マテリアル作成
            CreateProcessingMaterial();

            // 初期レイヤー状態設定
            UpdateLayerVisibility();
        }

        void Update()
        {
            // レイヤー表示/非表示の変更をリアルタイムで反映
            if (m_LayerEnabled != m_PreviousEnabled)
            {
                UpdateLayerVisibility();
                m_PreviousEnabled = m_LayerEnabled;
            }

            // 設定変更の検知と反映
            if (m_LayerEnabled)
            {
                bool needsUpdate = false;

                if (Mathf.Abs(m_ImageAlpha - m_PreviousAlpha) > 0.001f)
                {
                    UpdateImageAlpha();
                    m_PreviousAlpha = m_ImageAlpha;
                }

                if (m_ProcessingType != m_PreviousProcessingType)
                {
                    needsUpdate = true;
                    m_PreviousProcessingType = m_ProcessingType;
                }

                if (Mathf.Abs(m_ProcessingIntensity - m_PreviousIntensity) > 0.001f)
                {
                    needsUpdate = true;
                    m_PreviousIntensity = m_ProcessingIntensity;
                }

                if (needsUpdate)
                {
                    ProcessCameraImage();
                }
            }
        }

        /// <summary>
        /// 処理用マテリアル作成
        /// </summary>
        void CreateProcessingMaterial()
        {
            // シンプルなシェーダーを使用（実際はカスタムシェーダーが理想）
            Shader shader = Shader.Find("Unlit/Texture");
            m_ProcessingMaterial = new Material(shader);
        }

        /// <summary>
        /// レイヤーの表示/非表示を更新
        /// </summary>
        void UpdateLayerVisibility()
        {
            if (m_ProcessedImage != null)
            {
                m_ProcessedImage.gameObject.SetActive(m_LayerEnabled);
                
                if (m_LayerEnabled)
                {
                    ProcessCameraImage();
                }
            }
        }

        /// <summary>
        /// 処理済み画像の透過率を更新
        /// </summary>
        void UpdateImageAlpha()
        {
            if (m_ProcessedImage != null && m_LayerEnabled)
            {
                var color = m_ProcessedImage.color;
                color.a = m_ImageAlpha;
                m_ProcessedImage.color = color;
            }
        }

        /// <summary>
        /// カメラ映像を処理して表示
        /// </summary>
        void ProcessCameraImage()
        {
            if (m_RawCameraLayer == null || !m_LayerEnabled) return;

            // 生カメラからテクスチャ取得
            Texture2D[] cameraTextures = m_RawCameraLayer.GetCameraTextures();
            if (cameraTextures == null || cameraTextures[0] == null) return;

            // 処理タイプに応じて画像処理実行
            Texture2D processedTexture = ApplyImageProcessing(cameraTextures[0]);
            
            if (processedTexture != null && m_ProcessedImage != null)
            {
                m_ProcessedImage.texture = processedTexture;
                UpdateImageAlpha();
            }
        }

        /// <summary>
        /// 画像処理を適用
        /// </summary>
        /// <param name="sourceTexture">元画像</param>
        /// <returns>処理済み画像</returns>
        Texture2D ApplyImageProcessing(Texture2D sourceTexture)
        {
            if (sourceTexture == null) return null;

            int width = sourceTexture.width;
            int height = sourceTexture.height;

            // 処理済みテクスチャを作成/更新
            if (m_ProcessedTexture == null || m_ProcessedTexture.width != width || m_ProcessedTexture.height != height)
            {
                if (m_ProcessedTexture != null)
                    DestroyImmediate(m_ProcessedTexture);
                
                m_ProcessedTexture = new Texture2D(width, height, TextureFormat.RGB24, false);
            }

            // 元画像のピクセルデータ取得
            Color[] sourcePixels = sourceTexture.GetPixels();
            Color[] processedPixels = new Color[sourcePixels.Length];

            // 処理タイプに応じて処理実行
            switch (m_ProcessingType)
            {
                case ProcessingType.None:
                    processedPixels = sourcePixels;
                    break;

                case ProcessingType.EdgeDetection:
                    processedPixels = ApplyEdgeDetection(sourcePixels, width, height);
                    break;

                case ProcessingType.Blur:
                    processedPixels = ApplyBlur(sourcePixels, width, height);
                    break;

                case ProcessingType.Sharpen:
                    processedPixels = ApplySharpen(sourcePixels, width, height);
                    break;

                case ProcessingType.ColorFilter:
                    processedPixels = ApplyColorFilter(sourcePixels);
                    break;

                case ProcessingType.Sobel:
                    processedPixels = ApplySobelFilter(sourcePixels, width, height);
                    break;

                default:
                    processedPixels = sourcePixels;
                    break;
            }

            // 処理済みテクスチャに反映
            m_ProcessedTexture.SetPixels(processedPixels);
            m_ProcessedTexture.Apply();

            return m_ProcessedTexture;
        }

        /// <summary>
        /// エッジ検出処理
        /// </summary>
        Color[] ApplyEdgeDetection(Color[] pixels, int width, int height)
        {
            Color[] result = new Color[pixels.Length];
            
            for (int y = 1; y < height - 1; y++)
            {
                for (int x = 1; x < width - 1; x++)
                {
                    int index = y * width + x;
                    
                    // ソーベル演算子によるエッジ検出
                    float gx = 0, gy = 0;
                    
                    for (int dy = -1; dy <= 1; dy++)
                    {
                        for (int dx = -1; dx <= 1; dx++)
                        {
                            int neighborIndex = (y + dy) * width + (x + dx);
                            float intensity = pixels[neighborIndex].grayscale;
                            
                            // ソーベルカーネル
                            int sobelX = dx;
                            int sobelY = dy;
                            
                            gx += intensity * sobelX;
                            gy += intensity * sobelY;
                        }
                    }
                    
                    float magnitude = Mathf.Sqrt(gx * gx + gy * gy) * m_ProcessingIntensity;
                    magnitude = Mathf.Clamp01(magnitude);
                    
                    Color edgeColor = Color.Lerp(Color.black, m_ProcessingColor, magnitude);
                    result[index] = edgeColor;
                }
            }
            
            return result;
        }

        /// <summary>
        /// ぼかし処理
        /// </summary>
        Color[] ApplyBlur(Color[] pixels, int width, int height)
        {
            Color[] result = new Color[pixels.Length];
            int kernelSize = Mathf.RoundToInt(m_ProcessingIntensity * 3) + 1;
            
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    Color sum = Color.black;
                    int count = 0;
                    
                    for (int dy = -kernelSize; dy <= kernelSize; dy++)
                    {
                        for (int dx = -kernelSize; dx <= kernelSize; dx++)
                        {
                            int nx = x + dx;
                            int ny = y + dy;
                            
                            if (nx >= 0 && nx < width && ny >= 0 && ny < height)
                            {
                                sum += pixels[ny * width + nx];
                                count++;
                            }
                        }
                    }
                    
                    result[y * width + x] = sum / count;
                }
            }
            
            return result;
        }

        /// <summary>
        /// シャープ化処理
        /// </summary>
        Color[] ApplySharpen(Color[] pixels, int width, int height)
        {
            Color[] result = new Color[pixels.Length];
            
            // シャープ化カーネル
            float[,] kernel = {
                {0, -1, 0},
                {-1, 5, -1},
                {0, -1, 0}
            };
            
            for (int y = 1; y < height - 1; y++)
            {
                for (int x = 1; x < width - 1; x++)
                {
                    Color sum = Color.black;
                    
                    for (int dy = -1; dy <= 1; dy++)
                    {
                        for (int dx = -1; dx <= 1; dx++)
                        {
                            int index = (y + dy) * width + (x + dx);
                            sum += pixels[index] * kernel[dy + 1, dx + 1];
                        }
                    }
                    
                    sum *= m_ProcessingIntensity;
                    result[y * width + x] = new Color(
                        Mathf.Clamp01(sum.r),
                        Mathf.Clamp01(sum.g),
                        Mathf.Clamp01(sum.b),
                        1.0f
                    );
                }
            }
            
            return result;
        }

        /// <summary>
        /// カラーフィルタ処理
        /// </summary>
        Color[] ApplyColorFilter(Color[] pixels)
        {
            Color[] result = new Color[pixels.Length];
            
            for (int i = 0; i < pixels.Length; i++)
            {
                Color original = pixels[i];
                Color filtered = Color.Lerp(original, 
                    new Color(
                        original.r * m_ProcessingColor.r,
                        original.g * m_ProcessingColor.g,
                        original.b * m_ProcessingColor.b,
                        1.0f
                    ), 
                    m_ProcessingIntensity);
                
                result[i] = filtered;
            }
            
            return result;
        }

        /// <summary>
        /// ソーベルフィルタ処理
        /// </summary>
        Color[] ApplySobelFilter(Color[] pixels, int width, int height)
        {
            // エッジ検出の詳細版
            return ApplyEdgeDetection(pixels, width, height);
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
        /// 処理済み画像透過率をプログラムから制御
        /// </summary>
        /// <param name="alpha">透過率 (0-1)</param>
        public void SetImageAlpha(float alpha)
        {
            m_ImageAlpha = Mathf.Clamp01(alpha);
            if (m_LayerEnabled)
            {
                UpdateImageAlpha();
            }
        }

        /// <summary>
        /// 処理タイプを変更
        /// </summary>
        /// <param name="processingType">処理タイプ</param>
        public void SetProcessingType(ProcessingType processingType)
        {
            m_ProcessingType = processingType;
            if (m_LayerEnabled)
            {
                ProcessCameraImage();
            }
        }

        /// <summary>
        /// 処理強度を変更
        /// </summary>
        /// <param name="intensity">処理強度 (0-2)</param>
        public void SetProcessingIntensity(float intensity)
        {
            m_ProcessingIntensity = Mathf.Clamp(intensity, 0.0f, 2.0f);
            if (m_LayerEnabled)
            {
                ProcessCameraImage();
            }
        }

        void OnDestroy()
        {
            // リソース解放
            if (m_ProcessedTexture != null)
            {
                DestroyImmediate(m_ProcessedTexture);
            }
            if (m_RenderTexture != null)
            {
                m_RenderTexture.Release();
                DestroyImmediate(m_RenderTexture);
            }
            if (m_ProcessingMaterial != null)
            {
                DestroyImmediate(m_ProcessingMaterial);
            }
        }
    }
} 