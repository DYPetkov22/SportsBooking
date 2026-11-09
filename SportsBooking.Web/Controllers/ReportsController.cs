using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SportsBooking.Web.Reporting;
using SportsBooking.Web.Security;

namespace SportsBooking.Web.Controllers;

[Authorize(Roles = AppRoles.Staff)]
public sealed class ReportsController(ReportsService reports) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(DateOnly? from, DateOnly? to, CancellationToken ct)
    {
        if (!ResolvePeriod(from, to, out var start, out var end))
            return BadRequest("Невалиден период. Изберете най-много 24 последователни месеца.");
        return View(await reports.LoadAsync(start, end, ct));
    }

    [HttpGet]
    public async Task<IActionResult> ExportPdf(DateOnly? from, DateOnly? to, CancellationToken ct)
    {
        if (!ResolvePeriod(from, to, out var start, out var end)) return BadRequest("Невалиден период.");
        var data = await reports.LoadAsync(start, end, ct);
        return File(ReportExports.ToPdf(data), "application/pdf", $"sportsbooking-report-{DateTime.UtcNow:yyyyMMdd}.pdf");
    }

    [HttpGet]
    public async Task<IActionResult> ExportExcel(DateOnly? from, DateOnly? to, CancellationToken ct)
    {
        if (!ResolvePeriod(from, to, out var start, out var end)) return BadRequest("Невалиден период.");
        var data = await reports.LoadAsync(start, end, ct);
        return File(ReportExports.ToExcel(data),
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"sportsbooking-report-{DateTime.UtcNow:yyyyMMdd}.xlsx");
    }

    private static bool ResolvePeriod(DateOnly? from, DateOnly? to, out DateOnly start, out DateOnly end)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var thisMonth = new DateOnly(today.Year, today.Month, 1);
        start = from ?? thisMonth.AddMonths(-11);
        end = to ?? thisMonth.AddMonths(1).AddDays(-1);
        // max 24 months of reports, and avoid overflow from AddDays(1).
        return start > DateOnly.MinValue && end.Year < 9999 && start <= end &&
               (end.Year - start.Year) * 12 + end.Month - start.Month < 24;
    }
}
