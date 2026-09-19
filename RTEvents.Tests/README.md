# RTEvents.Tests

Run the tests from the repository root:

```powershell
dotnet test RTEvents.Tests/RTEvents.Tests.csproj
```

Collect coverage:

```powershell
dotnet test RTEvents.Tests/RTEvents.Tests.csproj --collect:"XPlat Code Coverage" --settings RTEvents.Tests/coverage.runsettings
```

The Cobertura report is written under `RTEvents.Tests/TestResults/`.

## Scope

- Database entities: constructor validation, event updates and lifecycle, ticket holds and capacity boundaries, purchase initialization, payment transitions.
- Core: event CRUD and validation, ticket purchases and idempotency branches, transaction commit/rollback, ticket availability, reporting totals and pagination, outbox delivery/retry limits, payment response processing, worker scope disposal and cancellation, domain exceptions.
- API: controller status codes and arguments, response DTO mapping, request validation, error responses, and HTTP route/binding checks.

Tests use xUnit and Moq. `Support/MockDatabase.cs` supplies mock DbSets backed by LINQ-to-objects, mock saves and mock transactions. No SQL Server, SQLite, EF in-memory provider, connection string or database setup is needed. The helper does not simulate EF tracking, relationship fixup, SQL translation, or rollback of entity state.

The small TestServer host loads the actual controllers and exception handler with mocked dependencies. It does not execute application startup, migrations or the background worker. Controller unit tests remain separate from those HTTP pipeline checks. Worker tests cancel immediately instead of waiting for its 90-second interval.

Coverage excludes database configuration/migrations, application startup and generated code. Simple property bags and EF-only private constructors are not tested just to raise the coverage percentage.

## Existing behavior to keep in mind

These tests leave production code unchanged. They expose these current behaviors and limitations:

- `ErrorsController` maps over-capacity and mismatched-idempotency exceptions to HTTP 400, although their domain statuses are 422 and 409.
- `UpdateEventRequestDTO.VenueId` is a string with a minimum-length constraint; the controller does not forward it to the service.
- Reporting currently includes held ticket costs in sales totals.
- Updating event capacity does not recalculate `AvailableTicketCount`.
- A newly added idempotency key is not assigned the new purchase ID. The replay tests supply an existing key with a purchase ID; they do not establish that an initial purchase can be replayed end to end.

Database persistence, concurrency, migrations and production startup require separate integration tests and are outside this suite's scope.
