# GitHub Actions Unity クラウドビルド設定手順

## 🔑 必要なSecrets設定

GitHubリポジトリの Settings > Secrets and variables > Actions で以下を設定：

### Unity ライセンス
```
UNITY_LICENSE: Unity Personal/Pro ライセンス文字列
UNITY_EMAIL: Unityアカウントのメールアドレス  
UNITY_PASSWORD: Unityアカウントのパスワード
```

### Android Signing (リリース用)
```
ANDROID_KEYSTORE_BASE64: Keystoreファイルのbase64エンコード
ANDROID_KEYSTORE_PASS: Keystoreのパスワード
ANDROID_KEYALIAS_NAME: Key aliasの名前
ANDROID_KEYALIAS_PASS: Key aliasのパスワード
```

## ⚡ クラウドビルドの利点

### 🚀 メリット
- **並列ビルド**: 複数プラットフォーム同時
- **チーム共有**: 誰でもビルドDL可能
- **自動化**: プッシュで自動ビルド
- **履歴管理**: 各コミットのAPK保存

### ⚠️ 制限・課題

#### 1. **XReal SDK問題**
- XReal SDK（NRSDK）はプライベートSDK
- GitHub Actionsでの自動DLは困難
- **解決方法**: SDKをリポジトリに含める（容量注意）

#### 2. **Unity ライセンス**
- **Personal**: 2000分/月まで無料
- **Pro**: $185/月で無制限
- **学生**: Education ライセンス利用可能

#### 3. **ビルド時間**
- **ローカル**: 47秒
- **GitHub Actions**: 10-15分（環境構築時間含む）
- **初回**: 20-30分（キャッシュなし）

#### 4. **Android SDK**
- 自動セットアップされるが時間がかかる
- Target SDK 34, MinSDK 28 対応

## 🎯 推奨使用ケース

### ✅ 適している場面
- **チーム開発**: 複数人でのAPK共有
- **CI/CD**: 自動テスト + 自動ビルド
- **リリース管理**: タグプッシュで自動配布
- **バックアップ**: ビルド成果物の自動保存

### ❌ 避けるべき場面
- **高速イテレーション**: ローカル47秒 vs クラウド15分
- **プライベートSDK**: XReal SDKの権利問題
- **コスト重視**: Personal ライセンスの制限

## 🔧 代替案

### Unity Cloud Build
- Unity公式サービス
- プライベートSDK対応良好
- $9/月から

### 自前Jenkins/GitLab CI
- 自社サーバーでビルド
- 完全制御可能
- 初期コスト高

## 📊 結論

**開発段階**: ローカルビルド（47秒）推奨  
**リリース段階**: GitHub Actions活用  
**チーム開発**: Unity Cloud Build検討 