---
name: grill-me
description: Relentless interview that turns a plan into a design tree and an example map, ready for /to-spec.
disable-model-invocation: true
---

Interview the user relentlessly until you reach a shared understanding, held in two shapes: a **design tree** of decisions and an **example map** of the rules those decisions produce. Your only output is messages in this conversation; the user turns them into a spec afterwards with `/to-spec`.

## Design tree

Every decision branches into the decisions that hang off it. Work the tree in **rounds**. The **frontier** is every decision whose prerequisites are already settled: the questions you can ask _now_ without guessing at answers you haven't heard yet. Ask the whole frontier in one round: number each question and give your recommended answer. Then wait for the user's answers before the next round.

Format a round like so:

```
❓ **Q1** - **<question title>**: <question body, might be multiple paragraphs, including multiple choices>

➡️ <your recommended answer>

---

❓ **Q2** - **<question title>**: <question body, might be multiple paragraphs, including multiple choices>

➡️ <your recommended answer>
```

Word each question so "yes" accepts your recommended answer.

Each round the user answers reshapes the tree: settled decisions push the frontier outward and unblock questions that depended on them. Recompute the frontier and ask the next round. A question whose answer depends on another question still open in this round belongs to a _later_ round, not this one.

## Facts and decisions

Finding _facts_ is your job, never the user's. When a frontier question needs a fact from the environment (code, docs, config), dispatch a sub-agent with a fresh context window to find it; ask the user only for what you cannot look up. Don't block on it: a running exploration is an unsettled prerequisite, so only the questions downstream of it wait for the sub-agent to report; ask the rest of the frontier now.

The _decisions_ are the user's: put each to them and wait. When the user doesn't know the answer, record an **open question** in place of your recommendation, so the gap stays visible instead of being filled by your guess. Note who could answer it (a venue manager, a lawyer, a measurement). Questions downstream of an open question are parked under it and leave the frontier.

## Example map

Each settled decision yields one or more **rules**. Give every rule its key examples, each a concrete case with real values and its outcome:

- the typical case the rule exists for;
- the **boundaries**: the case just inside and just outside every limit the rule sets (exactly at closing time, one minute past it);
- the **rejected** cases: what the system refuses, and what the refusal looks like.

Number examples within their rule: R3.E1, R3.E2, ... When the conversation revises an existing spec in `docs/specs/`, keep its IDs and continue each numbering after its last ID.

When an outcome depends on the current time, the example's situation states **now** with its time zone: "now is 2026-12-05 17:30 Europe/Prague".

Examples are decisions too. Put each example whose outcome the rule leaves ambiguous to the user as a frontier question, with your proposed outcome as the recommendation. Boundaries are where hidden decisions surface; each new decision becomes a branch of the tree.

Close every round with the rules and examples added or changed in it, then the full running list of open questions:

```
📋 **R3** - <rule>
- ✅ **R3.E1** <typical example> → <outcome>
- 📏 **R3.E2** <boundary example> → <outcome>
- ❌ **R3.E3** <rejected example> → <how it is refused>

❔ **Open questions**
- **O1** - <question> (who could answer; parks Q7, R4)
```

## Interface

Every example must be checkable from outside, at the highest public **interface** where the behaviour can be checked (for Reservr, the HTTP API), at both ends: its situation is **set up** and its outcome is **observed** through that interface.

Once an outcome is settled, put a frontier question on how the caller observes it, with your proposed **contract** as the recommended answer: the operation (HTTP method and route), its inputs, and for each kind of outcome the response the caller gets (status code and the fields that matter). Outcomes that share a response share one question.

For every situation, name the public operations that set it up. When a piece of the situation has no such operation (a record nothing in the interface creates, the server's clock behind **now**), put a frontier question on how a test sets it up, with your proposed answer: an operation from this or another spec, configuration the test passes at startup, or a change to the example.

## Done

The session is done when the frontier is empty: every branch of the design tree visited, every rule carrying its typical, boundary and rejected examples, every situation set up through the interface, every outcome mapped to a response in the contract, every unknown recorded as an open question, nothing silently assumed. Then present the whole picture in one message: the decisions, the full example map, the contract, and the open questions. Ask the user to confirm you have reached a shared understanding; once they do, point them to `/to-spec`.
