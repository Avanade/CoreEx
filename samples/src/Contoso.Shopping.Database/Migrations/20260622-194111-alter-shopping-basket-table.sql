-- Alter table: [shopping].[basket]

BEGIN TRANSACTION

ALTER TABLE [shopping].[basket]
  ADD [ShippingAddressJson] NVARCHAR(MAX) NULL

COMMIT TRANSACTION
