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
    /// FILM, not a gate (`docs/HERO_KIT_METHOD.md` section 7): Nemu's other two ghosts in a real Hero Strike match on Bayan
    /// Plaza at true game speed (30 frames per game second). Owner 2026-10-15: *"refine the opther skills of nemu ren"*.
    ///  * 0 to 6.5 s, SKITTER: an attacking Nemu's slipper lies 9 m off; she sends Skitter, it grabs it and brings it back,
    ///    then follows her holding it out while she walks (owner: it waits until she takes it), and she takes it with INTERACT.
    ///  * 6.5 to 15 s, LOOKOUT: the taya (also Nemu) leaves Lookout at the can; the can is knocked down; an attacker comes
    ///    right up to it and scares it off; it goes back to work and stands the can up.
    /// Views: a wide one of the court and a close one on the ghost. Runs only with TUMP_NEMU_GHOSTS_FILM=1; frames under
    /// TUMP_EVIDENCE (or Logs)/nemu-ghosts.
    /// </summary>
    [Category("Review")]
    public sealed class NemuGhostsFilm
    {
        private INetProvider _net;

        [UnitySetUp] public IEnumerator Before()
        {
            _net = NetAuthority.Provider;
            yield return PlayModeWorld.Reset();
            if (Environment.GetEnvironmentVariable("TUMP_NEMU_GHOSTS_FILM") != "1") yield break;
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
            NemuSkitter.RetireAll(); NemuLookout.RetireAll();
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

        private static void MakeNemu(CharacterMotor who)
        {
            who.CharacterIndex = Roster.IndexIn(Roster.HeroPeople, "nemu");
            who.AbilitySystem.BindHero("nemu");
            var art = RosterBook.Load().FindPersonArt("nemu");
            who.GetComponent<CharacterVisual>().ApplyModel(art.Model, art.Tint, art.Clips, art.Palette, art.PetModel);
            who.Intent.Clear(); who.Intent.Parked = false; who.IsBot = false;
        }

        [UnityTest, Timeout(900000)]
        public IEnumerator FilmSkitterAndLookoutInAMatch()
        {
            if (Environment.GetEnvironmentVariable("TUMP_NEMU_GHOSTS_FILM") != "1") Assert.Ignore("Film only: set TUMP_NEMU_GHOSTS_FILM=1.");
            var round = GameServices.Round;
            var can = Flat(round.Lata.transform.position);
            var taya = round.Players.First(p => p != null && p.IsDefender);
            var nemu = round.Players.First(p => p != null && !p.IsDefender);
            var runner = round.Players.First(p => p != null && !p.IsDefender && p != nemu);
            MakeNemu(nemu); MakeNemu(taya);

            // Nemu 10 m out from the can, her slipper 9 m further along the court, loose.
            var side = Vector3.right;
            var nemuAt = can + Vector3.back * 10f + Vector3.up * .12f;
            nemu.Teleport(nemuAt); nemu.transform.rotation = Quaternion.LookRotation(side);
            var shoe = Object.FindObjectsByType<Slipper>(FindObjectsSortMode.None).First(s => s != null && s.OwnerSlot == nemu.PlayerSlot);
            shoe.HostSnatch();
            shoe.transform.position = nemuAt + side * 9f + Vector3.up;
            shoe.HostScatter(Vector3.zero);
            // The taya 5 m off the can; the runner out of the way; everyone else well off.
            taya.Teleport(can + Vector3.left * 5f + Vector3.up * .12f); taya.transform.rotation = Quaternion.LookRotation(Vector3.right);
            var runnerOut = can + Vector3.forward * 9f + Vector3.up * .12f;
            runner.Teleport(runnerOut);
            foreach (var p in round.Players)
                if (p != null && p != nemu && p != taya && p != runner) p.Teleport(can + new Vector3(p.PlayerSlot % 2 == 0 ? -9f : 9f, .12f, 6f));
            yield return null;

            var wide = MakeCamera("GhostsWide", 50);
            var close = MakeCamera("GhostsClose", 40);
            var hdr = new RenderTexture(1280, 720, 24, RenderTextureFormat.DefaultHDR, RenderTextureReadWrite.Linear);
            var ldr = new RenderTexture(1280, 720, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            var pixels = new Texture2D(1280, 720, TextureFormat.RGB24, false);
            string root = Path.Combine(Environment.GetEnvironmentVariable("TUMP_EVIDENCE") ?? "Logs", "nemu-ghosts");
            foreach (var view in new[] { "wide", "close" }) Directory.CreateDirectory(Path.Combine(root, view));
            var timing = new StringBuilder().AppendLine("frame,seconds,skitter,lookout,shoe_to_nemu,can_upright,fetch_active,lookout_active,lookout_lift,can_drawn_lift,can_top_y");
            int previousRate = Time.captureFramerate;
            Time.captureFramerate = 30;
            int frames = 30 * 16;
            bool delivered = false, taken = false, knocked = false, restoredByLookout = false, lookoutFramed = false;
            Vector3 closeAt = Vector3.zero, closeLook = Vector3.zero;

            void Save(string view, int f)
            {
                Graphics.Blit(hdr, ldr);
                var active = RenderTexture.active; RenderTexture.active = ldr;
                pixels.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0); pixels.Apply(); RenderTexture.active = active;
                File.WriteAllBytes(Path.Combine(root, view, $"{f:D5}.jpg"), pixels.EncodeToJPG(92));
            }
            void Render(Camera c) { var before = c.targetTexture; c.targetTexture = hdr; ComicPopup.PrepareView(c); c.Render(); c.targetTexture = before; }

            try
            {
                for (int f = 0; f < frames; f++)
                {
                    float t = f / 30f;
                    nemu.Intent.Set(Verb.Skill2, t > .5f && t < .6f);
                    // She walks off with it waiting on her (it follows), then presses INTERACT and takes it out of its claws.
                    if (t > 3.6f && t < 4.9f) nemu.Teleport(nemu.transform.position - side * (1.6f / 30f));
                    // Her INTERACT, as the host resolves it for any peer's press (`Carrier.HostPickUp`, the grab request's
                    // handler): a press written by this coroutine lands between the motor's edge commit and the carrier's read.
                    nemu.Intent.Set(Verb.Grab, f >= 156 && f <= 159);
                    if (f == 156) nemu.GetComponent<Carrier>().HostPickUp(shoe);
                    taya.Intent.Set(Verb.Skill2, t > 6.8f && t < 6.9f);
                    if (!knocked && t > 7.8f) { knocked = true; round.Lata.HostKnockDown(runner.PlayerSlot); }
                    // The runner comes right up to the downed can, lingers, and goes.
                    if (t > 9.0f && t < 10.6f) runner.Teleport(Vector3.Lerp(runner.transform.position, can + Vector3.forward * .9f + Vector3.up * .12f, .12f));
                    if (t >= 10.6f) runner.Teleport(Vector3.Lerp(runner.transform.position, runnerOut, .1f));
                    runner.transform.rotation = Quaternion.LookRotation(Flat(can - runner.transform.position).sqrMagnitude > .01f ? Flat(can - runner.transform.position).normalized : Vector3.back);
                    bool wasDown = !round.Lata.IsUpright;
                    yield return null;
                    if (wasDown && round.Lata.IsUpright && knocked) restoredByLookout = true;

                    var skitter = NemuSkitter.Live.FirstOrDefault(g => g != null);
                    var lookout = NemuLookout.Live.FirstOrDefault(g => g != null);
                    float shoeToNemu = Vector3.Distance(Flat(shoe.transform.position), Flat(nemu.transform.position));
                    if (t > 1f && t < 6.5f && shoeToNemu < 1.5f) delivered = true;
                    if (t > 5.2f && t < 6.5f && shoe.Holder == nemu) taken = true;
                    timing.AppendLine(FormattableString.Invariant(
                        $"{f},{t:F3},{skitter != null},{lookout != null},{shoeToNemu:F2},{round.Lata.IsUpright},{nemu.AbilitySystem.Kit.AttackingSkill.IsActive},{taya.AbilitySystem.Kit.DefendingSkill.IsActive},{(lookout != null ? lookout.Lift : 0f):F3},{NemuLookout.LiftFor(round.Lata):F3},{(round.Lata.PresentedEnds(out _, out var canTop) ? canTop.y - can.y : -1f):F3}"));

                    if (t < 6.5f)
                    {
                        var mid = nemuAt + side * 4.5f;
                        wide.transform.position = mid + Vector3.back * 7.5f + Vector3.up * 3.2f;
                        wide.transform.LookAt(mid + Vector3.up * .5f);
                        // The close camera is out in front of Skitter (its face, its hands, the slipper in them).
                        Vector3 focus = skitter != null ? skitter.Position : shoe.transform.position;
                        Vector3 ahead = skitter != null ? skitter.Forward : -side;
                        Vector3 want = focus + ahead * 2.4f + Vector3.Cross(Vector3.up, ahead) * 1.0f + Vector3.up * .8f;
                        // (No lag: it runs faster than a following camera keeps up, film v2.)
                        closeAt = want; closeLook = focus;
                        close.transform.position = closeAt;
                        close.transform.LookAt(closeLook + Vector3.up * .3f);
                    }
                    else
                    {
                        wide.transform.position = can + Vector3.back * 6.5f + Vector3.left * 3f + Vector3.up * 3.2f;
                        wide.transform.LookAt(can + Vector3.up * .5f);
                        // The close camera faces Lookout (and the can it is working on).
                        Vector3 ghostAt = lookout != null ? lookout.Position : can;
                        Vector3 ahead = lookout != null ? lookout.Forward : Vector3.back;
                        Vector3 focus = Vector3.Lerp(ghostAt, can, .45f);
                        Vector3 want = focus + ahead * 2.0f + Vector3.Cross(Vector3.up, ahead) * 1.0f + Vector3.up * .8f;
                        bool first = !lookoutFramed; lookoutFramed = true;
                        closeAt = first ? want : Vector3.Lerp(closeAt, want, .06f);
                        closeLook = first ? focus : Vector3.Lerp(closeLook, focus, .15f);
                        close.transform.position = closeAt;
                        close.transform.LookAt(closeLook + Vector3.up * .25f);
                    }
                    // ⚠️ The can is DRAWN raised (the taya's channel, Lookout's heave) only between its LateUpdate and its next
                    // Update, and this coroutine runs in between those the wrong way round: raise it for the shot.
                    var lataType = typeof(Lata); var hidden = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
                    lataType.GetMethod("RestoreRaisePresentation", hidden).Invoke(round.Lata, null);
                    lataType.GetMethod("ApplyRaisePresentation", hidden).Invoke(round.Lata, null);
                    Render(wide); Save("wide", f);
                    Render(close); Save("close", f);
                    lataType.GetMethod("RestoreRaisePresentation", hidden).Invoke(round.Lata, null);
                }
            }
            finally
            {
                Time.captureFramerate = previousRate;
                nemu.Intent.Clear(); taya.Intent.Clear();
                File.WriteAllText(Path.Combine(root, "timing.csv"), timing.ToString());
                // (Skill sounds are muted until approved; the stitcher still wants the file.)
                File.WriteAllText(Path.Combine(root, "cues.csv"), "seconds,cue,pitch,gain" + Environment.NewLine);
                Object.Destroy(wide.gameObject); Object.Destroy(close.gameObject);
                hdr.Release(); ldr.Release(); Object.Destroy(hdr); Object.Destroy(ldr); Object.Destroy(pixels);
            }
            string summary = FormattableString.Invariant($"delivered {delivered}; taken {taken}; knocked {knocked}; stood up by Lookout {restoredByLookout}");
            File.WriteAllText(Path.Combine(root, "summary.txt"), summary + Environment.NewLine);
            Debug.Log("[GhostsFilm] " + summary);
            Assert.IsTrue(delivered, "Skitter never brought the slipper back to Nemu.");
            Assert.IsTrue(taken, "Nemu never took the slipper from Skitter with INTERACT.");
            Assert.IsTrue(restoredByLookout, "Lookout never stood the downed can back up.");
        }
    }
}
