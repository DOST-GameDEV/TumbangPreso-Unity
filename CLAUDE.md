# CLAUDE.md

Context and rules for this repository. Read this before touching anything.

---

## 1 · Commits

**Author every commit as `M4tyu633 <matthewtlabrador@gmail.com>`.**

⚠️ **NEVER add a `Co-Authored-By: Claude` trailer, or any co-author trailer at all.** This is
sole-authored work and it is entered in a competition. The same rule holds on the Godot
repo, where it is stated in ten places. Do not add one "just this once" and do not add one
because a default template suggests it.

Do not mention Claude, Anthropic, or any AI tooling in commit messages, code comments, the
README, or anything else in this repository.

**Never commit a handoff prompt as a file.** If a session needs to hand off, the handoff goes
in the chat reply to be copy-pasted. A stale one committed to a repo has already had to be
deleted once on another project.

## 2 · What this repository is

The **Unity 6 port** of Tumbang Preso, 1st place at the Gear Up NCR Esports Game Dev
Challenge and NCR's entry at the nationals in General Santos City.

The Godot 4.7 original is at
[DOST-GameDev](https://github.com/DOST-GameDEV/DOST-GameDev) and **remains the running
game** until this port reaches parity. It is the reference for every behaviour question.

Read [`docs/Port_Plan.md`](docs/Port_Plan.md) before starting work. It carries the phase
order, the exit criteria, and the reasoning behind both.

## 3 · The rule that matters most

**`Packages/com.tumbangpreso.core/` must never acquire a `UnityEngine` reference.**

It holds the match rules, scoring, trait tables, the stamina model, throw legality and the
combat geometry: every number in the game that was arrived at by measurement rather than by
taste. Keeping it engine-free is what lets those numbers be asserted in under a second by
`dotnet test` instead of playtested for an afternoon, and it is the entire verification
strategy for the port.

The asmdef enforces it with `"noEngineReferences": true`, so a violation is a compile error.
**Do not add an exception.**

⚠️ **The source lives in the package, and `Core/TumbangPreso.Core.csproj` compiles those same
files in place.** One copy, two toolchains. Never "fix" this by copying the files: two copies
of the balance layer is exactly the failure the structure prevents, because the copy that
drifts is the one nobody runs the tests against.

```bash
dotnet test Core.Tests/TumbangPreso.Core.Tests.csproj
```

## 4 · Design.md is the balance source of truth, and it has drifted

`docs/Design.md` in the **Godot** repo is the balance source of truth for both projects. It
opens with: *a number in the code must match a number here, or one of the two is a bug.*

⚠️ **Four numbers currently disagree, and in every case the code is the newer half.** They are
listed in `docs/Port_Plan.md` §7.1. **Port from the GDScript, never from the prose**, and if
you find a fifth, add it there rather than silently picking a side.

The most serious is the **lunge**: `LUNGE_SPEED` 7.746 gives 2.30 m of reach, while Design.md
reports 3.20 m as measured. That is the taya's primary scoring verb, and it is unresolved.

## 4a · The art in this repo is placeholder

Everything under `Assets/TumbangPreso/Art/` was carried over from the Godot build so the game
can RUN during the port. **All of it is being replaced with the team's own work.** Do not
polish, retopologise, or build finished materials for a mesh that is scheduled to go.

⚠️ **The IKE slipper carries the real Nike wordmark as geometry, and replacing the mesh is the
only thing that resolves it.** It is first in the queue. `docs/Port_Plan.md` §8 has the full
order and, more importantly, the list of properties a replacement must preserve, because
several props were tuned against the exact shape that was drawn.

## 5 · Architectural invariants, each learned the hard way

Do not "improve" any of these. Every one replaced something that failed in play or in a probe.

- **Contact resolves by DISTANCE on the host, never by a trigger volume.** A `hit_probe`
  measured 16 of 36 overlaps failing to land, split by target. This also keeps the most
  correctness-critical code free of any physics-engine dependency, which is most of why this
  port is tractable.
- **Every point is awarded in ONE function**, host-side (`MatchDirector.AddScore`). A point
  that can only be created in one place cannot be created on a client at all.
- **The taya role is DERIVED**, `(round - 1) % 4`, never accumulated. "Everyone defends
  exactly once, clockwise" is true by construction, not by bookkeeping.
- **The box is a SQUARE, not a circle**, and X and Z clamp independently. They disagree by
  2.9 m on the diagonal at the current radius, which is exactly where a taya moves to cover a
  corner. Making either the test or the clamp radial cost a whole session once.
- **A bot presses the same buttons a human does** (`InputIntent`). One physics step serves
  both. Never let AI call a gameplay method directly.
- **Stuns overlap via `Max()`, never additively.** That is the entire bound on a stun chain in
  a 1-vs-3 game.
- **Every impulse is derived from `Friction`** as `v²/(2·Friction)`. Write the distance you
  want and solve for the speed; never hard-code a distance beside a speed.
- **Entry 0 of each prop list stays neutral.** It is what an unpicked prop wears, so a
  non-neutral row silently retunes every AI seat and every peer that never opened the
  character screen.

## 6 · This machine

| | |
|---|---|
| Unity | `C:\Program Files\Unity\Hub\Editor\6000.5.8f1\Editor\Unity.exe` |
| Modules | Windows Standalone, WebGL, **Linux Dedicated Server** (the Singapore VPS build) |
| dotnet | `C:\Program Files\dotnet\dotnet.exe`, SDK 9.0.317 |
| RAM | 16 GB. It read 8 GB until a boot-time cap was cleared, so re-check before blaming Unity |

⚠️ **`Unity.exe -batchmode -quit` exits before compiling scripts.** It stops after package
registration and still returns exit code 0, so it looks like a clean build and proves nothing.
Use `-executeMethod` or `-runTests` when you need an actual compile.

⚠️ **Launch Unity with `Start-Process -Wait -PassThru`, not the `&` call operator.** With `&`,
`$LASTEXITCODE` comes back empty and the log file is sometimes never created at all, which is
indistinguishable from a run that failed. `Start-Process` returns a real `ExitCode`.

⚠️ **Unity leaves child processes holding the project lock after it exits.**
`Unity.ILPP.Runner`, `UnityPackageManager` and `UnityShaderCompiler` can outlive the editor,
and while they do, the next launch silently does nothing: no log, no error, no exit code. If a
run produces no log at all, check `Temp/UnityLockfile` and kill the stragglers rather than
assuming the command was wrong.

Working commands:

```bash
dotnet test Core.Tests/TumbangPreso.Core.Tests.csproj
```

```bash
"/c/Program Files/Unity/Hub/Editor/6000.5.8f1/Editor/Unity.exe" -batchmode -runTests -nographics -projectPath . -testPlatform EditMode -testResults Logs/tests.xml -logFile Logs/tests.log
```

⚠️ **PowerShell here-strings break on embedded double quotes** when passed to `git commit -m`.
The message gets split and the remainder is parsed as pathspecs. **Write the message to a file
and use `git commit -F`.**

⚠️ **Bash heredocs are unreliable on this machine.** Write the script to a file and run it.

## 6a · Do the work, do not manage the timeline

**Do not editorialise about deadlines, scope, or whether something is achievable.** This team
built the entire game, which won its regional, in under two weeks. Estimates and warnings about
what "realistically" fits are not useful here and have already been wrong once.

**Do not stop to ask for a test until everything you can do yourself is done.** Almost
everything is doable headlessly and the reflex to hand work back is usually laziness dressed as
caution:

- Unity **scenes can be built from code** (`EditorSceneManager` + `-executeMethod`). See
  `Assets/TumbangPreso/Editor/SceneBuilder.cs`. Do not claim a scene needs the GUI.
- Matches can be **run and measured** in headless Play Mode tests.
- Compilation, tests, probes and builds all run from the command line.

Hand something back only when it genuinely requires a human judgement (does this FEEL right,
is this the art we want) or a credential. Everything else: just do it, verify it yourself, and
report what the measurement said.

## 7 · Writing style

**No em dashes anywhere**, in code comments, docs, or commit messages. Rewrite the sentence
rather than swapping the character in. This holds in the Godot repo too, where the roster file
states it explicitly.

Match the Godot codebase's commenting discipline: it documents **why**, at length, in
⚠️-marked comments above the thing. Record deletions and the reasoning, not just the change. A
number that was measured says so, and says what it measured against.
