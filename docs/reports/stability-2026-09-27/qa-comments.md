# QA comments, owner-adopted 2026-09-27

Source: tester screenshots dated 2026-09-26, reposted at higher resolution by the owner
with "add these to comments ... resolve this all". These rows deduplicate the earlier
three screenshots; they do not represent a second set of bugs. No item is resolved by
source inspection alone unless an explicit newer product decision supersedes it.

| ID | Tester report and intended result | State / acceptance |
| --- | --- | --- |
| QA-01 | Title TUMP logo and can look blurry while floating leaves look excessively sharp. | OPEN: inspect native title at desktop/phone shapes; correct rendering/import/composition without repainting supplied artwork or lowering its quality. |
| QA-02 | Skill-tree columns are misaligned and the character circle looks poor. | SUPERSEDED SURFACE: newer ABILITY-2 hides the Home skill-tree door and guards the hero-popup link with SidegradesOpen=false. The supported preparation guide now shows four authored roles; [QA2](qa2-validation.md) tests that route. The old tree is not revived or claimed visually repaired. |
| QA-03 | Game-mode card artwork overflows its frame, illustrated by PRACTICE. | FIXED, focused native evidence: art-only shaped stencil, preserved poster pixels/aspect, title and tape. Mode-card method passes in [QA2](qa2-validation.md); no general UI redesign. |
| QA-04 | Lobby name cannot be changed. | ENGINE PATH VERIFIED: actual plate raycast, full-prefill replacement through Unity key events, CREATE and visible LAN heading pass. Session-owned known-listing titles also pass controller recreation, both seating orders, stale-callback refusal, disconnect and listening restart. [QA2](qa2-validation.md). Physical OS typing, real peer delivery and code-only title lifetime remain unverified; no speculative input fix was made. |
| QA-05 | Terms and Conditions consent should display a check mark. | FIXED, focused native evidence: checked/unchecked consent uses the existing check glyph and retains gating. Latest owner adoption overrides the old solid-fill-only instruction for this control. [QA2](qa2-validation.md); physical hardware qualification remains separate. |
| QA-06 | Password generation/entry failure needs an error message. | FIXED for the reproduced local/password-provider path: stale busy text clears and the field fault visibly renders. [QA2](qa2-validation.md) retains the initially invisible capture and repaired pass. No live authentication service claim. |
| QA-07 | Change the music. | RESOLVED BY OWNER: a temporary prank track caused this complaint and the owner explicitly says it is fixed. Leave current music alone. |
| QA-08 | Paete is too small in the shop. | FIXED in the view-only [framing unit](paete-framing.md): Paete now reads larger than Sean and within10% of Cheska on actual shop/selector at owner/phone shapes, without cropping. Full role captions also fit. No model, gait or Roster edit. |
| QA-09 | Dante appears twice in the profile-picture selection. | FIXED in the first [verified batch](validation.md): Amihan/Paete textures now import as Sprite, eliminating their Dante fallback. Offered-ID/resource test passes; no profile data removed. |
| QA-10 | Lobby chat placement obscures the room/player presentation. | FIXED for exercised lobby routes: coordinated player/chat columns, recenter on hide, history/back/navigation retained. [QA2](qa2-validation.md) covers native compact/history and larger-text views; match chat is unchanged. |
| QA-11 | "Labuan yung mga di available na character": blur/dim unavailable characters on character selection. | FIXED, focused native evidence: unowned online portraits dim with identity/hatching retained; inspect works, SELECT refuses, offline/LAN remain available. [QA2](qa2-validation.md) uses an isolated controlled route, not a live wallet. |
| QA-12 | Skill icons do not change with the selected character; only one character's icons remain visible. | FIXED in [first-batch](validation.md) symbol invalidation and [QA2](qa2-validation.md) current-role/copy checks. Reused meshes/materials refresh; closed sidegrades cannot leak saved alternate descriptions. |
| QA-13 | New maps load slowly; preload almost all assets during the intro. | Tester report retained. No implementation or validation result is claimed for this row in this batch. |
| QA-14 | Show how to escape Paete's ultimate. | FIXED in the first [verified batch](validation.md): live binding/progress and completed escape exercised with synthetic keyboard, pad and touch input. Physical hardware remains separate. |
| QA-15 | Sean stops moving after Cheska's ultimate. | OPEN: reproduce offline and owner/host/observer status expiration, movement and input release; evidence required before cause/fix claim. |
| QA-16 | "Nag kaka tansan sa practice mode": practice is awarding tansan. | LOCAL FIX VERIFIED: start-route eligibility, no offline career/upload, C#/JS settlement exclusion and old-upload no-op. [Reward report](practice-rewards.md) and [QA2](qa2-validation.md). Owner deployment/live wallet verification remain open; existing balances and temporary top-up are untouched. |
| QA-17 | HUD scores sometimes overflow. | FIXED for sampled native extremes and ordinary scores: nonwrapping 28-unit-floor fit, exact values below one million, compact M/B display above it. [QA2](qa2-validation.md) includes five view/value captures and formatter boundaries; actual scoring values remain unchanged. |
| QA-18 | Owner reports repeated skill icons. | FIXED in the first [verified batch](validation.md): 33 distinct real powers across 36 slots; three explicit COMING SOON slots intentionally share unavailable artwork. [Icon rationale](skill-icons.md) and native symbol/selector receipts preserved. |

Keep the IDs stable, record exact source revisions and receipts alongside each resolution,
and link any existing TODO implementation rather than duplicating completed changes.
