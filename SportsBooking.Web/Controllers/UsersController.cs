using Microsoft.AspNetCore.Authorization;
using SportsBooking.Web.Security;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SportsBooking.Web.Data;
using SportsBooking.Web.Models;

namespace SportsBooking.Web.Controllers;

[Authorize(Roles = AppRoles.Admin)]
public class UsersController : Controller
{
    private readonly SportsBookingContext _db;
    private readonly IdentityContext _identity;
    public UsersController(SportsBookingContext db, IdentityContext identity) { _db = db; _identity = identity; }

    public async Task<IActionResult> Index()
    {
        var rows = await _db.Users.AsNoTracking().OrderBy(x => x.FullName).ToListAsync();
        return View(rows);
    }

    public async Task<IActionResult> Details(int id)
    {
        var item = await _db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.UserID == id);
        return item == null ? NotFound() : View(item);
    }

    [HttpGet]
    public IActionResult Create() => View(new User());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("FullName,Email,Phone")] User item)
    {
        if (await _db.Users.AnyAsync(x => x.Email == item.Email))
            ModelState.AddModelError(nameof(item.Email), "Тази стойност вече съществува.");
        if (!ModelState.IsValid)
        {
            return View(item);
        }

        _db.Users.Add(item);
        try
        {
            await _db.SaveChangesAsync();
            TempData["Success"] = "Записът е добавен.";
            return RedirectToAction(nameof(Index));
        }
        catch (DbUpdateException)
        {
            ModelState.AddModelError("", "Записът не може да бъде добавен. Проверете стойностите.");
            return View(item);
        }
    }

    public async Task<IActionResult> Edit(int id)
    {
        var item = await _db.Users.FindAsync(id);
        if (item == null) return NotFound();
        return View(item);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [Bind("FullName,Email,Phone")] User item)
    {
        item.UserID = id;
        if (await _db.Users.AnyAsync(x => x.Email == item.Email && x.UserID != id))
            ModelState.AddModelError(nameof(item.Email), "Тази стойност вече съществува.");
        if (!ModelState.IsValid)
        {
            return View(item);
        }

        var existing = await _db.Users.FindAsync(id);
        if (existing == null) return NotFound();
        if (existing.Email != item.Email && await _identity.Users.AnyAsync(u => u.BookingUserId == id))
        {
            ModelState.AddModelError(nameof(item.Email), "Имейлът на свързан акаунт не може да се променя от тази форма.");
            return View(item);
        }
        existing.FullName = item.FullName;
        existing.Email = item.Email;
        existing.Phone = item.Phone;
        try
        {
            await _db.SaveChangesAsync();
            TempData["Success"] = "Промените са запазени.";
            return RedirectToAction(nameof(Index));
        }
        catch (DbUpdateException)
        {
            ModelState.AddModelError("", "Промените не могат да бъдат запазени.");
            return View(item);
        }
    }

    public async Task<IActionResult> Delete(int id)
    {
        var item = await _db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.UserID == id);
        return item == null ? NotFound() : View(item);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var item = await _db.Users.FindAsync(id);
        if (item == null) return NotFound();

        if (await _identity.Users.AnyAsync(u => u.BookingUserId == id))
        {
            ViewBag.DeleteError = "Този профил има акаунт за вход и не може да бъде изтрит оттук.";
            return View("Delete", item);
        }
        _db.Users.Remove(item);
        try
        {
            await _db.SaveChangesAsync();
            TempData["Success"] = "Записът е изтрит.";
            return RedirectToAction(nameof(Index));
        }
        catch (DbUpdateException)
        {
            ViewBag.DeleteError = "Не може да изтриете записа, защото е свързан с други данни.";
            return View("Delete", item);
        }
    }
}
