using System.Text.Json;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http.HttpResults;

namespace HolidayBookings.Api.Bookings;

internal static class BookingEndpoints
{
    const string _route = "/api/bookings";
    public static RouteGroupBuilder MapBookingEndpoints(this IEndpointRouteBuilder app)
    {
        var bookings = app.MapGroup(_route).WithTags("Bookings");

        bookings.MapGet("/", ListAsync).WithName("ListBookings");
        bookings.MapGet("/{id:guid}", GetAsync).WithName("GetBooking");

        bookings.MapPost("/", CreateAsync).WithName("CreateBooking");
        bookings.MapPut("/{id:guid}", UpdateAsync).WithName("UpdateBooking");
        bookings.MapDelete("/{id:guid}", DeleteAsync).WithName("DeleteBooking");

        return bookings;
    }

    internal static async Task<Ok<IReadOnlyList<Booking>>> ListAsync(
        IBookingRepository repository, CancellationToken ct) =>
        TypedResults.Ok(await repository.ListAsync(ct));

    internal static async Task<Results<Ok<Booking>, NotFound>> GetAsync(
        Guid id, IBookingRepository repository, CancellationToken ct) =>
        await repository.GetAsync(id, ct) is { } booking
            ? TypedResults.Ok(booking)
            : TypedResults.NotFound();


    /// <summary>
    /// Validates shared fields, then defers to the resolved handler for type-specific validation and supplier confirmation.
    /// </summary>
    internal static async Task<Results<Created<Booking>, ValidationProblem, ProblemHttpResult>> CreateAsync(
        BookingInput input,
        IValidator<BookingInput> sharedValidator,
        IBookingHandlerResolver handlers,
        IBookingRepository repository,
        TimeProvider clock,
        CancellationToken ct)
    {
        var shared = await sharedValidator.ValidateAsync(input, ct);

        if (!shared.IsValid)
        {
            return TypedResults.ValidationProblem(ToProblemErrors(shared));
        }

        var handler = handlers.For(input.Details);
        var typeSpecific = await handler.ValidateAsync(input.Details, ct);

        if (!typeSpecific.IsValid)
        {
            return TypedResults.ValidationProblem(ToProblemErrors(typeSpecific, "details"));
        }

        var outcome = await handler.ConfirmAsync(input.Details, ct);

        if (outcome is BookingOutcome.Rejected rejected)
        {
            return TypedResults.Problem(
                detail: rejected.Reason,
                statusCode: StatusCodes.Status409Conflict);
        }

        var confirmed = (BookingOutcome.Confirmed)outcome;

        var booking = new Booking(
            Guid.CreateVersion7(),
            input.PrimaryGuestName,
            input.PrimaryGuestEmail,
            confirmed.SupplierReference,
            input.Details,
            clock.GetUtcNow());

        await repository.AddAsync(booking, ct);

        return TypedResults.Created($"/api/bookings/{booking.Id}", booking);
    }

    internal static async Task<Results<Ok<Booking>, NotFound, ValidationProblem, ProblemHttpResult>> UpdateAsync(
        Guid id,
        BookingInput input,
        IValidator<BookingInput> sharedValidator,
        IBookingHandlerResolver handlers,
        IBookingRepository repository,
        TimeProvider clock,
        CancellationToken ct)
    {
        var shared = await sharedValidator.ValidateAsync(input, ct);

        if (!shared.IsValid)
        {
            return TypedResults.ValidationProblem(ToProblemErrors(shared));
        }

        if (await repository.GetAsync(id, ct) is not { } existing)
        {
            return TypedResults.NotFound();
        }

        // not an edit - delete and make new bookings
        if (existing.Details.GetType() != input.Details.GetType())
        {
            return TypedResults.Problem(
                detail: $"A booking cannot change from {existing.Details.Discriminator} to {input.Details.Discriminator}. Delete it and create a new one.",
                statusCode: StatusCodes.Status409Conflict);
        }

        var handler = handlers.For(input.Details);
        var typeSpecific = await handler.ValidateAsync(input.Details, ct);

        if (!typeSpecific.IsValid)
        {
            return TypedResults.ValidationProblem(ToProblemErrors(typeSpecific, "details"));
        }

        // An amended booking is not re-confirmed with the supplier here. 
        // Change rules differ sharply by type (a hotel re-prices, a ticketed flight may refuse outright), 
        // which is the follow-on step where IBookingHandler grows a CanChange member.
        var updated = existing with
        {
            PrimaryGuestName = input.PrimaryGuestName,
            PrimaryGuestEmail = input.PrimaryGuestEmail,
            Details = input.Details,
            UpdatedAt = clock.GetUtcNow(),
        };

        // Fails if another request changed the booking before this write.
        if (!await repository.UpdateAsync(existing, updated, ct))
        {
            return TypedResults.Problem(
                detail: "The booking was modified by another request. Re-read it and try again.",
                statusCode: StatusCodes.Status409Conflict);
        }

        return TypedResults.Ok(updated);
    }

    internal static async Task<Results<NoContent, NotFound>> DeleteAsync(
        Guid id, IBookingRepository repository, CancellationToken ct) =>
        await repository.RemoveAsync(id, ct)
            ? TypedResults.NoContent()
            : TypedResults.NotFound();

    
    /// <summary>
    /// Error keys must match the JSON the client sent, not the CLR property names FluentValidation
    /// reports. Per-type errors take a "details." prefix so a nested field is distinguishable from
    /// a top-level one of the same name.
    /// </summary>
    private static Dictionary<string, string[]> ToProblemErrors(ValidationResult result, string? prefix = null) =>
        result.Errors
            .GroupBy(failure => prefix is null
                ? ToJsonPath(failure.PropertyName)
                : $"{prefix}.{ToJsonPath(failure.PropertyName)}")
            .ToDictionary(
                group => group.Key,
                group => group.Select(failure => failure.ErrorMessage).Distinct().ToArray());

    /// <summary>Rooms[0].Adults becomes rooms[0].adults — each segment, indexers left intact.</summary>
    private static string ToJsonPath(string propertyName) =>
        string.Join('.', propertyName.Split('.').Select(JsonNamingPolicy.CamelCase.ConvertName));

}