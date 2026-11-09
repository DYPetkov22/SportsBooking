# Одит на изискванията (задания 1–5 и 7)

| Изискване | Файл/реализация | Как да го демонстрираш |
|---|---|---|
| SQL Server, шест свързани таблици, PK/FK/UNIQUE/CHECK | `database/01_SportsBooking_SQLServer.sql`, `Data/SportsBookingContext.cs` | `database/03_Verify_SQLServer.sql` и `docs/ER_Diagram.svg` |
| Начални (seed) данни | SQL инсталационен скрипт | Показва 3 спорта, 2 центъра, 4 игрища, примерни резервации/плащания |
| CRUD на основните обекти | `Controllers/{Sports, SportsCenters, Fields, Users, Bookings, Payments}Controller.cs` | Добавяне/редакция/детайли/изтриване от UI с подходяща роля |
| Валидация | `Models/*.cs`, POST проверки, DB CHECK и FK | Отрицателна цена, невалиден имейл, краен час преди начален |
| Identity / три роли | `Controllers/AccountController.cs`, `Security/*`, `Controllers/RolesController.cs` | Client не вижда staff CRUD; admin управлява роли |
| Защита от чужди резервации | `BookingsController.cs`, `FilesController.cs` | Достъп до чужд ID се отказва на ниво сървър |
| Търсене, категория, ценови диапазон и сортиране | `FieldsController.Index`, `Views/Fields/Index.cshtml` | Комбинирани филтри; сортировка по цена |
| Статус, дата и свободно търсене | `BookingsController.Index`, `Views/Bookings/Index.cshtml` | Активна/отказана, начална/крайна дата |
| LINQ пагинация | `CountAsync`, `Skip`, `Take`, `OrderBy` в двата Index | Показвай >10 записа и запазване на филтрите при Page 2 |
| Профилна снимка, галерия и документи | `FilesController`, `Services/LocalFileStorage.cs` | JPG/PNG и PDF/DOCX, лимити, минификации |
| Безопасно съхранение | `App_Data/uploads/` (извън `wwwroot`), random ID, permission checks | Проверка, че файлът не се отваря директно с URL към App_Data |
| High fidelity интерфейс | `Views/Shared/_Layout`, начална страница, `wwwroot/css/site.css`, `wwwroot/img/*.svg` | Десктоп и мобилен режим |

## Запазени ограничения / честно описание

- Дизайнът е **имплементация на high-fidelity визуалния стил**, а не директен export на векторни слоеве от Figma. В качения архив няма SVG файлове; SVG графиките са създадени локално и включени като активи.
- Плащанията са учебни записи — **няма реално картово таксуване**.
- Превключването към SQL Server **не мигрира SQLite записи автоматично**. Архивът не включва SQLite файл.
- Identity схемата се създава чрез `EnsureCreated`; при бъдещи промени изисква подготвени EF Core migrations.
- Кодът изисква реална проверка с .NET 9 SDK и локален SQL Server. Файловите проверки не са равностойни на интеграционен тест.

## Препоръчан демонстрационен сценарий

1. Пусни `database/01_SportsBooking_SQLServer.sql` и покажи ER диаграмата.
2. Влез като администратор и добави игрище с валидации.
3. Комбинирай филтри, сортиране по цена и пагинация.
4. Качи JPG за игрище, опитай забранен файл и покажи съобщението.
5. Влез като клиент, резервирай час, опитай конфликт и откажи собствена резервация.
6. Опитай клиентски достъп до чужда резервация — очаквай 403.
7. Качи PDF към своя резервация и го изтегли; опитай достъп от друг клиент — очаквай 403.


## Допълнение: Задание 7 и демонстрационни акаунти (09.11.2026)

| Изискване | Доказателство в проекта | Как се проверява |
|---|---|---|
| Готови Demo Admin + Manager в SQL Server Identity | `Security/DemoAccounts.cs`, `Security/IdentitySeeder.cs`, `Program.cs` | Първи старт в Development; проверка на `AspNetUsers`, `AspNetUserRoles` |
| Демо акаунти на Login страницата | `Views/Account/Login.cshtml`, `wwwroot/js/site.js` | Данни и 2 бутона „Попълни“ под формата; не се показват в Production |
| Административен панел | `Controllers/ReportsController.cs`, `Views/Reports/Index.cshtml` | Само Admin/Manager влизат в `/Reports` |
| KPI и 3 вида графики | `Reporting/ReportsService.cs`, Razor dashboard | Резервации/месец, приход/месец, топ спортове; нули за празни месеци |
| Филтър по период | `ReportsController.ResolvePeriod` | Период до 24 календарни месеца; 400 при невалиден период |
| PDF експорт | `ReportExports.ToPdf` с QuestPDF | PDF файл със суми, месечна таблица и топ спортове |
| Excel експорт | `ReportExports.ToExcel` с ClosedXML | `.xlsx` с 4 sheets: Обобщение, По месеци, Топ спортове, Начини на плащане |
| Дата на плащането | `Payment.PaidAtUtc`, `01_SportsBooking_SQLServer.sql`, `04_Upgrade_Reports.sql` | Новите плащания имат UTC дата; старите: дата на upgrade |
| Коректна видимост в sidebar | `Views/Shared/_Layout.cshtml` | Menu за Payments/Reports се показва на Admin и Manager |

**Ограничения:** няма реални плащания през банка/PSP; дата на старите плащания не може да бъде възстановена. Тестът на компилация/SQL Server се изпълнява на компютър с .NET 9 и SQL Server, а статичният audit сам по себе си не доказва runtime поведение.
