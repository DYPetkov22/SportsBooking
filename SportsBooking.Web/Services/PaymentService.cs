using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using SportsBooking.Web.Data;
using SportsBooking.Web.Models;

namespace SportsBooking.Web.Services;

public interface IPaymentStore
{
    Task<bool> BookingExistsAsync(int bookingId);
    Task<bool> HasOtherPaymentAsync(int bookingId, int excludeId);
    Task<Payment?> FindAsync(int id);
    void Add(Payment payment);
    Task SaveAsync();
    Task<IDbContextTransaction> BeginTransactionAsync();
}

public sealed class EfPaymentStore(SportsBookingContext db) : IPaymentStore
{
    public Task<bool> BookingExistsAsync(int id) => db.Bookings.AnyAsync(x => x.BookingID == id);
    public Task<bool> HasOtherPaymentAsync(int id, int excludeId) =>
        db.Payments.AnyAsync(x => x.BookingID == id && x.PaymentID != excludeId);
    public async Task<Payment?> FindAsync(int id) => await db.Payments.FindAsync(id);
    public void Add(Payment payment) => db.Payments.Add(payment);
    public async Task SaveAsync() => await db.SaveChangesAsync();
    public Task<IDbContextTransaction> BeginTransactionAsync() =>
        db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
}

public sealed record PaymentSaveResult(IReadOnlyList<string> Errors, bool NotFound = false)
{
    public bool Succeeded => !NotFound && Errors.Count == 0;
}

/// <summary>Preserves the existing payment rules; validation and write share a serializable transaction.</summary>
public sealed class PaymentService(IPaymentStore store)
{
    public async Task<IReadOnlyList<string>> ValidateAsync(Payment item, int excludeId = 0)
    {
        var errors = new List<string>();
        if (!await store.BookingExistsAsync(item.BookingID))
            errors.Add("Няма такава резервация.");
        if (await store.HasOtherPaymentAsync(item.BookingID, excludeId))
            errors.Add("Тази резервация вече има плащане.");
        return errors;
    }

    public async Task<PaymentSaveResult> SaveAsync(Payment item, int? paymentId = null)
    {
        // Dispose rolls back on validation failure or any exception, including commit failure.
        await using var transaction = await store.BeginTransactionAsync();
        var errors = await ValidateAsync(item, paymentId ?? 0);
        if (errors.Count > 0) return new(errors);
        if (paymentId is int id)
        {
            var existing = await store.FindAsync(id);
            if (existing == null) return new(errors, NotFound: true);
            existing.BookingID = item.BookingID;
            existing.Amount = item.Amount;
            existing.PaymentMethod = item.PaymentMethod;
            // PaidAtUtc belongs to the original insert and must never be changed by an edit.
        }
        else store.Add(item);
        await store.SaveAsync();
        await transaction.CommitAsync();
        return new(errors);
    }
}
