using Microsoft.EntityFrameworkCore;
using SportsBooking.Web.Data;
using SportsBooking.Web.Models;

namespace SportsBooking.Data;

public interface IFieldCatalog
{
    Task<List<Field>> ListAsync(int page, int pageSize, CancellationToken ct);
    Task<Field?> FindAsync(int id, CancellationToken ct);
}

public sealed class FieldCatalog(SportsBookingContext db) : IFieldCatalog
{
    private IQueryable<Field> Fields => db.Fields.AsNoTracking()
        .Include(field => field.Center).Include(field => field.Sport);

    public Task<List<Field>> ListAsync(int page, int pageSize, CancellationToken ct) =>
        Fields.OrderBy(field => field.FieldName).ThenBy(field => field.FieldID)
            .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);

    public Task<Field?> FindAsync(int id, CancellationToken ct) =>
        Fields.FirstOrDefaultAsync(field => field.FieldID == id, ct);
}
