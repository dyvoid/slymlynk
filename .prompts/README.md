# .prompts

Versioned prompts that generated significant code in this project.

When an AI session produces a meaningful chunk of code, save the prompt that drove it here as a
markdown file, and reference it from the commit body:

```
feat(symlink): add junction creation via reparse point

ai-assisted: claude-sonnet-4-6 | prompt: .prompts/symlink-service-m1.md
```

This makes significant code reproducible and debuggable: you can see not just what was written, but
what was asked for. Name files after the task, not the date.
