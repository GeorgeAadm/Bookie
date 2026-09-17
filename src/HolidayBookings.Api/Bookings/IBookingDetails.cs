using System.Text.Json.Serialization;
using HolidayBookings.Api.Hotels;
using HolidayBookings.Api.Vehicles;

namespace HolidayBookings.Api.Bookings;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(HotelDetails), "hotel")]
[JsonDerivedType(typeof(VehicleDetails), "vehicle")]
public interface IBookingDetails
{
    string Discriminator { get; }
}