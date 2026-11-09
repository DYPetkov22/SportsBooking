namespace SportsBooking.Web.ViewModels;

public sealed class ReportsDashboardViewModel
{
    public DateOnly From { get; init; }
    public DateOnly To { get; init; }
    public int TotalBookings { get; set; }
    public int ActiveBookings { get; set; }
    public int CancelledBookings { get; set; }
    public int TotalUsers { get; set; }
    public int TotalFields { get; set; }
    public int PaymentsCount { get; set; }
    public decimal TotalRevenue { get; set; }
    public List<MonthlyReportRow> Months { get; init; } = new();
    public List<TopSportReportRow> TopSports { get; init; } = new();
    public List<PaymentMethodReportRow> PaymentMethods { get; init; } = new();
    public int MaxMonthlyBookings => Math.Max(1, Months.Count == 0 ? 0 : Months.Max(x => x.Bookings));
    public decimal MaxMonthlyRevenue => Math.Max(1m, Months.Count == 0 ? 0 : Months.Max(x => x.Revenue));
    public int MaxSportBookings => Math.Max(1, TopSports.Count == 0 ? 0 : TopSports.Max(x => x.Bookings));
}

public sealed record MonthlyReportRow(int Year, int Month, int Bookings, int ActiveBookings, int CancelledBookings, decimal Revenue)
{
    public string Label => $"{Month:00}.{Year}";
}
public sealed record TopSportReportRow(string Sport, int Bookings);
public sealed record PaymentMethodReportRow(string Method, int Count, decimal Amount);
