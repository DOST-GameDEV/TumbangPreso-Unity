using System.Collections.Generic;
using TumbangPreso.CameraSystem;
using UnityEngine;

namespace TumbangPreso.Visual
{
    public sealed partial class HeroIntroductionScene
    {
        // =========================================================================================
        // NEMU, LET THEM IN: BEHIND YOU, 5.9 s (owner 2026-10-13; `docs/reports/nemu-ultimate-2026-10-11/direction.md` section 12).
        // Preview v3 was *"chonky and weird"*, *"dont make sense and lacks vfx and direction and visual communication or
        // storytelling"*. Of three stories the owner picked BEHIND YOU, the horror reveal.
        //
        // One sentence: she whispers, the lights fail, and every time they come back the dead stand closer behind each player,
        // until their hands close over the players' eyes and they are let in. The travelling thing is THE DARK: it leaves her
        // eyes, takes the court in three blackouts, and ends in the players' eyes.
        //
        //   A THE WHISPER 0.00-1.10, close from her left: she looks up (0.15); finger to her lips (0.30); her eyes ignite and a
        //     ring of dark goes out over the court from her feet (0.42); she lets her hand fall open (1.00).
        //   B THE FLICKER 1.10-3.80: behind her, watching the players, who do not know. The lights fail three times. In the
        //     first blackout (1.50) a pair of violet eyes opens far behind every player; when the light comes back a tall ghost
        //     stands there on a sigil. Second blackout (2.20): the eyes of the dead open all round the plaza, and the lens cuts
        //     closer, low; the ghosts are halfway. Third (2.90): cut in front of a player; the ghost is right behind them,
        //     looming over their head.
        //   C TAKEN 3.80-5.90: still pushing in on that player, long black hands slide round their head and close over their
        //     eyes (3.80-4.12); they jolt; the ghost sinks into them, smoke pulled in after it, a ring of dark at their feet
        //     (4.24-4.58); their eyes open violet (4.62); everyone turns to stare into the lens as it pulls back to hold them
        //     all, the aura on them (4.72-5.90). Hand back with no black-out.
        //
        // Rules the owner set that still hold: black and one violet (no white, no candle amber); no Kuro; after the entry no
        // ghost is drawn on a player, only the aura (`NemuAura`); below 6 s. Nothing here runs on Update and nothing is random:
        // every row is typed, every piece posed from the scene clock (`NemuSmokeBatch`, `Shaders/NemuSmoke`).
        // ⚠️ THE PLAYERS ARE THE REAL ONES (`StageOthers`), animated with their own rigs' clips (`RootedMotion`).
        // =========================================================================================
        private const float NmIgnite = .42f, NmB = 1.10f;
        /// <summary>The three blackouts: when each starts. Each lasts <see cref="NmDark"/>, with a stutter at its start.</summary>
        private static readonly float[] NmFlicker = { 1.50f, 2.20f, 2.90f };
        private const float NmDark = .19f;
        private const float NmC = 3.80f, NmHandsIn = 3.80f, NmHandsShut = 4.12f, NmSink = 4.24f, NmSunk = 4.58f, NmEyesOpen = 4.62f;
        private const float NmStare = 4.72f, NmPullBack = 4.78f, NmWide = 5.42f;
        /// <summary>How far behind each player the dead stand after each blackout (metres), and how tall they are (times the player).</summary>
        // (film r13: at 5.2 and 2.5 m, 1.75 times a player, the first two were specks in the dark.)
        private static readonly float[] NmBehind = { 3.3f, 1.6f, .42f };
        private const float NmTall = 2.1f;
        /// <summary>
        /// The eyes of the dead opening all round the plaza in the second and third blackouts: (angle round the players' middle
        /// in degrees, metres out from it, height, which blackout it opens in).
        /// </summary>
        private static readonly Vector4[] NmCrowdEyes =
        {
            new Vector4(-70f, 10.5f, 2.1f, 1), new Vector4(-52f, 12.8f, 1.6f, 1), new Vector4(-31f, 13.6f, 2.6f, 1), new Vector4(-12f, 11.9f, 1.9f, 1),
            new Vector4(8f, 14.2f, 2.3f, 1), new Vector4(27f, 12.4f, 1.7f, 1), new Vector4(46f, 13.1f, 2.4f, 1), new Vector4(64f, 10.9f, 2.0f, 1),
            new Vector4(-84f, 9.6f, 1.8f, 2), new Vector4(-61f, 9.1f, 2.7f, 2), new Vector4(-40f, 10.2f, 1.5f, 2), new Vector4(-20f, 9.4f, 2.2f, 2),
            new Vector4(-3f, 10.8f, 1.6f, 2), new Vector4(17f, 9.7f, 2.5f, 2), new Vector4(36f, 10.1f, 1.9f, 2), new Vector4(55f, 9.3f, 2.2f, 2),
            new Vector4(79f, 9.9f, 1.7f, 2), new Vector4(-95f, 12.0f, 2.4f, 2), new Vector4(92f, 12.6f, 2.0f, 2), new Vector4(-45f, 15.5f, 3.0f, 2),
        };
        /// <summary>How many motes of violet rise round her from the ignite.</summary>
        private const int NmMotes = 14;
        /// <summary>Strands of smoke drifting across the lens in the close shot: (from, screen x from, x to, y, share of eye-to-focus, length).</summary>
        private static readonly (float From, float X0, float X1, float Y, float Near, float Length)[] NmLensSmoke =
        {
            (.35f, -.95f, .7f, .38f, .45f, .5f), (.6f, .95f, -.6f, -.42f, .5f, .6f),
        };
        /// <summary>With nobody on court the dead approach the lens at these spots instead: (angle off her facing, metres out).</summary>
        private static readonly Vector2[] NmEmptySpots = { new Vector2(-18f, 5.5f), new Vector2(14f, 6.2f) };

        /// <summary>A FILM OUTSIDE PLAY: the editor's cutscene film still names a companion here (no longer drawn by Nemu's).</summary>
        public static GhostPetCompanion FilmCompanion;

        private sealed class NmVictim
        {
            public GameObject Holder;
            public Transform AnimRoot, Model, Head;
            public Transform[] Bones;
            public Quaternion[] Tmp;
            public Vector3[] TmpPos;
            public AnimationClip Shock, Struggle;
            public Vector3 Home, FaceLocal, FaceAt;
            public float Yaw, Top = 1.7f, Width = .7f, FaceDepth = .3f, FaceTop = .5f;
            public int Seat, Sigil, Ring;
            public bool Real = true;
        }

        private readonly List<NmVictim> _nmVictims = new List<NmVictim>(4);
        private NemuSmokeBatch _nmSmoke;
        private int _nmRing;
        private Vector3 _nmWay = Vector3.forward, _nmCentre = new Vector3(0f, 0f, 6f);
        private Vector3 _nmEye = new Vector3(-.72f, 1.08f, 1.75f), _nmLook = new Vector3(0f, .98f, 0f);
        private Vector3 _nmFaceLocal = Vector3.forward;
        private float _nmFaceDepth = .3f, _nmHeadTop = 1.2f;
        private Material _nmInk;

        // ------------------------------------------------------------------ build

        private void BuildNemu(CharacterMotor source)
        {
            _nmSmoke = new NemuSmokeBatch(_root.transform, "NmDead", 900, 1200, 16);
            // The ring of dark that goes out from her feet when her eyes ignite.
            _nmRing = Add("NmRing", VfxShapes.Collar(64, .02f, .93f), new Color(.52f, .2f, .92f, .9f), 1.1f);
            if (_head != null) NemuAura.MeasureFace(_head, _bodyRenderers, _root.transform.forward, out _nmFaceLocal, out _nmFaceDepth, out _nmHeadTop);

            StageOthers(3.0f, 8.5f);
            Vector3 centre = Vector3.zero;
            for (int i = 0; i < _others.Count; i++) { _nmVictims.Add(BuildVictim(_others[i], i)); centre += _others[i].Home; }
            if (_nmVictims.Count > 0)
            {
                centre /= _nmVictims.Count; centre.y = 0f;
                _nmCentre = centre;
                if (centre.sqrMagnitude > .04f) _nmWay = centre.normalized;
            }
            else
            {
                // Nobody on court: two of the dead come for the lens instead.
                for (int i = 0; i < NmEmptySpots.Length; i++)
                {
                    var spot = NmEmptySpots[i];
                    var home = Quaternion.Euler(0f, spot.x, 0f) * Vector3.forward * spot.y;
                    _nmVictims.Add(new NmVictim { Home = home, FaceAt = home + Vector3.up * 1.2f, Yaw = Mathf.Atan2(-home.x, -home.z) * Mathf.Rad2Deg, Real = false, Seat = i });
                }
                _nmCentre = Vector3.forward * 5.8f;
            }
            foreach (var v in _nmVictims)
            {
                v.Sigil = Add("NmSigil", VfxShapes.WardCircle(12, 4, .03f, 7 + v.Seat), new Color(.55f, .2f, .95f, .85f), 1.2f);
                v.Ring = Add("NmSinkRing", VfxShapes.Collar(48, .02f, .9f), new Color(.5f, .18f, .9f, .9f), 1.1f);
            }
        }

        private NmVictim BuildVictim(Other other, int index)
        {
            var v = new NmVictim { Holder = other.Holder, Home = other.Home, Yaw = other.Yaw, Seat = index };
            CharacterMotor live = null;
            var round = GameServices.Round;
            if (round != null)
                foreach (var p in round.Players)
                    if (p != null && other.Holder.name == "IntroductionOther-P" + (p.PlayerSlot + 1)) { live = p; v.Seat = p.PlayerSlot; break; }
            var animator = live != null ? live.GetComponent<CharacterVisual>()?.Model?.GetComponentInChildren<Animator>() : null;
            if (animator != null)
            {
                foreach (var t in other.Holder.GetComponentsInChildren<Transform>(true)) if (t.name == animator.transform.name) { v.AnimRoot = t; break; }
                var set = GeneratedMotionAssets.For(RootedMotion.Folder, DanceClip.ResourceName(animator.transform));
                if (set != null && set.Clips != null)
                    foreach (var clip in set.Clips)
                    {
                        if (clip == null) continue;
                        if (clip.name == RootedMotion.Breakout) v.Shock = clip;
                        else if (clip.name == RootedMotion.Struggle) v.Struggle = clip;
                    }
            }
            if (other.Holder.transform.childCount > 0) v.Model = other.Holder.transform.GetChild(0);
            foreach (var t in other.Holder.GetComponentsInChildren<Transform>(true)) if (t.name == "head") { v.Head = t; break; }
            v.Bones = other.Holder.GetComponentsInChildren<Transform>(true);
            v.Tmp = new Quaternion[v.Bones.Length]; v.TmpPos = new Vector3[v.Bones.Length];
            bool any = false; Bounds b = default;
            foreach (var r in other.Renderers) { if (r == null) continue; if (!any) { b = r.bounds; any = true; } else b.Encapsulate(r.bounds); }
            if (any)
            {
                v.Top = Mathf.Clamp(b.max.y - other.Holder.transform.position.y, .9f, 2.6f);
                v.Width = Mathf.Clamp(Mathf.Max(b.size.x, b.size.z), .4f, 1.2f);
            }
            if (v.Head != null) NemuAura.MeasureFace(v.Head, other.Renderers, other.Holder.transform.forward, out v.FaceLocal, out v.FaceDepth, out v.FaceTop);
            // Where their eyes are, in the scene (film r13: framed from the body's height the lens sat on their hair and the hands
            // over their eyes were under the frame).
            v.FaceAt = v.Head != null
                ? _root.transform.InverseTransformPoint(v.Head.position + Vector3.up * (v.FaceTop * .28f) + other.Holder.transform.forward * v.FaceDepth)
                : v.Home + Vector3.up * (v.Top * .7f);
            // Hidden until B: switched off at the holder (`SetVisibleForCapture` would turn renderers back on).
            other.Holder.SetActive(false);
            return v;
        }

        // ------------------------------------------------------------------ the clock's shape

        private Vector3 NmW(Vector3 local) => _root.transform.TransformPoint(local);
        private Vector3 NmWDir(Vector3 local) => _root.transform.TransformDirection(local);

        /// <summary>
        /// 0 lights on, 1 a blackout. Each failure stutters first (out, back, out) like a dying tube, then holds dark.
        /// Reduced effects keeps a short, soft dip instead of a stutter.
        /// </summary>
        private float NmBlack(float t)
        {
            float black = 0f;
            foreach (float f in NmFlicker)
            {
                if (t < f || t > f + NmDark + .05f) continue;
                float u = t - f;
                float b;
                if (_reducedEffects) b = Mathf.Sin(Mathf.Clamp01(u / (NmDark + .05f)) * Mathf.PI) * .7f;
                else if (u < .035f) b = 1f;
                else if (u < .065f) b = .15f;
                else if (u < NmDark) b = 1f;
                else b = 1f - (u - NmDark) / .05f;
                black = Mathf.Max(black, b);
            }
            return black;
        }

        /// <summary>How many blackouts have reached their dark (0 to 3): which stage the dead are at.</summary>
        private static int NmStage(float t)
        {
            int n = 0;
            foreach (float f in NmFlicker) if (t >= f + .1f) n++;
            return n;
        }

        private static Vector3 NmForward(NmVictim v) => Quaternion.Euler(0f, v.Yaw, 0f) * Vector3.forward;

        // ------------------------------------------------------------------ sample

        private void SampleNemu(float t)
        {
            _nmSmoke.Begin();
            _nmSmoke.SetView(NmW(_nmEye), NmW(_nmLook));
            Vector3 up = Vector3.up;

            // ------------------------------------------------------------- A: her eyes, the ring of dark, the motes
            if (_head != null)
            {
                Vector3 look = _head.rotation * _nmFaceLocal; look.y = 0f;
                if (look.sqrMagnitude < 1e-4f) look = _root.transform.forward;
                look.Normalize();
                Vector3 eyes = _head.position + Vector3.up * (_nmHeadTop * .13f) + look * (_nmFaceDepth + .015f);
                float lit = Ease(NmIgnite, NmIgnite + .08f, t) * (1f + 1.4f * Flash(t, NmIgnite + .06f, .12f)) * (1f - Ease(Seconds - .5f, Seconds, t));
                NemuAura.Eyes(_nmSmoke, eyes, look, _nmHeadTop * .30f, .062f, lit * 2.1f, NmW(_nmEye));
                // A glint as they ignite: a four-pointed star of violet light.
                float glint = Flash(t, NmIgnite + .05f, .1f);
                if (glint > 0f && t < NmB) Glint(eyes + look * .05f, .07f, 1.6f * glint, Vector3.Cross(Vector3.up, look), .45f * glint);
            }
            {
                float u = Mathf.Clamp01((t - NmIgnite) / .7f);
                float ring = t >= NmIgnite && t < NmIgnite + .7f ? 1f - u : 0f;
                float r = Mathf.Lerp(.3f, 9f, 1f - (1f - u) * (1f - u));
                Place(_nmRing, up * .03f, new Vector3(r, 1f, r), Quaternion.identity, ring);
            }
            for (int j = 0; j < NmMotes && t < NmB + .1f; j++)
            {
                float phase = Mathf.Repeat(t * (.45f + .04f * (j % 5)) + j * .371f, 1f);
                float a = (j * 137.5f) * Mathf.Deg2Rad;
                float rad = .35f + .25f * Mathf.Repeat(j * .53f, 1f);
                Vector3 at = new Vector3(Mathf.Sin(a) * rad, .1f + phase * 1.9f, Mathf.Cos(a) * rad * .7f);
                float on = Ease(NmIgnite, NmIgnite + .2f, t) * Mathf.Sin(phase * Mathf.PI);
                _nmSmoke.Light(NmW(at), .045f, on * 1.1f, NemuSmokeBatch.Violet);
            }
            // Thin strands of smoke drift across the lens in the close shot.
            foreach (var m in NmLensSmoke)
            {
                if (t < m.From || t > m.From + .9f || t > NmB) continue;
                float u = (t - m.From) / .9f;
                Vector3 toward = (_nmLook - _nmEye).normalized;
                Vector3 right = Vector3.Cross(up, toward).normalized;
                Vector3 at = _nmEye + toward * (Vector3.Distance(_nmEye, _nmLook) * m.Near) + right * Mathf.Lerp(m.X0, m.X1, u) * .55f + up * (m.Y * .35f);
                _nmSmoke.Wisp(NmW(at), NmWDir(right + up * .15f), .07f, m.Length, .45f * Mathf.Sin(u * Mathf.PI), m.From, u * .5f, .8f);
            }

            // ------------------------------------------------------------- B and C: the dead behind each player
            for (int i = 0; i < _nmVictims.Count; i++) SampleVictim(_nmVictims[i], i, t);

            // ------------------------------------------------------------- the eyes of the dead round the plaza
            float black = NmBlack(t);
            float crowdOut = 1f - Ease(NmC + .1f, NmC + .5f, t);
            for (int k = 0; k < NmCrowdEyes.Length; k++)
            {
                var row = NmCrowdEyes[k];
                float opens = NmFlicker[(int)row.w] + .04f + .006f * k;
                if (t < opens || crowdOut <= 0f) continue;
                // In the light they are faint; in the dark they burn.
                float on = Ease(opens, opens + .05f, t) * crowdOut * Mathf.Lerp(.35f, 1f, black);
                Vector3 dir = Quaternion.Euler(0f, row.x, 0f) * _nmWay;
                Vector3 at = _nmCentre + dir * row.y + up * row.z;
                Vector3 side = Vector3.Cross(up, -dir).normalized;
                float blink = Mathf.Repeat(t * .7f + k * .37f, 1f) > .94f ? .1f : 1f;
                for (int e = -1; e <= 1; e += 2)
                    _nmSmoke.Light(NmW(at + side * (.14f * e)), .3f, on * blink * 2.5f, NemuSmokeBatch.Violet);
            }

            _nmSmoke.End(t);
        }

        /// <summary>A four-pointed star of light (world): a long cross on up and <paramref name="side"/>, and a soft core.</summary>
        private void Glint(Vector3 at, float size, float strength, Vector3 side, float reach)
        {
            _nmSmoke.Light(at, size * 1.6f, strength, NemuSmokeBatch.Violet);
            _nmSmoke.Light(at, size * .5f, strength, NemuSmokeBatch.Violet, Vector3.up * reach);
            _nmSmoke.Light(at, size * .5f, strength, NemuSmokeBatch.Violet, side.normalized * reach * 1.3f);
        }

        private void SampleVictim(NmVictim v, int index, float t)
        {
            Vector3 up = Vector3.up;
            bool on = t >= NmB;
            if (v.Real && v.Holder.activeSelf != on) v.Holder.SetActive(on);
            if (!on) return;
            Vector3 fwd = NmForward(v);
            Vector3 side = Vector3.Cross(up, fwd);
            float lag = .06f * index;
            int stage = NmStage(t);
            float h = v.Top * NmTall;

            // ---- WHERE THE DEAD ONE STANDS: frozen between blackouts, nearer after each.
            float offSide = ((index % 3) - 1) * .35f;
            float behind = stage > 0 ? NmBehind[stage - 1] : NmBehind[0];
            Vector3 feet = v.Home - fwd * behind + side * (offSide * Mathf.Min(1f, behind / 2.5f));
            // Right behind them it leans over their head.
            float lean = stage >= 3 ? .18f : 0f;
            Vector3 centre = feet + up * (h * .5f) + fwd * (lean * h * .2f);

            // ---- C: IT SINKS INTO THEM
            float sink = Ease(NmSink + lag, NmSunk + lag, t);
            Vector3 chest = v.Home + up * (v.Top * .55f);
            Vector3 drawn = Vector3.Lerp(centre, chest, sink * sink);
            float height = Mathf.Lerp(h, v.Top * .45f, sink);
            bool present = stage >= 1;
            float density = present ? (1f - sink * sink) : 0f;
            float scream = Mathf.Max(.12f, Ease(NmHandsShut + lag, NmSink + lag, t));
            float arms = stage >= 3 ? Mathf.Lerp(.25f, .62f, Ease(NmHandsIn - .2f + lag, NmHandsIn + lag, t)) : .15f;
            Vector3 toEye = _nmEye - drawn; toEye.y = 0f;
            float facing = toEye.sqrMagnitude > 1e-4f ? Mathf.Clamp01((Vector3.Dot(-fwd, toEye.normalized) + .9f) * 1.2f) : 1f;
            // In a blackout only its eyes show (`NemuInk`'s _Black keeps bright violet and darkens everything else).
            if (present && density > .01f)
                _nmSmoke.Ghost(NmW(drawn), height, 0f, 0f, density, .13f + .29f * index, Mathf.Max(facing, .6f), arms, scream);
            // ITS EYES, burning: in a blackout they are all there is of it (film r13: the shader's pinpoints alone did not survive
            // the dark), and in the light they keep it from being a cut-out.
            if (present && density > .01f)
            {
                float black = NmBlack(t);
                Vector3 headAt = drawn + up * (height * .345f);
                Vector3 toLensFlat = _nmEye - headAt; toLensFlat.y = 0f;
                toLensFlat = toLensFlat.sqrMagnitude > 1e-4f ? toLensFlat.normalized : -fwd;
                Vector3 eyeSide = Vector3.Cross(up, toLensFlat);
                float burn = Mathf.Lerp(.55f, 2.5f, black) * density * Mathf.Max(facing, .6f);
                for (int e = -1; e <= 1; e += 2)
                    _nmSmoke.Light(NmW(headAt + toLensFlat * (height * .06f) + eyeSide * (e * height * .03f)), height * Mathf.Lerp(.045f, .07f, black), burn, NemuSmokeBatch.Violet);
            }

            // Its sigil on the court, flaring as it arrives, fading as it sinks; smoke strands rising off it.
            if (present)
            {
                float arrived = NmFlicker[stage - 1] + NmDark;
                float flare = 1f + 1.5f * Mathf.Clamp01(1f - (t - arrived) / .25f);
                float r = .55f * h / 1.75f;
                Place(v.Sigil, feet + up * .025f, new Vector3(r, 1f, r), Quaternion.Euler(0f, v.Yaw + 30f * stage, 0f),
                      Mathf.Clamp01(.45f * flare) * (1f - sink));
                for (int j = 0; j < 4; j++)
                {
                    float phase = Mathf.Repeat(t * .55f + j * .25f + index * .13f, 1f);
                    float a = (j * 90f + index * 40f) * Mathf.Deg2Rad;
                    Vector3 at = feet + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * (r * .55f) + up * (.15f + phase * h * .35f);
                    _nmSmoke.Wisp(NmW(at), Vector3.up, .06f, .5f + phase * .4f, .5f * Mathf.Sin(phase * Mathf.PI) * (1f - sink),
                                  index * .3f + j * .1f, phase * .6f, .9f);
                }
            }
            else Place(v.Sigil, Vector3.zero, Vector3.one, Quaternion.identity, 0f);

            // Smoke pulled into them after it as it sinks.
            if (sink > 0f && sink < 1f)
                for (int j = 0; j < 9; j++)
                {
                    float a = (j * 40f + index * 17f) * Mathf.Deg2Rad;
                    Vector3 outDir = (side * Mathf.Cos(a) + up * Mathf.Sin(a) * .8f - fwd * .4f).normalized;
                    float reach = (1.4f - 1.2f * sink) * (.7f + .3f * Mathf.Repeat(j * .41f, 1f));
                    _nmSmoke.Wisp(NmW(chest + outDir * reach * .5f), NmWDir(outDir), .05f, reach, .7f * Mathf.Sin(sink * Mathf.PI),
                                  j * .07f + index * .2f, sink * .5f, 1f);
                }

            // A ring of dark goes out from their feet as it is in, and a flash at the chest.
            {
                float u = Mathf.Clamp01((t - (NmSunk + lag)) / .45f);
                float ring = t >= NmSunk + lag && u < 1f ? 1f - u : 0f;
                float r = Mathf.Lerp(.3f, 2.6f, 1f - (1f - u) * (1f - u));
                Place(v.Ring, v.Home + up * .03f, new Vector3(r, 1f, r), Quaternion.identity, ring);
                float flash = Flash(t, NmSunk + lag, .1f);
                if (flash > 0f) _nmSmoke.Light(NmW(chest), v.Width * 1.4f, 1.3f * flash, NemuSmokeBatch.Violet);
            }

            if (!v.Real) return;

            // ---- THE HANDS over their eyes, from behind (3.80-4.12), gone into them with the rest of it.
            Vector3 look = NmWDir(fwd), face;
            if (v.Head != null)
            {
                look = v.Head.rotation * v.FaceLocal; look.y = 0f;
                look = look.sqrMagnitude > 1e-4f ? look.normalized : NmWDir(fwd);
                face = v.Head.position + Vector3.up * (v.FaceTop * .28f) + look * (v.FaceDepth + .015f);
            }
            else face = NmW(v.Home + up * (v.Top * .78f) + fwd * (v.Width * .5f));
            {
                float reachIn = Ease(NmHandsIn + lag, NmHandsShut + lag, t);
                float gone = Ease(NmSink + lag + .1f, NmSunk + lag, t);
                float handOn = Ease(NmHandsIn + lag, NmHandsIn + lag + .06f, t) * (1f - gone);
                if (handOn > .01f)
                {
                    Vector3 wSide = Vector3.Cross(Vector3.up, look).normalized;
                    float headHalf = v.Width * .55f;
                    float len = v.Width * .72f;
                    for (int e = -1; e <= 1; e += 2)
                    {
                        // From beside the back of the head, round the side, and across the eyes, fingertips meeting.
                        Vector3 from = face - look * (v.FaceDepth * 1.2f) + wSide * (e * (headHalf + .12f)) + Vector3.up * .06f;
                        Vector3 to = face + look * .07f + wSide * (e * len * .3f) - Vector3.up * .02f;
                        Vector3 at = Vector3.Lerp(from, to, reachIn) + look * (Mathf.Sin(reachIn * Mathf.PI) * .12f);
                        Vector3 fingers = Vector3.Slerp(look, -wSide * e, reachIn).normalized + Vector3.down * .15f;
                        _nmSmoke.Hand(at, fingers, len, handOn, Mathf.Lerp(.7f, .25f, reachIn), index * .23f, e < 0);
                    }
                }
            }

            // ---- THEIR BODY: still and unknowing in B; a shiver once it is right behind; a jolt as the hands close; fighting
            // it as it sinks; then still, turned to stare into the lens.
            float stare = Ease(NmStare + lag, NmStare + lag + .14f, t);
            float jolt = Mathf.Clamp01(1f - Mathf.Abs(t - (NmHandsShut + lag + .03f)) / .14f);
            float fight = Ease(NmHandsShut + lag, NmHandsShut + lag + .1f, t) * (1f - stare);
            float shiver = stage >= 3 ? Ease(NmFlicker[2] + .3f, NmC, t) * (1f - Ease(NmHandsIn, NmHandsShut, t)) : 0f;
            if (v.AnimRoot != null)
            {
                if (v.Struggle != null && (fight > 0f || shiver > 0f))
                    NmBlend(v, v.Struggle, Mathf.Repeat((t - NmHandsShut) * 1.3f + .3f * index, v.Struggle.length), Mathf.Clamp01(fight * .9f + shiver * .18f));
                if (v.Shock != null && jolt > 0f) NmBlend(v, v.Shock, .12f + .015f * Mathf.Sin(t * 41f), jolt);
            }
            Vector3 toLens = _nmEye - v.Home; toLens.y = 0f;
            float lensYaw = toLens.sqrMagnitude > 1e-4f ? Mathf.Atan2(toLens.x, toLens.z) * Mathf.Rad2Deg : v.Yaw;
            float yaw = Mathf.LerpAngle(v.Yaw, lensYaw, stare);
            v.Holder.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
            Vector3 faceFwd = Quaternion.Euler(0f, yaw, 0f) * Vector3.forward;

            // ---- WHAT IS LEFT: the aura (`NemuAura`, the same drawing play keeps), from the moment it is in.
            float auraOn = Ease(NmSunk + lag - .05f, NmSunk + lag + .3f, t);
            if (auraOn > 0f)
            {
                var state = new NemuAura.State
                {
                    Feet = NmW(v.Home), Forward = NmWDir(faceFwd), Stream = Vector3.zero, Top = v.Top, Width = v.Width, Clock = t,
                    Amount = auraOn, Swell = Flash(t, NmSunk + lag + .05f, .25f), Drag = 0f, Thin = 0f, Seat = v.Seat, Reduced = _reducedEffects,
                };
                NemuAura.Draw(_nmSmoke, state);
            }

            // ---- THEIR EYES open violet, with a glint.
            float eyes = Ease(NmEyesOpen + lag, NmEyesOpen + lag + .06f, t) * (1f + 1.3f * Flash(t, NmEyesOpen + lag + .05f, .12f));
            if (eyes > 0f)
            {
                NemuAura.Eyes(_nmSmoke, face, look, v.FaceTop * .32f, .1f, eyes * 1.9f, NmW(_nmEye));
                float glint = Flash(t, NmEyesOpen + lag + .04f, .1f);
                if (glint > 0f) Glint(face + look * .06f, .08f, 1.4f * glint, Vector3.Cross(Vector3.up, look), .5f * glint);
            }
        }

        private static void NmBlend(NmVictim v, AnimationClip clip, float time, float weight)
        {
            int n = v.Bones.Length;
            for (int i = 0; i < n; i++) { v.Tmp[i] = v.Bones[i].localRotation; v.TmpPos[i] = v.Bones[i].localPosition; }
            var holderPos = v.Holder.transform.localPosition; var holderRot = v.Holder.transform.localRotation;
            Vector3 modelPos = v.Model != null ? v.Model.localPosition : Vector3.zero;
            Quaternion modelRot = v.Model != null ? v.Model.localRotation : Quaternion.identity;
            clip.SampleAnimation(v.AnimRoot.gameObject, time);
            v.Holder.transform.localPosition = holderPos; v.Holder.transform.localRotation = holderRot;
            if (v.Model != null) { v.Model.localPosition = modelPos; v.Model.localRotation = modelRot; }
            if (weight >= .999f) return;
            for (int i = 0; i < n; i++)
            {
                v.Bones[i].localRotation = Quaternion.Slerp(v.Tmp[i], v.Bones[i].localRotation, weight);
                v.Bones[i].localPosition = Vector3.Lerp(v.TmpPos[i], v.Bones[i].localPosition, weight);
            }
        }

        // ------------------------------------------------------------------ camera and grade

        /// <summary>
        /// B and C, computed from where the players really are. B1 behind her right shoulder, watching them (to the second
        /// blackout); B2 low and nearer, looking up at the dead halfway to them (to the third); B3 in front of the first player
        /// with the dead one looming over their head, pushing in; C carries on in to their face for the hands and the eyes,
        /// then pulls back and up to hold every player staring. Every cut happens inside a blackout.
        /// </summary>
        private void NemuFrame(int index, float t, ref Vector3 eye, ref Vector3 look, ref float fov)
        {
            if (index >= 1 && _nmVictims.Count > 0)
            {
                Vector3 way = _nmWay, right = Vector3.Cross(Vector3.up, way);
                var first = _nmVictims[0];
                Vector3 f0 = NmForward(first), s0 = Vector3.Cross(Vector3.up, f0);
                float cut1 = NmFlicker[1] + .1f, cut2 = NmFlicker[2] + .1f;
                var second = _nmVictims.Count > 1 ? _nmVictims[1] : first;
                Vector3 f1 = NmForward(second), s1 = Vector3.Cross(Vector3.up, f1);
                Vector3 face = first.FaceAt;
                if (t < cut1)
                {
                    // B1: from behind her, wider (film r13: close on her shoulder her back filled half the frame).
                    float u = Ease(NmB, cut1, t);
                    eye = Vector3.Lerp(-way * 2.5f + right * 1.25f + Vector3.up * 1.5f, -way * 2.0f + right * 1.3f + Vector3.up * 1.45f, u);
                    look = _nmCentre + way * 1.2f + Vector3.up * 1.2f;
                    fov = Mathf.Lerp(50f, 46f, u);
                }
                else if (t < cut2)
                {
                    // B2: low in front of another player, looking up past them at the dead one halfway to them.
                    float u = Ease(cut1, cut2, t);
                    eye = Vector3.Lerp(second.Home + f1 * 3.2f + s1 * 1.1f + Vector3.up * .45f, second.Home + f1 * 2.7f + s1 * .9f + Vector3.up * .5f, u);
                    look = second.Home - f1 * 1.4f + Vector3.up * (second.Top * 1.45f);
                    fov = 52f;
                }
                else
                {
                    // B3 in front of the first player, the dead one looming over them, pushing in; C on to their face for the
                    // hands and the eyes; then back and up to hold every player staring.
                    Vector3 b3 = first.Home + f0 * 3.3f + s0 * .5f + Vector3.up * (face.y - first.Home.y + .1f);
                    Vector3 b3End = first.Home + f0 * 2.6f + s0 * .4f + Vector3.up * (face.y - first.Home.y + .1f);
                    Vector3 close = face + f0 * 1.75f + s0 * .2f + Vector3.up * .08f;
                    // The wide is backed off until every player fits across the frame (film r14: at a fixed 4.3 m two of three were off
                    // the edges), which puts her small in the foreground from behind: she stands, they stare.
                    // Each player must sit inside the frame's half-width at their own depth (film r15: measured from the group's
                    // middle, the nearer players still fell off the edges).
                    float back0 = 4.3f;
                    foreach (var v in _nmVictims)
                    {
                        Vector3 off = v.Home - _nmCentre;
                        back0 = Mathf.Max(back0, (Mathf.Abs(Vector3.Dot(off, right)) + 1.0f) / .68f - Vector3.Dot(off, way));
                    }
                    // (film r16: at 2 m up and 1 m right her head filled the foreground.)
                    Vector3 wide = _nmCentre - way * back0 + right * 1.7f + Vector3.up * 2.7f;
                    Vector3 lookB3 = first.Home - f0 * .3f + Vector3.up * (first.Top * 1.3f);
                    Vector3 lookWide = _nmCentre + Vector3.up * .9f;
                    float push = Ease(cut2, NmC, t), inC = Ease(NmC, NmHandsShut, t), back = Ease(NmPullBack, NmWide, t);
                    eye = Vector3.Lerp(Vector3.Lerp(Vector3.Lerp(b3, b3End, push), close, inC), wide, back);
                    look = Vector3.Lerp(Vector3.Lerp(lookB3, face + Vector3.up * .12f, inC), lookWide, back);
                    fov = Mathf.Lerp(Mathf.Lerp(50f, 40f, inC), 50f, back);
                }
                eye = KeepLensOffOthers(eye, t >= NmC && t < NmPullBack + .2f ? 1.2f : 1.5f);
                // Never inside her.
                Vector3 flat = new Vector3(eye.x, 0f, eye.z);
                if (flat.magnitude < .9f) eye += (flat.sqrMagnitude > 1e-4f ? flat.normalized : -way) * (.9f - flat.magnitude);
            }
            if (!_reducedEffects && index >= 1 && _nmVictims.Count > 0)
            {
                // The lens flinches as the hands close and as it goes in.
                float jolt = Mathf.Max(Mathf.Clamp01(1f - Mathf.Abs(t - (NmHandsShut + .03f)) / .1f), Mathf.Clamp01(1f - Mathf.Abs(t - NmSunk) / .1f));
                var tremble = new Vector3(Mathf.Sin(t * 71f), Mathf.Sin(t * 83f + 1.3f), 0f) * (.03f * jolt);
                eye += tremble; look += tremble * .5f;
            }
            _nmEye = eye; _nmLook = look;
        }

        /// <summary>
        /// The ink (`Shaders/NemuInk`): the frame's edges sink into near-black when her eyes ignite, and in each blackout the whole
        /// frame goes dark except bright violet light, so only the eyes of the dead show. Lets go as play returns.
        /// </summary>
        private void NemuPostProcess(RenderTexture frame, Camera camera, float t)
        {
            float ink = Ease(NmIgnite, NmIgnite + .35f, t) * .9f * (1f - Ease(NmWide, Seconds - .05f, t) * .8f);
            float black = NmBlack(t);
            // Night: the whole frame darkens but bright violet (film r13: the grade alone left a daylight sky).
            float night = Ease(NmIgnite, NmIgnite + .3f, t) * .7f * (1f - Ease(Seconds - .45f, Seconds, t));
            if (ink <= .01f && black <= .01f && night <= .01f) return;
            if (_nmInk == null)
            {
                var shader = Resources.Load<Shader>("Shaders/NemuInk");
                if (shader == null) return;
                _nmInk = new Material(shader) { name = "NemuInk" };
            }
            _nmInk.SetFloat("_Ink", ink);
            _nmInk.SetFloat("_Black", black);
            _nmInk.SetFloat("_Night", night);
            _nmInk.SetFloat("_Clock", t);
            _nmInk.SetFloat("_Aspect", frame.height > 0 ? frame.width / (float)frame.height : 16f / 9f);
            var tmp = RenderTexture.GetTemporary(frame.descriptor);
            Graphics.Blit(frame, tmp, _nmInk);
            Graphics.Blit(tmp, frame);
            RenderTexture.ReleaseTemporary(tmp);
        }

        /// <summary>
        /// Night falls with her eyes: the world dims hard but keeps its colour (darkness from brightness alone keeps violet
        /// violet), back to the map's own grade as play returns.
        /// </summary>
        private void NemuGrade(float t, out float brightness, out float saturation)
        {
            float dark = Ease(NmIgnite, NmIgnite + .3f, t) * (1f - Ease(Seconds - .45f, Seconds, t));
            brightness = Mathf.Lerp(1f, .26f, dark);
            saturation = Mathf.Lerp(1f, .9f, dark);
        }

        private void NemuDispose()
        {
            _nmSmoke?.Dispose(); _nmSmoke = null;
            if (_nmInk != null) { ObjectDestroy(_nmInk); _nmInk = null; }
        }
    }
}
