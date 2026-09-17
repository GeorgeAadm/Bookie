using FluentValidation;
using HolidayBookings.Api.Hotels;
using HolidayBookings.Api.Vehicles;

namespace HolidayBookings.Api.Bookings;

internal static class BookingRegistration
{
    /// <summary>
    /// Adding a type adds 2 lines per booking 'type'
    /// A missing registration is caught by Coverage Tests rather than at runtime.
    /// </summary>
    public static IServiceCollection AddBookingTypes(this IServiceCollection services)
    {
        services.AddSingleton<IValidator<BookingInput>, BookingInputValidator>();

        // Hotels
        services.AddSingleton<IValidator<HotelDetails>, HotelDetailsValidator>();
        services.AddSingleton<IBookingHandler, HotelBookingHandler>();

        // Vehicles
        services.AddSingleton<IValidator<VehicleDetails>, VehicleDetailsValidator>();
        services.AddSingleton<IBookingHandler, VehicleBookingHandler>();

        services.AddSingleton<IBookingHandlerResolver, BookingHandlerResolver>();

        return services;
    }
}