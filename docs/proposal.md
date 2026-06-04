# SlymLynk — Application Proposal

## 1. Overview

SlymLynk is a lightweight, portable Windows desktop utility for creating symbolic links and junctions via drag-and-drop. The entire interaction fits in a ~300×300 window. No installation required.

**Goals**
- Dead-simple symlink creation: drop a file or folder in, drag the link out
- Portable single `.exe` — no installation required, runs from any location
- Polished UI despite minimal footprint
- Secure by design — no destructive operations, no shell execution

**Non-goals**
- Symlink management, browsing, or deletion
- Network path support (out of scope for initial releases)
- Multi-window or multi-instance operation

---

## 2. UX Flow

The app has two interaction paths — drag-based and click-based — that cover the same two steps: selecting a source, then selecting a destination.

### Step 1 — Select source

**Drag path:** User drags a file or folder from Explorer and drops it onto the SlymLynk window. The drop zone accepts the item and transitions to the "ready to export" state.

**Click path:** User clicks anywhere on the window to open a standard Windows file browser. They select a file or folder. The app transitions to the "ready to export" state.

### Step 2 — Select destination

**Drag path:** User drags from the SlymLynk window to any Explorer location (folder, desktop, etc.). The symlink or junction is created at the drop destination.

**Click path:** User clicks anywhere on the window to open a Save dialog. They choose a destination folder. The symlink or junction is created there.

### States

| State | Description |
|---|---|
| Idle | Waiting for source input. Entire window is drop zone and click target. |
| Source selected | Source path loaded. Entire window becomes drag source and click target for save dialog. Source stays loaded — user can drag out to multiple destinations. Drop a new file/folder onto the window to replace the source. A small clear button (corner) resets to idle. |
| Error | Creation failed. Clear message explaining why (e.g., missing Dev Mode for file symlinks). |

### Constraints

- Single instance only — launching a second instance focuses the existing window
- Stateless — no history, no persistence between sessions
- Windows 10+ only

---

## 3. Technical Architecture

### Stack

| Component | Choice | Rationale |
|---|---|---|
| Framework | WPF + .NET 8 | Native Windows, strong drag-drop support, high UI ceiling, single-file publish |
| Language | C# | Familiar from Unity background; strong .NET ecosystem |
| MVVM library | CommunityToolkit.MVVM | Microsoft-maintained, removes boilerplate, keeps ViewModels testable |
| Test framework | xUnit | Standard .NET unit testing |

### MVVM Structure

```
Models/
  SymlinkService.cs       — symlink/junction creation logic, path validation
ViewModels/
  MainViewModel.cs        — UI state, commands, orchestration
Views/
  MainWindow.xaml         — drop zone, visual states, bindings
```

### Symlink Strategy

The app auto-selects link type based on source:

| Source type | Link type created | Elevation required |
|---|---|---|
| Folder | Junction | No |
| File | Symbolic link | Yes (Dev Mode or UAC) |

Junctions are used for folders because they require no elevation and work transparently across local volumes. Symbolic links are used for files because junctions do not support files. If a symlink operation fails due to insufficient privileges, the app shows a clear error with instructions to enable Windows Developer Mode.

### Key Implementation Rules

- All filesystem operations use .NET APIs directly — no `Process.Start`, no shell execution, no `cmd.exe`
- No write operations other than creating the symlink/junction pointer itself
- All paths validated before any operation (existence check, traversal check, length check)
- Fail loudly on unexpected conditions — no silent failures

### Project Structure

```
SlymLynk/
├── SlymLynk.sln
├── SlymLynk/
│   ├── Models/
│   ├── ViewModels/
│   ├── Views/
│   ├── Services/
│   ├── Assets/
│   └── App.xaml
├── SlymLynk.Tests/
└── .github/
    └── workflows/
├── AGENTS.md
├── README.md
└── docs/
    ├── design/
    ├── adr/
    └── testing/
```

---

## 4. Security

Given that symlinks and junctions interact directly with the filesystem, security is treated as a first-class concern — not an afterthought.

### Principles

- **No destructive operations.** The app creates one thing: a pointer. It never deletes, moves, or overwrites files or folders.
- **No shell execution.** All operations go through .NET APIs (`System.IO`, P/Invoke where needed). No `cmd.exe`, no PowerShell, no `Process.Start` for filesystem ops.
- **Validate everything.** Source and destination paths are validated before any operation: existence, path traversal, reserved names, max path length.
- **Least privilege.** The app requests no permissions beyond what the current operation requires.
- **Loud failures.** Any unexpected condition throws and surfaces a user-facing error. Nothing fails silently.

### Specific Mitigations

| Risk | Mitigation |
|---|---|
| Path traversal via crafted drag payload | Resolve and validate full path before use |
| Overwriting existing file/folder at destination | Check destination before creation; error if occupied |
| Dangling symlink (source deleted after creation) | Out of scope — standard OS behavior, not a SlymLynk concern |
| Privilege escalation | No admin operations except optional symlink creation; user is informed and consents via UAC |
| AI-generated code introducing unsafe patterns | Security-focused test suite; code review checklist in AGENTS.md |

---

## 5. UI Design

The app targets a polished, minimal aesthetic. The interaction surface is small — the design work is in making a tiny window feel considered rather than thrown together.

**Broad approach**
- Image-based UI — the drop zone and export zone are defined by assets, not just borders and colors
- Two primary visual states driven by image swaps: idle and source-loaded
- Hover states on interactive elements
- Drag-over indicator when a valid item is dragged above the drop zone
- Simple animations for state transitions (WPF Storyboards — no third-party animation library needed)

**Design specifics are deferred** to a dedicated designer pass in M3. This section will be updated with exact assets, color palette, and interaction specs at that stage.

---

## 6. Testing Strategy

Tests are written alongside each feature — no milestone closes without passing tests covering that milestone's scope.

### Layers

**Unit tests (SlymLynk.Tests)**
- `SymlinkService`: junction creation, symlink creation, path validation logic, error conditions
- `MainViewModel`: state transitions, command availability, error propagation

**Integration tests**
- End-to-end creation of junctions and symlinks against a temp directory
- Privilege failure handling (symlink on non-Dev Mode machine)
- Edge cases: long paths, reserved names, destination already exists

**UI tests**
- Kept minimal — interaction logic is in the ViewModel and tested there
- Smoke test: app launches, window appears at correct size, single-instance enforcement

### CI

All tests run automatically on every push and pull request via GitHub Actions. A release artifact cannot be produced if any test fails.

### AGENTS.md requirement

The `AGENTS.md` file specifies that all new code must include corresponding tests. PRs without test coverage for new logic are not considered complete.

---

## 7. Documentation

### README

The root `README.md` covers everything needed to use or build the app:
- What SlymLynk does
- Download and run (portable exe)
- Windows Developer Mode note (required for file symlinks)
- How to build locally
- How to run tests

The README is updated as an exit criterion of each milestone — it stays current as features land.

### Inline Documentation

All public classes and methods use XML doc comments (`/// <summary>`). This is the .NET standard and enables future tooling-based doc generation if needed.

### Architecture Decision Records

Non-obvious decisions are recorded as ADRs in `docs/adr/`. Initial ADRs to be created at project start:

- `ADR-001` — WPF over Tauri/Electron
- `ADR-002` — Junctions for folders, symlinks for files
- `ADR-003` — No shell execution policy
- `ADR-004` — CommunityToolkit.MVVM adoption

### AGENTS.md

The root `AGENTS.md` defines how AI agents should work in this repo:
- Coding conventions and naming standards
- What operations are explicitly forbidden (shell exec, destructive filesystem ops)
- How to write and where to place tests
- How to create ADRs
- How to update the README on feature completion

---

## 8. Milestone Plan

### M1 — Functional Core

Fully working app, no polish. Proves the interaction model and core logic.

M1 is built with M3 in mind. The MVVM structure, visual states, and window interaction model are designed so that assets and animations drop into M3 without structural refactoring. Zero tech debt between milestones.

- WPF project scaffolding, MVVM structure in place
- Drop zone accepts files and folders
- Click-to-browse (source) via file dialog
- Drag-out to create link at destination
- Click-to-browse (destination) via save dialog
- Auto-selection of junction vs symlink
- Basic error handling (privilege failure, invalid path)
- Single-instance enforcement
- Unit tests for `SymlinkService` and `MainViewModel`
- README covers build and run instructions

**Exit criterion:** A file and a folder can both be linked via both interaction paths on a clean Windows 10 machine.

---

### M2 — Security & Safety

Hardens the app before any further investment in polish.

- Full path validation (traversal, reserved names, length, existence)
- Destination conflict detection
- No-shell-execution audit of all filesystem code
- Security-focused test suite covering all mitigations in Section 4
- AGENTS.md security rules documented
- Initial ADRs written

**Exit criterion:** Security test suite passes. No code paths use shell execution. All failure modes surface user-facing errors.

---

### M3 — UI Polish

Designer pass. Makes the app feel considered.

- Custom assets for drop zone and export zone
- Image swap on state transition
- Hover states
- Drag-over indicator
- State transition animations (WPF Storyboards)
- ViewModel interaction logic tests updated to cover new states

**Exit criterion:** App looks polished on a 1080p and a 4K display. No placeholder UI remains.

---

### M4 — Quality & Edge Cases

Hardens correctness and coverage.

- Edge case handling: long paths, reserved filenames, destination already exists, source removed mid-operation
- Integration tests against temp filesystem
- Full test coverage review
- README updated with any new behavior

**Exit criterion:** All edge cases have tests. No known unhandled failure modes.

---

### M5 — Distribution

Automates the build and release pipeline.

- Single-file portable `.exe` build confirmed
- Windows installer (WiX or NSIS)
- GitHub Actions workflow:
  - Runs full test suite on every push and PR
  - On version tag push: builds portable zip and installer, attaches both to GitHub Release
  - Release blocked if any test fails
- README updated with installer option and GitHub release link

**Exit criterion:** Pushing a version tag produces a GitHub Release with both artifacts, with zero manual steps.
