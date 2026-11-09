# Инсталация и стартиране с Microsoft SQL Server (Windows / Rider)

## 1. Необходими програми

- .NET 9 SDK (`dotnet --info` трябва да показва SDK версия 9.x).
- Microsoft SQL Server Express LocalDB или SQL Server Express / Developer; SQL Server Management Studio (SSMS), ако предпочиташ графичен интерфейс.
- JetBrains Rider / Visual Studio.

**Вариант A — LocalDB (най-лесно на Windows):** `Server=(localdb)\MSSQLLocalDB`.

**Вариант B — SQL Server Express:** редактирай двата connection strings в `SportsBooking.Web/appsettings.json` или ги задай чрез environment variables. Пример: `Server=.\SQLEXPRESS;Database=SportsBookingDB_Simple;Trusted_Connection=True;TrustServerCertificate=True;` за бизнес базата и аналогично `SportsBookingIdentityDB` за Identity.

SQL Server трябва да е инсталиран **локално** или да имаш разрешена връзка към сървър. SQLite вече не се използва.

## 2. Създаване на база данни

1. Отвори SSMS; `Server name: (localdb)\MSSQLLocalDB`, Authentication: Windows Authentication.
2. Отвори `database/01_SportsBooking_SQLServer.sql` и натисни **Execute**. Файлът съдържа `GO` batch разделители (изисква SSMS/sqlcmd/Rider SQL console, а не EF `ExecuteSqlRaw`).
3. В Object Explorer → Databases ще видиш `SportsBookingDB_Simple` с таблици `Users`, `Sports`, `SportsCenters`, `Fields`, `Bookings`, `Payments`.
4. При нужда изпълни `database/03_Verify_SQLServer.sql` за проверки на ключове, ограничения и първоначални записи.

> Базата `SportsBookingIdentityDB` се създава при първо успешно стартиране чрез EF Core Identity `EnsureCreatedAsync`, ако Identity базата е нова и празна. Не създавай ръчно празни потребителски таблици с произволна схема.

## 3. Стартиране на .NET приложението

```powershell
cd SportsBooking
dotnet restore SportsBooking.sln
dotnet build SportsBooking.sln
dotnet run --project SportsBooking.Web --launch-profile SportsBooking.Web
```

Посети `http://localhost:5179`. При SQL грешка първо провери дали (1) SQL Server LocalDB е инсталиран, (2) SQL скриптът е изпълнен, (3) имената на сървъра и базите съвпадат, (4) акаунтът има права за създаване на Identity база.

## 4. Създаване на администратор (без секрети в GitHub)

PowerShell **само в текущата терминална сесия**:

```powershell
$env:SPORTSBOOKING_ADMIN_EMAIL="admin@local.test"
$env:SPORTSBOOKING_ADMIN_PASSWORD="ReplaceThis_Strong-Password123!"
dotnet run --project SportsBooking.Web --launch-profile SportsBooking.Web
```

Стойността по-горе е **пример, не истинска парола**. Избери уникална тайна и никога не я записвай в репозитория. Първоначалните роли са `User`, `Manager`, `Admin`. При регистрация обикновените акаунти получават `User`.

## 5. Чести проблеми

- **`LocalDB` not found** → инсталирай SQL Server Express LocalDB или смени двата connection strings към реален SQL Server.
- **`Cannot open database SportsBookingDB_Simple`** → първо изпълни SQL скрипта.
- **`Invalid object name 'Fields'`** → скриптът е изпълнен върху друг SQL Server или друга база.
- **`Database already exists`** → използвай idempotent новия скрипт; не изпълнявай исторически скриптове.
- **Администраторският профил** → новият Admin/Manager demo seeder създава и свързан `dbo.Users` booking profile. Ако ползваш стара Identity база, профилите не се променят автоматично; провери `BookingUserId`.
- **Старите SQLite данни ги няма** → архивът не съдържа SQLite база, не е правен автоматичен импорт; необходим е контролируем ETL/import.

## 6. SQL Server настройки без секрети в сорс кода

За отделни машини използвай `ConnectionStrings__SportsBooking` и `ConnectionStrings__Identity` като environment variables; всички настройки се четат от стандартната ASP.NET Core конфигурация. Не commit-вай реални потребителски пароли и SQL акаунти.

## 7. Два готови демо профила (Development ONLY)

След успешно изпълнение на `01_SportsBooking_SQLServer.sql` и първо стартиране на проекта под `Development` се създават чрез ASP.NET Core `UserManager` **два истински Identity акаунта с хеширани пароли**. Таблиците `AspNetUsers`, `AspNetRoles`, `AspNetUserRoles` са в `SportsBookingIdentityDB`. В `SportsBookingDB_Simple.dbo.Users` има отделни свързани профили за тях. **Провери резултата** в SSMS чрез `database/05_Verify_DemoAccounts.sql` (след стартиране на приложението).

- Admin: `admin@sportsbooking.local` / `AdminDemo#2026!`
- Manager: `manager@sportsbooking.local` / `ManagerDemo#2026!`

Паролите са видими под формата за вход; бутоните „Попълни“ автоматично попълват данните. Това е **уязвимо по дизайн за публичен deployment** и затова се активира само при `ASPNETCORE_ENVIRONMENT=Development` и `DemoAccounts:Enabled=true`. При `Production` механизмът е забранен дори при включен конфигурационен флаг. Ако акаунтът вече съществува, seeder-ът не сменя паролата. За reset използвай `UserManager` или пресъздай само демо Identity базата в тестова среда (това изтрива и останалите акаунти в нея!).

## 8. Upgrade за Задание 7 (съществуваща SQL Server база)

1. **Направи backup** на `SportsBookingDB_Simple` преди структурни промени.
2. В SSMS отвори и изпълни `database/04_Upgrade_Reports.sql` (добавя `Payments.PaidAtUtc` и индекс; повторното изпълнение е безопасно).
3. Стартирай приложението и отвори `/Reports` с Admin/Manager.
4. Новите плащания получават UTC timestamp от SQL Server чрез `SYSUTCDATETIME()`. **Старите плащания нямат историческо време** и се маркират с часа на изпълнение на миграцията — тази граница е отразена в справките.
5. Изтегли PDF и Excel за избрания период.

За нова инсталация използвай направо актуалния `01_SportsBooking_SQLServer.sql` — не е нужно да пускаш `04` отделно. След стартиране в `SportsBookingIdentityDB` се създават Identity таблиците и демо акаунтите в Development.
