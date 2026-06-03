---
agent: agent
description: Guide me through choosing and running the right CoreEx.Template dotnet new commands for a new solution
tools: ['execute/runInTerminal', 'read', 'search', 'todo']
---

Guide this workspace through greenfield CoreEx scaffolding.

Goals:
- Confirm whether the current workspace is suitable for greenfield scaffolding; if not, stop before generating files.
- Ask only the questions needed to choose the smallest safe CoreEx shape.
- Use `.github/skills/coreex-scaffold/SKILL.md` as the primary workflow source when it exists.
- Recommend the exact `dotnet new coreex*` command set grounded in the user's needs.
- Verify `CoreEx.Template` is installed; install or update it when needed.
- Run the selected `dotnet new` commands in the correct order.
- Summarize what was scaffolded and any next manual steps.

Required questions:
1. Solution name using `[Company].[Product].[Domain]`.
2. Whether the service needs an API host, an outbox relay, and/or a subscriber host.
3. Data provider: `SqlServer`, `Postgres`, or `None`.
4. Messaging provider: `ServiceBus` or `None`.
5. Whether `refdata-enabled`, `outbox-enabled`, `domain-driven-enabled`, and `rop-enabled` should be on.

Guardrails:
- Do not overwrite existing solution content without explicit confirmation.
- Do not scaffold an outbox relay when `--data-provider None` or `--outbox-enabled false`.
- Keep the recommendation minimal; explain tradeoffs briefly only when a choice is still open.