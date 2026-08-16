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

        [UnityTest]
        public IEnumerator ALiveRoundIsPhotographed()
        {
            Directory.CreateDirectory(OutDir);

            var load = SceneManager.LoadSceneAsync("Eskinita", LoadSceneMode.Single);
            while (load != null && !load.isDone) yield return null;

            for (int i = 0; i < 20; i++) yield return null;

            var round = GameServices.Round;
            Assert.IsNotNull(round, "The arena registered no round.");

            // The free-roam window, which is the state pic 3 was taken in.
            yield return Witness("freeroam");

            // ⚠️ THE DIRECT ANALOGUE OF `Logs/shots-godot/g04-ready.png`: same phase, same
            // camera, nothing held. It is the only frame in either build where the two
            // first-person arms can be compared against each other at rest.
            yield return Eyes("ready-eyes");

            round.BeginRound();

            yield return new WaitForSecondsRealtime(3.0f);

            yield return Eyes("round-eyes");
            yield return Witness("round-witness");

            DumpCarry("Logs/carry-live.txt");
            DumpFrame("Logs/fpp-live.txt");

            yield return new WaitForSecondsRealtime(3.0f);

            yield return Eyes("round-eyes-late");
            yield return Witness("round-witness-late");

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

            yield return Render(cam, name, flipCanvases: false);

            Object.DestroyImmediate(go);
        }

        private static IEnumerator Render(Camera cam, string name, bool flipCanvases)
        {
            var rt = new RenderTexture(Width, Height, 24, RenderTextureFormat.ARGB32);
            var prev = cam.targetTexture;

            cam.targetTexture = rt;

            if (flipCanvases)
            {
                foreach (var c in Object.FindObjectsByType<Canvas>(FindObjectsInactive.Exclude,
                                                                  FindObjectsSortMode.None))
                {
                    if (c.renderMode != RenderMode.ScreenSpaceOverlay) continue;

                    c.renderMode = RenderMode.ScreenSpaceCamera;
                    c.worldCamera = cam;
                    c.planeDistance = cam.nearClipPlane + 0.01f;
                }
            }

            // See UiRuntimeShots: the CanvasScaler needs a real frame after the target is
            // assigned or the layout is the runner's own aspect stretched into 16:9.
            yield return null;
            yield return null;

            Canvas.ForceUpdateCanvases();
            cam.Render();

            RenderTexture.active = rt;
            var tex = new Texture2D(Width, Height, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, Width, Height), 0, 0);
            tex.Apply();

            RenderTexture.active = null;
            cam.targetTexture = prev;

            File.WriteAllBytes($"{OutDir}/{name}.png", tex.EncodeToPNG());

            Object.DestroyImmediate(tex);
            rt.Release();
            Object.DestroyImmediate(rt);

            Debug.Log($"[Play] wrote {OutDir}/{name}.png");
        }
    }
}
