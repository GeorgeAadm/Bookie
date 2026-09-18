using FluentValidation;

namespace HolidayBookings.Api.Vehicles;

internal sealed class VehicleDetailsValidator : AbstractValidator<VehicleDetails>
{
    private const int MaximumRentalDays = 90;
    private const int MinimumRentalHours = 1;

    public VehicleDetailsValidator(TimeProvider clock)
    {
        RuleLevelCascadeMode = CascadeMode.Stop;

        RuleFor(x => x.Category)
            .Matches("^[A-Z]{4}$")
            .WithMessage("Category must be a four-letter ACRISS code, for example CDMR.");

        RuleFor(x => x.DriverLicenceCountry)
            .Matches("^[A-Z]{2}$")
            .WithMessage("Driver licence country must be an ISO 3166-1 alpha-2 code, for example ZA.");

        RuleFor(x => x.PickupLocation).NotEmpty().MaximumLength(100);
        RuleFor(x => x.DropoffLocation).NotEmpty().MaximumLength(100);

        RuleFor(x => x.PickupAt)
            .GreaterThanOrEqualTo(_ => clock.GetUtcNow())
            .WithMessage("Pickup cannot be in the past.");

        RuleFor(x => x.DropoffAt)
            .GreaterThan(x => x.PickupAt)
            .WithMessage("Drop-off must be after pickup.")
            .DependentRules(() =>
            {
                RuleFor(x => x)
                    .Must(x => (x.DropoffAt - x.PickupAt).TotalHours >= MinimumRentalHours)
                    .WithName(nameof(VehicleDetails.DropoffAt))
                    .WithMessage($"A rental must run for at least {MinimumRentalHours} hour.")
                    .Must(x => x.RentalDays <= MaximumRentalDays)
                    .WithName(nameof(VehicleDetails.DropoffAt))
                    .WithMessage($"A rental cannot exceed {MaximumRentalDays} days.");
            });
    }
}