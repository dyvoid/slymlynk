# ADR 0004: No shell execution for filesystem operations

- **Status:** Accepted
- **Date:** 2026-06-04

## Context

Symbolic links and junctions can be created via `cmd.exe` (`mklink`) or PowerShell. These are the
most commonly documented approaches. However, shell execution introduces risks that are unacceptable
for an app that operates on user-supplied paths: command injection via crafted filenames, implicit
reliance on PATH resolution, and behavior that varies across Windows versions and configurations.

## Decision

All filesystem operations use .NET APIs directly (`System.IO`, P/Invoke where needed). No
`Process.Start`, no `cmd.exe`, no PowerShell, no shell execution of any kind for filesystem work.

Specifically:
- Junctions: created via `DeviceIoControl` with `FSCTL_SET_REPARSE_POINT` (P/Invoke or a wrapper)
- Symbolic links: created via `CreateSymbolicLink` Win32 API (P/Invoke)
- Path operations: `System.IO.Path`, `System.IO.File`, `System.IO.Directory`

## Consequences

- Command injection via crafted drag payloads is impossible — there is no shell to inject into.
- The implementation is slightly more verbose than `Process.Start("cmd", "/c mklink ...")` but is
  fully testable, reliable, and version-independent.
- AGENTS.md explicitly forbids shell execution as an AI instruction. Any generated code that uses
  `Process.Start` for filesystem operations must be rejected before commit.
- This rule applies to all filesystem operations, including any future additions. If a new operation
  genuinely cannot be done without shell execution, that requires a new ADR and explicit approval.
