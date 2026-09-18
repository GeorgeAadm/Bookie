using System.Text.Json.Serialization;
using HolidayBookings.Api.Events;
using HolidayBookings.Api.Flights;
using HolidayBookings.Api.Hotels;
using HolidayBookings.Api.Vehicles;

namespace HolidayBookings.Api.Bookings;

/// <summary>
/// The 'type' specific part of a booking.
///
/// The derived types are listed here - explicit
/// OpenAPI sees the derived schemas - full catalogue of booking types in one place.
///
/// Adding a booking type - JsonDerivedType here + handler registration in BookingRegistration
/// </summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(HotelDetails), "hotel")]
[JsonDerivedType(typeof(VehicleDetails), "vehicle")]
[JsonDerivedType(typeof(EventDetails), "event")]
[JsonDerivedType(typeof(FlightDetails), "flight")]
public interface IBookingDetails
{
    string Discriminator { get; }
}