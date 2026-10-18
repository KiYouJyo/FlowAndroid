# FlowAndroid 应用内更新 / In-app updates / アプリ内更新

## 用户操作（无需浏览器）

打开 **关于 → 应用更新**，可看到已安装 MSIX 包版本、更新来源和检查更新按钮。

### GitHub 签名侧载版
1. 点击 **检查更新**：访问仓库 GitHub Releases 的最新**正式版**。不会自动使用 Action Artifacts、草稿和 RC 预发行包。
2. 发现严格高于当前应用产品版本的更新后，显示版本和更新日志。
3. 点击 **下载并验证**：下载固定名称 `FlowAndroid-<major>.<minor>.<patch>-x64.msix` 及 `SHA256SUMS.txt`。
4. 本地检查 SHA-256；然后使用 **WinVerifyTrust** 验证 Windows MSIX 签名，并确认包的 Name、Publisher 与当前安装身份相同，且完整四段 MSIX 版本号高于当前安装版本。任一检查不通过均禁止安装。
5. 点击 **重启并更新**，在二次确认后 `RegisterApplicationRestart`，再调用 Windows `PackageManager.AddPackageAsync(..., ForceApplicationShutdown)` 交给系统安全地替换旧版并重启。失败时保留旧版和可读提示，不启动任意 PowerShell/安装器进程。
6. OTA 传输中禁止替换正在运行的 FlowAndroid，防止手机刷机过程被中断。

### Microsoft Store 版
- Store 工作流使用 `FLOWANDROID_STORE` 独立构建标记；不会使用 GitHub 包。
- 使用 `Windows.Services.Store.StoreContext.GetAppAndOptionalStorePackageUpdatesAsync` 检查，通过 `RequestDownloadAndInstallStorePackageUpdatesAsync` 直接在应用内下载、安装。
- 显示进度和商店返回的真实版本；由 Windows Store 管理可用性和渠道。无需跳转 Store 页面。

### 开发版、验收版与无正式 Release
- 未打包运行的 Debug 应用会说明“先安装签名 MSIX”；不得偷偷更新为其他软件身份。
- 截至首次实现，只有签名验收包，尚未发布通过实机验证的 GitHub **stable** Release，所以界面会说明“暂无正式版”，不会误把验收版当作可用更新。
- 首次正式发布必须完成 `docs/MANUAL_ACCEPTANCE.md` 的 Windows/Pixel 验收。然后手动运行 **MSIX build and signing**，勾选 `sign_package=true`、`hardware_accepted=true`、`publish_stable=true`，设置 `release_version`（例如 `0.2.0`）。流程会生成正式版本对应的签名 MSIX + SHA256SUMS 并发布版本 `v0.2.0`。这一步不会自动发生。

## 安全/兼容性
- 发布证书必须匹配 Windows 当前已安装应用的发布者，否则会被拒绝升级。更换自签名证书等同于切换 Windows Publisher，不能假装无缝升级。
- MSIX 包版本严格增长；第四段由 GitHub Actions 运行编号填充。产品版本使用 `major.minor.patch`。
- Github 与 Store 是分离的应用身份/发布渠道，不跨渠道更新。
- 程序只读取 GitHub 的稳定版 Release，下载校验值与包体必须符合固定资产命名和来源约束；既检查 SHA-256 又检查 Windows Authenticode/MSIX 包身份。
- 真机 Recovery 或 OTA 正在执行时，应用更新按钮不会结束 FlowAndroid 自身进程。
- 已缓存的 MSIX 最终仍会在安装时再核对 Windows 包签名/身份与版本。

## 日本語の要約

「情報」→「アプリ更新」→「更新を確認」。GitHub 版は正式リリースのみを使用し、ダウンロード後に SHA-256、MSIX 署名と Publisher を検証し、「再起動して更新」で Windows に引き継ぎます。Store 版は `StoreContext` からアプリ内で更新します。Pixel OTA 転送中のアプリ更新は禁止です。端末実機の受け入れ試験は別途必要です。

## English summary

About → App updates → Check for updates. GitHub builds check **stable** releases only, verify SHA-256, Windows signature and installed package identity before handing installation and restart to Windows. Store builds use native StoreContext APIs without leaving the app. App replacement is blocked during an active Pixel OTA transfer. Hardware acceptance is still required.
