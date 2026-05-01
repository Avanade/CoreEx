-- Migration Script

BEGIN TRANSACTION

CREATE TABLE [Products].[Movement] (
  [MovementId] NVARCHAR(50) NOT NULL PRIMARY KEY,
  [ReferenceId] NVARCHAR(50) NOT NULL,
  [MovementKindCode] NVARCHAR(50) NOT NULL,
  [MovementStatusCode] NVARCHAR(50) NOT NULL,
  [ProductId] NVARCHAR(50) NOT NULL,
  [Quantity] DECIMAL(18, 2) DEFAULT 0 NOT NULL,
  [UnitOfMeasureCode] NVARCHAR(50) NOT NULL,
  [CreatedBy] NVARCHAR(250) NULL,
  [CreatedOn] DATETIMEOFFSET NULL,
  [UpdatedBy] NVARCHAR(250) NULL,
  [UpdatedOn] DATETIMEOFFSET NULL,
  [RowVersion] TIMESTAMP NOT NULL,

  CONSTRAINT [UQ_Products_Movement_Reference_Product] UNIQUE ([ReferenceId], [ProductId])
);
  
COMMIT TRANSACTION