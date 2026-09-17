using HolidayBookings.Api.Bookings;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<UnknownBookingTypeHandler>();

builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<IBookingRepository, InMemoryBookingRepository>();

builder.Services.AddBookingTypes();
builder.Services.ConfigureHttpJsonOptions(options =>
{
    // Polymorphism is declared by attributes on IBookingDetails
    options.SerializerOptions.AllowOutOfOrderMetadataProperties = true;
});

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapBookingEndpoints();

app.Run();

public partial class Program;
