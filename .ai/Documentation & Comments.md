# Documentation and Comments

Universal standards for all file types in this project (C#, XAML, YAML, Markdown, etc.). Always applied on every change.

## Scope

Applies to inline comments, XML documentation, XAML comments, changelog text, README prose, and any other written technical content in the repository.

C#-specific XML documentation conventions are defined in `@.ai/Instructions & Rules.md`.

---

## Language

- **All** documentation and comments must be in **English**.
- Translate any non-English comments or documentation into English while preserving technical meaning.

---

## Documentation

- **Improve** existing documentation (XML doc, README sections, changelog entries, etc.) for clarity, accuracy, and completeness.
- **Generate** documentation where it is missing and the artifact is user-facing or part of a public API surface.
- Do not leave new or modified public contracts undocumented when documentation is expected for that file type.

---

## Smart Commenting

- **Keep and refine** comments that explain the **why** (intent), business rules, workarounds, or non-obvious logic.
- **Delete AI meta-talk** (for example, `// Added by AI`, `// Modification starts here`, `// End of fix`).
- **Delete redundancy** — comments that merely restate the code (for example, `// increment i` above `i++`).
- **Add value** only where logic is non-trivial or requires architectural context.
- **No filler** — no obvious comments, no TODO placeholders unless explicitly requested.

---

## Consistency

- Keep terminology and formatting consistent within each file and across related files.

---

**Last Updated:** 2026-06-15  
**Version:** 2.0
