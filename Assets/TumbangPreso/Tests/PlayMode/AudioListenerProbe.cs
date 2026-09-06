using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace TumbangPreso.PlayTests
{
    /// <summary>
    /// ⚠️⚠️ THE GAME'S ONLY `AudioListener` SAT AT WORLD ORIGIN FOR THE WHOLE PORT, SO EVERY 3D
    /// CUE WAS PANNED FROM THE MIDDLE OF THE MAP RATHER THAN FROM THE PLAYER.
    ///
    /// `AudioDirector.Awake` added the listener to `~GameServices`, which is created at the
    /// origin and never moved, parented or rotated, while every pooled voice is
    /// `spatialBlend = 1.0` with a 2 m to 32 m linear rolloff. `docs/TODO.md` § 150.7 carries the
    /// arithmetic; the short version is that attenuation ran 1.00 at the centre of the box to
    /// 0.74 at a corner, which is modest, and **panning was anchored to the WORLD**, which is
    /// not: a player at `(-5, 0, 0)` heard a slipper land at `(+5, 0, 0)`, ten metres directly in
    /// FRONT of them, panned hard RIGHT, because that is world `+X` of the origin. Front and back
    /// did not exist at all.
    ///
    /// `docs/VISION.md` § 0 is why that is a gameplay defect and not a mix note: *"the tension is
    /// the retrieval, not the throw"*, and hearing which side the taya is closing from is part of
    /// the read a player makes on the run back in for their tsinelas.
    ///
    /// ⚠️⚠️ **THE PARKED-VOICE CASE IS THE ONE THAT PROVES THE IMPLEMENTATION AND NOT JUST THE
    /// INTENT.** `TakeVoice` parents every pooled voice to the `AudioDirector`'s own transform,
    /// so the obvious fix — move the object the listener is already on — would have dragged every
    /// ringing one-shot along with the player's head. A slipper landing behind you would have
    /// followed you and never fallen behind you at all, which is a worse bug than the one being
    /// fixed and would have measured as "the audio feels wrong" rather than as anything findable.
    /// The listener is on its own child for that reason and
    /// <see cref="AParkedOneShotDoesNotTravelWithTheEars"/> is what stops anybody undoing it.
    /// </summary>
    public class AudioListenerProbe
    {
        /// <summary>
        /// The pair that makes a full-suite result mean anything. `docs/TODO.md` § 126.8 and
        /// `PlayModeWorld.Reset`.
        /// </summary>
        [UnitySetUp]
        public IEnumerator ResetWorldBefore() => PlayModeWorld.Reset();

        [UnityTearDown]
        public IEnumerator ResetWorldAfter() => PlayModeWorld.Reset();

        /// <summary>
        /// A metre of slack. The ears copy the camera pose exactly, so this is measuring "did the
        /// follow run at all" rather than a tolerance on the copy; it is loose on purpose so a
        /// legitimate half-frame of camera smoothing can never make it flake.
        /// </summary>
        private const float PoseTolerance = 0.05f;

        private static AudioListener Ears()
        {
            var director = GameServices.Audio;
            Assert.IsNotNull(director, "no AudioDirector: GameServices never built");

            // ⚠️ FOUND THROUGH THE DIRECTOR'S OWN TRANSFORM, NOT THROUGH `FindObjectsByType`.
            // `~GameServices` is `HideAndDontSave` and a search never returns it; the ears are an
            // ordinary child of it, and reaching them through the parent is the only way that
            // does not depend on which of those two facts is true this week.
            var ears = director.GetComponentInChildren<AudioListener>(true);
            Assert.IsNotNull(ears, "the AudioDirector owns no AudioListener at all");
            return ears;
        }

        /// <summary>
        /// The pooled 3D voice closest to a point, or null if the director owns none.
        ///
        /// ⚠️⚠️ FOUND BY SHAPE AND NOT BY NAME, AND THE FIRST VERSION OF THIS FIXTURE FAILED FOR
        /// EXACTLY THAT REASON. It reached for a child called `Voice0` and got the ANNOUNCER's:
        /// `Audio.VoiceDirector` is a second component on the same `~GameServices` object, so it
        /// shares this transform, and its `BuildVoices` created two 2D sources parked at the
        /// origin under the identical names. Both assertions then failed describing the fix as
        /// broken when it was not, which is the harness accusing the game of its own mistake
        /// (`docs/TODO.md` § 150.3). `AudioDirector`'s pool is `WorldVoice*` now, so the names no
        /// longer collide, **and this still does not use the name**: a probe that can only pass
        /// while one string agrees with another string is the § 124.11 fault with extra steps.
        /// </summary>
        private static Transform NearestWorldVoice(Vector3 to)
        {
            Transform best = null;
            float bestDistance = float.MaxValue;

            foreach (var source in GameServices.Audio.GetComponentsInChildren<AudioSource>(true))
            {
                if (source.spatialBlend < 0.5f) continue;

                float d = Vector3.Distance(source.transform.position, to);
                if (d >= bestDistance) continue;

                bestDistance = d;
                best = source.transform;
            }

            return best;
        }

        private static IEnumerator LoadTheStreet()
        {
            var load = SceneManager.LoadSceneAsync("Eskinita", LoadSceneMode.Single);
            yield return ProbeWait.Done(load, "scene load");

            for (int i = 0; i < 20; i++) yield return null;
        }

        /// <summary>
        /// ⚠️ THE CLAIM IS "AT THE CAMERA", NOT "NOT AT THE ORIGIN". A camera that happened to be
        /// near the origin would pass the weaker version while the bug was fully intact.
        /// </summary>
        [UnityTest]
        public IEnumerator TheListenerRidesTheCameraRatherThanSittingAtWorldOrigin()
        {
            yield return LoadTheStreet();

            var head = Camera.main;
            Assert.IsNotNull(head, "the arena has no camera tagged MainCamera");

            // Somewhere unambiguous: far enough from the origin that the old behaviour cannot
            // pass by coincidence, and turned, so the ROTATION half is measured too. A listener
            // with the right position and the wrong facing still pans every cue wrongly.
            head.transform.SetPositionAndRotation(new Vector3(-5.0f, 1.6f, 3.0f),
                                                  Quaternion.Euler(0.0f, 137.0f, 0.0f));
            yield return null;
            yield return null;

            var ears = Ears();

            Assert.Less(Vector3.Distance(ears.transform.position, head.transform.position),
                        PoseTolerance,
                        $"the listener is at {ears.transform.position} and the camera is at " +
                        $"{head.transform.position}. Every 3D cue is panning from there.");

            Assert.Less(Quaternion.Angle(ears.transform.rotation, head.transform.rotation), 1.0f,
                        "the listener is at the camera but is not facing where it faces, so " +
                        "left and right are still wrong.");
        }

        /// <summary>
        /// ⚠️⚠️ THE ONE THAT PROVES THE EARS ARE NOT THE VOICES' PARENT. See the class header:
        /// this is the bug the straightforward fix would have shipped.
        /// </summary>
        [UnityTest]
        public IEnumerator AParkedOneShotDoesNotTravelWithTheEars()
        {
            yield return LoadTheStreet();

            var head = Camera.main;
            Assert.IsNotNull(head, "the arena has no camera tagged MainCamera");

            head.transform.position = new Vector3(-6.0f, 1.6f, 0.0f);
            yield return null;

            var landed = new Vector3(6.0f, 0.0f, 0.0f);
            GameServices.Audio.PlayAt("land", landed);

            var voice = NearestWorldVoice(landed);
            Assert.IsNotNull(voice, "no pooled 3D voice exists at all, so PlayAt created nothing");
            Assert.Less(Vector3.Distance(voice.position, landed), PoseTolerance,
                        $"the nearest 3D voice is at {voice.position} and the cue was fired at " +
                        $"{landed}, so PlayAt is not parking its voice where it was told to.");

            // Walk the whole width of the box. A voice that is a child of the ears follows.
            head.transform.position = new Vector3(6.0f, 1.6f, 6.0f);
            yield return null;
            yield return null;

            Assert.Less(Vector3.Distance(voice.position, landed), PoseTolerance,
                        $"a ringing one-shot moved from {landed} to {voice.position} when the " +
                        "camera did. The listener is parented over the voice pool, so every " +
                        "sound in the game now follows the player instead of staying where it " +
                        "happened.");
        }

        /// <summary>
        /// The listener has to follow whatever is the local view NOW, and this game changes that
        /// object rather than moving one: `MatchInstaller` tags the gameplay camera, the
        /// spectator camera and the watch camera `MainCamera` in turn, and
        /// `DebugPlayerSwitcher`'s header records the consequence from the other side
        /// (*"`Camera.main` IS THE SPECTATOR'S OWN OBJECT WHENEVER ONE IS UP"*).
        ///
        /// ⚠️ SO THIS CASE STANDS IN FOR ALL OF THEM: a role change, a possession, a seat
        /// handover, a spectator window and a rematch are the same event to the ears, which is a
        /// camera object being replaced. A fix that cached a `CameraRig` reference at `Awake`
        /// passes every other case here and fails this one.
        /// </summary>
        [UnityTest]
        public IEnumerator TheListenerMovesToTheNextCameraWhenTheLocalViewIsReplaced()
        {
            yield return LoadTheStreet();

            var first = Camera.main;
            Assert.IsNotNull(first, "the arena has no camera tagged MainCamera");

            var secondGo = new GameObject("StandInSpectatorCamera");
            secondGo.tag = "MainCamera";
            secondGo.transform.SetPositionAndRotation(new Vector3(4.0f, 8.0f, -9.0f),
                                                      Quaternion.Euler(35.0f, 0.0f, 0.0f));
            secondGo.AddComponent<Camera>();

            // The old view goes away, exactly as it does when a spectator window opens.
            first.gameObject.SetActive(false);
            yield return null;
            yield return null;

            var ears = Ears();

            Assert.Less(Vector3.Distance(ears.transform.position, secondGo.transform.position),
                        PoseTolerance,
                        $"the view moved to {secondGo.transform.position} and the ears stayed " +
                        $"at {ears.transform.position}.");

            Object.Destroy(secondGo);
        }

        /// <summary>
        /// ⚠️⚠️ THE MANDATORY SECOND HALF OF § 150.7. Moving the listener breaks every UI cue in
        /// the game on the same day unless the non-diegetic ones get an explicit 2D route: seven
        /// call sites fired a cue at `Vector3.zero` and four more fired at `Camera.main`'s own
        /// transform, and both tricks only ever worked because the listener could not move.
        ///
        /// The two routes are asserted from opposite sides in one case on purpose. A "fix" that
        /// made everything 2D would silence the whole world and pass a test that only checked the
        /// UI half.
        /// </summary>
        [UnityTest]
        public IEnumerator TheUiRouteIsFlatAndTheWorldRouteIsNot()
        {
            yield return LoadTheStreet();

            var at = new Vector3(3.0f, 0.0f, 3.0f);
            GameServices.Audio.PlayAt("land", at);
            GameServices.Audio.PlayUi("ui_click");
            yield return null;

            var world = NearestWorldVoice(at);
            var ui = GameServices.Audio.transform.Find("UiVoice0");

            Assert.IsNotNull(world,
                "PlayAt created no 3D voice at all, so the world route stopped being positional: " +
                "the whole arena would arrive centred at full volume.");
            Assert.Less(Vector3.Distance(world.position, at), PoseTolerance,
                        "the world route fired but parked its voice somewhere else.");

            Assert.IsNotNull(ui, "PlayUi created no UI voice: there is no non-diegetic route");
            Assert.AreEqual(0.0f, ui.GetComponent<AudioSource>().spatialBlend, 0.001f,
                            "the UI route is 3D, so a menu click pans as the player walks.");
        }

        /// <summary>
        /// ⚠️ ONE LISTENER, STILL. `AudioDirector.KeepOneListener`'s guard changed meaning on
        /// 2026-09-06: the ears used to live on the `HideAndDontSave` root, where the search
        /// could not reach them, and they are an ordinary child now, where it can. Without the
        /// `listener == mine` line the first scene load disables the game's only listener and
        /// **the whole game goes silent with no error and no warning**, which is the exact
        /// failure `AudioDirector.Awake`'s own header was written about.
        /// </summary>
        [UnityTest]
        public IEnumerator ExactlyOneListenerIsEnabledAfterASceneLoad()
        {
            yield return LoadTheStreet();

            var ears = Ears();
            Assert.IsTrue(ears.enabled,
                          "the services listener was disabled by its own duplicate sweep, so " +
                          "the game is silent.");

            int enabled = 0;
            foreach (var listener in Object.FindObjectsByType<AudioListener>(
                         FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (listener.enabled && listener != ears) enabled++;

            Assert.AreEqual(0, enabled,
                            $"{enabled} scene listeners are still enabled beside the services " +
                            "one. Unity's answer to two listeners is a per-frame warning and " +
                            "undefined behaviour about which one hears.");
        }
    }
}
