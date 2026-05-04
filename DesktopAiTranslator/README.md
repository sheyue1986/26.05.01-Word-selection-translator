# DesktopAiTranslator

Windows 桌面级 AI 划词翻译器。程序启动后常驻系统托盘，用户在任意软件中划选文字，松开鼠标后出现“译”按钮，点击后调用 AI 翻译并显示悬浮译文卡片。

## 功能

- WPF 桌面程序，支持 Windows 10 / Windows 11。
- 系统托盘常驻，支持启用/暂停、设置、退出。
- 全局鼠标低级 Hook 检测划词动作。
- 本地取词优先使用 UI Automation，失败后使用剪贴板复制方案。
- 支持 Mock、Qwen / 千问、DeepSeek。
- API Key 使用 Windows DPAPI 加密保存到当前用户环境。
- 悬浮译文卡片只显示译文，支持复制译文、重试、关闭。
- 目标语言可在翻译卡片中直接选择。

## 本地运行

```powershell
dotnet run --project ".\DesktopAiTranslator\DesktopAiTranslator.csproj"
```

## 发布为本地桌面程序

```powershell
dotnet publish ".\DesktopAiTranslator\DesktopAiTranslator.csproj" -c Release -o ".\publish\DesktopAiTranslator"
```

发布后运行：

```powershell
".\publish\DesktopAiTranslator\DesktopAiTranslator.exe"
```

桌面快捷方式可以指向发布目录中的 `DesktopAiTranslator.exe`。

## 配置位置

配置和日志不在仓库内，位于当前 Windows 用户目录：

- 配置：`%AppData%\DesktopAiTranslator\settings.json`
- 日志：`%AppData%\DesktopAiTranslator\logs\app.log`

请不要把 API Key 或 `%AppData%` 下的配置文件提交到 GitHub。

## GitHub 部署建议

推荐提交源码，不提交 `bin/`、`obj/`、`publish/` 等编译产物。换设备后：

1. 安装 .NET 6 Windows Desktop SDK。
2. 克隆仓库。
3. 执行 `dotnet publish`。
4. 第一次启动后在设置中重新填写 API Key。

API Key 使用本机 DPAPI 加密，不能跨 Windows 用户或跨设备直接复用，这是刻意的安全设计。
