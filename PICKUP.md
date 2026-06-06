# PICKUP

Where the last session left off. Update this at the end of each working session.

---

## Current milestone

**M1 — Functional Core** (complete)

Exit criterion: A file and a folder can both be linked via both interaction paths on a clean
Windows 10 machine.

**M1 checklist** — all items done:

- [x] WPF project scaffolding (`SlymLynk.sln`, `SlymLynk/`, `SlymLynk.Tests/`)
- [x] MVVM structure in place (Models / ViewModels / Views)
- [x] `SymlinkService` — junction creation via reparse points
- [x] `SymlinkService` — symbolic link creation via `CreateSymbolicLink` P/Invoke
- [x] `SymlinkService` — path validation (existence, traversal, reserved names, length, UNC)
- [x] `MainViewModel` — Idle / SourceLoaded / Error states
- [x] Drop zone accepts files and folders (drag-in)
- [x] Click-to-browse source (file dialog)
- [x] Drag-out to create link at destination (Mouse.Capture, no DoDragDrop)
- [x] Click-to-save destination (save dialog)
- [x] Auto-selection of junction vs. symlink
- [x] Error handling: privilege failure, invalid path, destination conflict
- [x] Single-instance enforcement (named Mutex)
- [x] Unit tests: `SymlinkService` (12 tests)
- [x] Unit tests: `MainViewModel` state transitions + dialog seam tests + drag-out tests (26 total)
- [x] README updated with build/run instructions

**Architecture refactors completed post-M1:**

- [x] `IFileDialogService` seam — dialogs behind an interface, unit-testable (ADR N/A)
- [x] `IDropTargetResolver` seam — drag-out orchestration moved from code-behind into ViewModel
- [x] `ValidatedSource` value type — compile-time enforcement of validated-before-create (ADR-0006)

**CI:**

- [x] GitHub Actions workflow (`.github/workflows/ci.yml`) — builds and tests on push to `main` and PRs

---

## Next milestone

**M2 — Security Hardening** (not started)

The functional core works; M2 hardens it against edge cases and misuse before adding visual polish.

**M2 checklist:**

- [ ] Path traversal audit — confirm `..` and symlink-based traversal are fully blocked
- [ ] Reserved filename validation extended to all path components, not just the leaf
- [ ] Destination path validation: reject if parent directory does not exist
- [ ] Handle long paths (>260 chars) gracefully — either support or reject with clear error
- [ ] Symlink creation privilege check: detect Developer Mode vs. admin vs. denied upfront
- [ ] Input sanitisation: reject control characters, null bytes, and non-printable chars in paths
- [ ] Review all `catch { }` blocks in DragOutHelper for information leakage or DoS
- [ ] Fuzz-test path validation with adversarial inputs (spaces, unicode, MAX_PATH, etc.)
- [ ] Document security model in README or SECURITY.md
- [ ] All new logic covered by tests

## M3 — UI Polish (design ready, deferred)

The wormhole metaphor design is captured in `docs/design/m3-wormhole-ui.md`. This milestone is
**blocked on M2 completion** and awaits final PNG animation assets.

---

## Last session

Architecture-deepening refactors landed on `main` (FF-only, per policy):

1. **Dialog seam** — `IFileDialogService` + `Win32FileDialogService`, injected into `MainViewModel`.
   Previously untestable dialog commands now have unit tests.
2. **Drag-out seam** — `IDropTargetResolver` + `ExplorerDropTargetResolver`, `CompleteDragOut`
   command in ViewModel. Drag-out orchestration removed from `MainWindow.xaml.cs`.
3. **ValidatedSource** — `ValidateSource` returns `ValidatedSource(Path, IsDirectory)`;
   `Create(ValidatedSource, dest)` enforces validated-before-create. ADR-0006 recorded.

Also: GitHub Actions CI badge + shields added to README; `AGENTS.md` updated to surface
FF-only merge policy in always-on context.

M3 wormhole UI design doc written and linked from README — **deferred until after M2**.

**Current branch:** `main`

**Nothing left mid-flight.**

---

## Known blockers / open questions

- Wormhole PNG sequences (opening, loop, closing, unstable) needed for M3 visual polish.
  Placeholder XAML visuals can unblock implementation in the meantime.
- Confirm whether `dotnet publish --self-contained` produces a single exe that works without
  the .NET runtime on a clean machine (test on a VM before M5).
