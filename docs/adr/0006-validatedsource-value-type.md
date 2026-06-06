# ADR 0006: Carry validation in a `ValidatedSource` value type

- **Status:** Accepted
- **Date:** 2026-06-06

## Context

`SymlinkService` originally exposed three public methods whose correct use depended on an implicit
ordering contract the interface did not enforce:

- `string ValidateSource(string path)` — validates and returns the resolved path.
- `bool IsDirectory(string validatedSourcePath)` — re-hits the filesystem to decide link type.
- `void Create(string sourcePath, string destinationPath)` — whose doc comment said *"Both paths
  must already be validated via `ValidateSource`"*, but whose signature accepted any `string`.

The "this path has been validated" invariant lived in a comment and in caller discipline, not in the
type system. Nothing stopped a caller passing an unvalidated source to `Create`, and `IsDirectory`
re-derived from the filesystem something validation already knew. This is a security-critical module
(path validation and symlink creation), so leaving the validation invariant unenforced by the type
system is a latent risk, not just an aesthetic one.

## Decision

Validation produces a `ValidatedSource` value type that carries the resolved absolute path and the
file-vs-directory distinction:

```csharp
public readonly record struct ValidatedSource(string Path, bool IsDirectory);
```

- `ValidateSource(string path)` returns a `ValidatedSource`.
- `Create(ValidatedSource source, string destination)` accepts the value type. You cannot call
  `Create` without first producing a `ValidatedSource`, so the validated-before-create invariant is
  enforced by the compiler rather than by discipline.
- The file-vs-directory decision is computed once during validation and carried on the value type,
  removing the separate `IsDirectory(string)` filesystem round-trip from the public interface.

The link-type selection rule itself (folder → junction, file → symlink) is unchanged and stays in
`SymlinkService`; this ADR does not revisit [ADR-0003](0003-junctions-for-folders-symlinks-for-files.md).

## Consequences

- The validation invariant is concentrated in one type. A caller holding a `ValidatedSource` has a
  compile-time guarantee that validation ran, eliminating a class of misuse on a security-critical
  path.
- `MainViewModel` holds the `ValidatedSource` after a successful drop instead of a bare path string,
  so it no longer re-queries the filesystem to decide link type at save time.
- The public interface of `SymlinkService` changes. Per AGENTS.md, that interface change required
  human review and this ADR.
- `ValidatedSource` is a `readonly record struct`: cheap, immutable, and value-comparable, which
  keeps it easy to assert against in tests.
