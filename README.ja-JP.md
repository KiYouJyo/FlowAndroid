<p align="center"><img src="docs/assets/logo.svg" width="112" height="112" alt="FlowAndroid アイコン"></p>
<h1 align="center">FlowAndroid</h1>
<p align="center">Google Pixel から始める、Windows ネイティブの Android デバイス管理ツール。</p>
<p align="center">
  <a href="https://github.com/KiYouJyo/FlowAndroid/actions/workflows/ci.yml"><img src="https://github.com/KiYouJyo/FlowAndroid/actions/workflows/ci.yml/badge.svg?branch=main" alt="CI"></a>
  <img src="https://img.shields.io/badge/Windows-WinUI%203-0078D4" alt="WinUI 3">
  <img src="https://img.shields.io/badge/Version-0.1.0_preview-64748b" alt="Preview">
</p>
<p align="center"><a href="README.md">简体中文</a> | 日本語 | <a href="README.en-US.md">English</a></p>

## 開発状況

**v0.1.0 は未公開の診断プレビューです。** 読み取り専用の検査のみ可能です。ファームウェア書き込み、ブートローダーのロック変更、初期化、任意コマンドの実行はできません。

- WinUI 3 / Mica によるネイティブ UI。中国語・日本語・英語を再起動せずに切り替え。
- 公式 Android SDK Platform-Tools で ADB / Fastboot を確認し、Pixel の型番・Android バージョン・コード名を取得。
- ローカルのフル OTA ZIP のメタデータを検査し、SHA-256 を計算。
- 複数デバイス、未承認、非 Pixel、OTA コード名不一致を安全側で拒否。
- セッション内の診断ログ。テレメトリとデバイス情報アップロードはありません。

**注意：** ローカルで計算した SHA-256 は Google による署名や配布元の真正性を証明するものではありません。

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

v0.2 では配布元の検証と安全な公式 OTA Sideload、以降では Factory Image と署名済み MSIX を計画しています。プライバシーについては [PRIVACY](PRIVACY.md)、[SECURITY](SECURITY.md)、[CONTRIBUTING](CONTRIBUTING.md) を参照。[MIT ライセンス](LICENSE)。© 2026 KiYouJyo。
