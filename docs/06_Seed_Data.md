# 6. Стратегия за начални данни

## Цел
Да има реалистични записи, с които може веднага да се демонстрират таблици, зависимости и CRUD, без да се използват реални лични данни.

## Ред на попълване
1. `Users` (3 демо бизнес клиента).
2. `Sports` (футбол, тенис, баскетбол).
3. `SportsCenters` (2 спортни центъра).
4. `Fields` (4 игрища с FK към център и спорт).
5. `Bookings` (3 записа с FK към потребител и игрище).
6. `Payments` (2 записа с FK към резервация).

## Изпълнение
SQL скриптът `database/01_SportsBooking_SQLServer.sql` създава базата и демо данните. Изпълнява се **само веднъж** в нов SQL Server. Резервациите използват относителни дати спрямо текущия ден, за да не остаряват автоматично след една седмица.

Identity базата и ролите Admin/Manager/User се създават при стартиране на .NET приложението; admin акаунтът се създава само когато са зададени environment variables `SPORTSBOOKING_ADMIN_EMAIL` и `SPORTSBOOKING_ADMIN_PASSWORD`.

## Сигурност
Не поставяйте действителни администраторски пароли, connection-string credentials или лични данни в seed скрипта и GitHub. Демо клиентите от `Users` не получават Identity пароли.

## Проверки
`SELECT COUNT(*)` за шестте таблици; `JOIN` от Bookings към Users/Fields; отрицателни сценарии за FK, UNIQUE, CHECK и конфликтен час.


> **Задание 7 (ноември 2026):** таблицата `Payments` включва и `PaidAtUtc DATETIME2(0)` с UTC стойност по подразбиране. За съществуващи бази изпълнете `database/04_Upgrade_Reports.sql`. Демо Admin и Manager акаунтите са ASP.NET Core Identity записи в **отделната SQL Server база** `SportsBookingIdentityDB` и се създават автоматично от приложението при стартиране в Development, а не с SQL INSERT с plaintext пароли.
