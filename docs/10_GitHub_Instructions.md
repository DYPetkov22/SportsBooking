# Публикуване на SportsBooking в GitHub

## Чрез браузъра (без Git терминал)
1. Изтегли и разархивирай готовия ZIP.
2. В GitHub избери **New repository** и име например `SportsBooking-MVC`.
3. Не добавяй автоматично README или `.gitignore` в празното repo — вече са включени.
4. Избери **uploading an existing file** и качи **файловете и папките вътре** в разархивирания корен, а не самия ZIP.
5. Натисни **Commit changes**.

## Чрез Git/терминал в Rider
```powershell
git init
git add .
git commit -m "Initial commit: SportsBooking MVC .NET 9"
git branch -M main
git remote add origin https://github.com/YOUR_USERNAME/SportsBooking-MVC.git
git push -u origin main
```
Заменете `YOUR_USERNAME` и името на репозитория. Ако той вече съдържа README/commits, не използвайте сляпо тези команди — първо сравнете състоянието.

## Какво се качва
`SportsBooking.sln`, `SportsBooking.Web/`, `database/`, `docs/`, `design/`, `.gitignore`, `.github/workflows/dotnet.yml`.

## Какво НЕ се качва
`bin/`, `obj/`, `.idea/`, `.env`, пароли, реални connection credentials, локални `.mdf`/`.ldf` бази, сертификати. DB схемата се възстановява от SQL файла, не от database binaries.

## CI
Workflow `.github/workflows/dotnet.yml` компилира с .NET 9 след push/PR. Не стартира SQL Server; функционалните CRUD проверки се правят локално.
