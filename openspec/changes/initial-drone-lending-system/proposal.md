# Proposal

## Why

Drones and accessories are lent to student groups today by means of a sheet of paper on the cupboard door. Nobody can reliably tell which drone is available, which group holds it, how many batteries and propellers were handed out, or what happened in the past. The Drone Lending System replaces that sheet with a traceable, persistent record, and it is the tool the class will use itself in the second semester.

## What Changes

- Introduce the domain concepts **Drone**, **StudentGroup**, **Loan** and **LoanItem** (ubiquitous language, used unchanged in specs, code, database and UI).
- Create and list drones; a drone's availability (`Available` | `Loaned`) is derived from whether it has an active loan and is never entered by hand (REQ-001, REQ-002).
- Create and list student groups (REQ-003).
- Loan an available drone to a student group, together with batteries and propellers as non-negative integer quantities (REQ-004, REQ-006).
- Enforce at most one active loan per drone; loaning an already loaned drone is rejected (REQ-005).
- List active loans with group, drone, battery count, propeller count and loan date (REQ-007).
- Return a loan: the loan becomes `Returned`, its return date is recorded and the drone becomes available again (REQ-008, REQ-009).
- Keep returned loans permanently and list them as loan history (REQ-010).
- All business rules and validation are enforced by the ASP.NET Core Web API; the Angular frontend only talks to the REST/JSON API.
- Replace the `dotnet new webapi` WeatherForecast sample and the Angular welcome page with the real application.

### Out of Scope

- Login, user accounts, roles.
- Individual inventory of batteries (e.g. `BAT-001`) — accessories are quantities only.
- Reservations in advance.
- Damage management and return differences (missing accessories at return).
- Reports, statistics, exports.
- Editing or deleting drones, student groups or loans (not required by REQ-001–REQ-010; loans must never be deleted).
- Extended group fields (Class, Members, ResponsibleStudent) — possible later change requests.

## Capabilities

### New Capabilities

- `drone`: Registering drones (name, description) and listing them with their derived availability status.
- `student-group`: Registering student groups and listing them as borrowers.
- `loan`: Loaning a drone with accessory quantities to a student group, the one-active-loan rule, returning a loan, and the active-loan and loan-history views.

### Modified Capabilities

None — `openspec/specs/` is empty; this is the initial change.

## Impact

- **Backend** (`backend/DroneInventory.Api`): new domain model, EF Core with SQLite (new NuGet dependencies), initial migration, REST endpoints for drones, student groups and loans; WeatherForecast sample removed. New test project for backend tests.
- **Frontend** (`frontend/drone-inventory-ui`): new routes and components for drones, student groups, loan creation, active loans and loan history; an HTTP API client; default welcome page and its test replaced.
- **Data**: a new SQLite database file holding drones, student groups, loans and loan items.
- **Traceability**: every task in `tasks.md` references the REQ IDs it implements so it can be turned into a GitHub Issue.
