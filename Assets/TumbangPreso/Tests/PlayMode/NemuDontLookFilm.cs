using System;
using System.Collections;
using System.IO;
using System.Linq;
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
    /// FILM, not a gate (`docs/HERO_KIT_METHOD.md` section 7): Nemu's DON'T LOOK in a real Hero Strike match on Bayan Plaza at
    /// true game speed (30 frames per game second). The local seat is the VICTIM, so every view shows what an opponent sees:
    /// the ghost settles, fades out at 3 s, then lunges when they walk in. Nemu really casts it (a held press, released).
    /// Views: the victim's own screen (with the blind drawn as their HUD draws it) and the court from the side.
    /// Runs only with TUMP_NEMU_DONTLOOK_FILM=1; frames under TUMP_EVIDENCE (or Logs)/nemu-dontlook.
    /// </summary>
    [Category("Review")]
    public sealed class NemuDontLookFilm
    {
        private INetProvider _net;

        [UnitySetUp] public IEnumerator Before()
        {
            _net = NetAuthority.Provider;
            yield return PlayModeWorld.Reset();
            if (Environment.GetEnvironmentVariable("TUMP_NEMU_DONTLOOK_FILM") != "1") yield break;
            yield return MapRetrievalProbe.Load(SceneFlow.BayanPlaza, GameMode.HeroStrike);
            Object.FindFirstObjectByType<ReadyGate>().StartLocalCountdown();
            yield return new WaitForSeconds(3.6f);
            NetAuthority.Provider = new SoloProvider();
            foreach (var brain in Object.FindObjectsByType<AIController>(FindObjectsSortMode.None)) brain.enabled = false;
            foreach (var input in Object.FindObjectsByType<PlayerInputReader>(FindObjectsSortMode.None)) input.enabled = false;
            foreach (var switcher in Object.FindObjectsByType<DebugPlayerSwitcher>(FindObjectsSortMode.None)) switcher.enabled = false;
        }

        [UnityTearDown] public IEnumerator After()
        {
            NemuBlindfold.RetireAll();
            yield return PlayModeWorld.Reset();
            NetAuthority.Provider = _net;
        }

        private static Vector3 Flat(Vector3 v) => new Vector3(v.x, 0f, v.z);

        private static Camera MakeCamera(string name, float fov)
        {
            var c = new GameObject(name).AddComponent<Camera>();
            c.CopyFrom(Camera.main); c.enabled = false; c.tag = "Untagged"; c.fieldOfView = fov; c.cullingMask &= ~(1 << 5);
            c.gameObject.AddComponent<ColourGrade>().AdoptFromScene();
            return c;
        }

        [UnityTest, Timeout(900000)]
        public IEnumerator FilmDontLookInAMatch()
        {
            if (Environment.GetEnvironmentVariable("TUMP_NEMU_DONTLOOK_FILM") != "1") Assert.Ignore("Film only: set TUMP_NEMU_DONTLOOK_FILM=1.");
            var round = GameServices.Round;
            var can = Flat(round.Lata.transform.position);
            var victim = round.PlayerAt(GameLaunch.SoloSeat);
            var nemu = round.Players.First(p => p != null && p != victim && !p.IsDefender);
            nemu.CharacterIndex = Roster.IndexIn(Roster.HeroPeople, "nemu");
            nemu.AbilitySystem.BindHero("nemu");
            var art = RosterBook.Load().FindPersonArt("nemu");
            nemu.GetComponent<CharacterVisual>().ApplyModel(art.Model, art.Tint, art.Clips, art.Palette, art.PetModel);

            // Nemu by the can, facing down the lane the victim will come up; the victim 12 m out, facing the can.
            var lane = Vector3.back;
            var nemuAt = can + lane * 2.0f + Vector3.right * 1.2f + Vector3.up * .12f;
            var victimAt = can + lane * 13.0f + Vector3.up * .12f;
            nemu.Teleport(nemuAt); nemu.transform.rotation = Quaternion.LookRotation(lane);
            victim.Teleport(victimAt); victim.transform.rotation = Quaternion.LookRotation(-lane);
            nemu.Intent.Clear(); nemu.Intent.Parked = false; nemu.IsBot = false;
            victim.Intent.Clear(); victim.Intent.Parked = false;
            foreach (var p in round.Players)
                if (p != null && p != nemu && p != victim) p.Teleport(can + new Vector3(p.PlayerSlot % 2 == 0 ? -6f : 6f, .12f, 3f));
            Camera.main.GetComponent<CameraRig>().Follow(victim);
            yield return null;

            var court = MakeCamera("DontLookCourt", 50);
            var blind = new Material(Shader.Find("TumbangPreso/DontLookBlind"));
            var hdr = new RenderTexture(1280, 720, 24, RenderTextureFormat.DefaultHDR, RenderTextureReadWrite.Linear);
            var ldr = new RenderTexture(1280, 720, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            var pixels = new Texture2D(1280, 720, TextureFormat.RGB24, false);
            string root = Path.Combine(Environment.GetEnvironmentVariable("TUMP_EVIDENCE") ?? "Logs", "nemu-dontlook");
            foreach (var view in new[] { "victimscreen", "court" }) Directory.CreateDirectory(Path.Combine(root, view));
            var timing = new StringBuilder().AppendLine("frame,seconds,ghost,armed,sprung,blinded_left,eyes_covered,speed_scale,victim_z");
            var cues = new StringBuilder().AppendLine("seconds,cue,pitch,gain");
            int frame = 0;
            Action<string, Vector3, float, float> heard = (id, at, pitch, gain) =>
                cues.AppendLine(FormattableString.Invariant($"{frame / 30.0:F3},{Audio.AudioCues.FileStemFor(id)},{pitch:F3},{gain:F3}"));
            AudioDirector.WorldCuePlayed += heard;
            int previousRate = Time.captureFramerate;
            Time.captureFramerate = 30;
            int frames = int.TryParse(Environment.GetEnvironmentVariable("TUMP_NEMU_DONTLOOK_FRAMES"), out int asked) && asked > 0 ? asked : 30 * 11;
            float sprungAt = -1f;

            // ⚠️ The blind is the HUD's, so it is laid over the TONE-MAPPED frame (the canvas draws after the grade). Over the
            // HDR target its 7 % of light let a sky worth several units through, and the first film showed the court under it.
            void Save(string view, int f, Material overlay = null)
            {
                Graphics.Blit(hdr, ldr);
                // ⚠️ AND WITH sRGB WRITES ON, as the canvas has them: without, the overlay's linear colours landed raw and the
                // hands (0.03 linear, about 48/255 on a real screen) filmed at 6/255, so film r5 showed only the light.
                if (overlay != null) { bool srgb = GL.sRGBWrite; GL.sRGBWrite = true; Graphics.Blit(null, ldr, overlay); GL.sRGBWrite = srgb; }
                var active = RenderTexture.active; RenderTexture.active = ldr;
                pixels.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0); pixels.Apply(); RenderTexture.active = active;
                File.WriteAllBytes(Path.Combine(root, view, $"{f:D5}.jpg"), pixels.EncodeToJPG(92));
            }
            void Render(Camera c) { var before = c.targetTexture; c.targetTexture = hdr; ComicPopup.PrepareView(c); c.Render(); c.targetTexture = before; }

            try
            {
                for (int f = 0; f < frames; f++)
                {
                    frame = f;
                    float t = f / 30f;
                    // Nemu holds the signature 0.3 s and lets go: the ghost lands about 5 m down the lane.
                    nemu.Intent.Set(Verb.Skill1, t > .5f && t < .8f);
                    // The victim waits, then walks up the lane toward the can, through where the ghost hides.
                    victim.transform.rotation = Quaternion.LookRotation(-lane);
                    victim.Intent.Move = t > 3.6f && t < 9.5f ? Vector2.up : Vector2.zero;
                    yield return null;

                    var ghost = NemuBlindfold.Live.FirstOrDefault(g => g != null);
                    if (ghost != null && ghost.Sprung && sprungAt < 0f && ghost.Victim != null) sprungAt = t;
                    timing.AppendLine(FormattableString.Invariant(
                        $"{f},{t:F3},{ghost != null},{ghost != null && ghost.Armed},{ghost != null && ghost.Sprung},{victim.BlindedLeft:F3},{victim.EyesCovered},{victim.StatusSpeedScale:F3},{victim.transform.position.z - can.z:F2}"));

                    // THE VICTIM'S OWN SCREEN: their first person, with the blind exactly as `UI.TumpHudEffects` drives it.
                    Render(Camera.main);
                    if (victim.IsBlinded)
                    {
                        float elapsed = victim.BlindedElapsed;
                        float covered = elapsed < NecroRules.DontLookBlindSeconds ? 1f : 0f;
                        blind.SetFloat("_Elapsed", elapsed);
                        blind.SetFloat("_Clock", t);
                        blind.SetFloat("_Open", Mathf.Clamp01((elapsed - (NecroRules.DontLookBlindSeconds - .3f)) / .3f));
                        blind.SetFloat("_Covered", covered);
                        blind.SetFloat("_Slow", Mathf.Clamp01((1f - victim.BlindedSpeedScale) / NecroRules.DontLookSlowPeak));
                        blind.SetFloat("_Aspect", 16f / 9f);
                        Save("victimscreen", f, blind);
                    }
                    else Save("victimscreen", f);

                    // THE COURT, from the side: the lane, the spot, the can.
                    var mid = can + lane * 7.0f;
                    court.transform.position = mid + Vector3.right * 5.5f + Vector3.up * 2.4f + lane * 1.0f;
                    court.transform.LookAt(mid + Vector3.up * .7f);
                    // The victim's own body is shadow-only in their first person; the court sees all of it.
                    var hiddenBody = victim.GetComponentsInChildren<Renderer>()
                        .Where(r => r.shadowCastingMode == UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly).ToArray();
                    foreach (var r in hiddenBody) r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
                    Render(court);
                    foreach (var r in hiddenBody) r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly;
                    Save("court", f);
                }
            }
            finally
            {
                Time.captureFramerate = previousRate;
                AudioDirector.WorldCuePlayed -= heard;
                nemu.Intent.Clear(); victim.Intent.Clear();
                File.WriteAllText(Path.Combine(root, "timing.csv"), timing.ToString());
                File.WriteAllText(Path.Combine(root, "cues.csv"), cues.ToString());
                Object.Destroy(court.gameObject); Object.Destroy(blind);
                hdr.Release(); ldr.Release(); Object.Destroy(hdr); Object.Destroy(ldr); Object.Destroy(pixels);
            }
            string summary = FormattableString.Invariant($"sprung at {sprungAt:F2} s; victim seat {victim.PlayerSlot}, Nemu seat {nemu.PlayerSlot}");
            File.WriteAllText(Path.Combine(root, "summary.txt"), summary + Environment.NewLine);
            Debug.Log("[DontLookFilm] " + summary);
            Assert.Greater(sprungAt, NecroRules.DontLookArmSeconds, "The ghost never sprang on the victim (or sprang before arming).");
        }
    }
}
