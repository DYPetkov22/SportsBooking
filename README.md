# SportsBooking — ASP.NET Core MVC (.NET 9)

Уеб приложение за резервация на спортни игрища с роли Admin, Manager и User, ASP.NET Core Identity, EF Core и SQLite (Development) / SQL Server.

## Бърз старт

Нужен е **.NET 9 SDK**. От корена на проекта:

```powershell
dotnet restore SportsBooking.sln
dotnet run --project SportsBooking.Web --launch-profile SportsBooking.Web
```

В Development се използва локална SQLite база с автоматична инициализация, без нужда от отделен SQL Server. Отвори `http://localhost:5179` (или URL от конзолата) и регистрирай акаунт.

## Работа с файлове

- **Моят профил:** качване, замяна, изтриване на профилна снимка (JPG/PNG, до 5 MB).
- **Резервации → Преглед:** качване, изтегляне, изтриване на PDF/DOCX към резервация (до 10 MB), достъп само за собственик и служители.
- **Игрища → Преглед:** снимкова галерия, достъпна за всички; Admin/Manager добавят и изтриват изображения.
- Файловете са извън `wwwroot`, със сървърна валидация, CSRF защита и оптимизация на снимките.

Подробности, ограничения и сценарии за проверка: **[docs/11_Uploads_Media_BG.md](docs/11_Uploads_Media_BG.md)**.

## Други документи

- [Стартиране с Rider](docs/SETUP_RIDER_BG.md)
- [Тестови сценарии](docs/TEST_CASES_BG.md)
- [Роли и сигурност](docs/09_Security_Authentication.md)

### SQL Server (по избор)

В продукция задайте `Database__Provider=SqlServer` и двата connection string-а чрез environment variables. Бизнес базата се инициализира от `database/SportsBooking_Simple.sql`; Identity използва отделна база. Локални пароли и данни не се добавят в GitHub.
