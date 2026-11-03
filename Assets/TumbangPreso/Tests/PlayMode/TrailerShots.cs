using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using TumbangPreso.Core;
using TumbangPreso.Map;
using TumbangPreso.UI;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using TumbangPreso.CameraSystem;
using TumbangPreso.Abilities;
using UnityEngine.SceneManagement;
using TumbangPreso.Visual;
using TumbangPreso.UI.Hub;
using Object = UnityEngine.Object;

namespace TumbangPreso.PlayTests
{
    /// <summary>
    /// THE TRAILER CAPTURE HARNESS: the 60 second gameplay trailer's animation shots, one test per shot, each recorded
    /// from a map film (`MapFilm`) or the Arena's intro on the film's own clock. Nothing here changes what a player sees:
    /// a shot that needs staging the film does not have (a camera path of its own, extras) poses it here, after the
    /// film has drawn its frame and before the frame is taken (`FilmFrames.Before`), and the film's `End` takes it away.
    ///
    /// For each shot, into Logs/trailer/&lt;NN&gt;_&lt;slug&gt;/ (Logs/ is not in Git):
    ///   * &lt;pass&gt;/frame_00000.png onward: 1920x1080 at 60 a second of SHOT time, from 0.5 s before the shot's in
    ///     point to 0.5 s after its out point (the editor's handles). CLEAN is the picture alone, no overlay canvas at
    ///     all; TITLED (only for a shot that names overlay pieces) draws only those (`MapFilm.OverlayParts`);
    ///   * cues.csv: every cue the film called and every ambience bed under it, at its shot and film time;
    ///   * shot.txt: the source, the film age of every frame and the measured frame timing.
    /// Then `python tools/render_trailer_shot.py Logs/trailer/&lt;NN&gt;_&lt;slug&gt;` mixes the stem WAV and encodes the
    /// reference MP4s. Run one shot at a time, by its full name (docs/TESTING.md, "Running the trailer harness").
    /// </summary>
    public sealed class TrailerShots
    {
        private string _settings;
        private CustomRules _rules;
        private bool _rulesPinned;
        private FilmFrames _frames;

        [UnitySetUp] public IEnumerator Before()
        {
            _rules = SceneFlow.SelectedRules.Clone(); _rulesPinned = SceneFlow.RulesPinned;
            yield return PlayModeWorld.Reset();
            _settings = JsonUtility.ToJson(Settings.SettingsStore.Current);
        }

        [UnityTearDown] public IEnumerator After()
        {
            _frames?.Dispose(); _frames = null;
            MapFilm.Heard = null;
            MapFilmTests.ArrivalActive(false);
            MapFilm.Stop();
            MapFilmTests.HoldClock(false);
            JsonUtility.FromJsonOverwrite(_settings, Settings.SettingsStore.Current);
            Time.timeScale = 1.0f;
            yield return PlayModeWorld.Reset();
            SceneFlow.AdoptRemoteRules(_rules);
            if (_rulesPinned) SceneFlow.PinSelectedRules(_rules); else SceneFlow.UnpinSelectedRules();
        }

        // ------------------------------------------------------------------ the shots

        [UnityTest, Timeout(1800000)] public IEnumerator Shot01DuskInTondo() => Record(DuskInTondo());
        [UnityTest, Timeout(1800000)] public IEnumerator Shot05TayaTaunts() => Record(TayaTaunts());
        [UnityTest, Timeout(2400000)] public IEnumerator Shot48KeyArt() => Record(KeyArt());
        [UnityTest, Timeout(3600000)] public IEnumerator Ult35RagoMayonAwakens() => RecordUltimate(RagoMayon());
        [UnityTest, Timeout(3600000)] public IEnumerator Ult36YasminAbsoluteZero() => RecordUltimate(YasminAbsoluteZero());
        [UnityTest, Timeout(3600000)] public IEnumerator Ult37AmihanAirburst() => RecordUltimate(AmihanAirburst());
        [UnityTest, Timeout(3600000)] public IEnumerator Ult38PaeteMakilingsEmbrace() => RecordUltimate(PaeteMakilingsEmbrace());

        // ------------------------------------------------------------------ 01 DUSK IN TONDO
        //
        // Trailer 0:00 to 0:02, the first image, under street ambience only. TondoFilm's SAMPAY beat (film 0.0 to 3.6)
        // is a 3.6 s move that ends looking up the court past the can; at 2 s it would whip and still not land on the
        // can. So the path is the trailer's own, inside the film's light (its Mood and Tint, film ages 0 to 3), its
        // laundry on the lines in front of the lens and its houses: under the shirts against the dusk sky, a slide
        // north that tilts down, landing on the lata in the middle of the chalk box, centred and a third of the way
        // up the frame, still for the last 0.4 s (and the handle after). Two kids idle at the far edge of the box and
        // one leans on a house front, all behind the can,
        // each acted on the film's acting layer (`Bouncer`, `Waver`, `Leaner`). No title (TONDO gets its own shot): clean only.

        private static Shot DuskInTondo()
        {
            var shot = new Shot { Number = 1, Slug = "dusk-in-tondo", Map = SceneFlow.Eskinita, Length = 2.0f, FilmIn = 0.5f, Titled = MapFilm.OverlayPart.None,
                                  Source = "TondoFilm SAMPAY (film 0.0 to 3.6): light, laundry and clock at film age = 0.5 + shot time; camera path and kids trailer-only" };
            const float Moving = 1.6f;   // the move lands here; still from here to the out point and through the handle
            Vector3 startEye = default, endEye = default; Quaternion startTurn = default, endTurn = default;
            const float StartFov = 62.0f, EndFov = 42.0f;
            var kids = new List<(Staging.Extra kid, Vector3 at, float yaw, string clip, float phase)>();
            shot.Prepare = s =>
            {
                // The film's fresh chalk is for its CHALK beat; here the box is whole, its own south line drawn.
                s.Hide<Renderer>("_chalkLine");
                var line = s.Field<Renderer>("_realLine"); if (line != null) line.enabled = true;

                Vector3 can = s.CanTop;
                startEye = s.Centre + new Vector3(0.6f, 1.3f, -14.2f);
                startTurn = Quaternion.LookRotation(s.Centre + new Vector3(0.2f, 7.5f, -8.0f) - startEye) * Quaternion.Euler(0.0f, 0.0f, 5.0f);
                // High enough over the south line that the chalk square reads round the can (r1 at 2.4 m showed only its far line).
                endEye = s.Centre + new Vector3(0.0f, 3.3f, -7.6f);
                endTurn = Staging.Placing(endEye, can, EndFov, 1.0f / 3.0f);

                // At the box's far edge, either side of the line of sight past the can, and one on a step by a house.
                var a = s.Person("Trailer kid A", 0.64f, "bebang", "jun_jun", "totoy", "tikboy");
                var b = s.Person("Trailer kid B", 0.64f, "totoy", "tikboy", "jun_jun", "bebang");
                var c = s.Person("Trailer kid C", 0.62f, "tikboy", "jun_jun", "bebang", "totoy");
                Vector3 at;
                at = s.Centre + new Vector3(-2.5f, 0.0f, 7.5f); at.y = s.Floor(at); if (a != null) kids.Add((a, at, 160.0f, "idle", 0.0f));
                at = s.Centre + new Vector3(1.9f, 0.0f, 7.9f); at.y = s.Floor(at); if (b != null) kids.Add((b, at, 205.0f, "idle", 0.7f));
                if (c != null)
                {
                    // Leaning on the east house front north of the box, watching the court: the map's own wall, no prop.
                    Vector3 from = s.Centre + new Vector3(0.0f, 0.6f, 8.6f);
                    float wallX = s.Wall(from, Vector3.right, 12.0f, out var hit) ? hit.point.x : s.Centre.x + 7.6f;
                    Vector3 lean = new Vector3(wallX - 0.45f, 0.0f, from.z); lean.y = s.Floor(lean);
                    s.Note($"wall {(hit.collider != null ? hit.collider.name : "not found")} at x {wallX:F2}");
                    kids.Add((c, lean, 250.0f, "idle", 1.3f));
                }
                foreach (var k in kids) s.Note($"{k.kid.Name} at {k.at:F2} yaw {k.yaw:F0} {k.clip}");
                s.Note($"can top {can:F2}; eye {startEye:F2} to {endEye:F2}");
            };
            shot.Done = s => { foreach (var k in kids) s.Note($"{k.kid.Name}: acting turned the torso up to {k.kid.MostTorso:F0} and the right arm up to {k.kid.MostArm:F0} degrees"); };
            shot.Pose = (s, t) =>
            {
                Vector3 can = s.CanTop;
                for (int i = 0; i < kids.Count; i++)
                {
                    var k = kids[i];
                    k.kid.Show(true);
                    k.kid.Pose(k.at, k.yaw);
                    k.kid.Play(k.clip, k.phase + t);
                    s.Act(k.kid, i == 0 ? Bouncer(k.at, k.yaw, kids[Mathf.Min(1, kids.Count - 1)].at, t) : i == 1 ? Waver(k.at, k.yaw, kids[0].at, t) : Leaner(k.at, k.yaw, kids[0].at, can, t));
                }
                // The lens: one move, eased in and out with no jerk (smootherstep), the turn trailing the slide by a
                // beat so the tilt down lands just after the dolly does, a rise and fall over the middle.
                float move = Smoother((t + 0.5f) / (Moving + 0.5f));
                float tilt = Smoother((t + 0.25f) / (Moving + 0.25f));
                Vector3 eye = Vector3.Lerp(startEye, endEye, move);
                eye.y += 0.35f * Mathf.Sin(Mathf.PI * move);
                s.Shoot(eye, Quaternion.Slerp(startTurn, endTurn, tilt), Mathf.Lerp(StartFov, EndFov, Smoother((t + 0.4f) / (Moving + 0.4f))));
            };
            return shot;
        }

        // ---- the kids, acted. Each is a small story inside two seconds, every curve continuous: weight that shifts
        // through the hips, the head and arms trailing the torso (overlap), and every move an anticipation, a peak
        // and a settle shaped as smooth bells, never a switch. They are far away in the frame, so the poses are big.

        // The camera lands at 1.6 s and the kids are only in frame, sharp, from about 1.2 s (r3: their beats at 1.05 s
        // went by blurred at the bottom edge). So each one's beat peaks in the settled hold, 1.4 to 2.1 s.

        /// <summary>A kid bouncing on their toes, impatient for the game: a sway, a crouch, a hop with both arms flung
        /// up in a wide V, the landing taken in the knees, a settle.</summary>
        private static Staging.Acting Bouncer(Vector3 at, float yaw, Vector3 friend, float t)
        {
            Vector3 f = Quaternion.Euler(0.0f, yaw, 0.0f) * Vector3.forward, side = Vector3.Cross(Vector3.up, f);
            float sway = Mathf.Sin((t + 0.5f) * Mathf.PI * 2.0f / 1.1f);
            float crouch = Bell(t, 1.3f, 0.09f), jump = Bell(t, 1.52f, 0.1f), land = Bell(t, 1.74f, 0.07f), after = Bell(t, 1.95f, 0.12f);
            Vector3 down = Vector3.down + f * 0.2f;
            Vector3 upL = Vector3.up * 0.8f - side + f * 0.15f, upR = Vector3.up * 0.8f + side + f * 0.15f;
            float arms = Smooth(jump * 1.5f - 0.15f) + 0.35f * after;
            return new Staging.Acting
            {
                Lean = 5.0f * sway + 24.0f * crouch - 16.0f * jump + 20.0f * land + 6.0f * after,
                Rock = 10.0f * sway * (1.0f - jump), Twist = 7.0f * sway,
                Nod = -4.0f + 12.0f * crouch - 18.0f * jump + 14.0f * land,
                Turn = Turn(at, f, friend) * 0.4f + 8.0f * Mathf.Sin((t + 0.2f) * 2.6f) * (1.0f - jump),
                Tilt = 8.0f * Mathf.Sin((t + 0.9f) * 3.1f),
                Left = Vector3.Slerp(down - side * (0.35f + 0.2f * sway), upL, Mathf.Clamp01(arms)),
                Right = Vector3.Slerp(down + side * (0.35f - 0.2f * sway), upR, Mathf.Clamp01(arms)),
                Arms = 0.6f + 0.4f * Mathf.Clamp01(arms),
                Hop = 0.06f * Mathf.Max(0.0f, sway) * (1.0f - Mathf.Clamp01(crouch * 3.0f)) * Span01(t, 0.0f, 1.1f) + 0.26f * jump,
            };
        }

        /// <summary>A kid with hands on their knees, who straightens (the torso first) and waves a big "tara!" at the
        /// bouncer: the arm swings up a beat after the body with an overshoot and waves three times across the frame.</summary>
        private static Staging.Acting Waver(Vector3 at, float yaw, Vector3 friend, float t)
        {
            Vector3 f = Quaternion.Euler(0.0f, yaw, 0.0f) * Vector3.forward, side = Vector3.Cross(Vector3.up, f);
            float breath = Mathf.Sin(t * 3.4f);
            float rise = Smoother((t - 1.05f) / 0.35f);
            float reach = Overshoot((t - 1.2f) / 0.3f);
            Vector3 toFriend = Vector3.ProjectOnPlane(friend - at, Vector3.up).normalized;
            float wave = Mathf.Sin((t - 1.45f) * Mathf.PI * 2.0f / 0.32f) * Span01(t, 1.42f, 1.55f);
            Vector3 raised = (Vector3.up * 1.1f + toFriend * (0.45f + 0.35f * wave) + f * 0.25f).normalized;
            Vector3 knee = Vector3.down + f * 0.75f;
            return new Staging.Acting
            {
                Lean = Mathf.Lerp(30.0f + 3.0f * breath, -8.0f, rise) - 4.0f * Bell(t, 1.5f, 0.1f),
                Rock = Mathf.LerpUnclamped(0.0f, -12.0f, reach) + 3.0f * wave, Twist = Mathf.LerpUnclamped(0.0f, 12.0f, reach),
                Nod = Mathf.Lerp(-18.0f, -8.0f, rise),
                Turn = Turn(at, f, friend) * Mathf.Lerp(0.2f, 0.85f, rise), Tilt = Mathf.Lerp(3.0f, -10.0f, rise) + 4.0f * wave,
                Left = Vector3.Slerp(knee - side * 0.25f, Vector3.down - side * 0.3f + f * 0.1f, rise),
                Right = Vector3.SlerpUnclamped(knee + side * 0.25f, raised, reach),
                Arms = 0.95f,
                Hop = 0.05f * Bell(t, 1.42f, 0.06f),
            };
        }

        /// <summary>A kid leaning back on the house front, arms behind, who turns to the bouncer's hop and laughs.</summary>
        private static Staging.Acting Leaner(Vector3 at, float yaw, Vector3 friend, Vector3 can, float t)
        {
            Vector3 f = Quaternion.Euler(0.0f, yaw, 0.0f) * Vector3.forward, side = Vector3.Cross(Vector3.up, f);
            float look = Smoother((t - 1.1f) / 0.35f);
            float laugh = Bell(t, 1.72f, 0.1f) + 0.7f * Bell(t, 1.95f, 0.08f);
            return new Staging.Acting
            {
                Lean = -10.0f + 1.5f * Mathf.Sin(t * 2.7f) + 16.0f * laugh, Rock = 6.0f - 6.0f * look, Twist = 4.0f * Mathf.Sin(t * 1.3f) + 8.0f * look,
                Nod = -4.0f + 14.0f * laugh, Turn = Mathf.Lerp(Turn(at, f, can), Turn(at, f, friend), look), Tilt = 10.0f - 14.0f * look,
                Left = Vector3.Slerp(Vector3.down - f * 0.5f - side * 0.15f, f * 0.6f + Vector3.down * 0.4f - side * 0.1f, Mathf.Clamp01(laugh)),
                Right = Vector3.Slerp(Vector3.down - f * 0.5f + side * 0.15f, f * 0.6f + Vector3.down * 0.4f + side * 0.1f, Mathf.Clamp01(laugh)),
                Arms = 0.8f,
            };
        }

        /// <summary>Head turn (+ left) from a body at `at` facing `f` toward `target`, as `MapFilm.LookTurn`.</summary>
        private static float Turn(Vector3 at, Vector3 f, Vector3 target)
        {
            Vector3 to = Vector3.ProjectOnPlane(target - at, Vector3.up);
            return to.sqrMagnitude < 0.01f ? 0.0f : Mathf.Clamp(-Vector3.SignedAngle(f, to, Vector3.up), -70.0f, 70.0f);
        }

        private static float Span01(float t, float from, float to) => Smooth((t - from) / Mathf.Max(0.0001f, to - from));
        private static float Bell(float t, float at, float width) { float x = (t - at) / width; return Mathf.Exp(-0.5f * x * x); }
        private static float Smoother(float t) { t = Mathf.Clamp01(t); return t * t * t * (t * (t * 6.0f - 15.0f) + 10.0f); }
        /// <summary>0 to 1 past the mark and back (a reach that overshoots and settles).</summary>
        private static float Overshoot(float t) { t = Mathf.Clamp01(t) - 1.0f; const float k = 1.4f; return 1.0f + t * t * ((k + 1.0f) * t + k); }

        // ------------------------------------------------------------------ 05 THE TAYA TAUNTS (docs/TRAILER_PROMPTS.md)
        //
        // Trailer 0:04.53 to 0:06.03, 90 frames. The cold open's match on TONDO (scene Eskinita), RAGO as taya: the human
        // in seat 1 as YASMIN, which puts RAGO in seat 0, round 1's taya, so his TAYA tag and ring show as an attacker
        // sees them. He stands beside the can and plays COME AT ME (`tpose`, his own fitted EmoteMotion.ComeOn) at the
        // lens. The clip's strongest pose, the arms thrown wide (0.76 s into it), lands on frame 68 (the 5.66 beat): the
        // clip starts on frame 22.4, the first beat, never sped up. A 35 mm equivalent (32.3 degrees vertical) at his
        // chest height; frames 0 to 15 still, then a push from a medium shot to a medium close-up that eases in and is
        // still moving at the cut. The attackers stand on their marks behind the lens, out of frame.
        // NOT AS WRITTEN: the throw line is 8 m from the can, where a 35 mm lens at chest height makes a full shot and
        // the line itself falls below the frame, so the slippers on it cannot be in this frame. Reported, not faked.

        private static Shot TayaTaunts()
        {
            var shot = new Shot { Number = 5, Slug = "taya-taunts", Map = SceneFlow.Eskinita, Length = 1.5f, HandleFrames = 12, FilmIn = 1.0f,
                                  Match = true, Mode = GameMode.HeroStrike, HumanSeat = 1, HumanPick = "cheska",
                                  Source = "live Hero Strike match on Eskinita, round 1, clock held; RAGO's COME AT ME (EmoteMotion.ComeOn) sampled on the shot clock" };
            const float Fov = 32.3f, StillUntil = 16.0f / 60.0f, PushTo = 89.0f / 60.0f, PeakAt = 68.0f / 60.0f, PeakInClip = 0.76f;
            const float EmoteFrom = PeakAt - PeakInClip;   // 0.373 s, frame 22.4: the first beat
            int taya = 0;
            Animator rago = null; AnimationClip idle = null, comeOn = null;
            Transform[] bones = null;
            Vector3 startEye = default, endEye = default, startAim = default, endAim = default;
            shot.Prepare = s =>
            {
                taya = MatchRules.DefenderSlotFor(1);
                var body = s.Players[taya];
                s.Note($"taya seat {taya}: {body.CharacterName()}");
                // Guarding: beside the can on the attackers' side of it, facing them and the lens, so the can stands just
                // behind his legs (r1 put him behind it, and from his chest height the nearer can fell under the frame).
                Vector3 at = s.Centre + new Vector3(0.7f, 0.0f, 0.55f); at.y = body.transform.position.y;
                Vector3 toLens = new Vector3(-0.12f, 0.0f, 1.0f).normalized;
                // The motor's own move (its CharacterController fights a transform write: r1 and r2 stayed on the mark).
                body.Teleport(at);
                body.transform.rotation = Quaternion.LookRotation(toLens);
                body.GetComponent<CharacterVisual>()?.SnapRemoteTransform();
                s.Note($"RAGO moved to {body.transform.position:F2}, the can at {s.Centre:F2}");
                var model = body.GetComponent<CharacterVisual>().Model;
                rago = model.GetComponentInChildren<Animator>(true);
                var art = RosterBook.Load().FindPersonArt("sean");
                comeOn = art.Clips.FirstOrDefault(c => c != null && c.name == EmoteMotion.ComeOn);
                idle = art.Clips.FirstOrDefault(c => c != null && c.name == "idle");
                Assert.IsNotNull(comeOn, "RAGO has no " + EmoteMotion.ComeOn + ": bake the emotes (EmoteMotionAuthor) first");
                bones = rago.GetComponentsInChildren<Transform>(true);
                s.Note($"RAGO's COME AT ME: {comeOn.name}, {comeOn.length:F2} s, from shot {EmoteFrom:F3} s (frame {EmoteFrom * 60.0f:F1}), arms wide at frame {PeakAt * 60.0f:F0}");
            };
            bool framed = false;
            Renderer[] tags = null;
            shot.Pose = (s, t) =>
            {
                // No "P1 · TAYA" over his head (owner, 2026-11-03): the trailer's UI stays hidden. The nameplates' words are
                // `TextMesh`es the nameplate shows again every frame, so they go off here, just before each frame is taken.
                // The role ring at his feet stays: it is on the court, not text.
                if (tags == null)
                    tags = Object.FindObjectsByType<CharacterNameplate>().SelectMany(n => n.GetComponentsInChildren<TextMesh>(true))
                        .Select(m => (Renderer)m.GetComponent<MeshRenderer>()).Where(r => r != null).ToArray();
                foreach (var tag in tags) if (tag != null) tag.enabled = false;
                // His body: the rig's idle until the beat, then COME ON, a six-frame blend between, on the shot clock.
                float into = t - EmoteFrom;
                float w = Smooth((into + 0.05f) / 0.1f);
                if (w <= 0.0f) idle.SampleAnimation(rago.gameObject, Mathf.Repeat(t + 1.0f, idle.length));
                else if (w >= 1.0f) comeOn.SampleAnimation(rago.gameObject, Mathf.Repeat(into, comeOn.length));
                else
                {
                    idle.SampleAnimation(rago.gameObject, Mathf.Repeat(t + 1.0f, idle.length));
                    var pose = bones.Select(b => (b.localPosition, b.localRotation)).ToArray();
                    comeOn.SampleAnimation(rago.gameObject, Mathf.Max(0.0f, into));
                    for (int i = 0; i < bones.Length; i++)
                    {
                        bones[i].localPosition = Vector3.Lerp(pose[i].localPosition, bones[i].localPosition, w);
                        bones[i].localRotation = Quaternion.Slerp(pose[i].localRotation, bones[i].localRotation, w);
                    }
                }
                if (!framed)
                {
                    // Framed once off his drawn height (idle): from a medium shot with his TAYA tag over him, to a medium
                    // close-up from mid-chest to just over the tag.
                    framed = true;
                    var b = s.Drawn(taya);
                    Vector3 feet = s.Players[taya].transform.position;
                    Vector3 toLens = s.Players[taya].transform.forward;
                    float height = b.max.y - feet.y, tag = b.max.y + 0.45f;
                    float half = Mathf.Tan(Fov * 0.5f * Mathf.Deg2Rad);
                    float fromWide = feet.y - 0.15f, fromClose = feet.y + 0.42f * height;
                    startAim = new Vector3(b.center.x, (fromWide + tag) * 0.5f, b.center.z);
                    endAim = new Vector3(b.center.x, (fromClose + tag) * 0.5f, b.center.z);
                    float chest = feet.y + 0.6f * height;
                    startEye = new Vector3(startAim.x, chest, startAim.z) + toLens * ((tag - fromWide) * 0.5f / half);
                    endEye = new Vector3(endAim.x, chest, endAim.z) + toLens * ((tag - fromClose) * 0.5f / half);
                    s.Note($"RAGO {height:F2} m tall; eye {startEye:F2} ({Vector3.Distance(startEye, startAim):F2} m) to {endEye:F2} ({Vector3.Distance(endEye, endAim):F2} m), chest height {chest - feet.y:F2} m");
                }
                // Still to frame 15; from 16 the push eases in and is still moving into the cut (it ends on frame 89).
                float u = Mathf.Clamp01((t - StillUntil) / (PushTo - StillUntil));
                float push = u * u * (3.0f - u) * 0.5f;
                Vector3 eye = Vector3.Lerp(startEye, endEye, push), aim = Vector3.Lerp(startAim, endAim, push);
                s.Shoot(eye, Quaternion.LookRotation(aim - eye), Fov);
            };
            return shot;
        }

        // ------------------------------------------------------------------ 48 THE KEY ART PLATE (docs/TRAILER_PROMPTS.md)
        //
        // Trailer 1:00.80 to 1:03.12, 139 frames, under the logo (tools/render_trailer_logo.py lays the logo, the white and the
        // call to action over this plate). The nine heroes on TONDO at dusk, in a loose arc round the can on its chalk, RAGO
        // in the taya's place by the can, each in a pose from their own emote set and each with the game's slipper in the
        // hand the game holds it in (`CharacterVisual`'s palm fit). Their current models and materials, through the roster.
        // The seats are Classic street players, out of sight under TondoFilm's SAMPAY (so no hero is a seat and every hero
        // can stand here). The light is the film's dusk held at film age 3.0. A very slow push for the whole plate, and a
        // 4K still of its middle. Paete and Phaister take their emote set's authored poses, nothing more.

        private static readonly (string id, string clip, float at)[] KeyArtCast =
        {
            // Roster.HeroPeople order, left to right on screen, RAGO taken out to the middle.
            ("dante", "emote-goodgame", 1.90f), ("cheska", "emote-cheer", 0.62f), ("zack", "emote-streetdance", 1.20f), ("nemu", "emote-cheer", 0.94f),
            ("phaister", "emote-goodgame", 1.90f), ("rafi", "emote-cheer", 0.20f), ("amihan", "emote-streetdance", 1.50f), ("paete", "emote-goodgame", 1.90f),
        };

        private static Shot KeyArt()
        {
            var shot = new Shot { Number = 48, Slug = "key-art", Map = SceneFlow.Eskinita, Length = 139.0f / 60.0f, HandleFrames = 12,
                                  FilmIn = 3.0f, FilmRate = 0.0f, Mode = GameMode.Classic, Titled = MapFilm.OverlayPart.None, Still4KAt = 1.15f,
                                  Source = "TondoFilm's dusk held at film age 3.0 (SAMPAY: seats out of sight); nine roster heroes posed from their emote sets" };
            var cast = new List<(Staging.Extra hero, Vector3 at, float yaw, AnimationClip clip, float t, string id)>();
            Vector3 startEye = default, endEye = default, aim = default;
            const float Fov = 38.0f;
            shot.Prepare = s =>
            {
                s.Hide<Renderer>("_chalkLine");
                var line = s.Field<Renderer>("_realLine"); if (line != null) line.enabled = true;
                // Low and looking a little up: the nine in the lower half of the frame, the bunting and the sky over
                // them for the logo (r1 put their heads where the logo goes).
                startEye = s.Centre + new Vector3(0.0f, 1.25f, 8.4f);
                endEye = s.Centre + new Vector3(0.0f, 1.2f, 7.5f);
                aim = s.Centre + new Vector3(0.0f, 1.75f, -1.0f);
                var book = RosterBook.Load();
                var slipper = Object.FindObjectsByType<Slipper>().FirstOrDefault();
                // A wide, shallow arc that opens toward the lens: RAGO by the can on its south side, four either side,
                // each a step nearer the lens than the one inside, so nobody stands behind anybody (r1's tight arc hid two).
                var places = new List<(string id, string clip, float t, Vector3 at)>();
                places.Add(("sean", EmoteMotion.ComeOn, 0.80f, s.Centre + new Vector3(0.0f, 0.0f, -0.65f)));
                float[] across = { 4.5f, 3.4f, 2.3f, 1.2f }, deep = { -0.25f, -0.75f, -1.25f, -1.75f };   // outer first
                for (int k = 0; k < 8; k++)
                {
                    int i = k < 4 ? k : 7 - k;          // the right side mirrors the left, inner to outer
                    float side = k < 4 ? 1.0f : -1.0f;   // +x is screen left from the north
                    places.Add((KeyArtCast[k].id, KeyArtCast[k].clip, KeyArtCast[k].at, s.Centre + new Vector3(side * across[i], 0.0f, deep[i])));
                }
                foreach (var (id, clipName, t, at0) in places)
                {
                    var hero = s.Person("Key art " + id, 1.0f, id);
                    if (hero == null) { s.Note($"{id}: no roster rig, left out"); continue; }
                    Vector3 at = at0; at.y = s.Floor(at);
                    Vector3 toLens = Vector3.ProjectOnPlane(startEye - at, Vector3.up).normalized;
                    float yaw = Quaternion.LookRotation(toLens).eulerAngles.y;
                    var clip = book.FindPersonArt(id).Clips.FirstOrDefault(c => c != null && c.name == clipName);
                    if (clip == null) s.Note($"{id}: no {clipName}");
                    hero.Pose(at, yaw);
                    if (clip != null) clip.SampleAnimation(hero.Model, t);
                    var hand = s.HandAnchor(hero);
                    if (hand != null && slipper != null) s.Hold(slipper, hand, id);
                    s.Note($"{id} at {at:F2}, {clipName} at {t:F2} s, hand {(hand != null ? "fitted" : "none")}");
                    cast.Add((hero, at, yaw, clip, t, id));
                }
            };
            shot.Pose = (s, t) =>
            {
                foreach (var c in cast)
                {
                    c.hero.Show(true);
                    c.hero.Pose(c.at, c.yaw);
                    if (c.clip != null) c.clip.SampleAnimation(c.hero.Model, c.t);
                }
                // A very slow push the whole plate long: linear, so it never seems to stop under the logo.
                float u = Mathf.Clamp01((t + 0.2f) / (139.0f / 60.0f + 0.4f));
                Vector3 eye = Vector3.Lerp(startEye, endEye, u);
                s.Shoot(eye, Quaternion.LookRotation(aim - eye), Fov);
            };
            return shot;
        }

        // ------------------------------------------------------------------ THE ULTIMATES, LIVE (shots 35 to 43)
        //
        // docs/TRAILER_PROMPTS.md: every ultimate captured live, the whole cast recorded first, from the press to the hand-back,
        // then cut to the shot's window (tools/cut_trailer_shot.py). A real Hero Strike round on the shot's map: the hero in
        // the human seat, the rivals' seats bound to the shot's heroes (kit and body, as the kit probes do), everyone placed,
        // the bar filled, the ultimate pressed through the hero's own Intent. Game time is stepped at 60 x `UltShutter` a
        // second (`Time.captureFramerate`) and the cutscene's clock is the frame clock (`SharedUltimatePhase.FilmClock`), so
        // it all plays at true speed however slowly the editor draws; every output frame is the average of `UltShutter`
        // steps across a 180 degree shutter. Two views of the one cast, each 1920x1080 from 3840x2160:
        //   owner/  the hero's screen: the cutscene's own picture ("UltimateScene") while it is up, the live camera otherwise;
        //   wide/   a spectator camera the shot directs.
        // events.csv logs, per output frame, the cutscene's state and each rival's height, speed and status, to find the
        // frames the prompt syncs to.

        internal sealed class UltShot
        {
            public int Number; public string Slug, Map, Hero; public string[] Rivals;
            /// <summary>Marks on the court, as offsets from the can (`Round.Lata`): each body is dropped onto the floor under its
            /// mark (the Arena's court is raised over a pit, so a world height is never right on every map).</summary>
            public Vector3 HeroAt, Aim; public Vector3[] RivalsAt;
            public float Lead = 0.6f, Seconds = 11.0f;
            /// <summary>Physics steps a second while recording; at least 60 so every frame gets one (see `RecordUltimate`).</summary>
            public float PhysicsHz = 60.0f * UltShutter;
            /// <summary>The spectator camera at `t` seconds after the press, given the can: eye, look-at, vertical field of view.</summary>
            public System.Func<float, Vector3, CharacterMotor, CharacterMotor[], (Vector3 eye, Vector3 look, float fov)> Wide;
            public System.Action<CharacterMotor, CharacterMotor[]> Setup;
            /// <summary>Every game step before it runs, at `t` seconds from the press: the rivals' own input (a run, a turn).</summary>
            public System.Action<float, CharacterMotor, CharacterMotor[]> Drive;
            /// <summary>The shot's own event column in events.csv (RAGO's: lava bombs in the air), read once a frame.</summary>
            public System.Func<string> Probe;
            public string Folder => Path.Combine("Logs", "trailer", $"{Number:00}_{Slug}");
        }

        private const int UltShutter = 3;

        /// <summary>The floor under a mark (`can` + `offset`): the first surface below 6 m over it that is not a body.</summary>
        private static Vector3 OnCourt(Vector3 can, Vector3 offset)
        {
            Vector3 at = can + new Vector3(offset.x, 0.0f, offset.z);
            foreach (var hit in Physics.RaycastAll(at + Vector3.up * 6.0f, Vector3.down, 20.0f, ~0, QueryTriggerInteraction.Ignore).OrderBy(h => h.distance))
                if (hit.collider.GetComponentInParent<CharacterMotor>() == null) return hit.point + Vector3.up * (0.02f + offset.y);
            return at + Vector3.up * offset.y;
        }

        private static CharacterMotor BindSeat(int slot, string hero, Vector3 at, Vector3 face)
        {
            var who = GameServices.Round.PlayerAt(slot);
            who.CharacterIndex = Roster.IndexIn(Roster.HeroPeople, hero);
            who.AbilitySystem.BindHero(hero);
            var art = RosterBook.Load().FindPersonArt(hero);
            who.GetComponent<CharacterVisual>().ApplyModel(art.Model, art.Tint, art.Clips, art.Palette, art.PetModel);
            if (slot == GameLaunch.SoloSeat)
                foreach (var arms in Object.FindObjectsByType<ViewmodelArms>()) arms.MatchCharacter(who);
            who.Teleport(at);
            Vector3 to = Vector3.ProjectOnPlane(face - at, Vector3.up);
            if (to.sqrMagnitude > 0.01f) who.transform.rotation = Quaternion.LookRotation(to);
            who.Intent.Clear(); who.Intent.Parked = false;
            return who;
        }

        private IEnumerator RecordUltimate(UltShot shot)
        {
            var disk = new DriveInfo(Path.GetPathRoot(Path.GetFullPath("Logs")));
            Assert.Greater(disk.AvailableFreeSpace, 8L << 30, "under 8 GB free: two 1080p views of an 11 s cast");
            yield return LoadMatch(new Shot { Map = shot.Map, Mode = GameMode.HeroStrike, HumanSeat = 1, HumanPick = shot.Hero });
            foreach (var switcher in Object.FindObjectsByType<DebugPlayerSwitcher>()) switcher.enabled = false;
            var round = GameServices.Round;
            Vector3 can = round.Lata != null ? round.Lata.transform.position : Object.FindFirstObjectByType<Lata>().transform.position;
            Vector3 heroAt = OnCourt(can, shot.HeroAt), aim = OnCourt(can, shot.Aim);
            var hero = BindSeat(1, shot.Hero, heroAt, aim);
            var rivals = new List<CharacterMotor>();
            int r = 0;
            foreach (int slot in new[] { 0, 2, 3 })
            {
                rivals.Add(BindSeat(slot, shot.Rivals[r], OnCourt(can, shot.RivalsAt[r]), heroAt));
                rivals[r].IsBot = true;
                r++;
            }
            hero.IsBot = false;
            hero.Intent.AimPoint = aim;
            shot.Setup?.Invoke(hero, rivals.ToArray());
            for (int i = 0; i < 30; i++) yield return null;
            hero.AbilitySystem.Kit.AddUltimateCharge(100);

            string folder = shot.Folder;
            foreach (string view in new[] { "owner", "wide" })
            {
                string into = Path.Combine(folder, view);
                if (Directory.Exists(into)) foreach (var old in Directory.GetFiles(into, "*.png")) File.Delete(old);
                Directory.CreateDirectory(into);
            }
            var wideCam = new GameObject("~TrailerWide").AddComponent<Camera>();
            wideCam.CopyFrom(Camera.main); wideCam.enabled = false; wideCam.tag = "Untagged"; wideCam.cullingMask &= ~(1 << 5);
            wideCam.gameObject.AddComponent<ColourGrade>().AdoptFromScene();
            var hdr = new RenderTexture(Width * 2, Height * 2, 24, RenderTextureFormat.DefaultHDR, RenderTextureReadWrite.Linear);
            hdr.filterMode = FilterMode.Bilinear;
            var ldr = new RenderTexture(Width, Height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            var read = new Texture2D(Width, Height, TextureFormat.RGB24, false);
            var image = new Texture2D(Width, Height, TextureFormat.RGB24, false);
            var sums = new[] { new int[Width * Height * 3], new int[Width * Height * 3] };
            var average = new Color32[Width * Height];
            void Add(Texture source, int view)
            {
                Graphics.Blit(source, ldr);
                var active = RenderTexture.active; RenderTexture.active = ldr;
                read.ReadPixels(new Rect(0, 0, Width, Height), 0, 0); read.Apply(false); RenderTexture.active = active;
                var px = read.GetPixels32(); var sum = sums[view];
                for (int p = 0, q = 0; p < px.Length; p++, q += 3) { sum[q] += px[p].r; sum[q + 1] += px[p].g; sum[q + 2] += px[p].b; }
            }
            void Write(int view, string path)
            {
                var sum = sums[view];
                for (int p = 0, q = 0; p < average.Length; p++, q += 3)
                    average[p] = new Color32((byte)((sum[q] + UltShutter / 2) / UltShutter), (byte)((sum[q + 1] + UltShutter / 2) / UltShutter), (byte)((sum[q + 2] + UltShutter / 2) / UltShutter), 255);
                image.SetPixels32(average); image.Apply(false);
                File.WriteAllBytes(path, image.EncodeToPNG());
                System.Array.Clear(sum, 0, sum.Length);
            }

            var events = new System.Text.StringBuilder("frame,seconds_from_press,scene_up,phase_up,ult_active,phase_shot,phase_age,probe,ult_charge," + string.Join(",", rivals.Select((x, i) => $"r{i}_y,r{i}_speed,r{i}_status")) + "\n");
            var lastAt = rivals.Select(x => x.transform.position).ToArray();
            int previousRate = Time.captureFramerate;
            float previousFixed = Time.fixedDeltaTime;
            Time.captureFramerate = 60 * UltShutter;
            // Physics on the step too: at the game's 50 Hz a 60 fps frame of three 180 Hz steps sometimes got no physics
            // tick at all, and every thrown body stood still for that frame (take 2's judder).
            Time.fixedDeltaTime = 1.0f / shot.PhysicsHz;
            int step = 0; double clockBase = Time.realtimeSinceStartupAsDouble;
            SharedUltimatePhase.FilmClock = () => clockBase + step / (60.0 * UltShutter);
            int frames = Mathf.RoundToInt((shot.Lead + shot.Seconds) * 60.0f);
            int sceneFrames = 0;
            try
            {
                for (int f = 0; f < frames; f++)
                {
                    bool sceneUp = false, phaseUp = false;
                    for (int k = 0; k < UltShutter; k++, step++)
                    {
                        float t = step / (60.0f * UltShutter) - shot.Lead;   // seconds from the press
                        hero.Intent.AimPoint = aim;
                        hero.Intent.Set(Verb.Ultimate, t >= 0.0f && t < 0.25f);
                        shot.Drive?.Invoke(t, hero, rivals.ToArray());
                        yield return null;
                        // No "P1 · TAYA" or names over heads: the nameplates show their words again every frame (shot 5).
                        foreach (var plate in Object.FindObjectsByType<CharacterNameplate>())
                            foreach (var words in plate.GetComponentsInChildren<TextMesh>(true))
                                if (words.TryGetComponent<MeshRenderer>(out var mesh)) mesh.enabled = false;
                        // Nor the status icons over heads (`StatusOverhead`): take 1 of shot 36 had black discs over the rivals.
                        foreach (var over in Object.FindObjectsByType<StatusOverhead>())
                            foreach (var badge in over.GetComponentsInChildren<SpriteRenderer>(true)) badge.enabled = false;
                        var phase = SharedUltimatePhase.Instance;
                        phaseUp |= phase != null && phase.Active;
                        RawImage scene = null;
                        foreach (var picture in Object.FindObjectsByType<RawImage>())
                            if (picture.name == "UltimateScene" && picture.isActiveAndEnabled && picture.texture != null) scene = picture;
                        if (scene != null) { Add(scene.texture, 0); sceneUp = true; }
                        else { PaeteKitPlayProbe.RenderFilmView(Camera.main, hdr); Add(hdr, 0); }
                        var (eye, look, fov) = shot.Wide(t, can, hero, rivals.ToArray());
                        wideCam.transform.SetPositionAndRotation(eye, Quaternion.LookRotation(look - eye)); wideCam.fieldOfView = fov;
                        PaeteKitPlayProbe.RenderFilmView(wideCam, hdr); Add(hdr, 1);
                    }
                    Write(0, Path.Combine(folder, "owner", $"frame_{f:00000}.png"));
                    Write(1, Path.Combine(folder, "wide", $"frame_{f:00000}.png"));
                    if (sceneUp) sceneFrames++;
                    float now = f / 60.0f - shot.Lead;
                    var line = new List<string> { f.ToString(), F(now), sceneUp ? "1" : "0", phaseUp ? "1" : "0",
                                                   hero.AbilitySystem.Kit.Ultimate.IsActive ? "1" : "0", UltimatePhaseView.LastShot.ToString(), F(UltimatePhaseView.LastAge), shot.Probe != null ? shot.Probe() : "-",
                                                   F(hero.AbilitySystem.Kit.UltimateCharge) };
                    for (int i = 0; i < rivals.Count; i++)
                    {
                        var x = rivals[i];
                        float speed = (x.transform.position - lastAt[i]).magnitude * 60.0f; lastAt[i] = x.transform.position;
                        string status = x.IsFrozen ? "frozen" : x.IsRooted ? "rooted" : x.IsWhirled ? "whirled" : x.IsPossessed ? "possessed" : x.IsZapped ? "zapped" : x.IsTagged ? "tagged" : "-";
                        line.Add(F(x.transform.position.y)); line.Add(F(speed)); line.Add(status);
                    }
                    events.AppendLine(string.Join(",", line));
                }
            }
            finally
            {
                SharedUltimatePhase.FilmClock = null;
                Time.captureFramerate = previousRate; Time.fixedDeltaTime = previousFixed;
                Object.Destroy(wideCam.gameObject); hdr.Release(); ldr.Release();
                Object.Destroy(read); Object.Destroy(image);
            }
            File.WriteAllText(Path.Combine(folder, "events.csv"), events.ToString());
            string report = $"ult {shot.Number:00} {shot.Slug}: {shot.Map}, {shot.Hero} vs {string.Join(", ", shot.Rivals)}; {frames} frames at 60 fps " +
                            $"(press on frame {Mathf.RoundToInt(shot.Lead * 60.0f)}), {UltShutter} steps a frame; the cutscene up for {sceneFrames} frames\n" +
                            "shots: " + UltimatePhaseView.LastShotReport + "\n";
            File.WriteAllText(Path.Combine(folder, "shot.txt"), report);
            Debug.Log("[TrailerShots] " + report);
            Assert.Greater(sceneFrames + frames, 0);
        }

        // ---- 35 RAGO, MAYON AWAKENS: PHILIPPINE ARENA, three rivals within 9 m; the burst, then a lava bomb on frame 67.
        private static UltShot RagoMayon()
        {
            // The mountain comes up where RAGO looks, pushed clear of the can (`SeanVolcano.SpotFor`), and the cutscene gathers
            // everyone before the hand-back, so take 3's fixed camera had it at the frame's edge. The camera is set from the
            // scene instead: just before the burst (5.0 s after the press; take 2 and 3 burst at 5.25) it takes the mountain's
            // spot (the live one if it is up, else the game's own SpotFor) and the gathered bodies, and stands off to the
            // court side of them, low, square to the line from the mountain to the rivals, so the throw reads across the frame.
            Vector3 spot = default, centre = default, side = default;
            bool locked = false, real = false;
            return new UltShot
            {
                Number = 35, Slug = "rago-mayon-awakens", Map = SceneFlow.Arena, Hero = "sean", Rivals = new[] { "cheska", "zack", "paete" },
                // Marks from the can: RAGO 4 m behind the volcano's spot, looking at it; the three within 3 m of the spot.
                HeroAt = new Vector3(0.0f, 0.0f, -5.0f), Aim = new Vector3(0.0f, 0.0f, -1.0f),
                RivalsAt = new[] { new Vector3(-2.5f, 0.0f, 0.2f), new Vector3(2.6f, 0.0f, -0.3f), new Vector3(0.4f, 0.0f, 1.8f) },
                Seconds = 11.0f,
                Wide = (t, can, hero, rivals) =>
                {
                    var volcano = Object.FindFirstObjectByType<SeanVolcano>();
                    if ((!locked && t >= 5.0f) || (volcano != null && !real))
                    {
                        spot = volcano != null ? volcano.Foot : SeanVolcano.SpotFor(hero.transform.position, hero.transform.forward, hero.Intent.AimPoint, GameServices.Round.Lata);
                        real = volcano != null;
                        Vector3 bodies = rivals.Aggregate(Vector3.zero, (sum, x) => sum + x.transform.position) / rivals.Length;
                        if (!locked) centre = (spot * 2.0f + bodies + hero.transform.position) / 4.0f;
                        Vector3 across = Vector3.ProjectOnPlane(bodies - spot, Vector3.up);
                        if (across.sqrMagnitude < 0.01f) across = Vector3.right;
                        Vector3 normal = Vector3.Cross(Vector3.up, across.normalized);
                        if (!locked) side = Vector3.Dot(normal, can - centre) >= 0.0f ? normal : -normal;
                        locked = true;
                    }
                    if (!locked) return (can + new Vector3(11.0f, 1.3f, -3.0f), can + new Vector3(0.0f, 2.0f, -1.0f), 52.0f);
                    float k = Smooth((t - 4.9f) / 2.6f);
                    Vector3 eye = centre + side * Mathf.Lerp(11.5f, 9.5f, k) + Vector3.up * Mathf.Lerp(1.3f, 1.15f, k);
                    return (eye, centre + Vector3.up * 2.3f, 50.0f);
                },
                Probe = () => Object.FindObjectsByType<SeanVolcanicBomb>().Length.ToString(),
            };
        }

        // ---- 36 YASMIN, ABSOLUTE ZERO: BGC (Kanto); three rivals mid-run across the court, an attacker sprinting back in with
        // his slipper; every one Frozen in the same frame (the hand-back), then a slow push on the frozen tableau.
        private static UltShot YasminAbsoluteZero()
        {
            Vector3 centre = default, toEye = default;
            bool locked = false; float lockedAt = float.NaN, spread = 0.0f;
            var heading = new[] { new Vector3(1.0f, 0.0f, 0.15f), new Vector3(-0.35f, 0.0f, -1.0f), new Vector3(-1.0f, 0.0f, 0.4f) };
            return new UltShot
            {
                Number = 36, Slug = "yasmin-absolute-zero", Map = SceneFlow.Kanto, Hero = "cheska", Rivals = new[] { "sean", "zack", "dante" },
                HeroAt = new Vector3(0.0f, 0.0f, -7.0f), Aim = new Vector3(0.0f, 0.0f, 0.0f),
                RivalsAt = new[] { new Vector3(-5.5f, 0.0f, 1.5f), new Vector3(1.2f, 0.0f, 5.5f), new Vector3(5.0f, 0.0f, -1.0f) },
                // Her authored cutscene is 11.9 s (Resources/UltimateIntros/cheska.txt); the freeze lands as it hands back.
                Seconds = 14.5f,
                // Every rival sprints along its own line the whole take; the freeze stops them wherever the stride is.
                Drive = (t, hero, rivals) =>
                {
                    for (int i = 0; i < rivals.Length; i++)
                    {
                        if (rivals[i].IsFrozen) continue;
                        rivals[i].transform.rotation = Quaternion.LookRotation(heading[i]);
                        rivals[i].Intent.Move = Vector2.up; rivals[i].Intent.Set(Verb.Sprint, true);
                    }
                },
                // Three-quarter high on the three, locked on the frame they freeze, then a slow push in. Take 2's 8 to 10 m
                // from their centre put the nearest frozen body across the lens, so the distance follows their spread.
                Wide = (t, can, hero, rivals) =>
                {
                    bool frozen = rivals.All(x => x.IsFrozen);
                    if (!locked || !frozen)
                    {
                        centre = rivals.Aggregate(Vector3.zero, (sum, x) => sum + x.transform.position) / rivals.Length;
                        spread = rivals.Max(x => Vector3.ProjectOnPlane(x.transform.position - centre, Vector3.up).magnitude);
                        Vector3 fromHero = Vector3.ProjectOnPlane(centre - hero.transform.position, Vector3.up).normalized;
                        toEye = (Quaternion.AngleAxis(40.0f, Vector3.up) * fromHero).normalized;
                        locked = frozen;
                    }
                    if (frozen && float.IsNaN(lockedAt)) lockedAt = t;
                    float k = locked ? Smooth((t - lockedAt) / 1.4f) : 0.0f;
                    float far = 8.0f + spread * 1.6f, near = far * 0.82f;
                    float d = Mathf.Lerp(far, near, k);
                    return (centre + toEye * d + Vector3.up * d * 0.62f, centre + Vector3.up * 0.7f, 42.0f);
                },
                Probe = () => GameServices.Round.Players.Count(x => x != null && x.IsFrozen).ToString(),
            };
        }

        // ---- 37 AMIHAN, AIRBURST: SITANGKAI (LagoonCove); three rivals in front of her and two loose slippers on the court.
        // The monsoon is released in her cutscene and play resumes on the hit, so the throw lands on the hand-back.
        private static UltShot AmihanAirburst()
        {
            Vector3 look = default; bool started = false, dropped = false;
            return new UltShot
            {
                Number = 37, Slug = "amihan-airburst", Map = SceneFlow.LagoonCove, Hero = "amihan", Rivals = new[] { "sean", "nemu", "phaister" },
                // Marks from the can: AMIHAN 6 m behind it facing through it; the three inside her 60 degree fan, 7 to 10 m out.
                HeroAt = new Vector3(0.0f, 0.0f, -6.0f), Aim = new Vector3(0.0f, 0.0f, 4.0f),
                RivalsAt = new[] { new Vector3(-2.2f, 0.0f, 1.5f), new Vector3(2.4f, 0.0f, 2.5f), new Vector3(0.3f, 0.0f, 4.2f) },
                // Her authored cutscene is 5.9 s (Resources/UltimateIntros/amihan.txt); 3 s of flight after the hand-back.
                Seconds = 9.0f,
                // AIRBURST lifts a resting slipper at 7 m/s: one 180 Hz step rises 3.9 cm, under its 4.5 cm rest height
                // (`Balance.SlipperRestHeight`), so takes 1 to 3 landed both on their first step. 90 Hz rises 7.8 cm (the game's 50, 14).
                PhysicsHz = 90.0f,
                // The rivals stand facing her until the wind takes them. Take 1 dropped the slippers before the take's 30 settling
                // frames and the bots had them back in hand by the cast (none flew), so they go down here, once, with the rivals held.
                Drive = (t, hero, rivals) =>
                {
                    if (!dropped)
                    {
                        dropped = true;
                        Vector3 can = GameServices.Round.Lata.transform.position;
                        var marks = new[] { new Vector3(-1.2f, 0.0f, -1.8f), new Vector3(1.4f, 0.0f, -0.6f) };
                        int n = 0;
                        foreach (var shoe in Object.FindObjectsByType<Slipper>().Where(x => x.Holder != hero && x.gameObject.activeInHierarchy).Take(2))
                        {
                            shoe.HostSnatch();
                            shoe.HostFinishMapRecoveryAt(OnCourt(can, marks[n++]));
                        }
                    }
                    foreach (var x in rivals) if (!x.IsWhirled) { x.Intent.Clear(); x.transform.rotation = Quaternion.LookRotation(Vector3.ProjectOnPlane(hero.transform.position - x.transform.position, Vector3.up)); }
                    // She faces the can through the press: the fan's direction is her body's facing (`AbilityContext.Forward`).
                    Vector3 lata = GameServices.Round.Lata.transform.position;
                    if (t < 0.3f) hero.transform.rotation = Quaternion.LookRotation(Vector3.ProjectOnPlane(lata - hero.transform.position, Vector3.up));
                },
                // Behind, above and to her left, so the fan crosses the frame left to right; after the hit the look follows the
                // thrown bodies out, lagging, so at least one crosses the frame edge.
                Wide = (t, can, hero, rivals) =>
                {
                    Vector3 at = hero.transform.position;
                    Vector3 fwd = Vector3.ProjectOnPlane(GameServices.Round.Lata.transform.position - at, Vector3.up).normalized;
                    if (fwd.sqrMagnitude < 0.01f) fwd = hero.transform.forward;
                    Vector3 right = Vector3.Cross(Vector3.up, fwd);
                    Vector3 bodies = rivals.Aggregate(Vector3.zero, (sum, x) => sum + x.transform.position) / rivals.Length;
                    Vector3 want = Vector3.Lerp(at, bodies, 0.55f) + Vector3.up * 1.2f;
                    if (!started) { look = want; started = true; }
                    look = Vector3.Lerp(look, want, 0.04f);
                    // The eye stays put: take 1's pull back after the hit brought a court pole across the left of the frame.
                    Vector3 eye = at - fwd * 5.0f - right * 5.5f + Vector3.up * 4.5f;
                    return (eye, look, 50.0f);
                },
                // Slippers by state: Held, Loose, in Flight.
                Probe = () => { var all = Object.FindObjectsByType<Slipper>(); return $"H{all.Count(x => x.State == SlipperState.Held)}L{all.Count(x => x.State == SlipperState.Loose)}F{all.Count(x => x.State == SlipperState.InFlight)}"; },
            };
        }

        // ---- 38 PAETE, MAKILING'S EMBRACE: BANAUE (Payo); capture only (his kit, tree, rig and materials are protected).
        // The guardian tree comes up where he looks, clear of the can; on the hand-back it pulls the three rivals in and Roots them.
        private static UltShot PaeteMakilingsEmbrace()
        {
            Vector3 tree = default, toEye = default; bool locked = false; float lockedAt = float.NaN;
            return new UltShot
            {
                Number = 38, Slug = "paete-makilings-embrace", Map = SceneFlow.Payo, Hero = "paete", Rivals = new[] { "cheska", "nemu", "rafi" },
                // Marks from the can: PAETE 8 m behind it looking at a spot 3 m short of it (the tree goes 5 m ahead of him, clear of
                // the can); the three 5 to 7 m from that spot on three sides, so each slides in from its own direction.
                HeroAt = new Vector3(0.0f, 0.0f, -8.0f), Aim = new Vector3(0.0f, 0.0f, -3.0f),
                RivalsAt = new[] { new Vector3(-5.2f, 0.0f, -1.5f), new Vector3(5.0f, 0.0f, -2.0f), new Vector3(1.0f, 0.0f, 3.8f) },
                // His authored cutscene is 9 s (Resources/UltimateIntros/paete.txt); 2.5 s after the hand-back for the pull and roots.
                Seconds = 11.5f,
                // The rivals stand still, facing the tree's spot, and let the pull take them.
                Drive = (t, hero, rivals) =>
                {
                    Vector3 spot = GameServices.Round.Lata.transform.position + new Vector3(0.0f, 0.0f, -3.0f);
                    foreach (var x in rivals)
                    {
                        x.Intent.Clear();
                        if (t < 0.0f) x.transform.rotation = Quaternion.LookRotation(Vector3.ProjectOnPlane(spot - x.transform.position, Vector3.up));
                    }
                },
                // Three-quarter high, wide enough for the tree and all three rivals: set once the tree stands (its own Centre), from
                // the side of the court away from PAETE, then a slow push as the roots close.
                Wide = (t, can, hero, rivals) =>
                {
                    var sentry = Object.FindFirstObjectByType<PaeteSentry>();
                    if (!locked)
                    {
                        tree = sentry != null ? sentry.Centre : can + new Vector3(0.0f, 0.0f, -3.0f);
                        Vector3 back = Vector3.ProjectOnPlane(tree - hero.transform.position, Vector3.up).normalized;
                        toEye = (Quaternion.AngleAxis(35.0f, Vector3.up) * back).normalized;
                        if (sentry != null) { locked = true; lockedAt = t; }
                    }
                    float k = locked ? Smooth((t - lockedAt - 0.4f) / 1.6f) : 0.0f;
                    float d = Mathf.Lerp(15.0f, 12.5f, k);
                    return (tree + toEye * d + Vector3.up * d * 0.55f, tree + Vector3.up * 1.2f, 45.0f);
                },
                Probe = () => GameServices.Round.Players.Count(x => x != null && x.IsRooted).ToString(),
            };
        }

        // ------------------------------------------------------------------ the harness

        internal sealed class Shot
        {
            public int Number; public string Slug; public string Map; public string Source;
            /// <summary>The shot's length on the trailer's bar; 0.5 s of handles are added either side.</summary>
            public float Length;
            /// <summary>The film's age at the shot's in point, and film seconds per shot second.</summary>
            public float FilmIn, FilmRate = 1.0f;
            /// <summary>The seat whose screen the film plays as (an attacker closes on themselves).</summary>
            public int Viewer = 1;
            /// <summary>What a TITLED pass draws; `None` records the clean pass only.</summary>
            public MapFilm.OverlayPart Titled = MapFilm.OverlayPart.Title | MapFilm.OverlayPart.Taya;
            /// <summary>After the film has begun: build the shot's own staging (owned by the film, gone at `End`).</summary>
            public System.Action<Staging> Prepare;
            /// <summary>Every frame, after the film has posed it: `t` is shot time, 0 at the in point.</summary>
            public System.Action<Staging, float> Pose;
            /// <summary>After the last frame, before the report is written: the staging's own notes.</summary>
            public System.Action<Staging> Done;
            /// <summary>Handles either side of the shot, in frames (the October draft's shots: 30; docs/TRAILER_PROMPTS.md: 12).</summary>
            public int HandleFrames = 30;
            public float Handle => HandleFrames / Fps;
            /// <summary>The shot's own frames, handles excluded: frame 0 is its first frame, as the prompts count.</summary>
            public int Frames => Mathf.RoundToInt(Length * Fps);
            /// <summary>A shot shot in a live match on its map (no film): the round goes live, then the clock is held.</summary>
            public bool Match;
            public GameMode Mode = GameMode.HeroStrike;
            /// <summary>For a match shot: the human's seat and roster pick (the bots' picks follow from it).</summary>
            public int HumanSeat = 1; public string HumanPick;
            /// <summary>Also take one 3840x2160 still at this shot time, after the passes (the key art).</summary>
            public float Still4KAt = float.NaN;
            public string Folder => Path.Combine("Logs", "trailer", $"{Number:00}_{Slug}");
            public float FilmAt(float t) => Mathf.Max(0.0f, FilmIn + t * FilmRate);
        }

        private const float Fps = 60.0f, Handle = 0.5f;
        private const int Width = 1920, Height = 1080;
        /// <summary>
        /// THE TRAILER'S PICTURE, ABOVE THE GAME'S OWN: every frame is rendered at twice the size and box-filtered down
        /// (`FilmFrames` supersample 2), and is the average of `Shutter` sub-frames spread over half a frame (a 180 degree
        /// shutter), each one posed by the film at its own instant: motion blur that follows every body and the lens.
        /// </summary>
        private const int Supersample = 2, Shutter = 5;
        private const float ShutterOpen = 0.5f;

        private IEnumerator Record(Shot shot)
        {
            var disk = new DriveInfo(Path.GetPathRoot(Path.GetFullPath("Logs")));
            Assert.Greater(disk.AvailableFreeSpace, 2L << 30, "under 2 GB free: a 60 fps 1080p pass takes several hundred MB");

            MapFilm film = null;
            if (shot.Match) yield return LoadMatch(shot);
            else yield return MapFilmTests.Load(shot.Map, false, shot.Mode);
            var camera = Camera.main;
            var rig = camera.GetComponent<CameraSystem.CameraRig>();
            if (rig != null) rig.SetActive(false);
            var players = GameServices.Round.Players.OrderBy(p => p.PlayerSlot).ToArray();
            if (!shot.Match)
            {
                film = MatchOpening.For(shot.Viewer) as MapFilm;
                Assert.IsNotNull(film, shot.Map + " has no film");
                MapFilmTests.ArrivalActive(true);
                Assert.IsTrue(film.Begin(camera, players, camera.transform.position, camera.transform.rotation, camera.fieldOfView, false), shot.Map + "'s film refused to begin");
            }

            string folder = shot.Folder;
            Directory.CreateDirectory(folder);
            var staging = new Staging(film, camera, players);
            shot.Prepare?.Invoke(staging);
            if (shot.Match)
            {
                // The staging moved bodies: let the visuals settle on them, then hold everything still.
                for (int i = 0; i < 20; i++) yield return null;
                MapFilmTests.HoldClock(true);
                yield return null;
            }

            // The sound: every cue the film calls (first pass only: a second pass would hear them again), and the map's
            // ambience beds, which play under any film in a match. A match shot is picture only.
            var cues = new List<string> { "shot_time,film_time,clip,gain,pitch,kind" };
            bool listening = true;
            float now = -shot.Handle;
            if (!shot.Match)
            {
                MapFilm.Heard = (clip, age, volume, pitch) =>
                {
                    if (!listening) return;
                    cues.Add(string.Join(",", F(now), F(age), clip.name, F(volume), F(pitch), "cue"));
                };
                foreach (var (clip, gain) in Beds()) cues.Add(string.Join(",", F(-shot.Handle), F(shot.FilmAt(-shot.Handle)), clip, F(gain), F(1.0f), "bed"));
            }

            var passes = new List<(string name, MapFilm.OverlayPart parts)> { ("clean", MapFilm.OverlayPart.None) };
            if (!shot.Match && shot.Titled != MapFilm.OverlayPart.None) passes.Add(("titled", shot.Titled));
            int count = shot.Frames + 2 * shot.HandleFrames;
            var report = new System.Text.StringBuilder();
            report.AppendLine($"shot {shot.Number:00} {shot.Slug}: {shot.Map}, {shot.Frames} frames ({shot.Length:F3} s) + {shot.HandleFrames} frames of handles either side, {count} frames at {Fps:F0} fps, {Width}x{Height} from {Width * Supersample}x{Height * Supersample}, {Shutter} sub-frames over a {ShutterOpen * 360.0f:F0} degree shutter");
            report.AppendLine($"source: {shot.Source}");
            if (film != null) report.AppendLine($"film ages {shot.FilmAt(-shot.Handle):F3} to {shot.FilmAt(shot.Length + shot.Handle):F3} (in point {shot.FilmIn:F3}, rate {shot.FilmRate:F3}); film length {film.Duration:F2}; viewer seat {shot.Viewer}, taya seat {MatchRules.DefenderSlotFor(1)}");
            else report.AppendLine($"live match, {shot.Mode}, human seat {shot.HumanSeat} ({shot.HumanPick}), taya seat {MatchRules.DefenderSlotFor(1)}: " + string.Join(", ", players.Select(p => $"seat {p.PlayerSlot} {p.CharacterName()}")));
            foreach (var line in staging.Notes) report.AppendLine("  " + line);

            float drawnAge = -1.0f;
            _frames = null;
            foreach (var (pass, parts) in passes)
            {
                string into = Path.Combine(folder, pass);
                if (Directory.Exists(into)) foreach (var old in Directory.GetFiles(into, "*.png")) File.Delete(old);
                Directory.CreateDirectory(into);
                _frames = new FilmFrames(camera, Width, Height, Supersample);
                if (film != null)
                {
                    film.OverlayParts = parts;
                    film.HeldAge = shot.FilmAt(-shot.Handle);
                    film.Roll();
                }
                yield return null; yield return null;
                if (pass != "clean") _frames.Flip();
                yield return null;
                _frames.Before = () => { drawnAge = film != null ? film.Age : shot.FilmAt(now); shot.Pose?.Invoke(staging, now); };

                var ages = new float[count];
                var wall = new List<float>();
                var sum = new int[Width * Height * 3];
                var image = new Texture2D(Width, Height, TextureFormat.RGB24, false);
                var average = new Color32[Width * Height];
                _frames.Pixels = pixels =>
                {
                    for (int p = 0, q = 0; p < pixels.Length; p++, q += 3) { sum[q] += pixels[p].r; sum[q + 1] += pixels[p].g; sum[q + 2] += pixels[p].b; }
                };
                float last = Time.realtimeSinceStartup, drift = 0.0f;
                for (int i = 0; i < count; i++)
                {
                    float centre = (i - shot.HandleFrames) / Fps, aged = 0.0f;
                    System.Array.Clear(sum, 0, sum.Length);
                    for (int k = 0; k < Shutter; k++)
                    {
                        now = centre + ((k + 0.5f) / Shutter - 0.5f) * ShutterOpen / Fps;
                        if (film != null) film.HeldAge = shot.FilmAt(now);
                        yield return null;
                        // Clean: no overlay canvas is composited at all (`Take` would put the film's own on the frame).
                        if (pass == "clean") yield return TakeClean("sub");
                        else yield return _frames.Take("sub");
                        aged += drawnAge;
                        drift = Mathf.Max(drift, Mathf.Abs(drawnAge - shot.FilmAt(now)));
                    }
                    for (int p = 0, q = 0; p < average.Length; p++, q += 3)
                        average[p] = new Color32((byte)((sum[q] + Shutter / 2) / Shutter), (byte)((sum[q + 1] + Shutter / 2) / Shutter), (byte)((sum[q + 2] + Shutter / 2) / Shutter), 255);
                    image.SetPixels32(average); image.Apply(false);
                    File.WriteAllBytes(Path.Combine(into, $"frame_{i:00000}.png"), image.EncodeToPNG());
                    ages[i] = aged / Shutter;
                    float t = Time.realtimeSinceStartup; wall.Add(t - last); last = t;
                }
                Object.Destroy(image);
                listening = false;
                _frames.Dispose(); _frames = null;

                // The frame timing: every sub-frame drawn at the clock age it asked for, the frames 1/60 s apart (the first
                // frame's earliest sub-frames sit before film age 0 when the in point is that early, so its own step is not
                // counted), and the wall clock per frame.
                float worst = 0.0f;
                for (int i = 2; i < count; i++) worst = Mathf.Max(worst, Mathf.Abs(ages[i] - ages[i - 1] - shot.FilmRate / Fps));
                report.AppendLine($"{pass}: {count} frames (shot frame 0 is file {shot.HandleFrames}), clock {ages[0]:F4} to {ages[count - 1]:F4}, worst step error {worst * 1000.0f:F3} ms, worst drawn-vs-asked {drift * 1000.0f:F3} ms; wall {wall.Average() * 1000.0f:F0} ms a frame (max {wall.Max() * 1000.0f:F0})");
                Assert.Less(drift, 0.0001f, $"{pass}: a sub-frame was drawn at another clock age than it asked for");
                Assert.Less(worst, 0.0005f, $"{pass}: a frame was off its 1/{Fps:F0} s step");
                Assert.AreEqual(count, Directory.GetFiles(into, "*.png").Length, $"{pass}: frames missing");
            }

            if (!float.IsNaN(shot.Still4KAt))
            {
                // One 4K frame, the scene through its grade, no canvas: posed at its own instant like every frame.
                now = shot.Still4KAt;
                if (film != null) film.HeldAge = shot.FilmAt(now);
                _frames = new FilmFrames(camera, 3840, 2160, 1);
                _frames.Before = () => shot.Pose?.Invoke(staging, now);
                yield return null; yield return null;
                yield return _frames.Take(Path.Combine(folder, "still-4k.png"), false);
                _frames.Dispose(); _frames = null;
                report.AppendLine($"4K still at shot {now:F3} s: {Path.Combine(folder, "still-4k.png")}");
            }

            int noted = staging.Notes.Count;
            shot.Done?.Invoke(staging);
            for (int n = noted; n < staging.Notes.Count; n++) report.AppendLine("  " + staging.Notes[n]);
            File.WriteAllLines(Path.Combine(folder, "cues.csv"), cues);
            File.WriteAllText(Path.Combine(folder, "shot.txt"), report.ToString());
            File.WriteAllText(Path.Combine(folder, "shot.json"), JsonUtility.ToJson(new Receipt
            {
                number = shot.Number, slug = shot.Slug, map = shot.Map, length = shot.Length, handle = shot.Handle, fps = Fps,
                frames = count, handleFrames = shot.HandleFrames, filmIn = shot.FilmIn, filmRate = shot.FilmRate, passes = passes.Select(p => p.name).ToArray(),
            }, true));
            Debug.Log("[TrailerShots] " + report);

            MapFilmTests.ArrivalActive(false);
            MapFilm.Heard = null;
            if (film != null) { film.HeldAge = -1.0f; film.End(); }
            yield return null;
        }

        /// <summary>
        /// A live match on the shot's map, as a player gets it: no map film (camera motion off), the arrival and the
        /// ready gate run, the round goes live with the HUD and the nameplates up; then the bots and the input stop.
        /// The clock is held by `Record` once the staging is placed.
        /// </summary>
        private static IEnumerator LoadMatch(Shot shot)
        {
            LogAssert.ignoreFailingMessages = true;
            SceneFlow.Networked = false;
            SceneFlow.SetSelectedRules(CustomGameRules.Defaults(shot.Mode));
            GameLaunch.SoloSeat = shot.HumanSeat; GameLaunch.GuidedTutorial = false; GameLaunch.Spectator = false; GameLaunch.AllBots = false;
            var people = Roster.GetPeople(shot.Mode);
            int pick = -1;
            for (int i = 0; i < people.Count; i++) if (people[i].Id == shot.HumanPick) pick = i;
            Assert.GreaterOrEqual(pick, 0, shot.HumanPick + " is not in the " + shot.Mode + " roster");
            Settings.SettingsStore.Current.CharacterPick = pick;
            Settings.SettingsStore.Current.CinematicCameraMotion = false;
            Settings.SettingsStore.Current.ReducedUiMotion = false;
            yield return SceneManager.LoadSceneAsync(shot.Map);
            while (HubLoading.Preparing) yield return null;
            var round = GameServices.Round;
            float waited = 0.0f;
            while (waited < 60.0f && (round == null || !round.RoundActive)) { waited += Time.unscaledDeltaTime; yield return null; round = GameServices.Round; }
            Assert.IsTrue(round != null && round.RoundActive, shot.Map + ": no live round after 60 s");
            foreach (var ai in Object.FindObjectsByType<AIController>()) ai.enabled = false;
            foreach (var reader in Object.FindObjectsByType<PlayerInputReader>()) reader.enabled = false;
            yield return null;
        }

        [System.Serializable] private sealed class Receipt
        {
            public int number; public string slug; public string map; public float length, handle, fps; public int frames, handleFrames;
            public float filmIn, filmRate; public string[] passes;
        }

        /// <summary>A clean frame: the scene through its grade, no canvas. `FilmFrames` without a `Flip`.</summary>
        private IEnumerator TakeClean(string path) => _frames.Take(path, false);

        /// <summary>The map's ambience beds at their in-game gains (Eskinita: `AlleySoundscape`'s five, every location
        /// weight and drift taken as 1, as `tools/render_map_film.py --bed` does).</summary>
        private static IEnumerable<(string clip, float gain)> Beds()
        {
            var scape = Object.FindFirstObjectByType<AlleySoundscape>();
            if (scape == null) yield break;
            var scaleField = typeof(KantoStreetSound).GetField("AmbientGainScale", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
            float scale = scaleField != null ? System.Convert.ToSingle(scaleField.GetValue(null)) : 1.0f;
            float all = scape.BedsGain * scale;
            if (scape.Neighbourhood != null) yield return (scape.Neighbourhood.name, scape.NeighbourhoodGain * all);
            if (scape.Street != null) yield return (scape.Street.name, scape.StreetGain * all);
            if (scape.Kids != null) yield return (scape.Kids.name, scape.KidsGain * all);
            if (scape.Sparrows != null) yield return (scape.Sparrows.name, scape.SparrowsGain * all);
            if (scape.Wind != null) yield return (scape.Wind.name, scape.WindGain * all);
        }

        private static string F(float v) => v.ToString("0.0000", CultureInfo.InvariantCulture);
        private static float Smooth(float t) { t = Mathf.Clamp01(t); return t * t * (3.0f - 2.0f * t); }

        /// <summary>
        /// What a shot stages with: the film's own builders (`Person`, `Prop`, `Floor`, all owned by the film and gone at
        /// its `End`), reached by reflection so the film's surface stays as it ships, and the camera, posed directly.
        /// </summary>
        internal sealed class Staging
        {
            private const BindingFlags Inside = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
            public readonly MapFilm Film;
            public readonly Camera Camera;
            public readonly Vector3 Centre;
            public readonly CharacterMotor[] Players;
            public readonly List<string> Notes = new List<string>();

            public Staging(MapFilm film, Camera camera, CharacterMotor[] players)
            {
                Film = film; Camera = camera; Players = players;
                if (film != null) Centre = (Vector3)typeof(MapFilm).GetProperty("Centre", Inside).GetValue(film);
                else
                {
                    var can = GameServices.Round != null && GameServices.Round.Lata != null ? GameServices.Round.Lata : Object.FindFirstObjectByType<Lata>();
                    Centre = can != null ? can.transform.position : Vector3.zero;
                }
            }

            /// <summary>
            /// A hand anchor on an extra, fitted the way `CharacterVisual.BuildHandAnchor` fits a seat's: the palm's centre on
            /// the first hand bone the rig has (forearm-right, arm-right, ...), out to its surface.
            /// </summary>
            public Transform HandAnchor(Extra hero)
            {
                var skinned = hero.Model != null ? hero.Model.GetComponentInChildren<SkinnedMeshRenderer>() : null;
                if (skinned == null || skinned.bones == null) return null;
                int bone = -1;
                foreach (string wanted in new[] { "forearm-right", "arm-right" })
                {
                    for (int i = 0; i < skinned.bones.Length && bone < 0; i++)
                        if (skinned.bones[i] != null && string.Equals(skinned.bones[i].name, wanted, System.StringComparison.OrdinalIgnoreCase)) bone = i;
                    if (bone >= 0) break;
                }
                if (bone < 0) return null;
                var centre = typeof(CharacterVisual).GetMethod("PalmCentre", BindingFlags.Static | BindingFlags.Public);
                var surface = typeof(CharacterVisual).GetMethod("PalmSurface", BindingFlags.Static | BindingFlags.NonPublic);
                var args = new object[] { skinned, bone, Vector3.zero, false };
                if (centre == null || !(bool)centre.Invoke(null, args)) return null;
                Vector3 palm = (Vector3)args[2];
                if (surface != null)
                {
                    var more = new object[] { skinned, bone, palm, Vector3.zero };
                    if ((bool)surface.Invoke(null, more)) palm = (Vector3)more[3];
                }
                var anchor = new GameObject("Key art hand").transform;
                anchor.SetParent(skinned.bones[bone], false);
                anchor.localPosition = palm;
                return anchor;
            }

            /// <summary>A drawn copy of the game's slipper (`MapFilm.Ghost`, gone at `End`) held at `hand`: the sole along the
            /// forearm, out past the fist, as a carried slipper hangs.</summary>
            public void Hold(Slipper source, Transform hand, string who)
            {
                var copy = (GameObject)typeof(MapFilm).GetMethod("Ghost", Inside).Invoke(Film, new object[] { source.gameObject, "Key art slipper " + who });
                if (copy == null) return;
                copy.SetActive(true);
                foreach (var r in copy.GetComponentsInChildren<Renderer>(true)) r.enabled = true;
                // Kept at its own world size (r1 parented it into the rig's scale and the slippers came out head-sized).
                copy.transform.SetParent(hand, true);
                // The copy's longest side is the sole's length: lay it along the bone, out of the hand.
                var bounds = copy.GetComponentsInChildren<MeshFilter>(true).Where(f => f.sharedMesh != null).Select(f => f.sharedMesh.bounds).FirstOrDefault();
                Vector3 size = bounds.size;
                Vector3 length = size.x >= size.y && size.x >= size.z ? Vector3.right : size.y >= size.z ? Vector3.up : Vector3.forward;
                Vector3 along = hand.parent != null ? (hand.position - hand.parent.position).normalized : Vector3.down;
                copy.transform.rotation = Quaternion.FromToRotation(copy.transform.rotation * length, along) * copy.transform.rotation;
                copy.transform.position = hand.position + along * Mathf.Max(size.x, size.y, size.z) * source.transform.lossyScale.x * 0.35f;
            }

            /// <summary>The seat's model as drawn this frame.</summary>
            public Bounds Drawn(int seat)
            {
                var model = Players[seat].GetComponent<CharacterVisual>()?.Model;
                Bounds drawn = default; bool found = false;
                if (model != null)
                    foreach (var r in model.GetComponentsInChildren<Renderer>()) { if (!r.enabled) continue; if (!found) { drawn = r.bounds; found = true; } else drawn.Encapsulate(r.bounds); }
                return found ? drawn : new Bounds(Players[seat].transform.position + Vector3.up * 0.7f, Vector3.one * 1.4f);
            }

            public void Note(string line) => Notes.Add(line);

            /// <summary>The top of the lata as drawn.</summary>
            public Vector3 CanTop
            {
                get
                {
                    var can = GameServices.Round != null && GameServices.Round.Lata != null ? GameServices.Round.Lata : Object.FindFirstObjectByType<Lata>();
                    if (can == null) return Centre;
                    Bounds drawn = default; bool found = false;
                    foreach (var r in can.GetComponentsInChildren<Renderer>()) { if (!r.enabled) continue; if (!found) { drawn = r.bounds; found = true; } else drawn.Encapsulate(r.bounds); }
                    return found ? drawn.center : can.transform.position;
                }
            }

            public T Field<T>(string name) where T : class
            {
                for (var type = Film.GetType(); type != null; type = type.BaseType)
                {
                    var field = type.GetField(name, Inside | BindingFlags.DeclaredOnly);
                    if (field != null) return field.GetValue(Film) as T;
                }
                return null;
            }

            public void Hide<T>(string field) where T : Renderer { var r = Field<T>(field); if (r != null) r.enabled = false; }

            public float Floor(Vector3 at) => (float)typeof(MapFilm).GetMethod("Floor", Inside).Invoke(Film, new object[] { at, Centre.y });

            public GameObject Prop(PrimitiveType shape, string name, Vector3 at, Quaternion turn, Vector3 size, Color colour, bool outline = true) =>
                (GameObject)typeof(MapFilm).GetMethod("Prop", Inside).Invoke(Film, new object[] { shape, name, at, turn, size, colour, outline });

            public Extra Person(string name, float scale, params string[] ids)
            {
                var made = typeof(MapFilm).GetMethod("Person", Inside).Invoke(Film, new object[] { name, scale, ids });
                return made == null ? null : new Extra(made, name);
            }

            /// <summary>The nearest world surface along `dir` (not a player, not a trigger).</summary>
            public bool Wall(Vector3 from, Vector3 dir, float reach, out RaycastHit wall)
            {
                wall = default; float nearest = float.PositiveInfinity;
                foreach (var hit in Physics.RaycastAll(from, dir, reach, ~0, QueryTriggerInteraction.Ignore))
                    if (hit.collider.GetComponentInParent<CharacterMotor>() == null && hit.distance < nearest) { nearest = hit.distance; wall = hit; }
                return !float.IsInfinity(nearest);
            }

            public void Shoot(Vector3 eye, Quaternion turn, float fov)
            {
                Camera.transform.SetPositionAndRotation(eye, turn);
                Camera.fieldOfView = fov;
            }

            /// <summary>The turn from `eye` that puts `subject` on the frame's vertical centre line, `fromBottom` of the
            /// way up the frame, at this vertical field of view.</summary>
            public static Quaternion Placing(Vector3 eye, Vector3 subject, float fov, float fromBottom)
            {
                float below = Mathf.Atan((0.5f - fromBottom) * 2.0f * Mathf.Tan(fov * 0.5f * Mathf.Deg2Rad)) * Mathf.Rad2Deg;
                return Quaternion.LookRotation(subject - eye) * Quaternion.Euler(-below, 0.0f, 0.0f);
            }

            /// <summary>One frame of acting, as `MapFilm.Feel` (degrees; arm directions in world space).</summary>
            internal struct Acting { public float Lean, Twist, Rock, Nod, Turn, Tilt, Arms, Hop; public Vector3 Left, Right; }

            private static System.Type _feel;
            private static MethodInfo _act;

            /// <summary>Lays `a` over the kid's pose this frame through the film's own acting layer (after `Play`).</summary>
            public void Act(Extra kid, Acting a)
            {
                if (kid == null) return;
                if (_feel == null) _feel = typeof(MapFilm).GetNestedType("Feel", BindingFlags.NonPublic | BindingFlags.Public);
                if (_act == null) _act = typeof(MapFilm).GetMethods(Inside).First(m => m.Name == "Act" && m.GetParameters()[0].ParameterType.GetElementType() == null && m.GetParameters()[0].ParameterType.Name == "Extra");
                object feel = System.Activator.CreateInstance(_feel);
                void Set(string name, object value) => _feel.GetField(name).SetValue(feel, value);
                Set("Lean", a.Lean); Set("Twist", a.Twist); Set("Rock", a.Rock); Set("Nod", a.Nod); Set("Turn", a.Turn); Set("Tilt", a.Tilt);
                Set("Left", a.Left); Set("Right", a.Right); Set("Arms", a.Arms); Set("Hop", a.Hop);
                var torso = kid.Bone("torso"); var arm = kid.Bone("arm-right");
                Quaternion torsoWas = torso != null ? torso.rotation : Quaternion.identity, armWas = arm != null ? arm.rotation : Quaternion.identity;
                _act.Invoke(Film, new[] { kid.Raw, feel });
                if (torso != null) kid.MostTorso = Mathf.Max(kid.MostTorso, Quaternion.Angle(torsoWas, torso.rotation));
                if (arm != null) kid.MostArm = Mathf.Max(kid.MostArm, Quaternion.Angle(armWas, arm.rotation));
            }

            /// <summary>A film extra (`MapFilm.Extra`), through its own public methods.</summary>
            internal sealed class Extra
            {
                private readonly object _extra;
                public readonly string Name;
                public Extra(object extra, string name) { _extra = extra; Name = name; }
                public object Raw => _extra;
                /// <summary>The largest turn the acting layer gave the torso and the right arm over the clip's pose.</summary>
                public float MostTorso, MostArm;
                private readonly Dictionary<string, Transform> _bones = new Dictionary<string, Transform>();
                public Transform Bone(string name)
                {
                    if (_bones.TryGetValue(name, out var bone) && bone != null) return bone;
                    var model = (GameObject)_extra.GetType().GetField("Model").GetValue(_extra);
                    bone = model != null ? model.GetComponentsInChildren<Transform>(true).FirstOrDefault(b => b.name == name) : null;
                    _bones[name] = bone;
                    return bone;
                }
                private void Call(string method, params object[] args) => _extra.GetType().GetMethod(method).Invoke(_extra, args);
                public void Pose(Vector3 at, float yaw, float lean = 0.0f, float roll = 0.0f) => Call("Pose", at, yaw, lean, roll);
                public void Play(string clip, float time) => Call("Play", clip, time);
                public void Show(bool shown) => Call("Show", shown);
                public GameObject Model => (GameObject)_extra.GetType().GetField("Model").GetValue(_extra);
            }
        }
    }
}
