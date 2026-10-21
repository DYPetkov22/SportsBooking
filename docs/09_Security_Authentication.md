# 9. Автентикация и роли

SportsBooking използва `AddIdentity<ApplicationUser, IdentityRole>()`, отделна `IdentityContext` база, login/logout cookie и роли `Admin`, `Manager`, `User`. Всички CRUD екрани се ограничават на ниво Controller/Action с `[Authorize]`. На сървъра се проверява ownership на резервациите за User.

## Password и cookie политики
- Парола: минимум 10 символа, главна и малка буква, цифра и специален символ.
- 5 грешни опита → блокиране за 10 минути.
- Cookie: `Secure`, `HttpOnly`, `SameSite=Lax`, 8 часа, sliding expiration.
- Anti-forgery token за изменящи MVC заявки.

## Първи администратор
Environment variables `SPORTSBOOKING_ADMIN_EMAIL` и `SPORTSBOOKING_ADMIN_PASSWORD` се четат при стартиране. В GitHub не се качват истински стойности. Admin се създава само за нов имейл; съществуващ обикновен акаунт не се повишава автоматично.

## Ограничения за обществен достъп
Няма email confirmation/password reset, MFA, audit trail, външен платежен оператор; от гледна точка на сигурност това остава **учебен проект**, не приложение за публикуване с истински плащания/данни.
