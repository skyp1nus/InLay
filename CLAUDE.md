# CLAUDE.md — InLay

## What this is
InLay is a Windows background utility that shows the current keyboard input language near the text caret (macOS-style), with switchable indicator modes (caret badge, full-screen splash, corner HUD, border glow).
The full technical plan lives in `docs/InLay-tech-plan.md`. Read the section relevant to your task (architecture §3–4, milestones §9) before writing code.

## Stack
.NET 10 LTS · C# 14 · WPF (`net10.0-windows`) · WPF-UI (lepo.co) · Microsoft.Windows.CsWin32 · .NET Generic Host · CommunityToolkit.Mvvm · H.NotifyIcon.Wpf · Serilog · xUnit + FluentAssertions.

## Solution layout
- `src/InLay.Core` — engine: layout monitoring, caret tracking, models, settings. **No UI dependencies, ever.**
- `src/InLay.App` — WPF host: overlays, indicator modes, settings window, tray.
- `tests/InLay.Tests` — xUnit tests for Core.

## Commands
- Build: `dotnet build InLay.sln`
- Test: `dotnet test tests/InLay.Tests`
- Run: `dotnet run --project src/InLay.App`

## Hard rules
- `InLay.Core` must never reference WPF, WinForms, or any UI assembly.
- All P/Invoke goes through CsWin32 (`NativeMethods.txt`). Never hand-write `DllImport` signatures.
- No low-level keyboard hooks (`WH_KEYBOARD_LL`), no DLL injection, no telemetry, no network calls except update checks. Ever.
- Win32 coordinates are physical pixels everywhere in Core; convert to DIPs only at the WPF boundary. DPI awareness is `PerMonitorV2`.
- Overlay windows: `WS_EX_LAYERED | WS_EX_TRANSPARENT | WS_EX_NOACTIVATE | WS_EX_TOOLWINDOW`, `ShowActivated=false`, `ShowInTaskbar=false`, positioned via `SetWindowPos` in physical pixels.
- Ask before adding any new NuGet dependency.
- Third-party NuGet dependencies must be MIT/Apache-2.0/BSD only, to keep InLay.Core and the app freely usable by everyone, including commercially.

## Git & commits
- Branch per task: `feature/<area>-<short-name>`. Never commit directly to `main`.
- **Small, atomic commits**: one logical change per commit, roughly ≤150 changed lines as a guideline. If a step feels bigger — split it before committing.
- Conventional Commits, in English: `feat:`, `fix:`, `refactor:`, `test:`, `docs:`, `chore:`, `build:`. Subject ≤72 chars, imperative mood. Add a body only when the *why* is non-obvious.
- The solution must build (`dotnet build`) before every commit. Run tests before committing anything in Core.
- Never mix refactoring and behavior changes in the same commit.
- Do not add attribution trailers, co-author lines, or "generated with" footers to commit messages or PR descriptions.

## Workflow
- For any non-trivial task: plan first, present the plan, wait for approval before writing code.
- Work strictly within the milestone you were asked to do (`docs/InLay-tech-plan.md` §9). Do not expand scope.
- When multiple agents work in parallel: stay strictly inside your assigned project/folder. Shared contracts (interfaces, event/model types in Core) are changed only via a dedicated task merged to `main` first — never edited ad hoc from a feature branch.
- After finishing a task: summarize what was done commit by commit, list anything intentionally left out, and stop.
