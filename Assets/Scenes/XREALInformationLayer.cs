using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

namespace Unity.AI.Cam
{
    /// <summary>
    /// XREAL One Pro & XREAL Eye 情報レイヤー（最上層）
    /// テキスト、UI、検知結果、メニューなどの情報を表示
    /// </summary>
    public class XREALInformationLayer : MonoBehaviour
    {
        [Header("XREAL 情報レイヤー設定")]
        [SerializeField]
        [Tooltip("情報表示エリアのキャンバス")]
        Canvas m_InformationCanvas;

        [Header("レイヤー制御")]
        [SerializeField]
        [Tooltip("情報レイヤーの表示/非表示")]
        bool m_LayerEnabled = true;

        [SerializeField]
        [Range(0.0f, 1.0f)]
        [Tooltip("情報レイヤーの透過率 (0=完全透明, 1=完全不透明)")]
        float m_LayerAlpha = 0.9f;

        [Header("検知結果表示")]
        [SerializeField]
        [Tooltip("検知結果表示用テキスト")]
        Text m_DetectionResultText;

        [SerializeField]
        [Tooltip("検知信頼度表示用テキスト")]
        Text m_ConfidenceText;

        [SerializeField]
        [Tooltip("検知結果背景パネル")]
        Image m_DetectionBackground;

        [Header("システム情報表示")]
        [SerializeField]
        [Tooltip("FPS表示用テキスト")]
        Text m_FpsText;

        [SerializeField]
        [Tooltip("デバイス情報表示用テキスト")]
        Text m_DeviceInfoText;

        [SerializeField]
        [Tooltip("ステータス表示用テキスト")]
        Text m_StatusText;

        [Header("UI要素")]
        [SerializeField]
        [Tooltip("設定メニューパネル")]
        GameObject m_SettingsPanel;

        [SerializeField]
        [Tooltip("クロスヘア（中央照準）")]
        Image m_Crosshair;

        [SerializeField]
        [Tooltip("バウンディングボックス表示用プレハブ")]
        GameObject m_BoundingBoxPrefab;

        [Header("情報表示設定")]
        [SerializeField]
        [Tooltip("FPS表示を有効にする")]
        bool m_ShowFPS = true;

        [SerializeField]
        [Tooltip("デバイス情報表示を有効にする")]
        bool m_ShowDeviceInfo = true;

        [SerializeField]
        [Tooltip("検知結果の表示時間（秒）")]
        float m_DetectionDisplayDuration = 3.0f;

        [SerializeField]
        [Tooltip("バウンディングボックスの色")]
        Color m_BoundingBoxColor = Color.green;

        // 内部データ
        private List<DetectionResult> m_CurrentDetections = new List<DetectionResult>();
        private List<GameObject> m_BoundingBoxInstances = new List<GameObject>();
        private float m_LastDetectionTime;
        private float m_CurrentFPS;
        private int m_FrameCount;
        private float m_FpsTimer;

        // 前回の設定値（変更検知用）
        private bool m_PreviousEnabled;
        private float m_PreviousAlpha;

        // 検知結果データ構造
        [System.Serializable]
        public class DetectionResult
        {
            public string objectName;      // 検知された物体名
            public float confidence;       // 信頼度 (0-1)
            public Rect boundingBox;       // バウンディングボックス (0-1正規化座標)
            public Color boxColor;         // ボックス色
            public float detectionTime;    // 検知時刻
        }

        void Start()
        {
            // 初期化
            InitializeInformationLayer();
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

            // 透過率の変更をリアルタイムで反映
            if (Mathf.Abs(m_LayerAlpha - m_PreviousAlpha) > 0.001f)
            {
                UpdateLayerAlpha();
                m_PreviousAlpha = m_LayerAlpha;
            }

            if (m_LayerEnabled)
            {
                // FPS計算と表示
                UpdateFPS();

                // デバイス情報更新
                UpdateDeviceInfo();

                // 検知結果の自動消去
                UpdateDetectionResults();

                // バウンディングボックス更新
                UpdateBoundingBoxes();
            }
        }

        /// <summary>
        /// 情報レイヤーの初期化
        /// </summary>
        void InitializeInformationLayer()
        {
            // FPS計算用の初期化
            m_FrameCount = 0;
            m_FpsTimer = 0.0f;
            m_CurrentFPS = 0.0f;

            // 初期ステータス設定
            if (m_StatusText != null)
            {
                m_StatusText.text = "XREAL AI Camera 起動中...";
            }

            // クロスヘア初期設定
            if (m_Crosshair != null)
            {
                m_Crosshair.color = new Color(1, 1, 1, 0.5f);
            }
        }

        /// <summary>
        /// レイヤーの表示/非表示を更新
        /// </summary>
        void UpdateLayerVisibility()
        {
            if (m_InformationCanvas != null)
            {
                m_InformationCanvas.gameObject.SetActive(m_LayerEnabled);
            }
        }

        /// <summary>
        /// レイヤーの透過率を更新
        /// </summary>
        void UpdateLayerAlpha()
        {
            if (m_InformationCanvas != null && m_LayerEnabled)
            {
                CanvasGroup canvasGroup = m_InformationCanvas.GetComponent<CanvasGroup>();
                if (canvasGroup == null)
                {
                    canvasGroup = m_InformationCanvas.gameObject.AddComponent<CanvasGroup>();
                }
                canvasGroup.alpha = m_LayerAlpha;
            }
        }

        /// <summary>
        /// FPS計算と表示更新
        /// </summary>
        void UpdateFPS()
        {
            if (!m_ShowFPS || m_FpsText == null) return;

            m_FrameCount++;
            m_FpsTimer += Time.deltaTime;

            if (m_FpsTimer >= 1.0f)
            {
                m_CurrentFPS = m_FrameCount / m_FpsTimer;
                m_FrameCount = 0;
                m_FpsTimer = 0.0f;

                m_FpsText.text = $"FPS: {m_CurrentFPS:F1}";
                
                // FPSに応じて色変更
                if (m_CurrentFPS >= 55.0f)
                    m_FpsText.color = Color.green;
                else if (m_CurrentFPS >= 30.0f)
                    m_FpsText.color = Color.yellow;
                else
                    m_FpsText.color = Color.red;
            }
        }

        /// <summary>
        /// デバイス情報表示更新
        /// </summary>
        void UpdateDeviceInfo()
        {
            if (!m_ShowDeviceInfo || m_DeviceInfoText == null) return;

            string deviceInfo = $"XREAL Device: {SystemInfo.deviceModel}\n";
            deviceInfo += $"Resolution: {Screen.width}x{Screen.height}\n";
            deviceInfo += $"Memory: {SystemInfo.systemMemorySize}MB";

            m_DeviceInfoText.text = deviceInfo;
        }

        /// <summary>
        /// 検知結果の表示時間管理と自動消去
        /// </summary>
        void UpdateDetectionResults()
        {
            if (m_CurrentDetections.Count == 0) return;

            // 古い検知結果を削除
            for (int i = m_CurrentDetections.Count - 1; i >= 0; i--)
            {
                if (Time.time - m_CurrentDetections[i].detectionTime > m_DetectionDisplayDuration)
                {
                    m_CurrentDetections.RemoveAt(i);
                }
            }

            // 検知結果テキスト更新
            UpdateDetectionText();
        }

        /// <summary>
        /// 検知結果テキストの更新
        /// </summary>
        void UpdateDetectionText()
        {
            if (m_DetectionResultText == null) return;

            if (m_CurrentDetections.Count == 0)
            {
                m_DetectionResultText.text = "";
                if (m_ConfidenceText != null)
                    m_ConfidenceText.text = "";
                return;
            }

            string resultText = "検知結果:\n";
            string confidenceText = "信頼度:\n";

            foreach (var detection in m_CurrentDetections)
            {
                resultText += $"• {detection.objectName}\n";
                confidenceText += $"{detection.confidence:P1}\n";
            }

            m_DetectionResultText.text = resultText;
            if (m_ConfidenceText != null)
                m_ConfidenceText.text = confidenceText;
        }

        /// <summary>
        /// バウンディングボックスの表示更新
        /// </summary>
        void UpdateBoundingBoxes()
        {
            // 既存のバウンディングボックスを削除
            foreach (var box in m_BoundingBoxInstances)
            {
                if (box != null)
                    DestroyImmediate(box);
            }
            m_BoundingBoxInstances.Clear();

            // 現在の検知結果でバウンディングボックス作成
            foreach (var detection in m_CurrentDetections)
            {
                CreateBoundingBox(detection);
            }
        }

        /// <summary>
        /// バウンディングボックスを作成
        /// </summary>
        void CreateBoundingBox(DetectionResult detection)
        {
            if (m_BoundingBoxPrefab == null || m_InformationCanvas == null) return;

            GameObject boxInstance = Instantiate(m_BoundingBoxPrefab, m_InformationCanvas.transform);
            m_BoundingBoxInstances.Add(boxInstance);

            // 位置とサイズ設定（正規化座標からスクリーン座標に変換）
            RectTransform rectTransform = boxInstance.GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                RectTransform canvasRect = m_InformationCanvas.GetComponent<RectTransform>();
                float canvasWidth = canvasRect.rect.width;
                float canvasHeight = canvasRect.rect.height;

                Vector2 position = new Vector2(
                    detection.boundingBox.x * canvasWidth,
                    detection.boundingBox.y * canvasHeight
                );

                Vector2 size = new Vector2(
                    detection.boundingBox.width * canvasWidth,
                    detection.boundingBox.height * canvasHeight
                );

                rectTransform.anchoredPosition = position;
                rectTransform.sizeDelta = size;
            }

            // 色設定
            Image boxImage = boxInstance.GetComponent<Image>();
            if (boxImage != null)
            {
                boxImage.color = detection.boxColor;
            }

            // ラベル設定
            Text labelText = boxInstance.GetComponentInChildren<Text>();
            if (labelText != null)
            {
                labelText.text = $"{detection.objectName}\n{detection.confidence:P1}";
            }
        }

        /// <summary>
        /// 検知結果を追加表示
        /// </summary>
        /// <param name="objectName">物体名</param>
        /// <param name="confidence">信頼度 (0-1)</param>
        /// <param name="boundingBox">バウンディングボックス (正規化座標)</param>
        public void AddDetectionResult(string objectName, float confidence, Rect boundingBox)
        {
            DetectionResult result = new DetectionResult
            {
                objectName = objectName,
                confidence = confidence,
                boundingBox = boundingBox,
                boxColor = m_BoundingBoxColor,
                detectionTime = Time.time
            };

            m_CurrentDetections.Add(result);
            m_LastDetectionTime = Time.time;

            // ステータス更新
            if (m_StatusText != null)
            {
                m_StatusText.text = $"検知中: {m_CurrentDetections.Count}個の物体";
            }
        }

        /// <summary>
        /// 複数の検知結果を一括追加
        /// </summary>
        /// <param name="detections">検知結果リスト</param>
        public void AddDetectionResults(List<DetectionResult> detections)
        {
            m_CurrentDetections.Clear();
            m_CurrentDetections.AddRange(detections);
            m_LastDetectionTime = Time.time;

            foreach (var detection in detections)
            {
                detection.detectionTime = Time.time;
            }
        }

        /// <summary>
        /// 全検知結果をクリア
        /// </summary>
        public void ClearDetectionResults()
        {
            m_CurrentDetections.Clear();
            UpdateDetectionText();
            UpdateBoundingBoxes();

            if (m_StatusText != null)
            {
                m_StatusText.text = "待機中...";
            }
        }

        /// <summary>
        /// 設定メニューの表示/非表示切り替え
        /// </summary>
        public void ToggleSettingsMenu()
        {
            if (m_SettingsPanel != null)
            {
                m_SettingsPanel.SetActive(!m_SettingsPanel.activeSelf);
            }
        }

        /// <summary>
        /// ステータスメッセージ設定
        /// </summary>
        /// <param name="message">表示メッセージ</param>
        public void SetStatusMessage(string message)
        {
            if (m_StatusText != null)
            {
                m_StatusText.text = message;
            }
        }

        /// <summary>
        /// クロスヘアの表示/非表示設定
        /// </summary>
        /// <param name="visible">表示する場合true</param>
        public void SetCrosshairVisible(bool visible)
        {
            if (m_Crosshair != null)
            {
                m_Crosshair.gameObject.SetActive(visible);
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
        /// 情報レイヤー透過率をプログラムから制御
        /// </summary>
        /// <param name="alpha">透過率 (0-1)</param>
        public void SetLayerAlpha(float alpha)
        {
            m_LayerAlpha = Mathf.Clamp01(alpha);
            if (m_LayerEnabled)
            {
                UpdateLayerAlpha();
            }
        }

        /// <summary>
        /// FPS表示の有効/無効設定
        /// </summary>
        /// <param name="show">表示する場合true</param>
        public void SetFPSVisible(bool show)
        {
            m_ShowFPS = show;
            if (m_FpsText != null)
            {
                m_FpsText.gameObject.SetActive(show);
            }
        }

        /// <summary>
        /// デバイス情報表示の有効/無効設定
        /// </summary>
        /// <param name="show">表示する場合true</param>
        public void SetDeviceInfoVisible(bool show)
        {
            m_ShowDeviceInfo = show;
            if (m_DeviceInfoText != null)
            {
                m_DeviceInfoText.gameObject.SetActive(show);
            }
        }

        void OnDestroy()
        {
            // バウンディングボックスインスタンスのクリーンアップ
            foreach (var box in m_BoundingBoxInstances)
            {
                if (box != null)
                    DestroyImmediate(box);
            }
            m_BoundingBoxInstances.Clear();
        }
    }
} 