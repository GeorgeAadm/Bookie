using HolidayBookings.Api.Bookings;

namespace HolidayBookings.Api.Vehicles;

/// <summary>
/// Category uses ACRISS codes, the car rental industry standard. Each letter has a meaning:
/// CDMR is Compact, 4-Door, Manual transmission, air conditioning (R).
///
/// Times are DateTimeOffset, not DateOnly. A pickup happens at a specific hour at a branch with
/// opening times, and a one-way rental can cross a timezone. Compare HotelDetails, where the
/// stay dates are local to the property and carry no time at all.
/// </summary>
public sealed record VehicleDetails(
    string Category,
    string PickupLocation,
    DateTimeOffset PickupAt,
    string DropoffLocation,
    DateTimeOffset DropoffAt,
    string DriverLicenceCountry) : IBookingDetails
{
    public string Discriminator => "vehicle";

    /// <summary>Rentals are priced per 24-hour period, not per calendar day.</summary>
    public int RentalDays => (int)Math.Ceiling((DropoffAt - PickupAt).TotalDays);

    public bool IsOneWay => !PickupLocation.Equals(DropoffLocation, StringComparison.OrdinalIgnoreCase);
}