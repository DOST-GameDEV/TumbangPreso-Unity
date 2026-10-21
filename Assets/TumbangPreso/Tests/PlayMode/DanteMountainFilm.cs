using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using NUnit.Framework;
using TumbangPreso.Abilities;
using TumbangPreso.CameraSystem;
using TumbangPreso.Core;
using TumbangPreso.UI;
using TumbangPreso.Visual;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace TumbangPreso.PlayTests
{
    /// <summary>
    /// FILM, not a gate: Dante in MOUNTAIN FORM moving in a real Hero Strike match on Bayan Plaza at true game speed (30
    /// frames per game second), for judging his own clip set in play rather than in stills. The suit closes over him live (no
    /// cutscene), he walks, sprints, jumps, carries and throws at a player; then the form's clock is moved on to its last 12 s
    /// and he walks while it sheds. Two views: a follow camera at his front quarter, and the court from high on its side. He is a
    /// scripted bot seat (simulated here, drawn for every camera; as the local seat his own first person hides his body and the
    /// suit). His first-person hands are filmed by `DanteDriftFilm`. Runs only with
    /// TUMP_DANTE_MOUNTAIN_FILM=1; frames under TUMP_EVIDENCE (or Logs)/dante-mountain-play for `tools/stitch_ability_film.py`.
    /// </summary>
    [Category("Review")]
    public sealed class DanteMountainFilm
    {
        private INetProvider _net;

        [UnitySetUp] public IEnumerator Before()
        {
            _net = NetAuthority.Provider;
            yield return PlayModeWorld.Reset();
            if (Environment.GetEnvironmentVariable("TUMP_DANTE_MOUNTAIN_FILM") != "1") yield break;
            yield return MapRetrievalProbe.Load(SceneFlow.BayanPlaza, GameMode.HeroStrike);
            Object.FindFirstObjectByType<ReadyGate>().StartLocalCountdown();
            yield return new WaitForSeconds(ReadyGate.HudRevealSeconds + 3.6f);
            NetAuthority.Provider = new SoloProvider();
            foreach (var brain in Object.FindObjectsByType<AIController>(FindObjectsSortMode.None)) brain.enabled = false;
            foreach (var input in Object.FindObjectsByType<PlayerInputReader>(FindObjectsSortMode.None)) input.enabled = false;
            foreach (var switcher in Object.FindObjectsByType<DebugPlayerSwitcher>(FindObjectsSortMode.None)) switcher.enabled = false;
        }

        [UnityTearDown] public IEnumerator After()
        {
            yield return PlayModeWorld.Reset();
            NetAuthority.Provider = _net;
        }

        private static void Bind(CharacterMotor body, string hero)
        {
            body.CharacterIndex = Roster.IndexIn(Roster.HeroPeople, hero);
            body.AbilitySystem.BindHero(hero);
            var art = RosterBook.Load().FindPersonArt(hero);
            if (art != null) body.GetComponent<CharacterVisual>().ApplyModel(art.Model, art.Tint, art.Clips, art.Palette, art.PetModel);
        }

        private static Vector3 Flat(Vector3 v) => new Vector3(v.x, 0f, v.z);

        [UnityTest, Timeout(1200000)]
        public IEnumerator FilmMountainFormInPlay()
        {
            if (Environment.GetEnvironmentVariable("TUMP_DANTE_MOUNTAIN_FILM") != "1") Assert.Ignore("Film only: set TUMP_DANTE_MOUNTAIN_FILM=1.");
            var round = GameServices.Round;
            var can = Flat(round.Lata.transform.position);
            // An attacker: the taya carries no slipper, so a taya Dante could neither carry nor throw (film run 5).
            var dante = round.Players.First(p => p != null && p != round.PlayerAt(GameLaunch.SoloSeat) && !p.IsDefender);
            Bind(dante, "dante");
            var origin = can + new Vector3(-4f, .12f, -7f);
            dante.Teleport(origin); dante.transform.rotation = Quaternion.identity;
            dante.Intent.Clear(); dante.Intent.Parked = false; dante.IsBot = true;
            var others = round.Players.Where(p => p != null && p != dante).ToArray();
            string[] looks = { "amihan", "cheska", "paete" };
            for (int i = 0; i < others.Length; i++)
            {
                Bind(others[i], looks[i % looks.Length]);
                others[i].Intent.Clear(); others[i].Intent.Parked = true;
            }
            // His loop: 9 m round the can, outside the taya's box (a loop at 5.5 m, steered a quarter-turn ahead, spiralled him into
            // its edge and he stood there). The one he throws at stands inside it.
            Vector3 Loop(float a) => can + new Vector3(Mathf.Sin(a) * 9f, .12f, -Mathf.Cos(a) * 9f);
            dante.Teleport(Loop(0f)); dante.transform.rotation = Quaternion.LookRotation(Vector3.right);
            var target = others[0];
            target.Teleport(can + new Vector3(4.5f, .12f, -6.5f));
            for (int i = 1; i < others.Length; i++) others[i].Teleport(can + new Vector3(-10f - 2f * i, .12f, 9f));
            // His own slipper in his hand: the carry (`mountain-holding-right`), the stone on it, and the throw.
            var own = Object.FindObjectsByType<Slipper>(FindObjectsSortMode.None).FirstOrDefault(x => x.SeatOfOrigin == dante.PlayerSlot);
            if (own != null && dante.GetComponent<Carrier>().Held == null) own.HostForceEquip(dante);
            var kit = (DanteHeroKit)dante.AbilitySystem.Kit;
            typeof(DanteHeroKit).GetMethod("BeginMountain", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(kit, new object[] { dante, false });
            yield return null;

            var follow = new GameObject("MountainFollow").AddComponent<Camera>();
            follow.CopyFrom(Camera.main); follow.enabled = false; follow.tag = "Untagged"; follow.fieldOfView = 42f; follow.cullingMask = ~(1 << 5);
            follow.gameObject.AddComponent<ColourGrade>().AdoptFromScene();
            var court = new GameObject("MountainCourt").AddComponent<Camera>();
            court.CopyFrom(follow); court.enabled = false; court.fieldOfView = 46f;
            court.gameObject.AddComponent<ColourGrade>().AdoptFromScene();
            court.transform.position = can + new Vector3(11f, 7.5f, -9f); court.transform.LookAt(can + Vector3.up * .6f);
            string root = Path.Combine(Environment.GetEnvironmentVariable("TUMP_EVIDENCE") ?? "Logs", "dante-mountain-play");
            foreach (var view in new[] { "follow", "court" }) Directory.CreateDirectory(Path.Combine(root, view));
            var hdr = new RenderTexture(1280, 720, 24, RenderTextureFormat.DefaultHDR, RenderTextureReadWrite.Linear);
            var ldr = new RenderTexture(1280, 720, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            var pixels = new Texture2D(1280, 720, TextureFormat.RGB24, false);
            var cues = new StringBuilder().AppendLine("seconds,cue,pitch,gain");
            int frame = 0;
            Action<string, Vector3, float, float> heard = (id, at, pitch, gain) =>
                cues.AppendLine(FormattableString.Invariant($"{frame / 30.0:F3},{Audio.AudioCues.FileStemFor(id)},{pitch:F3},{gain:F3}"));
            AudioDirector.WorldCuePlayed += heard;
            int previousRate = Time.captureFramerate; Time.captureFramerate = 30;
            var log = new StringBuilder("frame,seconds,left,armour,variant,clip,grounded,speed,x,z,stunned,move\n");
            void Save(Camera c, string view)
            {
                var before = c.targetTexture; c.targetTexture = hdr; ComicPopup.PrepareView(c); c.Render(); c.targetTexture = before;
                Graphics.Blit(hdr, ldr);
                var active = RenderTexture.active; RenderTexture.active = ldr;
                pixels.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0); pixels.Apply(); RenderTexture.active = active;
                File.WriteAllBytes(Path.Combine(root, view, $"{frame:D5}.jpg"), pixels.EncodeToJPG(90));
            }
            try
            {
                var ctx = new AbilityContext(dante, dante.GetComponent<Carrier>(), dante.GetComponent<CombatVerbs>());
                bool skipped = false, thrown = false;
                Vector3 camAt = dante.transform.position + new Vector3(3.5f, 1.7f, 3.5f);
                for (int f = 0; f < 30 * 27; f++)
                {
                    frame = f;
                    float t = f / 30f;
                    // The script: forming (0 to 2.5), walking (2.5 to 5.5), sprinting (5.5 to 8), a jump (8.2), the throw
                    // (9.5 to 11.2), then the clock moved on to the shedding (at 12) and a walk through it to the end.
                    if (!skipped && t >= 12f) { kit.Tick(ctx, Mathf.Max(0f, kit.MountainLeft - 11.5f)); skipped = true; }
                    // Steered round the loop by where he looks: the point a quarter-turn ahead of him on it.
                    var flat = Flat(dante.transform.position - can);
                    float angle = Mathf.Atan2(flat.x, -flat.z);
                    bool walking = (t >= 2.5f && t < 8f) || (t >= 12.5f && t < 26f);
                    // ⚠️ A bot's move is in WORLD axes, not his own (film run 3: `Vector2.up` walked him straight north into the
                    // court's edge). Along the loop's tangent, pulled back toward its radius.
                    var tangent = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) + (flat.magnitude - 9f) * -flat.normalized * .25f;
                    tangent = tangent.normalized;
                    dante.Intent.Move = walking ? new Vector2(tangent.x, tangent.z) : Vector2.zero;
                    dante.Intent.Set(Verb.Sprint, t >= 5.5f && t < 8f);
                    dante.Intent.Set(Verb.Jump, t >= 8.2f && t < 8.3f);
                    bool winding = t >= 9.5f && t < 10.7f && !thrown;
                    bool aiming = t >= 9.3f && t < 11.2f;
                    dante.Intent.FaceAimPoint = true;
                    dante.Intent.AimPoint = aiming ? target.transform.position + Vector3.up : Loop(angle + .35f) + Vector3.up;
                    dante.Intent.Set(Verb.SpecialAbility, winding);
                    if (t >= 10.7f && !thrown) thrown = true;
                    yield return null;
                    var at = dante.transform.position;
                    var fwd = Flat(dante.transform.forward).sqrMagnitude > .01f ? Flat(dante.transform.forward).normalized : Vector3.forward;
                    var right = Vector3.Cross(Vector3.up, fwd);
                    var want = at + fwd * 3.6f + right * 2.4f + Vector3.up * 1.6f;
                    camAt = Vector3.Lerp(camAt, want, 1f - Mathf.Exp(-4f * (1f / 30f)));
                    follow.transform.position = camAt; follow.transform.LookAt(at + Vector3.up * 1.0f);
                    Save(follow, "follow");
                    Save(court, "court");
                    var animator = dante.GetComponentInChildren<CharacterAnimator>();
                    log.AppendLine(FormattableString.Invariant($"{f},{t:F3},{kit.MountainLeft:F2},{(dante.GetComponentInChildren<DanteMountainArmour>() != null ? 1 : 0)},{animator?.ClipVariant},{animator?.CurrentClipName},{dante.IsGrounded},{Flat(dante.Velocity).magnitude:F2},{dante.transform.position.x:F2},{dante.transform.position.z:F2},{dante.IsStunned},{dante.Intent.Move.y:F1}"));
                }
            }
            finally
            {
                AudioDirector.WorldCuePlayed -= heard;
                Time.captureFramerate = previousRate;
                File.WriteAllText(Path.Combine(root, "cues.csv"), cues.ToString());
                File.WriteAllText(Path.Combine(root, "timing.csv"), log.ToString());
                hdr.Release(); ldr.Release(); Object.Destroy(hdr); Object.Destroy(ldr); Object.Destroy(pixels); Object.Destroy(follow.gameObject); Object.Destroy(court.gameObject);
            }
            Assert.IsTrue(File.ReadAllText(Path.Combine(root, "timing.csv")).Contains("mountain-"), "He never played his own clip set.");
        }
    }
}
