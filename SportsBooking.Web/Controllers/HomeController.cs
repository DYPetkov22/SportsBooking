using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SportsBooking.Web.Data;

namespace SportsBooking.Web.Controllers;

public class HomeController : Controller
{
    private readonly SportsBookingContext _db;
    public HomeController(SportsBookingContext db) => _db = db;

    public async Task<IActionResult> Index()
    {
        ViewBag.SportsCount = await _db.Sports.CountAsync();
        ViewBag.CentersCount = await _db.SportsCenters.CountAsync();
        ViewBag.FieldsCount = await _db.Fields.CountAsync();
        return View();
    }

    public IActionResult Error() => View();
}
