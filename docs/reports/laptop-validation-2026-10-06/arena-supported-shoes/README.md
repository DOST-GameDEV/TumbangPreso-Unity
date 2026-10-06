# Arena supported-shoe recovery

## Original failures and correction

Source43229b93bcd7f01a4bd8513d0acc5c7497729763 on local Windows11 host gamergmae, Unity6000.5.8f1. `Slipper.Land` and the half-second loose-shoe sweep judged every high shoe against its owner's current feet plus1.2m. Shoes supported by Tore's1.5m deck and Entablado's1.2m apron returned to the owner before the owner approached. This removes the retrieval challenge for people and gives misleading bot success.

The strengthened original run has two actual failed UnityTests. Supported landing and loose recovery move the Tore shoe4.755m and the Entablado shoe4.380m. The five-route test detects Tore's3.910m loose-shoe displacement. Floating and ordinary roof controls recover correctly. The preceding original3 test passed because it only asked whether the bot obtained its shoe; that weak result is explicitly unqualified for Tore traversal. Earlier original1 invalid difficulty enum and original2 invalid Plaza ramp filter were fixture setup failures, corrected before this causal run. Candidate1 launcher stopped before Unity/input mutation while original restoration was pending; candidate2 is the actual corrected-source run.

`NeedsHeightRecovery` retains the owner-height rule and exempts only a shoe resting directly on an upward-facing collider in the active Arena layout. It checks a short span around the skin's real rest height, so a roof above a deck or floating shoe cannot qualify. The existing nonalloc ground buffer is reused with a complete-query fallback if full. Authority, pickup radius, ownership, stage geometry, fall/drone timing and finalized heroes are unchanged. Both landing and the loose sweep use the same decision.

## Native evidence

Original Unity54752: actual0PASS/2FAIL, exit2. Candidate Unity51796: actual2PASS/0FAIL, exit0. Both qualified runs restore all21192 inputs, preserve279 import deltas, restore13 existing preferences and all four original named-profile files. Same two-test fixture and base runtime, with only Slipper.cs changed in the candidate. Each run freezes21192 named-worker inputs. Both use the isolated validation-qa-a-be075dcdfa07 profile;13 existing editor preferences and four original profile files are restored. Native import changes are retained before restoring source.

The route test runs the actual ordinary AI Update/planner/motor/Carrier on a real Arena scene, choosing authored ramp2 in each of Plaza, Tore, Krus, Hukay and Entablado. It controls starting marks and shoes, disables the match director/other actors, and requires retrieval without loose-shoe relocation. This proves those five controlled approaches, not general navigation, competitive decision quality, natural full matches or bonus-pad routing. The companion check exercises real Land and real timed loose Update on all five layouts, with inaccessible roof and floating-shoe controls. Roof Land is invoked explicitly to isolate that branch.

Full packaged current-source peer admission/readiness/match/saved-result agreement and physical input/operator acceptance remain open. No remote player was launched and no UI acceptance is claimed. Existing uncommitted Claude UI work is excluded.

Raw receipts are preserved byte-for-byte by the folder attributes; SHA256.json covers the retained evidence. The candidate source delta names the only runtime input change against the full original manifest.
