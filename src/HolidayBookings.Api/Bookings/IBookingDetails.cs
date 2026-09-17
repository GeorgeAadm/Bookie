using System.Text.Json.Serialization;
using HolidayBookings.Api.Hotels;
using HolidayBookings.Api.Vehicles;

namespace HolidayBookings.Api.Bookings;

/// <summary>
/// The 'type' specific part of a booking.
///
/// The derived types are listed here - not discovered at startup: registration is explicit
/// OpenAPI sees the derived schemas - full catalogue of booking types in one place.
///
/// Adding a booking type - 3 lines outside its own folder
/// JsonDerivedType here
/// handler registration in BookingRegistration
/// </summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(HotelDetails), "hotel")]
[JsonDerivedType(typeof(VehicleDetails), "vehicle")]
public interface IBookingDetails
{
    string Discriminator { get; }
}