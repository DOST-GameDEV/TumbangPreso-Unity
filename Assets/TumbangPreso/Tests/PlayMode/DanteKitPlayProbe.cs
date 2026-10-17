using System;
using System.Collections;
using System.IO;
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
    /// BASILIO'S KIT, PLAYED AND FILMED (plan section 8, the film rounds): a real Hero Strike match on Bayan Plaza, his skills
    /// pressed through `InputIntent` as a player presses them, host-resolved as in a match, filmed at 30 fps from his own screen,
    /// a court camera and a camera on the player it reaches, every world cue logged for `tools/stitch_ability_film.py`.
    /// Runs only with TUMP_DANTE_KIT_FILM=1; frames under TUMP_EVIDENCE (default Logs), folder `dante-kit-film-TAG`.
    /// </summary>
    public sealed class DanteKitPlayProbe
    {
        private INetProvider _net;

        [UnitySetUp] public IEnumerator Before()
        {
            _net = NetAuthority.Provider;
            yield return PlayModeWorld.Reset();
            yield return MapRetrievalProbe.Load(SceneFlow.BayanPlaza, GameMode.HeroStrike);
            Object.FindFirstObjectByType<ReadyGate>().StartLocalCountdown();
            yield return new WaitForSeconds(3.6f);
            NetAuthority.Provider = new SoloProvider();
            foreach (var brain in Object.FindObjectsByType<AIController>(FindObjectsSortMode.None)) brain.enabled = false;
            foreach (var input in Object.FindObjectsByType<PlayerInputReader>(FindObjectsSortMode.None)) input.enabled = false;
            foreach (var switcher in Object.FindObjectsByType<DebugPlayerSwitcher>(FindObjectsSortMode.None)) switcher.enabled = false;
            foreach (var player in GameServices.Round.Players)
            {
                player.Intent.Clear(); player.Intent.Parked = true;
                player.Teleport(new Vector3(10 + player.PlayerSlot * 2, .12f, -10));
            }
        }

        [UnityTearDown] public IEnumerator After()
        {
            yield return PlayModeWorld.Reset();
            NetAuthority.Provider = _net;
        }

        /// <summary>His body AND his kit on a seat (the Paete lesson: re-binding only the kit filmed someone else casting).</summary>
        private static CharacterMotor Basilio(int slot, Vector3 at)
        {
            var who = GameServices.Round.PlayerAt(slot);
            who.CharacterIndex = Roster.IndexIn(Roster.HeroPeople, "dante");
            who.AbilitySystem.BindHero("dante");
            var art = RosterBook.Load().FindPersonArt("dante");
            who.GetComponent<CharacterVisual>().ApplyModel(art.Model, art.Tint, art.Clips, art.Palette, art.PetModel);
            if (slot == GameLaunch.SoloSeat)
                foreach (var arms in Object.FindObjectsByType<ViewmodelArms>(FindObjectsSortMode.None)) arms.MatchCharacter(who);
            who.Teleport(at); who.transform.rotation = Quaternion.identity;
            who.Intent.Parked = false; who.IsBot = slot != GameLaunch.SoloSeat;
            return who;
        }

        private static Vector3 Flat(Vector3 v) => new Vector3(v.x, 0, v.z);

        private static void Face(CharacterMotor who, Vector3 toward)
        {
            var d = toward - who.transform.position; d.y = 0f;
            if (d.sqrMagnitude > .01f) who.transform.rotation = Quaternion.LookRotation(d.normalized);
        }

        /// <summary>A loose slipper put at their feet and picked up through the host's own pickup.</summary>
        private static bool Arm(CharacterMotor who)
        {
            var carrier = who.GetComponent<Carrier>();
            if (carrier.Held != null) return true;
            foreach (var shoe in Object.FindObjectsByType<Slipper>(FindObjectsSortMode.None))
            {
                if (shoe == null || !shoe.IsGrabbableIgnoringReach(who)) continue;
                shoe.ApplySnapshotPose(who.transform.position + who.transform.forward * .3f, Quaternion.identity, Vector3.zero);
                carrier.HostPickUp(shoe);
                if (carrier.Held != null) return true;
            }
            return false;
        }

        /// <summary>
        /// HIS KIT IN ONE MATCH, each skill given time to read:
        ///   0.0 to 2.4    EARTHBOUND: a hit from behind shoves him (from in front, the court's edge 12 m out stopped him dead); he leans into it, grit off his heels, two furrows.
        ///   2.4 to 7.0    BOULDER: he loads his held slipper (the stone coating), aims at a player 6 m ahead and throws; it
        ///                 Concusses them (their ochre rim is on THEIR screen; here the pebbles circling their head).
        ///   7.0 to 12.0   BASTION: a second Basilio, the taya, raises the wall; an attacker throws into it and it flies back.
        ///   12.0 to 17.5  UNSTOPPABLE: he carries three statuses; he roars, they shatter off him, the plates assemble from the feet.
        ///   (cut)         the last 3.5 s of its 15: the cooling warning, then the crumble.
        /// Views: `owner/` his screen (for Bastion a front three-quarter on the taya, who casts it), `wide/` the court, `victim/`
        /// on whoever it reaches (the Concussed player, the thrower whose slipper comes back, then him close).
        /// </summary>
        [UnityTest, Timeout(900000)]
        public IEnumerator FilmHisKitInAMatch()
        {
            if (Environment.GetEnvironmentVariable("TUMP_DANTE_KIT_FILM") != "1") Assert.Ignore("Film only: set TUMP_DANTE_KIT_FILM=1.");
            string tag = Environment.GetEnvironmentVariable("TUMP_DANTE_KIT_TAG") ?? "v1";
            string root = Path.Combine(Environment.GetEnvironmentVariable("TUMP_EVIDENCE") ?? "Logs", "dante-kit-film-" + tag);
            foreach (string view in new[] { "owner", "wide", "victim" }) Directory.CreateDirectory(Path.Combine(root, view));
            var round = GameServices.Round;
            var can = Flat(round.Lata.transform.position);
            int taya = -1;
            foreach (var p in round.Players) if (p.IsDefender) taya = p.PlayerSlot;
            Assert.GreaterOrEqual(taya, 0);
            int me = GameLaunch.SoloSeat != taya ? GameLaunch.SoloSeat : (taya + 1) % 4;
            int victimSeat = -1, thrower = -1;
            foreach (var p in round.Players)
                if (p.PlayerSlot != taya && p.PlayerSlot != me) { if (victimSeat < 0) victimSeat = p.PlayerSlot; else thrower = p.PlayerSlot; }
            Assert.GreaterOrEqual(thrower, 0, "The film needs four seats.");

            Vector3 start = can + new Vector3(0f, .12f, -12f);
            var him = Basilio(me, start);
            var wall = Basilio(taya, can + new Vector3(-2f, .12f, -1f));
            var victim = round.PlayerAt(victimSeat);
            victim.Teleport(start + new Vector3(0f, 0f, 6f)); victim.Intent.Parked = true;
            var other = round.PlayerAt(thrower);
            other.Teleport(can + new Vector3(8f, .12f, 4f)); other.Intent.Parked = true;
            Assert.IsTrue(Arm(him), "The fixture could not put a slipper in his hand.");
            var kit = (DanteHeroKit)him.AbilitySystem.Kit;
            float shield = kit.Skill1.Duration;

            Camera Make(string name, float fov)
            {
                var c = new GameObject(name).AddComponent<Camera>();
                c.CopyFrom(Camera.main); c.enabled = false; c.tag = "Untagged"; c.fieldOfView = fov; c.cullingMask &= ~(1 << 5);
                c.gameObject.AddComponent<ColourGrade>().AdoptFromScene();
                return c;
            }
            var wide = Make("DanteKitWide", 50);
            var close = Make("DanteKitClose", 45);
            var shoulder = Make("DanteKitShoulder", 55);
            var hdr = new RenderTexture(1280, 720, 24, RenderTextureFormat.DefaultHDR, RenderTextureReadWrite.Linear);
            var ldr = new RenderTexture(1280, 720, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            var pixels = new Texture2D(1280, 720, TextureFormat.RGB24, false);
            int index = 0;
            void Shoot(Camera c, string view)
            {
                PaeteKitPlayProbe.RenderFilmView(c, hdr);
                Graphics.Blit(hdr, ldr);
                var active = RenderTexture.active; RenderTexture.active = ldr;
                pixels.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0); pixels.Apply(); RenderTexture.active = active;
                File.WriteAllBytes(Path.Combine(root, view, $"{index:D5}.jpg"), pixels.EncodeToJPG(92));
            }
            // A front three-quarter on someone: their face and what their hands do.
            void FrontOn(Camera c, CharacterMotor who, float ahead, float side, float up, float lookUp)
            {
                var fwd = who.transform.forward; fwd.y = 0f; fwd = fwd.sqrMagnitude > .01f ? fwd.normalized : Vector3.forward;
                var right = Vector3.Cross(Vector3.up, fwd);
                c.transform.position = who.transform.position + fwd * ahead + right * side + Vector3.up * up;
                c.transform.LookAt(who.transform.position + Vector3.up * lookUp + fwd * .4f);
            }

            int previousRate = Time.captureFramerate;
            Time.captureFramerate = 30;
            var cues = new StringBuilder().AppendLine("seconds,cue,pitch,gain");
            bool slid = false, loaded = false, concussed = false, reflected = false, immune = false, shattered = false;
            Action<string, Vector3, float, float> heard = (id, at, pitch, gain) =>
            {
                cues.AppendLine(FormattableString.Invariant($"{index / 30.0:F3},{Audio.AudioCues.FileStemFor(id)},{pitch:F3},{gain:F3}"));
                reflected |= id.Contains("barrier_reflect");
            };
            AudioDirector.WorldCuePlayed += heard;
            float endShield = 12.6f + shield;
            var timeline = new StringBuilder().AppendLine("film_seconds,game_seconds,section");
            string lastSection = "";
            Vector3 heelStart = start;
            try
            {
                int frames = Mathf.CeilToInt(30 * (endShield + 1.2f));
                for (int f = 0; f < frames; f++)
                {
                    float t = f / 30f;
                    // EARTHBOUND: a hit from in front at 0.5 s.
                    if (t < 2.4f) Face(him, start + Vector3.forward * 8f);
                    if (f == 15) { heelStart = him.transform.position; him.ApplyResolvedImpact(Vector3.forward * 12f); }
                    // BOULDER: load, aim, throw at the player 6 m ahead.
                    if (f == 72) victim.Teleport(Flat(him.transform.position) + Vector3.up * .12f + Vector3.forward * 6f);
                    if (t >= 2.4f && t < 7.0f) { him.Intent.AimPoint = victim.transform.position + Vector3.up * .9f; Face(him, victim.transform.position); }
                    him.Intent.Set(Verb.Skill2, t > 2.6f && t < 2.8f);
                    him.Intent.Set(Verb.SpecialAbility, t > 4.0f && t < 4.7f);
                    // BASTION: the taya Basilio faces the thrower and raises it; the thrower throws into it.
                    if (f == 210)
                    {
                        other.Teleport(Flat(wall.transform.position) + Vector3.up * .12f + new Vector3(.6f, 0f, -6.5f));
                        other.Intent.Parked = false;
                        Arm(other);
                        // He steps out of the Bastion shots until his own turn.
                        him.Teleport(start + new Vector3(-9f, 0f, -4f));
                    }
                    if (t >= 7.0f && t < 12.0f)
                    {
                        Face(wall, other.transform.position); Face(other, wall.transform.position);
                        wall.Intent.AimPoint = other.transform.position; other.Intent.AimPoint = wall.transform.position + Vector3.up * 1.1f;
                    }
                    wall.Intent.Set(Verb.Skill2, t > 7.3f && t < 7.5f);
                    other.Intent.Set(Verb.SpecialAbility, t > 8.6f && t < 9.3f);
                    // UNSTOPPABLE: three statuses on him, then the roar.
                    if (f == 360)
                    {
                        him.Teleport(start); Face(him, start + Vector3.forward * 8f);
                        him.ApplyChilled(); him.ApplyZapped(); him.ApplyConcussed();
                    }
                    him.Intent.Set(Verb.Skill1, t > 12.6f && t < 12.8f);
                    yield return null;

                    slid |= t < 2.4f && Vector3.Distance(Flat(him.transform.position), Flat(heelStart)) > .5f;
                    if (f >= 14 && f <= 40 && f % 3 == 0)
                        Debug.Log(FormattableString.Invariant($"[DanteKitFilm] earthbound f {f} moved {Vector3.Distance(Flat(him.transform.position), Flat(heelStart)):F2} external {him.Velocity}"));
                    if (t > 8.5f && t < 10.5f && f % 3 == 0)
                        foreach (var s in Object.FindObjectsByType<Slipper>(FindObjectsSortMode.None))
                            if (s != null && s.State == SlipperState.InFlight)
                            {
                                // Flying away from the wall while still in front of it: it came back off the field.
                                reflected |= Vector3.Dot(s.Velocity, wall.transform.forward) > 0f && wall.transform.InverseTransformPoint(s.transform.position).z < 3f;
                                Debug.Log(FormattableString.Invariant($"[DanteKitFilm] bastion f {f} slipper by {s.ThrowerSlot} at wall-local {wall.transform.InverseTransformPoint(s.transform.position)} v {s.Velocity}"));
                            }
                    loaded |= t > 2.8f && t < 4.0f && him.GetComponent<Carrier>().Held != null && him.GetComponent<Carrier>().Held.Affinity == SlipperAffinity.Concussed;
                    concussed |= t > 4.0f && t < 7.0f && victim.IsConcussed;
                    shattered |= t > 12.8f && t < 13.4f && !him.IsChilled && !him.IsZapped && !him.IsConcussed;
                    immune |= t > 12.8f && kit.IsDemonicCarapaceActive;

                    // The cut: the armour's middle seconds are the same picture, so the film jumps to its last 3.5 s.
                    bool keep = t < 17.5f || t > endShield - 3.5f;
                    if (!keep) continue;
                    string section = t < 2.4f ? "earthbound" : t < 7.0f ? "boulder" : t < 12.0f ? "bastion" : t < 17.5f ? "unstoppable" : "unstoppable end";
                    if (section != lastSection) { timeline.AppendLine(FormattableString.Invariant($"{index / 30.0:F3},{t:F3},{section}")); lastSection = section; }

                    // Cameras per skill.
                    if (t < 2.4f)
                    {
                        // His right side, low, so the heels and the furrows read.
                        wide.transform.position = heelStart + new Vector3(3.8f, 1.1f, 1.0f); wide.transform.LookAt(heelStart + new Vector3(0f, .5f, 1.0f));
                        FrontOn(close, him, 2.4f, 1.2f, 1.2f, .8f);
                    }
                    else if (t < 7.0f)
                    {
                        var mid = (him.transform.position + victim.transform.position) * .5f;
                        wide.transform.position = mid + new Vector3(5.2f, 2.4f, -.8f); wide.transform.LookAt(mid + Vector3.up * .8f);
                        if (t < 3.6f) FrontOn(close, him, 2.2f, 1.3f, 1.3f, .9f);
                        else FrontOn(close, victim, 2.4f, -1.4f, 1.5f, 1.0f);
                    }
                    else if (t < 12.0f)
                    {
                        // Square to the throw's line, so the slipper's way in and its way back both cross the frame.
                        var mid = (wall.transform.position + other.transform.position) * .5f;
                        var line = Flat(other.transform.position - wall.transform.position).normalized;
                        var square = Vector3.Cross(Vector3.up, line);
                        wide.transform.position = mid + square * 6.5f + Vector3.up * 2.6f; wide.transform.LookAt(mid + Vector3.up * .9f);
                        FrontOn(close, other, 2.6f, 1.4f, 1.6f, 1.0f);
                    }
                    else
                    {
                        wide.transform.position = start + new Vector3(4.2f, 1.8f, 3.0f); wide.transform.LookAt(start + Vector3.up * 1.0f);
                        FrontOn(close, him, 2.1f, -.9f, 1.2f, .9f);
                    }
                    if (t >= 7.0f && t < 12.0f) { FrontOn(shoulder, wall, 3.2f, 2.0f, 1.7f, 1.0f); Shoot(shoulder, "owner"); }
                    else if (Camera.main != null) Shoot(Camera.main, "owner");
                    Shoot(wide, "wide");
                    Shoot(close, "victim");
                    index++;
                }
            }
            finally
            {
                AudioDirector.WorldCuePlayed -= heard;
                File.WriteAllText(Path.Combine(root, "cues.csv"), cues.ToString());
                File.WriteAllText(Path.Combine(root, "timeline.csv"), timeline.ToString());
                Time.captureFramerate = previousRate;
                foreach (var p in round.Players) p.Intent.Clear();
                Object.Destroy(wide.gameObject); Object.Destroy(close.gameObject); Object.Destroy(shoulder.gameObject);
                hdr.Release(); ldr.Release();
            }
            string claims = FormattableString.Invariant(
                $"slid,{slid}\nloaded,{loaded}\nconcussed,{concussed}\nreflected,{reflected}\nshattered,{shattered}\nimmune,{immune}\n");
            File.WriteAllText(Path.Combine(root, "claims.csv"), claims);
            Debug.Log("[DanteKitFilm] " + claims.Replace('\n', ' '));
            Assert.IsTrue(slid, "EARTHBOUND: the hit never moved him.");
            Assert.IsTrue(concussed, "BOULDER never Concussed its target.");
            Assert.IsTrue(reflected, "BASTION never sent the slipper back.");
            Assert.IsTrue(shattered, "UNSTOPPABLE left a status on him.");
            Assert.IsTrue(immune, "UNSTOPPABLE never held.");
        }
    }
}
