<p align="center"><img src="docs/assets/logo.svg" width="112" height="112" alt="FlowAndroid 图标"></p>
<h1 align="center">FlowAndroid</h1>
<p align="center">以 Google Pixel 为起点，打造直观、安全的 Windows 原生 Android 设备管理工具。</p>
<p align="center">
  <a href="https://github.com/KiYouJyo/FlowAndroid/actions/workflows/ci.yml"><img src="https://github.com/KiYouJyo/FlowAndroid/actions/workflows/ci.yml/badge.svg?branch=main" alt="CI"></a>
  <img src="https://img.shields.io/badge/Windows-WinUI%203-0078D4" alt="WinUI 3">
  <img src="https://img.shields.io/badge/Version-0.1.0_preview-64748b" alt="Preview">
  <img src="https://img.shields.io/badge/License-MIT-2EA043" alt="MIT">
</p>
<p align="center">简体中文 | <a href="README.ja-JP.md">日本語</a> | <a href="README.en-US.md">English</a></p>

## 当前进度

**0.1.0 诊断预览阶段，尚未正式发布可安装版本。** 此版本仅进行只读检测，绝不自动执行刷机、解锁、上锁或清除用户数据。

- WinUI 3 + Mica 原生 Windows 图形界面，简体中文、日本語、English 可即时切换。
- 使用官方 Android SDK Platform-Tools 检测 ADB / Fastboot 设备、Pixel 机型、Android 版本及硬件代号。
- 本地导入完整 OTA ZIP，检查 `META-INF/com/android/metadata`，计算 SHA-256 文件指纹。
- 默认拒绝多设备、未授权/无法识别设备、非 Pixel 设备、OTA 代号不匹配的情况。
- 可查看会话诊断日志；没有遥测和上传设备数据的功能。

**注意：** 本地计算 SHA-256 不能证明固件来自 Google，也不代表验证了官方签名。兼容性检查通过不意味着允许刷写。

## 获取与构建

暂未发布正式 Release 或 Microsoft Store 版本。CI 成功后可在 [Actions](https://github.com/KiYouJyo/FlowAndroid/actions/workflows/ci.yml) 下载仅供开发诊断的 x64 构建产物（不是签名安装包）。

开发环境：Windows 10 2004+ / Windows 11、.NET 10 SDK、Visual Studio 的 WinUI / Windows App SDK 开发工具。需要另外安装 [Android SDK Platform-Tools](https://developer.android.com/tools/releases/platform-tools)，并加入 PATH 或放在应用目录的 `platform-tools` 子目录。

```powershell
dotnet restore FlowAndroid.slnx
dotnet test tests/FlowAndroid.Core.Tests/FlowAndroid.Core.Tests.csproj -c Release
dotnet build src/FlowAndroid.App/FlowAndroid.App.csproj -c Release -p:Platform=x64
```

运行构建出的 `FlowAndroid.App.exe` 还需要对应 Windows App SDK Runtime。

## 项目结构

- `src/FlowAndroid.App` — 原生 WinUI 3 UI、动态本地化。
- `src/FlowAndroid.Core` — ADB/Fastboot 探测、OTA 检查、安全状态机基础。
- `tests` — 设备输出解析、兼容性及固件包单元测试。
- `docs` — [路线图](docs/ROADMAP.md)、[架构](docs/ARCHITECTURE.md)、[刷机安全](docs/FLASH_SAFETY.md)、[发布说明](docs/RELEASE.md)。
- `.github/workflows` — CI。

## 计划

0.2 版本将研究官方 OTA 来源验证、受控 Sideload 流程与设备状态复核；0.3 再考虑 Factory Image。签名 MSIX、公开证书和 SHA256SUMS 将在发布流程具备签名条件后实现。完整安排见 [ROADMAP](docs/ROADMAP.md)。

## 隐私与许可

本地优先，不上传设备信息。反馈问题前请从日志中移除序列号与本机路径。见 [PRIVACY](PRIVACY.md)、[SECURITY](SECURITY.md)、[CONTRIBUTING](CONTRIBUTING.md)。采用 [MIT License](LICENSE)。

© 2026 KiYouJyo
