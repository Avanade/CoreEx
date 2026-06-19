# Orchestration with the Durable Task SDK

This document explains when and how to incorporate workflow orchestration into a CoreEx-based application landscape. It draws on the `Contoso.Order.Workflow.*` sample, which demonstrates an order validation and submission workflow backed by Durable Task Scheduler (DTS).

## Table of Contents

- [When to Use Orchestration](#when-to-use-orchestration)
- [Durable Task SDK with DTS vs Durable Functions](#durable-task-sdk-with-dts-vs-durable-functions)
- [Long-Running Workflows](#long-running-workflows)
- [Business-Critical Orchestration](#business-critical-orchestration)
- [Compensation and Retries](#compensation-and-retries)
- [Deterministic Execution](#deterministic-execution)
- [Fan-Out / Fan-In](#fan-out--fan-in)
- [Batch Processing](#batch-processing)
- [External Events and Human Approval](#external-events-and-human-approval)
- [Auditability and Replay](#auditability-and-replay)
- [DTS Dashboard for Observability and Management](#dts-dashboard-for-observability-and-management)
- [Project Layout](#project-layout)
- [Worker Host Setup](#worker-host-setup)
- [Client Registration](#client-registration)
- [Running Locally](#running-locally)

---

## When to Use Orchestration

Standard request/response services, backed by application services, repositories, and outbox-relay messaging, cover the majority of business operations. Workflow orchestration solves a different class of problems where those patterns alone are insufficient.

| Scenario | Request/response + outbox sufficient? | Orchestration adds value? |
|---|---|---|
| CRUD operations with side-effect events | Yes | No |
| Simple pub/sub fan-out (fire-and-forget, no aggregated result) | Yes | No |
| Fan-out with aggregated result or partial-failure handling | No | Yes |
| Batch processing of a variable-size work list | No | Yes |
| Throttled parallel work with a concurrency cap | No | Yes |
| External-event wait (human approval, webhook callback) | No | Yes |
| Multi-step process spanning seconds to days | No | Yes |
| Process requiring compensation on failure | No | Yes |
| Steps that must run in strict order with branching | No | Yes |
| Audit trail of every execution step required | No | Yes |
| Step must retry independently from the whole workflow | No | Yes |

Choose orchestration when at least one of those characteristics is central to the business process.

---

## Durable Task SDK with DTS vs Durable Functions

The Durable Task SDK and Durable Functions share the same core orchestration concepts: orchestrators, activities, deterministic replay, durable timers, and external events. Both can use Durable Task Scheduler (DTS) as the durable backend. The primary difference is runtime hosting preference rather than orchestration semantics.

### What is different

Durable Functions is a Functions-hosted programming model. It is designed around Azure Functions triggers, bindings, and the Functions runtime lifecycle. That model is productive when the application is already centered on Functions and event-triggered serverless hosting.

The Durable Task SDK with DTS is a general-purpose .NET library and backend combination. Instead of writing against the Azure Functions host, you write orchestrators and activities and then host them inside any .NET process that can register a worker and a client. In this repository, the workflow is hosted in a normal ASP.NET Core process:

```csharp
builder.Services.AddDurableTaskWorker()
    .AddTasks(registry =>
    {
        registry.AddOrchestrator<OrderWorkflowOrchestration>();
        registry.AddActivity<ValidateOrderActivity>();
        registry.AddActivity<SubmitOrderActivity>();
    })
    .UseDurableTaskScheduler(connectionString);
```

And any host can schedule or query workflow instances through a normal client registration:

```csharp
services.AddDurableTaskClient(durableTaskBuilder =>
{
    durableTaskBuilder.UseDurableTaskScheduler(connectionString);
});
```

### Why that matters in practice

| Concern | Durable Functions | Durable Task SDK with DTS |
|---|---|---|
| Primary hosting model | Azure Functions runtime | Any .NET host |
| Programming surface | Triggers and bindings | Explicit worker and client APIs |
| DTS backend support | Yes | Yes |
| Best fit | Serverless Functions applications | Existing APIs, workers, services, and containerized apps |
| Runtime dependency | Functions host | No Functions host required |
| Local backend story | Can run locally with emulator tooling | DTS emulator can be started directly as infrastructure |
| Container hosting | Possible, but still centered on the Functions runtime model | Natural fit for regular ASP.NET Core or worker containers |

The difference is slight at the orchestration-code level, but important at the application-hosting level. If the goal is to use durable workflows inside an existing service landscape rather than build a Functions application, the Durable Task SDK with DTS is often the more direct fit. If the solution is already Functions-centric, Durable Functions with DTS can provide the same durable backend while preserving Functions triggers and bindings.

### Local emulator benefit

One of the practical advantages of DTS is that the backend can be run locally as infrastructure, without switching the application into a Functions-hosting model. This repository already includes the emulator in [docker-compose.yml](../docker-compose.yml):

```yaml
dts-emulator:
  image: mcr.microsoft.com/dts/dts-emulator:latest
  environment:
    DTS_TASK_HUB_NAMES: "default,order"
  ports:
    - "8080:8080"
    - "8082:8082"
```

That means developers can run the orchestration backend locally, start the workflow worker, and test orchestration behavior end-to-end without needing Azure-hosted infrastructure. In the sample worker, the connection logic explicitly detects the local emulator and switches authentication to `None`:

```csharp
var isLocalEmulator = hostAddress.StartsWith("http://localhost:8080", StringComparison.OrdinalIgnoreCase);

var connectionString = isLocalEmulator
    ? $"Endpoint={hostAddress};TaskHub={taskHubName};Authentication=None"
    : $"Endpoint={hostAddress};TaskHub={taskHubName};Authentication=DefaultAzure";
```

This gives a clean local-development loop:

1. Start the DTS emulator with container infrastructure.
2. Run the worker host locally.
3. Run an API, console app, or test that schedules orchestration instances.
4. Observe orchestration status and traces without changing the application architecture.

### Container-hosting benefit

Because the Durable Task SDK is hosted inside ordinary .NET applications, it fits naturally into containerized environments. A workflow worker can be packaged exactly like any other ASP.NET Core or background-service container, and the DTS backend can run either as the local emulator or as a managed service.

That is useful when the broader application landscape already uses:

- Containerized APIs and background workers.
- Kubernetes, Container Apps, or Docker Compose for local and deployed environments.
- Shared OpenTelemetry, health checks, and common ASP.NET Core hosting patterns.

In this sample, the worker is just another host process with logging, OpenTelemetry, and health checks, not a special Functions runtime host. That reduces the amount of platform-specific infrastructure needed when orchestration is only one capability inside a larger service estate.

### Guidance

Prefer Durable Functions when the solution is intentionally Functions-centric and benefits from trigger-and-binding composition.

Prefer the Durable Task SDK with DTS when:

- The application is already an API, worker, or service-host landscape.
- You want orchestration without adopting the Azure Functions runtime.
- You want to run the backend locally through the DTS emulator.
- You want workflow workers to be packaged and deployed like ordinary containers.

---

## Long-Running Workflows

Durable orchestrations persist their state between steps. A workflow can be suspended while waiting for an external event, a timer, or a slow downstream system, and then resume without holding a thread or blocking an HTTP request.

**Apply this when:**
- A business process spans minutes, hours, or days, for example order fulfilment, approval chains, or scheduled reminders.
- Steps involve human interaction, third-party callbacks, or polling.
- The initiating HTTP request cannot or should not block until the process finishes.

**Pattern in the sample:**

The `OrderWorkflowOrchestration` is initiated by a client call that returns an instance ID immediately. The caller can poll for status later using `GetMetadataAsync`:

```csharp
var instanceId = await _orderWorkflowClient.StartAsync(request, cancellationToken: ct);
var metadata = await _orderWorkflowClient.GetMetadataAsync(instanceId);
```

The orchestration itself runs as a durable sequence of activity calls, each persisted between steps:

```csharp
public override async Task<OrderWorkflowResult> RunAsync(
    TaskOrchestrationContext context, OrderWorkflowRequest input)
{
    var validation = await context.CallActivityAsync<bool>(
        nameof(ValidateOrderActivity),
        new ValidateOrderActivityInput(input.OrderId, input.Amount, input.Currency));

    if (!validation)
    {
        return new OrderWorkflowResult(
            input.OrderId,
            false,
            "Order request failed validation.",
            context.CurrentUtcDateTime);
    }

    return await context.CallActivityAsync<OrderWorkflowResult>(
        nameof(SubmitOrderActivity),
        new SubmitOrderActivityInput(input.OrderId, input.Amount, input.Currency, input.RequestedBy));
}
```

Each `CallActivityAsync` checkpoint is recorded. If the worker process restarts between steps, the orchestration replays only what is needed to reach the last durable checkpoint.

---

## Business-Critical Orchestration

Orchestration guarantees that every step is recorded and that the overall process will eventually reach a terminal state, even across process restarts or transient infrastructure failures.

**Apply this when:**
- Partial execution of a process would leave the system in an inconsistent or unacceptable state.
- A process coordinates writes across multiple services or systems that do not share a transaction boundary.
- Regulatory or commercial requirements demand that every step and outcome is traceable.

**Guidance:**
- Model each external call or side-effecting operation as a discrete `TaskActivity`.
- Keep orchestrator code free of direct I/O.
- Keep contracts serializable and explicit.
- Name activities and orchestrations clearly because those names become part of operations and diagnostics.

**Sample activity pattern:**

```csharp
[DurableTask]
public sealed class SubmitOrderActivity : TaskActivity<SubmitOrderActivityInput, OrderWorkflowResult>
{
    public override Task<OrderWorkflowResult> RunAsync(
        TaskActivityContext context,
        SubmitOrderActivityInput input)
    {
        var message = $"Order '{input.OrderId}' accepted for {input.Amount:0.00} {input.Currency}.";
        var result = new OrderWorkflowResult(input.OrderId, true, message, DateTimeOffset.UtcNow);
        return Task.FromResult(result);
    }
}
```

Activities receive typed input records and return typed result records. Keep those contracts as plain records so they serialize cleanly across durable boundaries.

---

## Compensation and Retries

When a step in a multi-step workflow fails, the process may need to undo work already performed by earlier steps. That is the classic compensation or saga pattern.

**Apply this when:**
- Earlier steps have already committed side effects, for example inventory reservation or payment authorization.
- There is no distributed rollback mechanism.
- The compensating action itself must be durable and observable.

**Pattern:**

```csharp
public override async Task<OrderWorkflowResult> RunAsync(
    TaskOrchestrationContext context, OrderWorkflowRequest input)
{
    var reservationId = await context.CallActivityAsync<string>(
        nameof(ReserveInventoryActivity), input);

    try
    {
        return await context.CallActivityAsync<OrderWorkflowResult>(
            nameof(ChargePaymentActivity), input);
    }
    catch (TaskFailedException)
    {
        await context.CallActivityAsync(
            nameof(ReleaseInventoryActivity), reservationId);

        return new OrderWorkflowResult(
            input.OrderId,
            false,
            "Payment failed; reservation released.",
            context.CurrentUtcDateTime);
    }
}
```

**Retry policies:**

Configure retries on the activity call rather than burying retry logic inside the activity:

```csharp
var retryOptions = new TaskOptions(new RetryPolicy(
    maxNumberOfAttempts: 3,
    firstRetryInterval: TimeSpan.FromSeconds(5),
    backoffCoefficient: 2.0));

await context.CallActivityAsync<OrderWorkflowResult>(
    nameof(SubmitOrderActivity),
    input,
    retryOptions);
```

Retries replay only the failing step. Previously completed steps are not re-executed.

---

## Deterministic Execution

Orchestrators are replayed whenever the worker resumes. Every line of orchestrator code may run multiple times during replay. Non-deterministic logic inside the orchestrator will corrupt the execution history.

**Rules:**
- Do not read `DateTime.UtcNow` or `DateTimeOffset.UtcNow` directly inside an orchestrator. Use `context.CurrentUtcDateTime`.
- Do not generate random values or GUIDs inside an orchestrator.
- Do not perform I/O inside an orchestrator.
- Do not use `Task.Delay`; use `context.CreateTimer`.
- Do not read environment variables or configuration directly inside the orchestrator.

**Correct:**

```csharp
var processedAt = context.CurrentUtcDateTime;
```

The sample follows that rule by keeping business work inside activities and using `context.CurrentUtcDateTime` for durable timestamps.

---

## Fan-Out / Fan-In

Fan-out/fan-in describes a pattern where one orchestrator dispatches many parallel work items, waits for them all to complete, and then aggregates the results.

**Apply this when:**
- One business action must trigger work against a dynamic list of targets.
- The caller needs an aggregated result before proceeding.
- Individual branches may fail and need independent retries.
- The work list is not fixed at design time.

**Pattern:**

```csharp
[DurableTask]
public sealed class NotifyRecipientsOrchestration
    : TaskOrchestrator<NotifyRecipientsRequest, NotifyRecipientsResult>
{
    public override async Task<NotifyRecipientsResult> RunAsync(
        TaskOrchestrationContext context, NotifyRecipientsRequest input)
    {
        var tasks = input.RecipientIds.Select(id =>
            context.CallActivityAsync<NotificationOutcome>(
                nameof(SendNotificationActivity),
                new SendNotificationInput(id, input.MessageTemplate)));

        var outcomes = await Task.WhenAll(tasks);

        var failed = outcomes.Where(x => !x.Delivered).Select(x => x.RecipientId).ToList();
        return new NotifyRecipientsResult(outcomes.Length, outcomes.Count(x => x.Delivered), failed);
    }
}
```

`Task.WhenAll` is safe here because the inner tasks are durable activity calls, not raw background work.

---

## Batch Processing

Batch processing involves iterating over a variable-size list of work items and executing durable work for each item. Orchestration adds per-item checkpointing, independent retries, and controllable parallelism.

**Apply this when:**
- A scheduled job, import file, or upstream event delivers a list of items.
- The batch must survive worker restarts.
- Some items may fail without invalidating the whole batch.
- Throughput must be capped to protect downstream systems.

**Pattern:**

```csharp
[DurableTask]
public sealed class OrderBatchOrchestration
    : TaskOrchestrator<OrderBatchRequest, OrderBatchResult>
{
    private const int MaxConcurrency = 10;

    public override async Task<OrderBatchResult> RunAsync(
        TaskOrchestrationContext context, OrderBatchRequest input)
    {
        var results = new List<OrderWorkflowResult>();
        var queue = new Queue<string>(input.OrderIds);

        while (queue.Count > 0)
        {
            var window = Enumerable.Range(0, Math.Min(MaxConcurrency, queue.Count))
                .Select(_ => queue.Dequeue())
                .ToList();

            var tasks = window.Select(orderId =>
                context.CallActivityAsync<OrderWorkflowResult>(
                    nameof(ProcessSingleOrderActivity),
                    new ProcessSingleOrderInput(orderId)));

            results.AddRange(await Task.WhenAll(tasks));
        }

        return new OrderBatchResult(
            results.Count,
            results.Count(x => x.Accepted),
            results.Count(x => !x.Accepted));
    }
}
```

For very large batches, use sub-orchestrations to shard the work and keep orchestration histories compact.

---

## External Events and Human Approval

An orchestration can pause and wait for an event raised by an external system or a human actor, then resume with the event payload.

**Apply this when:**
- A step requires human approval.
- A third-party system responds asynchronously.
- A timeout should trigger a compensating or fallback path.

**Pattern:**

```csharp
[DurableTask]
public sealed class OrderApprovalOrchestration
    : TaskOrchestrator<OrderWorkflowRequest, OrderWorkflowResult>
{
    public override async Task<OrderWorkflowResult> RunAsync(
        TaskOrchestrationContext context, OrderWorkflowRequest input)
    {
        await context.CallActivityAsync(nameof(NotifyApproverActivity), input);

        using var timeoutCts = new CancellationTokenSource();
        var approvalTask = context.WaitForExternalEvent<bool>("ApprovalDecision", timeoutCts.Token);
        var timeoutTask = context.CreateTimer(context.CurrentUtcDateTime.AddHours(48), timeoutCts.Token);

        var winner = await Task.WhenAny(approvalTask, timeoutTask);
        timeoutCts.Cancel();

        if (winner == timeoutTask || !approvalTask.Result)
        {
            await context.CallActivityAsync(nameof(CancelOrderActivity), input.OrderId);
            return new OrderWorkflowResult(
                input.OrderId,
                false,
                "Approval not received within deadline.",
                context.CurrentUtcDateTime);
        }

        return await context.CallActivityAsync<OrderWorkflowResult>(
            nameof(SubmitOrderActivity),
            new SubmitOrderActivityInput(input.OrderId, input.Amount, input.Currency, input.RequestedBy));
    }
}
```

An external caller raises the event with `DurableTaskClient.RaiseEventAsync`.

---

## Auditability and Replay

The Durable Task runtime stores execution history for every orchestration instance: inputs, outputs, activity timing, and status transitions. That gives a built-in audit trail and a basis for replay-aware diagnostics.

**Querying instance status:**

```csharp
var metadata = await _orderWorkflowClient.GetMetadataAsync(instanceId, getInputsAndOutputs: true);
```

`OrchestrationMetadata` provides runtime status, timestamps, inputs, outputs, and failure details.

Use a caller-supplied or business-key-derived instance ID when idempotent scheduling matters. That ties the durable history directly to the business entity and prevents duplicate scheduling.

---

## DTS Dashboard for Observability and Management

The DTS dashboard provides a unified operational view of orchestrations, activities, and entities. It is useful for both day-to-day observability and active management actions.

### What it provides

- Instance-level visibility: runtime status, duration, input and output payloads, and failure details.
- Execution flow insight: orchestration timelines including fan-out and fan-in activity branches.
- Operational controls: pause, terminate, and restart operations for orchestration instances.
- Query and filtering: locate instances by status, age, name, or identifier patterns.
- Troubleshooting support: correlate orchestration history with application logs and traces.

### Why it matters locally

The same dashboard experience is available when using the local emulator. That means developers can test workflows and inspect execution behavior on their workstation without deploying to Azure.

In this repository, the emulator dashboard is exposed on `http://localhost:8082`.

Typical local workflow:

1. Start the emulator and run the worker host.
2. Schedule or trigger orchestration instances.
3. Open `http://localhost:8082` and select the task hub.
4. Inspect timelines, activity outcomes, and instance metadata to verify behavior.
5. Use management actions as needed during testing and debugging.

### Relationship to OpenTelemetry

The dashboard and OpenTelemetry traces are complementary:

- DTS dashboard is orchestration-centric and state-history centric.
- OpenTelemetry is distributed-call centric across services and infrastructure.

Using both gives complete coverage: orchestration state transitions plus end-to-end dependency traces.

---

## Project Layout

The sample separates concerns across three projects:

| Project | Responsibility |
|---|---|
| `Contoso.Order.Workflow.Workflow` | Orchestrations, activities, and workflow contracts. |
| `Contoso.Order.Workflow.Worker` | Worker host that registers orchestration code with DTS. |
| `Contoso.Order.Workflow.Client` | Client library used by APIs or other callers to start and query workflows. |

This keeps workflow logic portable and independent of the specific host process.

---

## Worker Host Setup

The worker host wires the DTS connection, registers orchestrators and activities, and configures telemetry:

```csharp
builder.Services.AddDurableTaskWorker()
    .AddTasks(registry =>
    {
        registry.AddOrchestrator<OrderWorkflowOrchestration>();
        registry.AddActivity<ValidateOrderActivity>();
        registry.AddActivity<SubmitOrderActivity>();
    })
    .UseDurableTaskScheduler(connectionString);
```

The sample worker resolves endpoint and task hub from configuration and uses `Authentication=None` for the local emulator or `Authentication=DefaultAzure` for managed DTS.

---

## Client Registration

Any host that needs to schedule or query orchestrations can register the client:

```csharp
builder.Services.AddContosoOrderWorkflowClient(builder.Configuration);
```

The client registration falls back from `ConnectionStrings:DurableTaskScheduler` to `DurableTaskScheduler:Endpoint` and `DurableTaskScheduler:TaskHub`.

---

## Running Locally

This repository includes the DTS emulator in [docker-compose.yml](../docker-compose.yml). Start it with container infrastructure:

```bash
docker compose up -d dts-emulator
```

Then run the worker host:

```bash
dotnet run --project samples/src/Contoso.Order.Workflow.Worker
```

By default the sample worker connects to `http://localhost:8080`, task hub `order`, with `Authentication=None`.
