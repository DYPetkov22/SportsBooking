using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SportsBooking.Web.Data;
using SportsBooking.Web.Models;
using SportsBooking.Web.Security;

namespace SportsBooking.Web.Controllers;

public class HomeController(SportsBookingContext db, UserManager<ApplicationUser> users) : Controller
{
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        ViewBag.SportsCount = await db.Sports.CountAsync(ct);
        ViewBag.CentersCount = await db.SportsCenters.CountAsync(ct);
        ViewBag.FieldsCount = await db.Fields.CountAsync(ct);
        if (User.Identity?.IsAuthenticated == true)
        {
            var isStaff = User.IsInRole(AppRoles.Admin) || User.IsInRole(AppRoles.Manager);
            var account = await users.GetUserAsync(User);
            IQueryable<Booking> bookings = db.Bookings.AsNoTracking()
                .Include(x => x.User).Include(x => x.Field);
            if (!isStaff)
                bookings = account?.BookingUserId is int profileId
                    ? bookings.Where(x => x.UserID == profileId) : bookings.Where(x => false);
            var today = DateOnly.FromDateTime(DateTime.Today);
            ViewBag.BookingsToday = await bookings.CountAsync(x =>
                x.BookingDate == today && x.Status == "Активна", ct);
            ViewBag.RecentBookings = await bookings.OrderByDescending(x => x.BookingDate)
                .ThenByDescending(x => x.StartTime).Take(5).ToListAsync(ct);
        }
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        Response.StatusCode = 500;
        return View("~/Views/Errors/ServerError.cshtml", 500);
    }
}
