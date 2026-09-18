namespace HolidayBookings.Api.Bookings;
public interface IBookingHandlerResolver
{
    IBookingHandler For(IBookingDetails details);
}

internal sealed class BookingHandlerResolver : IBookingHandlerResolver
{
    private readonly Dictionary<Type, IBookingHandler> _handlersByDetailsType;

    public BookingHandlerResolver(IEnumerable<IBookingHandler> handlers)
    {
        _handlersByDetailsType = handlers.ToDictionary(handler => handler.DetailsType);
    }

    public IBookingHandler For(IBookingDetails details)
    {
        if (!_handlersByDetailsType.TryGetValue(details.GetType(), out var handler))
        {
            // runtime backstop.
            throw new InvalidOperationException($"No IBookingHandler is registered for {details.GetType().Name}.");
        }

        return handler;
    }
}