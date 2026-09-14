using FluentValidation;

internal sealed class BookingRequestValidator : AbstractValidator<BookingRequest>
{
    public BookingRequestValidator()
    {
        RuleFor(x => x.Type)
        .NotEmpty()
        .WithMessage("Type is required, e.g. hotel, vehicle or event.");

        RuleFor(x => x.PrimaryGuestName)
        .NotEmpty()
        .MaximumLength(200);

        RuleFor(x => x.PrimaryGuestEmail)
        .NotEmpty()
        .EmailAddress();

        RuleFor(x => x.EndDate)
        .GreaterThanOrEqualTo(x => x.StartDate)
        .WithMessage("End date must be on or after the start date.");

    }
}