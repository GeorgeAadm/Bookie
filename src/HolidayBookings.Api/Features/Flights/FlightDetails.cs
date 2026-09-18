using HolidayBookings.Api.Bookings;

namespace HolidayBookings.Api.Flights;

/// <summary>
/// IATA airline and airport codes, and passenger type codes ADT (adult), CHD (child) and INF (infant).
/// </summary>
public sealed record FlightDetails(
    IReadOnlyList<FlightSegment> Segments,
    IReadOnlyList<Passenger> Passengers,
    string CabinClass) : IBookingDetails
{
    public string Discriminator => "flight";

    public bool IsMultiSegment => Segments.Count > 1;
}

/// <summary>
/// Use DateTimeOffset because each is local to a different airport. Converting to UTC - inaccurte
///
/// MarketingCarrier sells the ticket - possibly a different operating carrier aircraft.
/// </summary>
public sealed record FlightSegment(
    string MarketingCarrier,
    string FlightNumber,
    string Origin,
    string Destination,
    DateTimeOffset DepartsAt,
    DateTimeOffset ArrivesAt);

public sealed record Passenger(string FullName, string Type);