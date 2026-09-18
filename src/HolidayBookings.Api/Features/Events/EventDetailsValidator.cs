using FluentValidation;

namespace HolidayBookings.Api.Events;

internal sealed class EventDetailsValidator : AbstractValidator<EventDetails>
{
    private const int MaximumTickets = 20;

    public EventDetailsValidator(TimeProvider clock)
    {
        RuleLevelCascadeMode = CascadeMode.Stop;

        RuleFor(x => x.EventName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Venue).NotEmpty().MaximumLength(200);
        RuleFor(x => x.SeatCategory).NotEmpty().MaximumLength(50);

        RuleFor(x => x.PerformanceAt)
            .GreaterThan(_ => clock.GetUtcNow())
            .WithMessage("The performance has already started.");

        RuleFor(x => x.Tickets)
            .InclusiveBetween(1, MaximumTickets)
            .WithMessage($"Between 1 and {MaximumTickets} tickets can be booked at once.");
    }
}