# FlowAndroid — Pixel 新手刷机向导

## 首选：官方完整 OTA（保留数据）

Google 官方说明：完整 OTA 通常无需解锁 Bootloader、无需清空手机数据。与此不同，Factory Image 的官方刷写通常会删除用户数据，因此 **FlowAndroid 默认只推荐官方完整 OTA**。请先备份，尤其是准备重要升级时。

## 交互式向导的七步

1. **连接手机**：打开 Pixel 的开发者选项与 USB 调试，用支持传输数据的 USB 线连接电脑，允许 USB 调试授权。只保留一个设备。应用检查官方 Platform-Tools、设备状态、真实机型、代号、系统构建和电量。没有手机也可以点击“无手机？查看流程演示”逐步体验。
2. **自动筛选**：点击“自动查找适配固件”。从 Google 官方目录加载完整 OTA，只保留设备代号对应的生产稳定版，排除 Beta、Preview、Canary。若最新发布日有多个地区／运营商版本，**不替用户猜测**，转为人工阅读 Google 的官方说明。
3. **下载与验证**：有唯一候选时自动预选，点击“下载并校验”。文件保存在当前用户的 FlowAndroid 固件缓存目录中；下载过程中可取消、可见真实字节比例；每次下载都会与 Google 官网页面公布的 SHA-256 比对。失败的未完成文件会被清理。
4. **兼容性复核**：导入或下载完成后，校验 OTA 内部 `pre-device`、更新类型和构建时间。不存在可信固件、设备代号不同、电量低于 50%、尝试回退版本等都阻止继续。注意：机型代号或哈希匹配仍不构成适用于所有地区/SKU/运营商的保证。
5. **备份并进入 Recovery**：确认备份和 Google 使用条款后，可点击“自动重启进入 Recovery”。应用在指令执行前再次确认仅有原来的那一台 Pixel，发送 `adb -s <serial> reboot recovery`。也可以手动进入 Recovery。出现 Android 红色感叹号时，按住电源键并点按音量上键一次，使用音量键选 **Apply update from ADB**。
6. **有确认才传输**：在手机确认已进入 Sideload 模式后，回到应用勾选确认，阅读最后一次对话框。系统再次检查 Google SHA-256、目标版本和设备序列号后，才执行官方完整 OTA 的单设备 ADB Sideload。传输是不可随意取消的系统更新阶段，页面不会展示虚构的安装百分比。
7. **完成验证**：手机 Recovery 显示安装成功后，**在手机上自行选择 Reboot system now**。进入 Android 并重新授权 USB 后，在应用中点击“重启后验证系统版本”，将当前 Android Build Fingerprint 与 OTA 目标精确比较。

## 遇到问题

| 症状 | 图形界面的提示与处理 |
| --- | --- |
| 找不到 ADB/Fastboot | 打开界面上的 Google 官方 Platform-Tools 下载入口，安装后重新检查 |
| USB `unauthorized` | 解锁手机并点击“允许 USB 调试” |
| USB `offline` | 重新插拔支持数据传输的线，重新授权 |
| 多台设备或模拟器 | 断开其他设备，仅保留更新目标 |
| Recovery / Fastboot | 首次检查应先进入正常 Android 系统，不能跳过识别 |
| 电量低于 50% 或未知 | 充电后重新扫描 |
| 最新固件有地区/运营商变体 | 不提供自动推荐；从 Google 官网确认渠道与机型后再用本地 OTA ZIP |
| Google 官网获取失败 | 不使用未验证固件；可以手动下载官方 ZIP，再点击页面上的 SHA-256 核验 |
| Google 哈希不一致 | 不接受该文件，不开启系统写入 |
| Sideload 结束但手机报错 | 以手机 Recovery 为准，不自动重试或重启 |

## 界限

- 目前 GUI 提供操作向导与受控官方 OTA 传输，但 **没有经过真实 Pixel 的完整验收**，因此只能作为实验性测试。对重要手机请优先使用 Google 官方工具。
- Factory Image 目前仅支持只读校验：没有 Bootloader 解锁/上锁、擦除、Fastboot 分区刷写、降级或自定义 ROM 的执行入口。
- 旧系统升级、运营商固件、A/B 双槽与防回滚规则很复杂，FlowAndroid 以“信息不足时拒绝”为原则，而不是自动猜测。
- 本项目未经 Google 认可或授权；固件使用受 Google 条款约束。

参考： [Google 官方 Full OTA](https://developers.google.com/android/ota) | [Google 官方 Factory Images](https://developers.google.com/android/images) | [安全规范](FLASH_SAFETY.md) | [人工验收清单](MANUAL_ACCEPTANCE.md)
