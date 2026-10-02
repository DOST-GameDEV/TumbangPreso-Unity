# Timed recovery, October 2

The latest owner Feedback retires all mash-to-escape status recovery. Frozen and
other stuns keep their authored duration; trips now expire on their existing
TripTotal clock, including the get-up beat. Edge catches advance through the
existing catch, timed hang and pull-over phases without repeated Jump. Bots wait
on the same clocks. Ordinary Jump, the single jump to begin a valid swim climb,
and hold-Interact root removal remain unchanged.

Both current and legacy HUD paths show state or timed get-up/climb progress,
without recovery key prompts, press-count pips or touch Jump emphasis. The native
Frozen and get-up captures were inspected. This is a shared recovery change,
not a redesign of any finalized hero or its art.

Legacy recovery APIs refuse presses. Old buffered presses and snapshot mash
counts cannot shorten restored clocks. The request receiver retains ownership
checks and ignores refused requests. Packet fields and recording format13 remain;
protocol134 requires matching rebuilt clients. Unused pure legacy Core helpers
remain for historical compatibility, with no live recovery callers.

## Evidence

The baseline Frozen case reproduces the old accepted recovery press. The final
runtime hashes match the frozen candidate across all subsequent checks. Fifteen
separate native cases pass, one per fresh process, with separate import and the
unchanged memory guard. Exact cases, durations and exits are in
[native-results.json](native-results.json).

Coverage includes automatic trip and Frozen expiry, repeated actual Jump input,
ordinary jump, bot input neutrality, autonomous edge phases, keyboard/controller/
touch input, held-input neutrality, menu/custom-binding boundaries, authoritative
snapshot ordering, refused legacy requests, Cheska ice retirement, and the live
HUD. The touch matrix covers its existing context/cadence combinations.

Two test-authoring errors were retained and corrected: the new InputEdge test
needed System.Linq to compile; the HUD progress assertion initially inspected
Image.fillAmount although this existing UI renders progress with anchorMax.x.
The corrected assertion checks the same exact half-progress value. Neither
correction changes production behavior or relaxes its requirement. The final
separate import and both rerun cases pass. No memory limit was raised.

Affected legacy rooftop/lagoon and bot matrix expectations were updated to the
new contract, but those entire longer matrices were not rerun. These are focused
native editor results, not a full regression, actual-peer qualification, new
Windows player validation or human approval. Existing saved profiles, unrelated
asset-import changes and reserved source remain untouched.

The BH Studios intro/skip-label/white-fade request is a separate next unit.

## Integrated publication candidate

Remote advanced to268c1abf with LAN identity retention. Integrated it without
conflicts; its NetSession changes are separate from the protocol134 version
change. Separate native import and the timed snapshot restoration case pass
after integration (40/35seconds, guard null). All recovery bodies are unchanged.
The contributor's retained identity evidence remains in its own report.
