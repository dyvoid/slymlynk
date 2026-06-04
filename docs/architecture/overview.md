# Architecture Overview

## What this is

SlymLynk is a lightweight, portable Windows desktop utility for creating symbolic links and
junctions via drag-and-drop. It ships as a single self-contained `.exe` with no installation
required, targeting Windows 10+.

## Shape

A small WPF desktop app (~300×300 window). Three layers: a service that owns all filesystem logic,
a ViewModel that owns all UI state, and a View that is pure bindings and visual states. There is no
backend, no persistence, and no network. The entire operational surface is: accept a source path,
accept a destination path, create a pointer.

```
┌─────────────────────────────┐
│         MainWindow          │  View — XAML only, no logic
│  (drop zone / drag source)  │
└────────────┬────────────────┘
             │ bindings
┌────────────▼────────────────┐
│        MainViewModel        │  ViewModel — UI state, commands, orchestration
│  State: Idle / Loaded / Err │
└────────────┬────────────────┘
             │ calls
┌────────────▼────────────────┐
│       SymlinkService        │  Model/Service — filesystem ops, path validation
│  (junction / symlink logic) │
└─────────────────────────────┘
```

## Key Components

### SymlinkService (`Models/SymlinkService.cs`)

All filesystem logic lives here. Responsible for:
- Path validation (existence, traversal, reserved names, length, destination conflict)
- Determining link type: junction for folders, symbolic link for files
- Creating the junction via `Directory.CreateDirectory` + junction reparse point
- Creating the symbolic link via `CreateSymbolicLink` P/Invoke
- Throwing typed exceptions on all failure modes (privilege, conflict, invalid path)

Nothing else in the app touches the filesystem.

### MainViewModel (`ViewModels/MainViewModel.cs`)

Owns the three UI states:
- **Idle** — waiting for source input
- **SourceLoaded** — source path set; window becomes drag source and click-to-save target
- **Error** — creation failed; message describes why and what to do

Exposes commands for drag-drop accept, browse (source), drag-out initiation, save (destination),
and clear. Calls `SymlinkService` and maps results to state transitions.

### MainWindow.xaml (`Views/MainWindow.xaml`)

Pure XAML: bindings to `MainViewModel`, `DragEnter`/`Drop` event wiring, `VisualStateManager`
states for Idle/SourceLoaded/Error, and (in M3) image swaps and storyboard animations.
No logic in code-behind beyond event-to-command forwarding.

## Data / Control Flow

**Drag-in path:**
1. User drags file/folder onto window → `DragEnter` validates MIME type → `Drop` fires
2. `MainViewModel.AcceptDropCommand` receives path string
3. `SymlinkService.ValidateSource(path)` — existence, type detection
4. State transitions to `SourceLoaded`, source path stored

**Drag-out path:**
1. User initiates drag from window → `MainViewModel` starts `DragDrop.DoDragDrop`
2. User drops onto Explorer location → shell returns destination path
3. `SymlinkService.Create(source, destination)` — validates destination, creates junction/symlink
4. State returns to `SourceLoaded` (source stays; user can link to multiple destinations)
5. On failure → state transitions to `Error` with message

**Click path:** Identical flow, substituting `OpenFileDialog` / `SaveFileDialog` for the drag
events.

## WPF / .NET Structure

```
SlymLynk/
├── Models/
│   └── SymlinkService.cs
├── ViewModels/
│   └── MainViewModel.cs
├── Views/
│   └── MainWindow.xaml
├── Services/               (future: single-instance enforcement, dialog abstraction)
├── Assets/                 (M3: drop zone images, icons)
└── App.xaml
```

`CommunityToolkit.MVVM` is used throughout ViewModels for `[ObservableProperty]` and
`[RelayCommand]` — eliminates boilerplate and keeps ViewModels unit-testable without a UI host.

## Constraints

- **Windows 10+ only.** WPF, junctions, and symbolic links are all Windows-specific. No
  cross-platform ambitions.
- **No shell execution.** All filesystem operations use `System.IO` or P/Invoke. This is a hard
  security constraint, not a preference. See [ADR-003](../adr/0004-no-shell-execution.md).
- **No destructive operations.** The app creates one thing: a pointer. It never deletes, moves,
  or overwrites.
- **Single instance.** A second launch focuses the existing window. Enforced via a named Mutex.
- **Stateless.** No persistence between sessions. No history, no registry writes.
- **No network paths.** UNC paths are rejected. Out of scope for initial releases.

## Decisions

The reasoning behind specific choices lives in the [ADR log](../adr/). Start there before changing
anything structural.
