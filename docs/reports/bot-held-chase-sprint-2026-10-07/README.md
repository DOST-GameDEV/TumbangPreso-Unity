# Preserve legal sprint during a held chase

A defender finishing a long-held lunge cleared the sprint request that ordinary
Hunt had just made. The next frame also lost sprint commitment, so a rested
defender could walk behind a faster vulnerable runner indefinitely. The aim
correction now uses the same stamina-gated sprint eligibility and distance rule
as ordinary pursuit. Charge, physical contact, cooldown and reset gates remain
unchanged.

The original source fails two of fourteen corrected native controls. With a
normal running attacker at5m/s, the fresh0.6s charge closes the gap6.6->6.4m at
7.5m/s. The long1.2s charge instead walks at3.75m/s and loses ground6.6->6.7m;
ordinary sprint commitment never starts. The candidate passes all14 controls:
both charged cases close6.6->6.4m at7.5m/s without spending an unreachable dash.
Stamina reserve, fatigue, sprint rest, cooldown, reset, targetless holds and
actual motor aim controls also pass.

The first original fixture accidentally used a walking2.5m/s attacker. Its
fresh-charge assertion rejected a physically reachable dash, so that failure is
preserved rather than called a production defect. The target was corrected to
consume ordinary legal sprint and the original production bytes were rerun
before the fix. The two sprint failures persisted.

PC independently verified the exact three candidate source files and nine raw
native receipts in [evidence.json](evidence.json). Original23176,
corrected-original49208 and candidate50220 plus their preservation/restoration
workers are terminal. All21,383 frozen inputs and shared preferences were
restored exactly after each run. The candidate AI SHA256 is
`a1636c4b0cabf68abe73b393c792c57661dc88869163e8da2cfc278b5ba3823f`.

This proves the focused producer/consumer movement correction. Natural-match
efficacy, all-feature bot quality, current packaged behavior and frame pacing
remain open. The separately qualified Desktop725e release is unchanged. The
current integration's replay endpoint failure still prevents release
qualification; it is being corrected in the laptop's reserved lane.
