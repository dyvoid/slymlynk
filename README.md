# SlymLynk

![Build](https://github.com/dyvoid/slymlynk/actions/workflows/ci.yml/badge.svg)
![.NET](https://img.shields.io/badge/.NET-8-512BD4?logo=dotnet)
![Windows](https://img.shields.io/badge/Windows-10+-0078D6?logo=windows)

A lightweight, portable Windows desktop utility for creating symbolic links and junctions via
drag-and-drop. The entire interaction fits in a ~300×300 window. No installation required.

## Status

**M2 in progress** — security hardening. The functional core (M1) is complete.

| Milestone | Description | Status |
|---|---|---|
| M1 | Functional core | ✅ Complete |
| M2 | Security hardening | In progress |
| M3 | UI polish | [Design ready](docs/design/m3-wormhole-ui.md) |
| M4 | Edge cases & full test coverage | Planned |
| M5 | Distribution (portable exe + installer + CI release) | Planned |

## How it works

1. Drag a file or folder onto the window — or click to browse.
2. Drag out to any Explorer location — or click to choose a destination.

SlymLynk creates a junction (for folders) or symbolic link (for files) at the destination.
Junctions require no elevation. File symlinks require Windows Developer Mode or UAC consent.

## Requirements

- Windows 10 or later
- .NET 8 runtime (or use the self-contained build)
- For file symlinks: [Windows Developer Mode](https://learn.microsoft.com/en-us/windows/apps/get-started/enable-your-device-for-development) enabled, or run as administrator

## Getting Started

### Run (portable)

Download the latest release from the [Releases page](#) and run `SlymLynk.exe` directly. No
installation required.

### Build from source

```
git clone https://github.com/your-org/slymlynk
cd slymlynk/SlymLynk
dotnet build
dotnet run
```

### Run tests

```
cd slymlynk
dotnet test
```

## Project Structure

```
SlymLynk/               Main WPF application
  Models/               SymlinkService — filesystem logic and path validation
  ViewModels/           MainViewModel — UI state and commands
  Views/                MainWindow.xaml — bindings and visual states
  Assets/               Icons and image assets
SlymLynk.Tests/         Unit and integration tests
docs/                   Architecture, decisions, and guides
.prompts/               Versioned prompts that generated significant code
AGENTS.md               Context and instructions for AI agents
```

## Documentation

- [Architecture Overview](docs/architecture/overview.md)
- [Architecture Decisions](docs/adr/)
- [Security Model](SECURITY.md)
- [Git Strategy](docs/git-strategy.md)
