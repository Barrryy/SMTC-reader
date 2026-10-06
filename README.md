# SMTC Reader

English | [简体中文](README.zh-CN.md)

A Windows command-line tool that reports everything an application publishes through
SMTC (System Media Transport Controls), the interface behind the media overlay and the
media keys. It is meant for diagnosing integrations that depend on the current track:
which fields an application sets, which it leaves empty, and which it never populates.

![SMTC Reader output](docs/screenshot.png)

*Output for the example session in [docs/sample-report.md](docs/sample-report.md). The
names are fictitious; the layout is not.*

## Requirements

Windows 10 version 1809 (build 17763) or later, or Windows 11. Release binaries are
self-contained. Building requires the .NET SDK 10.0 or later.

## Usage

```powershell
smtc-reader.exe                             # all sessions, full report
smtc-reader.exe --list --watch              # overview, refreshing
smtc-reader.exe --app potplayer -t art.png  # one application, export album art
smtc-reader.exe --json > sessions.json      # machine readable
```

Each run also writes a Markdown report beside the executable, named by capture time
(`smtc-20261006-190401.md`). See [docs/sample-report.md](docs/sample-report.md).

## Options

| Option | Description |
| --- | --- |
| `-a`, `--app <pattern>` | Restrict to sessions whose AUMID or resolved name matches. `*` is a wildcard; without one, a substring match. |
| `-l`, `--list` | Overview table only. |
| `-w`, `--watch` | Refresh until Ctrl+C. |
| `-i`, `--interval <seconds>` | Refresh interval. Default `1.0`. |
| `-n`, `--count <n>` | Stop after `n` refreshes; `0` runs until Ctrl+C. |
| `-t`, `--thumbnail <path>` | Export album art. With several sessions the index and AUMID are appended. |
| `--out-dir <dir>` | Report directory. Default: the executable's directory. |
| `--no-markdown` | Do not write a report. |
| `--raw` | Include a reflected dump of every property. |
| `-j`, `--json` | JSON on stdout; status messages move to stderr. |
| `--lang <auto\|en\|zh>` | Output language. Default `auto`. |
| `-h`, `--help`, `--version` | Usage and version. |

Exit codes: `0` success (including when no session is present), `1` runtime failure,
`2` invalid command line.

## Notes

- `Reported position` is the value published at `LastUpdatedTime`, not a running clock.
  `Extrapolated position` adds the elapsed time and clamps to the seek range; no
  extrapolation is performed if an application never updates the timestamp.
- `<null>` means the property was never set; `<empty>` means it was set to an empty
  string.
- The thumbnail extension follows the file signature, not the content type an
  application claims.
- An application may own several sessions; `★` marks the one Windows routes media keys
  to.

The full field reference is in [docs/fields.md](docs/fields.md). JSON uses the
`TimeSpan` round-trip format and ISO 8601 timestamps; thumbnail bytes are excluded.

## Build and test

```powershell
dotnet build -c Release
dotnet test  -c Release
dotnet publish src/SmtcReader -c Release -r win-x64 --self-contained true `
    -p:PublishSingleFile=true -o artifacts
```

58 tests. The published binary is about 95 MB because it is self-contained; trimming is
disabled because the raw report enumerates WinRT properties by reflection. WinRT
interop is confined to `src/SmtcReader/Smtc/WinRtSessionSource.cs`; the renderers are
pure functions over snapshots.

## License

[MIT](LICENSE). See [CONTRIBUTING.md](CONTRIBUTING.md) before sending a change.
