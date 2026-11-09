using Microsoft.AspNetCore.Authorization;
using SportsBooking.Web.Security;
using SportsBooking.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SportsBooking.Web.Data;
using SportsBooking.Web.Models;
using SportsBooking.Web.ViewModels;

namespace SportsBooking.Web.Controllers;

[Authorize(Roles = AppRoles.Staff)]
public class FieldsController : Controller
{
    private readonly SportsBookingContext _db;
    private readonly LocalFileStorage _storage;
    public FieldsController(SportsBookingContext db, LocalFileStorage storage) { _db = db; _storage = storage; }

    [AllowAnonymous]
    public async Task<IActionResult> Index([FromQuery] FieldSearchViewModel filters, CancellationToken ct)
    {
        filters.Page = Math.Clamp(filters.Page, 1, 10000);
        filters.Search = filters.Search?.Trim();
        if (filters.Search?.Length > 100) filters.Search = filters.Search[..100];
        if (filters.MinPrice is < 0) filters.MinPrice = 0;
        if (filters.MaxPrice is < 0) filters.MaxPrice = 0;
        var query = _db.Fields.AsNoTracking().Include(x => x.Center).Include(x => x.Sport).AsQueryable();
        if (!string.IsNullOrWhiteSpace(filters.Search))
            query = query.Where(x => x.FieldName.Contains(filters.Search) ||
                x.Center!.CenterName.Contains(filters.Search) || x.Sport!.SportName.Contains(filters.Search));
        if (filters.SportId is > 0) query = query.Where(x => x.SportID == filters.SportId);
        if (filters.CenterId is > 0) query = query.Where(x => x.CenterID == filters.CenterId);
        if (filters.MinPrice.HasValue) query = query.Where(x => x.PricePerHour >= filters.MinPrice.Value);
        if (filters.MaxPrice.HasValue) query = query.Where(x => x.PricePerHour <= filters.MaxPrice.Value);

        filters.TotalCount = await query.CountAsync(ct);
        query = filters.Sort switch
        {
            "name_desc" => query.OrderByDescending(x => x.FieldName).ThenBy(x => x.FieldID),
            "price_asc" => query.OrderBy(x => x.PricePerHour).ThenBy(x => x.FieldID),
            "price_desc" => query.OrderByDescending(x => x.PricePerHour).ThenBy(x => x.FieldID),
            "sport" => query.OrderBy(x => x.Sport!.SportName).ThenBy(x => x.FieldName),
            _ => query.OrderBy(x => x.FieldName).ThenBy(x => x.FieldID)
        };
        filters.Items = await query.Skip((filters.Page - 1) * filters.PageSize)
            .Take(filters.PageSize).ToListAsync(ct);
        filters.Sports = await _db.Sports.AsNoTracking().OrderBy(x => x.SportName)
            .Select(x => new SelectListItem(x.SportName, x.SportID.ToString())).ToListAsync(ct);
        filters.Centers = await _db.SportsCenters.AsNoTracking().OrderBy(x => x.CenterName)
            .Select(x => new SelectListItem(x.CenterName, x.CenterID.ToString())).ToListAsync(ct);
        return View(filters);
    }

    [AllowAnonymous]
    public async Task<IActionResult> Details(int id)
    {
        var item = await _db.Fields.Include(x => x.Center).Include(x => x.Sport).AsNoTracking().FirstOrDefaultAsync(x => x.FieldID == id);
        if (item == null) return NotFound();
        ViewBag.Images = await _storage.ListFieldImagesAsync(id, HttpContext.RequestAborted);
        return View(item);
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
            await _storage.DeleteAllFieldImagesAsync(id, HttpContext.RequestAborted);
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
