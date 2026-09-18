using FluentValidation;
using HolidayBookings.Api.Events;
using HolidayBookings.Api.Flights;
using HolidayBookings.Api.Hotels;
using HolidayBookings.Api.Vehicles;

namespace HolidayBookings.Api.Bookings;

internal static class BookingRegistration
{
    /// <summary>
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
        
        // Events
        services.AddSingleton<IValidator<EventDetails>, EventDetailsValidator>();
        services.AddSingleton<IBookingHandler, EventBookingHandler>();

        // Flights
        services.AddSingleton<IValidator<FlightDetails>, FlightDetailsValidator>();
        services.AddSingleton<IBookingHandler, FlightBookingHandler>();


        services.AddSingleton<IBookingHandlerResolver, BookingHandlerResolver>();

        return services;
    }
}