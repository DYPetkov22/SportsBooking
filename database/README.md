# SQL Server база

Локалното стартиране по подразбиране използва SQLite и не изисква тези SQL скриптове. За конфигурация със SQL Server виж [README](../README.md#optional-sql-server); задай provider и двата connection string-а чрез environment variables.

- `SportsBooking_Simple.sql` — създава `SportsBookingDB_Simple`, 6 таблици с ограничения и примерни данни. **Изпълнява се веднъж** върху нов SQL Server.
- `02_Optional_Booking_Index.sql` — по желание добавя индекс за проверка на резервирани часове (само след създаването на основните таблици; изпълнява се веднъж).

**Не качвай** `.mdf`, `.ldf`, `.bak` или реални бази в GitHub. За локално възстановяване изпълни основния SQL скрипт, конфигурирай връзките в `appsettings.json` и стартирай MVC проекта. Identity базата се създава от приложението при първо стартиране.

За повече подробности виж `docs/04_Backend_Schema.md` и `docs/06_Seed_Data.md`.
