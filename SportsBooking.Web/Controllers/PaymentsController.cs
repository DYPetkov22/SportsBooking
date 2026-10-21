using Microsoft.AspNetCore.Authorization;
using SportsBooking.Web.Security;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SportsBooking.Web.Data;
using SportsBooking.Web.Models;

namespace SportsBooking.Web.Controllers;

[Authorize(Roles = AppRoles.Staff)]
public class PaymentsController : Controller
{
    private readonly SportsBookingContext _db;
    public PaymentsController(SportsBookingContext db) => _db = db;

    public async Task<IActionResult> Index()
    {
        var rows = await _db.Payments.Include(x => x.Booking).ThenInclude(x => x!.User)
            .AsNoTracking().OrderByDescending(x => x.PaymentID).ToListAsync();
        return View(rows);
    }

    public async Task<IActionResult> Details(int id)
    {
        var item = await _db.Payments.Include(x => x.Booking).ThenInclude(x => x!.User)
            .AsNoTracking().FirstOrDefaultAsync(x => x.PaymentID == id);
        return item == null ? NotFound() : View(item);
    }

    public async Task<IActionResult> Create()
    {
        await FillLists();
        return View(new Payment());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("BookingID,Amount,PaymentMethod")] Payment item)
    {
        await ValidateBooking(item);
        if (!ModelState.IsValid) return await InvalidForm(item, "Create");
        _db.Payments.Add(item);
        try
        {
            await _db.SaveChangesAsync();
            TempData["Success"] = "Плащането е добавено.";
            return RedirectToAction(nameof(Index));
        }
        catch (DbUpdateException)
        {
            ModelState.AddModelError("", "Записът не може да бъде създаден. Възможно е вече да има плащане.");
            return await InvalidForm(item, "Create");
        }
    }

    public async Task<IActionResult> Edit(int id)
    {
        var item = await _db.Payments.FindAsync(id);
        if (item == null) return NotFound();
        await FillLists();
        return View(item);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [Bind("BookingID,Amount,PaymentMethod")] Payment item)
    {
        await ValidateBooking(item, id);
        if (!ModelState.IsValid) return await InvalidForm(item, "Edit", id);
        var existing = await _db.Payments.FindAsync(id);
        if (existing == null) return NotFound();
        existing.BookingID = item.BookingID;
        existing.Amount = item.Amount;
        existing.PaymentMethod = item.PaymentMethod;
        try
        {
            await _db.SaveChangesAsync();
            TempData["Success"] = "Плащането е редактирано.";
            return RedirectToAction(nameof(Index));
        }
        catch (DbUpdateException)
        {
            ModelState.AddModelError("", "Плащането не може да бъде редактирано.");
            return await InvalidForm(item, "Edit", id);
        }
    }

    public async Task<IActionResult> Delete(int id)
    {
        var item = await _db.Payments.Include(x => x.Booking)
            .AsNoTracking().FirstOrDefaultAsync(x => x.PaymentID == id);
        return item == null ? NotFound() : View(item);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var item = await _db.Payments.FindAsync(id);
        if (item == null) return NotFound();
        _db.Payments.Remove(item);
        await _db.SaveChangesAsync();
        TempData["Success"] = "Плащането е изтрито.";
        return RedirectToAction(nameof(Index));
    }

    private async Task ValidateBooking(Payment item, int excludeId = 0)
    {
        if (!await _db.Bookings.AnyAsync(x => x.BookingID == item.BookingID))
            ModelState.AddModelError(nameof(item.BookingID), "Няма такава резервация.");
        if (await _db.Payments.AnyAsync(x => x.BookingID == item.BookingID && x.PaymentID != excludeId))
            ModelState.AddModelError(nameof(item.BookingID), "Тази резервация вече има плащане.");
    }

    private async Task<IActionResult> InvalidForm(Payment item, string viewName, int id = 0)
    {
        item.PaymentID = id;
        await FillLists();
        return View(viewName, item);
    }

    private async Task FillLists()
    {
        // Резервациите са означени с номер и дата, за да се различават.
        var bookings = await _db.Bookings.OrderByDescending(x => x.BookingDate).ToListAsync();
        ViewBag.Bookings = new SelectList(bookings.Select(x => new
        {
            Id = x.BookingID,
            Label = $"№{x.BookingID} — {x.BookingDate:dd.MM.yyyy}"
        }), "Id", "Label");
    }
}
