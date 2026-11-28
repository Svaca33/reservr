# Spec 0002: Create lane

## Problem

A staff member cannot add the centre's lanes to Reservr. The only lanes that exist are ones inserted directly into the database. Without lanes, no reservation can be made (spec 0001), and acceptance tests for reservations cannot set up their situations through the public API.

## Solution

The staff member creates a lane by entering the lane number shown to customers. Reservr gives the lane a new id and lists it with the other lanes straight away. Reservr refuses the lane if its number is outside 1–999, is not a whole number, or already belongs to another lane. When the number is taken, the staff member sees which lane already has it.

Spec 0001 depends on this spec. It needs `POST /lanes` to return `201` with the new lane's `id`, so that every reservation example can create its own lanes.

## User stories

1. As a staff member, I want to add a lane by its number, so that it can be reserved for customers. (R1, R2, R3)
2. As a staff member, I want Reservr to refuse a lane number that is already used, so that customers and staff never see two lanes with the same number. (R4)
3. As a staff member, I want to see all lanes ordered by number, so that I can check which lanes the centre has. (R1, R5)

## Rules and examples

Unless an example says otherwise:

- All integration tests share one database, and lanes cannot be deleted through the API. Each example therefore observes only the lanes it created itself: they appear in `GET /lanes` and are ordered among themselves by number. Other lanes in the response are ignored.
- Lane numbers in the examples (42, 43, …) are labels. A test uses a unique number that a shared test helper hands out from 2–998. Numbers 1 and 999 are reserved for R2.E1 and R2.E2.
- Any lane that already exists in a situation is created beforehand through `POST /lanes`.
- R5 runs in its own test collection with its own AppHost and a fresh, empty database.
- The Guards column names the faulty behaviour the example catches.

### R1 - A staff member creates a lane by entering its number; the lane gets a new id and appears in `GET /lanes` straight away.

| ID | Situation | Action | Expected outcome | Guards |
|---|---|---|---|---|
| R1.E1 | No lane has number 42 | Create lane 42 | 201; body `{ id, number: 42 }`; `Location: /lanes/{id}`. `GET /lanes` then contains `{ id, number: 42 }` with the same id | The lane is not stored, or is listed with a different id. |
| R1.E2 | No lanes have numbers 43 and 44 | Create lane 44, then lane 43 | `GET /lanes` lists lane 43 before lane 44 | Lanes are listed in creation order instead of by number. |
| R1.E3 | No lanes have numbers 45 and 46 | Create lane 45 and lane 46 | The two responses carry different ids | Lanes share or reuse an id. |

### R2 - A lane number is a whole number from 1 to 999.

| ID | Situation | Action | Expected outcome | Guards |
|---|---|---|---|---|
| R2.E1 | No lane has number 1 | Create lane 1 | 201; body `{ id, number: 1 }` | The lower bound is treated as exclusive. |
| R2.E2 | No lane has number 999 | Create lane 999 | 201; body `{ id, number: 999 }` | The upper bound is treated as exclusive. |
| R2.E3 | — | Create lane 0 | 400; error on `number` | Zero is accepted as a lane number. |
| R2.E4 | — | Create lane 1000 | 400; error on `number` | A number above the upper bound is accepted. |
| R2.E5 | — | Create lane -3 | 400; error on `number`. `GET /lanes` then contains no lane with number -3 | A negative number is accepted or stored. |

### R3 - A request whose `number` is missing or not a whole-number JSON literal is refused like an out-of-range number; unknown fields are ignored.

| ID | Situation | Action | Expected outcome | Guards |
|---|---|---|---|---|
| R3.E1 | — | Send `{}` | 400; error on `number` | A missing number defaults to 0 or yields a non-validation error. |
| R3.E2 | — | Send `{ "number": null }` | 400; error on `number` | A null number is accepted or crashes the request. |
| R3.E3 | — | Send `{ "number": "3" }` | 400; error on `number` | A string is coerced into a number. |
| R3.E4 | — | Send `{ "number": 3.5 }` | 400; error on `number` | A fraction is rounded or truncated into a lane number. |
| R3.E5 | — | Send `{ "number": 3.0 }` | 400; error on `number` | A number written with a decimal part is accepted as whole. |
| R3.E6 | — | Send `{ "number": 1e2 }` | 400; error on `number` | A number in exponent notation is accepted. |
| R3.E7 | No lane has number 48 | Send `{ "number": 48, "name": "x" }` | 201; body `{ id, number: 48 }` | An unknown field makes the request fail. |

### R4 - Lane numbers are unique, even when requests arrive at the same moment.

| ID | Situation | Action | Expected outcome | Guards |
|---|---|---|---|---|
| R4.E1 | Lane 49 exists | Create lane 49 again | 409; `laneId` is the id of the existing lane 49. `GET /lanes` then contains number 49 exactly once | A duplicate number is stored. |
| R4.E2 | Lane 50 exists; no lane has number 51 | Create lane 51 | 201 | A neighbouring number is treated as a duplicate. |
| R4.E3 | No lane has number 47 | Send 10 identical requests to create lane 47 in parallel | Exactly one returns 201 and the other nine return 409. `GET /lanes` then contains number 47 exactly once | Concurrent requests store the same number twice. |

### R5 - `GET /lanes` on a fresh database returns an empty array.

| ID | Situation | Action | Expected outcome | Guards |
|---|---|---|---|---|
| R5.E1 | A fresh database with no lanes | Call `GET /lanes` | 200; the body is exactly `[]` | An empty database yields `null`, an error or a non-array body. |

## Interface

The behaviour is checked through the HTTP API.

### Create lane

`POST /lanes`

Inputs (JSON body):

- `number`: required, a whole-number JSON literal from 1 to 999, unique among lanes (R2, R3, R4). Other fields in the body are ignored (R3).

| Outcome | Response |
|---|---|
| Created | `201 Created`, `Location: /lanes/{id}`. The body is `{ id, number }`. |
| Invalid input (out of range, missing, null, not a whole-number literal) | `400 Bad Request`, validation problem details with an error keyed `number`. |
| Number already used | `409 Conflict`, problem details with `laneId`, the id of the lane that already has the number. |

### List lanes

`GET /lanes` (unchanged)

| Outcome | Response |
|---|---|
| Always | `200 OK`. The body is an array of `{ id, number }` ordered by number ascending; `[]` when there are no lanes. |

Every situation is set up through the API: existing lanes through `POST /lanes`, and the fresh database for R5 through its own AppHost. No outcome depends on now, so this spec needs no clock control.

## Decisions

- A lane has only a number for now. Name, type (for example a children's lane with bumpers) and status (active, under repair) can be added later as separate features.
- Lane numbers are unique across the system, and a unique database index enforces this. Two lanes with the same number would confuse staff and customers, and only the database can prevent duplicates under concurrent requests.
- Lane numbers are whole numbers from 1 to 999. The lower bound is the existing domain rule, and the upper bound catches typos, since no centre has close to 999 lanes.
- The range is enforced in the lane's constructor and mirrored as a database check constraint that replaces the positive-number constraint, in one new migration together with the unique index. Domain invariants live in the constructor and are mirrored in the database.
- Every malformed input returns 400 validation problem details keyed `number`, not the framework's default binding error. Spec 0001 needs the same error shape.
- `3.0` and `1e2` are refused, so a whole number must be written without a decimal part or exponent.
- Unknown fields are ignored. Future additions to the body should not break older callers.
- A duplicate number returns 409 with the existing lane's id, so the staff member sees at once which lane the number belongs to. Inside the back office this reveals nothing sensitive.
- `Location` points to `/lanes/{id}`, which does not exist yet. This matches how spec 0001 handles reservations.
- `GET /lanes` is unchanged: its item shape equals the `POST /lanes` body, so there is one shape for a lane, and it is how a test observes that a lane was stored.
- Tests observe only the lanes they created, plus their relative order, and the existing ordering test is rewritten that way. The shared database cannot be emptied through the API.
- The empty-database criterion from the walking skeleton moves to its own test collection with its own AppHost. This keeps that guard, at the cost of one extra AppHost start.
- A shared test helper hands out unique lane numbers 2–998 per run, safely even if tests later run in parallel, and spec 0001's tests use it too. Numbers 1 and 999 are reserved for the boundary examples. Every run starts with an empty database, and a run creates roughly 150–200 lanes, well within the range.
- Concurrent duplicates are checked with 10 parallel identical requests that expect exactly one 201. This is the same pattern as spec 0001.
- New: a create-lane slice in the lanes area.

## Out of scope

- Changing or deleting lanes.
- `GET /lanes/{id}`.
- Lane name, type and status.
- More than one centre (a lane number is unique across the whole system for now).
- UI.

## Open questions

None.
