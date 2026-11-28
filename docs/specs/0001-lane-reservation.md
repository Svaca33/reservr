# Spec 0001: Lane reservation

## Problem

A customer phones the bowling centre (or walks in) and wants to book one or more lanes. The staff member taking the call has no way to record the booking in Reservr. Nothing stops two staff members from promising the same lane at the same time to two different customers.

## Solution

The staff member creates a reservation for the customer. A reservation holds one or more lanes. Each lane has its own time range, but all lanes are booked together for at least part of the time. The staff member enters the customer's name, plus a phone number, an email address and a note if they have them. Reservr accepts the reservation only if every lane is free for its whole range, the times sit on the half-hour grid, the length of each booking is allowed and no booking starts in the past. If anything is wrong, Reservr reports every problem at once and stores nothing. The staff member can then fix the request and send it again.

This spec depends on spec 0002 (staff member creates a lane). All this spec needs from it is that `POST /lanes` returns `201` with the new lane's `id`.

## User stories

1. As a staff member, I want to book a lane for a customer who phones in, so that the lane is held for them. (R1, R2, R4, R5, R6, R7)
2. As a staff member, I want to book several lanes in one reservation, each for its own time range, so that a group that arrives together but plays for different lengths is one booking. (R1)
3. As a staff member, I want Reservr to refuse a lane that is already booked at that time, so that two customers are never promised the same lane. (R3)
4. As a staff member, I want to see every problem with a request at once, so that I can fix it in one go while the customer is still on the phone. (R8)
5. As a staff member, I want to record the customer's name with an optional phone, email and note, so that we know who the reservation is for and how to reach them. (R5, R6)

## Rules and examples

Unless an example says otherwise:

- Now is 2026-12-05 17:30 Europe/Prague (UTC+01:00).
- All times are Europe/Prague local times on 2026-12-05.
- Each example starts with the staff member creating the lanes it names through `POST /lanes`, and the requests use the `id`s that call returns.
- Lane numbers in the examples ("lane 1", "lane 2") are only labels within that example. A test may create its lanes with any free number.
- Any reservation that already exists in a situation is created beforehand through `POST /reservations`.
- "Afterwards" in an expected outcome means a follow-up `POST /reservations` that shows a refused request held nothing.
- The Guards column names the faulty behaviour the example catches.

### R1 - A reservation has at least one lane booking, each lane appears at most once, and all lane bookings share at least one instant.

Each lane booking is a half-open range [start, end). Bookings that only touch, where one ends at the instant the other starts, do not share an instant.

| ID | Situation | Action | Expected outcome | Guards |
|---|---|---|---|---|
| R1.E1 | Lane 1 is free | Book lane 1 18:00–20:00 | 201; the reservation has one lane booking, lane 1 18:00–20:00 | A single-lane booking is refused. |
| R1.E2 | Lanes 1 and 2 are free | Book lane 1 18:00–20:00 and lane 2 18:00–20:00 | 201; the reservation has both lane bookings | A multi-lane booking is refused. |
| R1.E3 | Lanes 1 and 2 are free | Book lane 1 17:30–19:00 and lane 2 18:30–21:00 | 201; each lane keeps its own range | All lanes in a reservation are forced onto the same range. |
| R1.E4 | Lanes 1 and 2 are free | Book lane 1 18:00–19:00 and lane 2 18:30–20:00 (30 minutes in common) | 201 | A short common window is not enough. |
| R1.E5 | Lanes 1 and 2 are free | Book lane 1 18:00–19:00 and lane 2 19:00–20:00 | 400; error on `lanes`. Afterwards, booking lane 1 18:00–19:00 and lane 2 18:00–20:00 returns 201 | Touching ranges count as sharing an instant. |
| R1.E6 | Lanes 1 and 2 are free | Book lane 1 18:00–19:00 and lane 2 20:00–21:00 | 400; error on `lanes`. Afterwards, booking lane 1 18:00–19:00 returns 201 | Disjoint ranges are accepted as one reservation. |
| R1.E7 | — | Send a reservation with `lanes: []` | 400; error on `lanes` | A reservation without lanes is accepted. |
| R1.E8 | Lane 1 is free | Book lane 1 18:00–19:00 and lane 1 19:00–20:00 in one reservation | 400; error on `lanes[1].laneId`. Afterwards, booking lane 1 18:00–20:00 returns 201 | The same lane is accepted twice in one reservation. |

### R2 - Every lane booking starts and ends on a whole or half hour in Europe/Prague time, lasts 60 to 240 minutes of elapsed time, and its times carry a UTC offset.

Length is the elapsed time between the two instants, not the difference on the wall clock. Europe/Prague offsets are whole hours, so the grid is the same in Prague time and in UTC.

| ID | Situation | Action | Expected outcome | Guards |
|---|---|---|---|---|
| R2.E1 | Lane 1 is free | Book lane 1 18:00–19:30 | 201 | A half-hour end is refused. |
| R2.E2 | Lane 1 is free | Book lane 1 18:00–19:00 (60 minutes) | 201 | The minimum length is treated as exclusive. |
| R2.E3 | Lane 1 is free | Book lane 1 18:00–22:00 (240 minutes) | 201 | The maximum length is treated as exclusive. |
| R2.E4 | Lane 1 is free | Book lane 1 18:00–18:30 (30 minutes) | 400; error on `lanes[0].end` | A booking shorter than the minimum is accepted. |
| R2.E5 | Lane 1 is free | Book lane 1 18:00–22:30 (270 minutes) | 400; error on `lanes[0].end` | A booking longer than the maximum is accepted. |
| R2.E6 | Lane 1 is free | Book lane 1 18:15–19:15 | 400; error on `lanes[0].start` | An off-grid time is accepted when the length is valid. |
| R2.E7 | Lane 1 is free | Book lane 1 with start `2026-12-05T17:00:00Z` and end `2026-12-05T19:00:00Z` (18:00–20:00 Prague) | 201; the response shows start `2026-12-05T17:00:00Z` | A time in another offset is checked against the grid without converting it to Prague time. |
| R2.E8 | Lane 1 is free | Book lane 1 with start `2026-12-05T23:00:00+05:45` (18:15 Prague) and end `2026-12-06T01:00:00+05:45` | 400; error on `lanes[0].start` | The grid is checked in the sender's offset instead of Prague time. |
| R2.E9 | Lane 1 is free | Book lane 1 with start `2026-12-05T18:00:00` (no offset) | 400; error on `lanes[0].start` | A time without an offset is guessed at instead of refused. |
| R2.E10 | Lane 1 is free | Book lane 1 20:00–18:00 | 400; error on `lanes[0].end` | An end before the start yields a negative or ignored length. |
| R2.E11 | Lane 1 is free; on 2027-03-28 Prague clocks jump from 02:00 to 03:00 | Book lane 1 with start `2027-03-28T01:30:00+01:00` and end `2027-03-28T03:30:00+02:00` (60 minutes elapsed, 120 on the wall clock) | 201 | Length is measured on the wall clock instead of as elapsed time. |
| R2.E12 | Lane 1 is free; on 2027-03-28 Prague clocks jump from 02:00 to 03:00 | Book lane 1 with start `2027-03-28T01:30:00+01:00` and end `2027-03-28T02:30:00+02:00` (the same instant, 0 minutes) | 400; error on `lanes[0].end` | A zero-length booking across the spring change is accepted. |
| R2.E13 | Lane 1 is free; on 2027-10-31 Prague clocks fall back from 03:00 to 02:00 | Book lane 1 with start `2027-10-31T02:00:00+02:00` and end `2027-10-31T02:00:00+01:00` (60 minutes elapsed, 0 on the wall clock) | 201 | The repeated autumn hour is counted as 0 minutes. |
| R2.E14 | Lane 1 is free; on 2027-10-31 Prague clocks fall back from 03:00 to 02:00 | Book lane 1 with start `2027-10-31T01:00:00+02:00` and end `2027-10-31T04:00:00+01:00` (240 minutes elapsed) | 201 | 240 minutes across the autumn change is counted as 180 or 300. |
| R2.E15 | Lane 1 is free; on 2027-10-31 Prague clocks fall back from 03:00 to 02:00 | Book lane 1 with start `2027-10-31T00:30:00+02:00` and end `2027-10-31T04:00:00+01:00` (270 minutes elapsed, 210 on the wall clock) | 400; error on `lanes[0].end` | 270 minutes across the autumn change is accepted because the wall clock shows 210. |

### R3 - Lane bookings on the same lane never overlap, across all reservations and even when requests arrive at the same moment; back-to-back bookings are allowed with no gap.

In R3.E1–R3.E7, all times are on Sunday 2026-12-06, and lane 1 is already booked 18:00–20:00 that day by another reservation. The next day is used so that bookings before 18:00 are not in the past.

| ID | Situation | Action | Expected outcome | Guards |
|---|---|---|---|---|
| R3.E1 | Lane 1 booked 18:00–20:00 | Book lane 2 18:00–20:00 | 201 | Bookings on other lanes count as conflicts. |
| R3.E2 | Lane 1 booked 18:00–20:00 | Book lane 1 20:00–21:00 | 201 | A booking that starts exactly when another ends is refused. |
| R3.E3 | Lane 1 booked 18:00–20:00 | Book lane 1 17:00–18:00 | 201 | A booking that ends exactly when another starts is refused. |
| R3.E4 | Lane 1 booked 18:00–20:00 | Book lane 1 19:30–21:00 | 409; `conflicts` lists lane 1 19:30–21:00 | A partial overlap at the end is accepted. |
| R3.E5 | Lane 1 booked 18:00–20:00 | Book lane 1 18:30–19:30 | 409; `conflicts` lists lane 1 18:30–19:30 | A booking inside an existing one is accepted. |
| R3.E6 | Lane 1 booked 18:00–20:00 | Book lane 1 17:00–21:00 | 409; `conflicts` lists lane 1 17:00–21:00 | A booking that wraps an existing one is accepted. |
| R3.E7 | Lane 1 booked 18:00–20:00; lane 2 free | Book lane 2 19:00–21:00 and lane 1 19:30–21:00 in one reservation | 409; `conflicts` lists only lane 1 19:30–21:00. Afterwards, booking lane 2 19:00–21:00 returns 201 | Part of a reservation is stored when another part conflicts. |
| R3.E8 | Lane 3 is free | Send 10 identical requests for lane 3 18:00–20:00 in parallel | Exactly one returns 201 and the other nine return 409 | Concurrent requests double-book a lane. |

### R4 - No lane booking starts before now; there is no limit on how far ahead a reservation can be made.

| ID | Situation | Action | Expected outcome | Guards |
|---|---|---|---|---|
| R4.E1 | Now is 2026-12-05 17:30 Europe/Prague | Book lane 1 19:00–21:00 | 201 | A later booking on the same day is refused. |
| R4.E2 | Now is 2026-12-05 17:30 Europe/Prague | Book lane 1 17:30–19:00 | 201 | A booking that starts exactly now is refused. |
| R4.E3 | Now is 2026-12-05 17:30 Europe/Prague | Book lane 1 17:00–19:00 | 400; error on `lanes[0].start` | The end is checked instead of the start. |
| R4.E4 | Now is 2026-12-05 17:30 Europe/Prague | Book lane 1 19:00–21:00 and lane 2 17:00–20:00 | 400; error on `lanes[1].start`. Afterwards, booking lane 1 19:00–21:00 returns 201 | Only the first lane booking is checked, or the valid part is stored. |
| R4.E5 | Now is 2026-12-05 17:30 Europe/Prague | Book lane 1 2031-06-01 18:00–20:00 (Prague, UTC+02:00) | 201 | A hidden booking horizon refuses far-future dates. |

### R5 - The customer's name is required; phone and email are optional, but when given they must be well formed.

The name is trimmed and must be 1–100 characters after trimming. A phone is at most 30 characters and contains only digits, spaces and `+ - ( )`. An email is at most 254 characters and has exactly one `@` with a non-empty part on each side.

| ID | Situation | Action | Expected outcome | Guards |
|---|---|---|---|---|
| R5.E1 | Lane 1 is free | Book with name "Jan Novák" and phone "+420 777 123 456" | 201; the response shows both | The typical phone booking is refused. |
| R5.E2 | Lane 1 is free | Book with name "Jan Novák", no phone, no email | 201; phone and email are null | Phone or email is required. |
| R5.E3 | Lane 1 is free | Book with a 100-character name; separately, with a 101-character name | 100 characters: 201; 101 characters: 400, error on `customer.name` | The name length limit is off by one. |
| R5.E4 | Lane 1 is free | Book with name "  Jan  " | 201; the response shows name "Jan" | Surrounding spaces are stored. |
| R5.E5 | Lane 1 is free | Book with the name missing, `""` or `"   "` | 400; error on `customer.name` | A blank name passes as present. |
| R5.E6 | Lane 1 is free | Book with a 30-character phone; separately, with a 31-character phone | 30 characters: 201; 31 characters: 400, error on `customer.phone` | The phone length limit is off by one. |
| R5.E7 | Lane 1 is free | Book with phone "volat večer" | 400; error on `customer.phone` | Free text is accepted as a phone. |
| R5.E8 | Lane 1 is free | Book with email "jan@example.cz" | 201; the response shows the email | A valid email is refused. |
| R5.E9 | Lane 1 is free | Book with email "jan.example.cz"; separately, with "@example.cz" | 400; error on `customer.email` | An email without `@` or without a local part is accepted. |

### R6 - A reservation may carry an optional note of at most 500 characters.

| ID | Situation | Action | Expected outcome | Guards |
|---|---|---|---|---|
| R6.E1 | Lane 1 is free | Book with no note | 201; note is null | The note is required. |
| R6.E2 | Lane 1 is free | Book with a 500-character note | 201; the response shows the full note | The note is truncated, or the limit is treated as exclusive. |
| R6.E3 | Lane 1 is free | Book with a 501-character note | 400; error on `note` | A note over the limit is accepted. |

### R7 - Every lane booking refers to an existing lane by its id.

| ID | Situation | Action | Expected outcome | Guards |
|---|---|---|---|---|
| R7.E1 | Lane 1 was created through `POST /lanes` | Book lane 1 using the `id` that `POST /lanes` returned | 201; the response shows that `laneId` | Lanes are looked up by number instead of id. |
| R7.E2 | No lane has id `00000000-0000-7000-8000-000000000001` | Book that id 18:00–20:00 | 400; error on `lanes[0].laneId` | An unknown lane returns 201, 404 or 500. |

### R8 - A refused request reports every validation error at once.

| ID | Situation | Action | Expected outcome | Guards |
|---|---|---|---|---|
| R8.E1 | Lane 1 is free | Book with an empty name and lane 1 18:15–19:15 | 400; errors on both `customer.name` and `lanes[0].start` | Validation stops at the first error. |

## Interface

The behaviour is checked through the HTTP API.

### Create lane (prerequisite from spec 0002)

`POST /lanes`

The examples use it only to set up their situation. Its inputs, rules and other outcomes belong to spec 0002.

| Outcome | Response |
|---|---|
| Created | `201 Created`; the body has the new lane's `id` |

### Create reservation

`POST /reservations`

Inputs (JSON body):

- `customer.name`: string, required. Trimmed, 1–100 characters (R5).
- `customer.phone`: string or null. At most 30 characters; digits, spaces and `+ - ( )` only (R5).
- `customer.email`: string or null. At most 254 characters; exactly one `@` with a non-empty part on each side (R5).
- `note`: string or null. At most 500 characters (R6).
- `lanes`: array of lane bookings, at least one, each lane at most once, all sharing an instant (R1). Each lane booking has:
  - `laneId`: the lane id that `POST /lanes` returned (R7);
  - `start`, `end`: ISO 8601 date-times with a required UTC offset, for example `2026-12-05T18:00:00+01:00`. They lie on :00 or :30 Europe/Prague time, the range is [start, end), it lasts 60–240 minutes of elapsed time and start is not before now (R2, R4).

| Outcome | Response |
|---|---|
| Created | `201 Created`, `Location: /reservations/{id}`. The body has `id`, `customer` (`name`, `phone`, `email`), `note` and `lanes[]` (`laneId`, `start`, `end` in UTC with `Z`). |
| Invalid input, including an unknown lane | `400 Bad Request`, validation problem details. `errors` is keyed by field path (`customer.name`, `customer.phone`, `customer.email`, `note`, `lanes`, `lanes[i].laneId`, `lanes[i].start`, `lanes[i].end`) and lists every error at once. Nothing is stored; a follow-up booking of any valid part succeeds. |
| Conflict with another reservation | `409 Conflict`, problem details with `conflicts: [{ laneId, start, end }]`. It lists the lane bookings from this request that overlap an existing booking. It does not reveal the other reservation or its customer. Nothing is stored; a follow-up booking of the non-conflicting part succeeds. |

Validation runs first; the conflict check runs only on a request that passed validation.

### Setting the clock in tests

The API reads now from a time provider. By default it uses the system clock. When the configuration value `Clock:FixedNow` (ISO 8601 with offset) is set, the API uses that fixed instant instead, but only outside the Production environment.

- **Ordinary tests:** the integration test fixture passes `Clock__FixedNow=2026-12-05T17:30:00+01:00` to the `api` resource as an environment variable before the app is built. The `api` resource runs in Development. One fixed now applies to the whole test collection, so tests choose their dates relative to it.
- **Production with a fixed clock:** if `Clock:FixedNow` is set while the API runs in Production, the API refuses to start. The `api` resource never becomes healthy, and its log has an error that names `Clock:FixedNow`.
- **Production without a fixed clock:** the API starts with the system clock, and `/health` reports healthy.

Two integration tests start the API in Production to check this: one with `Clock:FixedNow`, one without. In Production, migrations are not applied, so these tests check only startup and `/health`.

There is no per-request time override, because it would be a security hole in production.

## Decisions

- A reservation holds 1..N lane bookings, each with its own range. A group arriving together is one booking, and plays of different lengths are still allowed.
- All lane bookings in a reservation must share at least one instant; ranges that only touch do not count. Unrelated time blocks belong in separate reservations.
- A lane appears at most once per reservation. Two blocks on one lane are two reservations, which keeps the model simple.
- Ranges are half-open [start, end), and back-to-back bookings are allowed with no gap. This matches how the venue hands lanes over.
- Grid of 30 minutes, length 60–240 minutes, fixed for now. Making grid and limits configurable later must stay possible.
- Length is elapsed time, not wall-clock difference. The lane is occupied for the elapsed time, including on days when the clocks change.
- Times are sent with a required offset, stored in UTC, and rules are evaluated in Europe/Prague. This avoids guessing at ambiguous local times while keeping venue rules local; it also means a nonexistent or ambiguous local time cannot be sent.
- No booking may start before now; starting exactly at now is allowed. Staff should not book time that has already passed.
- There is no booking horizon. The venue sees no reason to limit it.
- Opening hours are not enforced. The staff member knows them; enforcing them is a later, separate feature.
- Customer contact details (name required, phone and email optional) are stored on the reservation, and there is no customer entity or deduplication yet. The model must allow linking a customer account later, so that the customer web is not blocked.
- A name with no phone and no email is allowed. A customer may book in person.
- A reservation carries an optional note of up to 500 characters; the number of players is not recorded.
- Lanes are referenced by id, not number, because lane numbers are not unique.
- An unknown lane id is a 400 validation error, not a 404. It is bad input in the body, not a missing resource at the URL.
- Reservations are all-or-nothing, and every error is reported at once. The staff member can fix everything while on the phone.
- A conflict is a 409 that lists only the requester's own colliding bookings. This avoids leaking another customer's data.
- No overlap is guaranteed by a database exclusion constraint over lane and time range, which mirrors the domain invariant like the existing check constraints. Only the database can prevent double booking under concurrent requests.
- The concurrency rule is checked with 10 parallel identical requests that expect exactly one 201. A correct system always passes, and a racy one fails with high probability.
- Lanes for the examples are created through `POST /lanes` (spec 0002), not raw SQL. Acceptance tests use only the public HTTP API, and fresh lanes per test keep tests on the shared database independent, since nothing can be deleted through the API.
- Lane numbers in the examples are labels, and tests work with the returned ids. Tests must not depend on whether spec 0002 makes lane numbers unique.
- "Nothing stored" is shown by a follow-up booking of the valid part that succeeds. Where a refused request has no valid part, no claim about storage is made. Internal state is not observable through the API.
- The API gets a time provider that can be fixed through `Clock:FixedNow`, which is honoured only outside Production; in Production its presence stops startup. Integration tests run the API out of process and can only control it through configuration, and a fixed clock must never reach production silently.
- Every Guards cell is one sentence naming the faulty behaviour the example catches. This gives the column one consistent style.
- New: a reservation entity with its lane bookings in the domain, their persistence configuration and a migration, and a create-reservation slice in a new reservations area.

## Out of scope

- Cancelling or changing a reservation.
- Reading reservations, including `GET /reservations/{id}`. `Location` already points to it, but the endpoint is a later feature.
- Creating lanes: everything about `POST /lanes` beyond returning the new `id`, including whether lane numbers are unique. That belongs to spec 0002.
- Payments, authentication and authorisation, recording which staff member created a reservation.
- UI for staff members and the customer web.
- An upper limit on the number of lanes in a reservation.
- Opening hours, cleaning gaps between bookings, the number of players.
- Configurable grid and length limits.
- A customer entity, deduplication of customers, linking reservations to customer accounts.

## Open questions

None.
