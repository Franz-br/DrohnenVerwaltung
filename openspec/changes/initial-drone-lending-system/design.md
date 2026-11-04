# Design

## Context

See proposal.md – Why. The repository currently contains two untouched templates:

- `backend/DroneInventory.Api`: `dotnet new webapi` on .NET 10 (minimal API, `Microsoft.AspNetCore.OpenApi`, WeatherForecast sample, dev URL `http://localhost:5092`).
- `frontend/drone-inventory-ui`: `ng new` on Angular 22 (standalone, signals, Vitest via `ng test`, empty `app.routes.ts`). `frontend/drone-inventory-ui/CLAUDE.md` sets the Angular conventions (signals, `inject()`, native control flow, Signal Forms, WCAG AA).

Fixed constraints from the assignment: ASP.NET Core 10 Web API + EF Core + SQLite in the backend, Angular in the frontend, REST/JSON between them, all business rules in the backend, ubiquitous language Drone / StudentGroup / Loan / LoanItem.

## Goals / Non-Goals

**Goals:**
- A backend that enforces every rule in `specs/loan/spec.md` even if the frontend is bypassed (e.g. via the `.http` file).
- One source of truth for drone availability.
- Every spec scenario covered by an automated backend test.
- A small layered backend that students can explain line by line.

**Non-Goals:**
- Authentication, CORS for production hosting, deployment.
- Pagination, filtering or sorting beyond what the views need.
- Update/delete endpoints.

## Decisions

### D1 – Drone status is computed, not stored
`Drone` has no status column. Status is `Loaned` if a `Loan` with `Status = Active` exists for the drone, otherwise `Available`; it is computed in the query that builds the drone list.
*Alternative:* a `Status` column on `Drone` updated on loan/return – rejected, two truths can drift ("Zwei Wahrheiten sind eine Fehlerquelle").
`Loan.Status` (`Active` | `Returned`) **is** stored, because it is the fact itself, not a derivation.

### D2 – Loan is the aggregate root; LoanItem belongs to it
`Loan` owns its `LoanItem`s (`Type`: `Battery` | `Propeller`, `Quantity`). LoanItems are only created through the loan and never accessed on their own. They are mapped as a separate table `LoanItems` with FK `LoanId` (matches the domain model in the assignment) rather than an EF owned type, so the schema stays readable for students. `Drone` and `StudentGroup` are separate aggregates referenced by id.
Domain behaviour lives on the entity: a static factory `Loan.Create(droneId, groupId, items, now)` validates quantities/types, `loan.Return(now)` performs Active → Returned and rejects a second return. This keeps rules unit-testable without HTTP or a database.

### D3 – One active loan per drone is guaranteed by the database too
The service checks for an existing active loan before creating one (→ 409). Additionally a **filtered unique index** on `Loans(DroneId) WHERE Status = 'Active'` makes a race between two simultaneous requests fail at the database; the resulting `DbUpdateException` is mapped to 409 as well.
*Alternative:* service check only – rejected, not safe under concurrency.

### D4 – Backend structure
Single project, folders instead of extra assemblies:
- `Domain/` – entities and enums (`Drone`, `StudentGroup`, `Loan`, `LoanItem`, `LoanStatus`, `LoanItemType`, `DroneStatus`), domain exceptions.
- `Data/` – `DroneInventoryDbContext`, EF configuration, `Migrations/`.
- `Application/` – `DroneService`, `StudentGroupService`, `LoanService` (use cases, return DTOs).
- `Contracts/` – request/response records.
- `Endpoints/` – minimal API route groups (`MapDroneEndpoints`, …).
*Alternative:* controllers – rejected, the template already uses minimal APIs and they keep the code shorter.
Time is taken from an injected `TimeProvider` so tests can control loan and return dates.

### D5 – REST API

| Method | Route | Purpose | REQ |
|---|---|---|---|
| POST | `/api/drones` | create drone `{name, description}` → 201 | 001 |
| GET | `/api/drones` | list `{id, name, description, status}` | 002 |
| POST | `/api/student-groups` | create group `{name}` → 201 | 003 |
| GET | `/api/student-groups` | list `{id, name}` | 003 |
| POST | `/api/loans` | `{droneId, studentGroupId, items:[{type, quantity}]}` → 201 | 004–006 |
| GET | `/api/loans?status=Active` | active loans | 007 |
| GET | `/api/loans?status=Returned` | loan history | 010 |
| POST | `/api/loans/{id}/return` | return loan → 200 with updated loan | 008, 009 |

Loan response: `{id, droneId, droneName, studentGroupId, studentGroupName, loanDate, returnDate, status, batteries, propellers}` – flattened quantities so the frontend tables need no logic. Enums are serialized as strings. Dates are stored and returned in UTC (ISO 8601); the frontend formats them as `dd.MM.yyyy`.
No DELETE route for loans exists at all (REQ-010).

### D6 – Error mapping
Errors are returned as RFC 7807 `ProblemDetails`:
- 400 – validation (empty/duplicate-type/negative quantity/unknown type, empty name).
- 404 – unknown drone, student group or loan.
- 409 – double loan, return of a returned loan, duplicate drone/group name (names have a unique index, case-insensitive via `COLLATE NOCASE`).
Domain exceptions (`ValidationException`, `NotFoundException`, `ConflictException`) are translated in one place (exception handler middleware).

### D7 – Persistence
EF Core with `Microsoft.EntityFrameworkCore.Sqlite`; connection string `Data Source=drone-inventory.db` in `appsettings.json`. Schema via EF migrations (`dotnet ef`, tool `dotnet-ef`); in Development the app runs `Database.Migrate()` at startup so a fresh clone works with `dotnet run`. The `.db` file is git-ignored.

### D8 – Tests
- Backend: new project `backend/DroneInventory.Api.Tests` (xUnit). Domain unit tests for `Loan`; API integration tests with `WebApplicationFactory<Program>` against an in-memory SQLite connection (real SQLite, so the filtered unique index is exercised) and a fake `TimeProvider`. A `backend/DroneInventory.slnx` groups both projects so `dotnet test` runs from `backend/`.
- Frontend: Vitest component/service tests via `ng test` with `HttpTestingController`.

### D9 – Frontend structure
Lazy-loaded routes: `/drones`, `/student-groups`, `/loans/new`, `/loans` (active), `/loans/history`; default redirect to `/drones`. A shared nav in `App`. One `@Service` per API resource (`DroneApi`, `StudentGroupApi`, `LoanApi`) using `HttpClient` (`provideHttpClient()`), state in signals, forms with Signal Forms. Client-side validation is only for convenience (required name, `min 0` quantities); the frontend displays the backend's `ProblemDetails` message on 400/404/409. The loan form only offers drones with status `Available`.
API base URL: Angular dev-server proxy (`proxy.conf.json`, `/api` → `http://localhost:5092`), so no CORS configuration is needed in development.

## Risks / Trade-offs

- [Computed status costs a subquery per drone] → negligible at classroom scale; index on `Loans(DroneId, Status)` covers it.
- [SQLite filtered indexes and `COLLATE NOCASE` are SQLite-specific] → acceptable, SQLite is a fixed project constraint; configured in one EF configuration class.
- [`Database.Migrate()` at startup can hide migration errors] → only in Development; migrations are still committed and reviewable.
- [Name uniqueness is an assumption not stated in REQ-001/REQ-003] → chosen because names like "TELLO-07" identify drones physically; recorded in the specs so it can be reviewed and changed before implementation.
- [UTC storage vs. local display] → loan date shown by the frontend in the browser's time zone; a loan made shortly after midnight is still shown on the correct local day.

## Migration Plan

Initial change, no existing data. Remove the WeatherForecast sample and the Angular welcome page; create the initial EF migration. Rollback = revert the merge.
