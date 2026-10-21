# Playtested movement revision — October 3

The owner's second movement request is implemented.

- Attacker walk/run: **2.5 / 5 m/s**. Defender: **3.75 / 7.5 m/s**.
- Jump model: **1 m / 0.5 s**, launch 8 m/s and character gravity 32 m/s².
  Slipper/projectile gravity stays 20 m/s².
- Stamina: **250**, drain/regen **100 per second**, start floor **50**, regen
  delay **1 second**. Fatigue remains 2.5 seconds, with no walking slowdown
  and no sprint or regeneration.
- Shove and defender lunge have **no stamina cost or fatigue gate**. Host and
  local paths agree. The bot shove planner no longer rejects an empty bar.
- Separate conditional retrieval slide remains **25 stamina**; its cost no
  longer aliases the now-zero shove constant. Other distances, cooldowns and
  active-effect modifiers stay unchanged.

## Validation

Managed **708/708 pass**. First 707/708 exposed a stale old sprint-distance
assertion; the new explicit speed covers 12.5 m in 2.5 s rather than 14.0625 m.
Generic paid-spend/refund checks now use the still-paid slide cost. Initial CLI
home failure and first test result are retained.

Native **5/5 pass** in 6.736 seconds:
- Eight physical samples across both modes exactly measured 2.5, 5, 3.75 and
  7.5 m/s for their respective role/state.
- Actual flat-ground jump measured **1.0816 m / 0.5200 s**, with fixed-step
  integration accounting for the difference from the ideal continuous model.
- Fatigue retains 2.5 m/s walking and blocks sprint/regeneration.
- Real tap/full lunge input retains its previous cooldowns.
- Real local shove plus authoritative shove/lunge resolve with an empty,
  fatigued bar and spend nothing. This is not an actual-peer network test.

The related old Unity hardening arithmetic was reconciled with the explicit
slide/shove costs and compiled; that separate EditMode method was not rerun.
No refreshed player, physical-device or human acceptance claim. Protocol **138**
requires matching rebuilt clients.

## Resource diagnosis and recovery

A process-scoped 512 MiB .NET heap experiment was too small for Roslyn and failed
compilation. One bounded compiler retry used 1 GiB plus two logical compiler
processors: all game/test assemblies were compiled, but late asset import
hit the container headroom guard. These failures remain in the report.

Rather than repeat compilation, first runtime qualification used the completed
assemblies after a measured diagnosis: idle file-backed pages, not active game
objects, occupied most baseline memory. Read-only per-file clean-page advice
on task-owned validation inputs, Library and installed tools reclaimed
1,358,880,768 bytes; another exact stale compiler released 406,396,928 RSS bytes.
No files were deleted or changed. No global cache purge or weaker guard.

The runtime completed at tree RSS 3,374,792,704 bytes and container
7,256,383,488 bytes, exit 0, **no guard stop**. Original settings and profiles
restored, frozen source hashes unchanged. Completed managed build servers were
shut down instead of left resident.
