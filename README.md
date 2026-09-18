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




## Did the extensibility goal work?

Hotels came first. Vehicles, events and flights followed as a test of exactly this, and each cost one folder and two shared lines. The service layer is closed: no endpoint has been touched to add a product type. `Every_product_type_books_through_the_same_endpoint` asserts four very different payloads, one route.

Three honest qualifications.

**Open/Closed** A pure implementation would discover types by assembly scanning, so adding one would edit nothing at all. 
I prototyped that and chose against it. With a handful of types, explicit registration reads better, the compiler checks the types exist, OpenAPI sees the derived schemas, and there is no startup reflection to reason about.

`BookingTypeCoverageTests` turns a forgotten line into a failing build, which buys back the safety that discovery would have given. I would switch if product types arrived from plugin assemblies, or if another team added them and could not edit shared files.

**The discriminator lives in two places.** It is declared in the `[JsonDerivedType]` attribute and mirrored by a `Discriminator` property on each record. `The_discriminator_property_matches_the_attribute` asserts the two never disagree, but it is duplication and worth noticing.

**It holds because booking is a single act here.** See below.

## Why one endpoint for every product type

This API assumes the consumer has already searched the supplier and selected a product. 
The call here records and confirms a choice already made. 

Since selection happens upstream and the brief assumes availability, booking is one uniform act regardless of product, and one endpoint serves all of them.

The rule to apply as this grows: **resolve on differing rules, split endpoints on differing operation shapes.** 
These four differ in rules, so one endpoint and a resolver is right. 
A product with a genuine two-phase lifecycle (a cruise cabin held pending final payment) differs in shape,
and that needs a booking status lifecycle with explicit transition endpoints rather than a separate creation route per type.

## Design notes

**Validation and confirmation are separate concerns.** 
Validation answers "is this request coherent" 
(check-out after check-in, a real ACRISS code, no more infants than adults, an itinerary whose segments actually connect). 

Confirmation answers "will a supplier sell it" 
(a 45-minute minimum connection is a perfectly well-formed itinerary that no airline will carry, so `FlightBookingHandler` returns 409 rather than 400).

**Types differ in data, not yet in behaviour.** 
The brief assumes availability and defines no change or cancellation rules, which is what makes one set of endpoints clean. 
In reality a hotel cancels free until a cut-off, an event ticket is non-refundable, a ticketed flight refuses a name
change outright. `IBookingHandler` is where that belongs, growing `CanChange` and `Cancel` members
each type answers differently. That is the next commit I would go for.

**Date types are per product type, deliberately.** 
Hotel stay dates are `DateOnly`, because they are local to the property and carry no timezone. A vehicle pickup is a `DateTimeOffset`, because it happens at a specific hour at a branch and a one-way rental can cross a timezone. A flight
carries an offset per segment, each local to a different airport. An event is a single moment. 
No flat model expresses all four correctly, which is the concrete argument for polymorphic details over one shape with nullable columns.

**Industry codes rather than invented ones.** 
ACRISS vehicle categories, ISO 3166 country codes for driver licences, IATA airport and airline codes, ADT/CHD/INF passenger types. Real supplier contracts would map to OTA or NDC messages in an adapter at the edge rather than adopting those
schemas internally.

**`TimeProvider` is injected**, so rules like "check-in cannot be in the past" are testable
without freezing the machine clock.

**The repository is async from day one** and returns `IReadOnlyList<T>` rather than `IQueryable<T>`, so the storage technology cannot leak into handlers. Swapping the in-memory store for EF Core is one registration line and a new implementation. `ConcurrentDictionary` is a correctness requirement, not a preference: the store is a singleton shared across concurrent
requests.

**`UpdateAsync` takes the expected value alongside the new one.** That is optimistic concurrency:
`ConcurrentDictionary.TryUpdate` makes the check and the write atomic, and because `Booking` is an immutable record, value equality does the comparison with no version column. A caller working from a stale read gets a 409 instead of silently overwriting someone else's change. Against EF Core the same signature maps onto a rowversion check.

**Error keys match the JSON the client sent.** FluentValidation reports CLR property names, so `ToProblemErrors` converts each path segment to camelCase and prefixes per-type failures with `details.`, giving `details.rooms[0].adults` rather than `Rooms[0].Adults`.

## Known limitations

- Limits such as maximum stay length, party size and segment count are hard-coded constants. They
  belong in configuration so commercial can change them without a deploy; `IOptions<BookingLimits>`
  bound from `appsettings.json` is the shape.
- An unrecognised `type` discriminator returns the framework's default 400 rather than a message
  listing the known types. Correct status, less helpful body.
- Delete really deletes. A real system cancels: a state change carrying a fee or refund that
  leaves the record auditable. The brief asked for removal, so removal is what it does.
- Optimistic concurrency is enforced inside the API but not exposed to clients. ETags with
  `If-Match` would extend it to two users editing the same booking from a browser.
- Nothing carries a price. The consumer knows it, having selected the product upstream, so a money
  value on the booking would be the natural next field.

## What I would add next, in order

1. Per-type change and cancellation rules on `IBookingHandler`, with a booking status lifecycle.
2. A supplier product reference on `BookingInput`, since the consumer already holds one.
3. Grouping bookings under a trip. The industry models a customer's purchases as one record — a
   PNR, order or itinerary — each item confirmed against a different supplier.
4. Idempotency keys on create, so a retried request cannot double-book.
