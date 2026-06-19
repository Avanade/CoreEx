# Aspire

Orchestrates the Aspire distributed application using the Aspire CLI — starting, stopping, inspecting resources, tailing logs, and managing the AppHost without needing to remember every command.

## When to use

Use for anything Aspire-specific: starting the app, waiting for a resource to be healthy, viewing logs or traces, adding an integration, or running environment diagnostics.

Use `dotnet` CLI directly for non-Aspire .NET operations (build, test, run a single project).

## How to invoke

**Claude Code:**
```
/aspire
```

**GitHub Copilot Chat:**
```
#file:.github/skills/aspire/SKILL.md  start the app and wait for the API to be healthy
```

Optionally supply a resource name, command, or context — e.g. `start isolated`, `logs products-api`, `debug startup failure`.

## Common operations

| What you want | Command |
|---------------|---------|
| Start the app | `aspire start` |
| Start isolated (no shared state with other instances) | `aspire start --isolated` |
| Wait until a resource is healthy | `aspire wait <resource>` |
| Stop the app | `aspire stop` |
| List all resources and their status | `aspire describe` |
| Stream console logs | `aspire logs [resource]` |
| View structured logs / traces | `aspire otel logs [resource]` · `aspire otel traces [resource]` |
| Rebuild a changed .NET project | `aspire resource <resource> rebuild` |
| Add an Aspire integration | `aspire add` |
| Environment diagnostics | `aspire doctor` |

## Reference

- [SKILL.md](./SKILL.md) — full CLI command reference, key workflows, and agent environment guidance.
