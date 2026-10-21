# Стартиране на SportsBooking в JetBrains Rider (Windows)

## Бърз старт (по подразбиране, без SQL Server)

Нужен е само .NET 9 SDK. Отвори `SportsBooking.sln` в Rider и стартирай профила `SportsBooking.Web`, или изпълни:

```powershell
dotnet run --project .\SportsBooking.Web --launch-profile SportsBooking.Web
```

Отвори **http://localhost:5179**. SQLite базите се създават автоматично в `SportsBooking.Web/App_Data`, с примерни спортове, центрове и игрища. Регистрирай нов профил за резервации. Данните се запазват след рестарт. Не е нужен HTTPS сертификат за този профил.

За HTTPS използвай `dotnet dev-certs https --trust` и `dotnet run --project .\SportsBooking.Web --launch-profile https`.

**Следващите инструкции са за допълнителната конфигурация със SQL Server.** За нея задай `Database__Provider=SqlServer` и двата connection string-а чрез environment variables, както е описано в [README](../README.md#optional-sql-server). Настройките в `appsettings.Development.json` имат предимство пред `appsettings.json`.

## 1. Нужно е
- Инсталиран **.NET 9 SDK** (`dotnet --list-sdks`).
- SQL Server Express **LocalDB** или друг работещ SQL Server.
- JetBrains Rider с отворен `SportsBooking.sln`.
- Интернет за първото изтегляне на NuGet пакети.

## 2. Проверка на средата
От корена на проекта (папката със `SportsBooking.sln`) изпълни:

```powershell
dotnet --list-sdks
sqllocaldb info
```

Ако `sqllocaldb` не е разпозната команда, LocalDB вероятно не е инсталиран или не е в PATH. При инсталация чрез SQL Server Express има отделен пакет `SqlLocalDB.msi`; официална инструкция: https://learn.microsoft.com/en-us/sql/database-engine/configure-windows/sql-server-express-localdb.

След инсталиране може да стартираш инстанцията чрез пълния път към `SqlLocalDB.exe`:

```powershell
& "C:\Program Files\Microsoft SQL Server\160\Tools\Binn\SqlLocalDB.exe" start MSSQLLocalDB
```

`160` е само пример за версията; използвай намерения път. Може да използваш SQL Server Express вместо LocalDB, но трябва да смениш **двата** connection string-а в `appsettings.json` (напр. `Server=.\SQLEXPRESS;...`).

## 3. Бизнес SQL база
Отвори `database/SportsBooking_Simple.sql` в SSMS или SQL editor на Rider и го изпълни **веднъж** върху правилния SQL Server. Ще се създаде `SportsBookingDB_Simple` и шест таблици.

Identity базата `SportsBookingIdentityDB` се създава автоматично при стартиране на приложението.

## 4. HTTPS dev certificate
```powershell
dotnet dev-certs https --trust
```

## 5. Admin акаунт (само при първото пускане)
Използвай собствени стойности, не ги записвай в GitHub:

```powershell
$env:SPORTSBOOKING_ADMIN_EMAIL = "myadmin@example.com"
$env:SPORTSBOOKING_ADMIN_PASSWORD = "ChooseYourOwnLongSecurePassword!29"
```

Смени примерния имейл и парола; не използвай предложените стойности за реален акаунт.

## 6. Компилация и стартиране
```powershell
dotnet restore .\SportsBooking.sln
dotnet build .\SportsBooking.sln
dotnet run --project .\SportsBooking.Web\SportsBooking.Web.csproj --launch-profile SportsBooking.Web
```

Отвори `http://localhost:5179` (или `https://localhost:7179` с профила `https`). **Не въвеждай текста `Now listening on ...` в PowerShell** — това е съобщение, не команда.

## 7. Типични проблеми
- `Couldn't find a project to run`: изпълни `dotnet run --project .\SportsBooking.Web\SportsBooking.Web.csproj` от корена.
- `sqllocaldb not recognized`: провери дали е инсталиран `.msi` пакетът; командата `Get-ChildItem "C:\Program Files\Microsoft SQL Server" -Filter SqlLocalDB.exe -Recurse -ErrorAction SilentlyContinue` може да открие EXE.
- `SqlException Error 52 Unable to locate a Local Database Runtime installation`: SQL Server LocalDB **не е достъпен**. Инсталирай го или използвай друг SQL Server и настрой connection string-а.
- `Invalid object name 'Fields'`: бизнес базата не е създадена или се използва грешен connection string.
- `There is already an object named ...`: вероятно си изпълнил seed скрипта повторно; той не е идемпотентен.
- HTTPS warning: `dotnet dev-certs https --trust` и рестарт на браузъра.
- При Role/Identity exception: провери дали Identity базата се създава и SQL акаунтът има права да създава база.

Приложението има **две бази** — `SportsBookingDB_Simple` (бизнес) и `SportsBookingIdentityDB` (Identity) — това е умишлено.
