# SportsBooking — .NET 9 MVC + Microsoft SQL Server

Учебен проект за резервации на спортни игрища. **Единствената СУБД е Microsoft SQL Server** (включително при разработка). Frontend: Razor MVC с адаптивна визия по high-fidelity Figma прототипа, SVG графики, форми и таблици.

## Стартиране на Windows / Rider (важно)

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
* [Матрица на изискванията и проверките](docs/13_Requirements_Audit_BG.md)
* [ER диаграма](docs/ER_Diagram.svg) и [правила](docs/05_Business_Rules.md)
* [Ръчни тестови сценарии](docs/TEST_CASES_BG.md)
* [Качване на файлове](docs/11_Uploads_Media_BG.md)
* [Дизайн и връзка с Figma](docs/03_Design_Brief.md)
* [Задание 7: функционалности, изчисления и експорти](docs/15_Assignment7_Reports_BG.md)

## 

