using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

namespace Unity.AI.Cam
{
    /// <summary>
    /// XREAL One Pro & XREAL Eye レイヤー管理システム
    /// 生カメラ、処理済み画像、情報の3レイヤーを統合管理
    /// </summary>
    public class XREALLayerManager : MonoBehaviour
    {
        [Header("XREAL レイヤー管理")]
        [SerializeField]
        [Tooltip("生カメラレイヤー（底層）")]
        XREALRawCameraLayer m_RawCameraLayer;

        [SerializeField]
        [Tooltip("処理済み画像レイヤー（中層）")]
        XREALProcessedImageLayer m_ProcessedImageLayer;

        [SerializeField]
        [Tooltip("情報レイヤー（最上層）")]
        XREALInformationLayer m_InformationLayer;

        [Header("統合制御")]
        [SerializeField]
        [Tooltip("全レイヤーのマスター有効/無効")]
        bool m_MasterEnabled = true;

        [SerializeField]
        [Range(0.0f, 1.0f)]
        [Tooltip("全レイヤーのマスター透過率")]
        float m_MasterAlpha = 1.0f;

        [Header("レイヤー個別制御")]
        [SerializeField]
        [Tooltip("生カメラレイヤー有効")]
        bool m_RawCameraEnabled = true;

        [SerializeField]
        [Tooltip("処理済み画像レイヤー有効")]
        bool m_ProcessedImageEnabled = true;

        [SerializeField]
        [Tooltip("情報レイヤー有効")]
        bool m_InformationEnabled = true;

        [Header("AI連携設定")]
        [SerializeField]
        [Tooltip("AI処理を有効にする")]
        bool m_AIProcessingEnabled = true;

        [SerializeField]
        [Range(0.1f, 5.0f)]
        [Tooltip("AI処理の実行間隔（秒）")]
        float m_AIProcessingInterval = 1.0f;

        [SerializeField]
        [Tooltip("AI処理タイプ")]
        XREALProcessedImageLayer.ProcessingType m_AIProcessingType = XREALProcessedImageLayer.ProcessingType.EdgeDetection;

        [Header("UI制御")]
        [SerializeField]
        [Tooltip("レイヤー制御UI")]
        GameObject m_LayerControlUI;

        [SerializeField]
        [Tooltip("生カメラON/OFFボタン")]
        Toggle m_RawCameraToggle;

        [SerializeField]
        [Tooltip("処理済み画像ON/OFFボタン")]
        Toggle m_ProcessedImageToggle;

        [SerializeField]
        [Tooltip("情報レイヤーON/OFFボタン")]
        Toggle m_InformationToggle;

        [SerializeField]
        [Tooltip("マスター透過率スライダー")]
        Slider m_MasterAlphaSlider;

        // 内部管理データ
        private float m_LastAIProcessingTime;
        private bool m_PreviousMasterEnabled;
        private float m_PreviousMasterAlpha;
        private bool m_PreviousRawCameraEnabled;
        private bool m_PreviousProcessedImageEnabled;
        private bool m_PreviousInformationEnabled;

        // AI処理結果キャッシュ
        private List<XREALInformationLayer.DetectionResult> m_CachedDetections = new List<XREALInformationLayer.DetectionResult>();

        void Start()
        {
            // 初期化
            InitializeLayerManager();
            SetupUI();
            UpdateAllLayers();
        }

        void Update()
        {
            // マスター設定の変更検知
            if (m_MasterEnabled != m_PreviousMasterEnabled)
            {
                UpdateMasterEnabled();
                m_PreviousMasterEnabled = m_MasterEnabled;
            }

            if (Mathf.Abs(m_MasterAlpha - m_PreviousMasterAlpha) > 0.001f)
            {
                UpdateMasterAlpha();
                m_PreviousMasterAlpha = m_MasterAlpha;
            }

            // 個別レイヤー設定の変更検知
            if (m_RawCameraEnabled != m_PreviousRawCameraEnabled)
            {
                UpdateRawCameraLayer();
                m_PreviousRawCameraEnabled = m_RawCameraEnabled;
            }

            if (m_ProcessedImageEnabled != m_PreviousProcessedImageEnabled)
            {
                UpdateProcessedImageLayer();
                m_PreviousProcessedImageEnabled = m_ProcessedImageEnabled;
            }

            if (m_InformationEnabled != m_PreviousInformationEnabled)
            {
                UpdateInformationLayer();
                m_PreviousInformationEnabled = m_InformationEnabled;
            }

            // AI処理実行
            if (m_AIProcessingEnabled && Time.time - m_LastAIProcessingTime > m_AIProcessingInterval)
            {
                ProcessAIDetection();
                m_LastAIProcessingTime = Time.time;
            }
        }

        /// <summary>
        /// レイヤーマネージャーの初期化
        /// </summary>
        void InitializeLayerManager()
        {
            // レイヤー参照の自動取得
            if (m_RawCameraLayer == null)
                m_RawCameraLayer = FindObjectOfType<XREALRawCameraLayer>();

            if (m_ProcessedImageLayer == null)
                m_ProcessedImageLayer = FindObjectOfType<XREALProcessedImageLayer>();

            if (m_InformationLayer == null)
                m_InformationLayer = FindObjectOfType<XREALInformationLayer>();

            // 初期設定の保存
            m_PreviousMasterEnabled = m_MasterEnabled;
            m_PreviousMasterAlpha = m_MasterAlpha;
            m_PreviousRawCameraEnabled = m_RawCameraEnabled;
            m_PreviousProcessedImageEnabled = m_ProcessedImageEnabled;
            m_PreviousInformationEnabled = m_InformationEnabled;

            // 初期AI処理時間設定
            m_LastAIProcessingTime = Time.time;

            // ステータス通知
            if (m_InformationLayer != null)
            {
                m_InformationLayer.SetStatusMessage("XREAL レイヤーシステム 初期化完了");
            }
        }

        /// <summary>
        /// UI設定とイベント接続
        /// </summary>
        void SetupUI()
        {
            // トグルボタンイベント設定
            if (m_RawCameraToggle != null)
            {
                m_RawCameraToggle.isOn = m_RawCameraEnabled;
                m_RawCameraToggle.onValueChanged.AddListener(SetRawCameraEnabled);
            }

            if (m_ProcessedImageToggle != null)
            {
                m_ProcessedImageToggle.isOn = m_ProcessedImageEnabled;
                m_ProcessedImageToggle.onValueChanged.AddListener(SetProcessedImageEnabled);
            }

            if (m_InformationToggle != null)
            {
                m_InformationToggle.isOn = m_InformationEnabled;
                m_InformationToggle.onValueChanged.AddListener(SetInformationEnabled);
            }

            // スライダーイベント設定
            if (m_MasterAlphaSlider != null)
            {
                m_MasterAlphaSlider.value = m_MasterAlpha;
                m_MasterAlphaSlider.onValueChanged.AddListener(SetMasterAlpha);
            }
        }

        /// <summary>
        /// 全レイヤーの設定を更新
        /// </summary>
        void UpdateAllLayers()
        {
            UpdateMasterEnabled();
            UpdateMasterAlpha();
            UpdateRawCameraLayer();
            UpdateProcessedImageLayer();
            UpdateInformationLayer();
        }

        /// <summary>
        /// マスター有効/無効の更新
        /// </summary>
        void UpdateMasterEnabled()
        {
            if (!m_MasterEnabled)
            {
                // マスター無効時は全レイヤー無効
                if (m_RawCameraLayer != null)
                    m_RawCameraLayer.SetLayerEnabled(false);
                if (m_ProcessedImageLayer != null)
                    m_ProcessedImageLayer.SetLayerEnabled(false);
                if (m_InformationLayer != null)
                    m_InformationLayer.SetLayerEnabled(false);
            }
            else
            {
                // マスター有効時は個別設定に従う
                UpdateRawCameraLayer();
                UpdateProcessedImageLayer();
                UpdateInformationLayer();
            }
        }

        /// <summary>
        /// マスター透過率の更新
        /// </summary>
        void UpdateMasterAlpha()
        {
            if (m_RawCameraLayer != null)
                m_RawCameraLayer.SetCameraAlpha(m_MasterAlpha);

            if (m_ProcessedImageLayer != null)
                m_ProcessedImageLayer.SetImageAlpha(m_MasterAlpha * 0.7f);

            if (m_InformationLayer != null)
                m_InformationLayer.SetLayerAlpha(m_MasterAlpha * 0.9f);
        }

        /// <summary>
        /// 生カメラレイヤーの更新
        /// </summary>
        void UpdateRawCameraLayer()
        {
            if (m_RawCameraLayer != null)
            {
                m_RawCameraLayer.SetLayerEnabled(m_MasterEnabled && m_RawCameraEnabled);
            }
        }

        /// <summary>
        /// 処理済み画像レイヤーの更新
        /// </summary>
        void UpdateProcessedImageLayer()
        {
            if (m_ProcessedImageLayer != null)
            {
                m_ProcessedImageLayer.SetLayerEnabled(m_MasterEnabled && m_ProcessedImageEnabled);
                
                if (m_ProcessedImageEnabled)
                {
                    m_ProcessedImageLayer.SetProcessingType(m_AIProcessingType);
                }
            }
        }

        /// <summary>
        /// 情報レイヤーの更新
        /// </summary>
        void UpdateInformationLayer()
        {
            if (m_InformationLayer != null)
            {
                m_InformationLayer.SetLayerEnabled(m_MasterEnabled && m_InformationEnabled);
            }
        }

        /// <summary>
        /// AI検知処理実行（サンプル実装）
        /// </summary>
        void ProcessAIDetection()
        {
            if (!m_AIProcessingEnabled || m_InformationLayer == null) return;

            // サンプル検知結果生成（実際はAIモデルからの結果）
            List<XREALInformationLayer.DetectionResult> detections = GenerateSampleDetections();
            
            if (detections.Count > 0)
            {
                m_InformationLayer.AddDetectionResults(detections);
                m_CachedDetections = detections;
            }
        }

        /// <summary>
        /// サンプル検知結果生成（テスト用）
        /// </summary>
        List<XREALInformationLayer.DetectionResult> GenerateSampleDetections()
        {
            List<XREALInformationLayer.DetectionResult> results = new List<XREALInformationLayer.DetectionResult>();

            // ランダムなサンプル検知結果
            if (Random.Range(0f, 1f) < 0.3f) // 30%の確率で検知
            {
                string[] sampleObjects = { "人", "車", "建物", "動物", "植物", "看板" };
                string objectName = sampleObjects[Random.Range(0, sampleObjects.Length)];
                
                XREALInformationLayer.DetectionResult result = new XREALInformationLayer.DetectionResult
                {
                    objectName = objectName,
                    confidence = Random.Range(0.6f, 0.95f),
                    boundingBox = new Rect(
                        Random.Range(0.1f, 0.7f),
                        Random.Range(0.1f, 0.7f),
                        Random.Range(0.1f, 0.3f),
                        Random.Range(0.1f, 0.3f)
                    ),
                    boxColor = Color.green,
                    detectionTime = Time.time
                };

                results.Add(result);
            }

            return results;
        }

        // ===== Public API Methods =====

        /// <summary>
        /// マスター有効/無効設定
        /// </summary>
        /// <param name="enabled">有効にする場合true</param>
        public void SetMasterEnabled(bool enabled)
        {
            m_MasterEnabled = enabled;
            UpdateMasterEnabled();
        }

        /// <summary>
        /// マスター透過率設定
        /// </summary>
        /// <param name="alpha">透過率 (0-1)</param>
        public void SetMasterAlpha(float alpha)
        {
            m_MasterAlpha = Mathf.Clamp01(alpha);
            UpdateMasterAlpha();
        }

        /// <summary>
        /// 生カメラレイヤー有効/無効設定
        /// </summary>
        /// <param name="enabled">有効にする場合true</param>
        public void SetRawCameraEnabled(bool enabled)
        {
            m_RawCameraEnabled = enabled;
            if (m_RawCameraToggle != null && m_RawCameraToggle.isOn != enabled)
                m_RawCameraToggle.isOn = enabled;
            UpdateRawCameraLayer();
        }

        /// <summary>
        /// 処理済み画像レイヤー有効/無効設定
        /// </summary>
        /// <param name="enabled">有効にする場合true</param>
        public void SetProcessedImageEnabled(bool enabled)
        {
            m_ProcessedImageEnabled = enabled;
            if (m_ProcessedImageToggle != null && m_ProcessedImageToggle.isOn != enabled)
                m_ProcessedImageToggle.isOn = enabled;
            UpdateProcessedImageLayer();
        }

        /// <summary>
        /// 情報レイヤー有効/無効設定
        /// </summary>
        /// <param name="enabled">有効にする場合true</param>
        public void SetInformationEnabled(bool enabled)
        {
            m_InformationEnabled = enabled;
            if (m_InformationToggle != null && m_InformationToggle.isOn != enabled)
                m_InformationToggle.isOn = enabled;
            UpdateInformationLayer();
        }

        /// <summary>
        /// AI処理タイプ変更
        /// </summary>
        /// <param name="processingType">処理タイプ</param>
        public void SetAIProcessingType(XREALProcessedImageLayer.ProcessingType processingType)
        {
            m_AIProcessingType = processingType;
            if (m_ProcessedImageLayer != null)
            {
                m_ProcessedImageLayer.SetProcessingType(processingType);
            }
        }

        /// <summary>
        /// AI処理有効/無効設定
        /// </summary>
        /// <param name="enabled">有効にする場合true</param>
        public void SetAIProcessingEnabled(bool enabled)
        {
            m_AIProcessingEnabled = enabled;
            
            if (m_InformationLayer != null)
            {
                if (enabled)
                {
                    m_InformationLayer.SetStatusMessage("AI処理 有効");
                }
                else
                {
                    m_InformationLayer.SetStatusMessage("AI処理 無効");
                    m_InformationLayer.ClearDetectionResults();
                }
            }
        }

        /// <summary>
        /// 検知結果を手動追加
        /// </summary>
        /// <param name="objectName">物体名</param>
        /// <param name="confidence">信頼度 (0-1)</param>
        /// <param name="boundingBox">バウンディングボックス</param>
        public void AddDetectionResult(string objectName, float confidence, Rect boundingBox)
        {
            if (m_InformationLayer != null)
            {
                m_InformationLayer.AddDetectionResult(objectName, confidence, boundingBox);
            }
        }

        /// <summary>
        /// 全検知結果をクリア
        /// </summary>
        public void ClearAllDetections()
        {
            if (m_InformationLayer != null)
            {
                m_InformationLayer.ClearDetectionResults();
            }
            m_CachedDetections.Clear();
        }

        /// <summary>
        /// レイヤー制御UIの表示/非表示
        /// </summary>
        /// <param name="visible">表示する場合true</param>
        public void SetLayerControlUIVisible(bool visible)
        {
            if (m_LayerControlUI != null)
            {
                m_LayerControlUI.SetActive(visible);
            }
        }

        /// <summary>
        /// 現在のレイヤー状態を取得
        /// </summary>
        /// <returns>レイヤー状態情報</returns>
        public string GetLayerStatusInfo()
        {
            string status = "=== XREAL レイヤー状態 ===\n";
            status += $"マスター: {(m_MasterEnabled ? "ON" : "OFF")} (Alpha: {m_MasterAlpha:F2})\n";
            status += $"生カメラ: {(m_RawCameraEnabled ? "ON" : "OFF")}\n";
            status += $"処理済み画像: {(m_ProcessedImageEnabled ? "ON" : "OFF")}\n";
            status += $"情報レイヤー: {(m_InformationEnabled ? "ON" : "OFF")}\n";
            status += $"AI処理: {(m_AIProcessingEnabled ? "ON" : "OFF")}\n";
            status += $"検知結果: {m_CachedDetections.Count}個";
            
            return status;
        }

        /// <summary>
        /// デバッグ用：レイヤー状態をコンソール出力
        /// </summary>
        [ContextMenu("デバッグ: レイヤー状態出力")]
        public void DebugPrintLayerStatus()
        {
            Debug.Log(GetLayerStatusInfo());
        }

        /// <summary>
        /// テスト用：サンプル検知実行
        /// </summary>
        [ContextMenu("テスト: サンプル検知実行")]
        public void TestDetection()
        {
            List<XREALInformationLayer.DetectionResult> testDetections = new List<XREALInformationLayer.DetectionResult>
            {
                new XREALInformationLayer.DetectionResult
                {
                    objectName = "テスト物体1",
                    confidence = 0.85f,
                    boundingBox = new Rect(0.2f, 0.3f, 0.3f, 0.4f),
                    boxColor = Color.green,
                    detectionTime = Time.time
                },
                new XREALInformationLayer.DetectionResult
                {
                    objectName = "テスト物体2",
                    confidence = 0.72f,
                    boundingBox = new Rect(0.6f, 0.5f, 0.2f, 0.3f),
                    boxColor = Color.blue,
                    detectionTime = Time.time
                }
            };

            if (m_InformationLayer != null)
            {
                m_InformationLayer.AddDetectionResults(testDetections);
            }
        }
    }
} 