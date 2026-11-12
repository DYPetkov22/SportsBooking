using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Moq;
using SportsBooking.Web.Models;
using SportsBooking.Web.Services;

namespace SportsBooking.Tests;

public sealed class PaymentServiceTests
{
    private readonly Mock<IPaymentStore> _store = new(MockBehavior.Strict);
    private readonly Mock<IDbContextTransaction> _transaction = new();
    private readonly PaymentService _service;
    private static Payment Input() => new() { BookingID = 17, Amount = 42.50m, PaymentMethod = "Карта" };

    public PaymentServiceTests()
    {
        _store.Setup(x => x.BeginTransactionAsync()).ReturnsAsync(_transaction.Object);
        _store.Setup(x => x.BookingExistsAsync(17)).ReturnsAsync(true);
        _store.Setup(x => x.HasOtherPaymentAsync(17, It.IsAny<int>())).ReturnsAsync(false);
        _store.Setup(x => x.SaveAsync()).Returns(Task.CompletedTask);
        _service = new(_store.Object);
    }

    [Fact]
    public async Task Create_AddsOriginalPaymentAndCommitsAfterSaving()
    {
        var item = Input();
        var order = new List<string>();
        _store.Setup(x => x.Add(item)).Callback(() => order.Add("add"));
        _store.Setup(x => x.SaveAsync()).Callback(() => order.Add("save")).Returns(Task.CompletedTask);
        _transaction.Setup(x => x.CommitAsync(It.IsAny<CancellationToken>()))
            .Callback(() => order.Add("commit")).Returns(Task.CompletedTask);
        var result = await _service.SaveAsync(item);
        Assert.True(result.Succeeded);
        Assert.Equal(new[] { "add", "save", "commit" }, order);
        _store.Verify(x => x.HasOtherPaymentAsync(17, 0), Times.Once);
        _transaction.Verify(x => x.DisposeAsync(), Times.Once);
    }

    [Fact]
    public async Task Create_MissingBookingDoesNotWriteOrCommit()
    {
        _store.Setup(x => x.BookingExistsAsync(17)).ReturnsAsync(false);
        var result = await _service.SaveAsync(Input());
        Assert.False(result.Succeeded);
        Assert.Contains("Няма такава резервация.", result.Errors);
        VerifyNotSaved();
    }

    [Fact]
    public async Task Create_DuplicatePaymentDoesNotWriteOrCommit()
    {
        _store.Setup(x => x.HasOtherPaymentAsync(17, 0)).ReturnsAsync(true);
        var result = await _service.SaveAsync(Input());
        Assert.False(result.Succeeded);
        Assert.Contains("Тази резервация вече има плащане.", result.Errors);
        VerifyNotSaved();
    }

    [Fact]
    public async Task Validate_ReturnsBothReferenceErrors()
    {
        _store.Setup(x => x.BookingExistsAsync(17)).ReturnsAsync(false);
        _store.Setup(x => x.HasOtherPaymentAsync(17, 0)).ReturnsAsync(true);
        Assert.Equal(2, (await _service.ValidateAsync(Input())).Count);
        _store.Verify(x => x.BeginTransactionAsync(), Times.Never);
    }

    [Fact]
    public async Task Validate_ExistingBookingWithoutPaymentIsValid()
    {
        Assert.Empty(await _service.ValidateAsync(Input()));
        _store.Verify(x => x.SaveAsync(), Times.Never);
    }

    [Fact]
    public async Task Edit_UpdatesValuesAndPreservesPaymentDateAndIdentity()
    {
        var paid = new DateTime(2025, 3, 4, 12, 0, 0);
        var existing = new Payment { PaymentID = 8, BookingID = 2, Amount = 10, PaymentMethod = "В брой", PaidAtUtc = paid };
        _store.Setup(x => x.FindAsync(8)).ReturnsAsync(existing);
        var input = Input();
        input.PaidAtUtc = DateTime.MaxValue;
        var result = await _service.SaveAsync(input, 8);
        Assert.True(result.Succeeded);
        Assert.Equal(17, existing.BookingID);
        Assert.Equal(42.50m, existing.Amount);
        Assert.Equal("Карта", existing.PaymentMethod);
        Assert.Equal(paid, existing.PaidAtUtc);
        Assert.Equal(8, existing.PaymentID);
        _store.Verify(x => x.HasOtherPaymentAsync(17, 8), Times.Once);
        _store.Verify(x => x.Add(It.IsAny<Payment>()), Times.Never);
        _transaction.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Edit_MissingPaymentReturnsNotFoundWithoutSaving()
    {
        _store.Setup(x => x.FindAsync(8)).ReturnsAsync((Payment?)null);
        var result = await _service.SaveAsync(Input(), 8);
        Assert.True(result.NotFound);
        Assert.False(result.Succeeded);
        VerifyNotSaved();
    }

    [Fact]
    public async Task Edit_DuplicateDoesNotLoadOrMutateExistingPayment()
    {
        _store.Setup(x => x.HasOtherPaymentAsync(17, 8)).ReturnsAsync(true);
        var result = await _service.SaveAsync(Input(), 8);
        Assert.False(result.Succeeded);
        _store.Verify(x => x.FindAsync(It.IsAny<int>()), Times.Never);
        VerifyNotSaved();
    }

    [Fact]
    public async Task Save_DatabaseFailurePropagatesAndDisposesWithoutCommit()
    {
        _store.Setup(x => x.Add(It.IsAny<Payment>()));
        var failure = new DbUpdateException("database failure");
        _store.Setup(x => x.SaveAsync()).ThrowsAsync(failure);
        Assert.Same(failure, await Assert.ThrowsAsync<DbUpdateException>(() => _service.SaveAsync(Input())));
        _transaction.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
        _transaction.Verify(x => x.DisposeAsync(), Times.Once);
    }

    [Fact]
    public async Task Save_CommitFailurePropagatesAndDisposesTransaction()
    {
        _store.Setup(x => x.Add(It.IsAny<Payment>()));
        _transaction.Setup(x => x.CommitAsync(It.IsAny<CancellationToken>())).ThrowsAsync(new IOException("commit failed"));
        await Assert.ThrowsAsync<IOException>(() => _service.SaveAsync(Input()));
        _transaction.Verify(x => x.DisposeAsync(), Times.Once);
    }

    [Fact]
    public async Task Save_ValidationQueryFailureDisposesWithoutWriting()
    {
        _store.Setup(x => x.BookingExistsAsync(17)).ThrowsAsync(new InvalidOperationException("query failed"));
        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.SaveAsync(Input()));
        VerifyNotSaved();
    }

    [Fact]
    public async Task Save_BeginTransactionFailureDoesNotQueryOrWrite()
    {
        _store.Setup(x => x.BeginTransactionAsync()).ThrowsAsync(new InvalidOperationException("connection failed"));
        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.SaveAsync(Input()));
        _store.Verify(x => x.BookingExistsAsync(It.IsAny<int>()), Times.Never);
        _store.Verify(x => x.SaveAsync(), Times.Never);
    }

    private void VerifyNotSaved()
    {
        _store.Verify(x => x.Add(It.IsAny<Payment>()), Times.Never);
        _store.Verify(x => x.SaveAsync(), Times.Never);
        _transaction.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
        _transaction.Verify(x => x.DisposeAsync(), Times.Once);
    }
}
