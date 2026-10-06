# SMTC Reader

[English](README.md) | 简体中文

SMTC Reader 是一个 Windows 命令行工具，用于输出应用程序通过 SMTC（System Media
Transport Controls，系统媒体传输控件）发布的全部数据。SMTC 是系统媒体面板与键盘媒体键
所依赖的接口。

本工具用于诊断依赖"当前播放内容"的集成场景：可以查看某个应用设置了哪些字段、哪些字段
为空、哪些字段从未上报。

![SMTC Reader 输出](docs/screenshot.png)

*上图为 [docs/sample-report.md](docs/sample-report.md) 中示例会话的实际输出。示例中的
应用名与曲名为虚构，排版未作修改。图片可用 `tools\make-screenshot.ps1` 重新生成。*

## 环境要求

- Windows 10 1809（build 17763）或更高版本，或 Windows 11。
- 仅运行：无附加依赖。Release 中的可执行文件为自包含版本。
- 自行构建：.NET SDK 10.0 或更高版本。

## 安装

从 [Releases 页面](http://localhost:8101/Share-with-Codex/SMTC-reader/releases)
下载 `smtc-reader.exe` 后直接运行，无需安装程序，也无需安装运行时。

## 用法

```powershell
smtc-reader.exe
smtc-reader.exe --list --watch
smtc-reader.exe --app potplayer --thumbnail cover.png
smtc-reader.exe --json > sessions.json
smtc-reader.exe --lang zh
```

每次运行还会在可执行文件所在目录写入一份 Markdown 报告，文件名以采集时间命名，例如
`smtc-20261006-190401.md`。示例见 [docs/sample-report.md](docs/sample-report.md)。

## 输出内容

SMTC 每个会话的数据分为四组，本工具全部输出，逐字段说明见
[docs/fields.md](docs/fields.md)。

| 分类 | 字段 |
| --- | --- |
| 会话 | `SourceAppUserModelId`、尽力解析出的应用显示名、是否为 Windows 认定的当前会话 |
| 播放 | 播放状态、媒体类型、循环模式、随机播放、倍速 |
| 能力位 | 全部 15 个标志（`IsPlayEnabled`、`IsNextEnabled` 等） |
| 时间轴 | 起点、终点、可拖动范围、应用上报进度、外推进度、进度更新时间 |
| 媒体信息 | 曲名、副标题、歌手、专辑歌手、专辑、音轨号、总音轨数、流派、封面格式与大小 |
| 原始数据（`--raw`） | 反射列出每个对象的全部属性，包括本工具尚不认识的属性 |

应用未实现的字段会被标记为缺失，而非省略。

## 命令行参数

| 参数 | 说明 |
| --- | --- |
| `-a`、`--app <关键词>` | 仅输出 AUMID 或应用显示名匹配的会话。`*` 视为通配符；不含通配符时按子串匹配。 |
| `-l`、`--list` | 仅打印概览表。 |
| `-w`、`--watch` | 持续刷新，Ctrl+C 中断。 |
| `-i`、`--interval <秒>` | `--watch` 的刷新间隔，默认 `1.0`。 |
| `-n`、`--count <次数>` | 刷新指定次数后停止，`0` 表示运行至 Ctrl+C。 |
| `-t`、`--thumbnail <路径>` | 导出封面到指定路径。多会话时会在文件名中附加序号与 AUMID，扩展名按实际图片格式确定。 |
| `--out-dir <目录>` | Markdown 报告的输出目录，默认为可执行文件所在目录。 |
| `--no-markdown` | 不写入报告。 |
| `--raw` | 附加每个属性的反射转储。 |
| `-j`、`--json` | 标准输出改为 JSON，状态信息写入标准错误。 |
| `--lang <auto\|en\|zh>` | 输出语言，默认 `auto`，跟随系统界面语言。 |
| `-h`、`--help` | 显示用法。 |
| `--version` | 显示版本。 |

### 退出码

| 退出码 | 含义 |
| --- | --- |
| `0` | 成功。未检测到会话同样属于成功。 |
| `1` | 运行失败，例如 SMTC 代理不可用或报告路径不可写。 |
| `2` | 命令行参数错误。 |

## 字段含义

解读报告时需要注意以下几点。

- `应用上报进度` 是应用在 `LastUpdatedTime` 时刻发布的位置，并非持续递增的时钟。
  `按上报时间外推` 会补上从该时刻到当前经过的时间，并收敛到可拖动范围内，制作进度条时
  应使用后者。若应用从不更新 `LastUpdatedTime`（部分应用报 `1601-01-01`），则不做外推。
- 缺失与空值分别表示。`<empty>` 表示应用提供了空字符串；`<null>` 表示该属性从未被设置。
- 封面扩展名依据文件头判断，而非应用声称的内容类型。部分播放器输出的是 BMP 数据。
- 一个应用可能拥有多个会话，PotPlayer 即为常见例子。`★` 标记 Windows 将媒体键路由到的
  那个会话。

## JSON 输出

```json
{
  "capturedAt": "2026-10-06T19:04:01.2796219+08:00",
  "count": 1,
  "sessions": [
    {
      "sourceAppUserModelId": "PotPlayerMini64.exe",
      "playback": { "status": "Playing", "isShuffleActive": true, "controls": [] },
      "timeline": { "position": "00:03:09.9250000", "livePosition": "00:03:10.3010000" },
      "media": { "title": "...", "thumbnailContentType": "image/bmp" }
    }
  ]
}
```

时长使用 `TimeSpan` 标准往返格式，时间戳使用 ISO 8601。封面字节不会内联输出，如需导出
请使用 `--thumbnail`。

## 实现说明

WinRT 互操作仅存在于 `src/SmtcReader/Smtc/WinRtSessionSource.cs` 一个文件中。过滤、
格式化与三个渲染器都是基于会话快照的纯函数，因此测试套件无需真实播放会话即可运行。

能力位通过反射枚举 Windows 类型，而非硬编码的 switch。当该 API 发生增删时，
`ControlCapabilitiesTests` 会失败，因此字段列表不会悄然过期；API 新增的标志会直接出现
在 `--raw` 输出中，无需改动代码。

项目最初是一个 PowerShell 脚本，后移植到 .NET，原因是 PowerShell 受执行策略限制、
PowerShell 7 没有 WinRT 投影，且动态语言不在编译期校验属性名，字段名拼写错误只会静默
得到 null。

## 构建

```powershell
git clone http://localhost:8101/Share-with-Codex/SMTC-reader.git
cd SMTC-reader
dotnet build -c Release
dotnet test  -c Release

# 自包含单文件
dotnet publish src/SmtcReader -c Release -r win-x64 --self-contained true `
    -p:PublishSingleFile=true -o artifacts
```

首次构建会从 NuGet 还原 Windows SDK 投影包。持续集成在 `windows-latest` 上执行相同的
命令。

发布产出的可执行文件约 95 MB，这是自包含部署避免运行时前置依赖的结果。裁剪（trimming）
被有意关闭：原始数据报告通过反射枚举 WinRT 属性，裁剪后的构建会遗漏字段而不会报错。

## 测试

58 个单元测试覆盖渲染、过滤、格式化、命令行解析与 API 面。其中两个断言文档示例
（`docs/sample-report.md`、`docs/sample-console.txt`）与真实渲染器的输出一致，因此文档
不会与实现脱节。构建将警告视为错误。

## 故障排查

**列表为空。** 当前没有任何应用在 SMTC 注册会话。没有播放内容时属于正常状态，不是错误。

**播放器界面显示歌手与专辑，但报告中为空。** 该应用未通过 SMTC 转发标签信息。报告反映的
是应用实际发布的内容。

**导出的封面是 BMP 文件。** 部分播放器发布的就是 BMP 数据。文件扩展名依据实际图片格式
确定。

## 参与贡献

见 [CONTRIBUTING.md](CONTRIBUTING.md)。反馈问题时附上
`smtc-reader.exe --raw --json` 的输出，会显著降低排查难度，issue 模板也是这样要求的。

## 许可

[MIT](LICENSE)。
