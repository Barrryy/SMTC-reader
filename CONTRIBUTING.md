# Contributing

Thanks for taking a look. This is a small tool and it is meant to stay small, so the bar
for a change is "does it make the report more accurate, or easier to read".

## Getting set up

```powershell
dotnet build -c Release
dotnet test  -c Release
```

You need the .NET SDK 10.0 or newer and Windows 10 1809+. The first build restores the
Windows SDK projections from NuGet.

## Layout

| Path | What belongs there |
| --- | --- |
| `src/SmtcReader/Smtc/WinRtSessionSource.cs` | Windows interop. Keep it thin. |
| `src/SmtcReader/Smtc/Models.cs` | Snapshot records. These are the JSON contract. |
| `src/SmtcReader/Rendering/` | Console, Markdown and JSON output. Pure functions. |
| `src/SmtcReader/Cli/` | Argument parsing. |
| `src/SmtcReader/Text.cs` | Every user visible string, English and Chinese. |
| `tests/SmtcReader.Tests/` | Unit tests. No live media session required. |

## Ground rules

- **Keep the renderers pure.** They take snapshots and return strings. If a change needs
  a real media session to test, it probably belongs behind `ISessionSource` instead.
- **Prefer reflection over hard coded field lists** when reading a Windows type. That is
  what keeps `--raw` complete across Windows updates.
- **Bump `ControlCapabilities.All` when the API grows.** The test suite will tell you.
- **Add a string in both languages.** `Text.cs` holds English and Chinese side by side;
  a new field without a Chinese label will silently fall back to the property name.
- **Never hide missing data.** A field the app did not report is interesting information.
  Show it as absent rather than omitting the row.
- **Regenerate the documentation when output changes.** `docs/sample-report.md` and
  `docs/sample-console.txt` are both asserted against the renderers, so a formatting
  change will fail the test suite until they are updated. The README image is then a
  one-liner:

  ```powershell
  powershell -NoProfile -ExecutionPolicy Bypass -File tools\make-screenshot.ps1
  ```

## Sending a change

1. One topic per pull request.
2. `dotnet test -c Release` has to be green, and the build treats warnings as errors.
3. If you are fixing a report field, include the output of
   `smtc-reader.exe --raw --json` from the app you were looking at, before and after.

## Reporting a bug

Use the issue template. The two things that make a report actionable are the Windows
build number and the raw dump of the session that misbehaved.
