# Reservr

Reservr is a booking system for sports venues, starting with bowling.

Right now it is a walking skeleton: an empty but runnable system with one
endpoint (`GET /lanes`) and no booking features yet. See
[docs/skeleton.md](docs/skeleton.md) for the brief and the decisions behind it.

## Prerequisites

- .NET 10 SDK
- Aspire CLI 17 (`dotnet tool install -g aspire.cli`)
- Docker (runs PostgreSQL; also needed by the integration tests)

## Run

From the repo root:

```sh
aspire run
```

This starts PostgreSQL in a container, the API and the Aspire dashboard. The
CLI prints the dashboard URL; the API URL is listed there under the `api`
resource. On startup in Development the API applies EF Core migrations, so the
database schema is created automatically. The database is not persisted: each
run starts with an empty database.

```sh
curl http://localhost:5147/lanes   # => []
```

If the browser warns about the dashboard's certificate, run `aspire certs trust` once.

## Test

```sh
dotnet test                            # all tests (Docker required)
dotnet test tests/Reservr.Domain.Tests # domain unit tests only (no Docker)
```

- `tests/Reservr.Domain.Tests`: xUnit unit tests for the domain. No database.
- `tests/Reservr.IntegrationTests`: start the whole AppHost with Aspire testing
  (PostgreSQL + API) and call the API over HTTP.

## Layout

| Project | What it contains |
| --- | --- |
| `Reservr.AppHost` | Aspire AppHost: PostgreSQL container + API |
| `Reservr.ServiceDefaults` | OpenTelemetry, health checks, resilience, service discovery |
| `Reservr.Api` | Minimal API, vertical slices |
| `Reservr.Domain` | Entities and rules. No EF Core or ASP.NET Core dependency |

The API uses Vertical Slice Architecture:

- `Reservr.Api/Features/<Area>/<UseCase>.cs`: one file per use case holding the
  endpoint, request/response and handler. Example: `Features/Lanes/ListLanes.cs`.
  A slice implements `IEndpoint` and is discovered at startup, so adding one
  does not touch `Program.cs`. Slices do not reference each other.
- `Reservr.Api/Infrastructure`: `ReservrDbContext`, one `IEntityTypeConfiguration`
  per entity, and `Migrations/`. Handlers use the DbContext directly (no
  repositories, no MediatR).

## Database migrations

```sh
dotnet ef migrations add <Name> --project Reservr.Api --output-dir Infrastructure/Migrations
```

Creating a migration does not need a running database. Migrations are applied
when the API starts in Development.
