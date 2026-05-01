-- Create table: [Products].[Outbox] and [Products].[OutboxLease]

BEGIN TRANSACTION

CREATE TABLE [Products].[Outbox] (
  [OutboxId] BIGINT IDENTITY (1, 1) NOT NULL PRIMARY KEY,
  [TenantId] NVARCHAR(255) NOT NULL,      -- Optional, null indicates no tenancy.
  [PartitionId] INT NOT NULL,             -- Partition number; computed in application from partition-key.
  [Status] TINYINT NOT NULL DEFAULT 0,    -- 0=Pending, 1=Processing, 2=Done.
  [EnqueuedUtc] DATETIME2 NOT NULL,       -- When the event was enqueued within application.
  [AvailableUtc] DATETIME2 NOT NULL,      -- When the event is eligible for processing (retry delay).
  [DequeuedUtc] DATETIME2 NULL,           -- When the event was successfully dequeued/relayed.
  [Attempts] INT NOT NULL DEFAULT 0,      -- Retry attempt count.

  -- Message:
  [Destination] NVARCHAR(255) NULL,       -- Message destination; i.e. queue/topic/etc.
  [Event] NVARCHAR(MAX) NOT NULL,         -- CloudEvent as JSON.

  -- Claim/leasing:
  [LeaseId] UNIQUEIDENTIFIER NULL,        -- Unique identifier of the lease.
  [LeaseUntilUtc] DATETIME2 NULL,         -- Leased until UTC; after which assume released due to possible application crash.

  INDEX [IX_Products_Outbox_PartitionOrder] ([TenantId], [PartitionId], [OutboxId]) INCLUDE ([Status], [AvailableUtc], [LeaseUntilUtc], [Destination], [Event], [Attempts]),
  INDEX [IX_Products_Outbox_WorkerPull] ([TenantId], [PartitionId], [Status]) INCLUDE ([OutboxId], [AvailableUtc]),
  INDEX [IX_Products_Outbox_CleanUp] ([OutboxId]) INCLUDE ([DequeuedUtc]) WHERE [Status] = 2
);

CREATE TABLE [Products].[OutboxLease] (
  [TenantId] NVARCHAR(255) NOT NULL,      -- Optional, null indicates no tenancy.
  [PartitionId] INT NOT NULL,             -- Partition number; computed in application from partition-key.
  [LeaseId] UNIQUEIDENTIFIER NULL,        -- Unique identifier of the leasee.
  [LeaseUntilUtc] DATETIME2 NULL          -- Leased until UTC; after which assume released due to possible application crash.

  CONSTRAINT PK_Products_OutboxLease PRIMARY KEY (TenantId, PartitionId)
);

COMMIT TRANSACTION