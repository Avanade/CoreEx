# CoreEx Scaffold

Guides a developer through selecting the right `CoreEx.Template` scaffolding shape and then runs the matching `dotnet new coreex*` commands.

## When to run

- The repository is empty or only contains bootstrap AI files.
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
#file:.github/skills/coreex-scaffold/SKILL.md scaffold a new CoreEx solution for my requirements
```

## What it will do

1. Inspect the workspace to confirm it is suitable for greenfield scaffolding.
2. Ask for the minimum set of choices needed to pick the right CoreEx shape.
3. Recommend the exact `dotnet new coreex*` commands.
4. Install `CoreEx.Template` if it is not already available.
5. Run the selected commands and summarize the output.

## Reference

- [SKILL.md](./SKILL.md) - main workflow guidance.