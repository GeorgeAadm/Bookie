using FluentValidation;

namespace HolidayBookings.Api.Vehicles;

/// <summary>
/// TimeProvider is injected so the past-date rule is testable without freezing the machine clock.
/// </summary>
internal sealed class VehicleDetailsValidator : AbstractValidator<VehicleDetails>
{
    private const int MaximumRentalDays = 90;
    private const int MinimumRentalHours = 1;

    public VehicleDetailsValidator(TimeProvider clock)
    {
        RuleFor(x => x.Category)
            .NotEmpty()
            .Matches("^[A-Z]{4}$")
            .WithMessage("Category must be a four-letter ACRISS code, for example CDMR.");

        RuleFor(x => x.PickupLocation).NotEmpty().MaximumLength(100);
        RuleFor(x => x.DropoffLocation).NotEmpty().MaximumLength(100);

        RuleFor(x => x.DriverLicenceCountry)
            .NotEmpty()
            .Matches("^[A-Z]{2}$")
            .WithMessage("Driver licence country must be an ISO 3166-1 alpha-2 code, for example ZA.");

        RuleFor(x => x.PickupAt)
            .GreaterThanOrEqualTo(_ => clock.GetUtcNow())
            .WithMessage("Pickup cannot be in the past.");

        RuleFor(x => x.DropoffAt)
            .GreaterThan(x => x.PickupAt)
            .WithMessage("Drop-off must be after pickup.");

        RuleFor(x => x)
            .Must(x => (x.DropoffAt - x.PickupAt).TotalHours >= MinimumRentalHours)
            .WithName(nameof(VehicleDetails.DropoffAt))
            .WithMessage($"A rental must run for at least {MinimumRentalHours} hour.")
            .When(x => x.DropoffAt > x.PickupAt);

        RuleFor(x => x.RentalDays)
            .LessThanOrEqualTo(MaximumRentalDays)
            .WithMessage($"A rental cannot exceed {MaximumRentalDays} days. Use a long-term lease instead.")
            .When(x => x.DropoffAt > x.PickupAt);
    }
}