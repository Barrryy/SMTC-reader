# SMTC Reader

English | [简体中文](README.zh-CN.md)

SMTC Reader is a command-line utility for Windows. It reports every value an
application publishes through SMTC (System Media Transport Controls), the interface
behind the media overlay and the keyboard media keys.

The tool is intended for diagnosing integrations that depend on the currently playing
item. It shows which fields an application sets, which it leaves empty, and which it
never populates at all.

![SMTC Reader output](docs/screenshot.png)

*Output for the example session documented in
[docs/sample-report.md](docs/sample-report.md). Application and track names in the
example are fictitious; the layout is unmodified. The image can be regenerated with
`tools\make-screenshot.ps1`.*

## Requirements

- Windows 10 version 1809 (build 17763) or later, or Windows 11.
- To run: no additional components. Release binaries are self-contained.
- To build: .NET SDK 10.0 or later.

## Installation

Download `smtc-reader.exe` from the
[releases page](http://localhost:8101/Share-with-Codex/SMTC-reader/releases). No
installer or runtime is required.

## Usage

```powershell
smtc-reader.exe
smtc-reader.exe --list --watch
smtc-reader.exe --app potplayer --thumbnail cover.png
smtc-reader.exe --json > sessions.json
smtc-reader.exe --lang zh
```

Each run also writes a Markdown report next to the executable, named after the capture
time (`smtc-20261006-190401.md`). An example is in
[docs/sample-report.md](docs/sample-report.md).

## Output

SMTC data is organised into four groups per session. All four are reported, together
with a per-field reference in [docs/fields.md](docs/fields.md).

| Group | Fields |
| --- | --- |
| Session | `SourceAppUserModelId`, a best-effort resolved application name, and whether Windows treats the session as the current one |
| Playback | status, media type, repeat mode, shuffle, playback rate |
| Controls | all 15 capability flags (`IsPlayEnabled`, `IsNextEnabled`, and so on) |
| Timeline | start, end, seek range, reported position, extrapolated position, last update time |
| Media properties | title, subtitle, artist, album artist, album, track number, album track count, genres, thumbnail format and size |
| Raw (`--raw`) | every reflected property of every object, including properties this tool does not know about |

Fields an application does not implement are reported as missing rather than omitted.

## Options

| Option | Description |
| --- | --- |
| `-a`, `--app <pattern>` | Restrict output to sessions whose AUMID or resolved name matches. `*` is treated as a wildcard; without one the pattern is a substring match. |
| `-l`, `--list` | Print the overview table only. |
| `-w`, `--watch` | Refresh until interrupted with Ctrl+C. |
| `-i`, `--interval <seconds>` | Refresh interval for `--watch`. Default `1.0`. |
| `-n`, `--count <n>` | Stop after `n` refreshes. `0` runs until Ctrl+C. |
| `-t`, `--thumbnail <path>` | Write the album art to this path. With multiple sessions the index and AUMID are appended; the extension follows the actual image format. |
| `--out-dir <dir>` | Directory for the Markdown report. Default: the executable's directory. |
| `--no-markdown` | Do not write a report. |
| `--raw` | Include a reflected dump of every property. |
| `-j`, `--json` | Write JSON to standard output instead of the text report. Status messages are written to standard error. |
| `--lang <auto\|en\|zh>` | Output language. Default `auto`, which follows the system UI language. |
| `-h`, `--help` | Show usage information. |
| `--version` | Show the version. |

### Exit codes

| Code | Meaning |
| --- | --- |
| `0` | Success. This includes the case where no session is present. |
| `1` | Runtime failure, for example an unavailable SMTC broker or an unwritable report path. |
| `2` | Invalid command line. |

## Field semantics

The following points are worth knowing when interpreting a report.

- `Reported position` is the value the application published at `LastUpdatedTime`;
  it is not a continuously advancing clock. `Extrapolated position` adds the time
  elapsed since `LastUpdatedTime` and clamps the result to the seekable range, which
  is the value to use for a progress indicator. If an application never updates
  `LastUpdatedTime` (some report `1601-01-01`), no extrapolation is performed.
- Missing and empty values are distinguished. `<empty>` means the application
  supplied an empty string; `<null>` means the property was never set.
- The thumbnail extension is derived from the file signature rather than from the
  content type reported by the application. Several players publish BMP data.
- An application may own more than one session. PotPlayer is a common example. The
  `★` marks the session that Windows routes media keys to.

## JSON output

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

Durations use the standard `TimeSpan` round-trip format and timestamps use ISO 8601.
Thumbnail bytes are never inlined; use `--thumbnail` to export them.

## Implementation notes

WinRT interop is confined to `src/SmtcReader/Smtc/WinRtSessionSource.cs`. Filtering,
formatting and the three renderers are pure functions over session snapshots, so the
test suite runs without a media session.

Capability flags are enumerated by reflection over the Windows type rather than a hard
coded switch. `ControlCapabilitiesTests` fails when that API is extended or changed, so
the reported field list cannot silently become stale, and a new flag appears in the
`--raw` output without a code change.

The project was initially a PowerShell script. It was ported to .NET because
PowerShell imposes an execution policy, PowerShell 7 has no WinRT projection, and a
dynamic language does not check property names at compile time, so a misspelled field
resolves to null without error.

## Building

```powershell
git clone http://localhost:8101/Share-with-Codex/SMTC-reader.git
cd SMTC-reader
dotnet build -c Release
dotnet test  -c Release

# self-contained single file
dotnet publish src/SmtcReader -c Release -r win-x64 --self-contained true `
    -p:PublishSingleFile=true -o artifacts
```

The first build restores the Windows SDK projections from NuGet. Continuous
integration runs the same commands on `windows-latest`.

The published executable is approximately 95 MB, because self-contained deployment
avoids a runtime prerequisite. Trimming is disabled deliberately: the raw report
enumerates WinRT properties by reflection, and a trimmed build would omit fields
without failing.

## Tests

58 unit tests cover rendering, filtering, formatting, the command line and the API
surface. Two of them assert that the documented examples
(`docs/sample-report.md`, `docs/sample-console.txt`) match the output of the real
renderers, so the documentation cannot drift. The build treats warnings as errors.

## Troubleshooting

**No sessions are listed.** No application is registered with SMTC at that moment.
This is the normal state when nothing is playing, and it is not an error.

**Artist and album fields are empty although the player displays them.** The
application does not forward tag data over SMTC. The report reflects what the
application publishes.

**The exported thumbnail is a BMP file.** Several players publish BMP data. The file
extension is chosen from the actual image format.

## Contributing

See [CONTRIBUTING.md](CONTRIBUTING.md). Bug reports are considerably easier to act on
when they include the output of `smtc-reader.exe --raw --json`, which is what the
issue template requests.

## License

[MIT](LICENSE).
