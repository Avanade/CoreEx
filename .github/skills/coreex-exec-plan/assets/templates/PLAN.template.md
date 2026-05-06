# <Short, action-oriented description>

This ExecPlan is a living document. The sections `Progress`, `Surprises & Discoveries`, `Decision Log`, and `Outcomes & Retrospective` must be kept up to date as work proceeds.

This repository does not currently contain a checked-in `PLANS.md` file. If one is added later, this document must be updated to reference it and remain consistent with it.

## Purpose / Big Picture

Explain in a few sentences what someone gains after this change and how they can see it working. State the user-visible behavior that will exist after implementation, the exact scenario to exercise it, and why that outcome matters in this repository.

## Progress

Use a checklist with timestamps and keep it accurate at every stopping point. Split partially completed work into separate completed and remaining items instead of leaving status ambiguous.

- [ ] (YYYY-MM-DD HH:MMZ) Replace this with the first concrete step.
- [ ] (YYYY-MM-DD HH:MMZ) Replace this with the next concrete step.

## Surprises & Discoveries

Record unexpected behavior, missing assumptions, performance findings, or repo-specific constraints discovered while researching or implementing the work. Include short evidence snippets in indented form when useful.

- Observation: Replace with an unexpected finding.
   Evidence:
      <short command output, test result, or file excerpt>

## Decision Log

Record every meaningful design or scope decision in this format.

- Decision: Replace with the decision that was made.
   Rationale: Replace with why this path was chosen over alternatives.
   Date/Author: YYYY-MM-DD / <name>

## Outcomes & Retrospective

Summarize what was achieved at major milestones or at completion. Compare the result against the purpose of the plan, note what remains, and capture lessons that would help the next contributor resume from this file alone.

- Outcome: Replace with what now works.
   Evidence: Replace with the proof that it works.
   Remaining gap: Replace with any remaining work or say `None`.

## Context and Orientation

Assume the reader knows nothing about this repository. Describe the current state relevant to this task in plain language. Define any term of art immediately and explain how it appears here by naming the exact repository-relative files, projects, commands, or tests where the reader will encounter it. Name the owning projects and likely files to edit using full repository-relative paths.

## Plan of Work

Describe the sequence of edits and additions in prose. Keep it concrete and minimal. For each planned change, name the repository-relative file, the specific type, method, or module to inspect or edit, what will change, and what new observable behavior that edit enables. If milestones are useful, write them as short narrative subsections that explain the goal, the work, the result, and the proof.

## Concrete Steps

State the exact commands to run, the working directory for each command, and the short expected result. Use indented command examples instead of nested code fences. Update this section as work proceeds so a new contributor can continue from the current state.

Example format:

   Working directory: `c:\dev\CoreEx`
   Command: `dotnet test tests/Example.Project.Tests`
   Expected result: `Passed!` appears and the new test `Example_should_behave_as_expected` is listed.

## Validation and Acceptance

Describe how to prove the change works in behavior, not just compilation. Include the exact test commands, startup commands when applicable, the observable success criteria, and any before-and-after evidence a novice can compare. Phrase acceptance as something a human can verify directly.

## Idempotence and Recovery

Explain which steps are safe to repeat, how to recover from a partial failure, and any cleanup needed to return the workspace to a known-good state. If a step is risky or destructive, say so explicitly and provide a safer fallback.

## Artifacts and Notes

Include the most important concise transcripts, diffs, or snippets as indented examples. Keep only the evidence that proves success or clarifies a tricky step.

   Example test output:
      Passed!  - Failed:     0, Passed:    12, Skipped:     0, Total:    12

## Interfaces and Dependencies

Be prescriptive about the libraries, services, interfaces, types, and function signatures that must exist when the work is complete. Name the repository-relative files where they belong and explain why each dependency or interface is needed. If introducing new terminology, define it in plain language before using it.

## Revision Note

When revising this plan, add a short note here describing what changed, why it changed, and how the rest of the document was brought back into sync.