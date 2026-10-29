using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SportsBooking.Web.Data;
using SportsBooking.Web.Models;
using SportsBooking.Web.Security;
using SportsBooking.Web.ViewModels;

namespace SportsBooking.Web.Controllers;

public class AccountController : Controller
{
    private readonly UserManager<ApplicationUser> _users;
    private readonly SignInManager<ApplicationUser> _signIn;
    private readonly SportsBookingContext _db;
    public AccountController(UserManager<ApplicationUser> users, SignInManager<ApplicationUser> signIn,
        SportsBookingContext db) { _users = users; _signIn = signIn; _db = db; }

    [AllowAnonymous, HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        ViewBag.ReturnUrl = Url.IsLocalUrl(returnUrl) ? returnUrl : null;
        return View(new LoginViewModel());
    }

    [AllowAnonymous, HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
    {
        ViewBag.ReturnUrl = Url.IsLocalUrl(returnUrl) ? returnUrl : null;
        if (!ModelState.IsValid) return View(model);
        var account = await _users.FindByEmailAsync(model.Email);
        if (account != null)
        {
            var result = await _signIn.PasswordSignInAsync(account, model.Password, model.RememberMe, lockoutOnFailure: true);
            if (result.Succeeded) return LocalRedirect(Url.IsLocalUrl(returnUrl) ? returnUrl : Url.Action("Index", "Home")!);
            if (result.IsLockedOut)
            {
                ModelState.AddModelError("", "Твърде много опити. Опитайте след 10 минути.");
                return View(model);
            }
        }
        ModelState.AddModelError("", "Невалиден имейл или парола.");
        return View(model);
    }

    [AllowAnonymous, HttpGet]
    public IActionResult Register() => View(new RegisterViewModel());

    [AllowAnonymous, HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid) return View(model);
        var email = model.Email.Trim();
        if (await _users.FindByEmailAsync(email) != null || await _db.Users.AnyAsync(u => u.Email == email))
        {
            ModelState.AddModelError(nameof(model.Email), "Имейлът вече се използва. Ако имате стар профил, свържете се с администратор.");
            return View(model);
        }

        // Само нов профил. НЕ присвояваме стар Users ред по имейл.
        var profile = new User { FullName = model.FullName.Trim(), Email = email, Phone = model.Phone };
        try { _db.Users.Add(profile); await _db.SaveChangesAsync(); }
        catch (DbUpdateException)
        {
            ModelState.AddModelError(nameof(model.Email), "Профилът не може да бъде създаден.");
            return View(model);
        }

        var user = new ApplicationUser { UserName = email, Email = email, BookingUserId = profile.UserID };
        IdentityResult created;
        try { created = await _users.CreateAsync(user, model.Password); }
        catch
        {
            _db.Users.Remove(profile); await _db.SaveChangesAsync();
            ModelState.AddModelError("", "Регистрацията не е успешна.");
            return View(model);
        }
        if (!created.Succeeded)
        {
            _db.Users.Remove(profile); await _db.SaveChangesAsync();
            foreach (var err in created.Errors) ModelState.AddModelError("", err.Description);
            return View(model);
        }
        var roleResult = await _users.AddToRoleAsync(user, AppRoles.User);
        if (!roleResult.Succeeded)
        {
            await _users.DeleteAsync(user);
            _db.Users.Remove(profile); await _db.SaveChangesAsync();
            ModelState.AddModelError("", "Ролята на профила не може да бъде запазена.");
            return View(model);
        }
        await _signIn.SignInAsync(user, isPersistent: false);
        return RedirectToAction("Index", "Home");
    }

    [Authorize, HttpGet]
    public async Task<IActionResult> Profile()
    {
        var account = await _users.GetUserAsync(User);
        if (account?.BookingUserId is not int profileId)
        {
            TempData["Error"] = "Този акаунт няма свързан потребителски профил.";
            return RedirectToAction("Index", "Home");
        }
        var profile = await _db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.UserID == profileId);
        return profile == null ? NotFound() : View(profile);
    }

    [Authorize, HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await _signIn.SignOutAsync();
        return RedirectToAction("Index", "Home");
    }

    [AllowAnonymous]
    public IActionResult AccessDenied() => View();
}
