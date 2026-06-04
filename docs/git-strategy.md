# Git Strategy — SlymLynk

## Core Approach: Trunk-Based Development

Single `main` branch. Short-lived branches (hours, not days). Everything merges fast or gets scrapped.

---

## Branch Naming

```
main
task/add-symlink-service
experiment/visual-states-redesign
fix/null-path-on-drop
```

---

## Merging

- **Fast-forward only** — no merge commits, keeps history linear
- **Rebase onto `main`** before merging, never merge `main` into your branch
- **No squashing** — each atomic commit is a meaningful unit; squashing destroys the audit trail

---

## Commits

One commit per task or prompt session. Keep commits atomic and scoped.

AI-generated code has no inherent intent — the commit message is the only record of *why* this code
exists. Use [Conventional Commits](https://www.conventionalcommits.org):

```
feat(symlink): add junction creation via reparse point
fix(validation): reject reserved Windows filenames
chore(deps): update CommunityToolkit.MVVM to 8.x
```

Annotate AI-assisted commits in the body, not the subject:

```
feat(symlink): add junction creation via reparse point

ai-assisted: claude-sonnet-4-6 | prompt: .prompts/symlink-service-m1.md
```

---

## Prompt Versioning

Store prompts that generated significant code alongside the code:

```
.prompts/
  symlink-service-m1.md
  viewmodel-states.md
  path-validation.md
```

---

## Feature Flags

Not used in this project at current scale. If M3 UI work needs to land incrementally on `main`
before it's visually complete, introduce a compile-time flag rather than a long-lived branch.

---

## Generated Sources

Do not commit generated source files. CommunityToolkit.MVVM source generators produce
`*.g.cs` files in `obj/` — these are already excluded by `.gitignore`. Commit lockfiles for
reproducibility; regenerate everything else from source.

---

## Code Review

Review diffs skeptically — AI code looks clean but can be subtly wrong.

High-blast-radius files always get manual review:

- `.gitignore` and `.gitattributes`
- `SymlinkService.cs` — any change to filesystem logic or path validation
- Anything touching P/Invoke declarations
- CI/CD configuration (`.github/workflows/`)

---

## CI

CI is load-bearing for this project — the release pipeline in M5 is gated on it.

Before anything merges to `main`:

- All existing tests must pass (`dotnet test`)
- New logic in `SymlinkService` and `MainViewModel` must have corresponding tests
- Build must succeed (`dotnet build`)

---

## Branch Protection (GitHub)

Enforce on GitHub:

- No direct push to `main`
- Require fast-forward / rebase-based merges
- Require CI to pass before merge

---

## Versioning

Tag milestones using `v0.x` until M5 ships a distribution artifact. After M5, use semver (`v1.0.0`
for first public release). The M5 GitHub Actions workflow triggers on version tag push to produce
the release artifacts.
