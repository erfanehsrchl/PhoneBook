# Architecture Decisions

## ADR-001: Clean Architecture

Status: Accepted

### Context

The exercise needs visible boundaries between business rules, use cases, technical implementations, and HTTP delivery.

### Decision

Use Domain, Application, Infrastructure, and Api projects with dependencies pointing inward. Api is the composition root.

### Consequences

Domain remains framework-independent, while wiring requires explicit project references and dependency registration.

## ADR-002: Contact as Aggregate Root

Status: Accepted

### Context

A contact's identity, details, phone number, tag, and audit timestamps change as one consistency boundary.

### Decision

Model `Contact` as the Aggregate Root and route its state transitions through domain methods.

### Consequences

Callers cannot directly set Contact state; Contact protects its invariants and audit consistency.

## ADR-003: Domain Value Objects

Status: Accepted

### Context

Identifiers, names, phone numbers, and tags have domain meaning and validation beyond their primitive storage types.

### Decision

Represent `ContactId`, `FirstName`, `LastName`, `PhoneNumber`, and `Tag` as Value Objects.

### Consequences

Domain intent and equality are explicit, at the cost of small conversion and mapping steps at boundaries.

## ADR-004: Tag as Value Object

Status: Accepted

### Context

A tag has no independent identity, lifecycle, or operations in this exercise.

### Decision

Model Tag as a case-insensitive Value Object rather than an Entity.

### Consequences

Tag comparison expresses domain equality without requiring separate persistence or identity management.

## ADR-005: Explicit Application Exceptions

Status: Accepted

### Context

Validation, missing data, uniqueness conflicts, and other expected failures need stable semantics without coupling Domain or Application to HTTP.

### Decision

Throw FluentValidation's `ValidationException` for input failures and explicit Application exception types for not-found, conflict, and business-rule failures. Give application exceptions stable error codes.

### Consequences

Handlers and repositories return their natural values, while one API exception handler maps known exceptions to consistent HTTP responses. Callers must understand the documented exception contracts.

## ADR-006: Contact-specific Repository

Status: Accepted

### Context

Use cases need Contact-oriented operations, including tag filtering and atomic phone uniqueness behavior.

### Decision

Define `IContactRepository` in Application with operations shaped around Contact use cases.

### Consequences

Application is persistence-agnostic without losing domain vocabulary in a generic data-access abstraction.

## ADR-007: In-memory Persistence

Status: Accepted

### Context

Durable database persistence is outside the interview exercise scope.

### Decision

Implement `IContactRepository` with a Singleton, process-local in-memory repository.

### Consequences

The solution remains focused and easy to run, but all data is lost on restart and cannot be shared across processes.

## ADR-008: Immutable Persistence Snapshots

Status: Accepted

### Context

Storing mutable Contact references would allow retrieved aggregates to alter persisted state without a repository update.

### Decision

Store immutable Contact snapshots and rehydrate a new Contact instance for each read.

### Consequences

Reads are isolated from persisted state; snapshot mapping and invariant-checked rehydration add a small amount of code.

## ADR-009: Atomic Phone Uniqueness

Status: Accepted

### Context

A separate uniqueness pre-check and write would permit races between concurrent requests.

### Decision

Keep Contact storage and the canonical phone-number index under one repository lock for checks and mutations.

### Consequences

Uniqueness is atomic within one repository instance, but this guarantee is neither distributed nor multi-process.

## ADR-010: Explicit CancellationToken

Status: Accepted

### Context

Request cancellation must be able to flow through every asynchronous application and persistence boundary.

### Decision

Require a non-optional `CancellationToken` on handlers and repository operations and propagate it from controllers.

### Consequences

Cancellation intent is explicit and testable; callers must deliberately supply a token, including `CancellationToken.None`.

## ADR-011: Auditable Entity Capability

Status: Accepted

### Context

Contact requires audit timestamps, but auditing is not an invariant of every possible Entity.

### Decision

Have Contact implement `IAuditableEntity` instead of placing audit properties on the Entity base class.

### Consequences

Auditing is opt-in and clear, while generic consumers can still recognize auditable domain types.

## ADR-012: Direct UTC System Time

Status: Accepted

### Context

Create and update handlers need current UTC time, and this exercise does not require replaceable system-time access.

### Decision

Capture `DateTime.UtcNow` once in each create or update operation and pass that value into the aggregate.

### Consequences

The implementation stays small and avoids a single-method abstraction. Tests assert timestamps against a bounded UTC interval rather than an exact injected instant.

## ADR-013: No Unit of Work

Status: Accepted

### Context

The in-memory implementation has one repository and no deferred persistence or multi-repository transaction.

### Decision

Do not introduce a Unit of Work abstraction.

### Consequences

The persistence API stays minimal; a future durable design may add transaction boundaries if actual use cases require them.

## ADR-014: No Generic Repository

Status: Accepted

### Context

Generic CRUD would hide Contact-specific queries and consistency rules without reducing meaningful duplication.

### Decision

Use only the Contact-specific repository contract.

### Consequences

Use cases remain expressive and focused, while future aggregates would receive contracts based on their own needs.

## ADR-015: No AutoMapper

Status: Accepted

### Context

The solution has a small number of straightforward mappings.

### Decision

Map HTTP contracts, commands, and Contact responses explicitly.

### Consequences

Mappings are discoverable and compile-time visible without adding configuration or another dependency.

## ADR-016: CQRS with MediatR

Status: Accepted

### Context

The API operations benefit from independently named command/query use cases and a shared pipeline.

### Decision

Represent writes as commands, reads as queries, and dispatch both through MediatR.

### Consequences

Controllers remain thin and cross-cutting pipeline behavior is reusable, with some additional request/handler types.

## ADR-017: Validation Pipeline

Status: Accepted

### Context

Input-shape validation should run consistently before handlers without duplicating checks in controllers.

### Decision

Use FluentValidation validators invoked by a MediatR `ValidationBehavior`; keep business invariants in Domain.

### Consequences

Invalid input short-circuits uniformly by throwing `ValidationException`, while repository-only concerns such as atomic uniqueness stay out of validators.

## ADR-018: Custom API Response Contracts

Status: Accepted

### Context

HTTP clients need consistent, machine-readable success and failure payloads without coupling Application or Domain to ASP.NET Core.

### Decision

Wrap successful payloads in `ApiResponse<T>`, map known exceptions globally to `ApiResponse` or `ValidationApiResponse`, and handle unexpected exceptions with a safe generic error shape. A successful delete remains an empty `204` response.

### Consequences

Clients receive stable status, message, error-code, and data fields; HTTP policy remains isolated in the delivery layer and unexpected details stay private.
