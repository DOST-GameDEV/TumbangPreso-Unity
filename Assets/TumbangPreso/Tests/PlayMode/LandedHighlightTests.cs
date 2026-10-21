using System.Collections;
using NUnit.Framework;
using TumbangPreso.Core;
using TumbangPreso.Settings;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace TumbangPreso.PlayTests
{
    /// <summary>
    /// § THE LANDED HIGHLIGHT actually lights, in the colour the player chose.
    ///
    /// ⚠️⚠️ THIS TEST EXISTS BECAUSE "IT COMPILES AND THE TESTS ARE GREEN" DID NOT MEAN THE
    /// FEATURE WAS VISIBLE. Everything about this highlight is a write into a
    /// MaterialPropertyBlock, and every way of getting that wrong is silent: writing a property
    /// the shader does not carry, writing the shared material instead of the block, or never
    /// reaching the call at all. None of those fail a compile and none of them fail any other
    /// test in this project. The only honest check is to fly a slipper, land it, and read the
    /// block back off the renderer.
    ///
    /// ⚠️ IT READS THE BLOCK, NOT THE MATERIAL. `ToonSkin` caches and SHARES one material per
    /// skin across every slipper wearing it, so asserting on the material would pass for a
    /// change that lights all four at once, which is the bug rather than the feature.
    /// </summary>
    public class LandedHighlightTests
    {
        /// <summary>
        /// ⚠️⚠️ THE PAIR THAT MAKES A FULL-SUITE RESULT MEAN ANYTHING. `docs/TODO.md` § 126.8:
        /// the full PlayMode run came back 42, 41 and then 56 red with the red set moving, and a
        /// gate whose red set moves is not measuring the code. `PlayModeWorld.Reset` has the
        /// mechanism and why BOTH hooks are needed rather than one.
        /// </summary>
        [UnitySetUp]
        public IEnumerator ResetWorldBefore() => PlayModeWorld.Reset();

        [UnityTearDown]
        public IEnumerator ResetWorldAfter() => PlayModeWorld.Reset();

        private static readonly int RimStrengthId = Shader.PropertyToID("_RimStrength");
        private static readonly int RimColorId = Shader.PropertyToID("_RimColor");

        /// <summary>
        /// THE LOCAL PLAYER'S OWN TSINELAS, and both tests below take it rather than the first
        /// one the scene hands back.
        ///
        /// ⚠️⚠️ THAT CHANGED ON 2026-09-19 AND IT IS THE FEATURE CHANGING, NOT THE TEST BEING
        /// MADE TO PASS. 🧑: *"other player's tsinelas does not get highlighted now. only
        /// yours."* `Slipper.RefreshHighlight` gates the landed rim on the owner glow, so the
        /// subject of this test is now a specific slipper rather than any of the four, and
        /// `FindFirstObjectByType` is explicitly unsorted: on this machine it usually handed back
        /// seat 0's, and `GameLaunch.SoloSeat` is 1.
        ///
        /// ⚠️ IT RESOLVES THE SEAT THROUGH `PlayerInputReader` RATHER THAN CALLING
        /// `SetOwnerGlow(true)` BY HAND. `MatchInstaller.HumanSeat` is what gives exactly one
        /// body that component, and `MatchInstaller.BuildSlipper` lights the glow from the same
        /// answer, so asking this way drives the real wiring. A test that set the flag itself
        /// would keep passing after that wiring broke, which is the one failure that would make
        /// every landed rim in the game go dark at once.
        /// </summary>
        private static Slipper LocalSlipper(out int seat)
        {
            seat = -1;

            foreach (var motor in Object.FindObjectsByType<CharacterMotor>(FindObjectsSortMode.None))
            {
                if (motor.GetComponent<PlayerInputReader>() == null) continue;
                seat = motor.PlayerSlot;
                break;
            }

            if (seat < 0) return null;

            foreach (var s in Object.FindObjectsByType<Slipper>(FindObjectsSortMode.None))
                if (s.OwnerSlot == seat) return s;

            return null;
        }

        /// <summary>
        /// The MODEL's renderer, skipping anything parented to the slipper as an effect.
        ///
        /// ⚠️⚠️ THIS TEST READ `GetComponentInChildren&lt;Renderer&gt;()` AND THAT STOPPED BEING
        /// SAFE THE DAY § THE RECALL BEAM LANDED. The beam is several renderers parented to the
        /// tsinelas, and `Slipper.RefreshHighlight` deliberately does not write rim properties
        /// into an effect, so an unlucky pick reads a block with no rim in it and reports the
        /// feature dark while it is working perfectly. `VfxRenderTag`'s own note is the rule and
        /// `InputEdgeTests.EverySlipperAndTheLataWearTheToonOutline` already applies it: *"an
        /// effect parented to a prop is not part of the prop"*, and asking that question rather
        /// than keeping a list is what exempts the next effect somebody parents here.
        ///
        /// ⚠️ IT NAMES WHAT IT LOOKED AT, because "the slipper has no renderer" and "every
        /// renderer it has is an effect" are the same red with completely different causes.
        /// </summary>
        private static Renderer ModelRenderer(Slipper slipper, out string looked)
        {
            var names = new System.Text.StringBuilder();
            Renderer model = null;

            foreach (var r in slipper.GetComponentsInChildren<Renderer>(includeInactive: true))
            {
                bool effect = r.GetComponent<Visual.VfxRenderTag>() != null;
                names.Append(names.Length > 0 ? ", " : "").Append(r.name)
                     .Append(effect ? " (effect)" : " (model)");

                if (!effect && model == null) model = r;
            }

            looked = names.Length > 0 ? names.ToString() : "none at all";
            return model;
        }

        [UnityTest]
        public IEnumerator ALandedTsinelasLightsInTheChosenColour()
        {
            var load = SceneManager.LoadSceneAsync("Eskinita", LoadSceneMode.Single);
            yield return ProbeWait.Done(load, "scene load");

            for (int i = 0; i < 20; i++) yield return null;

            // Blue, the shipped default. Set explicitly so a stale settings.json on whatever
            // machine runs this cannot turn the feature off and pass the test by accident.
            //
            // ⚠⚠ AND THE CHANGE IS RAISED, NOT ONLY WRITTEN. Setting the field alone leaves
            // every listener on whatever it last cached, so if anything earlier in the suite
            // turned the highlight OFF and raised it, this test ran against an off feature and
            // failed with "not lit at the landed rim strength" while the feature was working
            // perfectly. It passed in isolation and failed in a full run, which is the signature
            // of exactly this.
            SettingsStore.Current.SlipperHighlight = SlipperHighlights.Default;
            SettingsStore.RaiseSlipperHighlightChanged();

            var slipper = LocalSlipper(out int localSeat);
            Assert.IsNotNull(slipper,
                $"the match built no tsinelas owned by the local seat ({localSeat}) to test");

            var renderer = ModelRenderer(slipper, out string looked);
            Assert.IsNotNull(renderer,
                $"the slipper has no model renderer, so nothing can light. Looked at: {looked}");

            var lata = Object.FindFirstObjectByType<Lata>();
            Assert.IsNotNull(lata, "the arena has no lata to measure the floor from");

            Vector3 target = lata.transform.position + new Vector3(1.5f, 0.0f, 2.0f);
            slipper.HostThrow(null, target + Vector3.up * 1.5f, Vector3.zero);
            Assert.AreEqual(SlipperState.InFlight, slipper.State, "the throw did not take");

            // 5 m of fall is well under a second; 400 frames is a generous cap that still fails
            // rather than hanging if the landing never happens.
            float t0 = Time.time;
            for (int i = 0; i < 400 && slipper.State != SlipperState.Loose; i++)
                yield return new WaitForFixedUpdate();

            Assert.AreEqual(SlipperState.Loose, slipper.State,
                $"the slipper never landed. timeScale={Time.timeScale} " +
                $"elapsed={Time.time - t0:F2}s y={slipper.transform.position.y:F2}");

            var block = new MaterialPropertyBlock();
            renderer.GetPropertyBlock(block);

            Assert.AreEqual(Balance.LandedRimStrength, block.GetFloat(RimStrengthId), 0.001f,
                            $"a landed tsinelas is not lit at the landed rim strength. " +
                            $"seat={localSeat} read '{renderer.name}' out of: {looked}");

            Color want = SlipperHighlights.ColourOf(SlipperHighlights.Default);
            Color got = block.GetColor(RimColorId);

            Assert.AreEqual(want.r, got.r, 0.01f, "rim red");
            Assert.AreEqual(want.g, got.g, 0.01f, "rim green");
            Assert.AreEqual(want.b, got.b, 0.01f, "rim blue");
        }

        /// <summary>
        /// ⚠️ OFF MEANS OFF, AND IT TAKES EFFECT ON SLIPPERS ALREADY LYING THERE. The panel is
        /// reachable from the in-match pause menu, so a setting that only applied to the NEXT
        /// landing would read as the control being broken. This is the live-repaint path.
        /// </summary>
        [UnityTest]
        public IEnumerator TurningTheHighlightOffUnlightsASlipperAlreadyResting()
        {
            var load = SceneManager.LoadSceneAsync("Eskinita", LoadSceneMode.Single);
            yield return ProbeWait.Done(load, "scene load");

            for (int i = 0; i < 20; i++) yield return null;

            // Raised as well as written, for the reason the test above records.
            SettingsStore.Current.SlipperHighlight = SlipperHighlights.Default;
            SettingsStore.RaiseSlipperHighlightChanged();

            var slipper = LocalSlipper(out int localSeat);
            Assert.IsNotNull(slipper,
                $"the match built no tsinelas owned by the local seat ({localSeat}) to test");

            var renderer = ModelRenderer(slipper, out string looked);
            Assert.IsNotNull(renderer,
                $"the slipper has no model renderer, so nothing can light. Looked at: {looked}");

            var lata = Object.FindFirstObjectByType<Lata>();
            Assert.IsNotNull(lata, "the arena has no lata to measure the floor from");

            Vector3 target = lata.transform.position + new Vector3(1.5f, 0.0f, 2.0f);
            slipper.HostThrow(null, target + Vector3.up * 1.5f, Vector3.zero);

            Assert.AreEqual(SlipperState.InFlight, slipper.State, "the throw did not take");

            for (int i = 0; i < 400 && slipper.State != SlipperState.Loose; i++)
                yield return new WaitForFixedUpdate();
            Assert.AreEqual(SlipperState.Loose, slipper.State,
                $"the slipper never landed. timeScale={Time.timeScale} y={slipper.transform.position.y:F2}");

            var block = new MaterialPropertyBlock();
            renderer.GetPropertyBlock(block);
            Assert.Greater(block.GetFloat(RimStrengthId), 0.0f,
                $"it was not lit to begin with. state={slipper.State} " +
                $"pos={slipper.transform.position} setting={SettingsStore.Current.SlipperHighlight} " +
                $"seat={localSeat} read '{renderer.name}' out of: {looked}");

            // What the settings row does when the player cycles to Off.
            SettingsStore.Current.SlipperHighlight = SlipperHighlights.Off;
            SettingsStore.RaiseSlipperHighlightChanged();

            yield return null;

            block = new MaterialPropertyBlock();
            renderer.GetPropertyBlock(block);

            // ⚠️ THE OWNER GLOW MAY LEGITIMATELY STILL BE LIT. This slipper belongs to a seat,
            // and if that seat is the local player its gold "this one is yours" rim survives the
            // landed highlight going out, by design. The assertion is therefore that it is no
            // longer lit in the HIGHLIGHT's colour, not that it is dark.
            Color off = block.GetColor(RimColorId);
            Color blue = SlipperHighlights.ColourOf(SlipperHighlights.Default);

            bool stillBlue = Mathf.Abs(off.r - blue.r) < 0.01f
                             && Mathf.Abs(off.g - blue.g) < 0.01f
                             && Mathf.Abs(off.b - blue.b) < 0.01f;

            Assert.IsFalse(stillBlue, "Off left the slipper lit in the highlight colour");
        }
    }
}
