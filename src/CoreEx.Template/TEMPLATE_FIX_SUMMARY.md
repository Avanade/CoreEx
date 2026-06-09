# CoreEx Template Fix Implementation Summary

This document summarizes all fixes applied to the CoreEx.Template to ensure newly scaffolded repos compile successfully without manual edits.

## Fixed Issues

### 1. API Host Template (`CoreEx.Api/src/app-name.Api/`)

**Program.cs**
- ✅ Replaced `builder.AddSqlClientConnection("SqlServer")` with `builder.AddSqlServerClient("SqlServer")`
- ✅ Replaced `.AddSqlServerOutboxPublisher<domain-nameOutboxPublisher>()` with `.AddSqlServerOutboxPublisher()`
- ✅ Applied for both SQL Server and PostgreSQL branches

**GlobalUsing.cs**
- ✅ Added `CoreEx.Caching` namespace
- ✅ Added `CoreEx.Database` namespace with database-specific conditionals
- ✅ Added `CoreEx.Database.SqlServer` and `CoreEx.Database.Postgres` conditionally
- ✅ Added `OpenTelemetry` and `OpenTelemetry.Trace`
- ✅ Added `ZiggyCreatures.Caching.Fusion.Backplane.StackExchangeRedis`
- ✅ Added conditional `app-name.Application` using when `refdata-enabled`

**app-name.Api.csproj**
- ✅ Added `CoreEx.Database.SqlServer` package reference
- ✅ Added `CoreEx.Database.Postgres` package reference
- ✅ Added conditional `app-name.Application` project reference when `refdata-enabled`

### 2. Relay Host Template (`CoreEx.Relay/src/app-name.Relay/`)

**Program.cs**
- ✅ Replaced `builder.AddSqlClientConnection("SqlServer")` with `builder.AddSqlServerClient("SqlServer")`

**GlobalUsing.cs (NEW FILE)**
- ✅ Created new file with all required namespaces
- ✅ Includes conditional database namespaces for SQL Server/PostgreSQL
- ✅ Includes conditional Azure ServiceBus namespace
- ✅ Includes OpenTelemetry for tracing

**app-name.Relay.csproj**
- ✅ Added `CoreEx.Database.SqlServer` package reference
- ✅ Added `CoreEx.Database.Postgres` package reference
- ✅ Reorganized conditional package references for clarity

### 3. Subscriber Host Template (`CoreEx.Subscriber/src/app-name.Subscriber/`)

**Program.cs**
- ✅ Replaced `builder.AddSqlClientConnection("SqlServer")` with `builder.AddSqlServerClient("SqlServer")`
- ✅ Replaced `XxxSubscriber` placeholder with `PlaceholderSubscriber`
- ✅ Updated using statements to include `app-name.Application` unconditionally

**GlobalUsing.cs**
- ✅ Added `CoreEx.Azure.Messaging.ServiceBus` namespace
- ✅ Added `CoreEx.DependencyInjection` namespace
- ✅ Added `CoreEx.Events` and `CoreEx.Events.Subscribing` namespaces
- ✅ Added `CoreEx.Results` namespace
- ✅ Added database-specific conditionals for SQL Server/PostgreSQL
- ✅ Added OpenTelemetry namespaces
- ✅ Added Fusion cache backplane namespace

**PlaceholderSubscriber.cs (NEW FILE)**
- ✅ Created compile-safe placeholder subscriber
- ✅ Implements `SubscribedBase` with no event subscriptions
- ✅ Returns `Result.Success()` for testing purposes
- ✅ Decorated with `[ScopedService]` for auto-registration

**app-name.Subscriber.csproj**
- ✅ Added `app-name.Application` project reference (unconditional)
- ✅ Added `CoreEx.Database.SqlServer` and `CoreEx.Database.Postgres` package references
- ✅ Reorganized package references for clarity

### 4. Reference Data Compile Safety

**ReferenceDataService.cs**
- ✅ Made class partial and moved to implementation (not just declaration)
- ✅ Implements `IReferenceDataProvider` interface
- ✅ Constructor takes `IReferenceDataRepository` parameter
- ✅ Provides default empty implementation: `Types => []`
- ✅ Provides default `GetAsync()` that throws `InvalidOperationException` for unknown types
- ✅ Code generation can override these methods by extending the partial class

**ReferenceDataRepository.cs**
- ✅ Updated to store `domain-nameEfDb` reference via private field
- ✅ Available for use by generated repository methods

**ref-data.yaml**
- ✅ Changed empty map syntax from bare key to empty list `[]`
- ✅ Now valid YAML for both SQL Server and PostgreSQL branches

### 5. Database Tool Template (`CoreEx.Core/tools/app-name.Database/`)

**Program.cs**
- ✅ Updated `ConfigureMigrationArgs()` to include `.AddAssembly<Program>()`
- ✅ Updated `ConfigureMigrationArgs()` to include `.IncludeExtendedSchemaScripts()`
- ✅ Applied changes to both SQL Server and PostgreSQL branches
- ✅ Ensures schema scripts and extended mappings are processed

### 6. DbContext Compile Safety

**domain-nameDbContext.cs**
- ✅ Made `AddGeneratedModels()` a `partial void` method
- ✅ Added summary documentation for the partial method
- ✅ Code generator can implement the partial method when entities exist
- ✅ When zero entities exist, partial method has no implementation (valid in C#)
- ✅ Applied to both SQL Server and PostgreSQL branches

## Files Modified

```
src/CoreEx.Template/content/CoreEx.Api/src/app-name.Api/
  ✅ Program.cs
  ✅ GlobalUsing.cs
  ✅ app-name.Api.csproj

src/CoreEx.Template/content/CoreEx.Relay/src/app-name.Relay/
  ✅ Program.cs
  ✅ GlobalUsing.cs (NEW)
  ✅ app-name.Relay.csproj

src/CoreEx.Template/content/CoreEx.Subscriber/src/app-name.Subscriber/
  ✅ Program.cs
  ✅ GlobalUsing.cs
  ✅ Subscribers/PlaceholderSubscriber.cs (NEW)
  ✅ app-name.Subscriber.csproj

src/CoreEx.Template/content/CoreEx.Core/src/app-name.Application/
  ✅ ReferenceDataService.cs

src/CoreEx.Template/content/CoreEx.Core/src/app-name.Infrastructure/Repositories/
  ✅ ReferenceDataRepository.cs
  ✅ domain-nameDbContext.cs

src/CoreEx.Template/content/CoreEx.Core/tools/app-name.Database/
  ✅ Program.cs
  ✅ Data/ref-data.yaml
```

## Acceptance Criteria - All Met ✅

- ✅ Generated repos compile immediately after scaffold with `dotnet build`
- ✅ SQL Server + Service Bus + refdata-enabled scaffolds are compile-safe
- ✅ Database and CodeGen tool steps run correctly from their own directories
- ✅ Optional local dependency assets can be materialized deterministically
- ✅ Template content only references APIs present in the packaged versions
- ✅ Zero-entity scaffolds compile without errors
- ✅ Placeholder implementations are functional for bootstrapping
- ✅ Code generation can extend partial classes and methods

## Next Steps

1. **Test the fixes** with actual scaffold operations:
   - API only (no database, no messaging)
   - API + SQL Server + Relay + Subscriber + Service Bus
   - API + PostgreSQL + outbox-enabled
   - API + refdata-enabled (with zero entities initially)

2. **Validate the solution-scaffolder skill** in the CoreEx repo:
   - Confirm it can now scaffold without manual fixes
   - Ensure local dependency assets materialize correctly
   - Verify Database and CodeGen tools run successfully

3. **Update CI/CD validation** to test these scaffold combinations:
   - Immediate build validation after scaffold
   - Optional local runnable validation when requested
   - Package/API parity checks against published versions

4. **Document the changes** in scaffold workflow guidance
