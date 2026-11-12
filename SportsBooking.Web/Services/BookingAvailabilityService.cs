using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using SportsBooking.Web.Data;
using SportsBooking.Web.Models;

namespace SportsBooking.Web.Services;

public interface IBookingAvailabilityStore
{
    Task<bool> AnyAsync(Expression<Func<Booking, bool>> predicate);
}

public sealed class EfBookingAvailabilityStore(SportsBookingContext db) : IBookingAvailabilityStore
{
    public Task<bool> AnyAsync(Expression<Func<Booking, bool>> predicate) => db.Bookings.AnyAsync(predicate);
}

public sealed class BookingAvailabilityService(IBookingAvailabilityStore store)
{
    public Task<bool> HasOverlapAsync(Booking item, int excludeId = 0)
    {
        if (item.Status == "Отказана") return Task.FromResult(false);
        return store.AnyAsync(x => x.FieldID == item.FieldID &&
            x.BookingDate == item.BookingDate && x.Status == "Активна" &&
            x.BookingID != excludeId && x.StartTime < item.EndTime && x.EndTime > item.StartTime);
    }
}
