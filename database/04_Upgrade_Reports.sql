-- Run ONLY against an existing installation that was created before assignment 7.
-- Safe to re-run. The new timestamp is the migration date for historic payments;
-- the original payment date was not stored in the previous schema.
USE [SportsBookingDB_Simple];
GO
IF COL_LENGTH(N'dbo.Payments', N'PaidAtUtc') IS NULL
    ALTER TABLE dbo.Payments ADD PaidAtUtc DATETIME2(0) NOT NULL
        CONSTRAINT DF_Payments_PaidAtUtc DEFAULT SYSUTCDATETIME() WITH VALUES;
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Payments_PaidAtUtc' AND object_id = OBJECT_ID(N'dbo.Payments'))
    CREATE INDEX IX_Payments_PaidAtUtc ON dbo.Payments(PaidAtUtc);
GO
SELECT TOP 10 PaymentID, Amount, PaymentMethod, PaidAtUtc FROM dbo.Payments ORDER BY PaymentID DESC;
GO
