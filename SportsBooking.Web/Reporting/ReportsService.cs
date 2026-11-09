using Microsoft.EntityFrameworkCore;
using SportsBooking.Web.Data;
using SportsBooking.Web.ViewModels;

namespace SportsBooking.Web.Reporting;

/// <summary>Only server-side aggregate queries are executed: no complete bookings/payments table is loaded.</summary>
public sealed class ReportsService(SportsBookingContext db)
{
    public async Task<ReportsDashboardViewModel> LoadAsync(DateOnly from, DateOnly to, CancellationToken ct)
    {
        var bookingQuery = db.Bookings.AsNoTracking()
            .Where(b => b.BookingDate >= from && b.BookingDate <= to);
        var paymentStartUtc = from.ToDateTime(TimeOnly.MinValue);
        var paymentEndExclusiveUtc = to.AddDays(1).ToDateTime(TimeOnly.MinValue);
        var paymentQuery = db.Payments.AsNoTracking()
            .Where(p => p.PaidAtUtc >= paymentStartUtc && p.PaidAtUtc < paymentEndExclusiveUtc);

        var monthlyBookings = await bookingQuery
            .GroupBy(b => new { b.BookingDate.Year, b.BookingDate.Month })
            .Select(g => new { g.Key.Year, g.Key.Month, Count = g.Count(),
                Active = g.Count(x => x.Status == "Активна"),
                Cancelled = g.Count(x => x.Status == "Отказана") })
            .ToListAsync(ct);

        var monthlyPayments = await paymentQuery
            .GroupBy(p => new { p.PaidAtUtc.Year, p.PaidAtUtc.Month })
            .Select(g => new { g.Key.Year, g.Key.Month, Count = g.Count(), Revenue = g.Sum(p => p.Amount) })
            .ToListAsync(ct);

        var topSports = await bookingQuery.Where(x => x.Status == "Активна")
            .GroupBy(x => x.Field!.Sport!.SportName)
            .Select(g => new { Sport = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count).ThenBy(x => x.Sport)
            .Take(8).ToListAsync(ct);

        var paymentMethods = await paymentQuery.GroupBy(p => p.PaymentMethod)
            .Select(g => new { Method = g.Key, Count = g.Count(), Amount = g.Sum(p => p.Amount) })
            .OrderByDescending(x => x.Amount).ToListAsync(ct);

        var result = new ReportsDashboardViewModel { From = from, To = to };
        // Fill missing months with zeros, useful for fair charts and exports.
        for (var month = new DateOnly(from.Year, from.Month, 1);
             month <= to; month = month.AddMonths(1))
        {
            var b = monthlyBookings.FirstOrDefault(x => x.Year == month.Year && x.Month == month.Month);
            var paid = monthlyPayments.FirstOrDefault(x => x.Year == month.Year && x.Month == month.Month);
            result.Months.Add(new MonthlyReportRow(month.Year, month.Month,
                b?.Count ?? 0, b?.Active ?? 0, b?.Cancelled ?? 0, paid?.Revenue ?? 0m));
        }
        result.TotalBookings = monthlyBookings.Sum(x => x.Count);
        result.ActiveBookings = monthlyBookings.Sum(x => x.Active);
        result.CancelledBookings = monthlyBookings.Sum(x => x.Cancelled);
        result.TotalRevenue = monthlyPayments.Sum(x => x.Revenue);
        result.PaymentsCount = monthlyPayments.Sum(x => x.Count);
        result.TotalUsers = await db.Users.AsNoTracking().CountAsync(ct);
        result.TotalFields = await db.Fields.AsNoTracking().CountAsync(ct);
        result.TopSports.AddRange(topSports.Select(x => new TopSportReportRow(x.Sport, x.Count)));
        result.PaymentMethods.AddRange(paymentMethods.Select(x => new PaymentMethodReportRow(x.Method, x.Count, x.Amount)));
        return result;
    }
}
