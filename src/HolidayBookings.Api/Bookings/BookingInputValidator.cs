using FluentValidation;

namespace HolidayBookings.Api.Bookings;

/// <summary>
/// Only the fields every booking shares. 
/// Per-type rules live with their type and reach the endpoint through IBookingHandler.
/// This class never grows when a booking type is added.
/// </summary>
internal sealed class BookingInputValidator : AbstractValidator<BookingInput>
{
    public BookingInputValidator()
    {
        RuleFor(x => x.PrimaryGuestName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.PrimaryGuestEmail).NotEmpty().EmailAddress();

        RuleFor(x => x.Details)
            .NotNull()
            .WithMessage("Booking details are required, including the 'type' discriminator.");
    }
}