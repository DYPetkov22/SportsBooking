-- SportsBooking / MS SQL Server 2019+ and SQL Server LocalDB
-- Executable in SQL Server Management Studio or sqlcmd (requires GO support).
-- Safe to re-run: existing records are not deleted or overwritten.
-- Identity lives in a separate SQL Server database, created by ASP.NET Core Identity on first run.
IF DB_ID(N'SportsBookingDB_Simple') IS NULL
    CREATE DATABASE [SportsBookingDB_Simple];
GO
USE [SportsBookingDB_Simple];
GO
SET NOCOUNT ON;
GO
IF OBJECT_ID(N'dbo.Users', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Users (
        UserID INT IDENTITY(1,1) CONSTRAINT PK_Users PRIMARY KEY,
        FullName NVARCHAR(100) NOT NULL,
        Email NVARCHAR(100) NOT NULL,
        Phone VARCHAR(20) NULL,
        CONSTRAINT UQ_Users_Email UNIQUE (Email)
    );
END;
GO
IF OBJECT_ID(N'dbo.Sports', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Sports (
        SportID INT IDENTITY(1,1) CONSTRAINT PK_Sports PRIMARY KEY,
        SportName NVARCHAR(50) NOT NULL,
        CONSTRAINT UQ_Sports_SportName UNIQUE (SportName)
    );
END;
GO
IF OBJECT_ID(N'dbo.SportsCenters', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.SportsCenters (
        CenterID INT IDENTITY(1,1) CONSTRAINT PK_SportsCenters PRIMARY KEY,
        CenterName NVARCHAR(100) NOT NULL,
        City NVARCHAR(50) NOT NULL
    );
END;
GO
IF OBJECT_ID(N'dbo.Fields', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Fields (
        FieldID INT IDENTITY(1,1) CONSTRAINT PK_Fields PRIMARY KEY,
        FieldName NVARCHAR(50) NOT NULL,
        CenterID INT NOT NULL,
        SportID INT NOT NULL,
        PricePerHour DECIMAL(8,2) NOT NULL,
        CONSTRAINT FK_Fields_Centers FOREIGN KEY (CenterID) REFERENCES dbo.SportsCenters(CenterID),
        CONSTRAINT FK_Fields_Sports FOREIGN KEY (SportID) REFERENCES dbo.Sports(SportID),
        CONSTRAINT CK_Fields_Price CHECK (PricePerHour > 0)
    );
END;
GO
IF OBJECT_ID(N'dbo.Bookings', N'U') IS NULL
BEGIN
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
END;
GO
IF OBJECT_ID(N'dbo.Payments', N'U') IS NULL
BEGIN
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
END;
GO
-- Indexes for filtering, range locking during double-booking checks and pagination.
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Fields_Center_Sport' AND object_id = OBJECT_ID(N'dbo.Fields'))
    CREATE INDEX IX_Fields_Center_Sport ON dbo.Fields(CenterID, SportID);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Bookings_Field_Date_Start' AND object_id = OBJECT_ID(N'dbo.Bookings'))
    CREATE INDEX IX_Bookings_Field_Date_Start ON dbo.Bookings(FieldID, BookingDate, StartTime);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Bookings_User_Date' AND object_id = OBJECT_ID(N'dbo.Bookings'))
    CREATE INDEX IX_Bookings_User_Date ON dbo.Bookings(UserID, BookingDate);
IF COL_LENGTH(N'dbo.Payments', N'PaidAtUtc') IS NULL
    ALTER TABLE dbo.Payments ADD PaidAtUtc DATETIME2(0) NOT NULL
        CONSTRAINT DF_Payments_PaidAtUtc DEFAULT SYSUTCDATETIME() WITH VALUES;
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Payments_PaidAtUtc' AND object_id = OBJECT_ID(N'dbo.Payments'))
    CREATE INDEX IX_Payments_PaidAtUtc ON dbo.Payments(PaidAtUtc);
GO
-- Idempotent examples: avoid touching actual user records if re-run.
IF NOT EXISTS (SELECT 1 FROM dbo.Sports WHERE SportName = N'Футбол') INSERT dbo.Sports(SportName) VALUES (N'Футбол');
IF NOT EXISTS (SELECT 1 FROM dbo.Sports WHERE SportName = N'Тенис') INSERT dbo.Sports(SportName) VALUES (N'Тенис');
IF NOT EXISTS (SELECT 1 FROM dbo.Sports WHERE SportName = N'Баскетбол') INSERT dbo.Sports(SportName) VALUES (N'Баскетбол');
IF NOT EXISTS (SELECT 1 FROM dbo.SportsCenters WHERE CenterName = N'Спортен център Изток' AND City = N'София')
    INSERT dbo.SportsCenters(CenterName,City) VALUES (N'Спортен център Изток',N'София');
IF NOT EXISTS (SELECT 1 FROM dbo.SportsCenters WHERE CenterName = N'Спортен център Запад' AND City = N'Пловдив')
    INSERT dbo.SportsCenters(CenterName,City) VALUES (N'Спортен център Запад',N'Пловдив');
IF NOT EXISTS (SELECT 1 FROM dbo.Users WHERE Email = N'ivan@example.com')
    INSERT dbo.Users(FullName,Email,Phone) VALUES (N'Иван Иванов',N'ivan@example.com','0888111111');
IF NOT EXISTS (SELECT 1 FROM dbo.Users WHERE Email = N'maria@example.com')
    INSERT dbo.Users(FullName,Email,Phone) VALUES (N'Мария Петрова',N'maria@example.com','0888222222');
IF NOT EXISTS (SELECT 1 FROM dbo.Users WHERE Email = N'georgi@example.com')
    INSERT dbo.Users(FullName,Email,Phone) VALUES (N'Георги Георгиев',N'georgi@example.com','0888333333');
GO
IF NOT EXISTS (SELECT 1 FROM dbo.Fields WHERE FieldName = N'Футболно игрище 1')
INSERT dbo.Fields(FieldName,CenterID,SportID,PricePerHour)
SELECT N'Футболно игрище 1',c.CenterID,s.SportID,40 FROM dbo.SportsCenters c CROSS JOIN dbo.Sports s
WHERE c.CenterName=N'Спортен център Изток' AND c.City=N'София' AND s.SportName=N'Футбол';
IF NOT EXISTS (SELECT 1 FROM dbo.Fields WHERE FieldName = N'Тенис корт 1')
INSERT dbo.Fields(FieldName,CenterID,SportID,PricePerHour)
SELECT N'Тенис корт 1',c.CenterID,s.SportID,20 FROM dbo.SportsCenters c CROSS JOIN dbo.Sports s
WHERE c.CenterName=N'Спортен център Изток' AND c.City=N'София' AND s.SportName=N'Тенис';
IF NOT EXISTS (SELECT 1 FROM dbo.Fields WHERE FieldName = N'Баскетболно игрище 1')
INSERT dbo.Fields(FieldName,CenterID,SportID,PricePerHour)
SELECT N'Баскетболно игрище 1',c.CenterID,s.SportID,25 FROM dbo.SportsCenters c CROSS JOIN dbo.Sports s
WHERE c.CenterName=N'Спортен център Запад' AND c.City=N'Пловдив' AND s.SportName=N'Баскетбол';
IF NOT EXISTS (SELECT 1 FROM dbo.Fields WHERE FieldName = N'Футболно игрище 2')
INSERT dbo.Fields(FieldName,CenterID,SportID,PricePerHour)
SELECT N'Футболно игрище 2',c.CenterID,s.SportID,35 FROM dbo.SportsCenters c CROSS JOIN dbo.Sports s
WHERE c.CenterName=N'Спортен център Запад' AND c.City=N'Пловдив' AND s.SportName=N'Футбол';
GO
-- Insert 3 demo bookings only into a completely empty booking table, never duplicate existing reservations.
IF NOT EXISTS (SELECT 1 FROM dbo.Bookings)
BEGIN
    INSERT dbo.Bookings(UserID,FieldID,BookingDate,StartTime,EndTime,Status)
    SELECT u.UserID,f.FieldID,DATEADD(day,1,CONVERT(date,GETDATE())),'18:00','19:00',N'Активна'
      FROM dbo.Users u CROSS JOIN dbo.Fields f WHERE u.Email=N'ivan@example.com' AND f.FieldName=N'Футболно игрище 1';
    INSERT dbo.Bookings(UserID,FieldID,BookingDate,StartTime,EndTime,Status)
    SELECT u.UserID,f.FieldID,DATEADD(day,1,CONVERT(date,GETDATE())),'17:00','18:00',N'Активна'
      FROM dbo.Users u CROSS JOIN dbo.Fields f WHERE u.Email=N'maria@example.com' AND f.FieldName=N'Тенис корт 1';
    INSERT dbo.Bookings(UserID,FieldID,BookingDate,StartTime,EndTime,Status)
    SELECT u.UserID,f.FieldID,DATEADD(day,2,CONVERT(date,GETDATE())),'16:00','17:00',N'Активна'
      FROM dbo.Users u CROSS JOIN dbo.Fields f WHERE u.Email=N'georgi@example.com' AND f.FieldName=N'Баскетболно игрище 1';
END;
GO
IF NOT EXISTS (SELECT 1 FROM dbo.Payments)
BEGIN
    INSERT dbo.Payments(BookingID,Amount,PaymentMethod)
    SELECT b.BookingID, f.PricePerHour,N'Карта' FROM dbo.Bookings b
    JOIN dbo.Users u ON u.UserID=b.UserID JOIN dbo.Fields f ON f.FieldID=b.FieldID
    WHERE u.Email=N'ivan@example.com' AND f.FieldName=N'Футболно игрище 1'
    AND NOT EXISTS (SELECT 1 FROM dbo.Payments p WHERE p.BookingID=b.BookingID);
END;
GO
SELECT (SELECT COUNT(*) FROM dbo.Users) UsersCount,
       (SELECT COUNT(*) FROM dbo.Sports) SportsCount,
       (SELECT COUNT(*) FROM dbo.SportsCenters) CentersCount,
       (SELECT COUNT(*) FROM dbo.Fields) FieldsCount,
       (SELECT COUNT(*) FROM dbo.Bookings) BookingsCount,
       (SELECT COUNT(*) FROM dbo.Payments) PaymentsCount;
GO
