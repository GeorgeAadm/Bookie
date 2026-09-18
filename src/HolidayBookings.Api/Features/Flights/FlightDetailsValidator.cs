using FluentValidation;

namespace HolidayBookings.Api.Flights;

internal sealed class FlightDetailsValidator : AbstractValidator<FlightDetails>
{
    internal static readonly string[] PassengerTypes = ["ADT", "CHD", "INF"];

    internal static readonly string[] CabinClasses = ["ECONOMY", "PREMIUM_ECONOMY", "BUSINESS", "FIRST"];

    private const int MaximumSegments = 6;
    private const int MaximumPassengers = 9;

    public FlightDetailsValidator(TimeProvider clock)
    {
        RuleLevelCascadeMode = CascadeMode.Stop;

        RuleFor(x => x.CabinClass)
            .Must(CabinClasses.Contains)
            .WithMessage($"Cabin class must be one of: {string.Join(", ", CabinClasses)}.");

        RuleFor(x => x.Segments)
            .NotEmpty()
            .WithMessage("At least one flight segment is required.")
            .Must(segments => segments.Count <= MaximumSegments)
            .WithMessage($"An itinerary cannot exceed {MaximumSegments} segments.")
            .Must(BeAConnectedItinerary)
            .WithMessage("Each segment must depart from the previous segment's destination.")
            .Must(BeInChronologicalOrder)
            .WithMessage("Each segment must depart after the previous one arrives.");

        RuleForEach(x => x.Segments).ChildRules(segment =>
        {
            segment.RuleLevelCascadeMode = CascadeMode.Stop;

            segment.RuleFor(s => s.MarketingCarrier)
                .Matches("^[A-Z0-9]{2}$")
                .WithMessage("Must be a two-character IATA airline code, for example BA.");

            segment.RuleFor(s => s.FlightNumber)
                .Matches("^[0-9]{1,4}$")
                .WithMessage("Flight number must be one to four digits.");

            segment.RuleFor(s => s.Origin)
                .Matches("^[A-Z]{3}$")
                .WithMessage("Origin must be a three-letter IATA airport code, for example CPT.");

            segment.RuleFor(s => s.Destination)
                .Matches("^[A-Z]{3}$")
                .WithMessage("Destination must be a three-letter IATA airport code.")
                .NotEqual(s => s.Origin)
                .WithMessage("Destination must differ from the origin.");

            segment.RuleFor(s => s.DepartsAt)
                .GreaterThanOrEqualTo(_ => clock.GetUtcNow())
                .WithMessage("Departure cannot be in the past.");

            segment.RuleFor(s => s.ArrivesAt)
                .GreaterThan(s => s.DepartsAt)
                .WithMessage("Arrival must be after departure.");
        });

        RuleFor(x => x.Passengers)
            .NotEmpty()
            .WithMessage("At least one passenger is required.")
            .Must(passengers => passengers.Count <= MaximumPassengers)
            .WithMessage($"A single booking cannot exceed {MaximumPassengers} passengers.")
            .Must(passengers => passengers.Any(p => p.Type == "ADT"))
            .WithMessage("An itinerary must include at least one adult passenger.")
            // Infants travel on a lap, so they cannot outnumber the adults.
            .Must(passengers =>
                passengers.Count(p => p.Type == "INF") <= passengers.Count(p => p.Type == "ADT"))
            .WithMessage("There cannot be more infants than adults.");

        RuleForEach(x => x.Passengers).ChildRules(passenger =>
        {
            passenger.RuleFor(p => p.FullName).NotEmpty().MaximumLength(200);
            passenger.RuleFor(p => p.Type)
                .Must(PassengerTypes.Contains)
                .WithMessage($"Passenger type must be one of: {string.Join(", ", PassengerTypes)}.");
        });
    }

    private static bool BeAConnectedItinerary(IReadOnlyList<FlightSegment> segments) =>
        segments.Zip(segments.Skip(1)).All(pair => pair.First.Destination == pair.Second.Origin);

    private static bool BeInChronologicalOrder(IReadOnlyList<FlightSegment> segments) =>
        segments.Zip(segments.Skip(1)).All(pair => pair.Second.DepartsAt > pair.First.ArrivesAt);
}