# ADR 0003: Junctions for folders, symbolic links for files

- **Status:** Accepted
- **Date:** 2026-06-04

## Context

Windows offers three link types: hard links (files only, same volume), junctions (folders, local
volumes, no elevation), and symbolic links (files and folders, requires Developer Mode or admin).
The app must create links without forcing the user to run as administrator where possible.

## Decision

- **Folders → junction.** Junctions require no elevation, work transparently across local volumes,
  and behave identically to symlinks for the typical use case. Created via reparse point APIs.
- **Files → symbolic link.** Junctions do not support files. File symbolic links require either
  Windows Developer Mode (preferred — no UAC prompt) or UAC elevation. If creation fails due to
  insufficient privileges, the app surfaces a clear error with instructions to enable Developer Mode.

Hard links are not used. They are same-volume only, do not work for folders, and have copy
semantics that differ from symlinks in ways that would surprise users.

## Consequences

- Folder linking works silently for all users, no prompt required.
- File linking requires the user to have Developer Mode enabled or accept a UAC prompt. This is
  unavoidable given Windows security policy; the app explains it clearly rather than hiding it.
- The auto-selection logic (folder → junction, file → symlink) must be implemented in
  `SymlinkService` and covered by tests, since getting it backwards creates a silent failure.
- Network paths are out of scope. Both junctions and symlinks have edge-case behavior on network
  paths; rejecting them at validation time is the correct approach for now.
