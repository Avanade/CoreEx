---
agent: agent
tools: ['execute/runInTerminal', 'read', 'search', 'todo']
---
Run the repository initialization checklist.

Goals:
- Verify required local dependencies for the samples:
  - Podman.
  - Podman Desktop.
  - Podman Compose.
  - .NET 10 SDK.
- If any dependency is missing, attempt installation automatically using `winget`.
- Re-run verification and report final status.

Execution steps:
1. Verify dependencies using terminal commands:
  - Podman: `podman --version`.
  - Podman Compose: `podman compose version`.
  - .NET 10 SDK: `dotnet --list-sdks` and confirm at least one SDK starts with `10.`.
  - Podman Desktop:
    - First try `winget list --id RedHat.Podman-Desktop --exact --accept-source-agreements`.
    - If needed, also check for `Program Files\Podman Desktop\Podman Desktop.exe`.
2. If `winget` is available, install missing dependencies:
  - Podman: `winget install --id RedHat.Podman --exact --accept-package-agreements --accept-source-agreements`.
  - Podman Desktop: `winget install --id RedHat.Podman-Desktop --exact --accept-package-agreements --accept-source-agreements`.
  - .NET 10 SDK: `winget install --id Microsoft.DotNet.SDK.10 --exact --accept-package-agreements --accept-source-agreements`.
3. If Podman is present but Compose is missing, run a Podman upgrade:
  - `winget upgrade --id RedHat.Podman --exact --accept-package-agreements --accept-source-agreements`.
4. Re-run all verification checks.
5. Summarize what was installed and what still requires manual intervention.

If `winget` is unavailable, report that manual install is required and list the dependency names exactly.
