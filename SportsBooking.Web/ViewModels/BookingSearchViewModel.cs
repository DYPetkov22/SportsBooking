using Microsoft.AspNetCore.Mvc.Rendering;
using SportsBooking.Web.Models;

namespace SportsBooking.Web.ViewModels;

public sealed class BookingSearchViewModel
{
    public string? Search { get; set; }
    public int? FieldId { get; set; }
    public string? Status { get; set; }
    public DateOnly? From { get; set; }
    public DateOnly? To { get; set; }
    public string Sort { get; set; } = "date_desc";
    public int Page { get; set; } = 1;
    public int PageSize { get; } = 10;
    public int TotalCount { get; set; }
    public int TotalPages => Math.Max(1, (int)Math.Ceiling(TotalCount / (double)PageSize));
    public List<Booking> Items { get; set; } = [];
    public List<SelectListItem> Fields { get; set; } = [];
}
