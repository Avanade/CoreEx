-- Alter table: [shopping].[basket]

BEGIN TRANSACTION

ALTER TABLE [shopping].[basket]
  ADD [ShippingAddressJson] NVARCHAR(2000) NULL

COMMIT TRANSACTION
