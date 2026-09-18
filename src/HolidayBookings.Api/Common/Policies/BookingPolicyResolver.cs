using HolidayBookings.Api.Bookings;

namespace HolidayBookings.Api.Common.Policies;

public interface IBookingPolicyResolver
{
    IBookingPolicy For(IBookingDetails details);
}

internal sealed class BookingPolicyResolver : IBookingPolicyResolver
{
    private readonly Dictionary<Type, IBookingPolicy> _policiesByType;
    public BookingPolicyResolver(IEnumerable<IBookingPolicy> policies)
    {
        _policiesByType = policies.ToDictionary(policy => policy.DetailsType);
    }
    public IBookingPolicy For(IBookingDetails details)
    {
        if (!_policiesByType.TryGetValue(details.GetType(), out var policy))
        {
            throw new InvalidOperationException($"No booking policy is registered for {details.GetType().Name}.");
        }
        return policy;
    }
}