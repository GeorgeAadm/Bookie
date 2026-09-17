using FluentValidation;
using HolidayBookings.Api.Bookings;

namespace HolidayBookings.Api.Vehicles;

/// <summary>
/// The vehicle type's execution step. Reached only through IBookingHandlerResolver, so no
/// endpoint or shared handler names this class.
/// </summary>
internal sealed class VehicleBookingHandler(
    IValidator<VehicleDetails> validator,
    TimeProvider clock)
    : BookingHandler<VehicleDetails>(validator)
{
    protected override Task<BookingOutcome> ConfirmAsync(VehicleDetails details, CancellationToken ct)
    {
        // Stubbed, per the brief's assumption that inventory is always available. A real
        // implementation would query the supplier for fleet availability in that ACRISS category
        // at the pickup branch, and would also check branch opening hours against the pickup
        // time and whether the one-way drop-off is permitted between those two branches.
        if (!IsAvailable(details))
        {
            return Task.FromResult<BookingOutcome>(
                new BookingOutcome.Rejected(
                    $"No {details.Category} vehicles are available at {details.PickupLocation} " +
                    "for the requested period."));
        }

        var reference = $"VEH-{clock.GetUtcNow():yyyyMMdd}-{Guid.CreateVersion7().ToString("N")[..6].ToUpperInvariant()}";

        return Task.FromResult<BookingOutcome>(new BookingOutcome.Confirmed(reference));
    }

    private static bool IsAvailable(VehicleDetails details) => true;
}