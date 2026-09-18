using FluentValidation;

namespace HolidayBookings.Api.Hotels;

internal sealed class HotelDetailsValidator : AbstractValidator<HotelDetails>
{
    private const int MaximumNights = 30;
    private const int MaximumRooms = 5;

    public HotelDetailsValidator(TimeProvider clock)
    {
        RuleLevelCascadeMode = CascadeMode.Stop;

        RuleFor(x => x.HotelName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.City).NotEmpty().MaximumLength(100);

        RuleFor(x => x.CheckIn)
            .GreaterThanOrEqualTo(_ => DateOnly.FromDateTime(clock.GetUtcNow().UtcDateTime))
            .WithMessage("Check-in cannot be in the past.");

        RuleFor(x => x.CheckOut)
            .GreaterThan(x => x.CheckIn)
            .WithMessage("Check-out must be after check-in.")
            .DependentRules(() =>
            {
                // only meaningful once the dates are the right way round
                RuleFor(x => x.Nights)
                    .LessThanOrEqualTo(MaximumNights)
                    .WithMessage($"A stay cannot exceed {MaximumNights} nights. Split it into two bookings.");
            });

        RuleFor(x => x.Rooms)
            .NotEmpty()
            .WithMessage("At least one room is required.")
            .Must(rooms => rooms.Count <= MaximumRooms)
            .WithMessage($"A single booking cannot hold more than {MaximumRooms} rooms.");

        RuleForEach(x => x.Rooms).ChildRules(room =>
        {
            room.RuleFor(r => r.RoomType).NotEmpty().MaximumLength(20);
            room.RuleFor(r => r.Adults).InclusiveBetween(1, 4)
                .WithMessage("A room holds between one and four adults.");
            room.RuleFor(r => r.Children).InclusiveBetween(0, 4);
        });
    }
}