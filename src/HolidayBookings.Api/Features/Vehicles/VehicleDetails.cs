using HolidayBookings.Api.Bookings;

namespace HolidayBookings.Api.Vehicles;

/// <summary>
/// Category uses ACRISS codes, the car rental industry standard. Each letter has a meaning:
/// CDMR is Compact, 4-Door, Manual transmission, air conditioning (R).
///
/// Times are DateTimeOffset - pickup happens at a specific hour at a branch with opening times - a one-way rental can cross a timezone. 
/// Compare HotelDetails - the stay dates are local to the property - no time.
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

    /// <summary>Rentals priced per 24-hour period - Not cal day</summary>
    public int RentalDays => (int)Math.Ceiling((DropoffAt - PickupAt).TotalDays);

    public bool IsOneWay => !PickupLocation.Equals(DropoffLocation, StringComparison.OrdinalIgnoreCase);
}