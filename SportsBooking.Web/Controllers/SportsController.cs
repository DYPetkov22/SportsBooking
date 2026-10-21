using Microsoft.AspNetCore.Authorization;
using SportsBooking.Web.Security;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SportsBooking.Web.Data;
using SportsBooking.Web.Models;

namespace SportsBooking.Web.Controllers;

[Authorize(Roles = AppRoles.Staff)]
public class SportsController : Controller
{
    private readonly SportsBookingContext _db;
    public SportsController(SportsBookingContext db) => _db = db;

    [AllowAnonymous]
    public async Task<IActionResult> Index()
    {
        var rows = await _db.Sports.AsNoTracking().OrderBy(x => x.SportName).ToListAsync();
        return View(rows);
    }

    [AllowAnonymous]
    public async Task<IActionResult> Details(int id)
    {
        var item = await _db.Sports.AsNoTracking().FirstOrDefaultAsync(x => x.SportID == id);
        return item == null ? NotFound() : View(item);
    }

    [HttpGet]
    public IActionResult Create() => View(new Sport());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("SportName")] Sport item)
    {
        if (await _db.Sports.AnyAsync(x => x.SportName == item.SportName))
            ModelState.AddModelError(nameof(item.SportName), "Тази стойност вече съществува.");
        if (!ModelState.IsValid)
        {
            return View(item);
        }

        _db.Sports.Add(item);
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
        var item = await _db.Sports.FindAsync(id);
        if (item == null) return NotFound();
        return View(item);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [Bind("SportName")] Sport item)
    {
        item.SportID = id;
        if (await _db.Sports.AnyAsync(x => x.SportName == item.SportName && x.SportID != id))
            ModelState.AddModelError(nameof(item.SportName), "Тази стойност вече съществува.");
        if (!ModelState.IsValid)
        {
            return View(item);
        }

        var existing = await _db.Sports.FindAsync(id);
        if (existing == null) return NotFound();
        existing.SportName = item.SportName;
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
        var item = await _db.Sports.AsNoTracking().FirstOrDefaultAsync(x => x.SportID == id);
        return item == null ? NotFound() : View(item);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var item = await _db.Sports.FindAsync(id);
        if (item == null) return NotFound();

        _db.Sports.Remove(item);
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
