# CoreEx Onboard Checklists

## Readiness checklist

- [ ] Template files are present under `assets/templates/.github/`.
- [ ] Repository root is writable.
- [ ] User selected mode: `safe` (default) or `force`.

## Completion checklist

- [ ] `.github/copilot-instructions.md` exists.
- [ ] `.github/instructions/` contains all CoreEx `*.instructions.md` files from templates.
- [ ] `.github/agents/coreex-expert.agent.md` exists.
- [ ] `.github/coreex-bootstrap.json` exists and is valid JSON.
- [ ] Result summary includes created/overwritten/skipped file counts.

## Safety checklist

- [ ] No overwrite in safe mode.
- [ ] Overwrites in force mode are explicitly requested.
- [ ] Missing source templates fail fast with a clear message.
