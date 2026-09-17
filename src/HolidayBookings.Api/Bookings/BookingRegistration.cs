using FluentValidation;
using HolidayBookings.Api.Hotels;

namespace HolidayBookings.Api.Bookings;

internal static class BookingRegistration
{
    /// <summary>
    /// One block per booking type. Adding a type adds 2 lines here.
    /// </summary>
    public static IServiceCollection AddBookingTypes(this IServiceCollection services)
    {
        services.AddSingleton<IValidator<BookingInput>, BookingInputValidator>();

        // Hotels
        services.AddSingleton<IValidator<HotelDetails>, HotelDetailsValidator>();
        services.AddSingleton<IBookingHandler, HotelBookingHandler>();

        services.AddSingleton<IBookingHandlerResolver, BookingHandlerResolver>();

        return services;
    }
}