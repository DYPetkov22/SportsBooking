using System.Linq.Expressions;
using Moq;
using SportsBooking.Web.Models;
using SportsBooking.Web.Services;

namespace SportsBooking.Tests;

public sealed class BookingAvailabilityServiceTests
{
    private readonly Mock<IBookingAvailabilityStore> _store = new(MockBehavior.Strict);
    private readonly BookingAvailabilityService _service;
    private static Booking Input() => new() { BookingID = 8, FieldID = 3,
        BookingDate = new DateOnly(2030, 5, 20), StartTime = new(18, 0), EndTime = new(19, 0), Status = "Активна" };
    public BookingAvailabilityServiceTests() => _service = new(_store.Object);
    private void Existing(params Booking[] bookings) => _store
        .Setup(x => x.AnyAsync(It.IsAny<Expression<Func<Booking, bool>>>()))
        .Returns((Expression<Func<Booking, bool>> predicate) => Task.FromResult(bookings.Any(predicate.Compile())));

    [Fact] public async Task EmptyScheduleIsAvailable()
    { Existing(); Assert.False(await _service.HasOverlapAsync(Input())); }
    [Fact] public async Task SameIntervalIsOccupied()
    { Existing(Input()); Assert.True(await _service.HasOverlapAsync(Input())); }
    [Fact] public async Task PartialOverlapAtStartIsOccupied()
    { var b = Input(); b.StartTime = new(17, 30); b.EndTime = new(18, 30); Existing(b); Assert.True(await _service.HasOverlapAsync(Input())); }
    [Fact] public async Task PartialOverlapAtEndIsOccupied()
    { var b = Input(); b.StartTime = new(18, 30); b.EndTime = new(19, 30); Existing(b); Assert.True(await _service.HasOverlapAsync(Input())); }
    [Fact] public async Task EnclosingBookingIsOccupied()
    { var b = Input(); b.StartTime = new(17, 0); b.EndTime = new(20, 0); Existing(b); Assert.True(await _service.HasOverlapAsync(Input())); }
    [Fact] public async Task ContainedBookingIsOccupied()
    { var b = Input(); b.StartTime = new(18, 15); b.EndTime = new(18, 45); Existing(b); Assert.True(await _service.HasOverlapAsync(Input())); }
    [Fact] public async Task PreviousBookingEndingAtStartIsAvailable()
    { var b = Input(); b.StartTime = new(17, 0); b.EndTime = new(18, 0); Existing(b); Assert.False(await _service.HasOverlapAsync(Input())); }
    [Fact] public async Task NextBookingStartingAtEndIsAvailable()
    { var b = Input(); b.StartTime = new(19, 0); b.EndTime = new(20, 0); Existing(b); Assert.False(await _service.HasOverlapAsync(Input())); }
    [Fact] public async Task DifferentFieldIsAvailable()
    { var b = Input(); b.FieldID = 4; Existing(b); Assert.False(await _service.HasOverlapAsync(Input())); }
    [Fact] public async Task DifferentDateIsAvailable()
    { var b = Input(); b.BookingDate = b.BookingDate.AddDays(1); Existing(b); Assert.False(await _service.HasOverlapAsync(Input())); }
    [Fact] public async Task CancelledExistingBookingDoesNotBlock()
    { var b = Input(); b.Status = "Отказана"; Existing(b); Assert.False(await _service.HasOverlapAsync(Input())); }
    [Fact] public async Task CancelledRequestDoesNotQuerySchedule()
    { var b = Input(); b.Status = "Отказана"; Assert.False(await _service.HasOverlapAsync(b)); _store.VerifyNoOtherCalls(); }
    [Fact] public async Task EditExcludesItself()
    { Existing(Input()); Assert.False(await _service.HasOverlapAsync(Input(), 8)); }
    [Fact] public async Task EditStillDetectsOtherConflictingBooking()
    { var other = Input(); other.BookingID = 9; Existing(Input(), other); Assert.True(await _service.HasOverlapAsync(Input(), 8)); }
    [Fact] public async Task QueryFailurePropagatesInsteadOfReportingAvailability()
    { _store.Setup(x => x.AnyAsync(It.IsAny<Expression<Func<Booking, bool>>>())).ThrowsAsync(new IOException("database unavailable")); await Assert.ThrowsAsync<IOException>(() => _service.HasOverlapAsync(Input())); }
}
