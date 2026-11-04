CREATE SCHEMA Inventory;
GO
CREATE TABLE Inventory.Items (
  TenantId NVARCHAR(100) NOT NULL,
  Sku NVARCHAR(100) NOT NULL,
  Quantity INT NOT NULL,
  PRIMARY KEY (TenantId, Sku)
);
CREATE TABLE Inventory.Receipts (
  UnitKey CHAR(64) NOT NULL PRIMARY KEY,
  Content NVARCHAR(MAX) NOT NULL,
  ScopeId CHAR(32) NOT NULL
);
CREATE TABLE Inventory.Outbox (
  OutboxId BIGINT IDENTITY PRIMARY KEY,
  TenantId NVARCHAR(255) NOT NULL,
  PartitionId INT NOT NULL,
  Status TINYINT NOT NULL DEFAULT 0,
  EnqueuedUtc DATETIME2 NOT NULL,
  AvailableUtc DATETIME2 NOT NULL,
  DequeuedUtc DATETIME2 NULL,
  Attempts INT NOT NULL DEFAULT 0,
  Destination NVARCHAR(255) NULL,
  Event NVARCHAR(MAX) NOT NULL,
  LeaseId UNIQUEIDENTIFIER NULL,
  LeaseUntilUtc DATETIME2 NULL
);
GO
CREATE PROCEDURE Inventory.spOutboxEnqueue
  @TenantId NVARCHAR(255) = NULL,
  @PartitionId INT,
  @Destination NVARCHAR(255),
  @Event NVARCHAR(MAX),
  @EnqueuedUtc DATETIME2 = NULL,
  @AvailableUtc DATETIME2 = NULL
AS
BEGIN
  SET NOCOUNT ON;
  SET XACT_ABORT ON;
  INSERT INTO Inventory.Outbox (TenantId, PartitionId, Destination, Event, EnqueuedUtc, AvailableUtc)
  VALUES (COALESCE(@TenantId, '(none)'), @PartitionId, @Destination, @Event,
    COALESCE(@EnqueuedUtc, SYSUTCDATETIME()), COALESCE(@AvailableUtc, @EnqueuedUtc, SYSUTCDATETIME()));
END;
