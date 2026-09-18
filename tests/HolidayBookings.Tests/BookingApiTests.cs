using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using HolidayBookings.Api.Bookings;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace HolidayBookings.Tests;

/// <summary>
/// End to end via host: routing, polymorphic binding, validation and the mapping from failures to status codes. 
/// Payloads are anonymous objects deliberately: serialising BookingInput would pass even if the JSON was wrong.
/// </summary>
public class BookingApiTests(WebApplicationFactory<Program> factory)
    : IClassFixture<WebApplicationFactory<Program>>
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        AllowOutOfOrderMetadataProperties = true,
    };

    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task A_booking_can_be_added_edited_and_removed()
    {
        var create = await _client.PostAsJsonAsync("/api/bookings", HotelPayload(adults: 2));
        Assert.Equal(HttpStatusCode.Created, create.StatusCode);

        var created = await create.Content.ReadFromJsonAsync<Booking>(Json);
        Assert.NotNull(created);
        Assert.NotEqual(Guid.Empty, created.Id);
        Assert.Equal("hotel", created.Details.Discriminator);
        Assert.False(string.IsNullOrWhiteSpace(created.SupplierReference));

        var read = await _client.GetAsync($"/api/bookings/{created.Id}");
        Assert.Equal(HttpStatusCode.OK, read.StatusCode);

        var update = await _client.PutAsJsonAsync($"/api/bookings/{created.Id}", HotelPayload(adults: 3));
        Assert.Equal(HttpStatusCode.OK, update.StatusCode);

        var delete = await _client.DeleteAsync($"/api/bookings/{created.Id}");
        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);

        var gone = await _client.GetAsync($"/api/bookings/{created.Id}");
        Assert.Equal(HttpStatusCode.NotFound, gone.StatusCode);
    }

    [Fact]
    public async Task Every_product_type_books_through_the_same_endpoint()
    {
        foreach (var payload in new[] { HotelPayload(2), VehiclePayload(), EventPayload(), FlightPayload() })
        {
            var response = await _client.PostAsJsonAsync("/api/bookings", payload);

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        }
    }

    [Fact]
    public async Task Per_type_rules_are_enforced_at_the_api_boundary()
    {
        var response = await _client.PostAsJsonAsync("/api/bookings", new
        {
            primaryGuestName = "Alex Morgan",
            primaryGuestEmail = "alex@example.com",
            details = new
            {
                type = "hotel",
                hotelName = "Sea View Hotel",
                city = "Cape Town",
                checkIn = "2026-12-22",
                checkOut = "2026-12-18",
                rooms = new[] { new { roomType = "DBL", adults = 2, children = 0 } },
            },
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(problem.TryGetProperty("errors", out _));
    }

    [Fact]
    public async Task A_booking_cannot_change_its_product_type()
    {
        var create = await _client.PostAsJsonAsync("/api/bookings", HotelPayload(adults: 2));
        var created = await create.Content.ReadFromJsonAsync<Booking>(Json);

        var response = await _client.PutAsJsonAsync($"/api/bookings/{created!.Id}", VehiclePayload());

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Editing_a_booking_that_does_not_exist_returns_not_found()
    {
        var response = await _client.PutAsJsonAsync(
            $"/api/bookings/{Guid.NewGuid()}", HotelPayload(adults: 2));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private static object HotelPayload(int adults) => new
    {
        primaryGuestName = "Alex Morgan",
        primaryGuestEmail = "alex@example.com",
        details = new
        {
            type = "hotel",
            hotelName = "Sea View Hotel",
            city = "Cape Town",
            checkIn = "2026-12-18",
            checkOut = "2026-12-22",
            rooms = new[] { new { roomType = "DBL-SEA", adults, children = 0 } },
        },
    };

    private static object VehiclePayload() => new
    {
        primaryGuestName = "Alex Morgan",
        primaryGuestEmail = "alex@example.com",
        details = new
        {
            type = "vehicle",
            category = "CDMR",
            pickupLocation = "CPT",
            pickupAt = "2026-12-18T10:00:00+02:00",
            dropoffLocation = "CPT",
            dropoffAt = "2026-12-22T10:00:00+02:00",
            driverLicenceCountry = "ZA",
        },
    };

    private static object EventPayload() => new
    {
        primaryGuestName = "Alex Morgan",
        primaryGuestEmail = "alex@example.com",
        details = new
        {
            type = "event",
            eventName = "Hamilton",
            venue = "Artscape Theatre",
            performanceAt = "2027-03-04T19:30:00+02:00",
            seatCategory = "STALLS",
            tickets = 2,
        },
    };

    private static object FlightPayload() => new
    {
        primaryGuestName = "Alex Morgan",
        primaryGuestEmail = "alex@example.com",
        details = new
        {
            type = "flight",
            cabinClass = "ECONOMY",
            segments = new[]
            {
                new
                {
                    marketingCarrier = "BA",
                    flightNumber = "6234",
                    origin = "CPT",
                    destination = "LHR",
                    departsAt = "2026-12-18T19:30:00+02:00",
                    arrivesAt = "2026-12-19T06:15:00+00:00",
                },
            },
            passengers = new[] { new { fullName = "Alex Morgan", type = "ADT" } },
        },
    };

    [Fact]
    public async Task An_unknown_product_type_is_rejected_as_a_bad_request()
    {
        var response = await _client.PostAsJsonAsync("/api/bookings", new
        {
            primaryGuestName = "Alex Morgan",
            primaryGuestEmail = "alex@example.com",
            details = new { type = "submarine", vesselName = "Nautilus" },
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Deleting_a_booking_that_does_not_exist_returns_not_found()
    {
        var response = await _client.DeleteAsync($"/api/bookings/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}