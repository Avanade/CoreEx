# AI Context

Run `dotnet new coreex-ai` at the **repo root** to install CoreEx AI workflow assets for this domain.

Once installed, the following are available in `.github/`:

- `instructions/` — CoreEx coding conventions, application services, validators, repositories, host setup, tests, etc.
- `prompts/coreex-scaffold.prompt.md` — guided solution scaffolding via `/coreex-scaffold`
- `agents/coreex-expert.agent.md` — architecture guidance via `/coreex-expert`

For monorepos where CoreEx lives under a subfolder, pass `--app-folder <relative-path>` to scope the instruction files appropriately.
