using System.Collections.Concurrent;

namespace HolidayBookings.Api.Bookings;

internal sealed class InMemoryBookingRepository : IBookingRepository
{
    private readonly ConcurrentDictionary<Guid, Booking> _bookings = new();

    public Task<Booking?> GetAsync(Guid id, CancellationToken ct = default) =>
        Task.FromResult(_bookings.TryGetValue(id, out var booking) ? booking : null);

    public Task<IReadOnlyList<Booking>> ListAsync(CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<Booking>>(
            _bookings.Values.OrderBy(b => b.CreatedAt).ToList());

    public Task AddAsync(Booking booking, CancellationToken ct = default)
    {
        _bookings[booking.Id] = booking;
        return Task.CompletedTask;
    }

    public Task<bool> UpdateAsync(Booking booking, CancellationToken ct = default)
    {
        if (!_bookings.ContainsKey(booking.Id))
        {
            return Task.FromResult(false);
        }

        _bookings[booking.Id] = booking;
        return Task.FromResult(true);
    }

    public Task<bool> RemoveAsync(Guid id, CancellationToken ct = default) =>
        Task.FromResult(_bookings.TryRemove(id, out _));
}