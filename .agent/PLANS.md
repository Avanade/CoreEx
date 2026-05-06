# ExecPlans Index

This file maintains an index of all approved and active ExecPlans for the CoreEx repository. Each plan is a self-contained implementation guide that should be reviewed and approved before work begins.

## Active Plans

### [multi-tenancy-orders.md](execplans/multi-tenancy-orders.md)

**Title:** Add header-driven multi-tenancy to Contoso.Orders using tenant-specific SQL schemas

**Purpose:** Enable the Orders sample to isolate data by tenant using separate SQL schemas (`tenanta`, `tenantb`, etc.) while flowing tenant identity from HTTP headers through `ExecutionContext`.

**Status:** Approved and ready for implementation

**Key Decisions:**
- Tenant identifier flows via `X-Tenant-Id` HTTP header
- Cross-tenant access returns `404 Not Found` (not visible as authorization failure)
- Aggregate tables are tenant-scoped; reference data remains shared in `Orders` schema
- Workflow contracts updated to carry `TenantId`

**Scope:** CRUD operations, order workflow execution, database seed data

**Created:** 2026-05-06

---

## Completed Plans

(None yet)

---

## How to Use This Index

1. **Adding a new plan:** Update this file when `/coreex.plan` generates a new ExecPlan. Add a brief entry with title, purpose, status, and key decisions.

2. **Tracking status:** Keep the `Status` field current (e.g., "In progress", "Blocked", "Completed", "Archived").

3. **Archival:** Move completed plans to the "Completed Plans" section with a completion date and brief outcome summary.

4. **Discoverability:** This index is the canonical reference for all plans in the repository. Link to it from README or team wikis as needed.
