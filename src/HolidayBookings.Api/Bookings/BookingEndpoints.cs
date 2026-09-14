using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
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

    internal static async Task<Ok<IReadOnlyList<Booking>>> ListAsync(IBookingRepository repository, CancellationToken ct)
        => TypedResults.Ok(await repository.ListAsync(ct)); 
        
    internal static async Task<Results<Ok<Booking>, NotFound>> GetAsync(Guid id, IBookingRepository repository, CancellationToken ct)
        => await repository.GetAsync(id, ct) is { } existing
            ? TypedResults.Ok(existing)
            : TypedResults.NotFound();
        
    internal static async Task<Results<Created<Booking>, ValidationProblem>> CreateAsync(
        BookingRequest br,
        IValidator<BookingRequest> validator,
        IBookingRepository repository,
        CancellationToken ct)
    {
        var validation = await validator.ValidateAsync(br);

        if (!validation.IsValid)
        {
            return TypedResults.ValidationProblem(validation.ToDictionary());
        }

        var booking = new Booking(
            Guid.CreateVersion7(),
            br.Type,
            br.PrimaryGuestName,
            br.PrimaryGuestEmail,
            br.StartDate,
            br.EndDate,
            br.Notes,
            DateTimeOffset.UtcNow);

        await repository.AddAsync(booking, ct);

        return TypedResults.Created($"{_route}/{booking.Id}", booking);
    }
    internal static async Task<Results<Ok<Booking>, NotFound, ValidationProblem>> UpdateAsync(
        Guid id,
        BookingRequest br,
        IValidator<BookingRequest> validator,
        IBookingRepository repository,
        CancellationToken ct)
    {
        var validation = await validator.ValidateAsync(br);

        if (!validation.IsValid)
        {
            return TypedResults.ValidationProblem(validation.ToDictionary());
        }
        
        if (await repository.GetAsync(id, ct) is not { } existing)
        {
            return TypedResults.NotFound();
        }

        var updated = existing with
        {
            Type = br.Type,
            PrimaryGuestName = br.PrimaryGuestName,
            PrimaryGuestEmail = br.PrimaryGuestEmail,
            StartDate = br.StartDate,
            EndDate = br.EndDate,
            Notes = br.Notes,
            UpdatedAt = DateTimeOffset.UtcNow,
        };

        await repository.UpdateAsync(updated, ct);

        return TypedResults.Ok(updated);
    }

    internal static async Task<Results<NoContent, NotFound>> DeleteAsync(Guid id, IBookingRepository repository, CancellationToken ct)
        => await repository.RemoveAsync(id, ct)
            ? TypedResults.NoContent()
            : TypedResults.NotFound();
        
}