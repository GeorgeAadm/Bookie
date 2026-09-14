public sealed record BookingRequest(
    string Type,
    string PrimaryGuestName,
    string PrimaryGuestEmail,
    DateOnly StartDate,
    DateOnly EndDate,
    string? Notes);

public sealed record Booking(
    Guid Id,
    string Type,
    string PrimaryGuestName,
    string PrimaryGuestEmail,
    DateOnly StartDate,
    DateOnly EndDate,
    string? Notes,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt = null);