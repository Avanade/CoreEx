# app-name

This template creates an intentionally empty, AI-ready CoreEx bootstrap repository.

## Next Step

Run `/coreex-scaffold` in Copilot Chat or Claude Code. The workflow will ask about:

- the bounded context and solution name;
- whether the domain owns data or is a facade;
- whether it needs an API host;
- whether it needs reliable event publishing and an outbox relay;
- whether it needs to consume events with a subscriber host;
- whether reference data, domain-driven design, and ROP should be enabled.

From that interview, the workflow will derive and run the right `dotnet new coreex*` commands for the solution.