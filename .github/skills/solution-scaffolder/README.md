# CoreEx Scaffold

Guides a developer through selecting the right `CoreEx.Template` scaffolding shape and then runs the matching `dotnet new coreex*` commands.

## When to run

- The repository is empty or only contains the `coreex-bootstrap` AI shell.
- You want the agent to choose the smallest safe CoreEx shape before scaffolding.
- You want an interactive workflow instead of manually composing the template commands.

## How to invoke

**GitHub Copilot Chat:**
```
/coreex-scaffold
```

**Claude Code:**
```
/coreex-scaffold
```

If the prompt file is not present, attach the skill file directly in Copilot Chat:

```
#file:.github/skills/solution-scaffolder/SKILL.md scaffold a new CoreEx solution for my requirements
```

## What it will do

1. Inspect the workspace to confirm it is suitable for greenfield scaffolding.
2. Ask about the business/domain shape through one-question-at-a-time confirmation cards with defaults, then derive the required hosts.
3. Recommend the exact `dotnet new coreex*` commands.
4. Install `CoreEx.Template` if it is not already available.
5. When starting from `coreex-bootstrap`, replace the bootstrap placeholders with `dotnet new ... --force`.
6. Run the selected commands and summarize the output.

## Interview behavior

- The scaffold flow should use a form-style confirmation card for each question when `mcp_microsoft_git_confirm_options` is available.
- Each card should contain a single editable field and a preselected default.
- The agent should ask one question at a time and wait for confirmation before moving on.

## Reference

- [SKILL.md](./SKILL.md) - main workflow guidance.
