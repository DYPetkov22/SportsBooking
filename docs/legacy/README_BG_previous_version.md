# SportsBooking – .NET 9 MVC + ASP.NET Core Identity + Roles

Това е **надграденият** университетски проект за резервация на спортни игрища. Кодът използва ASP.NET Core MVC, Entity Framework Core 9, SQL Server и ASP.NET Core Identity (регистрация, вход, изход, роли).

## Изисквания

- Visual Studio 2022 с поддръжка на .NET 9 (или .NET 9 SDK + `dotnet` CLI).
- SQL Server LocalDB или SQL Server Express.
- Достъп до NuGet за първоначалното `dotnet restore`.
- HTTPS при стартиране (включен е `CookieSecurePolicy.Always`).

## Стартиране – стъпка по стъпка

1. Отвори `SportsBooking.sln` във Visual Studio.
2. В SQL Server Management Studio изпълни веднъж `database/SportsBooking_Simple.sql`. Той създава `SportsBookingDB_Simple` и първоначалните **шест** таблици.
3. Провери `SportsBooking.Web/appsettings.json`:
   - `SportsBooking` = връзка към оригиналната база `SportsBookingDB_Simple`;
   - `Identity` = връзка към нова база `SportsBookingIdentityDB` (същият локален SQL Server).
4. При първо стартиране Identity базата и таблиците `AspNetUsers`, `AspNetRoles` и др. ще се създадат **автоматично** с EF Core `EnsureCreatedAsync`. Не е необходим втори SQL скрипт.
5. За първоначален **Admin** задай секретите само в environment variables, не ги записвай в Git или `appsettings.json`:

   PowerShell (стартиране от същия терминал):

   ```powershell
   cd SportsBooking.Web
   $env:SPORTSBOOKING_ADMIN_EMAIL = "admin@example.com"
   $env:SPORTSBOOKING_ADMIN_PASSWORD = "Replace-With-A-Strong-Password1!"
   dotnet run --launch-profile SportsBooking.Web
   ```

   **Замени** примерната парола с твоя собствена. Не използвай горната парола за реален акаунт. При Visual Studio F5 задай тези променливи в средата на Visual Studio или стартирай IDE от терминала, в който са зададени. `admin@example.com` е примерен адрес, не истински акаунт.

6. Отвори HTTPS адреса, показан от приложението. Влез с admin имейла и паролата, които си задал. Admin се създава само ако няма такъв имейл; съществуващ не-Admin профил **не се повишава автоматично**.
7. Като Admin отвори **Роли**, за да назначиш Manager на вече регистриран потребител.

**Важно:** Променливите за Admin се четат при стартиране. При последващи стартирания могат да бъдат премахнати; съществуващият Admin остава в Identity базата.

## Разпределение на правата

| Обект/операция | Гост | User | Manager | Admin |
|---|---:|---:|---:|---:|
| Начало, каталог игрища/спортове/центрове | Да | Да | Да | Да |
| Регистрация, вход | Да | Да | Да | Да |
| Добавяне на собствена резервация | Не | Да | Да* | Да* |
| Преглед на резервации | Не | Само свои | Всички | Всички |
| Отказ на резервация | Не | Само своя | Всички | Всички |
| Редакция/изтриване на резервации | Не | Не | Да | Да |
| CRUD за игрища, спортове, центрове | Не | Само преглед | Да | Да |
| CRUD за плащания | Не | Не | Да | Да |
| CRUD на бизнес таблицата `Users` | Не | Не | Не | Да |
| Назначаване на роли | Не | Не | Не | Да |

\* Manager/Admin избират профил от списъка с потребители; Admin може да няма собствен `BookingUserId`.

## Къде се намира кодът

- `Program.cs`: регистрира EF Core контексти, Identity, cookie, валидатори, middleware.
- `Data/SportsBookingContext.cs`: оригинални 6 SQL таблици (без промени в схемата).
- `Data/IdentityContext.cs`: ASP.NET Identity (отделна база).
- `Models/ApplicationUser.cs`: `BookingUserId` = ID на бизнес профил от `Users`.
- `Controllers/AccountController.cs`: регистрация / вход / изход / отказан достъп.
- `Controllers/RolesController.cs`: Admin променя роли User / Manager / Admin.
- `Controllers/BookingsController.cs`: проверка на собственик, ограничения по роли, срещу припокривания.
- `Security/IdentitySeeder.cs`: създаване на ролите и Admin от environment variables.
- `Views/Account` и `Views/Roles`: новите Razor изгледи.

## Оригиналната Users таблица и новият Identity акаунт

`Users` е таблица с бизнес данни за клиент. `AspNetUsers` е **различна** таблица за вход/парола и роля. Identity `ApplicationUser.BookingUserId` свързва новорегистрирания акаунт с неговия ред в `Users`.

Първоначалните записи `ivan@example.com`, `maria@example.com` и `georgi@example.com` **нямат пароли** и **не могат да влизат**. За безопасност регистрация със съществуващ бизнес имейл се блокира, а не се присвоява чужд профил. За тестване регистрирай нови имейли, например `student@example.com`.

## Сигурност

- Password hashing / солиране от Identity; приложението не пази пароли в обикновен текст.
- Lockout след 5 грешни опита за 10 минути.
- Cookie `HttpOnly`, `Secure`, `SameSite=Lax`; HTTPS.
- CSRF защита за всички POST/PUT/PATCH/DELETE MVC заявки (`AutoValidateAntiforgeryTokenAttribute`).
- `[Authorize(Roles = "...")]` на контролери и actions – не само скрити бутони.
- UserID на обикновен потребител се задава **само от сървъра** при създаване на резервация.
- Филтриране по `BookingUserId` и server-side проверка на собственост за детайли и отказ.
- Login return URL се проверява с `Url.IsLocalUrl` (защита срещу open redirect).
- Администраторът не може да смени собствената си роля през интерфейса.

## Ограничения (учебен проект, не за реално внедряване)

- Няма изпращане на имейли, **няма потвърждение на имейл** и няма forgot-password/reset workflow. Не използвайте непотвърдени акаунти за реално разплащане.
- Няма реален платежен оператор — `Payments` са ръчно въведени записи.
- Две отделни бази означават, че регистрацията не е обща транзакция; при Identity грешка бизнес профилът се изтрива като компенсация, но при внезапно прекъсване може да остане осиротял запис.
- `EnsureCreatedAsync` е удобно само за първоначалната Identity схема в учебни условия; за продукционни промени използвайте EF Core migrations.
- Identity и бизнес данните са разделени; свързването с `BookingUserId` е логическо, а не междубазово FK.
- Регистрацията не изисква потвърждение на имейл, затова **не внедрявайте публично** без email verification, политики за поверителност и допълнителни проверки.
- Не е направено реално изпълнение на `dotnet build` в тази среда (липсва .NET SDK). Направете компилация и изпълнете тестовете във Visual Studio.

## Компилация

```bash
dotnet restore SportsBooking.sln
dotnet build SportsBooking.sln
dotnet run --project SportsBooking.Web --launch-profile SportsBooking.Web
```

Вижте `TEST_CASES_BG.md` за ръчен тест на правата и сигурността.
