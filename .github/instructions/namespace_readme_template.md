# NAMESPACE README TEMPLATE

This file is an authoring guide and template for creating `README.md` files across the `CoreEx` library projects. Every `<instruction>` block below is guidance for the author and must be removed from the final output. All placeholder text in `[square brackets]` must be replaced with real content.

---

## Research checklist (do this before writing)

Before authoring any README, gather the following context:

1. **Read all source files** in the target namespace/folder — XML doc comments are the primary source of truth for descriptions.
2. **Check usages in other `src\` projects** — frequent references reveal which types are most important.
3. **Check usages in `samples\`** — especially the Contoso domains; use this to inform importance, relevance, and accurate descriptions, but do not reference or link to samples in the README output itself.
4. **Read the `.csproj`** — NuGet dependencies (non-Microsoft ones in particular) to determine if they should be linked in Additional Resources.
5. **Check child folders** — decide which sub-namespaces have enough public surface to warrant their own README.

---

## Agreed authoring conventions

These conventions apply to every README created from this template:

- **No NuGet badges or install snippets.** The root `README.md` holds all package badges and `dotnet add package` commands; do not repeat them here.
- **No code blocks, usage examples, or sample references.** READMEs document the namespace/package — they do not demonstrate usage. The Contoso samples under `samples/` have their own dedicated READMEs for that purpose. Do not link to or mention samples anywhere in a namespace README.
- **Type table formatting:**
  - **`Bold link`** — concrete/public classes and structs.
  - _`Italic link`_ — abstract base classes.
  - [`Plain link`] — interfaces.
  - Enums and static utility classes use **bold** like concrete types.
- **`Abstractions` sub-folders** — contain internal or foundational base types; only create a child README if the public surface is large enough to warrant one.
- **README depth** — always create READMEs for first-level child namespaces. Only recurse into deeper levels (child-child and beyond) where there is genuinely material content — thin wrappers, single-file folders, or purely internal helpers do not need a README.
- **Related Namespaces** — always annotate test-only relationships with `_(test only)_` after the description.
- **Omit empty sections** — if a section has no material content (e.g., no external resources, no child namespaces), omit the section heading and body entirely rather than leaving a placeholder.
- **Proceed project by project** — complete all namespace READMEs for one project before starting the next.

---

## Template (copy everything below this line into the new README.md)

---

<!-- <instruction> Replace `[Namespace]` in the title with the actual namespace or package name, e.g. `CoreEx.Events`. </instruction> -->

# CoreEx.[Namespace]

<!-- <instruction> Write 1-2 sentences that precisely describe what this namespace/package provides. Be specific — avoid vague phrases like "provides utilities for". </instruction> -->

> Brief description of what this namespace/package provides.

## Overview

<!-- <instruction>
Write 2-3 paragraphs covering:
- The concrete problem or gap this namespace addresses.
- The primary scenarios and patterns where it is used.
- How it relates to and fits into the broader CoreEx ecosystem.
</instruction> -->

## Motivation

<!-- <instruction>
Include this section ONLY at the project (top-level package) README — not in child-namespace READMEs.
Explain why this project exists: what gaps in .NET or other frameworks it addresses, the design principles that shaped it, and why it was built the way it was. Prefer concise dot-pointed lists over dense paragraphs.
</instruction> -->

- Motivation point 1.
- Motivation point 2.

## Key capabilities

<!-- <instruction>
List the headline capabilities. Each bullet should start with a relevant emoji, a **bold capability name**, then a brief plain-English description of the benefit to the user. Aim for 4–8 bullets at the project level; 3–5 at child-namespace level.
</instruction> -->

- 🔷 **Capability name**: Description of the capability and its benefit.

## Key types

<!-- <instruction>
List the most important public types in this namespace. Derive importance from: XML doc comment content, how widely they are referenced across `src\` and `samples\`, and whether they form part of the public contract or extension points.

Formatting rules:
- **`Bold link`** — concrete classes and structs.
- _`Italic link`_ — abstract base classes.
- [`Plain link`] — interfaces.
- Enums and static utility classes use **bold** like concrete types.

Only include types from this specific namespace folder — child-namespace types are covered in their own README.
</instruction> -->

| Type | Description |
|------|-------------|
| **[`ClassName`](./ClassName.cs)** | What this class does. |
| _[`AbstractBase`](./AbstractBase.cs)_ | What this abstract class provides. |
| [`IInterface`](./IInterface.cs) | What this interface defines. |

## Namespaces

<!-- <instruction>
Include this section ONLY at the project (top-level package) README — not in child-namespace READMEs, unless that namespace itself has notable sub-namespaces with material content.
List all first-level child namespaces. For each, create the corresponding child README and link to it. Only include deeper levels (child-child) where there is material content worth documenting.
</instruction> -->

| Namespace | Description | Documentation |
|-----------|-------------|---------------|
| **`CoreEx.[Child]`** | Brief description of what this child namespace contains. | [📖 README](./[Child]/README.md) |

## Related Namespaces

<!-- <instruction>
List other CoreEx namespaces/packages that have a strong relationship to this one — including sibling packages, packages that extend this one, or packages this one commonly works alongside. Use relative links. Annotate test-only relationships with `_(test only)_`.
Omit this section if there are no meaningful relationships to document.
</instruction> -->

- **[`CoreEx.RelatedNamespace`](../RelatedNamespace/README.md)** - Brief description of the relationship.
- **[`CoreEx.TestHelper`](../../CoreEx.UnitTesting/README.md)** - Brief description. _(test only)_

## Additional Resources

<!-- <instruction>
Include this section only if there are genuinely useful external resources — non-Microsoft NuGet dependencies, relevant RFCs, specification documents, or authoritative external docs. Omit entirely if there is nothing material to link.
</instruction> -->

- [Resource name](https://example.com) - Brief description of why it is relevant.