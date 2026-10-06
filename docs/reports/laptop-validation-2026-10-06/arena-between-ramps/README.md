# Bot retrieval from between Arena ramps

Source438fcac65dc977e463859599f08a39336899318d, including the supported-shoe correction, on local Windows11 gamergmae/Unity6000.5.8f1. This follow-up checks whether the ordinary bot finds a crossing when its straight approach begins between ramps. No additional AI change was needed or justified by this run.

Actual native Unity8696 has one passed UnityTest and zero failures, exit0. Its five controlled approaches all retrieve with no body drone recovery and zero loose-shoe displacement:

| Layout | Retrieval time | Actual deck heights |
| --- | --- | --- |
| Plaza |17.278s|0 to0|
| Tore |11.741s|0 to1.5|
| Krus |16.141s|0 to0|
| Hukay |11.540s|0 to-1.2|
| Entablado |5.080s|1.2 to0|

The scene is the actual Arena. Starts are mid-ring at45 degrees and the central shoe is on the opposite side; Entablado starts at the apron centre bearing0 between its two crossings. Real AI Update/planner/motor/Carrier run, with other actors and match director ticking disabled. The35s limit is the test's route question, not an execution admission limit. No movement intent, retrieval, path or clock is driven through private helpers. Real3D pickup distances are1.724 to1.737m, within the current1.75m rule.

All21194 frozen inputs,13 existing editor preferences and four original isolated-profile files are restored after279 retained native import deltas. The prior qualified worker is reused; source432-to438 changes exactly Slipper.cs and the preceding passing fixture/meta. The added two fixture files are the only new inputs. Raw source-delta records that equivalence without duplicating the full manifest.

These five controlled routes extend the direct-ramp checks in the supported-shoe report. They do not qualify all possible start/goal pairs, moving stages, bonus jump-pad navigation, hostile pressure, natural full-match decisions or packaged peer gameplay. No new pathfinder, geometry change, hero retuning or UI change is included.
