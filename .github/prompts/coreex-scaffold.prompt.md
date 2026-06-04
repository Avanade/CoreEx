---
agent: agent
description: Guide me through choosing and running the right CoreEx.Template dotnet new commands for a new solution
tools: ['execute/runInTerminal', 'read', 'search', 'todo']
---

Guide this workspace through greenfield CoreEx scaffolding.

Goals:
- Confirm whether the current workspace is suitable for greenfield scaffolding; if not, stop before generating files.
- Treat a repository that only contains the bootstrap AI shell as greenfield and safe to scaffold.
- Ask only the questions needed to choose the smallest safe CoreEx shape.
- Use `.github/skills/coreex-scaffold/SKILL.md` as the primary workflow source when it exists.
- Recommend the exact `dotnet new coreex*` command set grounded in the user's needs.
- Verify `CoreEx.Template` is installed; install or update it when needed.
- Run the selected `dotnet new` commands in the correct order.
- Summarize what was scaffolded and any next manual steps.

Required questions:
1. Solution name using `[Company].[Product].[Domain]`.
2. Whether the domain owns persistence (`SqlServer`, `Postgres`) or is a facade (`None`).
3. Whether the domain needs an inbound HTTP API.
4. Whether it must publish reliable integration events, which implies deciding messaging and whether an outbox relay is needed.
5. Whether it must consume integration events, which implies deciding whether a subscriber host is needed.
6. Whether `refdata-enabled`, `outbox-enabled`, `domain-driven-enabled`, and `rop-enabled` should be on.

Host derivation rules:
- Add `coreex` for the shared solution core whenever the repo is still empty or bootstrap-only.
- Add `coreex-api` when the domain exposes HTTP.
- Add `coreex-relay` only when the solution owns data, uses the outbox, and publishes reliable integration events.
- Add `coreex-subscriber` when the domain consumes integration events.

Guardrails:
- Do not overwrite existing solution content without explicit confirmation.
- If the repository contains only the bootstrap template output, `dotnet new ... --force` is allowed to replace placeholder bootstrap files.
- Do not scaffold an outbox relay when `--data-provider None` or `--outbox-enabled false`.
- Keep the recommendation minimal; explain tradeoffs briefly only when a choice is still open.