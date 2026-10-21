using System.Data;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SportsBooking.Web.Data;
using SportsBooking.Web.Models;
using SportsBooking.Web.Security;

namespace SportsBooking.Web.Controllers;

[Authorize]
public class BookingsController : Controller
{
    private readonly SportsBookingContext _db;
    private readonly UserManager<ApplicationUser> _users;
    public BookingsController(SportsBookingContext db, UserManager<ApplicationUser> users)
    { _db = db; _users = users; }

    private bool IsStaff => User.IsInRole(AppRoles.Admin) || User.IsInRole(AppRoles.Manager);
    private async Task<int?> OwnProfileId()
    {
        var user = await _users.GetUserAsync(User);
        return user?.BookingUserId;
    }

    public async Task<IActionResult> Index()
    {
        var rows = _db.Bookings.Include(x => x.User).Include(x => x.Field).AsNoTracking().AsQueryable();
        if (!IsStaff)
        {
            var profileId = await OwnProfileId();
            if (!profileId.HasValue) return Forbid();
            rows = rows.Where(x => x.UserID == profileId.Value);
        }
        return View(await rows.OrderByDescending(x => x.BookingDate).ThenBy(x => x.StartTime).ToListAsync());
    }

    public async Task<IActionResult> Details(int id)
    {
        var item = await _db.Bookings.Include(x => x.User).Include(x => x.Field)
            .Include(x => x.Payment).AsNoTracking().FirstOrDefaultAsync(x => x.BookingID == id);
        if (item == null) return NotFound();
        if (!IsStaff && item.UserID != await OwnProfileId()) return Forbid();
        return View(item);
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        await FillLists();
        return View(new Booking { BookingDate = DateOnly.FromDateTime(DateTime.Today.AddDays(1)),
            StartTime = new TimeOnly(18, 0), EndTime = new TimeOnly(19, 0) });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("UserID,FieldID,BookingDate,StartTime,EndTime,Status")] Booking item)
    {
        if (!IsStaff)
        {
            var profileId = await OwnProfileId();
            if (!profileId.HasValue) return Forbid();
            item.UserID = profileId.Value; // Игнорираме подадения UserID от браузъра.
            item.Status = "Активна";
            ModelState.Remove(nameof(item.UserID));
            ModelState.Remove(nameof(item.Status));
        }
        await ValidateReferences(item);
        if (!ModelState.IsValid) return await InvalidForm(item);
        await using var transaction = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        try
        {
            if (await HasOverlap(item))
            {
                ModelState.AddModelError(nameof(item.StartTime), "Игрището вече е заето в този час.");
                return await InvalidForm(item);
            }
            _db.Bookings.Add(item);
            await _db.SaveChangesAsync();
            await transaction.CommitAsync();
            TempData["Success"] = "Резервацията е добавена.";
            return RedirectToAction(nameof(Index));
        }
        catch (DbUpdateException)
        {
            ModelState.AddModelError("", "Резервацията не може да бъде запазена.");
            return await InvalidForm(item);
        }
    }

    [Authorize(Roles = AppRoles.Staff)]
    public async Task<IActionResult> Edit(int id)
    {
        var item = await _db.Bookings.FindAsync(id);
        if (item == null) return NotFound();
        await FillLists(); return View(item);
    }

    [Authorize(Roles = AppRoles.Staff), HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [Bind("UserID,FieldID,BookingDate,StartTime,EndTime,Status")] Booking item)
    {
        item.BookingID = id;
        await ValidateReferences(item);
        if (!ModelState.IsValid) return await InvalidForm(item);
        await using var transaction = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        try
        {
            var existing = await _db.Bookings.FindAsync(id);
            if (existing == null) return NotFound();
            if (await HasOverlap(item, id))
            {
                ModelState.AddModelError(nameof(item.StartTime), "Игрището вече е заето в този час.");
                return await InvalidForm(item);
            }
            existing.UserID = item.UserID;
            existing.FieldID = item.FieldID;
            existing.BookingDate = item.BookingDate;
            existing.StartTime = item.StartTime;
            existing.EndTime = item.EndTime;
            existing.Status = item.Status;
            await _db.SaveChangesAsync();
            await transaction.CommitAsync();
            TempData["Success"] = "Резервацията е редактирана.";
            return RedirectToAction(nameof(Index));
        }
        catch (DbUpdateException)
        {
            ModelState.AddModelError("", "Резервацията не може да бъде редактирана.");
            return await InvalidForm(item);
        }
    }

    [Authorize(Roles = AppRoles.Staff)]
    public async Task<IActionResult> Delete(int id)
    {
        var item = await _db.Bookings.Include(x => x.User).Include(x => x.Field)
            .AsNoTracking().FirstOrDefaultAsync(x => x.BookingID == id);
        return item == null ? NotFound() : View(item);
    }

    [Authorize(Roles = AppRoles.Staff), HttpPost, ActionName("Delete"), ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var item = await _db.Bookings.FindAsync(id);
        if (item == null) return NotFound();
        _db.Bookings.Remove(item);
        try { await _db.SaveChangesAsync(); TempData["Success"] = "Резервацията е изтрита."; return RedirectToAction(nameof(Index)); }
        catch (DbUpdateException)
        {
            ViewBag.DeleteError = "Първо изтрийте свързаното плащане.";
            return View("Delete", item);
        }
    }

    // Обикновен потребител може само да ОТКАЖЕ своя активна резервация чрез POST.
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(int id)
    {
        var item = await _db.Bookings.FindAsync(id);
        if (item == null) return NotFound();
        if (!IsStaff && item.UserID != await OwnProfileId()) return Forbid();
        if (item.Status != "Активна" || item.BookingDate < DateOnly.FromDateTime(DateTime.Today))
            return BadRequest("Резервацията не може да бъде отказана.");
        item.Status = "Отказана";
        await _db.SaveChangesAsync();
        TempData["Success"] = "Резервацията е отказана.";
        return RedirectToAction(nameof(Index));
    }

    private async Task ValidateReferences(Booking item)
    {
        // MVC skips IValidatableObject when the omitted UserID fails initial validation.
        // Recheck booking rules after assigning the signed-in user's profile.
        if (ModelState.IsValid)
        {
            foreach (var error in item.Validate(new ValidationContext(item)))
                foreach (var member in error.MemberNames)
                    ModelState.AddModelError(member, error.ErrorMessage!);
        }
        if (!await _db.Users.AnyAsync(x => x.UserID == item.UserID))
            ModelState.AddModelError(nameof(item.UserID), "Няма такъв потребител.");
        if (!await _db.Fields.AnyAsync(x => x.FieldID == item.FieldID))
            ModelState.AddModelError(nameof(item.FieldID), "Няма такова игрище.");
    }

    private async Task<bool> HasOverlap(Booking item, int excludeId = 0)
    {
        if (item.Status == "Отказана") return false;
        return await _db.Bookings.AnyAsync(x => x.FieldID == item.FieldID &&
            x.BookingDate == item.BookingDate && x.Status == "Активна" &&
            x.BookingID != excludeId && x.StartTime < item.EndTime && x.EndTime > item.StartTime);
    }
    private async Task<IActionResult> InvalidForm(Booking item)
    { await FillLists(); return View(item.BookingID == 0 ? "Create" : "Edit", item); }
    private async Task FillLists()
    {
        if (IsStaff)
            ViewBag.Users = new SelectList(await _db.Users.OrderBy(x => x.FullName).ToListAsync(), "UserID", "FullName");
        ViewBag.Fields = new SelectList(await _db.Fields.OrderBy(x => x.FieldName).ToListAsync(), "FieldID", "FieldName");
    }
}
