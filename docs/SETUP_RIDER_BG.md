# Стартиране на SportsBooking в JetBrains Rider (.NET 9 + SQL Server)

1. Отвори `SportsBooking.sln` (от коренната папка) в Rider.
2. Провери, че .NET SDK е 9.0 (`dotnet --info`).
3. Инсталирай **SQL Server Express LocalDB** или SQL Server Express/Developer. Подробно: [SQL_SERVER_SETUP_BG.md](SQL_SERVER_SETUP_BG.md).
4. Отвори `database/01_SportsBooking_SQLServer.sql` в SSMS и изпълни скрипта върху `(localdb)\MSSQLLocalDB`.
5. В `SportsBooking.Web/appsettings.json` провери `ConnectionStrings:SportsBooking` и `ConnectionStrings:Identity` — **и двата сочат към Microsoft SQL Server, не SQLite**.
6. В терминала на Rider:

```powershell
dotnet restore SportsBooking.sln
dotnet build SportsBooking.sln
dotnet run --project SportsBooking.Web --launch-profile SportsBooking.Web
```

7. Отвори `http://localhost:5179`; регистрирай потребител или създай admin чрез environment variables (виж SQL Server Setup).

**Важно:** Identity база `SportsBookingIdentityDB` се създава автоматично; бизнес база `SportsBookingDB_Simple` се създава **само** чрез SQL скрипта. Старите SQLite данни не се импортират автоматично.
