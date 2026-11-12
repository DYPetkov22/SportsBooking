USE [SportsBookingDB_Simple];
GO
-- Expect six tables, six primary keys, five FKs, field/booking/payment constraints.
SELECT t.name AS TableName, (SELECT COUNT(*) FROM sys.columns c WHERE c.object_id=t.object_id) AS Columns
FROM sys.tables t WHERE t.name IN (N'Users',N'Sports',N'SportsCenters',N'Fields',N'Bookings',N'Payments')
ORDER BY t.name;
SELECT OBJECT_NAME(parent_object_id) AS TableName,name AS ForeignKey
FROM sys.foreign_keys ORDER BY TableName,name;
SELECT OBJECT_NAME(parent_object_id) AS TableName,name AS CheckConstraint
FROM sys.check_constraints ORDER BY TableName,name;
SELECT i.name AS IndexName, OBJECT_NAME(i.object_id) AS TableName
FROM sys.indexes i WHERE i.name LIKE N'IX_%' AND OBJECT_NAME(i.object_id) IN (N'Fields',N'Bookings');
SELECT f.FieldName,s.SportName,c.CenterName,f.PricePerHour
FROM dbo.Fields f JOIN dbo.Sports s ON f.SportID=s.SportID
JOIN dbo.SportsCenters c ON f.CenterID=c.CenterID;
GO
