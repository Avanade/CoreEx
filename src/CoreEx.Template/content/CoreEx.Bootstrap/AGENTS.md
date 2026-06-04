# app-name -- CoreEx Bootstrap

This repository is intentionally bootstrapped with only the **CoreEx AI workflow assets**.

Run `/coreex-scaffold` to interview the business/domain shape and scaffold the smallest safe `dotnet new coreex*` command set for this solution.

Until that workflow runs, the repository is expected to stay mostly empty apart from:

- `.github/` instructions, prompts, skills, and the cached CoreEx docs.
- `.claude/commands/` prompt shims.
- this bootstrap guidance file.

If the repository still only contains this bootstrap shell, it is safe for `/coreex-scaffold` to use `dotnet new ... --force` when replacing placeholder files with the full solution scaffold.

Use `/coreex-expert` when you need CoreEx architecture guidance before choosing the final shape.