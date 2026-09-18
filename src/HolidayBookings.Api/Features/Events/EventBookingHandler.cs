using FluentValidation;
using HolidayBookings.Api.Bookings;

namespace HolidayBookings.Api.Events;

internal sealed class EventBookingHandler(
    IValidator<EventDetails> validator,
    TimeProvider clock)
    : BookingHandler<EventDetails>(validator)
{
    protected override Task<BookingOutcome> ConfirmAsync(EventDetails details, CancellationToken ct)
    {
        // Stubbed, per the brief's assumption that product is always available ?? seats are limited IRL
        if (!IsAvailable(details))
        {
            return Task.FromResult<BookingOutcome>(
                new BookingOutcome.Rejected($"{details.EventName} has no {details.SeatCategory} seats available for {details.Tickets} tickets."));
        }

        var reference = $"EVT-{clock.GetUtcNow():yyyyMMdd}-{Guid.CreateVersion7().ToString("N")[..6].ToUpperInvariant()}";

        return Task.FromResult<BookingOutcome>(new BookingOutcome.Confirmed(reference));
    }

    private static bool IsAvailable(EventDetails details) => true;
}