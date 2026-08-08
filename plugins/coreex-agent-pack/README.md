# CoreEx Agent Pack

This plugin packages CoreEx Copilot assets (agents and skills), including onboarding templates used by `coreex-onboard`.

## Published vs internal skills

This plugin intentionally publishes only external-facing skills:

- `coreex-onboard`
- `solution-scaffolder`

The following skills are internal-only and are not published in this plugin package:

- `acquire-codebase-knowledge`
- `aspire`
- `coreex-docs-sync`

## Source of truth

The canonical CoreEx instruction files remain in the repository root under:

- `.github/copilot-instructions.md`
- `.github/instructions/*.instructions.md`
- `.github/agents/coreex-expert.agent.md`

The copies under `skills/coreex-onboard/assets/templates/.github/` are distribution templates for plugin consumers.

## Long-term maintenance approach

The long-term solution is to **automate template syncing in CI** so maintainers do not edit both locations manually.

Recommended direction:

1. Keep editing only canonical files under root `.github/`.
2. Add a sync script that copies canonical files into plugin template paths.
3. Run the sync script in CI (and optionally as a pre-release check).
4. Fail CI when plugin templates drift from canonical sources.

Until CI sync is in place, updates must be mirrored manually.
