using System.IO;
using TumbangPreso.Abilities;
using TumbangPreso.Core;
using TumbangPreso.Visual;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace TumbangPreso.EditorTools.MapKit
{
    /// <summary>
    /// In-engine capture of what the Hero Strike abilities actually put on the floor.
    ///
    /// ⚠️⚠️ IT EXISTS BECAUSE THE READABILITY BUDGET HAD NEVER BEEN PHOTOGRAPHED. `docs/VISION.md`
    /// § 2 is the most argued-over page in this repository and its rule 5 is a picture test:
    /// *"A screenshot taken mid-fight must still show the lata, the chalk and every player. If
    /// it does not, the effect is too big however good it looks alone."* Nothing in the harness
    /// took that screenshot, so the rule was enforced by opinion for months while the numbers
    /// underneath it drifted.
    ///
    /// The measured state on 2026-08-25, before the footprint pass: the worst credible frame
    /// painted **81.9 per cent of the 14 by 14 box**, before props, tsinelas and nameplates.
    /// `docs/Hero_Strike_Balance.md` § 1.5 has the arithmetic and § 3.2 has what it fell to.
    ///
    /// ⚠️ THE HAZARDS ARE SPAWNED DIRECTLY RATHER THAN CAST. An ability cast needs a motor, a
    /// round, a match and an input intent, none of which exist in edit mode, and the thing being
    /// judged here is the GEOMETRY each ability leaves rather than the code path that leaves it.
    /// `HeroHazards` is the single place every footprint is built, so calling it is calling the
    /// real thing. What this cannot show is timing, and it does not claim to.
    ///
    /// ⚠️⚠️ THE FILENAMES CARRY A VERSION AND `Version` MUST BE BUMPED EVERY TIME. `CLAUDE.md`
    /// § 6.1: chat clients cache by name, so overwriting a render leaves the previous image on
    /// screen and the review is conducted against a picture that is not on disk any more.
    ///
    /// ⚠️⚠️ AND IT NOW PHOTOGRAPHS THE TRANSIENTS TOO, WHICH IS WHAT IT COULD NEVER DO.
    /// `docs/TODO.md` § 8 item 2, open since 2026-08-25: this probe captured *"the persistent
    /// zones only, and every one of these changes is on a transient that lives 0.4 to 1.1 s, so
    /// the v7 captures do not show a single one of them."* Every blast core, every shockwave and
    /// all three ultimates are transients, so the entire § 8 silhouette pass — a nova shell
    /// instead of a sphere, a shockfront with a leading edge, an ion spire instead of a disc —
    /// was reviewed against pictures that could not contain it.
    ///
    /// Two things were in the way and both are fixed rather than worked around:
    ///  * `CreateExplosion` opened with `if (round == null) return;`, so in an edit-mode capture
    ///    it drew nothing at all. `HeroHazards.CreateExplosionVisual` is now the half that puts
    ///    pixels on screen and it does not need a match.
    ///  * `Update` never runs here, so a spawned blast froze on its first frame: scale 0.35, the
    ///    moment before it becomes the thing being argued about. `Visual.VfxTimeline.StepAll`
    ///    winds every effect to the same fraction of its own life, through the same code the
    ///    player's frame comes from.
    /// </summary>
    public static class AbilityShowcaseProbe
    {
        private const string OutDir = "Logs/shots-abilities";
        private const int ShotWidth = 1280;
        private const int ShotHeight = 720;

        /// <summary>
        /// The share of a frame that may sit at or above <see cref="BlownLevel"/> before the
        /// capture is a failure rather than a picture.
        ///
        /// ⚠️⚠️ THIS IS `docs/VISION.md` § 2 RULE 5 AS A NUMBER, AND IT EXISTS BECAUSE THE RULE
        /// WAS BEING BROKEN BY A FACTOR OF SEVEN WITH NOBODY ABLE TO SEE IT. Rule 5 is a picture
        /// test: *"a screenshot taken mid-fight must still show the lata, the chalk and every
        /// player"*. A frame that is 60 per cent white shows none of them, and no amount of
        /// reading the code says so.
        ///
        /// ⚠️ THE BOUND IS MEASURED, NOT PICKED. The v9 set, with Thunderstrike's flash still
        /// wrong: the empty street reads **3.0 per cent**, the worst legitimate effect
        /// (Cheska's frost blast) **8.3 per cent**, the ability corridors **3.0**, the deliberate
        /// worst-frame pile-up **4.1**, and Thunderstrike **62.8**. Everything the team has
        /// already accepted fits under 9; the one defect sits seven times over it. 12 leaves
        /// room for a brighter effect somebody argues for on purpose and still catches this
        /// class of fault on the day it lands.
        /// </summary>
        private const float MaxBlownFraction = 0.12f;

        /// <summary>Luminance at which a pixel counts as blown, out of 255.</summary>
        private const int BlownLevel = 245;

        /// <summary>Frames that broke the bound, reported together at the end of a run.</summary>
        private static readonly System.Collections.Generic.List<string> Blown =
            new System.Collections.Generic.List<string>();

        /// <summary>⚠️ ONLY THE TRANSIENTS ARE GATED, and only while one is on screen. A flash
        /// is the thing that blows a frame; a floor decal cannot. Measuring the persistent shots
        /// too would add nothing and would make the bound answer to a lighting change on the
        /// map rather than to an ability.</summary>
        private static bool _gateBlowout;

        /// <summary>Bump on every capture. See the class note.</summary>
        private const string Version = "v36";

        [MenuItem("Tumbang Preso/Capture Ability Showcase")]
        public static void RunFromMenu() => Execute();

        public static void Run() => EditorApplication.Exit(Execute() ? 0 : 1);

        public static bool Execute()
        {
            Directory.CreateDirectory(OutDir);
            EditorSceneManager.OpenScene(IlalimNgTulayBuilder.ScenePath, OpenSceneMode.Single);

            // Same reason the map probe does it: `EnvColourPass` runs from `Start()`, which never
            // happens in an edit-mode capture, and without it every surface is a raw .mtl colour.
            foreach (var pass in Object.FindObjectsByType<EnvColourPass>(
                         FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                pass.Apply();
            }

            var spawned = new System.Collections.Generic.List<GameObject>();

            try
            {
                // ---------------------------------------------------------------
                // 1. EACH FLOOR EFFECT ALONE, so a single silhouette can be judged on
                //    its own before anything overlaps it. `docs/VISION.md` § 2 rule 3
                //    is about DETAIL, and detail is what a lone frame shows.
                // ---------------------------------------------------------------

                Solo(spawned, "fire_trail",
                     () => HeroHazards.SpawnFireTrail(Vector3.zero, 1.0f, 60.0f, 0));

                Solo(spawned, "shock_trail",
                     () => HeroHazards.SpawnShockTrail(Vector3.zero, 1.0f, 60.0f, 1));

                Solo(spawned, "ice_sheet",
                     () => HeroHazards.SpawnIceSheet(Vector3.zero, 2.3f, 60.0f, 2));

                Solo(spawned, "seance_void",
                     () => HeroHazards.SpawnSeanceVoid(Vector3.zero, 2.8f, 60.0f, 3));

                Solo(spawned, "barricade",
                     () => HeroHazards.SpawnIceBarricade(Vector3.zero, Vector3.forward, 60.0f));

                Solo(spawned, "lava_decal",
                     () => HeroHazards.SpawnCrackedLavaDecal(Vector3.zero, 2.2f, 60.0f));

                // ⚠️⚠️ PHAISTER'S THREE POWERS, AND THE POINT OF PHOTOGRAPHING ALL THREE IS THAT
                // THEY MUST NOT MATCH. Until 2026-08-26 her kit went through one builder at
                // three radii and 🧑 read it straight off the screen: *"her Q is just 2 stars on
                // top of each other"*. `docs/TODO.md` § 24 rebuilt each on its own construction,
                // and these frames are the test of whether that worked: a ward, a tear and a
                // corona should not be mistakable for one another in a still.
                //
                // ⚠️ THE REAL SPAWNERS, NOT THE SHAPE BUILDERS UNDERNEATH THEM, which is the same
                // rule the class note gives for spawning hazards rather than casting them. The
                // ward without its standing marks and its light is not what a player ever sees.
                Solo(spawned, "hex_ward",
                     () => HeroHazards.SpawnHexSigil(Vector3.zero, 2.4f, 60.0f, 5));

                // ⚠️⚠️ THE THING A BLINK ACTUALLY SHOWS YOU IS THE AIM MARK, AND IT HAD NEVER
                // BEEN PHOTOGRAPHED. Every other frame in this probe is what happens AFTER a
                // cast; the blink is the one power in the game whose telegraph is on screen for
                // the whole decision, and it is the half 🧑 has now complained about twice: *"all
                // it shows is a frigging shadow, it's very easy to miss"* (2026-08-27), and then
                // *"I dont want Phaister's E HOLD for casting To just be a shadow, keep that
                // outline and give it her color so that it could be seen more"*. A change to it
                // that ships without a picture cannot be judged, which is `CLAUDE.md` § 6.1.
                Solo(spawned, "blink_aim_reticle", HeldBlinkReticle);

                Solo(spawned, "blink_rift",
                     () => HeroHazards.SpawnShadowRift(Vector3.zero, Vector3.forward));

                Solo(spawned, "blink_arrival",
                     () => HeroHazards.SpawnShadowArrival(Vector3.zero));

                Solo(spawned, "coven_eclipse",
                     () => HeroHazards.SpawnGrandCovenEclipse(Vector3.zero, 5.0f, 60.0f));

                // ⚠️⚠️ NEMU'S ULTIMATE IS HER PET NOW (`docs/TODO.md` § 28) AND IT HAD NEVER BEEN
                // PHOTOGRAPHED IN ANY FORM. The old Seance Void was captured through
                // `SpawnSeanceVoid` above; this is a different object with different geometry, and
                // the thing these two frames have to answer is whether a rim around NOTHING reads
                // as a hole rather than as a dark disc, which is § 27.5's whole claim.
                // ⚠️⚠️ `fromPet: false`, AND THE FLAG CHANGES WHAT IS IN THE FRAME. With a pet
                // out, Kuro himself is the centre and `GhostPetCompanion.Devour` grows him there;
                // an edit-mode capture has no pet and no match, so `true` photographed a torn ring
                // with a hole in it and read as a missing model. The fallback path is the one this
                // probe can honestly show, and it is also the one that most needed looking at,
                // because until 2026-08-27 it was genuinely empty.
                // ⚠️⚠️ THE REAL PET IS SPAWNED AND DEVOURED, WHICH IS THE ONLY HONEST WAY TO
                // PHOTOGRAPH THIS ULTIMATE. Everything else in this probe is a hazard called
                // directly, because the class note's rule is that the GEOMETRY is what is being
                // judged. Nemu's ultimate breaks that rule on purpose: after `docs/TODO.md` § 28
                // the geometry is what happens AROUND the pet, and a frame without him in it is
                // a frame of the half that is not the point. 🧑, twice, at two versions of
                // exactly that frame: *"where tf is kiro in this ult?"*.
                Solo(spawned, "kuro_unbound", () => KuroUnbound(true));

                // The no-pet fallback, which is a different composition and is also shipped.
                Solo(spawned, "kuro_unbound_nopet", () => KuroUnbound(false));

                Solo(spawned, "spirit_return",
                     () => HeroHazards.SpawnSpiritReturn(Vector3.zero));

                // ⚠️ ZACK'S ARCS ARE THE ONE EFFECT IN THE GAME WHOSE SHAPE DEPENDS ON WHAT IS
                // NEARBY, so an empty-street capture is the WORST case for it by construction:
                // nothing to arc to, which is the fallback stub path. That is deliberate. A frame
                // where the fallback looks like a mistake is a frame that says the fallback is
                // wrong, and there is no other way to see it.
                Solo(spawned, "circuit_arcs",
                     () => HeroHazards.SpawnCircuitArcs(Vector3.zero, 3.2f, 1, 60.0f));

                // ---------------------------------------------------------------
                // 2. A DASH CORRIDOR, which is the shape that was actually wrong and
                //    which no single-disc frame can show. Six drops at the capped
                //    count, laid along a run the way `OnTick` lays them.
                // ---------------------------------------------------------------

                Clear(spawned);
                for (int i = 0; i < 6; i++)
                {
                    spawned.Add(HeroHazards.SpawnFireTrail(
                        new Vector3(-3.0f + i * 1.2f, 0.0f, -1.0f), 1.0f, 60.0f, 0));
                    spawned.Add(HeroHazards.SpawnShockTrail(
                        new Vector3(-3.0f + i * 1.2f, 0.0f, 2.0f), 1.0f, 60.0f, 1));
                }
                Shot("corridors", new Vector3(0.0f, 5.4f, -8.6f),
                     Quaternion.Euler(28.0f, 0.0f, 0.0f), 62.0f);
                Shot("corridors_eye", new Vector3(0.0f, 1.65f, -7.4f),
                     Quaternion.Euler(4.0f, 0.0f, 0.0f), 72.0f);

                // ---------------------------------------------------------------
                // 3. THE WORST FRAME. Every persistent floor effect in the game live
                //    at once, which is the § 2 rule 5 test. The lata, the chalk and
                //    the throwing line all have to survive it.
                //
                // ⚠️ IT IS DELIBERATELY WORSE THAN A REAL ROUND. Four seats cannot
                //    realistically hold all of this live simultaneously under the
                //    charge economy, which is the point: if the budget holds here it
                //    holds anywhere.
                // ---------------------------------------------------------------

                Clear(spawned);
                spawned.Add(HeroHazards.SpawnIceSheet(new Vector3(-3.4f, 0.0f, 2.2f), 2.3f, 60.0f, 2));
                spawned.Add(HeroHazards.SpawnCrackedLavaDecal(new Vector3(3.2f, 0.0f, 2.6f), 2.2f, 60.0f));
                spawned.Add(HeroHazards.SpawnSeanceVoid(new Vector3(3.6f, 0.0f, -3.0f), 2.8f, 60.0f, 3));
                spawned.Add(HeroHazards.SpawnIceBarricade(new Vector3(-2.6f, 0.0f, -2.4f), Vector3.forward, 60.0f));

                for (int i = 0; i < 6; i++)
                {
                    spawned.Add(HeroHazards.SpawnFireTrail(
                        new Vector3(-1.0f + i * 0.9f, 0.0f, -5.2f), 1.0f, 60.0f, 0));
                    spawned.Add(HeroHazards.SpawnShockTrail(
                        new Vector3(-5.4f + i * 0.9f, 0.0f, 5.0f), 1.0f, 60.0f, 1));
                }

                Shot("worstframe", new Vector3(0.0f, 7.2f, -11.0f),
                     Quaternion.Euler(30.0f, 0.0f, 0.0f), 64.0f);

                // The same frame from the two positions a match is actually played from.
                float ring = Confinement.AttackerSpawnRing();
                Shot("worstframe_thrower", new Vector3(-1.4f, 1.65f, -ring),
                     Quaternion.Euler(5.0f, 6.0f, 0.0f), 72.0f);
                Shot("worstframe_taya", new Vector3(0.0f, 1.65f, 0.4f),
                     Quaternion.Euler(6.0f, 180.0f, 0.0f), 72.0f);

                // ---------------------------------------------------------------
                // 4. THE TRANSIENTS: the four blast styles and Zack's ultimate, each
                //    photographed at the moment it is biggest on screen rather than at
                //    the frame it is born on. See the class note for why this could not
                //    be done before.
                // ---------------------------------------------------------------

                Clear(spawned);
                _gateBlowout = true;

                Transient("blast_fire", () => HeroHazards.CreateExplosionVisual(
                    Vector3.zero, 4.8f, null, HeroHazards.ExplosionStyle.Fire));

                Transient("blast_quake", () => HeroHazards.CreateExplosionVisual(
                    Vector3.zero, 4.5f, null, HeroHazards.ExplosionStyle.Quake, Vector3.forward));

                Transient("blast_frost", () => HeroHazards.CreateExplosionVisual(
                    Vector3.zero, 4.2f, null, HeroHazards.ExplosionStyle.Frost));

                Transient("blast_slipper", () => HeroHazards.CreateExplosionVisual(
                    Vector3.zero, 2.2f, null, HeroHazards.ExplosionStyle.Slipper));

                Transient("blast_thunder", () => HeroHazards.CreateThunderstrike(Vector3.zero, 7.0f));

                // ---------------------------------------------------------------
                // 5. THE SIX WEATHERS.
                //
                // ⚠️⚠️ THEY ARE GATED FOR THE BLOWOUT BOUND AND THAT IS THE WHOLE REASON THEY ARE
                //    HERE. `Visual.SkyEvent` changes ambient, fog, the sun, the skybox and the
                //    frame grade for five seconds at a time: it is the only thing in the game
                //    that can move EVERY pixel, so it is the only thing that could break
                //    `docs/VISION.md` § 2 rule 5 without touching a square metre of floor.
                //    Every profile is capped at a brightness multiplier of 1.0 by construction
                //    (`ColourGrade.SetEventGrade` clamps it), and this is the measurement that
                //    says so rather than the comment.
                //
                // ⚠️ THE OTHER HALF OF THE TEST IS THE OPPOSITE FAULT, and no number catches it:
                //    a weather dark enough to hide the lata, the chalk and the players is just as
                //    much a rule 5 failure as one that whites them out. That is what the eye
                //    frame is for, and it is why every look also raises a coloured fill light.
                // ---------------------------------------------------------------

                foreach (Visual.SkyEvent.Look look in
                         System.Enum.GetValues(typeof(Visual.SkyEvent.Look)))
                {
                    Weather(look);
                }

                _gateBlowout = false;

                Debug.Log($"[AbilityShowcaseProbe] wrote the {Version} set to {OutDir}.");

                if (Blown.Count > 0)
                {
                    foreach (string line in Blown)
                        Debug.LogError($"[AbilityShowcaseProbe] FAIL {line}");

                    Debug.LogError("[AbilityShowcaseProbe] see MaxBlownFraction: VISION.md " +
                                   "section 2 rule 5 asks that a mid-fight frame still show the " +
                                   "lata, the chalk and every player.");
                    return false;
                }

                return true;
            }
            finally
            {
                Clear(spawned);
            }
        }

        /// <summary>
        /// One weather, at full strength, over the empty street.
        ///
        /// ⚠️⚠️ IT STOPS THE EVENT IN A `finally`, AND WITHOUT THAT THIS PROBE WOULD DARKEN THE
        /// MAP ON DISK. `SkyEvent` writes `RenderSettings`, which is scene state: an exception
        /// between the `Play` and the `StopAll` would leave the open scene holding an eclipse,
        /// and `EditorSceneManager` would offer to save it. The event restores from every exit it
        /// has, and this is the one that says WHEN in an edit-mode capture, where `Update` never
        /// runs and its own curve therefore never reaches the end.
        ///
        /// ⚠️ WOUND TO 1.0 OF THE RISE RATHER THAN THE 0.35 THE TRANSIENTS USE. A blast is judged
        /// at the moment its silhouette is most legible; a weather is judged at full strength,
        /// because full strength is where it either hides the arena or does not.
        /// </summary>
        private static void Weather(Visual.SkyEvent.Look look)
        {
            Visual.SkyEvent.Play(look, 6.0f);

            try
            {
                Visual.VfxTimeline.StepAll(0.5f);

                string name = "sky_" + look.ToString().ToLowerInvariant();
                Shot(name, new Vector3(0.0f, 5.4f, -8.6f),
                     Quaternion.Euler(24.0f, 0.0f, 0.0f), 62.0f);
                Shot(name + "_eye", new Vector3(0.0f, 1.65f, -7.4f),
                     Quaternion.Euler(4.0f, 0.0f, 0.0f), 72.0f);
            }
            finally
            {
                Visual.SkyEvent.StopAll();
            }
        }

        /// <summary>
        /// Nemu's ultimate, with or without the pet that is supposed to be inside it.
        ///
        /// ⚠️⚠️ `GhostPetCompanion` IS AN `IVfxTimeline` SO THAT THIS CAN WORK. His swell runs in
        /// `LateUpdate`, which never fires here, so without that he would stand at bind scale in
        /// the middle of the maw wearing his ordinary face: the exact opposite of what the frame
        /// is for. `Solo` calls `VfxTimeline.StepAll` through the same path every transient uses.
        ///
        /// ⚠️ THE MODEL IS LOADED BY PATH BECAUSE THERE IS NO ROSTER HERE. In a match
        /// `CharacterVisual` instantiates it from `RosterEntryAsset.PetModel`; an edit-mode
        /// capture has no match, no roster and no Nemu, so the probe reaches for the asset
        /// directly. If the pet is ever re-authored, this path is the thing that has to move.
        /// </summary>
        private const string PetModelPath =
            "Assets/TumbangPreso/Art/models/kits/graveyard/character-ghost.glb";

        /// <summary>
        /// Phaister's hold-to-aim mark, exactly as `HeroAbilitySystem.Aiming` draws it.
        ///
        /// ⚠️⚠️ THE COLOUR COMES FROM `UiTheme.BrightForHero`, NOT FROM A LITERAL, and the radius
        /// from `ShadowPhaseBlinkAbility`'s own `ArrivalMark`. A probe that types either number in
        /// is photographing a picture the game never draws, which is the same fault the class note
        /// records against binding the pet at scale 1.
        ///
        /// ⚠️ `EnsureBuilt`, BECAUSE AN EDIT-MODE CAPTURE GETS NO `Awake`. See that method: the
        /// component would otherwise come back with no geometry at all and the frame would be an
        /// empty road that the run reports as a success.
        ///
        /// ⚠️ NO OWNER, WHICH `GroundReticle.OwnerIsBeingDriven` READS AS "DRAW IT". The held ring
        /// is private to the player aiming it in a match, and a probe has no player; the null
        /// fallback is written down there for exactly this call.
        /// </summary>
        private static GameObject HeldBlinkReticle()
        {
            var go = new GameObject("~ProbeBlinkReticle");
            var reticle = go.AddComponent<GroundReticle>();
            reticle.EnsureBuilt();

            var kit = new PhaisterHeroKit();
            float radius = kit.Skill2 != null ? kit.Skill2.TelegraphRadius : 1.15f;

            reticle.SetBeacon(true);
            reticle.Show(Vector3.zero, radius, UI.UiTheme.BrightForHero(kit.HeroId));

            return go;
        }

        private static GameObject KuroUnbound(bool withPet)
        {
            var maw = HeroHazards.SpawnKuroUnbound(Vector3.zero, 2.8f, 60.0f, 3, withPet);
            if (!withPet) return maw;

            var model = AssetDatabase.LoadAssetAtPath<GameObject>(PetModelPath);
            if (model == null)
            {
                Debug.LogWarning($"[AbilityShowcaseProbe] no pet model at {PetModelPath}; " +
                                 "the Kuro Unbound frame will be the fallback composition.");
                return maw;
            }

            var pet = Object.Instantiate(model);
            pet.name = "~ProbeKuro";
            pet.transform.position = Vector3.zero;

            // ⚠️⚠️ `PersonScale`, NOT 1. `CharacterVisual` instantiates the pet at
            // `Vector3.one * PersonScale` and binds it at the same number, because the voxel
            // model is authored in centimetres. Binding at 1 here made the capture 2.38 times
            // too small and `ability_kuro_unbound_eye_v25.png` came back with a thumb-sized
            // Kuro sitting in a 2.8 m maw, which reads as the ultimate being broken rather than
            // as the probe being wrong. **A probe that does not set up what the game sets up
            // measures something the game never renders.**
            pet.transform.localScale = Vector3.one * Visual.CharacterVisual.PersonScale;

            var companion = pet.AddComponent<Visual.GhostPetCompanion>();

            // ⚠️ BOUND TO NOTHING, WHICH IS FINE AND IS WHY `Bind` TAKES A NULLABLE TARGET. The
            // follow behaviour never runs: `Devour` takes the transform over for its whole life
            // and this frame is inside that life.
            companion.Bind(null, Vector3.zero, Visual.CharacterVisual.PersonScale);
            companion.Devour(60.0f);

            // ⚠️⚠️ WOUND EXPLICITLY, BECAUSE `Solo` DOES NOT WIND ANYTHING. Only `Transient` calls
            // `VfxTimeline.StepAll`, and that is correct for it: the persistent zones `Solo`
            // photographs are built at full size by their spawners and have nothing to step.
            // The pet is the one thing in this probe that is BOTH persistent and animated, so
            // `ability_kuro_unbound_eye_v26.png` came back with an ordinary lavender pet at bind
            // scale sitting in the maw: `Devour` only arms the swell, and `LateUpdate` never runs
            // here to play it.
            //
            // ⚠️ HALF THE LIFE, WHICH IS THE HOLD. The swell finishes early by design (see
            // `StepDevour`), so any moment past the first second is full transformation: grown,
            // horned, darkened, mouth open. 30 of 60 is unambiguously inside it.
            companion.StepTo(30.0f);

            // Parent it to the maw so `Solo`'s sweep collects both as one effect.
            pet.transform.SetParent(maw.transform, worldPositionStays: true);
            return maw;
        }

        /// <summary>One effect, one overhead frame and one at eye height.</summary>
        private static void Solo(System.Collections.Generic.List<GameObject> live,
                                 string name, System.Func<GameObject> make)
        {
            Clear(live);

            var go = make();
            if (go != null) live.Add(go);

            // ⚠️⚠️ PERSISTENT ZONES ARE WOUND TOO NOW, AND THE NOTE THAT SAID THEY NEED NOT BE
            // WAS TRUE WHEN IT WAS WRITTEN AND IS NOT ANY MORE. It read: *"the persistent zones
            // `Solo` photographs are built at full size by their spawners and have nothing to
            // step"*. `HeroHazards.CovenCircleBuild` broke that on 2026-08-27: it creates every
            // ring at `localScale` zero and grows them in sequence, because 🧑 asked to *"see the
            // stages of the giant magic circle being cast"*. Photographed unwound it is
            // completely invisible, and `ability_coven_eclipse_eye_v32.png` is that frame: a
            // correct, finished effect that looks like nothing was built at all.
            //
            // ⚠️ `EclipseFall` HAS THE SAME PROBLEM AND HAS HAD IT ALL ALONG. It is an
            // `IVfxTimeline` that lowers the eclipse from the sky over 0.75 s, so every capture
            // of that ability before today was taken on its birth frame with the moon still
            // eleven metres up and out of shot.
            //
            // ⚠️ IT IS A NO-OP FOR ANYTHING WITH NOTHING TO STEP, which is every other zone here,
            // so this is strictly more correct rather than a behaviour change for them.
            Visual.VfxTimeline.StepAll(0.35f);

            // ⚠️ THE OVERHEAD FRAME IS THE ONE THAT SHOWS FOOTPRINT AND THE EYE FRAME IS THE ONE
            // THAT SHOWS WHETHER IT READS. Both are needed and they disagree constantly: a disc
            // that is obviously 2.3 m from above is two pixels tall from a metre away, which is
            // the asymmetry `Visual.DamageVignette` exists to answer.
            Shot(name, new Vector3(0.0f, 4.6f, -3.4f), Quaternion.Euler(46.0f, 0.0f, 0.0f), 55.0f);
            Shot(name + "_eye", new Vector3(0.0f, 1.65f, -4.6f), Quaternion.Euler(6.0f, 0.0f, 0.0f), 70.0f);
        }

        /// <summary>
        /// One transient effect, wound to the moment worth looking at, then swept up.
        ///
        /// ⚠️⚠️ IT SWEEPS BY DIFFING THE SCENE ROOTS, NOT BY COLLECTING RETURN VALUES. A blast
        /// is not one object: `CreateExplosionVisual` alone creates a core, a ground wave, up to
        /// 22 debris cubes, a light and a popup, and `CreateThunderstrike` adds three lightning
        /// columns and twelve sparks. None of them are returned, and `Object.Destroy(go, t)`
        /// never comes due in edit mode, so anything not swept here would survive into the NEXT
        /// capture and quietly appear in a frame that is supposed to show one ability.
        ///
        /// ⚠️ AT 0.35 OF EACH EFFECT'S OWN LIFE. `VfxTimeline.StepAll`'s note has the reasoning:
        /// a fraction rather than a time, because a core runs 0.5 s and its wave 0.4 s and
        /// asking both for the same number of seconds photographs them at different moments of
        /// the same event. 0.35 is past the birth frame, before the fade takes the alpha, and it
        /// is where a silhouette is at its most legible.
        /// </summary>
        private static void Transient(string name, System.Action make)
        {
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            var before = new System.Collections.Generic.HashSet<GameObject>(scene.GetRootGameObjects());

            make();

            int stepped = Visual.VfxTimeline.StepAll(0.35f);

            Shot(name, new Vector3(0.0f, 4.6f, -6.2f), Quaternion.Euler(30.0f, 0.0f, 0.0f), 62.0f);
            Shot(name + "_eye", new Vector3(0.0f, 1.65f, -7.4f), Quaternion.Euler(6.0f, 0.0f, 0.0f), 72.0f);

            Debug.Log($"[AbilityShowcaseProbe] {name}: stepped {stepped} timed effect(s).");

            foreach (var go in scene.GetRootGameObjects())
                if (!before.Contains(go)) Object.DestroyImmediate(go);
        }

        /// <summary>
        /// What share of a frame is at or above <see cref="BlownLevel"/>.
        ///
        /// ⚠️ LUMINANCE, NOT THE MAX CHANNEL. A saturated blue at full strength is a colour;
        /// white is an absence of picture. Rec. 601 weights are what separates the two, and the
        /// whole point of this measurement is to catch the second without punishing the first.
        /// </summary>
        private static float BlownFraction(Texture2D tex)
        {
            var pixels = tex.GetPixels32();
            if (pixels.Length == 0) return 0.0f;

            int over = 0;
            foreach (var p in pixels)
            {
                int luma = (p.r * 299 + p.g * 587 + p.b * 114) / 1000;
                if (luma >= BlownLevel) over++;
            }

            return over / (float)pixels.Length;
        }

        private static void Clear(System.Collections.Generic.List<GameObject> live)
        {
            foreach (var go in live)
                if (go != null) Object.DestroyImmediate(go);

            live.Clear();
        }

        private static void Shot(string name, Vector3 pos, Quaternion rot, float fov)
        {
            var camGo = new GameObject("~AbilityShowcaseCam");
            camGo.transform.position = pos;
            camGo.transform.rotation = rot;

            var cam = camGo.AddComponent<Camera>();
            cam.fieldOfView = fov;
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 260.0f;
            cam.clearFlags = CameraClearFlags.Skybox;

            // Adopts the loaded map's grade rather than a literal, for the reason written up on
            // `IlalimNgTulayShowcaseProbe.Shot`: hard-coding one there is what let a map that
            // rendered black pass eight rounds of showcase review.
            camGo.AddComponent<ColourGrade>().AdoptFromScene();

            var rt = new RenderTexture(ShotWidth, ShotHeight, 24, RenderTextureFormat.ARGB32)
            {
                antiAliasing = 4,
            };

            cam.targetTexture = rt;
            cam.Render();

            RenderTexture.active = rt;
            var tex = new Texture2D(ShotWidth, ShotHeight, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, ShotWidth, ShotHeight), 0, 0);
            tex.Apply();

            RenderTexture.active = null;
            cam.targetTexture = null;

            float blown = BlownFraction(tex);

            string path = Path.Combine(OutDir, $"ability_{name}_{Version}.png");
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Debug.Log($"[AbilityShowcaseProbe] {path} ({new FileInfo(path).Length / 1024} KB, " +
                      $"{blown * 100.0f:F1}% blown)");

            if (_gateBlowout && blown > MaxBlownFraction)
            {
                Blown.Add($"{name}: {blown * 100.0f:F1}% of the frame is at or above " +
                          $"{BlownLevel}/255, over the {MaxBlownFraction * 100.0f:F0}% bound");
            }

            Object.DestroyImmediate(tex);
            rt.Release();
            Object.DestroyImmediate(rt);
            Object.DestroyImmediate(camGo);
        }
    }
}
