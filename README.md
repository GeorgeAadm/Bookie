# Bookie — Holiday Bookings API

A .NET 10 Minimal API for adding, editing and removing holiday bookings across different product
types: hotels, vehicles, events and flights. Built as a demo for a technical assessment.

## Running it

```bash
dotnet build
dotnet test
dotnet run --project src/HolidayBookings.Api
```

The API listens on `http://localhost:5292`. `src/HolidayBookings.Api/Bookings.http` has a request
for every endpoint including the failure cases, and is the quickest way to see it working. The
OpenAPI document is at `/openapi/v1.json` in development.

## What this is

The brief asked for a basic solution allowing someone to add, edit and delete a booking, with
in-memory storage and availability assumed.

Beyond that I valued one design question most: **how easily can this grow to sell new kinds of
product? ** Today it is hotels and hire cars; next quarter it is cruises, transfers, guided tours.
If adding each one means touching the endpoints, the system fights every new product line.

The shape of the solution is the answer to that question. Everything else is deliberately small.

Not production code: no persistence, no authentication, no pricing, no payment, no supplier
integration. Availability is stubbed, as the brief allows.

## Endpoints

| Method | Route | Notes |
|---|---|---|
| `GET` | `/api/bookings` | All product types in one response |
| `GET` | `/api/bookings/{id}` | |
| `POST` | `/api/bookings` | One endpoint, every product type |
| `PUT` | `/api/bookings/{id}` | Cannot change a booking's product type |
| `DELETE` | `/api/bookings/{id}` | |

Failures return `ProblemDetails`: 400 for validation, 404 for a missing booking, 409 for a type
change, a concurrent edit, or a booking the supplier refuses.

## How it works

A booking is a shared envelope (who booked it, our id, the supplier's reference) plus a
polymorphic `details` object carrying everything type-specific. The `type` discriminator selects
the shape:

```json
{
  "primaryGuestName": "Alex Morgan",
  "primaryGuestEmail": "alex@example.com",
  "details": {
    "type": "hotel",
    "hotelName": "Sea View Hotel",
    "city": "Cape Town",
    "checkIn": "2026-12-18",
    "checkOut": "2026-12-22",
    "rooms": [{ "roomType": "DBL-SEA", "adults": 2, "children": 1 }]
  }
}
```

Each product type owns a folder under `Features/` with three files: the details record, a
FluentValidation validator, and an `IBookingHandler` that validates and confirms it. The endpoints
resolve a handler and never switch on type:

```csharp
var handler = handlers.For(input.Details);

var typeSpecific = await handler.ValidateAsync(input.Details, ct);
if (!typeSpecific.IsValid) return TypedResults.ValidationProblem(...);

var outcome = await handler.ConfirmAsync(input.Details, ct);
```

`BookingHandler<TDetails>` does the cast once and delegates validation, so a concrete handler only
writes the confirmation step.

**Adding a product type is three new files plus two lines** a `[JsonDerivedType]` entry on
`IBookingDetails` and a registration in `BookingRegistration`. No endpoint, resolver or other
product type changes.

## Tests

19 tests across four files:

```
BookingTypeCoverageTests    every product type is declared, registered, and its discriminator agrees
```
```
PerTypeValidationTests      per-type rules in isolation, including a regression guard against one
                            mistake reporting three errors
```
```
RepositoryConcurrencyTests  a stale write is rejected and the first writer's change survives
```
```
BookingApiTests             end-to-end add, edit, delete, all four types through one endpoint,
                            per-type rules and the type-change guard at the HTTP boundary
```

Coverage is deliberately shallow but aimed at what breaks silently: 
registration when a producttype is added, 
the per-type rules that only run through a runtime-resolved handler, 
and the concurrency check that is invisible until two requests collide.
