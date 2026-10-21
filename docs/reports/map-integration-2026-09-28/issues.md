# Map integration issues for the team (Kanto and Lagoon Cove merge, 2026-09-27 to 09-28)

What the Kanto and Lagoon Cove work turned up that is NOT specific to those maps. Each item says
whether it is fixed, and what we measured or did not measure. The TODO entries are LAGOON-1.6
and LAGOON-1.7.

## Already red on `ASTRAReworks` (not caused by the maps)

The baseline method: run the same fixtures with the same profile
(`presentation-validation-20260921`, `tools/playmode_suite.py --gate`) on a clean checkout of
`ASTRAReworks` c6506327 without our merge, and compare the results fixture by fixture.

1. **89 PlayMode failures fail identically on the clean branch.** The largest clusters:
   - Hub buttons are covered by `TumpLoadingCanvas`, so raycast presses miss.
   - HUD layout overflow.
   - Phaister/voodoo ability tests (the kit is mid-rework, HERO-10).

   Six more failures pass alone but fail inside a full group, because state leaks between
   fixtures. Treat a red gate as "compare to baseline", not "your change broke it".
2. **Bot matches never end on Eskinita or Ilalim ng Tulay.** In `BotBehaviourProbe`, all 8 rounds
   play out over 64,000 frames and no MatchRecord is ever written. This also happens on the clean
   branch. Nobody has found the cause yet.
3. **4 EditMode failures, all pre-existing:**
   - `GameLaunch.TrainingRange` is not on the tournament list.
   - The roster arm meshes are stale against their bakes.
   - Equipment clips into heads (head clearance).
4. **The upstream protocol test pin was stale.** `ChatAndLobbyChromeTests` pinned an older
   `NetSession.ProtocolVersion`. It is now updated.

## Fixed during the merge

5. **Five fixtures were in no gate group.** `tools/playmode_suite.py` silently skipped them. They
   are grouped now. When you add a PlayMode fixture, add it to a group.
6. **The catch cutscene ignored the map's look.** `CatchReconstruction` built its playback camera
   without `ColourGrade` or `WorldOutline`, and parented it under the recorded stage, so the
   stage capture swept it up. It now hangs off the reconstruction itself, carries the look, and is
   destroyed at End.
7. **The `~GameServices` editor leak** (heard as stray "sound emitters"): services survived
   leaving Play in the editor. Fixed.
8. **The map vote row overflowed with six courts.** `HubMapVote` now scales the row to fit 1800
   units.
9. **The AO line artifact.** A horizontal line appeared across the screen, first on the ground,
   then on the walls. The old fixed 1.8 m AO/contact gate is now conditional
   (`WorldOutline.NearGuard()`): it switches on only while a `NearFade` renderer (the Ilalim
   pillars) is within 2.8 m of the camera. Maps without NearFade never gate. **Ilalim still
   depends on it.**
10. **Foliage in the AO and outline.** Alpha-cut leaves drew as solid cards in the depth-normals
    prepass. `KantoFoliage` is now masked from `WorldOutline` and from the AO. Any new cutout
    shader needs the same mask.

## Know before you touch maps

11. **The look only exists in Play.** `WorldLookPresentation`, `ColourGrade` and `WorldOutline`
    do not run in edit mode, so editor renders and edit-mode review tools show the raw scene, not
    the game. Capture in Play (`KantoLookMeasure`, `MapCardCapture`).
12. **A map with no `WorldLookProfile` entry gets no AO and no look at all.** The asset row in
    `Resources/WorldLookProfile.asset` overrides the code defaults.
13. **Map indices travel over the network.** Adding, removing or reordering `SceneFlow.Maps`
    requires a `NetSession.ProtocolVersion` bump. On this busy branch the number collided four
    times in one day (82, 83, 84, 86, landed at 87). Fetch, bump, merge and push in one quick
    sequence.
14. **Custom and Host prepare a live preview of every registered map** behind the loading curtain
    and keep all of them cached. Six courts took about 3.5 s, against about 2.3 s for five. The
    Lagoon Cove alone is 3.47 M triangles. Memory grows with every map added.
15. **Tooling on Windows.** The installed dotnet lacks the .NET 9 runtime, so `Core.Tests` does
    not run there. Use EditMode instead. Long worktree paths also break Unity imports; use a short
    path such as `C:/Users/<you>/tpm`.

## Not verified yet

16. On Kanto and the Lagoon Cove, bots and a full played match are still unverified.
17. On the Lagoon Cove, still unverified: what a player wading off the sea side stands on, and
    whether a slipper thrown into the sea is recovered (LAGOON-1.7). The cove has no swim system.
    The old Lagoon's 12 map-specific tests are `[Ignore]`d until that is decided.
