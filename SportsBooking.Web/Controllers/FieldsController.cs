using Microsoft.AspNetCore.Authorization;
using SportsBooking.Web.Security;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SportsBooking.Web.Data;
using SportsBooking.Web.Models;

namespace SportsBooking.Web.Controllers;

[Authorize(Roles = AppRoles.Staff)]
public class FieldsController : Controller
{
    private readonly SportsBookingContext _db;
    public FieldsController(SportsBookingContext db) => _db = db;

    [AllowAnonymous]
    public async Task<IActionResult> Index()
    {
        var rows = await _db.Fields.Include(x => x.Center).Include(x => x.Sport).AsNoTracking().OrderBy(x => x.FieldName).ToListAsync();
        return View(rows);
    }

    [AllowAnonymous]
    public async Task<IActionResult> Details(int id)
    {
        var item = await _db.Fields.Include(x => x.Center).Include(x => x.Sport).AsNoTracking().FirstOrDefaultAsync(x => x.FieldID == id);
        return item == null ? NotFound() : View(item);
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        await FillLists();
        return View(new Field());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("FieldName,CenterID,SportID,PricePerHour")] Field item)
    {
        if (!await _db.SportsCenters.AnyAsync(x => x.CenterID == item.CenterID))
            ModelState.AddModelError(nameof(item.CenterID), "Изберете съществуващ център.");
        if (!await _db.Sports.AnyAsync(x => x.SportID == item.SportID))
            ModelState.AddModelError(nameof(item.SportID), "Изберете съществуващ спорт.");
        if (!ModelState.IsValid)
        {
            await FillLists();
            return View(item);
        }

        _db.Fields.Add(item);
        try
        {
            await _db.SaveChangesAsync();
            TempData["Success"] = "Записът е добавен.";
            return RedirectToAction(nameof(Index));
        }
        catch (DbUpdateException)
        {
            ModelState.AddModelError("", "Записът не може да бъде добавен. Проверете стойностите.");
            await FillLists();
            return View(item);
        }
    }

    public async Task<IActionResult> Edit(int id)
    {
        var item = await _db.Fields.FindAsync(id);
        if (item == null) return NotFound();
        await FillLists();
        return View(item);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [Bind("FieldName,CenterID,SportID,PricePerHour")] Field item)
    {
        item.FieldID = id;
        if (!await _db.SportsCenters.AnyAsync(x => x.CenterID == item.CenterID))
            ModelState.AddModelError(nameof(item.CenterID), "Изберете съществуващ център.");
        if (!await _db.Sports.AnyAsync(x => x.SportID == item.SportID))
            ModelState.AddModelError(nameof(item.SportID), "Изберете съществуващ спорт.");
        if (!ModelState.IsValid)
        {
            await FillLists();
            return View(item);
        }

        var existing = await _db.Fields.FindAsync(id);
        if (existing == null) return NotFound();
        existing.FieldName = item.FieldName;
        existing.CenterID = item.CenterID;
        existing.SportID = item.SportID;
        existing.PricePerHour = item.PricePerHour;
        try
        {
            await _db.SaveChangesAsync();
            TempData["Success"] = "Промените са запазени.";
            return RedirectToAction(nameof(Index));
        }
        catch (DbUpdateException)
        {
            ModelState.AddModelError("", "Промените не могат да бъдат запазени.");
            await FillLists();
            return View(item);
        }
    }

    public async Task<IActionResult> Delete(int id)
    {
        var item = await _db.Fields.Include(x => x.Center).Include(x => x.Sport).AsNoTracking().FirstOrDefaultAsync(x => x.FieldID == id);
        return item == null ? NotFound() : View(item);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var item = await _db.Fields.FindAsync(id);
        if (item == null) return NotFound();

        _db.Fields.Remove(item);
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

    private async Task FillLists()
    {
        ViewBag.Centers = new SelectList(
            await _db.SportsCenters.OrderBy(x => x.CenterName).ToListAsync(), "CenterID", "CenterName");
        ViewBag.Sports = new SelectList(
            await _db.Sports.OrderBy(x => x.SportName).ToListAsync(), "SportID", "SportName");
    }
}
