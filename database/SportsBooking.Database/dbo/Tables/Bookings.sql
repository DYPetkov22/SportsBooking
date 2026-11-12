CREATE TABLE dbo.Bookings (
    BookingID INT IDENTITY(1,1) CONSTRAINT PK_Bookings PRIMARY KEY,
    UserID INT NOT NULL,
    FieldID INT NOT NULL,
    BookingDate DATE NOT NULL,
    StartTime TIME(7) NOT NULL,
    EndTime TIME(7) NOT NULL,
    Status NVARCHAR(20) NOT NULL CONSTRAINT DF_Bookings_Status DEFAULT N'Активна',
    CONSTRAINT FK_Bookings_Users FOREIGN KEY (UserID) REFERENCES dbo.Users(UserID),
    CONSTRAINT FK_Bookings_Fields FOREIGN KEY (FieldID) REFERENCES dbo.Fields(FieldID),
    CONSTRAINT CK_Bookings_Time CHECK (EndTime > StartTime),
    CONSTRAINT CK_Bookings_Status CHECK (Status IN (N'Активна', N'Отказана'))
);
GO

CREATE INDEX IX_Bookings_Field_Date_Start ON dbo.Bookings(FieldID, BookingDate, StartTime);
GO

CREATE INDEX IX_Bookings_User_Date ON dbo.Bookings(UserID, BookingDate);
GO
