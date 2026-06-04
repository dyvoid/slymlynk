# PICKUP

Where the last session left off. Update this at the end of each working session.

---

## Current milestone

**M1 — Functional Core** (in progress)

Exit criterion: A file and a folder can both be linked via both interaction paths on a clean
Windows 10 machine.

## M1 checklist

- [ ] WPF project scaffolding (`SlymLynk.sln`, `SlymLynk/`, `SlymLynk.Tests/`)
- [ ] MVVM structure in place (Models / ViewModels / Views)
- [ ] `SymlinkService` — junction creation
- [ ] `SymlinkService` — symbolic link creation
- [ ] `SymlinkService` — path validation (basic)
- [ ] `MainViewModel` — Idle / SourceLoaded / Error states
- [ ] Drop zone accepts files and folders (drag-in)
- [ ] Click-to-browse source (file dialog)
- [ ] Drag-out to create link at destination
- [ ] Click-to-save destination (save dialog)
- [ ] Auto-selection of junction vs. symlink
- [ ] Error handling: privilege failure, invalid path
- [ ] Single-instance enforcement (named Mutex)
- [ ] Unit tests: `SymlinkService`
- [ ] Unit tests: `MainViewModel` state transitions
- [ ] README updated with build/run instructions

## Last session

_Update this section at the end of each session: what was done, what was left mid-flight, any
context needed to pick up cleanly._

Project scaffolding files created (AGENTS.md, README, docs/, ADRs, git config). No application
code written yet. Next step: create the Visual Studio solution and project structure for M1.

## Known blockers / open questions

- Decide on the P/Invoke wrapper approach for `CreateSymbolicLink` and reparse point operations
  (roll our own in `NativeMethods.cs`, or use a small wrapper lib?).
- Confirm whether `dotnet publish --self-contained` produces a single exe that works without
  the .NET runtime on a clean machine (test on a VM before M5).
