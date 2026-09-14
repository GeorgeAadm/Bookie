public interface IBookingRepository
{
    Task<Booking?> GetAsync(Guid id, CancellationToken ct);
    Task<IReadOnlyList<Booking>> ListAsync(CancellationToken ct);
    Task AddAsync(Booking booking, CancellationToken ct);
    Task<bool> UpdateAsync(Booking booking, CancellationToken ct);
    Task<bool> RemoveAsync(Guid id, CancellationToken ct);
}