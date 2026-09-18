using System.Reflection;
using System.Text.Json.Serialization;

namespace HolidayBookings.Api.Bookings;

/// <summary>
/// Reads the discriminators declared on IBookingDetails - error messages can list the known types.
/// one source of truth - attribute list.
/// </summary>
internal static class KnownBookingTypes
{
    public static readonly IReadOnlyDictionary<Type, string> Discriminators =
        typeof(IBookingDetails)
            .GetCustomAttributes<JsonDerivedTypeAttribute>()
            .ToDictionary(
                attribute => attribute.DerivedType,
                attribute => attribute.TypeDiscriminator?.ToString() ?? attribute.DerivedType.Name);

    public static IEnumerable<string> Names => Discriminators.Values.Order();
}