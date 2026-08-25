# Tumbang Preso — Unity

The Unity 6 port of **Tumbang Preso**, 1st place at the Gear Up NCR Esports Game Dev
Challenge and NCR's entry at the nationals in General Santos City.

Four players. Four rounds. One *taya*. A defender guards the lata inside a chalk box while
three attackers throw tsinelas at it, then have to run in and get them back. **The tension
is the retrieval, not the throw.**

## Where things stand

**Phase 0.** Nothing is built yet. Read [`docs/Port_Plan.md`](docs/Port_Plan.md) first — it
carries the phase order, the exit criteria, and the reasoning behind both.

## Repository layout

```
Core/        plain C# rules library, no Unity reference at all, testable with `dotnet test`
Core.Tests/  the balance assertions that prove the port preserved the tuning
Assets/      the Unity project (from Phase 2)
docs/        the port plan
```

## The one rule that matters

**`Core/` must never acquire a `UnityEngine` reference.** It holds the match rules, the
scoring, the trait tables, the stamina model, throw legality and the combat geometry — every
number in the game that was arrived at by measurement rather than by taste. Keeping it
engine-free is what lets those numbers be tested in a second rather than playtested for an
afternoon, and it is the entire verification strategy for the port. The assembly definitions
enforce this; do not add an exception.

## The original

The Godot 4.7 original lives in [DOST-GameDev](https://github.com/DOST-GameDEV/DOST-GameDev)
and remains the running game until this port reaches parity. Its `docs/Design.md` is the
balance source of truth for both, with the caveat recorded in §7.1 of the port plan: two of
its numbers have drifted from the code, and **the code is the newer half**.
