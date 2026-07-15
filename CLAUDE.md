# CLAUDE.md — InLay

## What this is
InLay is a Windows background utility that shows the current keyboard input language near the text caret (macOS-style). Indicators are **transient-only** — shown briefly on a real layout switch: the full-screen splash today, the caret-side badge next. Persistent modes (corner HUD, border glow, tray-only) were dropped as more distracting than useful.
The full technical plan lives in `docs/InLay-tech-plan.md`. Read the section relevant to your task (architecture §3–4, milestones §9) before writing code.

## Licensing & funding
- **MIT across the whole repo, free forever** — Core, App, tests, future Diag. No paid tiers, activation keys, source-available parts, or "pro" features. Decision is final (tech plan §10).
- Development is funded by **voluntary donations** — Buy Me a Coffee (primary) + GitHub Sponsors, wired via `.github/FUNDING.yml`; surfaced in the README **Support** section and the "About" window.
- **No nag screens, pop-ups, or reminders** — the donation model must never degrade the product. Do not add monetization prompts, upsells, or telemetry.

## Stack
.NET 10 LTS · C# 14 · WPF (`net10.0-windows`) · WPF-UI (lepo.co) · Microsoft.Windows.CsWin32 · .NET Generic Host · CommunityToolkit.Mvvm · H.NotifyIcon.Wpf · Serilog · xUnit + AwesomeAssertions.

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
- Third-party NuGet dependencies must be **MIT/Apache-2.0/BSD only**, to keep InLay.Core and the app freely usable by everyone, including commercially. "Free for non-commercial" or otherwise restricted licenses are a blocker regardless of convenience.
- Re-verify a dependency's license **on every major-version bump**, not just when first adding it — licenses can change between versions (FluentAssertions went commercial in v8, which is why tests use AwesomeAssertions). `Directory.Packages.props` centralizes versions, so that's the place to check.

## Code style
- **PascalCase for all member names — including test methods.** No `snake_case`, no underscores in identifiers (`ShowFullScreen`, not `Show_full_screen`). Test names are full PascalCase sentences: `ClassifyReasonIsARefreshWhenTheWindowChanged`, not `ClassifyReason_is_a_refresh_when_...`.
- **No `var`.** Use an explicit type, or a target-typed `new()` when the type is on the left (`MenuItem pause = new();`, `HWND hwnd = new(handle);`). Only fall back to `var` if no explicit form is possible (e.g. anonymous types) — which should be essentially never here.
- **Meaningful, correctly-spelled names** for locals, fields, constants, and methods. No abbreviations that aren't already idiomatic (HKL, DPI, HWND are fine); no typos.
- camelCase for locals/parameters, `_camelCase` for private fields, PascalCase for constants and everything public/internal.

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
