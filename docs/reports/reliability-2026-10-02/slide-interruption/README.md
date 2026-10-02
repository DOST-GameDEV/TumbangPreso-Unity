# Retrieval slide interruption, October 2

An accepted retrieval slide kept its pickup window when the attacker was stunned, feared or taken out of a live round. `CombatVerbs.Update` returned while the body could not act and cleared the lunge window, but left the slide window untouched. On recovery, that old window resumed its sweep and could pick up a nearby eligible slipper without another slide input.

The existing interruption branch now clears `_slideActiveLeft` alongside `_lungeActiveLeft`. A presentation hold or ordinary pause still preserves the accepted action. Movement, stamina spending, cooldowns, recovery timings, pickup ownership, manual retrieval and hero callbacks are unchanged. No protocol change was made.

## Controlled native evidence

Four EditMode cases make a real `HostResolveSlide`, apply the actual stun/fear/round-end or pause state, call the existing update path, then restore the ability to act with an eligible shoe nearby. They check actual carrier possession after recovery, with no new slide input. The fixture uses no input-action infrastructure, graphics world, transport, SDK or live service.

- Original baseline: **3 causal failures / 1 control passed**. Stun, fear and round end each resumed the cancelled sweep and executed an unwanted pickup. The pause control correctly resumed its legitimate slide.
- Candidate: **4/4 passed**. Interrupted actions no longer retrieve; the pause control still retrieves.
- Zero fixture/tool repairs or native retries. Baseline native duration was 0.1753864 seconds; final was 0.1549652 seconds. These durations are not a gameplay performance comparison.

This proves the interrupted action's state and real pickup behavior under controlled recovery. It does not measure physical slide travel, frame timing, hardware input, hero presentation or a network peer's latency.

## Source and preservation

Baseline source: `5f47abc848512d73df488ebd5147666b5f5a211f`. The only product change is the interruption branch in `Assets/TumbangPreso/Runtime/CombatVerbs.cs`, with new `Tests/SlideInterruptionTests.cs` and its valid metadata. Exact original/candidate snapshots remain under main `Logs/slide-interruption1002-inputs`, indexed by `inputs.json`.

The serial CPU jobs ran in `tump-feedback-0930`, profile `slide-interruption1002`, 1536 MB job budget / 2048 MB reserve and 450-second timeout. Baseline session 30095 used guard 21308 / Unity 22220; final session 50273 used guard 23956 / Unity 24084. Both completed profile/input preservation and released their leases, and both owned Editors exited. All 1,472 protected source/settings hashes remained unchanged; main and qualification candidate hashes matched their frozen inputs.

Raw XML, job/guard receipts, preparation record, inputs, protected hashes and case-level results are retained here. Full Editor logs remain in qualification `Logs/slide-interruption1002`. No private profile contents or unrelated private source assets are copied into the report.
