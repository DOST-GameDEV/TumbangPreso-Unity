# Canonical Native Rendering And Builds

Use Unity's actual game shaders,colour space,rigs and camera paths to qualify
models/motion. External concept/reference images are not runtime proof.
Read [character method](CHARACTER_MODEL_METHOD.md),[hero-kit method](HERO_KIT_METHOD.md)
or [HOME animation method](HOME_SCREEN_ANIMATION_METHOD.md) for the task's authored
process,then [TESTING](TESTING.md) for safe execution.

## 1. Prepare The Authored Candidate

- Use that character's own builder and source directory. Preserve the canonical
  builder,rig/bone paths,GUIDs,face,quiet colour and current approved identity.
  Do not stamp a shared appearance or motion recipe across the roster.
- Build the model/palette first; a failed builder must stop the render. Do not
  capture yesterday's imported model under a new iteration name.
- If the palette/roster representation changed, use the existing RosterBookBuilder
  path as required by current authoring source. Do not blindly rebuild every roster
  asset for an isolated change.
- Force-reimport rebuilt GLB sub-assets,including changed companion/accessory files.
  Reimporting only the body can leave a cached companion in the old pose.
- Preserve authored material/shader/outline and linear-colour handling. A raw model
  preview without ToonSkin is not the game's look.

## 2. Choose The Existing Probe

| Question | Tool/output |
|---|---|
| Does the body belong beside the cast? | PersonSwapProbe turnaround and cast lineup; CastRestyleReview for the current hero |
| Are bounds/rig/hand anchors and clips intact? | PersonSwapProbe and current authoring checks; no fixed universal clip count |
| What happens across a motion clip? | ClipMotionStrip using actual game action names and multiple times/views |
| Does it work from the player's eye and on peers? | Existing normal-speed in-game owner/observer/spectator capture route |
| Is the HOME film directed/timed correctly? | HOME method and its existing render/review tools,not a generic posed lineup |

A turnaround cannot approve an animation. A motion strip cannot establish actual
peer synchronization or full-speed feel. Inspect relevant greyscale/readability
views and first-person/body agreement when the changed behavior requires them.
Use the current roster/reference clip contract,not old hardcoded clip counts.

## 3. Guarded Execution

Run from a named ISOLATED candidate with frozen inputs. Example commands must be
adapted to the actual hero,action and installed target; do not run all of them as
a ritual. The guard supplies its checkout's projectPath.

```powershell
python tools/run_unity_guarded.py -batchmode -force-d3d11 -tp-profile presentation-validation-20260921 -executeMethod TumbangPreso.EditorTools.PersonSwapProbe.Run -logFile Logs/model-review-v1.log
python tools/run_unity_guarded.py -batchmode -force-d3d11 -tp-profile presentation-validation-20260921 -executeMethod TumbangPreso.EditorTools.ClipMotionStrip.Run -rig sean -clip slide -logFile Logs/motion-review-v1.log
```

ClipMotionStrip also accepts `-frames`,`-times` and `-views`; inspect its current
source before changing the sampling. It records resolved action/fallback names,
deformed vertices,hand height and motion statistics alongside time-sampled images.
The floor reference exposes penetration that a floating model render hides.

Use unique iteration/output names so a cached viewer cannot show an old image.
Do not overwrite the image a review refers to. Owner-authorized cleanup retains
the latest two coherent generated iterations while preserving essential method/
decision references and authored source. No native render uses `-nographics`.

## 4. Internal Player Build

After the coherent work warrants a playable player, use the canonical checkout
and the platform's single output folder. Do not build a player in a worktree:

```powershell
python tools/run_unity_guarded.py -batchmode -force-d3d11 -tp-profile validation-20261025 -executeMethod TumbangPreso.EditorTools.GameBuilder.BuildWindows -buildOutput Builds/Windows/TumbangPreso.exe -logFile Logs/build-windows.log
```

Use the platform's installed module/build method. The builder purges recognized
old output at its target; inspect ownership and preserve needed builds first.
Never replace the Desktop player by default. Verify executable/data/Runtime.dll,
then launch THAT player. Compilation,packaging and successful gameplay are distinct.

## 5. Preserved Pitfalls

The full prior pipeline and measured Nemu/companion examples remain in
[the snapshot](archive/snapshots-2026-09-27/docs/CANONICAL_RENDERING_PIPELINE.md).
Its raw launches,Desktop target and old blanket mandates are superseded by this page.

Keep the useful findings: separately import companion sub-assets; use versioned
image names; reason about parent/model yaw before changing a pet's facing; check
which arm dimension the hand-anchor probe measures; and inspect thin decals against
the actual inverted-hull outline. Historical millimetre values are examples,not
permission to apply one geometry correction to every character.

## Rules for leaves and shadows on every map (owner, 2026-10-31)

These hold on every map, including new ones. They live in shared code, so a new map follows them without any extra work. Do not undo them per map.

- **Leaves stay out of the world outline's depth-normals prepass.** `LagoonFoliage` uses `RenderType` `PayoFoliage`, as `PayoBlades` uses `PayoGrass`. Drawn in that prepass, leaves behind a floor or wall left specks of their normals over it, and the ink drew them as faint plant outlines. The pixels held the plant's normal at the floor's depth; why they got through is still not known. A new leaf shader must also use a `RenderType` the replacement shader does not know. Exception: `KantoFoliage` stays in the prepass and is masked instead (`WorldOutline.IsToonSurface`).
- **Tree leaves keep their full mesh to 80 m from the camera on Balanced and High.** `Runtime/Map/FoliageLodFloor.cs` pushes out the first switch of any `LODGroup` with leaves and at least two levels when a scene loads, so builders cannot switch nearer. Low is left alone on purpose. Single-level groups are culls and are not touched.
- **Every directional light with shadows gets a normal bias of at least 0.6.** `GraphicsProfiles.RaiseSunBias` applies it when a scene loads. Lower values gave shadow acne, visible as stripes on sunlit floors. High uses two shadow cascades.
