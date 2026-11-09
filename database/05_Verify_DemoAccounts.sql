-- Execute AFTER the ASP.NET application has started successfully in Development.
-- Identity passwords are stored as hashes, so this query does not reveal them.
USE [SportsBookingIdentityDB];
GO
SELECT u.Email, r.Name AS RoleName, u.EmailConfirmed, u.BookingUserId
FROM dbo.AspNetUsers AS u
JOIN dbo.AspNetUserRoles AS ur ON ur.UserId = u.Id
JOIN dbo.AspNetRoles AS r ON r.Id = ur.RoleId
WHERE u.Email IN (N'admin@sportsbooking.local', N'manager@sportsbooking.local')
ORDER BY r.Name;
GO
