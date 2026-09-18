using HolidayBookings.Api.Bookings;

public interface IBookingPolicy
{
    Type DetailsType { get; }
    Task<AvailabilityResult> CheckAvailabilityAsync(IBookingDetails details, CancellationToken ct);
    ChangeDecision CanChange(Booking existing, IBookingDetails requested, DateTimeOffset now);
    CancellationOutcome Cancel(Booking booking, DateTimeOffset now);
}

public sealed record AvailabilityResult(bool IsAvailable, string? Reason = null, string? HoldReference = null)
{
    public static AvailabilityResult Available(string? holdReference = null) => new(true, null, holdReference);
    public static AvailabilityResult Unavailable(string reason) => new(false, reason);
}

public abstract record ChangeDecision
{
    private ChangeDecision() { }
    public sealed record Allowed : ChangeDecision;
    public sealed record AllowedWithFee(Money Fee) : ChangeDecision;
    public sealed record Denied(string Reason) : ChangeDecision;
}

public sealed record CancellationOutcome(bool Permitted, Money Penalty, Money Refund, string? Reason = null)
{
    public static CancellationOutcome Free(Money total) => new(true, Money.Zero(total.Currency), total);
    public static CancellationOutcome WithPenalty(Money total, decimal penaltyPercent)
    {
        var penalty = total.Percent(penaltyPercent);
        return new CancellationOutcome(true, penalty, total.Subtract(penalty));
    }
    public static CancellationOutcome NonRefundable(Money total) => new(true, total, Money.Zero(total.Currency), "This booking is non-refundable.");
    public static CancellationOutcome NotPermitted(Money total, string reason) => new(false, Money.Zero(total.Currency), Money.Zero(total.Currency), reason);
}

// Utility 

public readonly record struct Money(decimal Amount, string Currency)
{
    public static Money Zero(string currency) => new(0m, currency);
    public Money Percent(decimal percent) => this with { Amount = Math.Round(Amount * percent / 100m, 2, MidpointRounding.AwayFromZero) };
    public Money Subtract(Money other) => other.Currency == Currency 
        ? this with { Amount = Amount - other.Amount }
        : throw new InvalidOperationException($"Cannot subtract {other.Currency} from {Currency}.");
    public override string ToString() => $"{Amount:0.00} {Currency}";
}