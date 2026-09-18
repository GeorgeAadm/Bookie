using HolidayBookings.Api.Bookings;

namespace HolidayBookings.Api.Events;

/// <summary>
/// venue's local time is what appears on the ticket.
///
/// SeatCategory and Tickets are effectively immutable - allocated at purchase
/// TODO: per-type change rules.
/// </summary>
public sealed record EventDetails(
    string EventName,
    string Venue,
    DateTimeOffset PerformanceAt,
    string SeatCategory,
    int Tickets) : IBookingDetails
{
    public string Discriminator => "event";
}