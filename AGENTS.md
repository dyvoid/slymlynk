# AGENTS.md

This file is the primary context source for AI agents working in this repository. Read it before
doing anything else, and follow the instructions here. For deeper context on a specific topic,
follow the links in the index at the bottom.

This is the one place that governs how AI agents behave in this repo. The linked docs carry context
and decisions; they deliberately avoid AI-specific instructions so there's no second source of truth.

---

## Project Overview

SlymLynk is a lightweight, portable Windows desktop utility for creating symbolic links and
junctions via drag-and-drop. The entire interaction fits in a ~300×300 window. No installation
required — it ships as a single self-contained `.exe`.

Users drag a file or folder onto the window to set a source, then drag out (or click to save) to
create the link at a destination. Folders become junctions (no elevation); files become symbolic
links (requires Windows Developer Mode or UAC consent). The app is in active development, currently
targeting M1 (functional core).

---

## Architecture

WPF + .NET 8 desktop app, structured as a standard MVVM project: `Models/` holds
`SymlinkService` (all filesystem logic), `ViewModels/` holds `MainViewModel` (UI state and
commands), and `Views/` holds `MainWindow.xaml` (bindings and visual states). All filesystem
operations go through .NET APIs directly — no shell execution, no `Process.Start` for filesystem
ops. See [Architecture Overview](docs/architecture/overview.md) for the full picture, and
[the ADR log](docs/adr/) for the reasoning behind specific decisions.

---

## AI Instructions

### You can do these freely
- Write, edit, and refactor code that follows the patterns already in the codebase
- Create new files consistent with existing conventions
- Add or update XML doc comments on public types and members
- Add tests for new or existing functionality
- Update `README.md` to reflect completed features (required on each milestone close)

### These need human review before they land
- `.gitignore` and `.gitattributes`
- Anything touching path validation or symlink creation logic — security-critical
- Dependency changes (`*.csproj`, `Directory.Packages.props`, `*.lock`)
- Cross-cutting refactors that touch multiple modules
- CI/CD configuration (`.github/workflows/`)
- Visual state definitions and storyboard animations (M3 work)

### Do not do these
- Commit directly to `main`
- Delete or rename files without being asked
- Change architecture without recording an ADR in `docs/adr/`
- Use `Process.Start`, `cmd.exe`, PowerShell, or any shell execution for filesystem operations
- Introduce write operations other than creating the symlink/junction pointer itself
- Add NuGet dependencies without explicit instruction

---

## Conventions

### Branching
Short-lived branches only: `task/`, `fix/`, `experiment/`. Details in [Git Strategy](docs/git-strategy.md).

### Commits
One commit per task or prompt session. [Conventional Commits](https://www.conventionalcommits.org).
Put AI context in the body, not the subject:

```
feat(scope): short imperative summary

ai-assisted: <model> | prompt: .prompts/<name>.md
```

### Prompts
Prompts that produced meaningful code live in `.prompts/`. Reference them from the commit body.

### C# / WPF specifics

- **Naming:** PascalCase for all public members; `_camelCase` for private fields. No `m_` prefix.
- **MVVM:** ViewModels use `CommunityToolkit.MVVM` (`[ObservableProperty]`, `[RelayCommand]`). No
  code-behind logic — interaction logic belongs in the ViewModel, not `MainWindow.xaml.cs`.
- **No shell execution.** All filesystem operations use `System.IO` or P/Invoke. Never
  `Process.Start` for filesystem work.
- **Validate before acting.** Existence, traversal, reserved names, and path length are checked
  before any `CreateSymbolicLink` or `Directory.CreateDirectory` call.
- **Fail loudly.** Unexpected conditions throw; errors surface as user-facing messages. No silent
  swallowing.
- **Tests alongside features.** Every new public method in `SymlinkService` and every state
  transition in `MainViewModel` needs a corresponding test in `SlymLynk.Tests`. A milestone does not
  close until its tests pass.
- **XML docs on all public members.** One-line `<summary>` minimum.

---

## Security Rules

These are non-negotiable. Flag any code that violates them before committing.

1. No shell execution. Period.
2. Resolve the full path (`Path.GetFullPath`) before any validation or operation.
3. Check that the resolved source path exists before use.
4. Check that the destination path does not already exist before creation.
5. Reject paths containing `..` sequences or UNC paths for now (network paths are out of scope).
6. Reject paths that exceed `MAX_PATH` (260) unless long-path support is confirmed.
7. Reject reserved Windows filenames (`CON`, `PRN`, `AUX`, `NUL`, `COM1`–`COM9`, `LPT1`–`LPT9`).

---

## Document Index

| Document | What it covers |
|---|---|
| [Architecture Overview](docs/architecture/overview.md) | System structure, key components, data flow |
| [ADR Log](docs/adr/) | Architecture decisions and their rationale |
| [Git Strategy](docs/git-strategy.md) | Branching, merging, commit rules |
| [PICKUP](PICKUP.md) | Where the last session left off |
