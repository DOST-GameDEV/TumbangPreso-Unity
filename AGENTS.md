# TUMP working instructions

## Start and resume

1. Inspect branch, remote divergence, worktrees and dirty files. Preserve others' work.
2. Read [project context](docs/UnityProjectContext.md), [current queue](docs/TODO.md)
   and the short [checkpoint](docs/ACTIVE_REWORK_LEDGER.md).
3. Use [docs/README](docs/README.md) to load only the method and source needed.
   Reports, archives and old OPEN headings are evidence, not assignments.

The newest owner instruction wins. Current code beats old counts and plans.
Do not load historical TODO bodies or all reports to start a session.

## Branches and publication

- `main` is the integration branch. The owner authorized merging `BugFix`, deleting
  it after verified integration, and publishing spectator refinements to `main`.
- The owner combined network and mobile work on `mobile+network` (formerly
  `NETWORKFIX`/`MOBILEFIX`). Keep that branch separate from main until integration
  is requested. Mobile follows the verified network work.
- Fetch and inspect before publication. Never reset, clean, force-push or discard
  unrelated changes. Delete a branch only after checking ancestry and worktrees.
- Stage explicit owned paths. Commit with `git commit -F`; author M4tyu633.
  No attribution trailers, tooling references or em dashes in repository prose.
  Push coherent commits and verify the remote commit.

## Technical contracts

- `Packages/com.tumbangpreso.core/Runtime` is the single engine-free rules source;
  `Core` compiles it in place. Never add UnityEngine or duplicate those files.
- Host owns outcomes; `MatchDirector.AddScore` owns points. Preserve seat ownership,
  action scope, movement epochs, append-only roster/wire IDs and asset GUIDs.
- Read [working rules](docs/WORKING_RULES.md) and the
  [skill network contract](docs/SKILL_NETWORK_CONTRACT.md) for affected systems.
- Preserve GenericPadBridge, MenuNav and input contexts. Desktop keyboard/controller
  is the default task scope; show live bindings and preserve existing touch behavior.
  Mobile changes and Android validation are required when the owner asks for them,
  not automatically in every feature/session. Task order belongs in TODO.
- Preserve profiles, saves, private/source art and unrelated processes. Do not
  degrade authored assets as a speculative optimization.

## Product and authored work

- Four players, one rotating taya; retrieval is the tension. Classic and Hero Strike
  both ship, normally eight rounds. No Street Hype.
- Stable IDs differ from displayed names. Read `Roster` and `EconomyRules.UnlockedHeroes`;
  Rago (`sean`) was unlocked on October 24. Do not restore the old five-hero lock.
- Paete and Phaister's finalized kits/art stay protected. Demonstrated bug fixes,
  authorized shared changes and newer explicit owner instructions still apply.
  Current authored models and motion supersede older prototype restoration plans.
- Keep the cute blocky cast, flat faces, individual direction and supplied UI art.
  Human hero voice recordings only. SFX replacement requires listening in context.
- Match text/icons use black `UiTheme.InGameOutline`; follow current font/theme
  helpers. Recorded map previews must be regenerated after source/framing changes.

## Validation and continuity

- Use focused meaningful checks, then integration checks for a coherent candidate.
  Reuse unchanged evidence; never weaken tests or infer runtime success from compile.
- [TESTING](docs/TESTING.md) owns commands and evidence rules. One heavy Unity job
  per machine, frozen source, named isolated profile, separate writable caches.
  Monitor real progress; obsolete RAM/admission guards are not an execution ban.
- Graphics checks require graphics. Read fresh nonzero test XML. Actual peers,
  cross-platform play and human visual/audio judgment are separate claims.
- Playable player builds only in the canonical checkout's `Builds/<platform>/`,
  with explicit `-buildOutput`; one current build per platform. No Desktop or
  worktree player builds. Preserve an existing playable build until replacement works.
- Keep TODO the only active queue. Keep the checkpoint short: branch/commit,
  owned changes, running job IDs, evidence limits and one exact next action.
- Retire superseded/unnecessary proposals with a reason; preserve useful history
  and stable pointers. Run `python tools/audit_docs.py` after doc changes.
- Do not create agents, message other chats, buy services or alter shared profiles
  without current authorization. Continue authorized work through ordinary failures.

[Historical instructions](docs/archive/cleanup-2026-10-25/AGENTS.before.md)
retain old incident context; expired schedules and branch commands are not current.

## Build and open the latest Windows game

When the owner asks to open/play the latest game, build current source and launch
that fresh player. Do not search old executables or open the newest existing file.
Use the canonical checkout and this one command:

`python tools/build_windows.py --launch`

The double-click entry is `tools/build_latest_windows.cmd`; the Desktop shortcut
is `Build Latest TUMP.lnk`. Output is always `Builds/Windows/TumbangPreso.exe`.
The helper fetches origin, requires latest main to be included, invokes the matching
installed Editor with an explicit project/output, preserves settings/profiles,
checks successful build plus fresh source identity and runtime files, then launches
and verifies a responsive window. It refreshes the Desktop play shortcuts only
after that fresh launch. Read `Logs/windows-build-current.json` and its job folder
for progress/results. A failed build never falls back to an old player.

On this PC, bare `unity` is `C:\Users\matth\bin\unity.cmd`, an Editor wrapper,
not the Unity CLI. Never send it CLI verbs such as self-update, status or command:
that opens the projectless Editor wizard. Normal builds need no CLI install/update,
Hub changes, sign-out, license reset or project creation. The helper uses the
installed Editor executable directly. Hub's `resources/unity.exe serve` process
is not a Unity Editor and must be preserved. Detect Editors by executable path,
not name alone. Preserve an active Editor's unsaved work rather than killing it.
