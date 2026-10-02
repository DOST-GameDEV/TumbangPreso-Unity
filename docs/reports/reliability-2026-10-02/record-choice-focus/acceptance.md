# Career dropdown refresh preserves current navigation focus

The current PlayerHub CareerMode picker uses RecordChoice. Its value callback synchronously rebuilds the career page, deactivating the old choice and assigning focus to an active new control. Installed UGUI Dropdown.OnSelectItem then calls Hide after that value callback; Hide calls virtual Select, and the default Selectable.Select does not reject an inactive object. The old choice therefore overwrote current focus with itself immediately before its row was destroyed.

RecordChoice now declines Select when inactive, disabled or noninteractable, and otherwise calls the existing base behavior. The callback, layout, options and cancel behavior are unchanged. A separate OwnerOptionMenu investigation was stopped before any patch or test because that class has no active Create caller; it is not this fix's consumer.

## Actual current UI proof

The three PlayMode cases open real dropdowns through OnPointerClick and use their real generated Toggle selection callbacks. The causal case uses current PlayerHub.Install/OpenTab(Career), changes the actual CareerMode option, verifies its replacement shows the new value and checks current selection remains active both immediately and on the next frame. Two controls verify normal active cancellation and ordinary option selection still return focus to the opener and preserve callbacks. No raw input event framework, SDK, layout retune or Account/Social production edit is involved.

- First original baseline 3 (session 48405) failed all three before focus assertions because the fixture clicked a newly created control before its real Start initialized UGUI's fade runner. Original XML/logs/native input bytes were preserved.
- One bounded fixture repair added one normal playing-frame wait after control creation. Repaired original 3 (session 52733) passed the two controls and reproduced one causal CareerMode stale inactive selection.
- Candidate 3 (session 37580) passed all three against the identical repaired fixture. No further repair or unrelated suite ran.

Unity 6000.5.8f1 PlayMode used the exclusive serialized GPU pool 2048MB/2048MB reserve, 450-second limit, graphics D3D11 at 960x540 and named `record-choice-focus1002` profile. Every preparation completed exit0 before its dependent launch; repaired preparation 89480 was explicitly awaited to terminal before launch. Exactly three source/fixture/meta bytes were verified each phase. All 1294 protected source/private hashes and three installed native inputs matched after final. Guards completed restoration without a held lease, and no task-owned Unity/player process remained at release.

This accepts current EventSystem and callback lifetime behavior. Programmatic pointer/cancel/toggle actions are not a physical controller or mouse acceptance, pixel-layout review, live-authentication proof, whole-match test or standalone inclusion claim.

Original RecordChoice SHA256: `E3746EA42B7091C1668D8B83C5A821458276AB340DA94EB6BDAE4A553C4F52B2`.

Candidate source SHA256: `3CCD25757ED0943310454E95828101077913B9A9763909062A8D3EE43AC1CF9E`.

Original fixture SHA256: `A688D2A50A7E41BE2FB2DF9883FF9304FDA0D9FE0B6B490C953D9091D4B6255D`; corrected fixture hash is recorded in fixture-repair.json and final-inputs.json. Meta stayed `72715D2551836C9D27B4114855C21D766B09A265FAD117D2C88A6AF3615C7A38`.

Raw XML, logs, guard receipts, separate counts and protected/preparation/frozen manifests accompany this report. These narrow results do not establish full competition readiness.
