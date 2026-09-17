namespace HolidayBookings.Api.Bookings;

public sealed record BookingInput(
    string Type,
    string PrimaryGuestName,
    string PrimaryGuestEmail,
    IBookingDetails Details);

public sealed record Booking(
    Guid Id,
    string PrimaryGuestName,
    string PrimaryGuestEmail,
    string SupplierReference,
    IBookingDetails Details,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt = null);