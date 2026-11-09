using Microsoft.AspNetCore.Mvc.Rendering;
using SportsBooking.Web.Models;

namespace SportsBooking.Web.ViewModels;

// GET filters remain in the query string, including on pagination links.
public sealed class FieldSearchViewModel
{
    public string? Search { get; set; }
    public int? SportId { get; set; }
    public int? CenterId { get; set; }
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }
    public string Sort { get; set; } = "name";
    public int Page { get; set; } = 1;
    public int PageSize { get; } = 10;
    public int TotalCount { get; set; }
    public int TotalPages => Math.Max(1, (int)Math.Ceiling(TotalCount / (double)PageSize));
    public List<Field> Items { get; set; } = [];
    public List<SelectListItem> Sports { get; set; } = [];
    public List<SelectListItem> Centers { get; set; } = [];
}
