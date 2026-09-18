using FluentValidation.TestHelper;
using HolidayBookings.Api.Flights;
using HolidayBookings.Api.Hotels;
using HolidayBookings.Api.Vehicles;
using Microsoft.Extensions.Time.Testing;

namespace HolidayBookings.Tests;

/// <summary>
/// Per-type rules in isolation. Validators are plain classes, so these need no host and no mocks.
/// TimeProvider is injected, which is what makes the past-date rules testable at all.
/// </summary>
public class PerTypeValidationTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 12, 9, 0, 0, TimeSpan.Zero);
    private static FakeTimeProvider Clock() => new(Now);

    [Fact]
    public void Hotel_check_out_must_follow_check_in()
    {
        var details = ValidHotel() with
        {
            CheckIn = new DateOnly(2026, 12, 22),
            CheckOut = new DateOnly(2026, 12, 18),
        };

        new HotelDetailsValidator(Clock()).TestValidate(details)
            .ShouldHaveValidationErrorFor(x => x.CheckOut);
    }

    [Fact]
    public void Hotel_room_needs_at_least_one_adult()
    {
        var details = ValidHotel() with { Rooms = [new RoomAllocation("DBL", 0, 2)] };

        new HotelDetailsValidator(Clock()).TestValidate(details)
            .ShouldHaveValidationErrorFor("Rooms[0].Adults");
    }

    [Fact]
    public void Vehicle_category_must_be_an_acriss_code()
    {
        var details = ValidVehicle() with { Category = "SMALL" };

        new VehicleDetailsValidator(Clock()).TestValidate(details)
            .ShouldHaveValidationErrorFor(x => x.Category);
    }

    [Fact]
    public void A_backwards_rental_reports_one_error_not_three()
    {
        // Regression - Without CascadeMode.Stop and DependentRules 
        // a negative duration also fires the minimum-hours and maximum-days rules for the same mistake.
        var details = ValidVehicle() with
        {
            PickupAt = new DateTimeOffset(2026, 12, 22, 10, 0, 0, TimeSpan.FromHours(2)),
            DropoffAt = new DateTimeOffset(2026, 12, 18, 10, 0, 0, TimeSpan.FromHours(2)),
        };

        var result = new VehicleDetailsValidator(Clock()).Validate(details);

        Assert.Single(result.Errors);
    }

    [Fact]
    public void A_flight_itinerary_must_connect()
    {
        var details = ValidFlight() with
        {
            Segments =
            [
                new FlightSegment("KL", "598", "CPT", "AMS",
                    new DateTimeOffset(2027, 1, 5, 23, 55, 0, TimeSpan.FromHours(2)),
                    new DateTimeOffset(2027, 1, 6, 10, 30, 0, TimeSpan.FromHours(1))),
                // Departs from CDG, but the previous segment landed in AMS.
                new FlightSegment("AF", "1281", "CDG", "LHR",
                    new DateTimeOffset(2027, 1, 6, 12, 45, 0, TimeSpan.FromHours(1)),
                    new DateTimeOffset(2027, 1, 6, 13, 0, 0, TimeSpan.Zero)),
            ],
        };

        new FlightDetailsValidator(Clock()).TestValidate(details)
            .ShouldHaveValidationErrorFor(x => x.Segments);
    }

    [Fact]
    public void Infants_cannot_outnumber_adults()
    {
        var details = ValidFlight() with
        {
            Passengers =
            [
                new Passenger("Sam Patel", "ADT"),
                new Passenger("Kit Patel", "INF"),
                new Passenger("Robin Patel", "INF"),
            ],
        };

        new FlightDetailsValidator(Clock()).TestValidate(details)
            .ShouldHaveValidationErrorFor(x => x.Passengers);
    }

    [Fact]
    public void Well_formed_details_pass_for_every_type()
    {
        new HotelDetailsValidator(Clock()).TestValidate(ValidHotel()).ShouldNotHaveAnyValidationErrors();
        new VehicleDetailsValidator(Clock()).TestValidate(ValidVehicle()).ShouldNotHaveAnyValidationErrors();
        new FlightDetailsValidator(Clock()).TestValidate(ValidFlight()).ShouldNotHaveAnyValidationErrors();
    }

    private static HotelDetails ValidHotel() => new(
        "Sea View Hotel", "Cape Town",
        new DateOnly(2026, 12, 18), new DateOnly(2026, 12, 22),
        [new RoomAllocation("DBL-SEA", 2, 1)]);

    private static VehicleDetails ValidVehicle() => new(
        "CDMR", "CPT",
        new DateTimeOffset(2026, 12, 18, 10, 0, 0, TimeSpan.FromHours(2)), "CPT",
        new DateTimeOffset(2026, 12, 22, 10, 0, 0, TimeSpan.FromHours(2)), "ZA");

    private static FlightDetails ValidFlight() => new(
        [new FlightSegment("BA", "6234", "CPT", "LHR",
            new DateTimeOffset(2026, 12, 18, 19, 30, 0, TimeSpan.FromHours(2)),
            new DateTimeOffset(2026, 12, 19, 6, 15, 0, TimeSpan.Zero))],
        [new Passenger("Alex Morgan", "ADT")],
        "ECONOMY");
}