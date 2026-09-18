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
        RuleFor(x => x.CabinClass)
            .NotEmpty()
            .Must(CabinClasses.Contains)
            .WithMessage($"Cabin class must be one of: {string.Join(", ", CabinClasses)}.");

        RuleFor(x => x.Segments)
            .NotEmpty()
            .WithMessage("At least one flight segment is required.");

        RuleFor(x => x.Segments)
            .Must(segments => segments.Count <= MaximumSegments)
            .WithMessage($"An itinerary cannot exceed {MaximumSegments} segments.")
            .When(x => x.Segments is not null);

        RuleForEach(x => x.Segments).ChildRules(segment =>
        {
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

        // A connection has to leave from where the previous segment landed, and after it landed.
        RuleFor(x => x.Segments)
            .Must(BeAConnectedItinerary)
            .WithName(nameof(FlightDetails.Segments))
            .WithMessage("Each segment must depart from the previous segment's destination.")
            .When(x => x.Segments is { Count: > 1 });

        // A connection has to leave only after previous arrival landed.
        RuleFor(x => x.Segments)
            .Must(BeInChronologicalOrder)
            .WithName(nameof(FlightDetails.Segments))
            .WithMessage("Each segment must depart after the previous one arrives.")
            .When(x => x.Segments is { Count: > 1 });

        RuleFor(x => x.Passengers)
            .NotEmpty()
            .WithMessage("At least one passenger is required.");

        RuleFor(x => x.Passengers)
            .Must(passengers => passengers.Count <= MaximumPassengers)
            .WithMessage($"A single booking cannot exceed {MaximumPassengers} passengers.")
            .When(x => x.Passengers is not null);

        RuleFor(x => x.Passengers)
            .Must(passengers => passengers.Any(p => p.Type == "ADT"))
            .WithName(nameof(FlightDetails.Passengers))
            .WithMessage("An itinerary must include at least one adult passenger.")
            .When(x => x.Passengers is { Count: > 0 });

        // Infants travel on an adult's lap, so they cannot outnumber the adults.
        RuleFor(x => x.Passengers)
            .Must(passengers =>
                passengers.Count(p => p.Type == "INF") <= passengers.Count(p => p.Type == "ADT"))
            .WithName(nameof(FlightDetails.Passengers))
            .WithMessage("There cannot be more infants than adults.")
            .When(x => x.Passengers is { Count: > 0 });

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