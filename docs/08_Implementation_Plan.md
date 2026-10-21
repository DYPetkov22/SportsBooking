# 8. Implementation Plan — изпълнение

## Етапи
| Етап | Задачи | Проверка |
|---|---|---|
| 1. Анализ | Изисквания, роли, ограничения | Product Requirements / ER |
| 2. База данни | 6 свързани таблици, PK/FK/CHECK/UNIQUE, seed | SQL работи в празна база |
| 3. Backend | EF модели и DbContext-и | CRUD read/write операции |
| 4. MVC UI | Razor Views, таблици, форми | Навигация и валидация |
| 5. Сигурност | Identity, login/register, роли, ownership checks | Проверки за 401/403 и собствени данни |
| 6. Качество | Негативни сценарии, FK/времеви конфликти, код преглед | `docs/TEST_CASES_BG.md` |
| 7. Предаване | README, DOCX/Markdown документация, ER, GitHub | Репото се отваря в Rider и минава CI build |

## Разпределение на екип от двама
- Студент 1: SQL Server, ER схема, модели, бизнес ограничения.
- Студент 2: MVC контролери, Razor изгледи, регистрация/роли.
- Заедно: integration testing, документация, GitHub.

## Локална проверка
```powershell
dotnet --version
dotnet restore SportsBooking.sln
dotnet build SportsBooking.sln
dotnet run --project .\SportsBooking.Web\SportsBooking.Web.csproj --launch-profile SportsBooking.Web
```

## Рискове
Липсващ LocalDB, неправилен connection string, неподготвена бизнес база, липсващ HTTPS dev certificate, seed с вече съществуващи записи, грешни роли/права.

## Критерий за готовност
Успешна компилация, стартиране със SQL Server, работещи CRUD операции по права и преминати тестови сценарии. GitHub Actions компилацията не замества локалния тест с база данни.
