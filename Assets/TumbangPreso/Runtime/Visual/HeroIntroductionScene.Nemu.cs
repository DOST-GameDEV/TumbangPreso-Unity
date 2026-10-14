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
        //   S2 THE STORM (0.90-2.20): high and wide; the cracks run on across the whole court, souls pour up out of them and
        //     spiral into a vortex circling the court; she throws her arms up; the players look up at it.
        //   v8 (owner on v7: *"its not obvious that theyre entering the ppl"*) makes the ENTRY the shot, one soul first:
        //   S3 THE FIRST (2.20-3.50): the lens finds ONE soul in the storm, a thread of violet links it to the first player's
        //     chest, and the lens rides it down as it peels off and dives (slow, then plunging). It hits: a ring bursts round
        //     the spot, he is thrown back along its line, back arched, head snapped, feet off the court, held; the head squashes
        //     into him and sinks, its tail pours in after it into a black hole that closes. Then three more drain in quickly.
        //   S4 THE FLOOD (3.50-4.40): smash cuts to the other players, each hit by its own soul the same way, so "everyone is
        //     being entered" reads before the crane.
        //   S5 ALL OF THEM (4.40-5.90): back on the first, the violet light leaking from his chest climbs his neck into his
        //     eyes, they ignite, and the lens cranes up and back: every player possessed, the storm thinning.
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
        private const float NmIgnite = .32f, NmWide = .90f, NmBurst = .95f, NmLookUp = 1.4f, NmDive = 2.2f;
        /// <summary>The cuts: to the other players (THE FLOOD), back to the first, and the crane up.</summary>
        private const float NmFlood = 3.5f, NmReturn = 4.4f, NmCrane = 4.75f;
        /// <summary>The first soul: when it peels off the storm and when it hits (slow, so the lens can ride it down).</summary>
        private const float NmHeroOut = 2.28f, NmHeroIn = 2.98f;
        /// <summary>The first player's eyes open just after the cut back to him.</summary>
        private const float NmFirstWake = 4.58f;
        /// <summary>Souls per player, and (THE FLOOD) when a cut-to player's souls hit after the cut.</summary>
        private const int NmPerVictim = 4;
        private static readonly float[] NmFloodHits = { .17f, .27f, .34f, .41f };
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
            /// <summary>A diver: when it leaves the storm, when its head meets the body, and how long it takes to sink in.</summary>
            public float Out, In, Sink;
            public Vector3 Offset;
            /// <summary>The way it goes into the body (scene space): into their front, along the line it came from.</summary>
            public Vector3 Dir;
            /// <summary>The first soul of all, the one the lens rides down.</summary>
            public bool Hero;
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
            /// <summary>Which soul makes each hit, and how hard it lands (the first soul of all is held, above 1).</summary>
            public int[] Souls;
            public float[] Strength;
            public float Wake;
            /// <summary>Which side of them the lens stands (+1 or -1); their first soul comes in from the other.</summary>
            public float Side = 1f;
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
        private float _nmFaceDepth = .3f, _nmHeadTop = 1.2f, _nmStormReach = 6f, _nmFirstHit = NmHeroIn;
        /// <summary>The first soul's index (-1 if no player), how many players THE FLOOD cuts to, and how long each cut is.</summary>
        private int _nmHero = -1, _nmCuts;
        private float _nmCutLength = .45f;
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
            // The divers, four per player, taken from the main storm (never the ones that leaked out in S1). v8: each is the
            // soul that is nearest, as it peels off, to where the shot wants it (in the sky on the far side from the lens), so the
            // dive crosses the frame into their front instead of arriving from anywhere in the sky at once.
            _nmCuts = Mathf.Min(2, victims - 1);
            _nmCutLength = (NmReturn - NmFlood) / Mathf.Max(1, _nmCuts);
            var used = new HashSet<int>();
            Vector3 up = Vector3.up;
            for (int v = 0; v < victims; v++)
            {
                var victim = _nmVictims[v];
                Vector3 f = NmForward(victim), side = Vector3.Cross(up, f);
                int cut = Mathf.Min(v, _nmCuts);
                float sgn = v == 0 || (cut & 1) == 0 ? 1f : -1f;
                victim.Side = sgn;
                victim.Hits = new float[NmPerVictim]; victim.Souls = new int[NmPerVictim]; victim.Strength = new float[NmPerVictim];
                // A cut-to player's souls land just after the cut to them; any past the second cut land with the last, off screen.
                float cutAt = NmFlood + (cut - 1) * _nmCutLength + (v > _nmCuts ? .06f * (v - _nmCuts) : 0f);
                for (int j = 0; j < NmPerVictim; j++)
                {
                    float inAt, dive, sink, k;
                    Vector3 target;
                    if (v == 0 && j == 0)
                    {
                        // THE FIRST: slow, held, from high on the far side so the lens can ride it across and down.
                        inAt = NmHeroIn; dive = NmHeroIn - NmHeroOut; sink = .3f; k = 1.4f;
                        target = victim.Home + f * 3.6f - side * (sgn * .6f) + up * 3.0f;   // (film r33: from the far side it was a speck)
                    }
                    else if (v == 0)
                    {
                        // Then the drain: three more pour in fast from just above him.
                        inAt = 3.22f + .09f * (j - 1); dive = .3f; sink = .16f; k = .55f;
                        target = victim.Home + f * 1.3f + side * (sgn * .8f * (j - 2)) + up * 3.3f;
                    }
                    else
                    {
                        inAt = cutAt + NmFloodHits[j]; dive = j == 0 ? .36f : .3f; sink = j == 0 ? .2f : .14f; k = j == 0 ? 1f : .5f;
                        target = j == 0 ? victim.Home + f * 2f - side * (sgn * 1.6f) + up * 3f
                                        : victim.Home + f * 1.2f + side * (sgn * .8f * (j - 2)) + up * 3.2f;
                    }
                    float outAt = inAt - dive;
                    int pick = NmPick(target, outAt, used, early, count);
                    used.Add(pick);
                    var soul = _nmSouls[pick];
                    soul.Victim = v; soul.Out = outAt; soul.In = inAt; soul.Sink = sink; soul.Hero = v == 0 && j == 0;
                    // Each goes in at its own spot on the chest, not one point (the first dead centre).
                    soul.Offset = soul.Hero ? Vector3.zero : new Vector3((NmHash(pick, 5) - .5f) * .2f, (NmHash(pick, 6) - .3f) * .2f, 0f);
                    Vector3 from = NmOrbit(soul, outAt, out _);
                    Vector3 chest = victim.Home + up * (victim.Top * .55f);
                    soul.Dir = ((chest - from).normalized * .45f - f * .8f + Vector3.down * .15f).normalized;
                    _nmSouls[pick] = soul;
                    victim.Hits[j] = inAt; victim.Souls[j] = pick; victim.Strength[j] = k;
                }
                victim.Wake = v == 0 ? NmFirstWake : victim.Hits[NmPerVictim - 1] + .55f;
            }
            if (victims > 0) { _nmHero = _nmVictims[0].Souls[0]; _nmFirstHit = _nmVictims[0].Hits[0]; }
        }

        /// <summary>The storm soul (not yet a diver, and already risen into the storm) nearest <paramref name="target"/> at
        /// <paramref name="at"/>.</summary>
        private int NmPick(Vector3 target, float at, HashSet<int> used, int from, int count)
        {
            int best = -1; float bestGap = float.MaxValue;
            for (int pass = 0; pass < 2 && best < 0; pass++)
                for (int i = from; i < count; i++)
                {
                    var soul = _nmSouls[i];
                    if (used.Contains(i) || soul.Victim >= 0) continue;
                    if (pass == 0 && soul.Born + soul.Rise > at - .05f) continue;
                    float gap = (NmOrbit(soul, at, out _) - target).sqrMagnitude;
                    if (gap < bestGap) { bestGap = gap; best = i; }
                }
            return Mathf.Max(best, from);
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
            v.Hits = new[] { NmDive + .4f }; v.Souls = new[] { 0 }; v.Strength = new[] { 1f };
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
            Vector3 orbit;
            if (soul.Victim >= 0 && s >= soul.Out)
            {
                var victim = _nmVictims[soul.Victim];
                Vector3 door = NmDoor(soul, victim);
                if (s >= soul.In)
                {
                    // INTO THEM: the head meets their front and pushes on in along its line, slowing as the body takes it
                    // (the body hides what is inside, so for a few frames its face is half in him); its tail follows.
                    float p = (s - soul.In) / Mathf.Max(.01f, soul.Sink);
                    if (p >= 1f) return false;
                    at = door + soul.Dir * ((victim.Width * .42f + .12f) * (1f - (1f - p) * (1f - p)));
                    return true;
                }
                // THE DIVE: a curve from its flight in the storm into their front along its own line. The first soul peels off
                // slowly, swoops, and slows again as it reaches him, so its press into his chest is seen (film r35: plunging
                // at full speed into him it was a smear).
                float span = soul.In - soul.Out;
                float u = (s - soul.Out) / span;
                if (soul.Hero) u = u * u * (3f - 2f * u);
                Vector3 from = NmOrbit(soul, soul.Out, out var vel);
                Vector3 m0 = vel * (span * (soul.Hero ? .35f : .6f));
                Vector3 m1 = soul.Dir * ((door - from).magnitude * 1.2f);
                float u2 = u * u, u3 = u2 * u;
                at = (2 * u3 - 3 * u2 + 1) * from + (u3 - 2 * u2 + u) * m0 + (-2 * u3 + 3 * u2) * door + (u3 - u2) * m1;
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

        /// <summary>Where a diver meets the body (scene space): out from their chest, back along its own line, to their front.</summary>
        private static Vector3 NmDoor(in NmSoul soul, NmVictim v)
        {
            Vector3 fwd = NmForward(v), side = Vector3.Cross(Vector3.up, fwd);
            return v.Chest + side * soul.Offset.x + Vector3.up * soul.Offset.y - soul.Dir * (v.Width * .42f);
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
            // While the lens rides the first soul down, the rest of the storm steps back so it is the one you follow.
            if (_nmHero >= 0 && !soul.Hero)
                fade *= 1f - .6f * Ease(NmDive - .1f, NmDive + .1f, t) * (1f - Ease(NmHeroIn + .1f, NmHeroIn + .5f, t));
            if (fade <= .01f) return;
            for (int k = 0; k < NmTail; k++) _nmTailOn[k] = NmSoulAt(soul, index, t - k * NmTailStep, out _nmTailAt[k]);

            // Each soul its own size (owner on v7: faces *"copy pasted"*), the first soul a little larger.
            float headSize = soul.Hero ? .5f : .36f + .12f * NmHash(index, 21);
            // A diver near its door: the tail narrows as it is sucked into the hole.
            bool entering = soul.Victim >= 0 && t > soul.In - .35f;
            Vector3 door = entering ? NmDoor(soul, _nmVictims[soul.Victim]) : default;

            // THE TAIL: strands from the head back along its path, thinning, fraying and fading toward the end.
            // (film r29: at 0.3 m and a dim glow the heads read as dark smudges with dots from across the court.)
            for (int k = 0; k < NmTail - 1; k++)
            {
                if (!_nmTailOn[k] || !_nmTailOn[k + 1]) continue;
                Vector3 a = NmW(_nmTailAt[k]), b = NmW(_nmTailAt[k + 1]);
                Vector3 d = a - b;
                float len = d.magnitude;
                if (len < 1e-3f) continue;
                float u = k / (float)(NmTail - 1);
                float width = headSize * .75f * Mathf.Pow(1f - u, .8f) + .025f;
                if (entering) width *= Mathf.Lerp(.3f, 1f, Mathf.Clamp01(Vector3.Distance((_nmTailAt[k] + _nmTailAt[k + 1]) * .5f, door) / .7f));
                _nmSmoke.Wisp((a + b) * .5f, d, width, len * 1.4f + width, fade * (.9f - .5f * u), index * .071f + k * .13f, u * .55f, .9f);
            }

            // THE HEAD (gone once it has sunk in: only its tail is left, pouring in after it).
            if (!_nmTailOn[0]) return;
            Vector3 head = NmW(_nmTailAt[0]);
            float dist = Vector3.Distance(head, eye);
            if (dist < .5f) return;
            // Which way it is flying (world): the face is on the front of the head, not turned to the lens (owner on v7: *"they
            // all face the same friggingw ay despite not having the same angles"*).
            Vector3 heading = _nmTailOn[1] ? head - NmW(_nmTailAt[1]) : Vector3.zero;
            if (heading.sqrMagnitude < 1e-6f && NmSoulAt(soul, index, t + .02f, out var ahead)) heading = NmW(ahead) - head;
            heading = heading.sqrMagnitude > 1e-6f ? heading.normalized : (eye - head) / dist;
            // Sinking into a body: squashed flat against it along its line, then pushed in.
            float sink = soul.Victim >= 0 && t >= soul.In ? Mathf.Clamp01((t - soul.In) / Mathf.Max(.01f, soul.Sink)) : -1f;
            float squash = sink >= 0f ? 1f - .45f * Mathf.Sqrt(sink) : 1f;
            Vector3 axis = sink >= 0f ? NmWDir(soul.Dir) : heading;
            // Drawn a little long along its flight (squashed along it as it presses in).
            _nmSmoke.Wisp(head, axis, headSize * (sink >= 0f ? 1f + .5f * sink : 1f), headSize * (sink >= 0f ? squash : 1.18f),
                          fade * .95f, index * .113f, 0f, 1f);
            // Lit from inside: violet, never white (strength kept under the clip).
            _nmSmoke.Light(head, headSize * (soul.Hero ? 1.25f : 1.6f) * (sink >= 0f ? 1f - .3f * sink : 1f), (soul.Hero ? 1.15f : .95f) * fade, NemuSmokeBatch.Violet);
            DrawSoulFace(index, t, head, heading, headSize, squash, fade, eye, dist);
        }

        /// <summary>
        /// A soul's face, on the front of its head the way it flies: seen whole as it comes at the lens, in profile as it
        /// crosses, gone as it flies away. Every soul its own: the size, spacing and slant of its hollow eyes (grieving, raging or
        /// blank, one sometimes larger), and its mouth (a tall wail, a round scream, a wide crooked howl), each wailing on its
        /// own beat. Nothing random: all from its index.
        /// </summary>
        private void DrawSoulFace(int index, float t, Vector3 head, Vector3 heading, float size, float squash, float fade, Vector3 eye, float dist)
        {
            Vector3 toLens = (eye - head) / dist;
            Vector3 faceUp = Vector3.up - heading * Vector3.Dot(Vector3.up, heading);
            faceUp = faceUp.sqrMagnitude > 1e-4f ? faceUp.normalized : Vector3.Cross(heading, Vector3.Cross(Vector3.up, toLens)).normalized;
            Vector3 faceRight = Vector3.Cross(faceUp, heading);
            float r = size * .5f;

            float spread = .3f + .14f * NmHash(index, 22);
            float eyeH = .12f + .1f * NmHash(index, 23);
            float slant = (NmHash(index, 24) - .4f) * 1.3f;          // + inner corners up (grief), - down (rage)
            float eyeW = .13f + .06f * NmHash(index, 25), eyeL = .22f + .1f * NmHash(index, 26);
            float odd = NmHash(index, 27) > .65f ? .3f * (NmHash(index, 28) - .5f) : 0f;   // one eye larger
            int mouth = (int)(NmHash(index, 29) * 3f);                 // 0 tall wail, 1 round scream, 2 wide crooked howl
            float beat = 5f + 6f * NmHash(index, 30);
            float wail = .5f + .5f * Mathf.Sin(t * beat + index * 1.7f);

            // A point on the head's front: across, up (head radii), and how much it faces the lens.
            Vector3 On(float x, float y, out float facing)
            {
                y *= squash;
                float z = Mathf.Sqrt(Mathf.Max(0f, 1f - x * x - y * y));
                Vector3 n = heading * z + faceRight * x + faceUp * y;
                facing = Vector3.Dot(n, toLens);
                return head + n * r;
            }
            for (int e = -1; e <= 1; e += 2)
            {
                Vector3 at = On(e * spread, eyeH, out float facing);
                float show = Mathf.Clamp01(facing * 3f + .65f);   // (film r36: in profile at .2 the face was gone)
                if (show <= .01f) continue;
                float big = 1f + e * odd;
                Vector3 along = (faceUp + faceRight * (e * slant * .5f)).normalized;
                float narrow = Mathf.Lerp(.35f, 1f, Mathf.Clamp01(facing));
                _nmSmoke.Mark(at, along, size * eyeW * big * narrow, size * eyeL * big * squash, fade * show, NmHash(index, 31 + e));
            }
            {
                float open = mouth == 0 ? .3f + .18f * wail : mouth == 1 ? .22f + .1f * wail : .16f + .08f * wail;
                float wide = mouth == 0 ? .19f : mouth == 1 ? .22f + .06f * wail : .36f;
                float crook = mouth == 2 ? (NmHash(index, 33) - .5f) * .5f : 0f;
                Vector3 at = On(crook * .3f, -.32f - open * .45f, out float facing);
                float show = Mathf.Clamp01(facing * 3f + .65f);   // (film r36: in profile at .2 the face was gone)
                if (show > .01f)
                {
                    Vector3 along = (faceUp + faceRight * crook).normalized;
                    float narrow = Mathf.Lerp(.35f, 1f, Mathf.Clamp01(facing));
                    float across = size * wide * narrow, tall = size * open * squash * 1.2f;
                    // A mark is never wider than it is long, so a wide howl is drawn lying along the face's right instead.
                    if (across > tall) _nmSmoke.Mark(at, Vector3.Cross(heading, along), tall, across, fade * show, NmHash(index, 34));
                    else _nmSmoke.Mark(at, along, across, tall, fade * show, NmHash(index, 34));
                }
            }
        }

        /// <summary>
        /// What shows on a player as the souls go in (v8, owner on v7: *"its not obvious that theyre entering the ppl"*): a thread
        /// linking each soul to the spot it will go in; at the hit a ring bursting round that spot (and, the hardest, round their
        /// feet on the court), a flash, smoke punched out of their back; a black hole ringed violet that swallows the tail and
        /// closes; the aura stepping up with every soul; the violet left in their chest climbing up into their eyes as they wake.
        /// </summary>
        private void SampleVictim(NmVictim v, int index, float t)
        {
            Vector3 up = Vector3.up;
            Vector3 fwd = NmForward(v);
            Vector3 chest = v.Chest;
            int n = v.Hits.Length;
            float tailSeconds = NmTail * NmTailStep;

            for (int h = 0; h < n; h++)
            {
                var soul = _nmSouls[v.Souls[h]];
                float k = v.Strength[h];
                Vector3 dir = soul.Dir;
                Vector3 door = NmDoor(soul, v);
                Vector3 a = Vector3.Cross(dir, up); a = a.sqrMagnitude > 1e-4f ? a.normalized : Vector3.Cross(up, fwd);
                Vector3 b = Vector3.Cross(dir, a);

                // THE LINK: just before it peels off and all through its dive, a thread of violet from the soul to the spot it
                // will go in, with pulses running down it, and the spot itself throbbing on them.
                if (!_reducedEffects && t > soul.Out - .22f && t < soul.In && NmSoulAt(soul, v.Souls[h], t, out var at))
                {
                    float on = Ease(soul.Out - .22f, soul.Out + .05f, t) * Mathf.Min(1f, .45f + .45f * k);
                    float len = Vector3.Distance(at, door);
                    int dots = Mathf.Clamp(Mathf.CeilToInt(len / .13f), 2, 40);
                    for (int j = 1; j < dots; j++)
                    {
                        float u = j / (float)dots;
                        float pulse = .5f + .5f * Mathf.Sin(u * len * 5f - t * 30f);
                        _nmSmoke.Light(NmW(Vector3.Lerp(at, door, u)), .08f + .03f * pulse, on * (.2f + .4f * pulse), NemuSmokeBatch.Violet);
                    }
                    _nmSmoke.Light(NmW(door - dir * .05f), .3f + .1f * k, on * (.55f + .35f * Mathf.Sin(t * 20f)), NemuSmokeBatch.Violet, default, .4f);
                }

                float since = t - v.Hits[h];
                if (since < 0f) continue;

                // THE HIT: a ring bursting out round the spot, across its line; a flash; smoke punched out of their back.
                if (since < .32f)
                {
                    float e = 1f - Mathf.Pow(1f - since / .32f, 3f), gone = 1f - since / .32f;
                    float r = .12f + (k > 1f ? 1.3f : .4f + .5f * k) * e;
                    const int ring = 20;
                    for (int j = 0; j < ring; j++)
                    {
                        float ang = j * Mathf.PI * 2f / ring;
                        Vector3 p = door - dir * .06f + (a * Mathf.Cos(ang) + b * Mathf.Sin(ang)) * r;
                        _nmSmoke.Light(NmW(p), .13f + .05f * k, 1.1f * Mathf.Min(1f, k) * gone, NemuSmokeBatch.Violet, default, .5f);
                    }
                    // The hardest also rings out across the court round their feet (flat, so pulled: never cut by the court).
                    if (k >= 1f)
                        for (int j = 0; j < ring; j++)
                        {
                            float ang = j * Mathf.PI * 2f / ring;
                            Vector3 p = v.Home + new Vector3(Mathf.Cos(ang), 0f, Mathf.Sin(ang)) * (.3f + 1.5f * e) + up * .12f;
                            _nmSmoke.Light(NmW(p), .22f, .9f * gone, NemuSmokeBatch.Violet, default, 1f);
                        }
                    _nmSmoke.Light(NmW(chest), v.Width * 1.2f * Mathf.Min(1.2f, k), 1.1f * Mathf.Min(1f, k) * Flash(t, v.Hits[h] + .02f, .1f), NemuSmokeBatch.Violet);
                    float burst = since / .32f;
                    for (int j = 0; j < 7; j++)
                    {
                        float ang = (j * 51f + h * 23f + index * 17f) * Mathf.Deg2Rad;
                        Vector3 outDir = (a * Mathf.Cos(ang) + up * (.4f + .6f * Mathf.Abs(Mathf.Sin(ang))) + dir * .9f).normalized;
                        float reach = .25f + 1.0f * burst;
                        _nmSmoke.Wisp(NmW(chest + outDir * reach * .6f), NmWDir(outDir), .07f * Mathf.Min(1f, k), reach * .8f,
                                      Mathf.Min(1f, k) * .8f * (1f - burst), j * .07f + h * .3f, burst * .7f, 1f);
                    }
                }

                // THE HOLE: while it sinks in and its tail pours after it, a black hole on them, ringed violet, strands of smoke
                // spiralling into it; it closes behind the tail.
                float hole = soul.Sink + tailSeconds * .5f;
                if (since < hole)
                {
                    float rad = (.07f + .03f * Mathf.Min(1f, k))   // (film r33: at .18 m a black ball sat on the chest)
                               * Ease(0f, .04f, since) * (1f - Ease(hole * .55f, hole, since));
                    if (rad > .005f)
                    {
                        Vector3 mouth = door - dir * .05f;
                        _nmSmoke.Mark(NmW(mouth), NmWDir(b), rad * 2f, rad * 2f, .9f, .5f + h * .1f);
                        const int rim = 12;
                        for (int j = 0; j < rim; j++)
                        {
                            float ang = j * Mathf.PI * 2f / rim + since * 9f;
                            _nmSmoke.Light(NmW(mouth + (a * Mathf.Cos(ang) + b * Mathf.Sin(ang)) * rad * 1.3f), .06f, 1.2f, NemuSmokeBatch.Violet, default, .5f);
                        }
                        float p = since / hole;
                        for (int j = 0; j < 6; j++)
                        {
                            float ang = j * Mathf.PI / 3f + since * 14f;
                            Vector3 round = a * Mathf.Cos(ang) + b * Mathf.Sin(ang), spin = a * -Mathf.Sin(ang) + b * Mathf.Cos(ang);
                            float out_ = rad * (2.6f - 1.4f * p);
                            Vector3 pos = mouth + round * out_ - dir * (.15f * (1f - p));
                            _nmSmoke.Wisp(NmW(pos), NmWDir(-round * out_ + spin * out_ * .7f + dir * .1f), .05f, out_ * 1.3f, .7f * (1f - p), j * .17f + h * .3f, p * .5f, 1f);
                        }
                    }
                }
            }

            // ---- WHAT IS LEFT: the aura (`NemuAura`, the same drawing play keeps), stepping up with every soul in.
            float auraOn = 0f, swell = Flash(t, v.Wake, .3f);
            for (int h = 0; h < n; h++)
            {
                auraOn += Ease(v.Hits[h] - .02f, v.Hits[h] + .1f, t) * (h == 0 ? .4f : .6f / Mathf.Max(1, n - 1));
                swell = Mathf.Max(swell, Mathf.Min(1f, v.Strength[h]) * Flash(t, v.Hits[h] + .04f, .22f));
            }
            auraOn = Mathf.Min(1f, auraOn);
            if (auraOn > 0f)
            {
                var state = new NemuAura.State
                {
                    Feet = NmW(v.Home), Forward = v.Holder.transform.forward, Stream = Vector3.zero, Top = v.Top, Width = v.Width, Clock = t,
                    Amount = auraOn, Swell = swell, Drag = 0f, Thin = 0f, Seat = v.Seat, Reduced = _reducedEffects,
                };
                NemuAura.Draw(_nmSmoke, state);
            }

            // ---- THEIR EYES: their own painted eyes burn violet, flaring as they open, with a soft glow round them.
            Vector3 face = default;
            if (v.Head != null)
            {
                Vector3 look = v.Head.rotation * v.FaceLocal; look.y = 0f;
                look = look.sqrMagnitude > 1e-4f ? look.normalized : NmWDir(fwd);
                face = v.Head.position + up * (v.FaceTop * .28f) + look * (v.FaceDepth + .03f);
            }
            // THE CLIMB: the violet left in their chest leaks and throbs while they hang there, then climbs up the neck into the
            // eyes, and the eyes ignite as it arrives.
            if (t > v.Hits[0] && t < v.Wake + .05f)
            {
                Vector3 front = NmW(chest - (n > 0 ? _nmSouls[v.Souls[0]].Dir : -fwd) * (v.Width * .3f));
                float leak = auraOn * (.35f + .15f * Mathf.Sin(t * 13f + index)) * (1f - Ease(v.Wake - .3f, v.Wake - .1f, t));
                _nmSmoke.Light(front, v.Width * .6f, leak, NemuSmokeBatch.Violet, default, .3f);
                float climb = Ease(v.Wake - .3f, v.Wake, t);
                if (climb > 0f && climb < 1f && v.Head != null)
                    for (int j = 0; j < 5; j++)
                    {
                        float u = Mathf.Clamp01(climb - j * .06f);
                        _nmSmoke.Light(Vector3.Lerp(front, face, u), .16f - .02f * j, .9f * (1f - j * .18f),   // (film r34: at .26 it hid his face)
                                        NemuSmokeBatch.Violet, default, .3f);
                    }
            }
            float eyes = Ease(v.Wake, v.Wake + .06f, t);
            v.Eyes?.Set(eyes, t + v.Seat);
            if (eyes > 0f && v.Head != null)
                _nmSmoke.Light(face, v.FaceTop * .9f, eyes * (.3f + .9f * Flash(t, v.Wake + .05f, .15f)), NemuSmokeBatch.Violet);
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
            float turnAt = Mathf.Max(NmCrane + .1f, v.Wake + .2f) + .05f * index;
            float headTurn = Ease(turnAt, turnAt + .25f, t);
            float chestTurn = Ease(turnAt + .1f, turnAt + .4f, t);
            float bodyTurn = Ease(turnAt + .2f, turnAt + .5f, t);
            float bodyYaw = turn * .5f * bodyTurn;
            v.Holder.transform.localRotation = Quaternion.Euler(0f, v.Yaw + bodyYaw, 0f);
            Vector3 up = Vector3.up;
            Vector3 fwd = v.Holder.transform.forward, right = v.Holder.transform.right;

            // Every hit: a snap (a frame and a half) and a fall-off, thrown back along the line the soul came in on. The first
            // soul of all is held: arched, head back, feet off the court, for a beat before it lets go.
            float jerk = 0f, lift = 0f;
            Vector3 shove = Vector3.zero;
            for (int h = 0; h < v.Hits.Length; h++)
            {
                float x = t - v.Hits[h];
                if (x < 0f) continue;
                float k = v.Strength[h];
                float hold = k > 1f ? .22f : 0f;
                float j = x < .05f ? x / .05f : x < .05f + hold ? 1f : Mathf.Exp(-(x - .05f - hold) / .14f);
                jerk += k * j;
                Vector3 d = _nmSouls[v.Souls[h]].Dir; d.y = 0f;
                shove += d * (.16f * k * j);
                if (k > 1f) lift += .09f * j;
            }
            jerk = Mathf.Min(jerk, 1.5f);
            float hit = Ease(first - .02f, first + .02f, t);
            float struggle = hit * (1f - Ease(last + .05f, last + .25f, t));
            float limp = Ease(last + .1f, last + .35f, t);
            float woken = Ease(v.Wake - .05f, v.Wake + .3f, t);
            float calm = 1f - hit;
            // Looking up at the storm, uneasy, until it hits them.
            float lookUp = Ease(NmLookUp + .12f * index, NmLookUp + .12f * index + .5f, t) * calm;

            float breathe = Mathf.Sin(t * 2.1f + seat) * 1.8f * calm;
            float shift = Mathf.Sin(t * 1.3f + seat * .7f) * 1.6f * calm;
            if (v.Root.T != null) v.Root.T.position += right * (Mathf.Sin(t * 1.3f + seat * .7f) * .02f * calm) + NmWDir(shove) + up * lift;
            float shiver = lookUp * Mathf.Sin(t * 47f + seat) * .8f;
            float torsoPitch = breathe - lookUp * 5f - jerk * 22f - struggle * 6f + limp * (1f - woken) * 12f;
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
            float headPitch = -lookUp * 20f - Mathf.Min(jerk, 1.2f) * 26f + limp * (1f - woken) * 18f - woken * 4f;
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
                else if (t < (_nmCuts > 0 ? NmFlood : NmReturn))
                {
                    // S3 THE FIRST: low at his three-quarter, the lens finds the first soul in the storm and rides it down as it
                    // peels off and dives across the frame into his front; it holds on the hit as the rest drain in.
                    var hero = _nmSouls[_nmHero];
                    Vector3 eyeA = first.Home + f0 * 2.6f + s0 * 2.5f + up * .6f;
                    Vector3 eyeB = first.Home + f0 * 2.3f + s0 * 2.1f + up * .8f;
                    Vector3 eyeC = first.Home + f0 * 2.4f + s0 * 2.2f + up * .85f;
                    Vector3 chestLook = first.Home + up * (first.Top * .68f);
                    if (t < hero.In && NmSoulAt(hero, _nmHero, t, out var soulAt))
                    {
                        float w = Ease(hero.Out - .05f, hero.In + .02f, t);
                        eye = Vector3.Lerp(eyeA, eyeB, Ease(NmDive, hero.In, t));
                        // On the soul until it is nearly on him (film r34: settling early lost its plunge), tight on it up in the
                        // storm and widening onto him as it lands (film r33: at 62 it was a speck).
                        look = Vector3.Lerp(soulAt, chestLook, .1f + .85f * Mathf.Pow(w, 2.5f));
                        fov = Mathf.Lerp(40f, 52f, w * w);
                    }
                    else
                    {
                        float u = Ease(hero.In, _nmCuts > 0 ? NmFlood : NmReturn, t);
                        eye = Vector3.Lerp(eyeB, eyeC, u);
                        look = chestLook;
                        fov = Mathf.Lerp(52f, 50f, u);
                    }
                    if (!_reducedEffects)
                    {
                        // Shoved by the hit, along his line: in, then eased back out over the held beat.
                        float push = Mathf.Clamp01((t - _nmFirstHit) / .05f) * (1f - Ease(_nmFirstHit + .25f, _nmFirstHit + .5f, t));
                        eye += (chestLook - eye).normalized * (.22f * push);
                    }
                }
                else if (t < NmReturn)
                {
                    // S4 THE FLOOD: a smash cut to each other player, the soul already diving at them from the far side.
                    int c = Mathf.Clamp(1 + (int)((t - NmFlood) / _nmCutLength), 1, _nmCuts);
                    var v = _nmVictims[c];
                    float c0 = NmFlood + (c - 1) * _nmCutLength;
                    float u = Ease(c0, c0 + _nmCutLength, t);
                    Vector3 fv = NmForward(v), sv = Vector3.Cross(up, fv) * v.Side;
                    eye = Vector3.Lerp(v.Home + fv * 2.7f + sv * 1.8f + up * .85f, v.Home + fv * 2.35f + sv * 1.55f + up * .8f, u);
                    look = v.Home + up * (v.Top * .68f + .45f * (1f - Ease(c0, v.Hits[0], t)));
                    fov = Mathf.Lerp(54f, 50f, u);
                    if (!_reducedEffects)
                    {
                        float push = Mathf.Clamp01((t - v.Hits[0]) / .05f) * (1f - Ease(v.Hits[0] + .05f, v.Hits[0] + .3f, t));
                        eye += (look - eye).normalized * (.14f * push);
                    }
                }
                else
                {
                    // S5 ALL OF THEM: back on the first as the violet climbs into his eyes, then one eased crane up and back.
                    float headY = first.Top * .8f;
                    float back0 = 4.3f;
                    foreach (var v in _nmVictims)
                    {
                        Vector3 off = v.Home - _nmCentre;
                        back0 = Mathf.Max(back0, (Mathf.Abs(Vector3.Dot(off, right)) + 1.4f) / .62f - Vector3.Dot(off, way));
                    }
                    Vector3 wide = _nmCentre - way * back0 + right * 2.4f + up * 3.9f;
                    Vector3 lookWide = _nmCentre + up * 1.5f;
                    // (film r33: from 2.7 m at 45 degrees his head filled the lens; this is a medium, head and chest.)
                    Vector3 face = first.Home + up * (first.Top * .56f);
                    var keys = new[]
                    {
                        new NmKey(NmReturn, first.Home + f0 * 2.9f + s0 * 1.7f + up * .9f, face, 54f),
                        new NmKey(NmCrane, first.Home + f0 * 2.7f + s0 * 1.6f + up * .9f, face, 53f),
                        new NmKey(Seconds, wide, lookWide, 56f),
                    };
                    NmPath(keys, t, out eye, out look, out fov);
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
            // Each cut-to player's first hit too, and the lines close on whoever is being hit, wherever they are in the frame.
            var focus = new Vector2(.5f, .55f);
            if (!_reducedEffects)
                for (int c = 1; c <= _nmCuts; c++) lines = Mathf.Max(lines, .7f * Flash(t, _nmVictims[c].Hits[0] + .06f, .22f));
            int shot = t >= NmDive && _nmVictims.Count > 0 ? (t >= NmFlood && t < NmReturn && _nmCuts > 0
                ? Mathf.Clamp(1 + (int)((t - NmFlood) / _nmCutLength), 1, _nmCuts) : 0) : -1;
            if (shot >= 0 && camera != null)
            {
                Vector3 vp = camera.WorldToViewportPoint(NmW(_nmVictims[shot].Chest));
                if (vp.z > 0f) focus = new Vector2(Mathf.Clamp(vp.x, .2f, .8f), Mathf.Clamp(vp.y, .2f, .8f));
            }
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
            _nmInk.SetVector("_Focus", new Vector4(focus.x, focus.y, 0f, 0f));
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
