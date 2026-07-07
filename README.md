# InLay

**A macOS-style input-language indicator for Windows.** InLay is a lightweight
background utility that shows your current keyboard layout *where you're looking* —
near the text caret — the moment you switch languages, instead of hiding it in the
system tray.

[![License: MIT](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)
[![.NET 10](https://img.shields.io/badge/.NET-10-512BD4?logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/)
[![Platform: Windows](https://img.shields.io/badge/platform-Windows%2010%20%2F%2011-0078D6?logo=windows&logoColor=white)](#)
[![Status: v0.1 preview](https://img.shields.io/badge/status-v0.1%20preview-orange.svg)](#status)

<!-- Placeholder: record a short screen capture of a layout switch and save it as docs/demo.gif -->
<p align="center">
  <img src="docs/demo.gif" alt="InLay showing a layout switch near the caret" width="640">
</p>

## Why

On Windows there's no comfortable way to see your current input language at the
point of focus. The tray indicator sits far from your eyes, and existing third-party
tools are often dated, bloated, or closed-source. macOS has this built in — InLay
brings it to Windows and aims to do it better.

A key differentiator is the **indicator-mode system**: you choose *how* to see the
language — a compact badge by the caret, a full-screen splash, a corner HUD, a screen-edge
glow — and modes can be combined. (Windows can't render emoji flags in these overlays,
so InLay identifies languages by color and text, not flags.)

## Features

- **Full-screen splash** — a large, translucent pill centered on the active monitor,
  shown on a real layout switch. Useful even before caret tracking exists.
- **Corner HUD** — a small persistent pill in a screen corner that always reflects the
  current layout.
- **Splash on real switches only** — the splash fires when you actually switch language,
  not when you alt-tab between windows that happen to have different layouts.
- **Event-driven & quiet** — near-zero idle CPU; no polling of the keyboard.
- **No keyboard hooks, no DLL injection, no telemetry, no network calls** (except update
  checks). Your input is never read.
- **Per-monitor DPI-aware** (`PerMonitorV2`); click-through overlays that never steal focus.
- **System tray** control with an indicator-mode submenu, pause, and settings.

### Indicator modes

| Mode | Status |
|---|---|
| Full-screen splash | ✅ Available |
| Corner HUD | ✅ Available |
| Caret badge (by the text caret) | 🔜 Planned |
| Cursor badge (fallback near the mouse) | 🔜 Planned |
| Border glow (screen-edge color) | 🔜 Planned |
| Tray-only (glyph/color) | 🔜 Planned |

## Status

**v0.1 — preview (milestone M1).** The layout monitor plus the Full-screen splash and
Corner HUD modes work today; caret tracking, the full settings window, per-language
colors, and additional modes are on the [roadmap](#roadmap). Expect rough edges — feedback
is very welcome.

## Building from source

**Prerequisites:** the [.NET 10 SDK](https://dotnet.microsoft.com/) and Windows 10 (1809+)
or Windows 11.

```powershell
# Clone
git clone https://github.com/skyp1nus/InLay.git
cd InLay

# Build
dotnet build InLay.sln

# Run
dotnet run --project src/InLay.App

# Test (engine unit tests)
dotnet test tests/InLay.Tests
```

InLay is split into a UI-free engine (`src/InLay.Core`) and the WPF host
(`src/InLay.App`), so the core logic is testable without any UI.

## Roadmap

The full technical plan and milestone roadmap live in
[`docs/InLay-tech-plan.md`](docs/InLay-tech-plan.md) (§9 — *Дорожня карта*). The document
is currently in Ukrainian; an English translation is planned for the public release.

## Support the project

InLay is free and open source, and it stays that way. If it saves you a glance at the
tray a hundred times a day, you can support development:

- ❤️ **GitHub Sponsors** — https://github.com/sponsors/skyp1nus *(placeholder)*
- ☕ **Ko-fi** — https://ko-fi.com/your-ko-fi *(placeholder)*

Starring the repo and sharing it helps just as much.

## License

InLay is licensed under the [MIT License](LICENSE) — the entire repository, forever.
"InLay" and its logo are trademarks of the project author; the permissive license covers
the code, not the brand.
