# Acquire Codebase Knowledge

Maps an unfamiliar codebase and produces seven structured onboarding documents — everything a new team member needs to work effectively on the project, grounded entirely in what the files and tooling actually show.

## When to use

- Onboarding onto a codebase you haven't worked in before.
- Producing architecture documentation for a team or reviewer.
- Auditing a project's structure, conventions, and concerns before a large change.

Not for routine feature work, bug fixes, or narrow code edits.

## How to invoke

**Claude Code:**
```
/acquire-codebase-knowledge
```

**GitHub Copilot Chat:**
```
#file:.github/skills/acquire-codebase-knowledge/SKILL.md  map this codebase
```

Optionally supply a focus area — e.g. `architecture only` or `testing and concerns` — to prioritise those documents first while still producing the full set.

## What gets produced

Seven documents written to `docs/codebase/`:

| Document | Content |
|----------|---------|
| `STACK.md` | Languages, frameworks, runtimes, and key dependencies |
| `STRUCTURE.md` | Directory layout and project organisation |
| `ARCHITECTURE.md` | Architectural style, layers, component relationships |
| `CONVENTIONS.md` | Coding standards, naming, patterns in use |
| `INTEGRATIONS.md` | External services, APIs, and infrastructure dependencies |
| `TESTING.md` | Test strategy, frameworks, and coverage approach |
| `CONCERNS.md` | Known issues, gaps, tech debt, and open questions |

Every claim is traceable to a source file or terminal output. Unknowns are marked `[TODO]`; intent-dependent decisions are marked `[ASK USER]` and surfaced at the end of the run.

## Reference

- [SKILL.md](./SKILL.md) — full 4-phase workflow, output contract, and focus-area mode detail.
