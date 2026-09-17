using System.Text.Json;
using Microsoft.AspNetCore.Diagnostics;

namespace HolidayBookings.Api.Bookings;

/// <summary>
/// An unrecognised "type" discriminator makes System.Text.Json throw - surface as code 500.
/// Is a bad request and should read as one.
/// </summary>
internal sealed class UnknownBookingTypeHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        if (exception is not JsonException)
        {
            return false;
        }

        var known = string.Join(", ", KnownBookingTypes.Names);

        await Results.ValidationProblem(new Dictionary<string, string[]>
        {
            ["details.type"] = [$"Unknown or malformed booking details. Known types: {known}."],
        }).ExecuteAsync(httpContext);

        return true;
    }
}
