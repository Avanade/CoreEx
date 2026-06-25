# CoreEx Onboard Workflow

## Phase 1: Preflight

1. Confirm the working directory is a repository root.
2. Ensure these paths exist or can be created:
   - `.github/`
   - `.github/instructions/`
   - `.github/agents/`
3. Discover template files from:
   - `assets/templates/.github/copilot-instructions.md`
   - `assets/templates/.github/instructions/*.instructions.md`
   - `assets/templates/.github/agents/coreex-expert.agent.md`

## Phase 2: Plan file operations

1. Build a source-to-target map.
2. For each target:
   - If missing: mark `create`.
   - If present and byte-equal: mark `skip-unchanged`.
   - If present and different:
     - `safe` mode: mark `skip-existing`.
     - `force` mode: mark `overwrite`.

## Phase 3: Apply

1. Create any missing directories.
2. Execute operations in this order:
   - `.github/copilot-instructions.md`
   - `.github/instructions/*.instructions.md` (sorted name order)
   - `.github/agents/coreex-expert.agent.md`
3. Create/update `.github/coreex-bootstrap.json`:

```json
{
  "bootstrapVersion": "1.1.0",
  "plugin": "coreex-agent-pack",
  "skill": "coreex-onboard",
  "mode": "safe|force",
  "updatedAtUtc": "<ISO-8601 UTC>"
}
```

## Phase 4: Validate

1. Verify all expected files exist after copy.
2. Verify marker file exists and is valid JSON.
3. Report:
   - created files
   - overwritten files
   - skipped files (unchanged or protected by safe mode)
4. If any copy fails, surface the specific path and error.
