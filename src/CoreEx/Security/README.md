# CoreEx.Security

> Provides `AuthenticationUser` — the standardized representation of the authenticated caller — and `AuthenticationType` enum, both surfaced via `ExecutionContext.User` and consumed by change-log stamping and authorization checks.

## Overview

`CoreEx.Security` defines the minimal security identity contract used across the CoreEx runtime. `AuthenticationUser` is a record type that carries the typed identity of the caller (`Id`, `UserName`, `Type`), and is intentionally designed to be extended by consuming applications to add claims, roles, or other authorization context.

Static sentinel instances (`Unknown`, `Anonymous`, `EnvironmentUser`, `System`) cover the common non-user scenarios; the ambient user is accessed through `ExecutionContext.Current.User`, which resolves to `AuthenticationUser.UserName` (or `Id` as fallback). Change-log stamping (`ChangeLog.SetCreatedBy`, `SetUpdatedBy`) uses this value automatically.

## Key capabilities

- 👤 **Standard user identity**: `AuthenticationUser` carries `Id` (string), `UserName`, and `AuthenticationType`; designed for extension to add claims or role membership.
- 🏷️ **Sentinel users**: Static `Unknown`, `Anonymous`, `EnvironmentUser`, and `System` instances cover unauthenticated, environment-process, and internal system callers; all are replaceable via their static setters.
- 🔢 **Authentication type enum**: `AuthenticationType` distinguishes `Unknown`, `Unauthenticated`, `AccountUser`, `ServiceAccount`, `System`, and `Certificate`, enabling authorization logic to branch on authentication mechanism.
- 🔗 **ExecutionContext integration**: `ExecutionContext.UserName` resolves to the current `AuthenticationUser.UserName` and is the value stamped into `ChangeLog.CreatedBy` / `UpdatedBy`.

## Key types

| Type | Description |
|------|-------------|
| **[`AuthenticationUser`](./AuthenticationUser.cs)** | Record carrying the authenticated caller: `Id`, `UserName`, `Type` (`AuthenticationType`); static sentinels `Unknown`, `Anonymous`, `EnvironmentUser`, `System`. |
| **[`AuthenticationType`](./AuthenticationType.cs)** | Enum: `Unknown`, `Unauthenticated`, `AccountUser`, `ServiceAccount`, `System`, `Certificate`. |

## Related Namespaces

- **[`CoreEx`](../README.md)** - `ExecutionContext` holds the current `AuthenticationUser` and exposes `UserName`; `ChangeLog` consumes `UserName` for audit stamping.