# Paete wooden slipper flight correction

The owner's attacking Bakya Bloom ready-recast report reproduced in a real Hero Strike match on Bayan Plaza. The recast spawned its wooden slipper, but the shoe travelled only 0.1951218 m during the test window and dropped beside the plant. A direct launch independently travelled only 0.2113819 m after the quarter-second observation. The existing kit probe only tested object existence, so it missed the faulty flight.

`Slipper.SolveArc` returns a unit direction, as documented and used by ordinary slipper launch. `PaeteWoodenSlipper.Spawn` assigned that direction as velocity without applying `PaeteRules.WoodenSlipperSpeed` (13 m/s). Multiply by that existing speed; preserve aim solving, gravity, plant growth/reload, host-only scoring, withering and finalized visuals/kit names.

## Native evidence

- Immutable base `0b16a2e98484e3df8c480953c0ea3d1bdbc479f9`, protocol148. Windows laptop gamergmae; installed/project Unity6000.5.8f1; D3D11 PlayMode on physically isolated qa-a with its separate validation company/product and profile.
- Original Unity36860: three exact cases, two causal flight failures and one passing ungrown-refusal control, zero skips; exit2.
- Candidate Unity33888: same three cases pass, zero skips; exit0. Actual ready recast travels2.536600 m and direct launch2.536602 m. The grown slipper is consumed and the sole/both straps have enabled renderers and nonempty bounds. Ungrown `Fire` still refuses and creates no projectile.
- Both runs freeze19256 source/asset/settings inputs. The only original/candidate input difference is PaeteHazards.cs; the same added test bytes run on both. Candidate reuses the original inputs after verifying every hash.
- Each run has210 generated metadata/Auditor deltas, classified separately from runtime/test inputs. Before/after bytes remain in the native output folders and were restored to the exact frozen inputs. Existing9 EditorPrefs values and QualitySettings are restored. No shipping checkout import or user profile is used.
- Raw XML, launch receipts, complete qualified input maps, source/fixture snapshots and restored-state receipts have SHA-256 values in raw-hashes.json. Native full logs and generated-delta bytes remain under `C:/Users/Matthew/dev/tump-workers1003/qa-a/Logs/paete-flight-{original,candidate}1-direct1004`.

## Limits and next work

This proves the actual offline attacking ready-recast path and native flight displacement. Enabled renderer/bounds checks do not replace camera pixels or human visual acceptance. It does not prove remote host-approved recast, hit accuracy on every map, or a fresh packaged two-machine match. The earlier shared5d148 package excludes this correction. Paete's carried-slipper emote report remains unresolved and separate; no attachment, model, animation or artwork was changed. Practice-bot physical operator acceptance remains open.
