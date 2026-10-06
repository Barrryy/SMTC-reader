# SMTC Reader

[English](README.md) · **简体中文**

一个不依赖任何运行时的命令行小工具，把 Windows 应用通过 SMTC（System Media Transport
Controls，系统媒体传输控件）抛出来的信息**全部**打出来——也就是媒体弹窗和键盘上那个
播放/暂停键背后的一套数据。

如果你正在做任何"跟随当前播放内容"的东西，这个工具能告诉你对面那个应用到底报了什么、
又悄悄留空了哪些字段。

```
==============================================================================
 [1/1] PotPlayerMini64.exe  (PotPlayer)  <-- 当前会话
==============================================================================
  来源应用 (AUMID) : PotPlayerMini64.exe
  应用显示名       : PotPlayer

-- 播放 ----------------------------------------------------------------------
  播放状态 : Playing
  媒体类型 : Music
  循环模式 : List
  随机播放 : True
  倍速     : 1

-- 时间轴 --------------------------------------------------------------------
  终点           : 00:05:01.975
  应用上报进度   : 00:01:25.924
  按上报时间外推 : 00:01:26.247

-- 媒体信息 ------------------------------------------------------------------
  曲名 : Nirvana - Smells Like Teen Spirit.mp3
  歌手 : <empty>
  流派 : <none>
  封面 : image/bmp / 1.56 MB
```

## 为什么要有它

SMTC 是"约定"而不是"保证"：每个应用自己决定填多少。Spotify 会报出完整的曲目信息，
桌面播放器可能只给一个文件名，还有的应用会把 BMP 数据顶着 PNG 的名字丢出来。
从外面猜是没用的，得看原始值。

这个工具会把它们全列出来，并且**严格区分**三种状态：*字段不存在*（`<null>`）、
*字段存在但内容为空*（`<empty>`）、*有值*。

## 环境要求

- Windows 10 1809（build 17763）及以上，或 Windows 11。
- 只运行：不需要装任何东西，Release 是自包含的单文件 exe。
- 要自己编译：.NET SDK 10.0 或更新。

## 安装

从 [Releases 页面](http://localhost:8101/Share-with-Codex/SMTC-reader/releases) 下载
`smtc-reader.exe` 直接运行。没有安装程序，也不用装运行时。

## 快速上手

```powershell
# 全都打出来
smtc-reader.exe

# 只看概览，每秒刷新
smtc-reader.exe --list --watch

# 只看某个播放器，顺便导出封面
smtc-reader.exe --app potplayer --thumbnail cover.png

# 给程序读的 JSON
smtc-reader.exe --json > sessions.json

# 中文输出
smtc-reader.exe --lang zh
```

每次运行都会在 exe 旁边写一份按时间命名的 Markdown 报告（形如
`smtc-20261006-190401.md`），样例见 [docs/sample-report.md](docs/sample-report.md)
——里面应用名和曲名是编的，但排版和工具真实输出完全一致，有测试盯着不让它跑偏。

## 都会读出什么

| 分类 | 字段 |
| --- | --- |
| 会话 | `SourceAppUserModelId`、尽力解析出的应用显示名、是否为系统当前会话 |
| 播放 | 播放状态、媒体类型、循环模式、随机播放、倍速 |
| 能力位 | 全部 15 个 `IsXxxEnabled` 标志 |
| 时间轴 | 起点、终点、可拖动范围、应用上报进度、外推进度、进度更新时间 |
| 媒体信息 | 曲名、副标题、歌手、专辑歌手、专辑、音轨号、总音轨数、流派、封面（格式 + 大小） |
| 原始（`--raw`） | 反射列出每个对象的全部属性，包括本工具还不认识的 |

应用没实现的字段会被明确标成"没有"，而不是悄悄省略。逐字段说明见
[docs/fields.md](docs/fields.md)。

## 命令行参数

| 参数 | 说明 |
| --- | --- |
| `-a`、`--app <关键词>` | 只保留 AUMID 或显示名匹配的会话。支持 `*` 通配符；不含通配符时按子串匹配。 |
| `-l`、`--list` | 只打印概览表。 |
| `-w`、`--watch` | 持续刷新，Ctrl+C 退出。 |
| `-i`、`--interval <秒>` | `--watch` 的刷新间隔，默认 `1.0`。 |
| `-n`、`--count <次数>` | 刷够 n 次就停，`0` 表示一直刷。 |
| `-t`、`--thumbnail <路径>` | 导出封面。多会话时会附加序号和 AUMID，扩展名按真实格式取。 |
| `--out-dir <目录>` | Markdown 报告的输出目录，默认在 exe 旁边。 |
| `--no-markdown` | 不写报告。 |
| `--raw` | 额外反射列出所有属性。 |
| `-j`、`--json` | stdout 输出 JSON，状态信息改走 stderr。 |
| `--lang <auto\|en\|zh>` | 输出语言，默认 `auto`（跟随系统）。 |
| `-h`、`--help` | 帮助。 |
| `--version` | 版本。 |

### 退出码

| 码 | 含义 |
| --- | --- |
| `0` | 成功——包括"没有检测到会话"，那是正常状态。 |
| `1` | 运行失败（拿不到 SMTC 代理、报告写不进去等）。 |
| `2` | 命令行参数错误。 |

## 关于数据本身

几个用了才不至于被坑的点：

- **进度是快照。** "应用上报进度"是应用最后一次推上来的值；"按上报时间外推"会补上
  `LastUpdatedTime` 到现在这一段，并收敛到可拖动区间内——做进度条用这个。
  有的应用根本不给有效时间戳（报 1601 年），这种情况直接不外推，不会算出天文数字。
- **"没有"和"空"是两回事。** `歌手 = <empty>` 是应用送了个空字符串，
  `歌手 = <null>` 是它压根没设过这个字段。
- **封面会撒谎。** 扩展名按文件头判断，不按应用声称的来。
- **一个应用可能开着多个会话。** PotPlayer 就是这样，星号标的是系统当前路由媒体键的那个。

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

时长用 `TimeSpan` 的标准往返格式序列化，时间戳用 ISO 8601。封面字节不会塞进 JSON，
要导出封面请用 `--thumbnail`。

## 它是怎么做的

设计上的重点是：**有意思的逻辑全都不需要真放音乐就能测**。

- `Smtc/WinRtSessionSource.cs` 是唯一碰 WinRT 的文件，很薄。
- 其余部分——过滤、格式化、控制台报告、Markdown 报告、JSON 契约——全是关于快照的纯函数。
- 能力位是**反射** Windows 类型读出来的，不是硬编码 switch；
  `ControlCapabilitiesTests` 会在 Windows 更新改动这个 API 时变红。
  系统新增的字段会立刻出现在 `--raw` 里，不用改代码。
- 57 个单元测试覆盖渲染、过滤、格式化、命令行解析和 API 面，其中一个专门盯
  `docs/sample-report.md` 有没有和真实输出脱节。

```
src/SmtcReader
  Cli/            命令行解析
  Smtc/           模型、WinRT 读取、过滤、图片嗅探
  Formatting/     各渲染器共用的取值格式化
  Rendering/      控制台、Markdown、JSON 输出
tests/SmtcReader.Tests
```

## 编译与测试

```powershell
git clone http://localhost:8101/Share-with-Codex/SMTC-reader.git
cd SMTC-reader
dotnet build -c Release
dotnet test  -c Release

# 自包含单文件
dotnet publish src/SmtcReader -c Release -r win-x64 --self-contained true `
    -p:PublishSingleFile=true -o artifacts
```

编译需要 Windows SDK 投影包，.NET SDK 会在首次使用时从 NuGet 还原。CI 在
`windows-latest` 上跑同样的命令。

发布出来的 exe 大约 95 MB，因为它是自包含的：用户不用装 .NET 运行时。裁剪（trimming）
是**故意关掉**的——`--raw` 靠反射枚举 WinRT 属性，裁剪过的构建会悄悄少字段，
而不是报错。

## 常见问题

**什么都没有。** 当前没有应用注册 SMTC。放点东西再跑。播放器开着但没在播放，属于正常情况。

**`歌手`/`专辑` 是空的，但我播放器里明明有。** 播放器没把标签转发到 SMTC。这是应用的
选择，报告说的是实话。

**封面怎么是 BMP？** 有的播放器就是丢 BMP 出来。工具按实际内容给文件命名。

**为什么不用 PowerShell？** 最初的原型就是 PowerShell 脚本，能跑，但撞三堵墙：
执行策略、PowerShell 7 没有 WinRT 投影、字段名写错不会报错（会静默读成 null）。
这个版本把三个都解决了，并且能发成 exe。

## 参与

见 [CONTRIBUTING.md](CONTRIBUTING.md)。反馈问题时附上
`smtc-reader.exe --raw --json` 的输出会好处理很多——issue 模板也是这么要求的。

## 许可

[MIT](LICENSE)。
