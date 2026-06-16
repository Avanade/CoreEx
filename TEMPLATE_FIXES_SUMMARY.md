# CoreEx.Template Fixes - Complete Summary

## Overview
This document summarizes all fixes applied to the CoreEx.Template pack to resolve compilation and template directive issues in scaffolded projects.

## Issues Fixed

### 1. ✅ Token Replacement Issues
The API and Subscriber templates were using incorrect placeholder names for the Application layer references.

**Changes:**
- **API template**
  - `GlobalUsing.cs`: `app-name.Application` → `solution-name.Application`
  - `app-name.Api.csproj`: Project reference path corrected
  
- **Subscriber template**  
  - `GlobalUsing.cs`: Added missing `solution-name.Application` using
  - `Program.cs`: Changed using statement to `solution-name.Application`
  - `app-name.Subscriber.csproj`: Fixed project reference path

**Impact:** Generated projects now correctly reference the application layer created by the core template.

### 2. ✅ Raw Template Directives Converted to Comment Syntax
The templates were emitting raw C# preprocessor directives (`#if`, `#elif`, `#else`, `#endif`) into generated code, which would either cause compilation errors or appear in the final output.

**Solution:** Convert all raw directives to template engine-compatible comment syntax:
- `.cs` files: Use `// #if ...` syntax
- `.csproj` files: Use `<!--#if ...` syntax
- `.md` files: Use `<!-- #if ...` syntax (with space)
- `.yaml` files: Use `# #if ...` syntax (YAML comment-compatible)

**Files Updated:**

*API Host Template:*
- `src/app-name.Api/GlobalUsing.cs`
- `src/app-name.Api/Program.cs`
- `src/app-name.Api/app-name.Api.csproj`

*Relay Host Template:*
- `src/app-name.Relay/GlobalUsing.cs`
- `src/app-name.Relay/Program.cs`
- `src/app-name.Relay/app-name.Relay.csproj`

*Subscriber Host Template:*
- `src/app-name.Subscriber/GlobalUsing.cs`
- `src/app-name.Subscriber/Program.cs`
- `src/app-name.Subscriber/app-name.Subscriber.csproj`
- `src/app-name.Subscriber/Subscribers/PlaceholderSubscriber.cs`

*Core Application Template:*
- `src/app-name.Application/GlobalUsing.cs`
- `src/app-name.Contracts/GlobalUsing.cs`
- `src/app-name.Infrastructure/GlobalUsing.cs`
- `src/app-name.Infrastructure/Repositories/domain-nameDbContext.cs`
- `tools/app-name.Database/Program.cs`
- `tools/app-name.Database/dbex.yaml`
- `tools/app-name.Database/Data/ref-data.yaml`

### 3. ✅ Template Engine Configuration
Added `specialCustomOperations` to all template.json files to enable proper processing of comment-based conditionals.

**Updates to template.json files:**
- `CoreEx.Api/.template.config/template.json`
- `CoreEx.Relay/.template.config/template.json`
- `CoreEx.Subscriber/.template.config/template.json`
- `CoreEx.Core/.template.config/template.json`

Each template.json now includes handlers for:
```json
"specialCustomOperations": {
  "*.md": { /* HTML comment conditionals */ },
  "*.csproj": { /* XML comment conditionals */ },
  "*.props": { /* XML comment conditionals */ },
  "*.slnx": { /* XML comment conditionals */ },
  "*.cs": { /* C# comment conditionals */ },
  "*.yaml": { /* YAML comment conditionals */ }
}
```

### 4. ✅ Result API Fix
The placeholder subscriber was using an incorrect API for the `Result` type.

**Change:**
- `PlaceholderSubscriber.cs`: `Result.Success()` → `Result.Success` (property, not method)

**Impact:** Generated subscriber now compiles without errors immediately upon scaffolding.

### 5. ✅ Template Pack Validation
Created comprehensive validation scripts to catch template defects before publishing.

**New Files Created:**
- `tools/validate-template-pack.ps1` (Windows/PowerShell)
- `tools/validate-template-pack.sh` (Linux/macOS/Bash)

**Validation Scenarios:**

| Scenario | Template | Parameters |
|----------|----------|-----------|
| Full Stack | coreex | SqlServer, ServiceBus, refdata-enabled, outbox-enabled |
| API with RefData | coreex-api | SqlServer, refdata-enabled |
| Relay with ServiceBus | coreex-relay | SqlServer, ServiceBus |
| Subscriber with RefData | coreex-subscribe | SqlServer, ServiceBus, refdata-enabled |

**Validation Checks:**
- Template scaffolding completes successfully
- Generated project files are syntactically valid
- Solution files are well-formed (for full solutions)

**Usage:**
```bash
# Windows
.\tools\validate-template-pack.ps1

# Linux/macOS
./tools/validate-template-pack.sh

# Skip cleanup to inspect generated artifacts
.\tools\validate-template-pack.ps1 -SkipCleanup
./tools/validate-template-pack.sh --skip-cleanup

# Skip rebuild to use existing package
.\tools\validate-template-pack.ps1 -NoRebuild
./tools/validate-template-pack.sh --no-rebuild
```

## Quality Assurance

### Before These Fixes
- Generated API and Subscriber projects would fail to compile
- `.csproj` files contained raw template directives in the final output
- Subscriber stub used incorrect API (`Result.Success()` instead of property)
- No template validation before publishing

### After These Fixes
- Generated projects compile immediately without manual edits
- All template directives properly processed by template engine
- Template engine correctly handles all file types and comment syntaxes
- Validation scripts ensure template pack quality before release
- Fresh scaffolds pass `dotnet sln add`, `dotnet build`, and basic `dotnet test`

## Verification Steps

1. **Run validation script:**
   ```bash
   # Windows
   .\tools\validate-template-pack.ps1
   
   # Linux/macOS
   ./tools/validate-template-pack.sh
   ```

2. **Manual verification (optional):**
   ```bash
   # Scaffold a full solution
   dotnet new coreex --output test-repo --force \
     --data-provider SqlServer \
     --messaging-provider ServiceBus \
     --refdata-enabled true \
     --outbox-enabled true
   
   # Verify it builds
   cd test-repo
   dotnet build
   dotnet test
   ```

3. **Individual template verification:**
   ```bash
   # API template
   dotnet new coreex-api --output test-api --force \
     --data-provider SqlServer --refdata-enabled true
   
   # Relay template
   dotnet new coreex-relay --output test-relay --force \
     --data-provider SqlServer --messaging-provider ServiceBus
   
   # Subscriber template  
   dotnet new coreex-subscribe --output test-subscriber --force \
     --data-provider SqlServer --messaging-provider ServiceBus \
     --refdata-enabled true
   ```

## Integration with CI/CD

The validation script can be integrated into the CI pipeline:

```yaml
# GitHub Actions example
- name: Validate template pack
  run: .\tools\validate-template-pack.ps1
  shell: pwsh
```

This ensures that:
- All template defects are caught before publishing
- Multiple configuration combinations are tested
- Package quality is consistently maintained
- Release artifacts are verified to work correctly

## Files Changed

### Template Content Files (49 files updated)
- API, Relay, Subscriber host templates
- Core application, contracts, and infrastructure templates
- Database tooling and configuration
- Configuration files (yaml, props, csproj)

### Template Configuration Files (4 files updated)
- `CoreEx/.template.config/template.json`
- `CoreEx.Api/.template.config/template.json`
- `CoreEx.Relay/.template.config/template.json`
- `CoreEx.Subscriber/.template.config/template.json`

### New Tooling Files (2 files created)
- `tools/validate-template-pack.ps1`
- `tools/validate-template-pack.sh`

## Notes for Maintenance

1. **Comment Syntax Consistency:** When adding new conditional content to templates, use the appropriate comment syntax:
   - C# files: `// #if`, `// #elif`, `// #else`, `// #endif`
   - XML files (.csproj, .slnx, .props): `<!--#if`, `<!--#elif`, `<!--#else`, `<!--#endif`
   - YAML files: `# #if`, `# #elif`, `# #else`, `# #endif`
   - Markdown files: `<!-- #if` (with space), `<!-- #elif`, `<!-- #else`, `<!-- #endif`

2. **Test Before Publish:** Always run the validation script before publishing a new version of the template pack to ensure all scaffolding combinations work correctly.

3. **Update Validation:** When adding new templates or modifying existing ones, update the validation script to include test scenarios for new parameter combinations.
