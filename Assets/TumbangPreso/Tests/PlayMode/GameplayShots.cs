using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace TumbangPreso.PlayTests
{
    /// <summary>
    /// Photographs a LIVE ROUND rather than the frame it spawns on.
    ///
    /// ⚠️⚠️ `UiRuntimeShots.Arena` CAPTURES TWELVE FRAMES AFTER LOAD, WHICH IS BEFORE ANYTHING
    /// HAPPENS. The round has not begun, no bot has chosen a plan, nobody is carrying anything
    /// and no verb has been thrown. Every report about how the game LOOKS in play — bots
    /// standing still, hands floating, the red taya screen, the charge ring — is about a state
    /// that shot has never contained.
    ///
    /// So this one begins the round, lets it run, and photographs it from the player's own eyes
    /// AND from a witness camera parked on the cast, because a first-person view cannot show
    /// what the first-person body looks like.
    /// </summary>
    public class GameplayShots
    {
        private const string OutDir = "Logs/shots-play";
        private const int Width = 1920;
        private const int Height = 1080;

        /// <summary>Unity's built-in UI layer. The shot's UI camera is culled to this and to
        /// nothing else. See the note where it is built.</summary>
        private const int UiLayer = 5;

        /// <summary>
        /// Moves a canvas and everything under it onto <paramref name="layer"/>, remembering
        /// what each transform had so <see cref="RestoreLayers"/> can put it back.
        ///
        /// ⚠️ THE WHOLE SUBTREE, NOT THE ROOT. Unity culls per RENDERER, and a canvas whose root
        /// moved while its children did not renders as an empty rectangle.
        /// </summary>
        private static void SetLayer(Transform t,
            int layer, System.Collections.Generic.Dictionary<Transform, int> saved)
        {
            if (t == null) return;

            if (!saved.ContainsKey(t)) saved[t] = t.gameObject.layer;
            t.gameObject.layer = layer;

            foreach (Transform child in t) SetLayer(child, layer, saved);
        }

        private static void RestoreLayers(
            System.Collections.Generic.Dictionary<Transform, int> saved)
        {
            foreach (var pair in saved)
                if (pair.Key != null) pair.Key.gameObject.layer = pair.Value;

            saved.Clear();
        }

        [UnityTest]
        public IEnumerator ALiveRoundIsPhotographed()
        {
            Directory.CreateDirectory(OutDir);

            var load = SceneManager.LoadSceneAsync("Eskinita", LoadSceneMode.Single);
            yield return ProbeWait.Done(load, "scene load");

            for (int i = 0; i < 20; i++) yield return null;

            var round = GameServices.Round;
            Assert.IsNotNull(round, "The arena registered no round.");

            // The free-roam window, which is the state pic 3 was taken in.
            yield return Witness("freeroam");

            // ⚠️ THE DIRECT ANALOGUE OF `Logs/shots-godot/g04-ready.png`: same phase, same
            // camera, nothing held. It is the only frame in either build where the two
            // first-person arms can be compared against each other at rest.
            yield return Eyes("ready-eyes");

            // ⚠️⚠️ THROUGH `SliceRunner.Begin`, NOT `RoundDirector.BeginRound`. This called the
            // director straight and that is why no capture in this suite has ever shown an
            // attacker holding anything: `BeginRound` only flips the round flags, while
            // everything that PLACES the world — the marks, the facing, the can, and the
            // round-start slipper equip — hangs off `MatchDirector.RoundStarted`, which only
            // the runner raises. Every shot of "the arms" was therefore a shot of an attacker
            // who had been given no tsinelas, and the empty hand read as a viewmodel bug.
            //
            // It is also the path a player takes (the ready gate calls exactly this), so the
            // shot is of the game rather than of the probe.
            var runner = Object.FindFirstObjectByType<SliceRunner>();

            if (runner != null) runner.Begin();
            else round.BeginRound();

            // ⚠️⚠️ AND IT HAS TO BE TAKEN DOWN AGAIN AT THE END OF THIS TEST. `Begin` subscribes
            // the runner to the `DontDestroyOnLoad` directors, and a PlayMode batch does not
            // unload this scene before the next test builds its world in place — so the runner
            // outlives the shot, hears the NEXT test's `RoundStarted`, and runs `ResetWorld`,
            // which teleports every seat back to its spawn mark.
            //
            // Measured: `MatchRunTests.AnAttackerMovesFreelyThroughTheChalk` failed at z 8.909,
            // which is `Confinement.AttackerSpawnRing()` to three decimals — the attacker had
            // not been stopped by anything, it had been picked up and put back. It passes alone
            // and fails in the batch, which is the signature of exactly this. Same leak class as
            // the one `SliceRunner.Subscribe` documents; this end is the harness's.
            //
            // ⚠️ IT IS DESTROYED AT THE BOTTOM OF THIS METHOD RATHER THAN IN A `finally`,
            // because a coroutine may not yield inside a try/catch and this body is nothing but
            // yields. An assertion failure here leaks it again, which is acceptable: the run has
            // already failed and the next thing anybody does is read the log.

            yield return new WaitForSecondsRealtime(3.0f);

            yield return Eyes("round-eyes");
            yield return Witness("round-witness");

            // § 127.3's owed frame. See `RoleMarkers`: `round-witness` above is the shot that
            // could not answer this, from a camera 2.6 m up looking at chest height.
            yield return RoleMarkers("role-markers-v1");

            DumpCarry("Logs/carry-live.txt");
            DumpFrame("Logs/fpp-live.txt");

            yield return new WaitForSecondsRealtime(3.0f);

            yield return Eyes("round-eyes-late");
            yield return Witness("round-witness-late");

            // ---- THE EMOTE SWING ---------------------------------------------------------
            // ⚠️⚠️ NOTHING HAS EVER PHOTOGRAPHED AN EMOTE, WHICH IS HOW *"doing emote doesnt
            // show myself in tpp, i think my body is hidden"* SURVIVED. The swing is the only
            // state in the game where the local player's own body is supposed to be VISIBLE, and
            // the first-person self-hide had no reason to be re-run at the boundary. A shot from
            // the player's own camera is the whole test: if the body is there, the fix landed,
            // and if the street is empty the bug is still live. He asked for this frame by name.
            //
            // ⚠️ IT GOES THROUGH `EmotePlayer.Play`, WHICH IS WHAT THE RIG SUBSCRIBES TO. Poking
            // `BeginEmoteView` on the rig would photograph the camera move and prove nothing
            // about the path a player takes to it.
            var driven = Object.FindFirstObjectByType<TumbangPreso.CameraSystem.CameraRig>()
                               ?.Following;

            if (driven != null)
            {
                // An emote refuses while something is in hand (`EmotePlayer.CanEmote`), which by
                // now is every attacker: they start the round holding their own tsinelas.
                var carry = driven.GetComponent<Carrier>();
                if (carry != null && carry.Held != null) carry.HostThrowAt(
                    driven.transform.position,
                    driven.transform.position + driven.transform.forward * 6.0f, 0.2f);

                // ⚠️⚠️ THE BRAIN HAS TO LET GO FIRST, THE SAME WAY THE CHARGE SHOT BELOW NEEDS IT
                // TO. `EmotePlayer.Update` aborts on ANY movement — that is §3aa, an emote ends
                // only by interruption — and `AIController.Act` writes a move axis every frame.
                // The first version of this shot photographed an emote that had been cancelled on
                // the frame after it started, which looks identical to an emote that never fired.
                var brain = driven.GetComponent<AIController>();
                if (brain != null) brain.enabled = false;

                driven.Intent.Clear();
                driven.Intent.CommitFrame();

                var emotes = driven.GetComponent<Social.EmotePlayer>();

                if (emotes != null)
                {
                    emotes.Play("crouch");

                    yield return new WaitForSecondsRealtime(1.2f);
                    yield return Eyes("emote-eyes");
                    yield return Portrait(driven, "emote-body");

                    emotes.Stop();

                    // And back. A rig stuck in the emote view would silently ruin every shot
                    // after this one, which is exactly the failure mode CLAUDE.md §3aa warns
                    // about for a second restore path.
                    yield return new WaitForSecondsRealtime(0.5f);
                    yield return Eyes("emote-ended-eyes");
                }

                if (brain != null) brain.enabled = true;
            }

            // ---- THE EMOTE WHEEL ---------------------------------------------------------
            // ⚠️ NOTHING HAS EVER PHOTOGRAPHED IT EITHER, WHICH IS HOW IT STAYED THE ONE SCREEN
            // IN THE MATCH BUILT OUT OF THE WRONG COLOURS. 🧑 2026-08-18, with his own capture:
            // *"the emote wheeel looks ugly tho"*. It is only reachable by HOLDING a key, so no
            // capture pass had ever had it open.
            var wheel = Object.FindFirstObjectByType<UI.EmoteWheel>();

            if (wheel != null)
            {
                wheel.Open();

                yield return null;
                yield return Eyes("emote-wheel");

                wheel.Close(play: false);
            }

            // ---- THE TAYA'S OWN SCREEN --------------------------------------------------
            CharacterMotor taya = null;

            foreach (var m in Object.FindObjectsByType<CharacterMotor>(FindObjectsSortMode.None))
                if (m.IsDefender) { taya = m; break; }

            if (taya != null)
            {
                var hud = Object.FindFirstObjectByType<UI.Hud>();
                if (hud != null) hud.Bind(taya);

                var rig = Object.FindFirstObjectByType<TumbangPreso.CameraSystem.CameraRig>();
                if (rig != null) rig.Follow(taya);

                yield return new WaitForSecondsRealtime(1.0f);
                yield return Eyes("taya-eyes");

                // ⚠️ THE DANGER HOLD, WHICH IS THE STATE THE COMPLAINT IS ABOUT. A taya's tint
                // is live while the can is DOWN — most of a round — and every capture so far
                // has been taken with it standing, so the one screen 🧑 called *"just red"* had
                // never been photographed at all. Knocked down through the host path so the HUD
                // is driven by the event rather than by the probe.
                if (round.Lata != null)
                {
                    round.Lata.HostKnockDown(-1);

                    yield return new WaitForSecondsRealtime(1.2f);
                    yield return Eyes("taya-danger");
                }
            }

            // ---- A CHARGED THROW, HELD ---------------------------------------------------
            // 🧑, on a cropped frame of one: *"THIS charge outline is so ugly, it doesnt behave
            // naturally"*. Nothing in the suite has ever photographed a wind-up, so this puts
            // one on screen from the thrower's eyes and from outside the body.
            CharacterMotor thrower = null;

            foreach (var m in Object.FindObjectsByType<CharacterMotor>(FindObjectsSortMode.None))
            {
                var carry = m.GetComponent<Carrier>();
                if (m.IsDefender || carry == null || carry.Held == null) continue;
                thrower = m;
                break;
            }

            if (thrower != null)
            {
                var rig0 = Object.FindFirstObjectByType<TumbangPreso.CameraSystem.CameraRig>();
                if (rig0 != null) rig0.Follow(thrower);

                // ⚠️ THE BOT HAS TO LET GO OF THE BUTTONS FIRST. `AIController.Act` rewrites
                // this seat's whole intent every frame, including a release sweep over any verb
                // its plan did not touch, so a charge poked in from outside is cancelled before
                // the physics step reads it. The first version of this shot photographed a
                // wind-up that was never running.
                var brain = thrower.GetComponent<AIController>();
                if (brain != null) brain.enabled = false;

                var lata = round.Lata;
                if (lata != null) thrower.Intent.AimPoint = lata.transform.position;

                // Hold the charge for a beat, so the arc and whatever the body does with its
                // arm are both at full commitment when the shutter opens.
                for (float t = 0.0f; t < 1.4f; t += Time.deltaTime)
                {
                    thrower.Intent.Set(Verb.SpecialAbility, true);
                    yield return null;
                }

                yield return Eyes("charge-eyes");
                yield return Portrait(thrower, "charge-body");

                thrower.Intent.Set(Verb.SpecialAbility, false);
            }

            // ---- ONE BODY, CLOSE UP ------------------------------------------------------
            // "The hands are just floating" is a claim about a rig, and a wide shot of four
            // bodies cannot answer it.
            int shot = 0;

            foreach (var m in Object.FindObjectsByType<CharacterMotor>(FindObjectsSortMode.None))
            {
                yield return Portrait(m, $"body{shot}-{(m.IsDefender ? "taya" : "atk")}");
                shot++;
            }

            // ---- THE INTERMISSION CARD ---------------------------------------------------
            // ⚠️ NOTHING HAS EVER PHOTOGRAPHED IT. It is only reachable through a round
            // boundary, so every capture pass so far has stopped short of the one screen 🧑
            // singled out by screenshot: *"PIC 5 ugly ui unlike in godot"*.
            var card = Object.FindFirstObjectByType<UI.RoleSwapCard>();

            if (card != null && GameServices.Match != null)
            {
                GameServices.Match.AddScore(GameServices.Match.DefenderSlot,
                                            Core.ScoreEvent.DefenseTick);

                // Driven through the event the card actually listens to, so the shot is the
                // screen a player gets and not a hand-filled mock of it.
                card.ShowForShot(nextRound: 3, nextDefenderSlot: 2);

                yield return new WaitForSecondsRealtime(2.0f);
                yield return Eyes("intermission");

                // ⚠️ TAKEN BACK DOWN, or it sits over every shot after this one. The first pass
                // photographed the taya's danger vignette through this card's own backdrop and
                // proved nothing about either.
                card.gameObject.SetActive(false);
            }

            // ---- THE MATCH-END BOARD -----------------------------------------------------
            // 🧑: *"the end win screen UI and round end ui or hud looks ugly comapred to
            // godot"*. Raised through the same entry point the match-won event uses.
            var board = Object.FindFirstObjectByType<UI.MatchResult>();

            if (board != null)
            {
                board.OnMatchWon(0);

                yield return new WaitForSecondsRealtime(0.6f);
                yield return Eyes("result");

                // ⚠️ TIME IS PUT BACK. The board freezes the game in single player, and every
                // shot after this one would be of a stopped world.
                Time.timeScale = 1.0f;
                board.gameObject.SetActive(false);
            }

            // § THE TEARDOWN. See the note where `Begin` is called.
            if (runner != null) Object.DestroyImmediate(runner);
        }

        /// <summary>The player's own camera, HUD and all.</summary>
        private static IEnumerator Eyes(string name)
        {
            var cam = Camera.main;
            if (cam == null) { Debug.LogWarning($"[Play] no main camera for {name}."); yield break; }

            yield return Render(cam, name, flipCanvases: true);
        }

        /// <summary>
        /// A camera that is not the player's, parked so the whole cast is in frame. This is the
        /// only way to see what the BODIES are doing: the shipped rig is first person, so the
        /// local player's own limbs never appear in their own shot.
        /// </summary>
        /// <summary>
        /// § 127.3's owed frame: the TAYA'S FEET, from eight metres, at an angle that can
        /// actually see the ground.
        ///
        /// ⚠️⚠️ `Witness` CANNOT ANSWER THIS AND THAT IS WHY THERE IS A SECOND CAMERA. It sits at
        /// 2.6 m and looks at 0.9 m, roughly chest height on a standing Person, which frames the
        /// CAST. A role marker is painted on the floor, and from a near-level camera a ring and a
        /// disc are the same picture: `round-witness.png` is exactly that shot and § 127.3 records
        /// it failing for exactly that reason (*"the taya's marker was caught only edge-on"*).
        ///
        /// ⚠️⚠️ EIGHT METRES IS THE NUMBER § 16.1 ASKS THE QUESTION AT, so it is the distance
        /// rather than whatever framed nicely: *"it does not say a player can read it at eight
        /// metres."* The camera is 8 m from the taya on the ground plane and 4.2 m up, which is
        /// about a 27 degree look-down, and it aims at their FEET rather than their chest.
        ///
        /// ⚠️ AN ATTACKER IS PULLED INTO THE FRAME ON PURPOSE. The claim is not "the ring is
        /// visible", it is "the taya can be picked out", and a shot with one marker in it cannot
        /// fail. The camera is placed on the line between the taya and the nearest attacker so
        /// both markers are in shot at similar sizes.
        /// </summary>
        private static IEnumerator RoleMarkers(string name)
        {
            var match = GameServices.Match;
            var round = GameServices.Round;

            var taya = round?.PlayerAt(match != null ? match.DefenderSlot : 0);
            if (taya == null) yield break;

            Vector3 tayaAt = taya.transform.position;

            CharacterMotor nearest = null;
            float best = float.MaxValue;

            foreach (var m in Object.FindObjectsByType<CharacterMotor>(FindObjectsSortMode.None))
            {
                if (m == taya) continue;

                float d = Vector3.Distance(m.transform.position, tayaAt);
                if (d >= best) continue;

                best = d;
                nearest = m;
            }

            // Look along the taya-to-attacker line so both floor marks are in shot. With nobody
            // else in the arena, fall back to a fixed bearing rather than skipping the frame.
            Vector3 along = nearest != null
                ? (tayaAt - nearest.transform.position)
                : new Vector3(0.0f, 0.0f, -1.0f);

            along.y = 0.0f;
            if (along.sqrMagnitude < 0.0001f) along = new Vector3(0.0f, 0.0f, -1.0f);
            along.Normalize();

            var go = new GameObject("RoleMarkerCam");
            var cam = go.AddComponent<Camera>();
            cam.fieldOfView = 50.0f;
            cam.nearClipPlane = 0.05f;
            cam.farClipPlane = 400.0f;

            cam.transform.position = tayaAt + (along * 8.0f) + new Vector3(0.0f, 4.2f, 0.0f);

            // ⚠️ AIMED AT THE FLOOR, NOT AT THE BODY. The mark is at the taya's feet and a shot
            // centred on their chest puts it at the bottom edge of the frame where a reader
            // cannot judge it.
            cam.transform.LookAt(tayaAt + new Vector3(0.0f, 0.05f, 0.0f));

            Grade(cam);
            yield return Render(cam, name, flipCanvases: false);

            Object.DestroyImmediate(go);
        }

        private static IEnumerator Witness(string name)
        {
            var go = new GameObject("WitnessCam");
            var cam = go.AddComponent<Camera>();
            cam.fieldOfView = 50.0f;
            cam.nearClipPlane = 0.05f;
            cam.farClipPlane = 400.0f;

            // Frame the cast: centre on the average seat position and back off far enough to
            // hold the spread.
            Vector3 sum = Vector3.zero;
            int n = 0;
            float spread = 0.0f;

            foreach (var m in Object.FindObjectsByType<CharacterMotor>(FindObjectsSortMode.None))
            {
                sum += m.transform.position;
                n++;
            }

            Vector3 centre = n > 0 ? sum / n : Vector3.zero;

            foreach (var m in Object.FindObjectsByType<CharacterMotor>(FindObjectsSortMode.None))
                spread = Mathf.Max(spread, Vector3.Distance(m.transform.position, centre));

            float back = Mathf.Max(7.0f, spread * 2.2f);

            cam.transform.position = centre + new Vector3(0.0f, 2.6f, -back);
            cam.transform.LookAt(centre + new Vector3(0.0f, 0.9f, 0.0f));

            Grade(cam);
            yield return Render(cam, name, flipCanvases: false);

            Object.DestroyImmediate(go);
        }

        /// <summary>
        /// Everything the FIRST-PERSON camera can see AT THE MOMENT THE SHOT WAS TAKEN, with
        /// the screen fraction each renderer covers.
        ///
        /// ⚠️ `FppFrameProbe` answers the same question at SPAWN, and the complaint is about
        /// play. A body that is beside you on the spawn line and pressed against your lens three
        /// seconds later are the same seat in two different places.
        /// </summary>
        private static void DumpFrame(string path)
        {
            var cam = Camera.main;
            if (cam == null) return;

            var log = new System.Text.StringBuilder();
            log.AppendLine($"fpp frame, live. camera at {cam.transform.position} " +
                           $"fov {cam.fieldOfView} near {cam.nearClipPlane}");
            log.AppendLine();

            var planes = GeometryUtility.CalculateFrustumPlanes(cam);
            var rows = new System.Collections.Generic.List<(float d, string line)>();

            foreach (var r in Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None))
            {
                if (!r.enabled || !GeometryUtility.TestPlanesAABB(planes, r.bounds)) continue;

                float d = Vector3.Distance(cam.transform.position, r.bounds.center);

                // The screen box of the bounds, which is what "it fills half my view" means.
                Vector3 min = new Vector3(float.MaxValue, float.MaxValue, 0.0f);
                Vector3 max = new Vector3(float.MinValue, float.MinValue, 0.0f);

                for (int i = 0; i < 8; i++)
                {
                    Vector3 c = r.bounds.center + Vector3.Scale(
                        r.bounds.extents,
                        new Vector3((i & 1) == 0 ? -1 : 1,
                                    (i & 2) == 0 ? -1 : 1,
                                    (i & 4) == 0 ? -1 : 1));

                    Vector3 v = cam.WorldToViewportPoint(c);
                    if (v.z <= 0.0f) continue;

                    min = Vector3.Min(min, v);
                    max = Vector3.Max(max, v);
                }

                float coverage = Mathf.Clamp01(max.x - min.x) * Mathf.Clamp01(max.y - min.y);

                rows.Add((d, $"{d,6:F2}  cover {coverage,5:P0}  " +
                             $"x[{min.x,6:F2}..{max.x,6:F2}] y[{min.y,6:F2}..{max.y,6:F2}]  " +
                             $"size {r.bounds.size}  {Path(r.transform)}"));
            }

            rows.Sort((a, b) => a.d.CompareTo(b.d));

            foreach (var row in rows) log.AppendLine(row.line);

            File.WriteAllText(path, log.ToString());
            Debug.Log($"[Play] wrote {path}");
        }

        private static string Path(Transform t)
        {
            string s = t.name;
            while (t.parent != null) { t = t.parent; s = t.name + "/" + s; }
            return s;
        }

        /// <summary>
        /// What every body is actually WEARING mid-round: where its hand is, what it is
        /// holding, how big that thing is drawn, and how far from the hand it ended up.
        ///
        /// ⚠️ "The hands are just floating" names a renderer, and pixels cannot. A carried
        /// tsinelas at the wrong scale, a hand blob left in its bind position and a
        /// neighbouring body pressed against the lens all read the same in a capture.
        /// </summary>
        private static void DumpCarry(string path)
        {
            var log = new System.Text.StringBuilder();
            log.AppendLine("carry state, mid-round");
            log.AppendLine();

            foreach (var m in Object.FindObjectsByType<CharacterMotor>(FindObjectsSortMode.None))
            {
                var visual = m.GetComponentInChildren<Visual.CharacterVisual>();
                var carrier = m.GetComponent<Carrier>();
                Transform hand = visual != null ? visual.HandAnchor : null;

                log.AppendLine($"seat {m.PlayerSlot} {(m.IsDefender ? "TAYA" : "atk")} " +
                               $"at {m.transform.position}");
                log.AppendLine($"  hand anchor: " +
                               $"{(hand == null ? "NONE" : hand.position.ToString())} " +
                               $"scale {(hand == null ? "-" : hand.lossyScale.ToString())}");

                var held = carrier != null ? carrier.Held : null;

                if (held == null)
                {
                    log.AppendLine("  holding: nothing");
                }
                else
                {
                    var r = held.GetComponentInChildren<Renderer>();
                    log.AppendLine($"  holding: {held.name} at {held.transform.position} " +
                                   $"scale {held.transform.lossyScale}");
                    log.AppendLine($"    drawn size {(r == null ? "no renderer" : r.bounds.size.ToString())}");
                    if (hand != null)
                        log.AppendLine($"    hand->object {Vector3.Distance(hand.position, held.transform.position):F3} m");
                }

                // Every renderer on the body, so a limb parked somewhere it should not be is
                // named rather than guessed at.
                foreach (var r in m.GetComponentsInChildren<Renderer>())
                {
                    log.AppendLine($"    r {r.name} centre {r.bounds.center} size {r.bounds.size} " +
                                   $"shadow {r.shadowCastingMode}");
                }

                log.AppendLine();
            }

            File.WriteAllText(path, log.ToString());
            Debug.Log($"[Play] wrote {path}");
        }

        /// <summary>One body, front on, close enough to count its limbs.</summary>
        private static IEnumerator Portrait(CharacterMotor who, string name)
        {
            var go = new GameObject("PortraitCam");
            var cam = go.AddComponent<Camera>();
            cam.fieldOfView = 45.0f;
            cam.nearClipPlane = 0.05f;
            cam.farClipPlane = 400.0f;

            Vector3 at = who.transform.position + new Vector3(0.0f, 0.9f, 0.0f);

            // In front of the body's own facing, so the shot is a face rather than a back.
            cam.transform.position = at + who.transform.forward * 3.4f + Vector3.up * 0.2f;
            cam.transform.LookAt(at);

            Grade(cam);
            yield return Render(cam, name, flipCanvases: false);

            Object.DestroyImmediate(go);
        }

        /// <summary>
        /// ⚠️⚠️ THE SHOT CAMERA GRADES, OR THE SHOT IS NOT OF THIS GAME. A camera created here by
        /// hand has no <see cref="TumbangPreso.Visual.ColourGrade"/>, so the witness and portrait
        /// frames were being rendered with no tonemap and no BCS while every camera a PLAYER
        /// looks through has both. Every "is the game washed out" judgement made from these files
        /// was therefore made against a picture the game never draws — including the ones that
        /// concluded the ambient needed darkening.
        /// </summary>
        private static void Grade(Camera cam)
        {
            var grade = cam.GetComponent<TumbangPreso.Visual.ColourGrade>();
            if (grade == null) grade = cam.gameObject.AddComponent<TumbangPreso.Visual.ColourGrade>();

            grade.AdoptFromScene();
        }

        private static IEnumerator Render(Camera cam, string name, bool flipCanvases)
        {
            // ⚠️⚠️ AN HDR TARGET, AND THE LDR ONE MADE THESE SHOTS LIE ABOUT THE ONE THING THEY
            // WERE BEING USED TO JUDGE. `ColourGrade` runs an ACES roll-off in `OnRenderImage`,
            // which receives the camera's TARGET — and assigning an ARGB32 target overrides
            // `Camera.allowHDR` outright, so every channel was clamped to 1.0 as the surface
            // shader wrote it and the curve was handed an already-flat frame. Eskinita's ambient
            // alone is (1.02, 0.96, 0.86) before a single light, so what these files showed was a
            // white sky over a pale street: not the build's look, the harness's.
            bool hdr = cam.allowHDR;
            var format = hdr ? RenderTextureFormat.DefaultHDR : RenderTextureFormat.ARGB32;

            var rt = new RenderTexture(Width, Height, 24, format,
                hdr ? RenderTextureReadWrite.Linear : RenderTextureReadWrite.Default);

            var prev = cam.targetTexture;

            cam.targetTexture = rt;

            // ⚠️⚠️ THE UI'S TARGET IS CREATED HERE, BEFORE THE LAYOUT FRAMES, AND CREATING IT
            // LATER IS WHY THE TEXT CAME OUT SOFT. 🧑 2026-08-18: *"why do ur fonts look blurry
            // in ur pics?"*. A ScreenSpaceCamera canvas sizes itself to its camera's VIEWPORT,
            // and a camera with no target texture has the viewport of the batch-mode window —
            // which is nothing like 1920x1080. So the CanvasScaler laid the HUD out small, the
            // font atlas was rasterised at that small size, and the whole thing was then scaled
            // up into the shot. The glyphs were never blurry in the game; they were photographed
            // at the wrong resolution and enlarged.
            var resolved = RenderTexture.GetTemporary(Width, Height, 0,
                RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);

            // ⚠️⚠️ THE UI GETS ITS OWN CAMERA, AND HANGING IT OFF THE SCENE CAMERA MADE EVERY UI
            // SHOT IN THIS PROJECT A LIE. 🧑 2026-08-18, comparing these files to the Godot
            // capture: *"look at the huds and ui of everything u js sent. It looks so diff"*. He
            // is right that they differ and the difference is in the harness.
            //
            // A ScreenSpaceOverlay canvas is composited by the engine AFTER post-processing, so
            // in the running game the HUD is never touched by `ColourGrade`. Re-pointing it at
            // the scene camera to get it into a RenderTexture put it INSIDE the graded frame
            // instead, where the ACES roll-off and then Eskinita's contrast 1.03 run over it.
            // Contrast pivots on 0.5 and `saturate()` clips, so anything dark enough is pushed
            // below zero and comes out PURE BLACK:
            //
            //     WOOD_DEEP #31190b  ->  linear 0.0307  ->  tonemap 0.0082  ->  contrast -0.007
            //
            // Measured on the last pass: Godot's scoreboard fill (49, 25, 11), this file's
            // (0, 0, 0). The panels were never black. The photograph was.
            //
            // So the scene renders through the grade, and the UI renders through a SECOND camera
            // with no grade on it, into the resolved sRGB target — which is the same order and
            // the same colour space the engine composites an overlay canvas in.
            var uiCanvases = new System.Collections.Generic.List<Canvas>();
            var layers = new System.Collections.Generic.Dictionary<Transform, int>();

            if (flipCanvases)
            {
                foreach (var c in Object.FindObjectsByType<Canvas>(FindObjectsInactive.Exclude,
                                                                  FindObjectsSortMode.None))
                {
                    if (c.renderMode != RenderMode.ScreenSpaceOverlay) continue;
                    uiCanvases.Add(c);
                }
            }

            Camera uiCam = null;

            if (uiCanvases.Count > 0)
            {
                var uiGo = new GameObject("~ShotUiCamera");
                uiCam = uiGo.AddComponent<Camera>();

                // ⚠️ NOTHING IS CLEARED. This camera draws ON TOP of the frame the scene camera
                // has already resolved into the target; clearing would throw the game away and
                // photograph the HUD on a blank field.
                uiCam.clearFlags = CameraClearFlags.Nothing;

                // ⚠️⚠️ THE CANVASES ARE MOVED ONTO THE UI LAYER FOR THE SHOT, AND A CULLING MASK
                // TAKEN FROM THEIR OWN LAYERS IS NOT ENOUGH. Several of this project's canvases
                // are built in code on the DEFAULT layer, so a mask that includes it hands this
                // orthographic camera the whole arena as well and it redraws slabs of street
                // across the middle of the frame. Moving them to layer 5 for the duration is the
                // only version that renders the UI and nothing but the UI.
                foreach (var c in uiCanvases) SetLayer(c.transform, UiLayer, layers);

                uiCam.cullingMask = 1 << UiLayer;

                // Before the layout frames below, so the canvas scaler sizes to the SHOT.
                uiCam.targetTexture = resolved;
                uiCam.orthographic = true;
                uiCam.nearClipPlane = 0.01f;
                uiCam.farClipPlane = 10.0f;
                uiCam.depth = 100.0f;

                foreach (var c in uiCanvases)
                {
                    c.renderMode = RenderMode.ScreenSpaceCamera;
                    c.worldCamera = uiCam;
                    c.planeDistance = 1.0f;
                }
            }

            // See UiRuntimeShots: the CanvasScaler needs a real frame after the target is
            // assigned or the layout is the runner's own aspect stretched into 16:9.
            yield return null;
            yield return null;

            Canvas.ForceUpdateCanvases();
            cam.Render();

            // ⚠️⚠️ THE HDR TARGET IS RESOLVED THROUGH AN sRGB ONE BEFORE IT IS READ, AND SKIPPING
            // THAT MAKES EVERY SHOT ROUGHLY A STOP AND A HALF TOO DARK. An HDR target is LINEAR;
            // a PNG is sRGB. `ReadPixels` is a raw copy and applies no transfer function at all,
            // so reading the float target straight into an RGB24 texture writes linear values
            // into a file that will be displayed as if they were sRGB — mid-grey 0.5 lands at
            // 0.21 and the street reads as asphalt at night.
            //
            // The engine does this conversion for free when a camera renders to the BACK BUFFER,
            // which is why the game itself was always correct and only these files were not. A
            // Blit into an sRGB target is the same conversion, done explicitly.
            //
            // ⚠️ AND IT IS A SEPARATE FAULT FROM THE CLAMPING ABOVE. They cancel each other
            // roughly out, which is exactly why neither was noticed: fixing only the LDR target
            // gives a dark frame, fixing only this one gives a blown frame, and the pair of them
            // gave a frame that was wrong in a way that looked like a lighting problem.
            // The scene lands in the sRGB target whether it was rendered HDR or not, so the UI
            // below always composites into the same gamma buffer the engine would use.
            Graphics.Blit(rt, resolved);

            // § THE UI, ON TOP, UNGRADED. See the note where uiCam is built.
            if (uiCam != null)
            {
                Canvas.ForceUpdateCanvases();
                uiCam.Render();
                uiCam.targetTexture = null;
            }

            RenderTexture.active = resolved;

            var tex = new Texture2D(Width, Height, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, Width, Height), 0, 0);
            tex.Apply();

            RenderTexture.active = null;
            cam.targetTexture = prev;

            // ⚠️ THE CANVASES GO BACK TO OVERLAY. Leaving them pointed at a destroyed camera
            // blanks the HUD for every shot after this one, and the game itself if the probe is
            // ever run against a live session.
            foreach (var c in uiCanvases)
            {
                if (c == null) continue;
                c.renderMode = RenderMode.ScreenSpaceOverlay;
                c.worldCamera = null;
            }

            RestoreLayers(layers);

            if (uiCam != null) Object.DestroyImmediate(uiCam.gameObject);

            File.WriteAllBytes($"{OutDir}/{name}.png", tex.EncodeToPNG());

            Object.DestroyImmediate(tex);

            if (resolved != null) RenderTexture.ReleaseTemporary(resolved);

            rt.Release();
            Object.DestroyImmediate(rt);

            Debug.Log($"[Play] wrote {OutDir}/{name}.png");
        }
    }
}
