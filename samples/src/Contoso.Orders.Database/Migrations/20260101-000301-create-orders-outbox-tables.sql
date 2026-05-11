-- Create table: [Orders].[Outbox] and [Orders].[OutboxLease]

BEGIN TRANSACTION

CREATE TABLE [Orders].[Outbox] (
  [OutboxId] BIGINT IDENTITY (1, 1) NOT NULL PRIMARY KEY,
  [TenantId] NVARCHAR(255) NOT NULL,
  [PartitionId] INT NOT NULL,
  [Status] TINYINT NOT NULL DEFAULT 0,
  [EnqueuedUtc] DATETIME2 NOT NULL,
  [AvailableUtc] DATETIME2 NOT NULL,
  [DequeuedUtc] DATETIME2 NULL,
  [Attempts] INT NOT NULL DEFAULT 0,
  [Destination] NVARCHAR(255) NULL,
  [Event] NVARCHAR(MAX) NOT NULL,
  [LeaseId] UNIQUEIDENTIFIER NULL,
  [LeaseUntilUtc] DATETIME2 NULL,

  INDEX [IX_Orders_Outbox_PartitionOrder] ([TenantId], [PartitionId], [OutboxId]) INCLUDE ([Status], [AvailableUtc], [LeaseUntilUtc], [Destination], [Event], [Attempts]),
  INDEX [IX_Orders_Outbox_WorkerPull] ([TenantId], [PartitionId], [Status]) INCLUDE ([OutboxId], [AvailableUtc]),
  INDEX [IX_Orders_Outbox_CleanUp] ([OutboxId]) INCLUDE ([DequeuedUtc]) WHERE [Status] = 2
);

CREATE TABLE [Orders].[OutboxLease] (
  [TenantId] NVARCHAR(255) NOT NULL,
  [PartitionId] INT NOT NULL,
  [LeaseId] UNIQUEIDENTIFIER NULL,
  [LeaseUntilUtc] DATETIME2 NULL,

  CONSTRAINT PK_Orders_OutboxLease PRIMARY KEY (TenantId, PartitionId)
);

COMMIT TRANSACTION