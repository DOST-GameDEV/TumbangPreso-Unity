using System.Collections.Generic;
using TumbangPreso.CameraSystem;
using UnityEngine;

namespace TumbangPreso.Visual
{
    public sealed partial class HeroIntroductionScene
    {
        // =========================================================================================
        // NEMU, LET THEM IN: SOUL STORM, 5.9 s (owner 2026-10-14; `docs/reports/nemu-ultimate-2026-10-11/direction.md`
        // sections 15 and 16). The owner, in his words: *"a lot of soults to like swirl around in the map annd like some of them
        // go inside the characters and it look slike theyre being possessed"*. Of the references shown he picked Ruined King's
        // Black Mist, and of four soul sketches the WAILING SOUL: a glowing violet head, hollow black eyes, an open black mouth,
        // a long black streaming tail with a broken violet edge.
        //
        //   S1 THE CRACKS (0.00-0.90): her whole body on the court; she flings her hands down, violet cracks split the court
        //     out from her feet, her own eyes ignite (0.32) and the first souls claw up out of the cracks round her.
        //     (Owner on the face close-up: *"i dont like ur intro ... it donnt make sense"*; of the openings offered he picked
        //     the storm and the cracks.)
        //   S2 THE STORM (0.90-2.55): high and wide; the cracks run on across the whole court, souls pour up out of them and
        //     spiral into a vortex circling the court; she throws her arms up; the players look up at it.
        //   S3 THE DIVE (2.55-4.40): low, in front of one player, the storm in the sky behind them; souls peel off the vortex and
        //     dive into every player; each hit jerks them (an impact frame on the first); they fight, go limp, then lift their
        //     heads with violet eyes and the aura.
        //   S4 ALL OF THEM (4.40-5.90): the same move craning up and back: every player possessed, staring, the storm thinning.
        //
        // EYES: the possessed eyes (hers and theirs) are their own painted eyes burning violet (`NemuEyeGlow`), not glows
        // floating in front of the face (owner: *"i liek glowing eyes but ur current impleemtnation sucsk"*).
        // Readability (v3 to v6 the owner could not follow them): daylight; medium and wide shots only (a chibi head close up
        // is a wall of hair); one soul design, the same in every shot; the cause in the frame with its effect.
        // Rules that still hold: black and one violet (no white, no amber); no Kuro; after the entry no ghost is drawn on a
        // player, only the aura (`NemuAura`); below 6 s; nothing chonky. Nothing runs on Update and nothing is random: every
        // soul is posed from the scene clock by its own typed path (`NmSoulAt`).
        // =========================================================================================
        /// <summary>The cracks: when they leave her feet, how far they have run by the cut to the storm, and when they reach
        /// their end.</summary>
        private const float NmCrackFrom = .12f, NmCrackNear = 3f, NmCrackDone = 1.8f;
        private const int NmCracks = 12, NmCrackSteps = 9;
        private const float NmIgnite = .32f, NmWide = .90f, NmBurst = .95f, NmLookUp = 1.55f, NmDive = 2.55f, NmPullBack = 4.40f;
        /// <summary>When the first divers leave the vortex, and each victim's lag after the one before.</summary>
        private const float NmDiveFrom = 2.42f, NmDiveLag = .16f;
        /// <summary>When each of a victim's divers leaves the vortex (after <see cref="NmDiveFrom"/>), and how long a dive takes.</summary>
        private static readonly float[] NmDiverOut = { 0f, .22f, .38f, .6f };
        private const float NmDiveSeconds = .5f;
        /// <summary>From their last soul in to their eyes opening.</summary>
        private const float NmWake = .45f;
        /// <summary>How long the impact frame as the first soul hits lasts: two film frames (the first inverted).</summary>
        private const float NmImpact = .067f;
        /// <summary>The storm thins out and lifts away over the last of the cutscene.</summary>
        private const float NmThinFrom = 4.95f;
        /// <summary>How long a soul's tail is: samples of its own path, this far apart in time.</summary>
        private const int NmTail = 9;
        private const float NmTailStep = .03f;

        /// <summary>A FILM OUTSIDE PLAY: the editor's cutscene film still names a companion here (no longer drawn by Nemu's).</summary>
        public static GhostPetCompanion FilmCompanion;

        /// <summary>One of their bones and how it was when the cutscene began.</summary>
        private struct NmBone
        {
            public Transform T;
            public Quaternion Rot;
            public Vector3 Pos, Scale;
            public void Reset() { if (T != null) { T.localRotation = Rot; T.localPosition = Pos; T.localScale = Scale; } }
        }

        /// <summary>One soul of the storm: where it circles, and (if it is a diver) whom it goes into and when.</summary>
        private struct NmSoul
        {
            public float Born, Rise, Radius, Height, Speed, Phase;
            /// <summary>Where it claws up out of the court (scene space): a point on a crack.</summary>
            public Vector3 From;
            public int Victim;
            public float Out, In;
            public Vector3 Offset;
        }

        private sealed class NmVictim
        {
            public GameObject Holder;
            public Transform Head;
            public NmBone Root, Torso, HeadBone, ArmL, ArmR;
            public Vector3 Home, FaceLocal;
            public float Yaw, Reach, Top = 1.7f, Width = .7f, FaceDepth = .3f, FaceTop = .5f;
            /// <summary>When each of their souls goes in (scene seconds), first to last, and when their eyes open.</summary>
            public float[] Hits;
            public float Wake;
            public NemuEyeGlow Eyes;
            /// <summary>Where the souls go in this frame (scene space): their chest, as it jerks.</summary>
            public Vector3 Chest;
            public int Seat;
        }

        private readonly List<NmVictim> _nmVictims = new List<NmVictim>(4);
        /// <summary>Each crack's points from her feet out (scene space, on the court) and how far along it each point is.</summary>
        private readonly Vector3[,] _nmCrack = new Vector3[NmCracks, NmCrackSteps + 1];
        private readonly float[,] _nmCrackAlong = new float[NmCracks, NmCrackSteps + 1];
        private readonly List<NemuEyeGlow> _nmEyeGlows = new List<NemuEyeGlow>(5);
        private NemuEyeGlow _nmHerEyes;
        private readonly List<NmSoul> _nmSouls = new List<NmSoul>(64);
        private readonly Vector3[] _nmTailAt = new Vector3[NmTail];
        private readonly bool[] _nmTailOn = new bool[NmTail];
        private NemuSmokeBatch _nmSmoke;
        private Vector3 _nmWay = Vector3.forward, _nmCentre = new Vector3(0f, 0f, 6f), _nmEyeOfStorm = new Vector3(0f, 0f, 4.5f);
        private Vector3 _nmEye = new Vector3(-.72f, 1.08f, 1.75f), _nmLook = new Vector3(0f, .98f, 0f);
        private Vector3 _nmFaceLocal = Vector3.forward, _nmEyesLocal, _nmHerChest = new Vector3(0f, .7f, 0f);
        private float _nmFaceDepth = .3f, _nmHeadTop = 1.2f, _nmStormReach = 6f, _nmFirstHit = 2.95f;
        private Material _nmInk;

        // ------------------------------------------------------------------ build

        private void BuildNemu(CharacterMotor source)
        {
            _nmSmoke = new NemuSmokeBatch(_root.transform, "NmStorm", 1600, 2400, 320);
            if (_head != null)
            {
                NemuAura.MeasureFace(_head, _bodyRenderers, _root.transform.forward, out _nmFaceLocal, out _nmFaceDepth, out _nmHeadTop);
                // HER EYES, in the head's own space, so they ride its pitch. They are the two black blocks on her face just under
                // her fringe (owner on v6, the glows sitting on the fringe above them: *"what even is this"*).
                Vector3 face = _head.rotation * _nmFaceLocal;
                _nmEyesLocal = _head.InverseTransformPoint(_head.position + _head.up * (_nmHeadTop * .1f - .17f) + face * (_nmFaceDepth - .01f));
                Vector3 head = _root.transform.InverseTransformPoint(_head.position);
                _nmHerChest = new Vector3(0f, Mathf.Max(.4f, head.y * .75f), 0f);
            }

            _nmHerEyes = NemuEyeGlow.On(_bodyRenderers);
            if (_nmHerEyes != null) _nmEyeGlows.Add(_nmHerEyes);

            StageOthers(3.0f, 8.5f);
            Vector3 centre = Vector3.zero;
            for (int i = 0; i < _others.Count; i++)
            {
                _nmVictims.Add(BuildVictim(_others[i], i));
                centre += _others[i].Home;
            }
            if (_nmVictims.Count > 0)
            {
                centre /= _nmVictims.Count; centre.y = 0f;
                _nmCentre = centre;
                if (centre.sqrMagnitude > .04f) _nmWay = centre.normalized;
            }
            // The storm turns round a point between her and them, wide enough to take in every player and her.
            _nmEyeOfStorm = _nmCentre * .75f;
            float reach = _nmEyeOfStorm.magnitude * .8f;
            foreach (var v in _nmVictims) reach = Mathf.Max(reach, (v.Home - _nmEyeOfStorm).magnitude);
            _nmStormReach = Mathf.Clamp(reach + 1.6f, 3.5f, 9f);
            BuildCracks();
            BuildSouls();
        }

        private static float NmHash(int i, int k) => Mathf.Repeat(Mathf.Sin(i * 12.9898f + k * 78.233f) * 43758.5453f, 1f);

        /// <summary>
        /// The storm: every soul's typed path. The first few leak out of her in S1; the rest pour out of her as she throws her
        /// arms out. Four per player are divers: they circle with the rest, then peel off and go into that player.
        /// </summary>
        private void BuildSouls()
        {
            int count = _reducedEffects ? 36 : 64;
            const int early = 12;
            float length = _nmStormReach + _nmEyeOfStorm.magnitude;
            int victims = _nmVictims.Count;
            for (int i = 0; i < count; i++)
            {
                var soul = new NmSoul { Victim = -1 };
                // It claws up out of a crack just behind the crack's front: the first ones round her in S1, the rest over the
                // whole court as the cracks run on.
                bool near = i < early;
                float along = near ? Mathf.Lerp(.6f, NmCrackNear - .2f, i / (float)early)
                                   : Mathf.Lerp(NmCrackNear, length * .95f, Mathf.Pow((i - early) / (float)(count - early), .9f));
                int crack = (i * 5) % NmCracks;
                soul.From = NmCrackPoint(crack, along);
                soul.Born = NmCrackAt(along) + .08f + .1f * NmHash(i, 8);
                soul.Rise = near ? 1.0f : .7f + .25f * NmHash(i, 7);
                // A funnel: low souls circle tight, high ones wide.
                soul.Height = .9f + 3.1f * NmHash(i, 1);
                float wide = Mathf.Lerp(.35f, 1f, Mathf.Sqrt(NmHash(i, 2)));
                soul.Radius = _nmStormReach * wide * (.8f + .07f * soul.Height);
                // Fast, so it streaks: 5 to 7.5 m/s round the court, one way.
                soul.Speed = (5f + 2.5f * NmHash(i, 3)) / Mathf.Max(1f, soul.Radius);
                soul.Phase = NmHash(i, 4) * Mathf.PI * 2f;
                _nmSouls.Add(soul);
            }
            // The divers, four per player, taken from the main storm (never the ones that leaked out in S1).
            for (int v = 0; v < victims; v++)
            {
                var victim = _nmVictims[v];
                victim.Hits = new float[NmDiverOut.Length];
                for (int j = 0; j < NmDiverOut.Length; j++)
                {
                    int k = early + (v + victims * j * 3) % (count - early);
                    var soul = _nmSouls[k];
                    soul.Victim = v;
                    soul.Out = NmDiveFrom + NmDiveLag * v + NmDiverOut[j];
                    soul.In = soul.Out + (j == 0 ? NmDiveSeconds : NmDiveSeconds * .85f);
                    // Each goes in at its own spot on the chest, not one point.
                    soul.Offset = new Vector3((NmHash(k, 5) - .5f) * .2f, (NmHash(k, 6) - .3f) * .25f, 0f);
                    _nmSouls[k] = soul;
                    victim.Hits[j] = soul.In;
                }
                System.Array.Sort(victim.Hits);
                victim.Wake = victim.Hits[victim.Hits.Length - 1] + NmWake;
            }
            if (victims > 0) _nmFirstHit = _nmVictims[0].Hits[0];
        }

        /// <summary>
        /// The cracks: jagged lines out from her feet across the whole court, each step turning a little, long enough to pass
        /// every player.
        /// </summary>
        private void BuildCracks()
        {
            float length = _nmStormReach + _nmEyeOfStorm.magnitude;
            for (int c = 0; c < NmCracks; c++)
            {
                float a = (c + .4f * (NmHash(c, 11) - .5f)) / NmCracks * Mathf.PI * 2f;
                Vector3 at = Vector3.zero;
                float along = 0f, step = length / NmCrackSteps;
                _nmCrack[c, 0] = at; _nmCrackAlong[c, 0] = 0f;
                for (int k = 1; k <= NmCrackSteps; k++)
                {
                    a += (NmHash(c * 31 + k, 12) - .5f) * .9f;
                    float len = step * (.75f + .5f * NmHash(c * 31 + k, 13));
                    at += new Vector3(Mathf.Sin(a), 0f, Mathf.Cos(a)) * len;
                    along += len;
                    _nmCrack[c, k] = at; _nmCrackAlong[c, k] = along;
                }
            }
        }

        /// <summary>When the cracks' front reaches <paramref name="along"/> metres from her feet: quick round her, then on over
        /// the whole court.</summary>
        private float NmCrackAt(float along)
        {
            if (along <= NmCrackNear) return NmCrackFrom + (NmWide - NmCrackFrom) * along / NmCrackNear;
            float far = Mathf.Max(NmCrackNear + .1f, _nmStormReach + _nmEyeOfStorm.magnitude);
            return NmWide + (NmCrackDone - NmWide) * Mathf.Clamp01((along - NmCrackNear) / (far - NmCrackNear));
        }

        /// <summary>How far the cracks' front has run at <paramref name="t"/> (metres).</summary>
        private float NmCrackReach(float t)
        {
            if (t <= NmCrackFrom) return 0f;
            if (t <= NmWide) return NmCrackNear * (t - NmCrackFrom) / (NmWide - NmCrackFrom);
            float far = Mathf.Max(NmCrackNear + .1f, _nmStormReach + _nmEyeOfStorm.magnitude);
            return NmCrackNear + (far - NmCrackNear) * Mathf.Clamp01((t - NmWide) / (NmCrackDone - NmWide));
        }

        /// <summary>The point <paramref name="along"/> metres down crack <paramref name="c"/> (scene space).</summary>
        private Vector3 NmCrackPoint(int c, float along)
        {
            for (int k = 1; k <= NmCrackSteps; k++)
                if (along <= _nmCrackAlong[c, k])
                {
                    float u = Mathf.InverseLerp(_nmCrackAlong[c, k - 1], _nmCrackAlong[c, k], along);
                    return Vector3.Lerp(_nmCrack[c, k - 1], _nmCrack[c, k], u);
                }
            return _nmCrack[c, NmCrackSteps];
        }

        private NmVictim BuildVictim(Other other, int index)
        {
            var v = new NmVictim { Holder = other.Holder, Home = other.Home, Yaw = other.Yaw, Reach = other.Reach, Seat = index };
            var round = GameServices.Round;
            if (round != null)
                foreach (var p in round.Players)
                    if (p != null && other.Holder.name == "IntroductionOther-P" + (p.PlayerSlot + 1)) { v.Seat = p.PlayerSlot; break; }
            ReadBones(other.Holder.transform, out v.Root, out v.Torso, out v.HeadBone, out v.ArmL, out v.ArmR);
            v.Head = v.HeadBone.T;
            bool any = false; Bounds b = default;
            foreach (var r in other.Renderers) { if (r == null) continue; if (!any) { b = r.bounds; any = true; } else b.Encapsulate(r.bounds); }
            if (any)
            {
                v.Top = Mathf.Clamp(b.max.y - other.Holder.transform.position.y, .9f, 2.6f);
                v.Width = Mathf.Clamp(Mathf.Max(b.size.x, b.size.z), .4f, 1.2f);
            }
            if (v.Head != null) NemuAura.MeasureFace(v.Head, other.Renderers, other.Holder.transform.forward, out v.FaceLocal, out v.FaceDepth, out v.FaceTop);
            v.Chest = v.Home + Vector3.up * (v.Top * .55f);
            v.Eyes = NemuEyeGlow.On(other.Renderers);
            if (v.Eyes != null) _nmEyeGlows.Add(v.Eyes);
            v.Hits = new[] { NmDive + .4f };
            v.Wake = NmDive + 1f;
            return v;
        }

        private static void ReadBones(Transform holder, out NmBone root, out NmBone torso, out NmBone head, out NmBone armL, out NmBone armR)
        {
            root = torso = head = armL = armR = default;
            foreach (var t in holder.GetComponentsInChildren<Transform>(true))
            {
                var bone = new NmBone { T = t, Rot = t.localRotation, Pos = t.localPosition, Scale = t.localScale };
                switch (t.name)
                {
                    case "root": if (root.T == null) root = bone; break;
                    case "torso": if (torso.T == null) torso = bone; break;
                    case "head": if (head.T == null) head = bone; break;
                    case "arm-left": if (armL.T == null) armL = bone; break;
                    case "arm-right": if (armR.T == null) armR = bone; break;
                }
            }
        }

        // ------------------------------------------------------------------ the clock's shape

        private Vector3 NmW(Vector3 local) => _root.transform.TransformPoint(local);
        private Vector3 NmWDir(Vector3 local) => _root.transform.TransformDirection(local);
        private static Vector3 NmForward(NmVictim v) => Quaternion.Euler(0f, v.Yaw, 0f) * Vector3.forward;

        /// <summary>Where a soul circling the storm is at <paramref name="s"/> (scene space), and which way it is going.</summary>
        private Vector3 NmOrbit(in NmSoul soul, float s, out Vector3 velocity)
        {
            float lift = Ease(NmThinFrom, Seconds, s);
            float r = soul.Radius * (1f + .08f * Mathf.Sin(s * 1.1f + soul.Phase) + .35f * lift);
            float a = soul.Phase + soul.Speed * s;
            Vector3 round = new Vector3(Mathf.Sin(a), 0f, Mathf.Cos(a));
            Vector3 along = new Vector3(Mathf.Cos(a), 0f, -Mathf.Sin(a));
            float h = soul.Height + .35f * Mathf.Sin(s * 1.7f + soul.Phase * 3f) + 3f * lift * lift;
            velocity = along * (soul.Speed * r) + Vector3.up * (.6f * Mathf.Cos(s * 1.7f + soul.Phase * 3f));
            return _nmEyeOfStorm + round * r + Vector3.up * h;
        }

        /// <summary>
        /// Where a soul is at <paramref name="s"/> (scene space): out of her chest in a rising spiral, into the storm, and (a
        /// diver) out of the storm and into its player's chest. False before it is born and once it is inside them.
        /// </summary>
        private bool NmSoulAt(in NmSoul soul, int index, float s, out Vector3 at)
        {
            at = default;
            if (s < soul.Born) return false;
            if (soul.Victim >= 0 && s >= soul.In) return false;
            Vector3 orbit;
            if (soul.Victim >= 0 && s >= soul.Out)
            {
                // THE DIVE: a curve from its flight in the storm into the chest, steepening at the end so it plunges in.
                float span = soul.In - soul.Out;
                float u = (s - soul.Out) / span;
                Vector3 from = NmOrbit(soul, soul.Out, out var vel);
                var victim = _nmVictims[soul.Victim];
                Vector3 to = victim.Chest + NmWDirToLocal(victim, soul.Offset);
                Vector3 m0 = vel * (span * .7f);
                Vector3 m1 = (to - from) * .8f + Vector3.down * (span * 3f);
                float u2 = u * u, u3 = u2 * u;
                at = (2 * u3 - 3 * u2 + 1) * from + (u3 - 2 * u2 + u) * m0 + (-2 * u3 + 3 * u2) * to + (u3 - u2) * m1;
                return true;
            }
            orbit = NmOrbit(soul, s, out _);
            float rise = (s - soul.Born) / soul.Rise;
            if (rise >= 1f) { at = orbit; return true; }
            // UP OUT OF THE CRACK: straight up at first, clawing free, then a widening spiral handing over to the storm.
            float a = soul.Phase + rise * 7f;
            float climb = 1f - (1f - rise) * (1f - rise);
            Vector3 spiral = soul.From + Vector3.up * (.05f + climb * 1.8f) + new Vector3(Mathf.Sin(a), 0f, Mathf.Cos(a)) * (.7f * rise * rise);
            float k = rise * rise * (3f - 2f * rise);
            at = Vector3.Lerp(spiral, orbit, k);
            return true;
        }

        /// <summary>A small offset on a player's chest, turned the way they face (scene space).</summary>
        private static Vector3 NmWDirToLocal(NmVictim v, Vector3 offset)
        {
            Vector3 fwd = NmForward(v), side = Vector3.Cross(Vector3.up, fwd);
            return side * offset.x + Vector3.up * offset.y + fwd * .1f;
        }

        // ------------------------------------------------------------------ sample

        private void SampleNemu(float t)
        {
            _nmSmoke.Begin();
            _nmSmoke.SetView(NmW(_nmEye), NmW(_nmLook));

            // ---- HER EYES: her own painted eyes burn violet, and a soft glow round them.
            float lit = Ease(NmIgnite, NmIgnite + .06f, t) * (1f - Ease(Seconds - .5f, Seconds, t));
            _nmHerEyes?.Set(lit, t);
            if (_head != null && lit > .01f)
                _nmSmoke.Light(_head.TransformPoint(_nmEyesLocal) + (_head.rotation * _nmFaceLocal) * .04f, .3f, lit * (.35f + .8f * Flash(t, NmIgnite + .05f, .12f)), NemuSmokeBatch.Violet);

            // ---- THE CRACKS, out from her feet across the court.
            DrawCracks(t);
            // Smoke rising off her while she calls them.
            float calling = Ease(NmIgnite, NmIgnite + .3f, t) * (1f - Ease(NmDive, NmDive + .4f, t));
            if (calling > .01f)
            {
                var state = new NemuAura.State
                {
                    Feet = NmW(Vector3.zero), Forward = _root.transform.forward, Stream = Vector3.zero, Top = _nmHerChest.y / .55f, Width = .55f,
                    Clock = t, Amount = calling, Swell = Flash(t, NmBurst, .3f), Drag = 0f, Thin = 0f, Seat = 7, Reduced = _reducedEffects,
                };
                NemuAura.Draw(_nmSmoke, state);
            }

            // ---- THE PLAYERS ACT FIRST, so the divers go into where their chest is now.
            for (int i = 0; i < _nmVictims.Count; i++)
            {
                var v = _nmVictims[i];
                NmAct(v, i, t);
                Vector3 chest = v.Torso.T != null ? _root.transform.InverseTransformPoint(v.Torso.T.position) + Vector3.up * (v.Top * .12f) : v.Home + Vector3.up * (v.Top * .55f);
                v.Chest = chest;
            }

            // ---- THE STORM.
            Vector3 eye = NmW(_nmEye);
            Vector3 f = (NmW(_nmLook) - eye); f = f.sqrMagnitude > 1e-6f ? f.normalized : Vector3.forward;
            Vector3 right = Vector3.Cross(Vector3.up, f).normalized, camUp = Vector3.Cross(f, right);
            for (int i = 0; i < _nmSouls.Count; i++) DrawSoul(_nmSouls[i], i, t, eye, right, camUp);

            for (int i = 0; i < _nmVictims.Count; i++) SampleVictim(_nmVictims[i], i, t);
            _nmSmoke.End(t);
        }

        /// <summary>
        /// The cracks: violet light along each jagged line as far as its front has run, and a
        /// brighter tip where it is splitting now. Laid 12 cm up (the court sits a few centimetres above her ground point, and
        /// flat pieces never showed: light and smoke do). They burn on until the storm thins, then fade.
        /// </summary>
        private void DrawCracks(float t)
        {
            float reach = NmCrackReach(t);
            if (reach <= .01f) return;
            float fade = 1f - Ease(NmThinFrom, Seconds - .1f, t);
            if (fade <= .01f) return;
            Vector3 lift = Vector3.up * .12f;
            for (int c = 0; c < NmCracks; c++)
            {
                for (int k = 1; k <= NmCrackSteps; k++)
                {
                    float a0 = _nmCrackAlong[c, k - 1], a1 = _nmCrackAlong[c, k];
                    if (a0 >= reach) break;
                    Vector3 p0 = _nmCrack[c, k - 1];
                    Vector3 p1 = a1 <= reach ? _nmCrack[c, k] : Vector3.Lerp(p0, _nmCrack[c, k], (reach - a0) / (a1 - a0));
                    Vector3 d = p1 - p0;
                    float len = d.magnitude;
                    if (len < .02f) continue;
                    // Wider near her, thinner out on the court; it flickers as if something under it is pushing up.
                    float near = 1f - Mathf.Clamp01(a0 / (reach + 1f));
                    float flicker = _reducedEffects ? 1f : .8f + .2f * Mathf.Sin(t * 11f + c * 1.3f + k);
                    Vector3 mid = NmW((p0 + p1) * .5f + lift);
                    // (film r29: 10 cm glows were a few pixels on the court. Film r30: glows and smoke facing the lens were cut
                    // in half by the court; smoke cannot pull, so the cracks are light alone, pulled toward the lens.)
                    _nmSmoke.Light(mid, .3f + .15f * near, (1.2f + .8f * near) * flicker * fade, NemuSmokeBatch.Violet, NmWDir(d) * len, 1f);
                }
                // The tip, splitting now.
                if (reach < _nmCrackAlong[c, NmCrackSteps])
                    _nmSmoke.Light(NmW(NmCrackPoint(c, reach) + lift), .28f, 1.4f * fade, NemuSmokeBatch.Violet, default, 1f);  // (r31: at .6 the tips were blobs)
            }
            // Where they all start: under her feet.
            _nmSmoke.Light(NmW(lift), 1.4f, 1.0f * fade * Ease(NmCrackFrom, NmCrackFrom + .15f, t), NemuSmokeBatch.Violet, default, 1.5f);
        }

        /// <summary>
        /// One wailing soul: a black head lit violet from inside, hollow black eyes and an open black mouth turned to the lens,
        /// and a long black tail drawn along its own path (so it streams the way it flew, and sinks into a player after its head).
        /// </summary>
        private void DrawSoul(in NmSoul soul, int index, float t, Vector3 eye, Vector3 right, Vector3 camUp)
        {
            if (t < soul.Born) return;
            float fade = soul.Victim >= 0 ? 1f : 1f - Ease(NmThinFrom + .3f, Seconds - .05f, t);
            float born = Ease(soul.Born, soul.Born + .15f, t);
            fade *= born;
            if (fade <= .01f) return;
            for (int k = 0; k < NmTail; k++) _nmTailOn[k] = NmSoulAt(soul, index, t - k * NmTailStep, out _nmTailAt[k]);

            // THE TAIL: strands from the head back along its path, thinning, fraying and fading toward the end.
            // (film r29: at 0.3 m and a dim glow the heads read as dark smudges with dots from across the court.)
            const float headSize = .42f;
            for (int k = 0; k < NmTail - 1; k++)
            {
                if (!_nmTailOn[k] || !_nmTailOn[k + 1]) continue;
                Vector3 a = NmW(_nmTailAt[k]), b = NmW(_nmTailAt[k + 1]);
                Vector3 d = a - b;
                float len = d.magnitude;
                if (len < 1e-3f) continue;
                float u = k / (float)(NmTail - 1);
                float width = headSize * .75f * Mathf.Pow(1f - u, .8f) + .025f;
                _nmSmoke.Wisp((a + b) * .5f, d, width, len * 1.4f + width, fade * (.9f - .5f * u), index * .071f + k * .13f, u * .55f, .9f);
            }

            // THE HEAD (gone once it is inside them: only its tail is left, sinking in).
            if (!_nmTailOn[0]) return;
            Vector3 head = NmW(_nmTailAt[0]);
            float dist = Vector3.Distance(head, eye);
            if (dist < .5f) return;
            _nmSmoke.Puff(head, headSize, fade * .95f, index * .113f, 0f, 1f, index * 37f);
            // Lit from inside: violet, never white (strength kept under the clip).
            _nmSmoke.Light(head, headSize * 1.6f, .95f * fade, NemuSmokeBatch.Violet);
            // The face, toward the lens: two hollow eyes and a mouth open in a wail that stretches and slackens.
            Vector3 toLens = (eye - head) / dist;
            Vector3 face = head + toLens * (headSize * .3f);
            float wail = .5f + .5f * Mathf.Sin(t * 9f + index * 1.7f);
            for (int e = -1; e <= 1; e += 2)
                _nmSmoke.Mark(face + right * (e * headSize * .19f) + camUp * (headSize * .1f), camUp, headSize * .17f, headSize * .26f, fade, index * .21f + e * .1f);
            _nmSmoke.Mark(face - camUp * (headSize * .22f), camUp, headSize * .19f, headSize * (.3f + .14f * wail), fade, index * .31f);
        }

        /// <summary>What shows on a player as the souls go in: a burst at each hit, then the aura and violet eyes.</summary>
        private void SampleVictim(NmVictim v, int index, float t)
        {
            Vector3 up = Vector3.up;
            Vector3 fwd = NmForward(v);
            Vector3 chest = v.Chest;

            // Each hit: a flash in the chest and smoke bursting out of their back and shoulders with the force of it.
            for (int h = 0; h < v.Hits.Length; h++)
            {
                float since = t - v.Hits[h];
                if (since < 0f || since > .35f) continue;
                float k = h == 0 ? 1f : .7f;
                _nmSmoke.Light(NmW(chest), v.Width * 1.3f * k, 1.1f * k * Flash(t, v.Hits[h] + .02f, .1f), NemuSmokeBatch.Violet);
                float burst = since / .35f;
                Vector3 side = Vector3.Cross(up, fwd);
                for (int j = 0; j < 7; j++)
                {
                    float a = (j * 51f + h * 23f + index * 17f) * Mathf.Deg2Rad;
                    Vector3 outDir = (side * Mathf.Cos(a) + up * (.4f + .6f * Mathf.Abs(Mathf.Sin(a))) - fwd * .7f).normalized;
                    float reach = .25f + 1.0f * burst;
                    _nmSmoke.Wisp(NmW(chest + outDir * reach * .6f), NmWDir(outDir), .07f * k, reach * .8f, k * .8f * (1f - burst),
                                  j * .07f + h * .3f, burst * .7f, 1f);
                }
            }

            // ---- WHAT IS LEFT: the aura (`NemuAura`, the same drawing play keeps), growing with every soul in.
            float auraOn = Ease(v.Hits[0] - .05f, v.Hits[v.Hits.Length - 1] + .3f, t);
            if (auraOn > 0f)
            {
                var state = new NemuAura.State
                {
                    Feet = NmW(v.Home), Forward = v.Holder.transform.forward, Stream = Vector3.zero, Top = v.Top, Width = v.Width, Clock = t,
                    Amount = auraOn, Swell = Flash(t, v.Wake, .3f), Drag = 0f, Thin = 0f, Seat = v.Seat, Reduced = _reducedEffects,
                };
                NemuAura.Draw(_nmSmoke, state);
            }

            // ---- THEIR EYES: their own painted eyes burn violet, flaring as they open, with a soft glow round them.
            float eyes = Ease(v.Wake, v.Wake + .06f, t);
            v.Eyes?.Set(eyes, t + v.Seat);
            if (eyes > 0f && v.Head != null)
            {
                Vector3 look = v.Head.rotation * v.FaceLocal; look.y = 0f;
                look = look.sqrMagnitude > 1e-4f ? look.normalized : NmWDir(fwd);
                Vector3 face = v.Head.position + up * (v.FaceTop * .28f) + look * (v.FaceDepth + .03f);
                _nmSmoke.Light(face, v.FaceTop * .9f, eyes * (.3f + .9f * Flash(t, v.Wake + .05f, .15f)), NemuSmokeBatch.Violet);
            }
        }

        // ------------------------------------------------------------------ their acting

        /// <summary>
        /// A player who does not know, then does. Their own bones, turned in the world from the pose they had when the
        /// cutscene began: breathing; looking up at the storm; a violent jerk at every soul that hits them (the first the
        /// hardest); fighting it, clawing at their chest; limp; then the possessed lift of the head, tilted, and the turn to the
        /// lens, head first.
        /// </summary>
        private void NmAct(NmVictim v, int index, float t)
        {
            v.Root.Reset(); v.Torso.Reset(); v.HeadBone.Reset(); v.ArmL.Reset(); v.ArmR.Reset();
            float seat = v.Seat * 1.7f;
            float first = v.Hits[0], last = v.Hits[v.Hits.Length - 1];

            // The turn to the lens as it pulls back: how far round, and how much of it the body, the chest and the head have taken.
            Vector3 fwd0 = NmWDir(NmForward(v));
            Vector3 toLens = NmW(_nmEye) - v.Holder.transform.position; toLens.y = 0f;
            float turn = toLens.sqrMagnitude > 1e-4f ? Vector3.SignedAngle(fwd0, toLens, Vector3.up) : 0f;
            float turnAt = Mathf.Max(NmPullBack + .1f, v.Wake + .2f) + .05f * index;
            float headTurn = Ease(turnAt, turnAt + .25f, t);
            float chestTurn = Ease(turnAt + .1f, turnAt + .4f, t);
            float bodyTurn = Ease(turnAt + .2f, turnAt + .5f, t);
            float bodyYaw = turn * .5f * bodyTurn;
            v.Holder.transform.localRotation = Quaternion.Euler(0f, v.Yaw + bodyYaw, 0f);
            Vector3 up = Vector3.up;
            Vector3 fwd = v.Holder.transform.forward, right = v.Holder.transform.right;

            // Every hit: a snap (a frame and a half) and a fall-off; the first is the hardest.
            float jerk = 0f;
            for (int h = 0; h < v.Hits.Length; h++)
            {
                float x = t - v.Hits[h];
                if (x < 0f) continue;
                float k = h == 0 ? 1f : .6f;
                jerk += k * (x < .05f ? x / .05f : Mathf.Exp(-(x - .05f) / .13f));
            }
            jerk = Mathf.Min(jerk, 1.3f);
            float hit = Ease(first - .02f, first + .02f, t);
            float struggle = hit * (1f - Ease(last + .05f, last + .25f, t));
            float limp = Ease(last + .1f, last + .35f, t);
            float woken = Ease(v.Wake - .05f, v.Wake + .3f, t);
            float calm = 1f - hit;
            // Looking up at the storm, uneasy, until it hits them.
            float lookUp = Ease(NmLookUp + .12f * index, NmLookUp + .12f * index + .5f, t) * calm;

            float breathe = Mathf.Sin(t * 2.1f + seat) * 1.8f * calm;
            float shift = Mathf.Sin(t * 1.3f + seat * .7f) * 1.6f * calm;
            if (v.Root.T != null) v.Root.T.position += right * (Mathf.Sin(t * 1.3f + seat * .7f) * .02f * calm) - fwd * (.06f * jerk);
            float shiver = lookUp * Mathf.Sin(t * 47f + seat) * .8f;
            float torsoPitch = breathe - lookUp * 5f - jerk * 20f - struggle * 6f + limp * (1f - woken) * 12f;
            float torsoTwist = struggle * Mathf.Sin(t * 19f + seat) * 9f + (turn * .8f * chestTurn - bodyYaw);
            if (v.Torso.T != null)
            {
                Turn(v.Torso.T, up, torsoTwist);
                Turn(v.Torso.T, right, torsoPitch);
                Turn(v.Torso.T, fwd, shift + shiver);
            }

            float headYaw = struggle * Mathf.Sin(t * 27f + seat) * 16f + Mathf.Sin(t * .9f + seat) * 4f * calm
                          + (turn * headTurn - turn * .8f * chestTurn);
            // Snapped back by every hit, hanging as they go limp, lifted as they wake (film r20: drooping past 22 degrees the top
            // of their hair filled the lens).
            float headPitch = -lookUp * 20f - jerk * 26f + limp * (1f - woken) * 18f - woken * 4f;
            if (v.HeadBone.T != null)
            {
                Turn(v.HeadBone.T, up, headYaw);
                Turn(v.HeadBone.T, v.HeadBone.T.parent != null ? Vector3.Cross(up, v.HeadBone.T.parent.forward).normalized : right, headPitch);
                // The possessed tilt as they stare.
                Turn(v.HeadBone.T, fwd, woken * 14f * ((v.Seat & 1) == 0 ? 1f : -1f));
            }

            // Arms flung out by each hit, then clawing at their chest while they fight it, hanging as they go limp.
            float fling = jerk * 70f;
            float claw = struggle * (1f - limp) * 80f;
            float armsOut = calm * Mathf.Sin(t * 1.7f + seat) * 2f;
            if (v.ArmL.T != null) { Turn(v.ArmL.T, right, -claw - jerk * 30f); Turn(v.ArmL.T, fwd, armsOut + fling + struggle * 8f * Mathf.Sin(t * 23f)); }
            if (v.ArmR.T != null) { Turn(v.ArmR.T, right, -claw * .92f - jerk * 30f); Turn(v.ArmR.T, fwd, -armsOut - fling - struggle * 8f * Mathf.Sin(t * 21f)); }
        }

        private static void Turn(Transform bone, Vector3 axis, float degrees)
        {
            if (Mathf.Abs(degrees) < .01f) return;
            bone.rotation = Quaternion.AngleAxis(degrees, axis) * bone.rotation;
        }

        // ------------------------------------------------------------------ camera and grade

        private struct NmKey
        {
            public float T, Fov;
            public Vector3 Eye, Look;
            public NmKey(float t, Vector3 eye, Vector3 look, float fov) { T = t; Eye = eye; Look = look; Fov = fov; }
        }

        /// <summary>
        /// One continuous move through keys: a cubic through each (Hermite on their own times), still at its first and last
        /// key and wherever two keys hold the same place, so speed eases in and out instead of v4's straight lines between
        /// corners.
        /// </summary>
        private static void NmPath(NmKey[] keys, float t, out Vector3 eye, out Vector3 look, out float fov)
        {
            int n = keys.Length;
            if (t <= keys[0].T) { eye = keys[0].Eye; look = keys[0].Look; fov = keys[0].Fov; return; }
            if (t >= keys[n - 1].T) { eye = keys[n - 1].Eye; look = keys[n - 1].Look; fov = keys[n - 1].Fov; return; }
            int i = 0;
            while (i < n - 2 && t > keys[i + 1].T) i++;
            var a = keys[i]; var b = keys[i + 1];
            float span = Mathf.Max(1e-4f, b.T - a.T);
            float u = (t - a.T) / span;
            Vector3 Tan(int k, System.Func<NmKey, Vector3> f)
            {
                if (k <= 0 || k >= n - 1) return Vector3.zero;
                if ((f(keys[k + 1]) - f(keys[k])).sqrMagnitude < 1e-6f || (f(keys[k]) - f(keys[k - 1])).sqrMagnitude < 1e-6f) return Vector3.zero;
                return (f(keys[k + 1]) - f(keys[k - 1])) / Mathf.Max(1e-4f, keys[k + 1].T - keys[k - 1].T) * span;
            }
            Vector3 Herm(Vector3 p0, Vector3 m0, Vector3 p1, Vector3 m1)
            {
                float u2 = u * u, u3 = u2 * u;
                return (2 * u3 - 3 * u2 + 1) * p0 + (u3 - 2 * u2 + u) * m0 + (-2 * u3 + 3 * u2) * p1 + (u3 - u2) * m1;
            }
            eye = Herm(a.Eye, Tan(i, k => k.Eye), b.Eye, Tan(i + 1, k => k.Eye));
            look = Herm(a.Look, Tan(i, k => k.Look), b.Look, Tan(i + 1, k => k.Look));
            fov = Mathf.Lerp(a.Fov, b.Fov, u * u * (3f - 2f * u));
        }

        /// <summary>
        /// Computed from her face and from where the players really are. S1 her, head and torso; S2 high and wide behind her,
        /// her in the near corner and the storm over the whole court; S3 low in front of one player with the storm in the sky
        /// behind them, then one eased crane up and back (S4) to hold every player.
        /// </summary>
        private void NemuFrame(int index, float t, ref Vector3 eye, ref Vector3 look, ref float fov)
        {
            Vector3 up = Vector3.up;
            if (t < NmWide)
            {
                // S1 THE CRACKS: her whole body and the court round her, from in front and above, so the cracks splitting out from
                // her feet and the souls clawing up out of them are in the frame with her (owner: the face close-up *"donnt make
                // sense"*). A slow push in.
                Vector3 face = Vector3.forward;
                if (_head != null) { face = _root.transform.InverseTransformDirection(_head.rotation * _nmFaceLocal); face.y = 0f; }
                face = face.sqrMagnitude > 1e-4f ? face.normalized : Vector3.forward;
                Vector3 across = Vector3.Cross(up, face);
                float u = Ease(0f, NmWide, t);
                // (film r29: from 4.4 m out and 2.2 m up she was small and the cracks lay edge-on; from above they spread.)
                eye = face * Mathf.Lerp(3.3f, 2.9f, u) + across * .8f + up * Mathf.Lerp(3.4f, 2.9f, u);
                look = face * .3f + up * Mathf.Lerp(.25f, .55f, u);
                fov = Mathf.Lerp(58f, 54f, u);
            }
            else if (_nmVictims.Count > 0)
            {
                Vector3 way = _nmWay, right = Vector3.Cross(up, way);
                var first = _nmVictims[0];
                Vector3 f0 = NmForward(first), s0 = Vector3.Cross(up, f0);
                if (t < NmDive)
                {
                    // S2 THE STORM: high behind her, turning slowly round the storm's eye; she is in the near corner as they pour
                    // out of her, the players below look up at it.
                    float u = Ease(NmWide, NmDive, t);
                    float swing = Mathf.Lerp(-8f, 10f, u);
                    Vector3 from = Quaternion.AngleAxis(swing, up) * (-way * 3.4f + right * 1.6f) + up * Mathf.Lerp(3.4f, 4.4f, u);
                    eye = from;
                    look = Vector3.Lerp(_nmEyeOfStorm * .6f, _nmEyeOfStorm, u) + up * Mathf.Lerp(1.4f, 1.9f, u);
                    fov = Mathf.Lerp(58f, 64f, u);
                }
                else
                {
                    // S3 THE DIVE, then S4: one path. Low in front of them looking up, so the storm fills the sky behind.
                    float headY = first.Top * .8f;
                    Vector3 lookNear = first.Home + up * (headY + .45f);
                    float back0 = 4.3f;
                    foreach (var v in _nmVictims)
                    {
                        Vector3 off = v.Home - _nmCentre;
                        back0 = Mathf.Max(back0, (Mathf.Abs(Vector3.Dot(off, right)) + 1.4f) / .62f - Vector3.Dot(off, way));
                    }
                    Vector3 wide = _nmCentre - way * back0 + right * 2.4f + up * 3.9f;
                    Vector3 lookWide = _nmCentre + up * 1.5f;
                    float wake = first.Wake;
                    var keys = new[]
                    {
                        new NmKey(NmDive, first.Home + f0 * 3.6f + s0 * .7f + up * .5f, lookNear + up * .3f, 54f),
                        new NmKey(_nmFirstHit, first.Home + f0 * 3.1f + s0 * .6f + up * .55f, lookNear, 52f),
                        new NmKey(wake, first.Home + f0 * 2.6f + s0 * .45f + up * .7f, first.Home + up * (headY + .15f), 46f),
                        new NmKey(Mathf.Max(wake + .25f, NmPullBack), first.Home + f0 * 2.6f + s0 * .45f + up * .7f, first.Home + up * (headY + .15f), 46f),
                        new NmKey(Seconds, wide, lookWide, 56f),
                    };
                    NmPath(keys, t, out eye, out look, out fov);
                    // One shove of the lens as the first soul hits.
                    if (!_reducedEffects)
                    {
                        float push = Mathf.Clamp01((t - _nmFirstHit) / .06f) * (1f - Ease(_nmFirstHit + .06f, _nmFirstHit + .4f, t));
                        eye += (first.Home + up * headY - eye).normalized * (.18f * push);
                    }
                }
                eye = KeepLensOffOthers(eye, 1.4f);
                // Never inside her.
                Vector3 flat = new Vector3(eye.x, 0f, eye.z);
                if (flat.magnitude < .9f) eye += (flat.sqrMagnitude > 1e-4f ? flat.normalized : -way) * (.9f - flat.magnitude);
            }
            _nmEye = eye; _nmLook = look;
        }

        /// <summary>
        /// The ink (`Shaders/NemuInk`): the frame's edges sink a little into near-black from her eyes on; one impact frame as the
        /// first soul hits; focus lines as her eyes ignite, as the storm bursts out, as the first soul hits. Lets go as play returns.
        /// </summary>
        private void NemuPostProcess(RenderTexture frame, Camera camera, float t)
        {
            float ink = Ease(NmIgnite, NmIgnite + .3f, t) * .7f * (1f - Ease(Seconds - .6f, Seconds, t));
            float impact = !_reducedEffects && t >= _nmFirstHit && t < _nmFirstHit + NmImpact ? 1f : 0f;
            bool invert = t < _nmFirstHit + NmImpact * .5f;
            float lines = _reducedEffects ? 0f : Mathf.Max(Mathf.Max(
                Flash(t, NmIgnite + .1f, .22f),
                .7f * Flash(t, NmBurst + .15f, .3f)),
                Flash(t, _nmFirstHit + .1f, .3f));
            if (ink <= .01f && impact <= 0f && lines <= .01f) return;
            if (_nmInk == null)
            {
                var shader = Resources.Load<Shader>("Shaders/NemuInk");
                if (shader == null) return;
                _nmInk = new Material(shader) { name = "NemuInk" };
            }
            _nmInk.SetFloat("_Ink", ink);
            _nmInk.SetFloat("_Black", 0f);
            _nmInk.SetFloat("_Night", 0f);
            _nmInk.SetFloat("_Impact", impact);
            _nmInk.SetFloat("_Invert", impact > 0f && invert ? 1f : 0f);
            _nmInk.SetFloat("_Lines", lines);
            _nmInk.SetVector("_Focus", new Vector4(.5f, .55f, 0f, 0f));
            _nmInk.SetFloat("_Clock", t);
            _nmInk.SetFloat("_Aspect", frame.height > 0 ? frame.width / (float)frame.height : 16f / 9f);
            var tmp = RenderTexture.GetTemporary(frame.descriptor);
            Graphics.Blit(frame, tmp, _nmInk);
            Graphics.Blit(tmp, frame);
            RenderTexture.ReleaseTemporary(tmp);
        }

        /// <summary>Daylight stays (v5's black night hid everything); the world steps back as the storm comes, so it is eerie and
        /// the violet heads read, but the black tails still read against the court.</summary>
        private void NemuGrade(float t, out float brightness, out float saturation)
        {
            float dim = Ease(NmIgnite, NmBurst + .6f, t) * (1f - Ease(Seconds - .6f, Seconds, t));
            brightness = Mathf.Lerp(1f, .72f, dim);
            saturation = Mathf.Lerp(1f, .7f, dim);
        }

        private void NemuDispose()
        {
            _nmSmoke?.Dispose(); _nmSmoke = null;
            foreach (var g in _nmEyeGlows) g.Remove();
            _nmEyeGlows.Clear(); _nmHerEyes = null;
            if (_nmInk != null) { ObjectDestroy(_nmInk); _nmInk = null; }
        }
    }
}
