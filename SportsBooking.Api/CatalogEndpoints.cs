using SportsBooking.Data;
using SportsBooking.Web.Models;

namespace SportsBooking.Api;

public static class CatalogEndpoints
{
    public static void MapFieldCatalog(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/fields", async (IFieldCatalog catalog, CancellationToken ct, int page = 1) =>
        {
            if (page is < 1 or > 10000)
                return Results.BadRequest(new { error = "Page must be between 1 and 10000." });
            var fields = await catalog.ListAsync(page, 20, ct);
            return Results.Ok(fields.Select(FieldResponse.From));
        });

        app.MapGet("/api/fields/{id:int:min(1)}", async (int id, IFieldCatalog catalog, CancellationToken ct) =>
        {
            var field = await catalog.FindAsync(id, ct);
            return field is null ? Results.NotFound() : Results.Ok(FieldResponse.From(field));
        });
    }
}

internal sealed record FieldResponse(int Id, string Name, string Center, string City, string Sport, decimal PricePerHour)
{
    public static FieldResponse From(Field field) => new(field.FieldID, field.FieldName,
        field.Center!.CenterName, field.Center.City, field.Sport!.SportName, field.PricePerHour);
}
