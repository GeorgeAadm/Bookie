using System.Reflection;
using System.Text.Json.Serialization;
using HolidayBookings.Api.Bookings;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Runtime.CompilerServices;

namespace HolidayBookings.Tests;

/// <summary>
/// Adding a booking type touches BookingRegistration.cs & IBookingDetails.cs
/// Turns 'someone forgot' = runtime exeption into a failed build -> explicit registration.
/// </summary>
public class BookingTypeCoverageTests(WebApplicationFactory<Program> factory)
    : IClassFixture<WebApplicationFactory<Program>>
{
    private static readonly IReadOnlyList<Type> DetailsTypes = typeof(IBookingDetails).Assembly
        .GetTypes()
        .Where(t => t is { IsAbstract: false, IsInterface: false }
                    && t.IsAssignableTo(typeof(IBookingDetails)))
        .ToList();

    [Fact]
    public void Every_details_type_is_declared_on_the_interface()
    {
        var declared = typeof(IBookingDetails)
            .GetCustomAttributes<JsonDerivedTypeAttribute>()
            .Select(attribute => attribute.DerivedType)
            .ToHashSet();

        var missing = DetailsTypes.Where(t => !declared.Contains(t)).Select(t => t.Name).ToList();

        Assert.NotEmpty(DetailsTypes);
        Assert.True(missing.Count == 0,
            $"Missing a [JsonDerivedType] entry on IBookingDetails for: {string.Join(", ", missing)}");
    }

    [Fact]
    public void Every_details_type_has_a_registered_handler()
    {
        var covered = factory.Services
            .GetServices<IBookingHandler>()
            .Select(handler => handler.DetailsType)
            .ToHashSet();

        var missing = DetailsTypes.Where(t => !covered.Contains(t)).Select(t => t.Name).ToList();

        Assert.True(missing.Count == 0,
            $"No IBookingHandler registered for: {string.Join(", ", missing)}");
    }

    [Fact]
    public void The_discriminator_property_matches_the_attribute()
    {
        foreach (var (detailsType, attributeDiscriminator) in KnownBookingTypes.Discriminators)
        {
            var instance = (IBookingDetails)RuntimeHelpers.GetUninitializedObject(detailsType);

            Assert.Equal(attributeDiscriminator, instance.Discriminator);
        }
    }
}