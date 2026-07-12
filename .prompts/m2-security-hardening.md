# M2 security hardening (design-independent work)

## Prompt

> Are there any steps we can take in this project that doesn't require design?
>
> Yep go ahead and do what you can

## Interpretation

M3 (UI polish) is blocked on wormhole PNG assets, but M2 (security hardening) is entirely
design-independent and is the declared next milestone. The session implemented the full M2
checklist from PICKUP.md:

- Hardened `SymlinkService` path validation (control chars, reserved names in all
  components, ADS colons, invalid filename chars, trailing dot/space, destination parent
  existence).
- Added upfront symlink privilege detection (`SymlinkPrivilege`,
  `DetectSymlinkPrivilege()`), with `Create` refusing file links when denied.
- Reviewed and documented the swallowed exceptions in `DragOutHelper`; capped shell window
  enumeration.
- Added `SymlinkServiceHardeningTests` (curated adversarial battery + seeded random fuzz).
- Wrote `SECURITY.md`; fixed the stale M1 status in README; updated PICKUP.md.

Verified by compiling the changed files and running the platform-neutral tests (44
passing) on Linux; Windows-specific cases (junction creation, UNC parsing) rely on the
Windows CI run.
