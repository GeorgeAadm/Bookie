using FluentValidation;

namespace HolidayBookings.Api.Hotels;

/// <summary>
/// TimeProvider is injected so the past-date rule is testable without freezing the machine clock.
/// </summary>
internal sealed class HotelDetailsValidator : AbstractValidator<HotelDetails>
{
    private const int MaximumNights = 30;

    public HotelDetailsValidator(TimeProvider clock)
    {
        RuleFor(x => x.HotelName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.City).NotEmpty().MaximumLength(100);

        RuleFor(x => x.CheckIn)
            .GreaterThanOrEqualTo(_ => DateOnly.FromDateTime(clock.GetUtcNow().UtcDateTime))
            .WithMessage("Check-in cannot be in the past.");

        RuleFor(x => x.CheckOut)
            .GreaterThan(x => x.CheckIn)
            .WithMessage("Check-out must be after check-in.");

        RuleFor(x => x.Nights)
            .LessThanOrEqualTo(MaximumNights)
            .WithMessage($"A stay cannot exceed {MaximumNights} nights. Split it into two bookings.")
            .When(x => x.CheckOut > x.CheckIn);

        RuleFor(x => x.Rooms)
            .NotEmpty()
            .WithMessage("At least one room is required.");

        RuleFor(x => x.Rooms)
            .Must(rooms => rooms.Count <= 5)
            .WithMessage("A single booking cannot hold more than five rooms.")
            .When(x => x.Rooms is not null);

        RuleForEach(x => x.Rooms).ChildRules(room =>
        {
            room.RuleFor(r => r.RoomType).NotEmpty().MaximumLength(20);
            room.RuleFor(r => r.Adults).InclusiveBetween(1, 4)
                .WithMessage("A room holds between one and four adults.");
            room.RuleFor(r => r.Children).InclusiveBetween(0, 4);
        });
    }
}