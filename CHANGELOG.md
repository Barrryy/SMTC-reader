# Changelog

All notable changes to this project are documented in this file.

The format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

## [0.1.0] - 2026-10-06

First release. Ported from the original `smtc-reader.ps1` PowerShell prototype.

### Added

- Enumerate every active SMTC (System Media Transport Controls) session and dump
  the full set of fields the app exposes: source app, playback state, media type,
  repeat mode, shuffle, playback rate, control capabilities, timeline, track
  metadata and thumbnail.
- `Controls` capability set (15 flags) with human readable labels.
- Computed live playback position (extrapolated from `LastUpdatedTime`, with
  sanity checks for apps that never update their timeline).
- Reflection based `--raw` dump, so fields added by future Windows builds show up
  without code changes.
- Markdown report written next to the executable, named by timestamp.
- `--json` output for scripting.
- `--watch` mode with configurable interval and iteration cap.
- `--thumbnail` export with image type sniffing (some apps hand out BMP data
  under a misleading extension).
- Bilingual console/report output (`--lang auto|en|zh`).
- Unit tests covering rendering, filtering, formatting and API-surface drift.

[Unreleased]: http://localhost:8101/Share-with-Codex/SMTC-reader/compare/v0.1.0...HEAD
[0.1.0]: http://localhost:8101/Share-with-Codex/SMTC-reader/releases/tag/v0.1.0
