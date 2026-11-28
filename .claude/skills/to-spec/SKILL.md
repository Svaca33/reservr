---
name: to-spec
description: Turn the current conversation into a new or revised spec in docs/specs: no interview, just synthesis of what you've already discussed.
disable-model-invocation: true
---

This skill takes the current conversation and your understanding of the codebase and writes a spec. Synthesize what the conversation already settled and ask the user nothing. Whatever it left unsettled goes to **Open questions**, never to your own answer.

## Process

1. Explore the repo to understand the current state of the codebase, if you haven't already. Use the project's domain vocabulary throughout the spec, and respect the decisions already recorded in its docs.

2. Pick the file:
   - When the conversation revises an existing spec, edit that file in place. Existing IDs are permanent: keep them, give new rules and examples the next ID after the last one (R7 after R6, R2.E5 after R2.E4), and mark a dropped rule or example **Removed** in place, leaving its ID and text as they were.
   - Otherwise list `docs/specs/`, take the highest four-digit number and add one (`0001` when the folder is empty or missing), then add a kebab-case slug of the feature: `docs/specs/0003-lane-reservation.md`.

3. Write the spec in English using the template below. It is complete when every rule, every example, every part of the contract, every decision and every unanswered question from the conversation sits in exactly one section. Then tell the user the file's path.

The spec describes behaviour and decisions in prose and tables, free of source file paths and code: those go stale as soon as the code moves. HTTP methods, routes and status codes are behaviour, and belong in the contract.

<spec-template>

## Problem

The problem the actors face, from their perspective.

## Solution

The solution to the problem, from the actors' perspective.

## User stories

A short, numbered list of user stories, each naming the rules that fulfil it. Behaviour detail lives only in Rules and examples.

1. As a <actor>, I want <feature>, so that <benefit>. (R1, R3)

## Rules and examples

Numbered rules (R1, R2, ...). Each rule is one sentence, followed by a table of its examples, numbered within the rule:

### R1 - <the rule in one sentence>

| ID | Situation | Action | Expected outcome | Guards |
|---|---|---|---|---|
| R1.E1 | <state before, with concrete data> | <what an actor does> | <what the actor observes> | <the mistake this example catches> |

Every rule carries its typical case, its **boundaries** (just inside and just outside each limit) and its **rejected** cases. Use concrete data, dates and times: "Lane 3 is booked on Saturday 2026-12-05 18:00–19:00", never "a lane with an existing booking". When the outcome depends on the current time, the situation states **now** with its time zone: "Now is 2026-12-05 17:30 Europe/Prague". Every expected outcome is observable from outside the system: a response the actor gets, or what a later query shows, never internal state. An example whose outcome the conversation left open goes to Open questions instead of the table.

## Interface

The highest place where the behaviour can be checked from outside (for Reservr, the HTTP API), and the feature's contract there, recorded as the conversation settled it. One block per operation:

### <operation name>

`<HTTP method> <route>`

Inputs: <each input with its meaning and constraints>

| Outcome | Response |
|---|---|
| <kind of outcome> | <status code and the fields the caller gets> |

Every expected outcome in the rule tables matches one row here. When any outcome depends on **now**, state how a test through the interface sets the server's clock. Any part of the contract the conversation left unsettled goes to Open questions.

## Decisions

One line per decision with its reason: `<decision> - <one-line reason>`. Include product choices, architectural choices, modules built or changed, and schema changes.

## Out of scope

What this spec deliberately leaves out.

## Open questions

Every question the conversation left unanswered, as it was asked, with who could answer it and the rules or decisions it blocks. Leave the answer blank.

</spec-template>
