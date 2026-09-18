using HolidayBookings.Api.Bookings;
using HolidayBookings.Api.Hotels;

namespace HolidayBookings.Tests;

public class RepositoryConcurrencyTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 12, 9, 0, 0, TimeSpan.Zero);

    private static Booking Existing() => new(
        Guid.CreateVersion7(),
        "Alex Morgan",
        "alex@example.com",
        "HTL-20260912-ABC123",
        new HotelDetails(
            "Sea View Hotel", "Cape Town",
            new DateOnly(2026, 12, 18), new DateOnly(2026, 12, 22),
            [new RoomAllocation("DBL-SEA", 2, 1)]),
        Now);

    [Fact]
    public async Task A_write_against_the_current_version_succeeds()
    {
        var repository = new InMemoryBookingRepository();
        var original = Existing();
        await repository.AddAsync(original, default);

        var updated = original with { PrimaryGuestName = "Sam Patel", UpdatedAt = Now };

        Assert.True(await repository.UpdateAsync(original, updated, default));

        var stored = await repository.GetAsync(original.Id, default);
        Assert.Equal("Sam Patel", stored!.PrimaryGuestName);
    }
    [Fact]
    public async Task A_write_against_a_stale_version_is_rejected()
    {
        var repository = new InMemoryBookingRepository();
        var original = Existing();
        await repository.AddAsync(original, default);

        // other request gets there first
        var theirs = original with { PrimaryGuestName = "Sam Patel", UpdatedAt = Now };
        Assert.True(await repository.UpdateAsync(original, theirs, default));

        // ours read before theirs landed -> overwriting a version that no longer exists
        var ours = original with { PrimaryGuestName = "Robin Patel", UpdatedAt = Now };
        Assert.False(await repository.UpdateAsync(original, ours, default));

        // first writer change survives
        var stored = await repository.GetAsync(original.Id, default);
        Assert.Equal("Sam Patel", stored!.PrimaryGuestName);
    }
}