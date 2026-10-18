# Navigation and settings / 导航与设置 / ナビゲーションと設定

FlowAndroid follows native WinUI 3 NavigationView conventions:

- **Main navigation / 主导航：** Devices, Update wizard, Diagnostic logs.
- **Bottom fixed navigation / 底部固定：** About uses `NavigationView.FooterMenuItems`; Settings uses the built-in `IsSettingsVisible=true` gear, with `args.IsSettingsSelected` selecting its page.
- **Language / 语言 / 言語：** Settings → Display language. Chinese, Japanese and English change immediately. The language dropdown is not inside the hamburger pane or `NavigationView.PaneFooter`.
- **Appearance / 外观 / 外観：** Settings → System / Light / Dark; the selection applies immediately and is restored at next launch.
- **Persistence / 持久化：** `%LOCALAPPDATA%\FlowAndroid\preferences.json` stores only language and theme; malformed values fall back to zh-CN and system theme. No device identifiers are recorded in preferences.
- **Responsive:** Native `NavigationView` retains its normal expanded/compact visual states. Footer items and Settings remain at the bottom and do not scroll with regular menu entries.

Regression checks: `pwsh ./scripts/Test-NavigationLayout.ps1`, plus `UserPreferencesTests` for save/reopen and malformed JSON fallback.

日本語: サイドバー下部に「情報」と歯車の「設定」を固定しました。言語とテーマの選択は設定ページだけに配置し、選択を次回起動でも保持します。
