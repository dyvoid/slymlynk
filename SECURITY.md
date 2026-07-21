# Security Model

SlymLynk creates filesystem link pointers (junctions and symbolic links) from user-chosen
sources to user-chosen destinations. That is the entire attack surface: two paths in, one
reparse point out. This document describes how that surface is defended.

## Principles

1. **No shell execution.** All filesystem operations use `System.IO` or direct P/Invoke
   (`CreateSymbolicLink`, `DeviceIoControl`). Nothing is ever passed to `cmd.exe`,
   PowerShell, or `Process.Start`. See [ADR-0004](docs/adr/0004-no-shell-execution.md).
2. **Single write operation.** The only write SlymLynk performs is creating the link
   pointer itself (plus the empty directory stub a junction requires). It never creates
   intermediate directories, never overwrites, never deletes.
3. **Validate before acting.** Every path is fully resolved (`Path.GetFullPath`) and
   validated before any filesystem call. Validation is enforced at compile time: `Create`
   only accepts a `ValidatedSource`, which can only be obtained from `ValidateSource`
   ([ADR-0006](docs/adr/0006-validatedsource-value-type.md)).
4. **Fail closed.** Invalid input produces a typed exception and a user-facing error
   message. There is no fallback path that proceeds with an unvalidated input.

## Validation pipeline

Both the source path and the destination path pass through the same checks, in order:

| # | Check | Rejects |
|---|---|---|
| 1 | Null/empty/whitespace | Empty input |
| 2 | Control characters | Null bytes, newlines, escape chars — truncation/injection vectors. Checked on the *raw* input, before normalisation. Error messages deliberately do not echo the offending path. |
| 3 | Full resolution | `Path.GetFullPath` — all later checks run on the resolved absolute path |
| 4 | UNC paths (`\\…`) | Network paths, `\\?\` and `\\.\` device namespaces — out of scope |
| 5 | Traversal remnants (`..`) | Any `..` surviving normalisation (belt-and-suspenders; also rejects filenames that legitimately contain `..`) |
| 6 | Colon past the drive specifier | NTFS alternate data streams (`file.txt:stream`), malformed drive syntax |
| 7 | Length > 260 (`MAX_PATH`) | Long paths, until long-path support is confirmed end-to-end (revisit in M4) |
| 8 | Per-component checks | Reserved device names (`CON`, `NUL`, `COM1`…, including with extensions, in *every* component), characters Windows forbids in filenames (`" < > \| * ?`), components ending in a dot or space |

Source paths must additionally **exist**; destination paths must **not exist** and must
have an **existing parent directory** (SlymLynk will not create intermediate directories).

Adversarial inputs (traversal payloads, device paths, RTL-override tricks, unicode
homoglyphs, oversized paths, random fuzz with a fixed seed) are exercised in
`SymlinkServiceHardeningTests` — validation must either reject with a controlled
exception type or resolve to a real, existing path.

## Privilege model

- **Junctions (folders):** never require elevation. This is why folders map to junctions
  ([ADR-0003](docs/adr/0003-junctions-for-folders-symlinks-for-files.md)).
- **File symbolic links:** require Windows Developer Mode or an elevated process.
  `SymlinkService.DetectSymlinkPrivilege()` checks upfront — Developer Mode via the
  `AppModelUnlock` registry value, then elevation via `WindowsPrincipal` — and `Create`
  refuses file-link attempts with a clear remediation message when neither is present.
  The `ERROR_PRIVILEGE_NOT_HELD` (1314) handler remains as a backstop in case the
  detected state goes stale between check and use.
- SlymLynk never requests elevation itself and contains no UAC prompts.

## Reviewed exception swallowing

`DragOutHelper` (drop-target detection via `Shell.Application` COM) intentionally
swallows exceptions in two places, reviewed for M2:

- No user-controlled data crosses that boundary and no state is mutated there, so
  swallowing cannot mask a partial write.
- Failure degrades to "not an Explorer window": the caller falls back to the standard
  save dialog, so nothing fails silently from the user's perspective.
- Shell window enumeration is capped (`MaxShellWindows = 512`) so an out-of-process COM
  server cannot drive an unbounded loop.

Everywhere else, the "fail loudly" rule from `AGENTS.md` applies: unexpected conditions
throw, and errors surface as user-facing messages.

## Out of scope

- Network (UNC) paths — rejected outright.
- Long-path (`\\?\`) support — rejected until confirmed; tracked for M4.
- SlymLynk does not defend against an attacker who already has code execution as the
  user: it runs unelevated with the user's own rights and grants nothing beyond them.

## Reporting

This is a small utility project; please report security issues via GitHub issues on the
repository (or privately to the maintainer if disclosure matters).
