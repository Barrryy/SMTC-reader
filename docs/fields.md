# Field reference

Where every value in the report comes from, and what to expect from it.

Legend: **App-dependent** means the field is only populated when the app chooses to;
a dash in the report means the app did not provide it.

## Session

| Report field | SMTC source | Notes |
| --- | --- | --- |
| Source app (AUMID) | `GlobalSystemMediaTransportControlsSession.SourceAppUserModelId` | Packaged apps use `PackageFamilyName!AppId`; desktop players usually just report `something.exe`. |
| Resolved app name | derived | Best effort. For an executable AUMID the version description of the running process is used; otherwise the field stays empty. |
| Current session | `SessionManager.GetCurrentSession()` | Windows hands out a fresh wrapper object per call, so the match is done on AUMID + media title. App-dependent. |

## Playback (`GetPlaybackInfo()`)

| Report field | API property | Notes |
| --- | --- | --- |
| Playback status | `PlaybackStatus` | `Closed`, `Opened`, `Changing`, `Stopped`, `Playing`, `Paused`. |
| Media type | `PlaybackType` | Nullable: `Music`, `Video`, `Image`, `Unknown`. |
| Repeat mode | `AutoRepeatMode` | Nullable: `None`, `Track`, `List`. |
| Shuffle | `IsShuffleActive` | Nullable. **Not** `ShuffleActive` — that property does not exist, and reading it from a dynamic language fails silently. |
| Playback rate | `PlaybackRate` | Nullable. `1` is normal speed. |

## Controls (`GetPlaybackInfo().Controls`)

All 15 flags, read by reflection:

`IsPlayEnabled`, `IsPauseEnabled`, `IsPlayPauseToggleEnabled`, `IsStopEnabled`,
`IsNextEnabled`, `IsPreviousEnabled`, `IsFastForwardEnabled`, `IsRewindEnabled`,
`IsShuffleEnabled`, `IsRepeatEnabled`, `IsPlaybackRateEnabled`,
`IsPlaybackPositionEnabled`, `IsChannelUpEnabled`, `IsChannelDownEnabled`,
`IsRecordEnabled`.

`ControlCapabilitiesTests.The_known_list_covers_the_whole_windows_surface` fails when a
Windows update changes this list, so the report cannot silently go stale.

Note that these are *capabilities*, not state: `IsPauseEnabled = true` while paused does
not mean "pause again".

## Timeline (`GetTimelineProperties()`)

| Report field | API property | Notes |
| --- | --- | --- |
| Start / End | `StartTime`, `EndTime` | The media's own span. Zero for live streams and for apps that do not report a timeline. |
| Seek range | `MinSeekTime`, `MaxSeekTime` | |
| Reported position | `Position` | A snapshot taken when the app last pushed an update, not a continuous clock. |
| Extrapolated position | derived | `Position + (now - LastUpdatedTime)`, clamped into the seek range. Dropped when `LastUpdatedTime` is before the year 2000 (several apps report `1601-01-01`, which would otherwise produce a value millions of hours long) or when the gap is longer than 24 hours. |
| Position updated at | `LastUpdatedTime` | |

## Media properties (`TryGetMediaPropertiesAsync()`)

| Report field | API property | Notes |
| --- | --- | --- |
| Title / Subtitle / Artist / Album artist / Album | same names | App-dependent. A desktop player that reads tags from the file name typically reports only `Title`. |
| Track | `TrackNumber`, `AlbumTrackCount` | App-dependent; `0 / 0` means not reported. |
| Genres | `Genres` | App-dependent. |
| Thumbnail | `Thumbnail`, `OpenReadAsync().Size` | The content type is sniffed from the file's magic bytes (PNG, JPEG, GIF, WebP, BMP) because the stream's own `ContentType` is not reliably populated and some apps hand out BMP data. |

## Raw dump (`--raw`)

Every public property of every object above is listed by reflection, including
properties this tool has no friendly label for. If Microsoft adds a field in a future
Windows build, it appears here with no code change.

Values are rendered as:

- `<null>` — the property was not set.
- `<empty>` — the property was set to an empty string.
- `<none>` — an empty collection.
- Otherwise, the value. Timestamps use the ISO 8601 round-trip format and durations the
  `TimeSpan` round-trip format, so they can be pasted straight into other tools.
