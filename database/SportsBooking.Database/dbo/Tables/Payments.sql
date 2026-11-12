CREATE TABLE dbo.Payments (
    PaymentID INT IDENTITY(1,1) CONSTRAINT PK_Payments PRIMARY KEY,
    BookingID INT NOT NULL,
    Amount DECIMAL(8,2) NOT NULL,
    PaymentMethod NVARCHAR(20) NOT NULL,
    PaidAtUtc DATETIME2(0) NOT NULL CONSTRAINT DF_Payments_PaidAtUtc DEFAULT SYSUTCDATETIME(),
    CONSTRAINT UQ_Payments_BookingID UNIQUE(BookingID),
    CONSTRAINT FK_Payments_Bookings FOREIGN KEY (BookingID) REFERENCES dbo.Bookings(BookingID),
    CONSTRAINT CK_Payments_Amount CHECK (Amount > 0),
    CONSTRAINT CK_Payments_Method CHECK (PaymentMethod IN (N'Карта', N'В брой'))
);
GO

CREATE INDEX IX_Payments_PaidAtUtc ON dbo.Payments(PaidAtUtc);
GO
