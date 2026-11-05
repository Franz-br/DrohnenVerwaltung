# Tasks

Each task is meant to become one GitHub Issue, one branch and one pull request. REQ IDs show traceability to the requirements.

## 1. Backend setup

- [x] 1.1 Remove the WeatherForecast sample from `Program.cs` and `DroneInventory.Api.http`, add a root `.gitignore` (bin/, obj/, `*.db`, node_modules/), and verify `dotnet build` succeeds and `git status` no longer lists `bin/`/`obj/` (#1)
- [x] 1.2 Add EF Core SQLite + Design packages, `DroneInventoryDbContext`, connection string `Data Source=drone-inventory.db`, `TimeProvider` registration and Development-only `Database.Migrate()` (design D4, D7); verify `dotnet build` succeeds (#2)
- [x] 1.3 Create test project `backend/DroneInventory.Api.Tests` (xUnit, `Microsoft.AspNetCore.Mvc.Testing`), `backend/DroneInventory.slnx`, a `WebApplicationFactory` fixture using in-memory SQLite and a fake `TimeProvider`, and the `ProblemDetails` exception handler for `ValidationException`/`NotFoundException`/`ConflictException` (design D6, D8); verify `dotnet test` in `backend/` runs a smoke test green (#3)

## 2. Drone (backend) — REQ-001, REQ-002

- [x] 2.1 Drone domain model: `Drone` entity (Id, Name, Description), `DroneStatus` enum, EF configuration with unique `NOCASE` name index, migration `AddDrone`; verify the migration applies on a fresh database (#4)
- [x] 2.2 Drone API: `POST /api/drones` and `GET /api/drones` with status computed from active loans (design D1, D5) — status initially always `Available`; verify integration tests for every scenario of "Create Drone" and "List Drones" in `specs/drone/spec.md` pass (#5)

## 3. StudentGroup (backend) — REQ-003

- [x] 3.1 StudentGroup domain model: `StudentGroup` entity (Id, Name), EF configuration with unique `NOCASE` name index, migration `AddStudentGroup`; verify the migration applies on a fresh database (#6)
- [x] 3.2 StudentGroup API: `POST /api/student-groups` and `GET /api/student-groups`; verify integration tests for every scenario in `specs/student-group/spec.md` pass (#7)

## 4. Loan (backend) — REQ-004 to REQ-010

- [x] 4.1 Loan domain model: `Loan` aggregate (Id, DroneId, StudentGroupId, LoanDate, ReturnDate, Status) with `LoanItem` (Id, LoanId, Type, Quantity), enums `LoanStatus`/`LoanItemType`, `Loan.Create(...)` validating type, duplicate type and non-negative quantity (design D2); verify domain unit tests for the "Loan Accessories" validation scenarios pass (#8)
- [x] 4.2 Loan persistence: EF configuration for `Loans`/`LoanItems`, filtered unique index `Loans(DroneId) WHERE Status = 'Active'`, migration `AddLoan` (design D3); verify a test inserting two active loans for one drone fails at the database (#9)
- [x] 4.3 Loan API create: `POST /api/loans` with 404 for unknown drone/group, 409 for a drone with an active loan (also on `DbUpdateException`), 400 for invalid items, loan date from `TimeProvider` (REQ-004, REQ-005, REQ-006); verify integration tests for every scenario of "Loan Drone", "No Double Loan" and "Loan Accessories" pass (#10)
- [x] 4.4 Derived drone status: verify via integration tests that `GET /api/drones` shows `Loaned` with an active loan and `Available` after return (scenarios of "Derived Drone Status" in `specs/drone/spec.md`) (#11)
- [x] 4.5 Active loan list: `GET /api/loans?status=Active` returning group, drone, batteries, propellers, loan date (REQ-007); verify integration tests for "List Active Loans" pass (#12)
- [x] 4.6 Return operation: `Loan.Return(now)` and `POST /api/loans/{id}/return` setting `Returned` and `ReturnDate`, 409 on second return, 404 on unknown loan (REQ-008, REQ-009); verify domain and integration tests for "Return Loan" and "Record Return Date" pass (#13)
- [x] 4.7 Loan history: `GET /api/loans?status=Returned` including return date, no delete route (REQ-010); verify integration tests for "Keep Loan History" pass, including unchanged quantities and loan date after return (#14)
- [x] 4.8 Update `DroneInventory.Api.http` with one example request per endpoint (TELLO-07, Group 3, 4 batteries, 8 propellers); verify each request runs against `dotnet run` with the expected status code (#15)

## 5. Frontend setup

- [x] 5.1 Replace the welcome page with a nav shell and lazy routes `/drones`, `/student-groups`, `/loans/new`, `/loans`, `/loans/history` (default `/drones`), add `provideHttpClient()`, `proxy.conf.json` (`/api` → `http://localhost:5092`) and API model types (design D9); update `app.spec.ts` and verify `ng test` and `ng build` pass (#16)

## 6. Drone (frontend) — REQ-001, REQ-002

- [x] 6.1 `DroneApi` service and drone list page showing name, status and description; verify a Vitest test with `HttpTestingController` renders the list (#17)
- [ ] 6.2 Drone form (name, description) that creates a drone, refreshes the list and shows backend errors (e.g. duplicate name); verify a component test for success and a 409 error (#18)

## 7. StudentGroup (frontend) — REQ-003

- [ ] 7.1 `StudentGroupApi` service and student group page with list and create form showing backend errors; verify component tests for list, create and error display (#19)

## 8. Loan (frontend) — REQ-004 to REQ-010

- [ ] 8.1 `LoanApi` service and loan form: select an available drone and a student group, enter batteries and propellers (`min 0`), submit and show backend errors (REQ-004–REQ-006); verify component tests that only available drones are offered and a 409 message is displayed (#20)
- [ ] 8.2 Active loan list table (Group | Drone | Batteries | Propellers | Loan Date as `dd.MM.yyyy`) (REQ-007); verify a component test renders the example rows from the spec (#21)
- [ ] 8.3 Return action on each active loan that calls the return endpoint and removes the loan from the list (REQ-008, REQ-009); verify a component test for the return flow (#22)
- [ ] 8.4 Loan history table including return date (REQ-010); verify a component test renders returned loans with loan and return date (#23)

## 9. Integration check

- [ ] 9.1 End-to-end walkthrough with `dotnet run` and `ng serve`: create TELLO-07 and Group 3, loan with 4 batteries and 8 propellers, try a second loan (rejected), return, check history and that TELLO-07 is available; verify `dotnet test` and `ng test` are green and record the result in the PR (#24)

## Workflow follow-up

- Create GitHub Issues from these tasks with `/openspec-to-github` and add them to the GitHub Project.
- After all issues are implemented, reviewed and merged, archive the change with `/opsx:archive` and verify `openspec/specs/drone`, `student-group` and `loan` contain the requirements.
