# Regression contract review, 2026-09-27

Read-only review against EditMode baseline `04886cc4`, `Logs/stability-0927-edit-baseline.xml` (100 cases, 14 failed). Three `HeroPresentationTests` failures have a separate owner and are excluded here. No test or gameplay source changed during this review. The six loadout cases below share one assertion failure, so they are one contract correction with six inputs.

## InputMapAndAbilityTests.cs

### `UltimateCostsAreRankedByHowMuchTheUltimateSwingsARound`

The old total order `Zack > Cheska > Sean > Dante > Nemu` is superseded by the owner-adopted role kits. Current costs are Zack 20, Rafi 16, Paete 16, Sean 15, Phaister 15, Amihan 15, Dante 14, Cheska 12, Nemu 10. In particular, ABSOLUTE ZERO is explicitly 12, EARTHQUAKE 14, and HIGOP 15 in `RosterReworkRules.cs`; `Core.Tests/StatusAndAmihanRulesTests.cs` already checks the revised rule values. Replace the total-order assertions and old Glacial Nova/Titan Fissure text with a table asserting each shipped kit's adopted cost, all within the retained 10-to-20 objective range. Keep the safe-throw anti-spam bound, checked against the actual minimum cost rather than assuming a specific hero is always cheapest. Do not tune Cheska's ability to satisfy the retired rank.

### `EveryShippedAbilityIsGatedByExactlyOneOfCooldownOrCharges`

Keep the useful invariant but walk `kit.AllAbilities` for all nine current Hero Strike kits, excluding each ultimate, so both attacking and defending role abilities are checked. A charge skill must have zero cooldown and start full; a cooldown skill must have positive cooldown and zero charges. The old universal 45-to-65-second band is no longer the design: Dante BOULDER is 30 seconds and BARRIER 25 in `RosterReworkRules.cs`, while the three deliberate defending `PlaceholderRoleAbility` slots have 10-second cooldowns and no gameplay effect. Assert the exact owner-set cooldowns in the core rules tests, including the placeholder count/identity separately, instead of installing a new global floor that would erase tactical differences. Keep the ultimate meter-only gate assertion.

### `ChargesComeBackOnPlayAndOnlyForTheSkillsThatShould`

Cheska now has COLD FEET, FROSTBITE and GLACIAL WALL, all cooldown skills (`CheskaHeroKit.cs:49-53,80-84,113-117`). The first failing assertion still expects the retired one-charge Ice Barricade. Replace its wall-spawn EditMode fixture and broad `LogAssert.ignoreFailingMessages` with bookkeeping on real charge skills: Sean's attacking IGNITION CANNON starts at two and refills on `LataKnocked`; Zack's attacking MAGNET starts at one and refills on `LataKnocked`; Rafi's CROSSCURRENT and MIRRORWAKE each start at two and have `Recharge.Never`. Use `ApplyNetworkSnapshot` to represent a spent charge, then assert a wrong event does nothing, the owned event returns exactly one, and further events cannot overfill. This tests the resource contract without spawning a barricade in EditMode or hiding unrelated errors.

## HeroLoadoutRefreshTests.cs

All six `SameHeroVariantRefreshPreservesLiveStateAndDoesNotStack` inputs fail at `UpdateLoadout(alternate) == true`. `HeroLoadoutRules.SidegradesOpen` is intentionally false (`HeroLoadout.cs:163`), and `HeroAbilitySystem.UpdateLoadout` returns false at its guard (`HeroAbilitySystem.cs:132-136`). `BindHero` discards incoming saved/wire builds and keeps authored default presentation (`HeroAbilitySystem.cs:150-205`). Replace the six positive alternate-refresh cases with a hardcoded-kit contract: bind each hero with a stale alternate build, capture the kit and all real ability instances/tuning, set nonzero ultimate charge, cooldown, charges and active duration, then call `UpdateLoadout` repeatedly with alternate and null. Every call must return false; ability references, authored names/tuning and live state must stay unchanged. Check `VariantFor(1/2)` resolves the default IDs while the tree is off. Preserve `RemotePickRefreshAppliesTheNewDefaultWithoutReplacingTheHero` and `ClassicPickRefreshNeverCreatesPowers`; revise their stale wording only if the behavioral assertion changes. Do not re-enable sidegrades to make the six old cases pass.

## RuntimeLayerTests.cs

`Nemu_AstralProjection_SupportsReactivation` describes the removed Astral Projection. Nemu now has TERRIFY, attacking KURO FETCH and defending KURO GUARD (`NemuHeroKit.cs:51-60`); KURO FETCH requires an attacker with a loose owned slipper (`NemuHeroKit.cs:124-141`) and has no early recast. Replace this test with the role contract: `HasRoleAbilities` true, attacking `Skill2` is KURO FETCH and not recastable, `SetRole(true, ctx)` changes live `Skill2` to KURO GUARD and keeps KURO FETCH as the idle role ability, and switching back restores the attacking slot without creating another kit. Existing `NemuKitContractProbe` remains the place for actual fetch/guard gameplay and round-state checks. Do not restore Astral Projection or add a loose-slipper fixture to a removed behavior test.

## BroadcastPassTests.cs

`AllSixHeroesHaveTheirOwnNamedUltimate` still expects Dante TITAN FISSURE, Cheska GLACIAL NOVA and Phaister GRAND COVEN. The current owner-approved names are EARTHQUAKE, ABSOLUTE ZERO and HIGOP (`DanteHeroKit.cs:180`, `CheskaHeroKit.cs:143`, `PhaisterHeroKit.cs:333`); Sean SUPERNOVA, Zack THUNDERSTRIKE and Nemu DEVOURING SEANCE remain. Replace those three expected names and keep the nonempty/unique assertions so the introduction card's actual kit names stay pinned. The six-hero motif test shares this array; expanding that separate motif scope to nine belongs to its own product review, not this stale-name repair. Nemu's proposed KURO PLAYS remains future work and must not be asserted as shipped.

## ModelPreviewTests.cs and current character-select routes

The default route is `ConvertedMatchSetup.HubEnabled = true` (`ConvertedMatchSetup.Hub.cs:27-42`), then the lobby's CharacterDoor into `HubCharacterSelect`. That screen already reads all four `kit.ScreenSlots` and draws four ability tiles (`HubCharacterSelect.cs:205-222,242-260`). The test `HeroCharacterSelectShowsAbilitiesInsteadOfClassicAttributes` instead directly opens the legacy `CharacterSelectPanel`, walks three `TumpSkillSlot` tabs and expects a `HeroLoadoutRules` description (`ModelPreviewTests.cs:379-454`). It therefore does not qualify the default hub route. Add or relocate focused default-route assertions: select two distinct current heroes, compare all four visible tile glyphs and each inspected name/summary with that hero's `ScreenSlots`, assert no stale Classic SPEED/POWER/GRIT strip, and assert no SkillTree/EQUIP affordance while `SidegradesOpen=false`. This also covers the QA report that skill icons can stay on the previous hero.

There is one current-route product defect to fix before that assertion can be trusted: `HubCharacterSelect.InspectAbility` reads a saved alternate name/description even while `SidegradesOpen=false` (`HubCharacterSelect.cs:280-289`), but the match kit ignores the saved alternate (`HeroAbilitySystem.cs:150-205`). Guard that display lookup with `SidegradesOpen`; test with a previously saved/unlocked alternate and verify the name/summary shown match the authored ability actually equipped. `HubHero` detail already uses the current ability and gates its skill-tree link by the same flag (`HubHero.cs:289-315`).

The `-tp-preparation-board` flag is a documented supported fallback (`ConvertedMatchSetup.Hub.cs:27-42`), and its player-visible SKILLS door still reaches `TumpSkillView` through `ConvertedCharacterSelect.cs:99-110`. `TumpSkillView.FieldGuide.cs:25-42` and `TumpSkillView.OwnerPainted.cs:85-129` still show three tabs and old variant/EQUIP controls. Keep a separate fallback test and align that guide with four `ScreenSlots`, their current names/summaries and role labels, with no retired sidegrade controls while the switch is off. Do not delete this fallback assertion to make a default-route test green, and do not reopen the hidden SkillTree.

## Implementation and remaining checks

The eleven reviewed EditMode failures now have replacements in the four scoped test files: `InputMapAndAbilityTests.cs` checks all nine prices, four-slot gates and current charge-event rules; `HeroLoadoutRefreshTests.cs` checks that six stale alternate builds cannot change authored kits or live state while sidegrades are closed; `RuntimeLayerTests.cs` checks Nemu's fetch/guard role swap; `BroadcastPassTests.cs` pins the three renamed ultimates and retains the six-card uniqueness check. No production kit values or status rules changed.

Fresh graphics-enabled guarded EditMode validation passed **74/74**, zero failures or skips, in 0.4033123 s: BroadcastPass 15, HeroLoadoutRefresh 8, InputMapAndAbility 27, RuntimeLayer 24. The isolated candidate is `026fed74` plus these four test corrections and opt-in diagnostic instrumentation. The parent independently read the XML and verified all four tested files are byte-identical to the main working copy. [Exact receipt](checks/current-kit-contracts.xml). The named profile and shared input preferences were restored by the runner. This focused pass is not the full PlayMode gate or a release verdict.

The separate UI correction subsequently passed its current-selector, stale-alternate,
availability and four-slot preparation-guide methods in [QA2](qa2-validation.md).
The availability fixture needed its real lobby-to-CHARACTER route before its targeted
retry passed. The three `HeroPresentationTests` baseline failures were separately
repaired and passed 24/24 in the first published stability batch. A host-only or
EditMode pass does not establish full peer behavior.

## Shared Ultimate Input Contract

The baseline's two failed two/four-caster cohorts used held input for release-only
HIGOP and the retired 1.55-second warning. The corrected existing cases pre-arm HIGOP,
release it alongside the other casts, and use current VoodooRules.HigopCastSeconds.
Exact member/execution counts, paused simulation, common deadlines, cleanup and a
subsequent cohort remain asserted. Runtime phase and ability code are unchanged.
The graphics-enabled [targeted receipt](checks/current-shared-cohorts.xml) passed 2/2
in 29.992 seconds on the first focused run, with no skips or tooling retry. The original
failed gate remains preserved; this local result does not establish network-peer timing.
