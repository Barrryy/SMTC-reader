# SMTC Reader

**English** · [简体中文](README.zh-CN.md)

A small, dependency-free command line tool that dumps **everything** a Windows app
exposes through SMTC (System Media Transport Controls) — the same data that drives the
media popup and the play/pause key on your keyboard.

If you are building anything that reacts to "what is playing right now", this is the
tool that tells you what the app on the other side is actually reporting, field by
field, including the fields it silently leaves empty.

```
==============================================================================
 [1/1] PotPlayerMini64.exe  (PotPlayer)  <-- current session
==============================================================================
  Source app (AUMID)        : PotPlayerMini64.exe
  Resolved app name         : PotPlayer

-- Playback ------------------------------------------------------------------
  Playback status           : Playing
  Media type                : Music
  Repeat mode               : List
  Shuffle                   : True
  Playback rate             : 1

-- Timeline ------------------------------------------------------------------
  Start                     : 00:00:00.000
  End                       : 00:05:01.975
  Reported position         : 00:01:25.924
  Extrapolated position     : 00:01:26.247
  Position updated at       : 2026-10-06 19:01:54.779 +08:00

-- Media properties ----------------------------------------------------------
  Title                     : Nirvana - Smells Like Teen Spirit.mp3
  Artist                    : <empty>
  Album                     : <empty>
  Genres                    : <none>
  Thumbnail                 : image/bmp / 1.56 MB
```

## Why this exists

SMTC is a contract, not a guarantee: every app decides how much of it to fill in.
Spotify reports rich metadata, a desktop player may report nothing but a file name, and
some apps hand out BMP data while insisting it is a PNG. Debugging that from the outside
is guesswork unless you can see the raw values.

This tool shows all of it, and it is deliberately explicit about the difference between
*absent* (`<null>`), *present but empty* (`<empty>`) and *an actual value*.

## Requirements

- Windows 10 version 1809 (build 17763) or newer, or Windows 11.
- To **run**: nothing else. Releases are published as a self-contained single file.
- To **build**: the .NET SDK 10.0 or newer.

## Install

Download `smtc-reader.exe` from the
[releases page](http://localhost:8101/Share-with-Codex/SMTC-reader/releases) and run it.
There is no installer and no runtime to install.

## Quick start

```powershell
# everything, right now
smtc-reader.exe

# just the overview, keep refreshing
smtc-reader.exe --list --watch

# one player only, and pull the album art out
smtc-reader.exe --app potplayer --thumbnail cover.png

# machine readable
smtc-reader.exe --json > sessions.json

# Chinese output
smtc-reader.exe --lang zh
```

Every run also writes a Markdown report next to the executable, named by timestamp
(`smtc-20261006-190401.md`). See [docs/sample-report.md](docs/sample-report.md) for what
that looks like — it uses made-up app and track names, but the layout is exactly what
the tool prints, and a test keeps it that way.

## What gets dumped

| Area | Fields |
| --- | --- |
| Session | `SourceAppUserModelId`, best-effort resolved app name, whether Windows considers it the current session |
| Playback | status, media type, repeat mode, shuffle, playback rate |
| Controls | all 15 capability flags (`IsPlayEnabled`, `IsNextEnabled`, …) |
| Timeline | start, end, seek range, reported position, extrapolated position, last update time |
| Media properties | title, subtitle, artist, album artist, album, track number, album track count, genres, thumbnail (format + size) |
| Raw (`--raw`) | every reflected property of every object, including ones this tool does not know about |

Fields that an app does not implement are reported as missing rather than silently
hidden — see [docs/fields.md](docs/fields.md) for the per-field notes.

## Command line

| Option | Meaning |
| --- | --- |
| `-a`, `--app <pattern>` | Only sessions whose AUMID or resolved name matches. `*` wildcards supported; a pattern without one is a substring match. |
| `-l`, `--list` | Overview table only. |
| `-w`, `--watch` | Keep refreshing until Ctrl+C. |
| `-i`, `--interval <sec>` | Refresh interval for `--watch` (default `1.0`). |
| `-n`, `--count <n>` | Stop after `n` refreshes; `0` means until Ctrl+C. |
| `-t`, `--thumbnail <path>` | Write the album art. With several sessions the index and AUMID are appended, and the extension follows the real image format. |
| `--out-dir <dir>` | Where to write the Markdown report (default: next to the executable). |
| `--no-markdown` | Do not write a report. |
| `--raw` | Add a reflected dump of every property. |
| `-j`, `--json` | JSON on stdout instead of the text report. Status lines move to stderr. |
| `--lang <auto\|en\|zh>` | Output language (default `auto`, follows the system UI language). |
| `-h`, `--help` | Help. |
| `--version` | Version. |

### Exit codes

| Code | Meaning |
| --- | --- |
| `0` | Success — including "no sessions found", which is a normal state. |
| `1` | Runtime failure (SMTC broker unavailable, report not writable, …). |
| `2` | Bad command line. |

## Notes on the data

A few things worth knowing before you trust a field:

- **Position is a snapshot.** Reported position is what the app last pushed. The
  extrapolated position adds the time since `LastUpdatedTime` and clamps to the seekable
  range, which is what you want for a progress bar. When an app never updates its
  timeline (some report `1601-01-01`), the extrapolation is dropped instead of producing
  nonsense.
- **Missing is not the same as empty.** `Artist = <empty>` means the app sent a string
  with no content; `Artist = <null>` means it never set the field.
- **Thumbnails lie about their format.** The extension follows the file's magic bytes,
  not what the app claims.
- **One app can hold several sessions.** PotPlayer is a good example; the star marks the
  one Windows currently routes media keys to.

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

Durations are serialized in the standard `TimeSpan` round-trip format and timestamps as
ISO 8601. Thumbnail bytes are never inlined — use `--thumbnail` for that.

## How it is built

The interesting logic needs no media session to test:

- `Smtc/WinRtSessionSource.cs` is the only file that talks to WinRT. It is thin.
- Everything else — filtering, formatting, the console report, the Markdown report, the
  JSON contract — is a pure function over snapshots.
- Capabilities are read by **reflection** over the Windows type rather than a hard coded
  switch, and `ControlCapabilitiesTests` fails when a Windows update changes that
  surface. A new flag shows up in `--raw` immediately, without a code change.
- 57 unit tests cover rendering, filtering, formatting, the CLI and the API surface,
  including one that fails if `docs/sample-report.md` drifts from the real output.

```
src/SmtcReader
  Cli/            argument parsing
  Smtc/           models, the WinRT source, filtering, image sniffing
  Formatting/     value formatting shared by the renderers
  Rendering/      console, Markdown and JSON output
tests/SmtcReader.Tests
```

## Build and test

```powershell
git clone http://localhost:8101/Share-with-Codex/SMTC-reader.git
cd SMTC-reader
dotnet build -c Release
dotnet test  -c Release

# self-contained single file
dotnet publish src/SmtcReader -c Release -r win-x64 --self-contained true `
    -p:PublishSingleFile=true -o artifacts
```

The build needs the Windows SDK projections, which the .NET SDK restores from NuGet on
first use. CI runs the same commands on `windows-latest`.

The published executable is about 95 MB because it is self-contained: there is no .NET
runtime to install. Trimming is switched off deliberately — the `--raw` report reads
WinRT properties by reflection, and a trimmed build would quietly drop fields rather
than fail loudly.

## FAQ

**Nothing shows up.** No app is currently registered with SMTC. Play something, then run
it again. A player with nothing playing is a normal empty result.

**`Artist` / `Album` are empty but my player shows them.** The player is not forwarding
tags over SMTC. That is the app's choice and the report is telling you the truth.

**The thumbnail is a BMP.** Some players hand out BMP data. The tool names the file after
what is actually inside it.

**Why not PowerShell?** The original prototype was a PowerShell script and it works, but
it hits three walls: execution policy, PowerShell 7 having no WinRT projection, and no
compile-time checking of field names (a typo silently reads as "null"). This port fixes
all three and ships as an executable.

## Contributing

See [CONTRIBUTING.md](CONTRIBUTING.md). Bug reports are much easier to act on when they
include the output of `smtc-reader.exe --raw --json`, which is exactly what the issue
template asks for.

## License

[MIT](LICENSE).
