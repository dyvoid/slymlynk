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

**M2 — Security Hardening** (implemented on `claude/design-independent-steps-u40r05`,
awaiting human review — path-validation changes are security-critical per AGENTS.md — and a
green Windows CI run before merging to `main` and closing the milestone)

The functional core works; M2 hardens it against edge cases and misuse before adding visual polish.

**M2 checklist:**

- [x] Path traversal audit — confirm `..` and symlink-based traversal are fully blocked
- [x] Reserved filename validation extended to all path components, not just the leaf
      (including names with extensions: `NUL.txt`, `con.tar.gz`)
- [x] Destination path validation: reject if parent directory does not exist
- [x] Handle long paths (>260 chars) gracefully — rejected with a clear error; long-path
      support deferred to M4 (see SECURITY.md)
- [x] Symlink creation privilege check: detect Developer Mode vs. admin vs. denied upfront
      (`SymlinkService.DetectSymlinkPrivilege`; `Create` refuses file links when denied)
- [x] Input sanitisation: reject control characters, null bytes, and non-printable chars in paths
      (also: NTFS alternate data streams, `" < > | * ?`, components ending in dot/space)
- [x] Review all `catch { }` blocks in DragOutHelper for information leakage or DoS
      (reviewed and documented in SECURITY.md; shell window enumeration now capped)
- [x] Fuzz-test path validation with adversarial inputs (spaces, unicode, MAX_PATH, etc.)
      (curated battery + 300 seeded random inputs in `SymlinkServiceHardeningTests`)
- [x] Document security model in README or SECURITY.md
- [x] All new logic covered by tests (44 pass cross-platform; junction/UNC cases need Windows CI)

## M3 — UI Polish (design ready, deferred)

The wormhole metaphor design is captured in `docs/design/m3-wormhole-ui.md`. This milestone is
**blocked on M2 completion** and awaits final PNG animation assets.

---

## Last session

M2 security hardening implemented on branch `claude/design-independent-steps-u40r05`
(all design-independent work; M3 visual work still awaits assets):

1. **`SymlinkService` validation hardening** — control-character/null-byte rejection on raw
   input, reserved device names checked in every path component (extension-aware), NTFS
   alternate-data-stream (`:`) rejection, invalid filename characters (`" < > | * ?`),
   components ending in dot/space, destination parent-must-exist check.
2. **Upfront privilege detection** — `SymlinkPrivilege` enum +
   `SymlinkService.DetectSymlinkPrivilege()` (Developer Mode registry → elevation →
   denied); `Create` refuses file-symlink attempts with a remediation message when denied.
   The Win32 1314 handler stays as a backstop.
3. **DragOutHelper review** — swallowed COM exceptions documented as reviewed; shell window
   enumeration capped at 512.
4. **`SECURITY.md`** — full security model documented; linked from README.
5. **Tests** — `SymlinkServiceHardeningTests` (24 new tests incl. curated + seeded-random
   fuzz battery). 44 tests verified passing cross-platform on Linux; junction/UNC-specific
   cases need the Windows CI run.
6. **Docs** — README status table corrected (M1 was still marked in progress).

**Merge path:** human review required (path validation is security-critical per AGENTS.md),
then FF-only onto `main`. CI only runs on `main` pushes and PRs, so open a PR or run the
workflow against the branch to get the Windows test run.

**Current branch:** `claude/design-independent-steps-u40r05`

---

## Known blockers / open questions

- Wormhole PNG sequences (opening, loop, closing, unstable) needed for M3 visual polish.
  Placeholder XAML visuals can unblock implementation in the meantime.
- Confirm whether `dotnet publish --self-contained` produces a single exe that works without
  the .NET runtime on a clean machine (test on a VM before M5).
