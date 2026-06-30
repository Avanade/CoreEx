---
description: Create or modify a hand-authored contract (DTO/entity) in a CoreEx domain — root entity, subordinate, request/response, or base class extraction
---

Guide this workspace through creating or modifying a CoreEx contract.

Use `.github/skills/coreex-contract/SKILL.md` and its referenced workflow as the authoritative workflow contract when they exist.

Operational contract:
- Ask root vs. subordinate, identifier type, `IETag` (default yes for root), and `IChangeLog` need before emitting any code — batch all questions, never interrupt per-property.
- `[Contract]` + `partial` on all contract classes by default; only omit when explicitly asked.
- Only `[ReferenceData<T>]`-decorated properties are `partial` — never mark plain properties `partial` (CS9248).
- `[ReadOnly(true)]` on all server-assigned or derived fields.
- Apply `[Schema]` only when the user explicitly requests a custom event schema name or version.
- Never hand-author generated members or create/edit `*.g.cs` files.
- If any prompt text conflicts with the skill, the skill wins.

Outcome:
- The contract is declared correctly, builds cleanly, and Roslyn-generated members are available after build.
