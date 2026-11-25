# SportsBooking — .NET 9 MVC + Microsoft SQL Server

Учебен проект за резервации на спортни игрища. **Единствената СУБД е Microsoft SQL Server** (включително при разработка). Frontend: Razor MVC с адаптивна визия, SVG графики, форми и таблици. Текущият дизайн е описан в docs/18_Design_Revision.md.

## Стартиране на Windows / Rider (важно)

Базите данни са включени като отделен **SQL Server проект** в [database/](database/README.md): [SportsBooking.Database.sln](database/SportsBooking.Database.sln) съдържа схемите за резервации и Identity, ограниченията, индексите и примерните данни. Цялата папка се commit-ва заедно с приложението в GitHub.

При първо стартиране след клониране инсталирай SQL Server LocalDB и изпълни от основната папка:

```powershell
dotnet tool install --global Microsoft.SqlPackage
powershell -ExecutionPolicy Bypass -File .\database\Deploy.ps1
```

След това стартирай приложението:

```powershell
dotnet restore SportsBooking.sln
dotnet build SportsBooking.sln
dotnet run --project SportsBooking.Web --launch-profile SportsBooking.Web
```

### Демо акаунти (локално / Development)

|Роля|Имейл|Парола|
|-|-|-|
|Admin|`admin@sportsbooking.local`|`AdminDemo#2026!`|
|Manager|`manager@sportsbooking.local`|`ManagerDemo#2026!`|

## Документи

* [Инсталиране на SQL Server, LocalDB и Rider](docs/SQL_SERVER_SETUP_BG.md)
* [Отделен SQL Server проект и създаване на базите след клониране](database/README.md)
* [Матрица на изискванията и проверките](docs/13_Requirements_Audit_BG.md)
* [ER диаграма](docs/ER_Diagram.svg) и [правила](docs/05_Business_Rules.md)
* [Ръчни тестови сценарии](docs/TEST_CASES_BG.md)
* [Качване на файлове](docs/11_Uploads_Media_BG.md)
* [Дизайн и връзка с Figma](docs/03_Design_Brief.md)
* [Задание 7: функционалности, изчисления и експорти](docs/15_Assignment7_Reports_BG.md)

## 


## Задания 8 и 9

Добавени са глобална обработка на грешки, страници 404/500, дневни файлови логове и транзакционна защита на плащанията. Отделният xUnit проект съдържа 38 теста, от които 27 за бизнес услуги с Moq.

```powershell
dotnet test SportsBooking.sln
```

Подробности, променени файлове и ограничения на проверката: [Задания 8 и 9](docs/16_Assignments8_9_BG.md).

## Обновен дизайн

Топъл светъл фон, керемиденочервен акцент, спортна фотография и горна навигация. Офлайн преглед: отворете `docs/Design_Preview.html` в браузър. Промените, проверките и връзката с диаграмите са описани в [Design revision](docs/18_Design_Revision.md).

## REST API и конзолен клиент (.NET 9)

API използва бизнес базата от `database/01_SportsBooking_SQLServer.sql`. При различен SQL Server задайте `ConnectionStrings:SportsBooking` в `SportsBooking.Api/appsettings.json`. От директорията на решението стартирайте API:

```shell
dotnet run --project SportsBooking.Api
```

Във втори терминал изпълнете:

```shell
dotnet run --project SportsBooking.Client
dotnet run --project SportsBooking.Client -- page 2
dotnet run --project SportsBooking.Client -- field 1
```

Клиентът чете JSON от `http://localhost:5180`. За друг адрес задайте `SPORTSBOOKING_API_URL`. Публичният каталог поддържа `GET /api/fields?page=1` (20 терена на страница) и `GET /api/fields/{id}` (404 при липсващ терен). Административните операции остават в MVC приложението.

Repository Pattern е приложен чрез `IFieldCatalog`. MVC страницата за детайли и API използват общата EF Core заявка в `FieldCatalog`. Dependency Injection подава реализацията за текущата заявка. Същият подход при плащанията позволява тестовете да заменят хранилището с Moq.
