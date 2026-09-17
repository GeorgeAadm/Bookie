using FluentValidation;
using HolidayBookings.Api.Bookings;

namespace HolidayBookings.Api.Hotels;

/// <summary>
/// The hotel type's execution step. Found by assembly scanning and reached through
/// IBookingHandlerResolver, so no shared file names this class.
///
/// The base class supplies DetailsType, the cast and validation delegation; only the
/// confirmation is written here.
/// </summary>
internal sealed class HotelBookingHandler(
    IValidator<HotelDetails> validator,
    TimeProvider clock)
    : BookingHandler<HotelDetails>(validator)
{
    protected override Task<BookingOutcome> ConfirmAsync(HotelDetails details, CancellationToken ct)
    {
        // Stubbed, per the brief's assumption that inventory is always available. A real
        // implementation would call the property management system for the dates and room types.
        
        if (!IsAvailable(details))
        {
            return Task.FromResult<BookingOutcome>(
                new BookingOutcome.Rejected($"{details.HotelName} has no availability for the requested dates."));
        }

        var reference = $"HTL-{clock.GetUtcNow():yyyyMMdd}-{Guid.CreateVersion7().ToString("N")[..6].ToUpperInvariant()}";

        return Task.FromResult<BookingOutcome>(new BookingOutcome.Confirmed(reference));
    }

    private static bool IsAvailable(HotelDetails details) => true;
}