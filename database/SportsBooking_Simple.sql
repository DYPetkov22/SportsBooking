-- Система за резервация на спортни игрища
-- MS SQL Server: 6 свързани таблици + примерни данни
-- Изпълнете скрипта веднъж в SQL Server Management Studio.

CREATE DATABASE SportsBookingDB_Simple;
GO
USE SportsBookingDB_Simple;
GO

-- 1. Потребители
CREATE TABLE Users (
    UserID INT IDENTITY(1,1) PRIMARY KEY,
    FullName NVARCHAR(100) NOT NULL,
    Email NVARCHAR(100) NOT NULL UNIQUE,
    Phone VARCHAR(20)
);

-- 2. Видове спорт
CREATE TABLE Sports (
    SportID INT IDENTITY(1,1) PRIMARY KEY,
    SportName NVARCHAR(50) NOT NULL UNIQUE
);

-- 3. Спортни центрове
CREATE TABLE SportsCenters (
    CenterID INT IDENTITY(1,1) PRIMARY KEY,
    CenterName NVARCHAR(100) NOT NULL,
    City NVARCHAR(50) NOT NULL
);

-- 4. Игрища
CREATE TABLE Fields (
    FieldID INT IDENTITY(1,1) PRIMARY KEY,
    FieldName NVARCHAR(50) NOT NULL,
    CenterID INT NOT NULL,
    SportID INT NOT NULL,
    PricePerHour DECIMAL(8,2) NOT NULL CHECK (PricePerHour > 0),
    FOREIGN KEY (CenterID) REFERENCES SportsCenters(CenterID),
    FOREIGN KEY (SportID) REFERENCES Sports(SportID)
);

-- 5. Резервации
CREATE TABLE Bookings (
    BookingID INT IDENTITY(1,1) PRIMARY KEY,
    UserID INT NOT NULL,
    FieldID INT NOT NULL,
    BookingDate DATE NOT NULL,
    StartTime TIME NOT NULL,
    EndTime TIME NOT NULL,
    Status NVARCHAR(20) NOT NULL DEFAULT N'Активна',
    FOREIGN KEY (UserID) REFERENCES Users(UserID),
    FOREIGN KEY (FieldID) REFERENCES Fields(FieldID),
    CHECK (EndTime > StartTime),
    CHECK (Status IN (N'Активна', N'Отказана'))
);

-- 6. Плащания
CREATE TABLE Payments (
    PaymentID INT IDENTITY(1,1) PRIMARY KEY,
    BookingID INT NOT NULL UNIQUE,
    Amount DECIMAL(8,2) NOT NULL CHECK (Amount > 0),
    PaymentMethod NVARCHAR(20) NOT NULL,
    FOREIGN KEY (BookingID) REFERENCES Bookings(BookingID)
);

-- ПРИМЕРНИ ДАННИ

INSERT INTO Users (FullName, Email, Phone) VALUES
(N'Иван Иванов', 'ivan@example.com', '0888111111'),
(N'Мария Петрова', 'maria@example.com', '0888222222'),
(N'Георги Георгиев', 'georgi@example.com', '0888333333');

INSERT INTO Sports (SportName) VALUES
(N'Футбол'), (N'Тенис'), (N'Баскетбол');

INSERT INTO SportsCenters (CenterName, City) VALUES
(N'Спортен център Изток', N'София'),
(N'Спортен център Запад', N'Пловдив');

INSERT INTO Fields (FieldName, CenterID, SportID, PricePerHour) VALUES
(N'Футболно игрище 1', 1, 1, 40.00),
(N'Тенис корт 1', 1, 2, 20.00),
(N'Баскетболно игрище 1', 2, 3, 25.00),
(N'Футболно игрище 2', 2, 1, 35.00);

INSERT INTO Bookings (UserID, FieldID, BookingDate, StartTime, EndTime) VALUES
(1, 1, DATEADD(DAY, 1, CAST(GETDATE() AS DATE)), '18:00', '19:00'),
(2, 2, DATEADD(DAY, 1, CAST(GETDATE() AS DATE)), '17:00', '18:00'),
(3, 3, DATEADD(DAY, 2, CAST(GETDATE() AS DATE)), '16:00', '17:00');

INSERT INTO Payments (BookingID, Amount, PaymentMethod) VALUES
(1, 40.00, N'Карта'),
(2, 20.00, N'В брой');

-- ПРОВЕРКА: всички резервации с клиент и игрище
SELECT u.FullName, f.FieldName, b.BookingDate, b.StartTime, b.EndTime, b.Status
FROM Bookings b
JOIN Users u ON b.UserID = u.UserID
JOIN Fields f ON b.FieldID = f.FieldID;

-- Забележка: този опростен проект не блокира автоматично
-- две резервации за едно и също игрище с припокриващи се часове.
