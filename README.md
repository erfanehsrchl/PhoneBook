# PhoneBook

## Overview

PhoneBook is a RESTful API built as a .NET interview exercise. It demonstrates Domain-Driven Design fundamentals, Clean Architecture, CQRS, Value Objects, application exceptions, thread-safe in-memory persistence, atomic phone-number uniqueness, snapshot isolation, FluentValidation, consistent API response envelopes, and integration testing. The solution is intentionally designed to demonstrate architectural and engineering practices while keeping the persistence model in memory, as required by the exercise; it is not presented as a production-ready service.

## Features

- Create, update, delete, and retrieve a Contact by ID.
- List all Contacts with pagination.
- Filter Contacts by Tag with pagination.
- Normalize supported Iranian mobile-number formats to a canonical `+989xxxxxxxxx` representation.
- Enforce canonical phone-number uniqueness atomically.
- Record UTC creation and update timestamps.
- Validate application inputs and domain invariants.
- Return consistent success and error response contracts.
- Expose Swagger/OpenAPI in the Development environment.
- Verify behavior with domain, application, infrastructure, concurrency, and HTTP integration tests.

## Architecture

The Domain project contains the model and rules without framework dependencies. Application contains CQRS use cases, validation, application exceptions, and the repository abstraction. Infrastructure implements persistence. Api owns HTTP contracts, maps exceptions to HTTP, and acts as the composition root that wires Application and Infrastructure.

```text
Api
 ├─> Application ─> Domain
 └─> Infrastructure ─> Application abstractions
                       └─> Domain
```

Dependencies point inward; Domain does not depend on MediatR, FluentValidation, persistence, or ASP.NET Core.

## Project Structure

```text
src/
  PhoneBook.Domain/                 Domain model and Value Objects
  PhoneBook.Application/            CQRS requests, handlers, validation, and abstractions
  PhoneBook.Infrastructure/         Thread-safe in-memory repository implementation
  PhoneBook.Api/                    ASP.NET Core controllers, contracts, and HTTP error mapping
tests/
  PhoneBook.Domain.UnitTests/       Domain behavior tests
  PhoneBook.Application.UnitTests/  Validator, pipeline, and handler tests
  PhoneBook.Infrastructure.UnitTests/ Repository and concurrency tests
  PhoneBook.Api.IntegrationTests/   End-to-end HTTP tests using WebApplicationFactory
docs/                               Architecture decisions and API examples
.github/workflows/                  Continuous integration workflow
```

## Technology Stack

- .NET SDK 9.0.102 and .NET/ASP.NET Core 9 (`net9.0`)
- MediatR 14.2.0
- FluentValidation 12.1.1
- Swashbuckle.AspNetCore 10.2.3
- xUnit 2.9.2 and FluentAssertions 8.10.0
- Microsoft.AspNetCore.Mvc.Testing 9.0.10
- Coverlet collector 6.0.4

## Domain Model

`Contact` is the Aggregate Root. `ContactId`, `FirstName`, `LastName`, `PhoneNumber`, and `Tag` are Value Objects. `PhoneNumber` owns supported-format normalization and Iranian mobile-number validation. `Contact` controls state transitions and keeps creation/update timestamps consistent. `Tag` is intentionally a Value Object because it has no independent identity or lifecycle in this exercise.

## Application Flow

```text
HTTP request
  -> API contract
  -> MediatR command/query
  -> ValidationBehavior
  -> Handler
  -> Domain
  -> Repository
  -> Application response or exception
  -> HTTP response
```

FluentValidation and explicit application exceptions represent expected failures. The global exception handler maps them to stable API contracts, while logging unexpected failures and returning a safe generic response.

## API Endpoints

| Method | Route | Success |
| --- | --- | --- |
| `POST` | `/api/contacts` | `201 Created` |
| `GET` | `/api/contacts/{id}` | `200 OK` |
| `GET` | `/api/contacts?pageNumber={pageNumber}&pageSize={pageSize}` | `200 OK` |
| `GET` | `/api/contacts/by-tag/{tag}?pageNumber={pageNumber}&pageSize={pageSize}` | `200 OK` |
| `PUT` | `/api/contacts/{id}` | `200 OK` |
| `DELETE` | `/api/contacts/{id}` | `204 No Content` |

List endpoints default to page 1 with 20 items and accept page sizes from 1 through 100. Common failures are `400` for validation, `404` for missing contacts, `409` for uniqueness conflicts, `422` for other expected business failures, and `500` for unexpected errors. Except for successful `204 No Content` deletion, responses use `ApiResponse<T>` for success or `ApiResponse` for errors. See [API examples](docs/api-examples.md) for complete samples.

## Running Locally

Prerequisite: the .NET 9 SDK compatible with `global.json`.

```bash
dotnet restore
dotnet build
dotnet run --project src/PhoneBook.Api
```

Use the URL printed by ASP.NET Core at startup and open `/swagger`. Swagger is available only in the Development environment.

## Running with Docker

```bash
docker build -t phonebook-api .
docker run --rm -p 8080:8080 phonebook-api
```

The container defaults to the Production environment, where Swagger is disabled. To enable it for local container exploration:

```bash
docker run --rm -p 8080:8080 -e ASPNETCORE_ENVIRONMENT=Development phonebook-api
```

The API is then reachable over HTTP at `http://localhost:8080`; Swagger is at `/swagger` when Development is enabled.

## Running Tests

```bash
dotnet test
```

The suite covers domain rules, application validators and handlers, repository snapshot/concurrency behavior, and real HTTP requests through ASP.NET Core's integration-test host.

## Code Coverage

Each test project uses the Coverlet collector. Generate Cobertura coverage files under ignored `TestResults` directories with:

```bash
dotnet test --collect:"XPlat Code Coverage"
```

No coverage threshold is enforced.

## Important Design Decisions

- **Application exceptions:** expected use-case and persistence failures use explicit exception types with stable error codes.
- **Contact Aggregate Root:** Contact controls its state transitions and invariants.
- **Value Objects:** primitive values with domain meaning are explicit types.
- **Repository interface location:** `IContactRepository` belongs to Application because use cases depend on it.
- **In-memory persistence:** the exercise does not require a database.
- **Snapshot isolation:** persistence stores immutable snapshots, not mutable aggregate references.
- **Atomic uniqueness:** contact and phone-index mutations share one synchronization boundary.
- **Mandatory CancellationToken:** asynchronous application and repository boundaries require an explicit token.
- **No Generic Repository:** the contract expresses Contact-specific use cases.
- **No Unit of Work:** there is no deferred persistence or multi-repository transaction boundary.
- **No AutoMapper:** the small mappings remain explicit.
- **Audit abstraction:** Contact implements `IAuditableEntity` without imposing audit fields on every Entity.
- **UTC timestamps:** create and update handlers capture `DateTime.UtcNow` once per operation.

Further rationale is recorded in [Architecture Decisions](docs/architecture-decisions.md).

## Concurrency and Thread Safety

`InMemoryContactRepository` is registered as a Singleton, and its state belongs to that repository instance; there is no static mutable state. Contact storage and the canonical phone-number index are checked and changed inside one lock, making uniqueness checks and writes atomic within the process. Reads rehydrate new Contact instances, so changing a retrieved Contact does not affect persisted state until `UpdateAsync` succeeds.

The repository is process-local. It does not provide distributed consistency, cross-process locking, or durable storage.

## Error Handling

Expected failures are represented by FluentValidation or explicit application exceptions and mapped globally: validation to `400`, not found to `404`, conflict to `409`, and other business failure to `422`. Validation responses add a field-keyed `errors` dictionary; other failures use the custom `ApiResponse` contract. The response body's `statusCode` always matches the HTTP status. Unexpected exceptions are logged and converted to a generic `500` response with error code `Server.UnexpectedError`. Stack traces and exception details are not returned to clients.

## Validation Strategy

- **API:** ASP.NET Core model binding creates HTTP contracts.
- **Application:** FluentValidation checks input shape before handlers run.
- **Domain:** Value Objects and Contact enforce business invariants.
- **Repository:** the synchronized write boundary enforces phone-number uniqueness atomically.

Phone uniqueness is deliberately not a FluentValidation rule: a pre-check followed by a separate write would race. The repository performs the uniqueness check and mutation under the same lock.

## Current Limitations

- Persistence is in memory; data is lost when the application restarts and state is process-local.
- There is no authentication or authorization.
- Pagination uses offset-based, process-local in-memory slicing rather than a durable cursor.
- There is no distributed cache, distributed lock, or database transaction support.
- There is no production observability stack.

These are deliberate interview-scope constraints rather than claims of production completeness.

## Possible Future Improvements

If product requirements justify them, future work could add EF Core persistence, database migrations, a database unique constraint on canonical phone numbers, real transaction boundaries, authentication and authorization, broader search or cursor-based pagination, structured production logging, metrics and tracing, and container orchestration when deployment needs it. These capabilities were intentionally excluded from the exercise scope.
