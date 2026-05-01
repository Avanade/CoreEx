---
agent: agent
description: Get my development workspace ready
tools: ['browser', 'execute/runInTerminal', 'read', 'search', 'todo']
---

Goals:
- Start the docker-compose dependencies for Aspire only if not already running.
- Start the Aspire host without the debugger so all sample services start.
- Run the Contoso E2E runner to validate behavior.

## checklist

- [ ] Start the docker-compose dependencies for Aspire if not already running (which is at root of repo):
   - `podman compose -f docker-compose.yaml up -d`
   - Wait for all containers to report healthy status by polling `podman ps` output.
   - If any container fails to start or becomes unhealthy, capture key log lines with `podman logs` and report failure with remediation suggestions.

- [ ] Start Aspire without debugger in a dedicated terminal:
   - `dotnet run --project samples/aspire/Contoso.Aspire`
   - Keep this terminal running and do not await for any user input
   - Wait for readiness by polling output until:
     - startup/readiness messages indicate services are running, and
     - no fatal startup exception is present.
   - If readiness is not reached within a reasonable timeout, report failure with key log lines.

- [ ] Direct the user to start the Contoso E2E runner in the next step by running `dotnet run` from `samples/tests/Contoso.E2E.Runner` directory in a new terminal.

Failure handling:
- If any command fails, capture the key error lines and include a concise remediation suggestion.