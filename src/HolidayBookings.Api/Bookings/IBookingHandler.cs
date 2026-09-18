using FluentValidation;
using FluentValidation.Results;

namespace HolidayBookings.Api.Bookings;

/// <summary>
/// type-specific interface: how it is validated and how it is actually placed. 
/// The shared endpoints resolve one of these and never switch on type.
/// </summary>
public interface IBookingHandler
{
    Type DetailsType { get; }

    Task<ValidationResult> ValidateAsync(IBookingDetails details, CancellationToken ct);

    Task<BookingOutcome> ConfirmAsync(IBookingDetails details, CancellationToken ct);
}

public abstract record BookingOutcome
{
    private BookingOutcome() { }

    /// <param name="SupplierReference">How supplier IDs this booking</param>
    public sealed record Confirmed(string SupplierReference) : BookingOutcome;

    public sealed record Rejected(string Reason) : BookingOutcome;
}

/// <summary>
/// Does the cast once, so implementations work in their own concrete type. 
/// Validation delegated to the type - validator
/// </summary>
public abstract class BookingHandler<TDetails>(IValidator<TDetails> validator) : IBookingHandler
    where TDetails : IBookingDetails
{
    public Type DetailsType => typeof(TDetails);

    // The resolver only ever passes matching details, so these casts cannot fail.
    public Task<ValidationResult> ValidateAsync(IBookingDetails details, CancellationToken ct) =>
        validator.ValidateAsync((TDetails)details, ct);

    public Task<BookingOutcome> ConfirmAsync(IBookingDetails details, CancellationToken ct) =>
        ConfirmAsync((TDetails)details, ct);

    protected abstract Task<BookingOutcome> ConfirmAsync(TDetails details, CancellationToken ct);
}