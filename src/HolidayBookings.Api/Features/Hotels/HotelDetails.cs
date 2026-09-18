using FluentValidation;
using HolidayBookings.Api.Bookings;

namespace HolidayBookings.Api.Hotels;

public sealed record HotelDetails(
    string HotelName,
    string City,
    DateOnly CheckIn,
    DateOnly CheckOut,
    IReadOnlyList<RoomAllocation> Rooms) : IBookingDetails
{
    public string Discriminator => "hotel";
    public int Nights => CheckOut.DayNumber - CheckIn.DayNumber;
};

public sealed record RoomAllocation(string RoomType, int Adults, int Children);
