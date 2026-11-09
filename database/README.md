# Microsoft SQL Server — database/

`01_SportsBooking_SQLServer.sql` — **основният SQL инсталационен скрипт**: създава `SportsBookingDB_Simple` с шест домейн таблици, PK/FK/CHECK/UNIQUE, индекси и примерни данни. Повторното изпълнение не изтрива данни.

`02_Optional_Booking_Index.sql` — исторически допълнителен индекс (не е нужен, защото вече е включен в основния скрипт).

`03_Verify_SQLServer.sql` — проверки за структурите и началните данни.

`04_Upgrade_Reports.sql` — добавя UTC дата на плащането (`PaidAtUtc`) и индекс в **съществуваща база**. Направи backup и изпълни преди стартиране на обновеното приложение. Не се изисква за чиста инсталация с `01`.

`05_Verify_DemoAccounts.sql` — проверява наличните Identity Admin и Manager потребители и роли след стартиране в Development; **не показва пароли**.

`SportsBooking_Simple.sql` — остарялото име, оставено като указател към новия скрипт; **не го изпълнявай вместо 01**.

ASP.NET Core Identity таблиците са в отделна SQL Server база `SportsBookingIdentityDB`, която се създава автоматично само при първоначално стартиране на нова празна база.

Подробни стъпки: [docs/SQL_SERVER_SETUP_BG.md](../docs/SQL_SERVER_SETUP_BG.md).
