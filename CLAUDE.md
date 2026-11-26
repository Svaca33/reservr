# CLAUDE.md

Reservr is a booking system for sports venues, starting with bowling. It is currently a walking skeleton (.NET 10, Aspire 17, PostgreSQL, Minimal API); the brief and the reasons behind each decision are in [docs/skeleton.md](docs/skeleton.md).

## Commands

```sh
aspire run                                   # PostgreSQL container + API + dashboard (Docker required)
dotnet build
dotnet test                                  # all tests; integration tests need Docker
dotnet test tests/Reservr.Domain.Tests       # domain unit tests, no Docker
dotnet test --filter "FullyQualifiedName~LaneTests.Lane_with_non_positive_number_throws"   # one test
dotnet ef migrations add <Name> --project Reservr.Api --output-dir Infrastructure/Migrations
```

`dotnet ef migrations add` does not connect to a database (`DesignTimeDbContextFactory` supplies a dummy connection). Migrations are applied when the API starts in Development; the database is not persisted, so every `aspire run` starts empty.

Hooks in `.claude/settings.json` gate your work: `git commit` is blocked unless the domain tests pass, and finishing a turn is blocked unless `dotnet build` succeeds.

## Architecture

Vertical Slice Architecture. One ticket should touch one slice, so parallel agents rarely edit the same file.

- **Slice**: `Reservr.Api/Features/<Area>/<UseCase>/<UseCase>.cs` holds the endpoint, request/response records and handler for one use case (model: `Features/Lanes/ListLanes/ListLanes.cs`). A slice implements `IEndpoint` and is discovered by reflection in `Infrastructure/Endpoints.cs`, so adding one leaves `Program.cs` untouched. Slices never reference each other; shared rules belong in `Reservr.Domain`.
- **Slice = one use case** (one command or one query) in its own namespace `Reservr.Api.Features.<Area>.<UseCase>`; nothing lives directly in an area namespace, and `tests/Reservr.ArchitectureTests` (no Docker) fails on a type outside a use-case namespace, on any reference between use cases (even within one area), and on Domain depending on EF Core or ASP.NET Core.
- **Data access**: handlers inject `ReservrDbContext` directly. There is no repository layer and no MediatR.
- **Domain**: `Reservr.Domain` has no EF Core or ASP.NET Core dependency so it stays testable without a database. Entities enforce invariants in the constructor (throwing `ArgumentOutOfRangeException` etc.), generate their own `Guid.CreateVersion7()` ids, and expose private setters for EF.
- **Persistence**: one `IEntityTypeConfiguration<T>` per entity in `Reservr.Api/Infrastructure`, picked up by `ApplyConfigurationsFromAssembly`, so adding an entity leaves `ReservrDbContext.OnModelCreating` untouched (add only the `DbSet`). Tables and columns are mapped explicitly to snake_case, ids use `ValueGeneratedNever()`, and domain invariants are mirrored as database check constraints (e.g. `ck_lanes_number_positive`).
- **Hosting**: `Reservr.AppHost` wires the `postgres` container, the `reservr` database and the `api` project (run with the `http` launch profile to avoid a dev-certificate dependency; health check at `/health`). The API reads the `reservr` connection string from Aspire.

## Integration tests

`tests/Reservr.IntegrationTests` starts the whole AppHost once per test collection (`ReservrAppFixture`) and calls the API over HTTP. All tests share one database, so each test that writes data seeds it with raw SQL through `fixture.DatabaseConnectionString` (snake_case table names) and deletes it in a `finally`, leaving the database empty for the others.
