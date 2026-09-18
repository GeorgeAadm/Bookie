using FluentValidation;
using HolidayBookings.Api.Bookings;

namespace HolidayBookings.Api.Flights;

/// <summary>
/// Flights book through the shared endpoint like every other product.
/// This API assumes the consumer has already selected a product from the supplier and is recording that choice
/// The brief assumes availability, no uncertainty exists - one phase is correct.
/// </summary>
internal sealed class FlightBookingHandler(
    IValidator<FlightDetails> validator,
    TimeProvider clock)
    : BookingHandler<FlightDetails>(validator)
{
    private const int MinimumConnectionMinutes = 45;

    protected override Task<BookingOutcome> ConfirmAsync(FlightDetails details, CancellationToken ct)
    {
        if (ShortestConnectionMinutes(details) is { } minutes && minutes < MinimumConnectionMinutes)
        {
            return Task.FromResult<BookingOutcome>(
                new BookingOutcome.Rejected($"A connection of {minutes} minutes is below the {MinimumConnectionMinutes} minute minimum connection time."));
        }

        var reference = $"FLT-{clock.GetUtcNow():yyyyMMdd}-{Guid.CreateVersion7().ToString("N")[..6].ToUpperInvariant()}";

        return Task.FromResult<BookingOutcome>(new BookingOutcome.Confirmed(reference));
    }

    private static int? ShortestConnectionMinutes(FlightDetails details) =>
        details.IsMultiSegment
            ? details.Segments.Zip(details.Segments.Skip(1)).Min(pair => (int)(pair.Second.DepartsAt - pair.First.ArrivesAt).TotalMinutes)
            : null;
}