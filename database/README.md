# SportsBooking SQL Server database solution

This folder contains the database source to commit to GitHub alongside the web application. Open `SportsBooking.Database.sln` for the separate database solution. Both projects target Microsoft SQL Server 2019 or later, including SQL Server Express LocalDB.

| Project | Default database | Contents |
| --- | --- | --- |
| `SportsBooking.Database/SportsBooking.Database.sqlproj` | `SportsBookingDB_Simple` | Six booking tables, keys, constraints, indexes and sample data |
| `SportsBooking.Identity/SportsBooking.Identity.sqlproj` | `SportsBookingIdentityDB` | Seven ASP.NET Core Identity tables and indexes, including `AspNetUsers.BookingUserId` |

Each table has its own SQL file under its project's `dbo/Tables/` folder. The booking project's `PostDeployment/Seed.sql` adds the existing demonstration sports, centers, fields, customer profiles, bookings and payment. Repeated deployment skips existing sample records; it does not reset them.

## Set up after cloning

Install the .NET 9 SDK and SQL Server LocalDB (Windows) or use an accessible SQL Server 2019+ instance. The deployment script uses Windows authentication. Install Microsoft's SqlPackage command-line tool once:

```powershell
dotnet tool install --global Microsoft.SqlPackage
```

From the repository root, build and publish both databases:

```powershell
powershell -ExecutionPolicy Bypass -File .\database\Deploy.ps1
dotnet run --project SportsBooking.Web --launch-profile SportsBooking.Web
```

The script builds the database solution, then publishes to `(localdb)\MSSQLLocalDB` using the database names in `SportsBooking.Web/appsettings.json`. A database that does not exist is created. Destructive changes that could lose data are blocked, and objects outside the projects are retained. The two databases are published sequentially; if publishing one fails, any successful earlier publish remains applied.

To use SQL Server Express or another Windows-authenticated instance:

```powershell
powershell -ExecutionPolicy Bypass -File .\database\Deploy.ps1 -ServerInstance '.\SQLEXPRESS'
$env:ConnectionStrings__SportsBooking='Server=.\SQLEXPRESS;Database=SportsBookingDB_Simple;Trusted_Connection=True;TrustServerCertificate=True;'
$env:ConnectionStrings__Identity='Server=.\SQLEXPRESS;Database=SportsBookingIdentityDB;Trusted_Connection=True;TrustServerCertificate=True;'
dotnet run --project SportsBooking.Web --launch-profile SportsBooking.Web
```

Optional `-BusinessDatabaseName` and `-IdentityDatabaseName` arguments change the deployment targets; update the application connection strings to match. For SQL authentication, use SqlPackage directly with credentials supplied locally instead of putting them in tracked files.

On first application startup, the existing Identity seeder creates roles and, in Development, the documented Admin/Manager demo accounts with hashed passwords. Identity schema deployment itself does not create login accounts. Existing accounts and passwords are retained.

## Build without a running SQL Server

```powershell
dotnet build database/SportsBooking.Database.sln --configuration Release
```

The pinned `Microsoft.Build.Sql` SDK is restored automatically from NuGet. The build validates SQL object references and produces a `.dacpac` in each project's `bin/Release/` folder. SQL Server and SqlPackage are required only for publishing. The main `SportsBooking.sln` links this folder; the database projects build separately from the web application.

For example, publish a built project manually:

```powershell
sqlpackage /Action:Publish /SourceFile:database/SportsBooking.Database/bin/Release/SportsBooking.Database.dacpac /TargetServerName:'(localdb)\MSSQLLocalDB' /TargetDatabaseName:SportsBookingDB_Simple /TargetTrustServerCertificate:True /p:BlockOnPossibleDataLoss=True /p:DropObjectsNotInSource=False
```

Publish the Identity package to `SportsBookingIdentityDB` in the same way, using `SportsBooking.Identity/bin/Release/SportsBooking.Identity.dacpac`.

## Existing SQL scripts

The original SSMS/sqlcmd setup remains available as an alternative to building the database projects:

| Script | Purpose |
| --- | --- |
| `01_SportsBooking_SQLServer.sql` | Self-contained booking database installer with sample data. Run in SSMS, Rider's SQL console or sqlcmd with `GO` support. The application creates the Identity schema at first startup if it is absent. |
| `02_Optional_Booking_Index.sql` | Legacy index script; already included in the installer and database project. |
| `03_Verify_SQLServer.sql` | Inspect booking tables, relationships, constraints, indexes and sample fields. |
| `04_Upgrade_Reports.sql` | Legacy upgrade adding `Payments.PaidAtUtc` and its index. Unnecessary for a fresh installation or project deployment. |
| `05_Verify_DemoAccounts.sql` | Inspect Identity accounts/roles after starting the application in Development; does not display passwords. |
| `SportsBooking_Simple.sql` | Legacy pointer to installer `01`; not an installer itself. |

For future schema changes, update the relevant SQL project and the matching EF Core model. If the change also affects the standalone installer, update `01_SportsBooking_SQLServer.sql` as well. Keep the sample data in that installer and `PostDeployment/Seed.sql` consistent. Publishing the Identity project updates the schema; the application's `EnsureCreatedAsync` only handles first-time creation.

## What goes into GitHub

Commit this entire folder's source (`.sln`, `.sqlproj`, `.sql`, `.ps1` and this README). It contains reproducible schema and sample data. Git does not automatically copy rows from your running SQL Server: real local customers, bookings and login accounts are not exported by these projects. Export any additional shareable sample data as reviewed SQL inserts if needed.

Build outputs, `.mdf`/`.ndf`/`.ldf` data files, backups (`.bak`/`.trn`) and generated deployment/data packages are excluded by the repository's `.gitignore`. Keep actual database backups outside the source repository.

More Windows/Rider setup details: [SQL_SERVER_SETUP_BG.md](../docs/SQL_SERVER_SETUP_BG.md).
