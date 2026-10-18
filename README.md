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

## 新手图形向导（已实现实验性流程）

启动应用后打开 **刷机向导**，系统将帮你检查 USB / ADB、识别 Pixel 设备型号与电量、从 Google 官网筛选稳定完整 OTA、下载并核验 SHA-256、引导进入 Recovery、受控 Sideload，并在手机重启后验证实际 Android 构建。

- 自动筛选存在地区/运营商版本歧义时不会擅自选择或下载。
- 可点击“无手机？查看流程演示”，在无设备情况下逐步查看完整七步操作（绝不执行设备命令）。
- 一键进入 Recovery 仅在官方固件校验、备份、许可、机型与电量检查通过后允许，需再次确认。
- Factory Image 的高级工具默认折叠且仅限只读检查。

[新手使用指南](docs/BEGINNER_GUIDE.md) · [无真机测试矩阵](docs/WIZARD_ACCEPTANCE.md) · [实机验收清单](docs/MANUAL_ACCEPTANCE.md)

## 当前进度

**0.1.0 开发预览阶段，尚未正式发布签名安装包。** 已实现只读设备检测、官方固件校验、人工确认式 OTA Sideload 原型；OTA 是实际系统更新操作，尚未完成 Pixel 真机验收，请勿将 CI 产物用于重要设备。Factory Image 分区刷写、解锁、上锁及擦除仍被禁用。

- WinUI 3 + Mica 原生 Windows 图形界面，简体中文、日本語、English 可即时切换。
- 使用官方 Android SDK Platform-Tools 检测 ADB / Fastboot 设备、Pixel 机型、Android 版本及硬件代号。
- 本地导入完整 OTA ZIP，检查 `META-INF/com/android/metadata`，计算 SHA-256 文件指纹。
- 默认拒绝多设备、未授权/无法识别设备、非 Pixel 设备、OTA 代号不匹配的情况。
- 可从 Google 官方页面在线核对 OTA / Factory ZIP 的 SHA-256；OTA 传输前二次校验固件、构建时间和设备身份，要求备份、许可与 Recovery 操作确认。
- 可对 Factory Image 做只读检查；任何 fastboot 分区刷写均禁止。
- 可查看会话诊断日志；没有遥测和上传设备数据的功能。

**注意：** 单纯计算本地 SHA-256 不具备可信性；只有与 Google HTTPS 页面公布值匹配的文件才有资格进入受控 OTA 流程。Recovery 侧仍需验证软件包签名。ADB 传输成功也不等于安装成功，应以手机 Recovery 屏幕为准。详细参阅 [OTA 操作说明](docs/OTA_WORKFLOW.md) 与 [安全约束](docs/FLASH_SAFETY.md)。

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

0.2 所需的 OTA 校验与受控传输代码已进入开发预览，但仍需 Pixel 真机、Google 页面格式及异常流程验收。0.3 先完成 Factory Image 只读检查；分区刷写须待反回滚与机型矩阵完备。另提供手动触发的**未签名诊断预发布工作流**，签名 MSIX 与商店发布仍需证书、应用身份和额外验收。完整安排见 [ROADMAP](docs/ROADMAP.md)。

## 隐私与许可

本地优先，不上传设备信息。反馈问题前请从日志中移除序列号与本机路径。见 [PRIVACY](PRIVACY.md)、[SECURITY](SECURITY.md)、[CONTRIBUTING](CONTRIBUTING.md)。采用 [MIT License](LICENSE)。

© 2026 KiYouJyo

### 应用内自动更新

在**关于 → 应用更新**点击“检查更新”。GitHub 签名版支持下载更新、SHA-256 与 MSIX 签名校验后“重启并更新”；Microsoft Store 版直接通过商店 API 在应用内下载安装。两者严格区分发布渠道，且 OTA 刷机过程中不会替换应用。只有正式稳定 Release 才会触发 GitHub 更新，验收包不会自动升级。[实现与测试说明](docs/IN_APP_UPDATES.md)。

### 导航与设置

“设备、更新向导、诊断日志”保持在主导航；“关于、设置”固定在最底部。**语言切换已经从汉堡菜单移到独立的设置页**，并加入系统／浅色／深色主题选择，三语及主题偏好可即时切换、重新打开后保持。[设置与导航设计](docs/SETTINGS_AND_NAVIGATION.md)。
