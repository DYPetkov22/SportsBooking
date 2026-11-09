-- LEGACY: index is now in 01_SportsBooking_SQLServer.sql.
USE [SportsBookingDB_Simple];
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Bookings_Field_Date_Start' AND object_id=OBJECT_ID(N'dbo.Bookings'))
    CREATE INDEX IX_Bookings_Field_Date_Start ON dbo.Bookings(FieldID,BookingDate,StartTime);
GO
