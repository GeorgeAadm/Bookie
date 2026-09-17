using FluentValidation;
using FluentValidation.Results;

namespace HolidayBookings.Api.Bookings;

public interface IBookingHandler
{
    Type DetailsType { get; }

    Task<ValidationResult> ValidateAsync(IBookingDetails details, CancellationToken ct);

    Task<BookingOutcome> ConfirmAsync(IBookingDetails details, CancellationToken ct);
}

public abstract record BookingOutcome
{
    private BookingOutcome() { }

    public sealed record Confirmed(string SupplierReference) : BookingOutcome;

    public sealed record Rejected(string Reason) : BookingOutcome;
}

public abstract class BookingHandler<TDetails>(IValidator<TDetails> validator) : IBookingHandler
    where TDetails : IBookingDetails
{
    public Type DetailsType => typeof(TDetails);

    public Task<ValidationResult> ValidateAsync(IBookingDetails details, CancellationToken ct) =>
        validator.ValidateAsync((TDetails)details, ct);

    public Task<BookingOutcome> ConfirmAsync(IBookingDetails details, CancellationToken ct) =>
        ConfirmAsync((TDetails)details, ct);

    protected abstract Task<BookingOutcome> ConfirmAsync(TDetails details, CancellationToken ct);
}