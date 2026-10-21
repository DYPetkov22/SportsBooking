using Microsoft.AspNetCore.Authorization;
using SportsBooking.Web.Security;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SportsBooking.Web.Data;
using SportsBooking.Web.Models;

namespace SportsBooking.Web.Controllers;

[Authorize(Roles = AppRoles.Staff)]
public class SportsCentersController : Controller
{
    private readonly SportsBookingContext _db;
    public SportsCentersController(SportsBookingContext db) => _db = db;

    [AllowAnonymous]
    public async Task<IActionResult> Index()
    {
        var rows = await _db.SportsCenters.AsNoTracking().OrderBy(x => x.CenterName).ToListAsync();
        return View(rows);
    }

    [AllowAnonymous]
    public async Task<IActionResult> Details(int id)
    {
        var item = await _db.SportsCenters.AsNoTracking().FirstOrDefaultAsync(x => x.CenterID == id);
        return item == null ? NotFound() : View(item);
    }

    [HttpGet]
    public IActionResult Create() => View(new SportsCenter());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("CenterName,City")] SportsCenter item)
    {
        if (!ModelState.IsValid)
        {
            return View(item);
        }

        _db.SportsCenters.Add(item);
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
        var item = await _db.SportsCenters.FindAsync(id);
        if (item == null) return NotFound();
        return View(item);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [Bind("CenterName,City")] SportsCenter item)
    {
        item.CenterID = id;
        if (!ModelState.IsValid)
        {
            return View(item);
        }

        var existing = await _db.SportsCenters.FindAsync(id);
        if (existing == null) return NotFound();
        existing.CenterName = item.CenterName;
        existing.City = item.City;
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
        var item = await _db.SportsCenters.AsNoTracking().FirstOrDefaultAsync(x => x.CenterID == id);
        return item == null ? NotFound() : View(item);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var item = await _db.SportsCenters.FindAsync(id);
        if (item == null) return NotFound();

        _db.SportsCenters.Remove(item);
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
