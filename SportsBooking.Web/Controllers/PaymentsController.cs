using Microsoft.AspNetCore.Authorization;
using SportsBooking.Web.Security;
using SportsBooking.Web.Services;
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
    private readonly PaymentService _payments;
    private readonly ILogger<PaymentsController> _logger;
    public PaymentsController(SportsBookingContext db, PaymentService payments, ILogger<PaymentsController> logger)
    { _db = db; _payments = payments; _logger = logger; }

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
        if (!ModelState.IsValid)
        {
            await ValidateBooking(item);
            return await InvalidForm(item, "Create");
        }
        try
        {
            var result = await _payments.SaveAsync(item);
            foreach (var error in result.Errors) ModelState.AddModelError(nameof(item.BookingID), error);
            if (!result.Succeeded) return await InvalidForm(item, "Create");
            TempData["Success"] = "Плащането е добавено.";
            return RedirectToAction(nameof(Index));
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Payment database operation failed");
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
        if (!ModelState.IsValid)
        {
            await ValidateBooking(item, id);
            return await InvalidForm(item, "Edit", id);
        }
        try
        {
            var result = await _payments.SaveAsync(item, id);
            if (result.NotFound) return NotFound();
            foreach (var error in result.Errors) ModelState.AddModelError(nameof(item.BookingID), error);
            if (!result.Succeeded) return await InvalidForm(item, "Edit", id);
            TempData["Success"] = "Плащането е редактирано.";
            return RedirectToAction(nameof(Index));
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Payment database operation failed");
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
        foreach (var error in await _payments.ValidateAsync(item, excludeId))
            ModelState.AddModelError(nameof(item.BookingID), error);
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
