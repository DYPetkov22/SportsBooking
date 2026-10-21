-- По желание: ускорява търсенето на заети часове.
USE SportsBookingDB_Simple;
GO
CREATE INDEX IX_Bookings_Field_Date_Status_Time
ON Bookings (FieldID, BookingDate, Status, StartTime, EndTime);
GO
