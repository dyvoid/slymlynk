# ADR 0002: WPF over Tauri / Electron

- **Status:** Accepted
- **Date:** 2026-06-04

## Context

SlymLynk is a Windows-only desktop utility. The framework choice determines the distribution story
(single exe vs. runtime dependency), the drag-drop capability, the UI ceiling, and the dev
experience. The primary alternatives considered were Tauri (Rust + WebView2) and Electron (Node +
Chromium).

## Decision

WPF + .NET 8, published as a self-contained single-file executable.

## Consequences

- Native Windows drag-drop support (`IDropTarget`, `IDataObject`) is first-class in WPF. Getting
  the same behavior in Tauri or Electron requires bridging between the web layer and the OS shell,
  which adds complexity and reduces reliability for the core interaction.
- Single-file self-contained publish (`dotnet publish -r win-x64 --self-contained`) produces one
  portable `.exe` with no runtime dependency. Tauri achieves a similar result; Electron bundles are
  significantly larger.
- C# is the developer's primary language (Unity background), so WPF has no context-switching cost.
  Tauri would require Rust; Electron would require JavaScript for the Chromium layer.
- WPF is Windows-only by design. This is not a constraint — SlymLynk is a Windows-only tool
  targeting Windows filesystem features. Cross-platform is explicitly out of scope.
- WPF's visual ceiling (storyboards, vector graphics, custom controls) is more than sufficient for
  the M3 polish goals without adding a third-party animation library.
