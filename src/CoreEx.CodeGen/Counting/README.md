# CoreEx.CodeGen.Counting

> Provides file and line counting utilities that report the proportion of generated vs. hand-authored code across the solution output directories.

## Overview

`CoreEx.CodeGen.Counting` implements the `Count` command exposed by `CodeGenConsole`. When invoked, `CodeGenCounter` walks the output directories that the code-generation run would target, classifies each supported file as generated (matching the `.g` + extension suffix) or hand-authored, and accumulates per-directory totals into a `DirectoryCountStatistics` tree. The tree is then rendered as a formatted table — columns for total files, total lines, generated file count with percentage, and generated line count with percentage — indented to reflect directory hierarchy. `CodeGenDirectoryWalker` shares the same solution root and directory exclusions with the `Clean` command.

This is a diagnostic aid for understanding the relative investment in generated vs. bespoke code across a CoreEx solution.

## Key capabilities

- 🔍 **Generated-file detection**: classifies supported files by the `.g.<extension>` suffix convention, matching the output patterns of CoreEx code generation.
- 📊 **Hierarchical statistics**: `DirectoryCountStatistics` accumulates counts at each directory level and aggregates totals recursively across children for roll-up reporting.
- 🖨️ **Formatted table output**: `DirectoryCountStatistics.Write` renders a multi-column table (all files, all lines, generated files %, generated lines %) to the `ILogger`, with directory hierarchy shown through indentation.

## Key types

| Type | Description |
|------|-------------|
| **[`CodeGenCounter`](./CodeGenCounter.cs)** | Walks the solution output directories, classifies files, and builds the `DirectoryCountStatistics` tree; drives the `Count` command in `CodeGenConsole`. |
| **[`CodeGenDirectoryWalker`](../CodeGenDirectoryWalker.cs)** | Resolves the solution directory and recursively enumerates files, excluding `bin`, `obj`, and hidden directories. |
| **[`DirectoryCountStatistics`](./DirectoryCountStatistics.cs)** | Holds file and line counts (total and generated) for one directory and its children; supports recursive totals and formatted log-table rendering via `Write`. |

## Related namespaces

- **[`CoreEx.CodeGen`](../README.md)** - Root package; the `Count` value of `CommandType` activates this pipeline via `CodeGenConsole`.