# Walking skeleton: Reservr

Reservr is a booking system for sports venues, starting with bowling.
Goal: an empty but runnable system that an agent can start, test and
navigate. No booking features yet.

## Decisions

- .NET 10, Aspire 17, PostgreSQL run by Aspire as a container.
- API style: Minimal API.
- Architecture: Vertical Slice Architecture. Each use case is one slice in
  Reservr.Api/Features/<Area>/<UseCase>.cs (endpoint, request/response, handler).
  Slices do not reference each other. Shared rules live in Reservr.Domain,
  the DbContext in Reservr.Api/Infrastructure. Handlers use the DbContext
  directly: no repository layer, no MediatR.
  Reason: tickets are vertical slices, so one ticket should touch one slice,
  and parallel agents should rarely edit the same file.
- Reservr.Domain has no dependency on EF Core or ASP.NET Core.
  Reason: the domain must stay testable without a database.

## Acceptance criteria

1. Projects: Reservr.AppHost, Reservr.ServiceDefaults, Reservr.Api,
   Reservr.Domain, tests/Reservr.Domain.Tests (xUnit, unit tests),
   tests/Reservr.IntegrationTests (Aspire testing). One solution file
   at the repo root.
2. `aspire run` from the repo root starts PostgreSQL, the API and the
   Aspire dashboard.
3. `GET /lanes` returns 200 and a JSON array of lanes read from the
   database. With an empty database it returns [].
4. Reservr.Domain has a Lane entity with an id and a number.
   The number must be positive.
   Examples: number 1 is valid; 0 and -3 throw ArgumentOutOfRangeException.
5. `dotnet test tests/Reservr.Domain.Tests` runs without Docker.
6. `dotnet test` at the repo root runs all tests. Integration tests
   start the AppHost and call GET /lanes (Docker required).
7. The schema comes from an EF Core migration checked into the repo and
   is applied automatically when the API starts in Development.
8. README.md says what this is, how to run it and how to test it.
9. GET /lanes is implemented as a slice in Reservr.Api/Features/Lanes/ListLanes.cs.

## Out of scope

Reservations, UI, authentication, CI, CLAUDE.md, .claude/settings.json.
