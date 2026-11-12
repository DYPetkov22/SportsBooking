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
