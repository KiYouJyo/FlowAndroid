<p align="center"><img src="docs/assets/logo.svg" width="112" height="112" alt="FlowAndroid アイコン"></p>
<h1 align="center">FlowAndroid</h1>
<p align="center">Google Pixel から始める、Windows ネイティブの Android デバイス管理ツール。</p>
<p align="center">
  <a href="https://github.com/KiYouJyo/FlowAndroid/actions/workflows/ci.yml"><img src="https://github.com/KiYouJyo/FlowAndroid/actions/workflows/ci.yml/badge.svg?branch=main" alt="CI"></a>
  <img src="https://img.shields.io/badge/Windows-WinUI%203-0078D4" alt="WinUI 3">
  <img src="https://img.shields.io/badge/Version-0.1.0_preview-64748b" alt="Preview">
</p>
<p align="center"><a href="README.md">简体中文</a> | 日本語 | <a href="README.en-US.md">English</a></p>

## Pixel 初心者向け更新ウィザード

USB 接続・端末識別、Google 公式 OTA の安定版候補、SHA-256 検証付きダウンロード、確認付き Recovery 再起動、監督下の Sideload、再起動後のビルド確認まで案内します。地域・キャリアの不明なビルドは自動選択しません。端末なしでも書き込み不可の七段階デモを使用できます。Factory Image は読み取り専用です。

[ガイド](docs/BEGINNER_GUIDE.md)、[テスト項目](docs/WIZARD_ACCEPTANCE.md)。Pixel 実機検証は未完了です。

## 開発状況

**v0.1.0 は未公開の開発プレビューです。** デバイス診断、Google 公式 SHA-256 の照合、確認付き OTA Sideload の試作を実装しました。OTA 転送はシステムを変更しますが、Pixel 実機検証は未完了のため通常使用を推奨しません。Factory パーティション書き込み、ロック変更、初期化は無効です。

- WinUI 3 / Mica によるネイティブ UI。中国語・日本語・英語を再起動せずに切り替え。
- 公式 Android SDK Platform-Tools で ADB / Fastboot を確認し、Pixel の型番・Android バージョン・コード名を取得。
- ローカルのフル OTA ZIP のメタデータを検査し、SHA-256 を計算。
- 複数デバイス、未承認、非 Pixel、OTA コード名不一致を安全側で拒否。
- Google 公式ページの SHA-256 と OTA / Factory ZIP を照合します。
- バックアップ・規約・Recovery の明示確認後、ビルド日時や元のデバイス識別子を再確認して OTA を転送する試作です。
- Factory ZIP を読み取り専用で検査し、スクリプト実行とパーティション書き込みを禁止します。
- セッション内の診断ログ。テレメトリとデバイス情報アップロードはありません。

**注意：** ローカル SHA-256 だけでは真正性を証明できません。Google が公表したチェックサムと照合し、Recovery 側での署名検証も必要です。ADB 転送完了はインストール成功を保証しません。[OTA 手順](docs/OTA_WORKFLOW.md)を参照してください。

## ビルド方法

Windows 10 2004 以降 / Windows 11、.NET 10 SDK、Visual Studio WinUI / Windows App SDK 開発環境が必要です。[Android SDK Platform-Tools](https://developer.android.com/tools/releases/platform-tools) を PATH またはアプリ直下の `platform-tools` に配置してください。

```powershell
dotnet restore FlowAndroid.slnx
dotnet test tests/FlowAndroid.Core.Tests/FlowAndroid.Core.Tests.csproj -c Release
dotnet build src/FlowAndroid.App/FlowAndroid.App.csproj -c Release -p:Platform=x64
```

生成された exe の実行には対応する Windows App SDK Runtime が必要です。署名済み MSIX、Microsoft Store、正式 Release はまだありません。CI が成功すれば [Actions](https://github.com/KiYouJyo/FlowAndroid/actions/workflows/ci.yml) から開発用の成果物を取得できます。

## 構成と予定

`src/FlowAndroid.App` は UI、`src/FlowAndroid.Core` は検出と安全検査、`tests` は単体テストです。[アーキテクチャ](docs/ARCHITECTURE.md)、[ロードマップ](docs/ROADMAP.md)、[安全ポリシー](docs/FLASH_SAFETY.md)、[リリース手順](docs/RELEASE.md) を参照してください。

v0.2 相当の OTA 検証・転送コードはありますが実機試験が未完了です。Factory Image は検査のみ可能で、実際のパーティション書き込みは禁止しています。未署名の開発用 GitHub プレリリースは手動実行できます。署名済み MSIX と Store 公開には証明書・製品 ID と追加検証が必要です。プライバシーについては [PRIVACY](PRIVACY.md)、[SECURITY](SECURITY.md)、[CONTRIBUTING](CONTRIBUTING.md) を参照。[MIT ライセンス](LICENSE)。© 2026 KiYouJyo。
