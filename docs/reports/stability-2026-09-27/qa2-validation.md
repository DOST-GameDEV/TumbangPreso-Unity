# QA2 Validation, 2026-09-27

The guarded Windows/D3D11 QA candidate initially used the isolated `work/stability-validation` checkout at `026fed74` plus a recorded 51-file authored-source snapshot. A two-file joiner-title amendment is included in that count; newer unrelated main-checkout changes are not. Later scoped amendments reached 55 files, with recorded source hashes checked before each run. Source copies, native originals, failed attempts and profile-preservation records remain in the isolated checkout. [Checksums](qa2-evidence.sha256) cover 39 evidence files: the initial 11 XMLs and 15 images, the Paete receipt and eight after images, the upload-copy receipt, and the combined title/input receipt with two images. No older evidence was overwritten.

## Results

**Latest accepted amendment:** the combined room-title/input group passed 4/4 in
14.860 seconds on the 55-file candidate. It replaces the earlier controller-local
cache case and adds two session cases plus the actual name-field path. Latest distinct
outcomes are **3/3 EditMode and 17/17 PlayMode** across the focused runs. The reviewer
parsed the XML, inspected the edited-field and created-lobby images, and matched all
48 source files selected for integration to the candidate. This does not supersede
the failed broad baseline gate or establish real-peer/physical-device coverage.
See [title lifecycle](qa04-joiner-title.md) and [field investigation](qa04-lobby-title.md).

The subsequent [Practice upload-copy check](checks/qa2-play-practice-upload-copy1.xml)
passed 1/1 on the preceding 54-file snapshot, bringing that stage to **3/3 EditMode and
14/14 PlayMode**. That check exercises the pure copy decision,
not a rendered result page or live upload. That stage preceded the session-title
refinement; no physical typing or real-peer claim follows from the earlier cache test.

Later three-file Paete/shop amendment: the expanded same-method check passed 1/1 in
20.889 s on a 52-file recorded candidate, with 16 captures saved. The before-failure
below remains preserved. At that stage the distinct outcomes were **3/3 EditMode and
13/13 PlayMode**, not a single full-suite run or final release verdict. Exact framing,
caption bounds and ratios are in [Paete framing](paete-framing.md). The later upload-copy
and session-title amendments above address the subsequent review findings.

Before that Paete amendment, deduplicating XML identities across targeted retries gave
**3/3 distinct EditMode passes** and **12/13 distinct PlayMode passes**. The table below
preserves that initial candidate's failure/repair lineage.

| Scope | Initial receipt | Final targeted receipt | State |
| --- | --- | --- | --- |
| Career and LAN room-title parsing, 3 EditMode methods | First launch compiled no tests; no XML | [edit-title-career-repair1](checks/qa2-edit-title-career-repair1.xml), 3/3 | Pass after one-character test syntax repair |
| Practice/LAN match eligibility | First PlayMode launch produced no XML | [identity-fixture-repair1](checks/qa2-play-identity-fixture-repair1.xml), 1/1 | Pass after preserving Test Runner `InitTestScene` in fixture teardown |
| Selector roles, closed sidegrades, availability, guide | [selector-guide](checks/qa2-play-selector-guide.xml), 3/4 | [selector-repair1](checks/qa2-play-selector-repair1.xml), 1/1 for the failed availability case | Four distinct methods pass; fixture now waits for lobby and presses CHARACTER |
| Login, mode cards, Practice picker | [login-modes](checks/qa2-play-login-modes.xml), 3/3 | [login-visible-repair1](checks/qa2-play-login-visible-repair1.xml), 1/1 for visual password fault | Three distinct methods pass; first login image had no visible fault despite the nominal test pass |
| Score formatting and native fit | [score](checks/qa2-play-score.xml), 1/2 | [score-repair1](checks/qa2-play-score-repair1.xml), 1/1 for native fit | Two distinct methods pass; score box widened 118 to 120 units within the same chip |
| Room chat layout and routes | [lobby-chat](checks/qa2-play-lobby-chat.xml), 1/1 | None needed | Pass; four seats clear in compact/history and larger-text captures |
| Paete versus Sean in shop and selector | [paete-sean](checks/qa2-play-paete-sean.xml), 0/1 | None | **Fail:** shop Paete 0.678 versus Sean 0.705; selector Paete 0.650 versus Sean 0.680 at 1600x680 |
| Joined client title and seated code | [joiner-title](checks/qa2-play-joiner-title.xml), 1/1 | None needed | Pass; controlled client-provider path |

The first identity launch had no XML; the disk guard stopped its still-running editor above the 4 GiB reserve. A fixture-only retry logged both successful test-body completion and preservation of the Test Runner scene, then produced 1/1. The Paete XML is complete and records a real failed assertion after all eight shop/selector images were captured; its Unity process was stopped by the disk guard during result teardown. Neither abort is counted as a passing test.

## Native Samples

The [QA2 image set](native/qa2/) contains the visible password fault and its invisible before-state, the checked consent box, mode cards, Practice popup, owned/unowned portraits, negative and compact extreme scores, owner-window chat history, and the defending field guide. Shop-stage Sean/Paete before-pairs at 1600x680 and 2340x1080 are kept there for later comparison; the full eight shop/selector originals remain in isolated Logs. These images do **not** close Paete sizing. The shop captures also show ATTACKING and DEFENDING captions colliding across adjacent four-power tiles, a separate UI-fit defect.

No player build, broad suite, physical keyboard/device input, real two-peer delivery, Relay session, or human gameplay verdict was produced by this candidate. The joiner-title pass is not peer-delivery proof, and the native captures are not a performance before/after comparison.
