using System.Collections.Generic;
using TumbangPreso.CameraSystem;
using UnityEngine;

namespace TumbangPreso.Visual
{
    public sealed partial class HeroIntroductionScene
    {
        // =========================================================================================
        // NEMU, LET THEM IN v10: THE GATE, 7.0 s (owner 2026-10-14; `docs/reports/nemu-ultimate-2026-10-11/direction.md` section
        // 19). v9's story kept, told slower and cleaner. The owner on v9: *"its kinda hard to follow wtf is happening everything
        // feels rigid"*, *"the souls feel to cluttered, madami masyado"*, *"siguro wag na floor cracks"*, *"js show some sort of
        // otherwordly portal somewhere where the ghosts come from"*, *"the animation feels so sharp and rigid, bilis ng
        // nangyayare"*. So: five beats instead of six, each held long enough to read; one soul at a time on screen while the
        // story is about one soul; a third of the souls, spread high round the court; no cracks: a gate torn in the air over
        // the court that they all pour out of, and which stays open in play (`NemuWorld`) with the souls still circling it.
        //
        //   THE PRAYER (0.00-0.90): her whole body, hands clasped, head bowed, chanting (a dip of the head and a ring of violet
        //     across the court on each beat); violet gathering between her palms; the day draining to dusk. Her eyes snap open.
        //   THE GATE (0.90-1.85): low behind her: she lifts her hands and the air over the court tears open, a slit widening
        //     into the gate; the players stagger and look up at it.
        //   THE FIRST ONE (1.85-3.55): low in front of the first player looking up at the gate: ONE soul crawls out of it,
        //     glides down and circles over them; every head follows it. It turns and dives into him (3.25); he is thrown back,
        //     arched; its tail coils round him and sinks in; his eyes ignite (3.50).
        //   THE HUNT (3.55-5.75): beside the one nearest him: the gate pours out the rest and he runs; one hunts him down, he
        //     trips, rolls onto his back, sees it over him, and it plunges in (5.00). Behind him the other is caught running.
        //     He is hauled up like a puppet, his eyes ignite.
        //   ALL OF THEM (5.75-7.00): the lens cranes up and back: every player possessed, floating, heads tipped too far,
        //     turning to the lens, the gate open over them and the souls circling it (they go on circling in play).
        //
        // Rules that still hold: black and one violet (no white, no amber); no Kuro; after the entry nothing is drawn on a
        // player but the aura and their own eyes burning violet; medium and wide shots only; nothing chonky; 5 to 7 s. Nothing
        // runs on Update and nothing is random: every soul, fall and twitch is posed from the scene clock.
        // =========================================================================================
        /// <summary>Her eyes; the cut to the gate; the gate tearing open; the first soul out; the rest pouring out; THE HUNT.</summary>
        // v11 (owner on v10: *"give the possession shit more time to breathe di ko na kasi magets ung bandang dulo"*, then *"start
        // from this wag na prayer"*, pointing at the gate shot): no prayer; it opens on her, hands raised, as the gate tears;
        // THE HUNT has 3.1 s and the third is caught before the trip, not over it. NmSplit stays as the start of the gate shot.
        // v12 (owner on v11: *"give this ending time to breathe ang bilis and sudden ng cutoff"*, *"theres still some random shit
        // to end it and make it feel satisfying to see or eerie"*): the ending has 1.7 s, THE STRINGS (below).
        private const float NmIgnite = .05f, NmSplit = 0f, NmGateFrom = .1f, NmGateOpen = .55f, NmLead = .8f, NmBurst = 2.5f, NmDive = 2.5f;
        /// <summary>The first soul: when it turns on the first player and when it is in him; his eyes.</summary>
        private const float NmLeadOut = 1.85f, NmLeadIn = 2.25f, NmFirstWake = 2.35f;
        /// <summary>The one who falls: running, the chaser turning on him, the trip, the roll onto his back, the soul in, the pull
        /// up off the court, his eyes.</summary>
        private const float NmTripRun = 2.75f, NmTripOut = 2.8f, NmTrip = 3.5f, NmRoll = 3.85f, NmTripIn = 4.3f, NmRise = 4.7f, NmTripWake = 4.4f, NmTripSettle = 5.2f;
        /// <summary>The rest: running, the chaser, caught in the background.</summary>
        private const float NmBackRun = 2.8f, NmBackOut = 2.9f, NmBackIn = 3.35f;
        /// <summary>
        /// THE STRINGS, her ending (5.30-7.00), through her own eyes: the possessed before her across the court, bodies turned
        /// away, turn only their heads all the way round to her, too far, together, until every one stares into the lens (5.40);
        /// every one is yanked up on its strings at once and left hanging (5.75), then they come for her in three stop-motion
        /// lurches; the lights die (6.05): the frame sinks into black until only violet is left (their
        /// eyes, the gate's lip, the souls); the eyes flare once (6.30) and hold in the dark until play comes back. (Film r56:
        /// from behind her, her back filled half the frame and they were specks; and the last 0.4 s is the hand-back's
        /// dissolve, so the dark comes before it.)
        /// </summary>
        private const float NmCrane = 5.3f, NmTurnAll = 5.4f, NmTug = 5.75f, NmDark = 6.05f, NmFlare = 6.3f;
        /// <summary>The chant: her head dips and a ring goes out across the court on each beat.</summary>
        private const float NmChantBeat = .21f;
        private const float NmRunSpeed = 2.2f;
        /// <summary>How long an impact frame's window is; the frame itself is the first half of it.</summary>
        private const float NmImpact = .067f;
        /// <summary>How tall the gate is when open (metres), and how high its middle hangs over the court.</summary>
        private const float NmGateSize = 3.6f, NmGateHeight = 4.0f;   // (owner on v10: *"portal could be bigger"*)
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

        /// <summary>One soul of the storm: where it comes up and circles, and (if it is a diver) whom it goes into and when.</summary>
        private struct NmSoul
        {
            public float Born, Rise, Radius, Height, Speed, Phase;
            /// <summary>What it circles (scene space): the storm's eye, or, for one flung out over the map, its own spot there.</summary>
            public Vector3 Centre;
            public bool Far;
            /// <summary>Where it comes out of the gate (scene space).</summary>
            public Vector3 From;
            public int Victim;
            /// <summary>A diver: when it leaves the storm, when its head meets the body, and how long it takes to sink in.</summary>
            public float Out, In, Sink;
            public Vector3 Offset;
            /// <summary>The way it goes into the body (scene space).</summary>
            public Vector3 Dir;
            /// <summary>THE FIRST ONE; a chaser (runs a player down); the soul the lens rides through THE FALL.</summary>
            public bool Lead, Chase, Hero;
        }

        private sealed class NmVictim
        {
            public GameObject Holder;
            public Transform Head;
            public NmBone Root, Torso, HeadBone, ArmL, ArmR, LegL, LegR;
            public Vector3 Home, FaceLocal;
            public float Yaw, Reach, Top = 1.7f, Width = .7f, FaceDepth = .3f, FaceTop = .5f;
            /// <summary>0 the first possessed, 1 the one who falls, 2 caught in the background.</summary>
            public int Role;
            /// <summary>When each of their souls goes in (scene seconds), which soul, how hard; when the eyes open.</summary>
            public float[] Hits;
            public int[] Souls;
            public float[] Strength;
            public float Wake;
            /// <summary>When they stop jerking and settle into the possessed stance (after the eyes: the one who falls is still on
            /// the court when his eyes light, owner on v10: *"eyes should glow purple whenn it enters him"*).</summary>
            public float Settle;
            /// <summary>Which way they run (scene space, flat), when, and its heading.</summary>
            public Vector3 Flee = Vector3.forward;
            public float RunAt = 99f, RunYaw;
            /// <summary>Which side of them the lens tracks (+1 or -1).</summary>
            public float Side = 1f;
            public NemuEyeGlow Eyes;
            /// <summary>THE STRINGS: where they hang before her through her eyes (scene space), and which way their body faces
            /// (away from her, so the head has to come round too far to stare).</summary>
            public Vector3 Mark;
            public float MarkYaw;
            /// <summary>Their chest this frame (scene space), as it jerks.</summary>
            public Vector3 Chest;
            public int Seat;
        }

        private readonly List<NmVictim> _nmVictims = new List<NmVictim>(4);
        private readonly List<NemuEyeGlow> _nmEyeGlows = new List<NemuEyeGlow>(5);
        private NemuEyeGlow _nmHerEyes;
        private readonly List<NmSoul> _nmSouls = new List<NmSoul>(64);
        private readonly Vector3[] _nmTailAt = new Vector3[NmTail];
        private readonly bool[] _nmTailOn = new bool[NmTail];
        private NemuSmokeBatch _nmSmoke;
        private NemuPortal _nmGate;
        /// <summary>The gate's middle (scene space), and where THE FIRST ONE circles over the players.</summary>
        private Vector3 _nmGateAt = new Vector3(0f, NmGateHeight, 8f), _nmLeadRing = new Vector3(0f, 2.2f, 4f);
        private Vector3 _nmWay = Vector3.forward, _nmCentre = new Vector3(0f, 0f, 6f), _nmEyeOfStorm = new Vector3(0f, 0f, 4.5f);
        private Vector3 _nmEye = new Vector3(-.72f, 1.08f, 1.75f), _nmLook = new Vector3(0f, .98f, 0f);
        // Her front is the scene's +z, as the authored shots have it (film r38: her measured face pointed at her back).
        private Vector3 _nmFaceLocal = Vector3.forward, _nmEyesLocal, _nmHerChest = new Vector3(0f, .7f, 0f), _nmHerFace = Vector3.forward;
        private float _nmFaceDepth = .3f, _nmHeadTop = 1.2f, _nmStormReach = 6f;
        /// <summary>The one who falls (-1 if there are fewer than two players) and the soul that hunts him (-1 none).</summary>
        private int _nmTripper = -1, _nmChaser = -1;
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
            var all = new List<NmVictim>();
            for (int i = 0; i < _others.Count; i++)
            {
                all.Add(BuildVictim(_others[i], i));
                centre += _others[i].Home;
            }
            if (all.Count > 0)
            {
                centre /= all.Count; centre.y = 0f;
                _nmCentre = centre;
                if (centre.sqrMagnitude > .04f) _nmWay = centre.normalized;
            }
            // THE ROLES: the first possessed is the one most in the middle of the shot over her shoulder; the one who falls is
            // the nearest to him (they were together); the rest are caught in the background.
            Vector3 across = Vector3.Cross(Vector3.up, _nmWay);
            all.Sort((a, b) => Mathf.Abs(Vector3.Dot(a.Home - _nmCentre, across)).CompareTo(Mathf.Abs(Vector3.Dot(b.Home - _nmCentre, across))));
            if (all.Count > 2)
            {
                var firstHome = all[0].Home;
                var rest = all.GetRange(1, all.Count - 1);
                rest.Sort((a, b) => (a.Home - firstHome).sqrMagnitude.CompareTo((b.Home - firstHome).sqrMagnitude));
                all.RemoveRange(1, all.Count - 1); all.AddRange(rest);
            }
            _nmVictims.AddRange(all);
            for (int i = 0; i < _nmVictims.Count; i++) _nmVictims[i].Role = i == 0 ? 0 : i == 1 ? 1 : 2;
            _nmTripper = _nmVictims.Count > 1 ? 1 : -1;

            // The storm turns round a point between her and them, wide enough to take in every player and her.
            _nmEyeOfStorm = _nmCentre * .75f;
            float reach = _nmEyeOfStorm.magnitude * .8f;
            foreach (var v in _nmVictims) reach = Mathf.Max(reach, (v.Home - _nmEyeOfStorm).magnitude);
            _nmStormReach = Mathf.Clamp(reach + 1.6f, 3.5f, 9f);
            // THE GATE: over the court beyond the players, where the lens on them looks up into it (`NemuWorld` opens the same
            // gate in play, over the possessed).
            _nmGateAt = _nmCentre + _nmWay * 4.2f + Vector3.up * NmGateHeight;   // (film r44: at 2.6 m it filled the lens on him)
            _nmGate = new NemuPortal(_root.transform, _nmGateAt, NmGateSize);
            if (all.Count > 0)
            {
                Vector3 toward = all[0].Home; toward.y = 0f; toward = toward.sqrMagnitude > 1e-4f ? toward.normalized : _nmWay;
                _nmLeadRing = all[0].Home - toward * .7f + Vector3.up * 2.1f;
            }
            BuildSouls();
        }

        private static float NmHash(int i, int k) => NemuSouls.Hash(i, k);

        /// <summary>
        /// The storm: every soul's typed path. Soul 0 is THE FIRST ONE, crawling out of the gate alone and diving into the first
        /// player; the rest pour out of the gate one after another (THE HUNT) and spread out high round the court (owner on v9:
        /// *"the souls feel to cluttered, madami masyado ... ikalat mo more or bawasan mo"*: a third of v9's, wider and higher).
        /// The first player takes only THE FIRST ONE; each of the others takes the one that gets them and one more after it.
        /// </summary>
        private void BuildSouls()
        {
            // A third of the storm stays round the court; the rest are flung out over the WHOLE MAP, each to circle its own
            // spot there (owner on v10: *"expand it to whole map di lang dyan sa area na yan"*), so the court stays readable
            // and the whole map is haunted. The map is the playable bounds every map measures (`MatchInstaller`).
            int count = _reducedEffects ? 16 : 30;
            int victims = _nmVictims.Count;
            Vector3 up = Vector3.up;

            _nmSouls.Add(new NmSoul
            {
                Victim = -1, Lead = true, From = _nmGateAt, Born = NmLead, Rise = .6f,
                Height = 2.6f, Radius = 1.3f, Speed = 2.3f, Phase = 1.1f,
            });
            // THE POURING: out of the gate one after another over most of a second, each out to its own place in the storm.
            for (int i = 1; i < count; i++)
            {
                var soul = new NmSoul { Victim = -1 };
                float spin = NmHash(i, 9) * Mathf.PI * 2f;
                soul.From = _nmGateAt + new Vector3(Mathf.Sin(spin), Mathf.Cos(spin) * 1.3f, 0f) * (.35f * NmHash(i, 10));
                soul.Born = NmBurst + .8f * ((i - 1) / (float)Mathf.Max(1, count - 2)) + .05f * NmHash(i, 8);
                soul.Rise = .8f + .4f * NmHash(i, 7);
                soul.Centre = _nmEyeOfStorm;
                soul.Height = 2.5f + 2.1f * NmHash(i, 1);
                float wide = Mathf.Lerp(.6f, 1.15f, Mathf.Sqrt(NmHash(i, 2)));
                soul.Radius = _nmStormReach * wide;
                if (i % 3 != 1)
                {
                    // Out over the map: a spot spread evenly over the playable bounds (a jittered grid, so none clump).
                    soul.Far = true;
                    soul.Centre = NmMapSpot(i, count);
                    soul.Height = 3.2f + 3.5f * NmHash(i, 1);
                    soul.Radius = 1.5f + 3.5f * NmHash(i, 2);
                    soul.Rise = 1.3f + .7f * NmHash(i, 7);
                }
                soul.Speed = (3.4f + 1.6f * NmHash(i, 3)) / Mathf.Max(1f, soul.Radius);
                soul.Phase = (i - 1) / (float)Mathf.Max(1, count - 1) * Mathf.PI * 2f + .4f * NmHash(i, 4);
                _nmSouls.Add(soul);
            }
            if (victims == 0) return;

            var used = new HashSet<int> { 0 };
            var firstV = _nmVictims[0];
            for (int v = 0; v < victims; v++)
            {
                var victim = _nmVictims[v];
                if (victim.Role > 0)
                {
                    // They run from the first one (now possessed), round the court rather than off it.
                    Vector3 from = victim.Home - firstV.Home; from.y = 0f;
                    from = from.sqrMagnitude > 1e-4f ? from.normalized : Vector3.Cross(up, _nmWay);
                    Vector3 round = Vector3.Cross(up, victim.Home - _nmEyeOfStorm); round.y = 0f;
                    round = round.sqrMagnitude > 1e-4f ? round.normalized : from;
                    if (Vector3.Dot(round, from) < 0f) round = -round;
                    victim.Flee = (from * .6f + round * .6f).normalized;
                    victim.RunYaw = Mathf.Atan2(victim.Flee.x, victim.Flee.z) * Mathf.Rad2Deg;
                    victim.RunAt = victim.Role == 1 ? NmTripRun : NmBackRun + .05f * (v - 2);
                }
                int n = victim.Role == 0 ? 1 : 2;
                victim.Hits = new float[n]; victim.Souls = new int[n]; victim.Strength = new float[n];
                for (int j = 0; j < n; j++)
                {
                    float inAt, outAt, sink, k;
                    if (victim.Role == 0) { inAt = NmLeadIn; outAt = NmLeadOut; sink = .4f; k = 1.4f; }
                    else if (victim.Role == 1)
                    {
                        inAt = j == 0 ? NmTripIn : NmTripIn + .3f; outAt = j == 0 ? NmTripOut : inAt - .45f;
                        sink = j == 0 ? .4f : .25f; k = j == 0 ? 1.4f : .5f;
                    }
                    else
                    {
                        float at = NmBackIn + .25f * (v - 2);
                        inAt = j == 0 ? at : at + .35f; outAt = j == 0 ? NmBackOut + .1f * (v - 2) : inAt - .45f;
                        sink = j == 0 ? .3f : .25f; k = j == 0 ? 1f : .5f;
                    }
                    int pick;
                    if (victim.Role == 0) pick = 0;
                    else
                    {
                        Vector3 target = j == 0 ? victim.Home - victim.Flee * 2.5f + up * 3f
                                                : victim.Home + victim.Flee * 2f + up * 3.2f;
                        pick = NmPick(target, outAt, used, count);
                    }
                    used.Add(pick);
                    var soul = _nmSouls[pick];
                    soul.Victim = v; soul.Out = outAt; soul.In = inAt; soul.Sink = sink;
                    soul.Chase = j == 0 && victim.Role > 0; soul.Hero = j == 0 && victim.Role == 1;
                    soul.Offset = j == 0 ? Vector3.zero : new Vector3((NmHash(pick, 5) - .5f) * .2f, (NmHash(pick, 6) - .3f) * .2f, 0f);
                    if (victim.Role == 0)
                    {
                        // THE FIRST ONE dives at him from where it circles, into his front.
                        Vector3 chest = victim.Home + up * (victim.Top * .55f);
                        soul.Dir = (chest - NmFree(soul, outAt)).normalized;
                    }
                    else if (victim.Role == 1 && j == 0) soul.Dir = (Vector3.down * .95f + victim.Flee * .1f).normalized;   // into him, lying on his back
                    else if (j == 0) soul.Dir = (victim.Flee * .85f + Vector3.down * .25f).normalized;                       // into the back of a runner
                    else soul.Dir = Vector3.down;
                    _nmSouls[pick] = soul;
                    victim.Hits[j] = inAt; victim.Souls[j] = pick; victim.Strength[j] = k;
                    if (soul.Hero) _nmChaser = pick;
                }
                // The eyes light as the soul goes in (owner on v10); the stance comes after the jerking.
                victim.Wake = victim.Role == 0 ? NmFirstWake : victim.Role == 1 ? NmTripWake : victim.Hits[0] + .1f;
                victim.Settle = victim.Role == 0 ? NmFirstWake + .15f : victim.Role == 1 ? NmTripSettle : victim.Hits[n - 1] + .45f;
            }
            // THE STRINGS' marks: an arc 4.4 m before her, in the order they stand across her view, each body turned away from her
            // and a little aside, alternately.
            {
                var order = new List<NmVictim>(_nmVictims);
                Vector3 across = Vector3.Cross(up, _nmWay);
                order.Sort((a, b) => Vector3.Dot(a.Home, across).CompareTo(Vector3.Dot(b.Home, across)));
                for (int k = 0; k < order.Count; k++)
                {
                    float ang = (k - (order.Count - 1) * .5f) * 24f;
                    Vector3 dir = Quaternion.Euler(0f, ang, 0f) * _nmWay;
                    order[k].Mark = dir * (4.4f + .5f * (k & 1));
                    order[k].MarkYaw = Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg + ((k & 1) == 0 ? 28f : -28f);
                }
            }
            // The lens on the one who falls stands on the side that has the others behind him.
            if (_nmTripper >= 0)
            {
                var t = _nmVictims[_nmTripper];
                Vector3 behind = Vector3.zero; int n = 0;
                for (int v = 0; v < victims; v++) if (v != _nmTripper) { NmBody(_nmVictims[v], NmBackIn, out var p, out _, out _); behind += p; n++; }
                NmBody(t, NmBackIn, out var at, out _, out _);
                Vector3 sd = Vector3.Cross(up, t.Flee);
                if (n > 0 && Vector3.Dot(-sd, behind / n - at) < 0f) t.Side = -1f;
            }
        }

        /// <summary>The storm soul (not the first one, not yet a diver, preferably already risen) nearest
        /// <paramref name="target"/> at <paramref name="at"/>.</summary>
        private int NmPick(Vector3 target, float at, HashSet<int> used, int count)
        {
            int best = -1; float bestGap = float.MaxValue;
            for (int pass = 0; pass < 2 && best < 0; pass++)
                for (int i = 1; i < count; i++)
                {
                    var soul = _nmSouls[i];
                    if (used.Contains(i) || soul.Victim >= 0 || soul.Far) continue;
                    if (pass == 0 && soul.Born + soul.Rise > at - .05f) continue;
                    float gap = (NmOrbit(soul, at, out _) - target).sqrMagnitude;
                    if (gap < bestGap) { bestGap = gap; best = i; }
                }
            return Mathf.Max(best, 1);
        }

        /// <summary>Soul <paramref name="i"/>'s spot out over the map (scene space): a jittered grid over the playable bounds.</summary>
        private Vector3 NmMapSpot(int i, int count)
        {
            float minX = AIController.PlayableMinX, maxX = AIController.PlayableMaxX, minZ = AIController.PlayableMinZ, maxZ = AIController.PlayableMaxZ;
            if (maxX - minX < 4f || maxZ - minZ < 4f) { minX = -10f; maxX = 10f; minZ = -14f; maxZ = 14f; }
            int cells = Mathf.Max(1, Mathf.CeilToInt(Mathf.Sqrt(count)));
            int k = (i * 7) % (cells * cells);
            float u = (k % cells + .15f + .7f * NmHash(i, 40)) / cells, w = (k / cells + .15f + .7f * NmHash(i, 41)) / cells;
            var world = new Vector3(Mathf.Lerp(minX, maxX, u), _root.transform.position.y, Mathf.Lerp(minZ, maxZ, w));
            var local = _root.transform.InverseTransformPoint(world);
            local.y = 0f;
            return local;
        }

        /// <summary>How far the gate is open at <paramref name="t"/> (0 a slit, 1 whole): it tears, catches, then rips wide.</summary>
        private static float NmGateAmount(float t)
        {
            if (t <= NmGateFrom) return 0f;
            float u = Mathf.Clamp01((t - NmGateFrom) / (NmGateOpen - NmGateFrom));
            return Mathf.Clamp01(.12f * Ease(0f, .3f, u) + .88f * Ease(.35f, 1f, u));
        }

        private NmVictim BuildVictim(Other other, int index)
        {
            var v = new NmVictim { Holder = other.Holder, Home = other.Home, Yaw = other.Yaw, Reach = other.Reach, Seat = index };
            var round = GameServices.Round;
            if (round != null)
                foreach (var p in round.Players)
                    if (p != null && other.Holder.name == "IntroductionOther-P" + (p.PlayerSlot + 1)) { v.Seat = p.PlayerSlot; break; }
            ReadBones(other.Holder.transform, v);
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
            v.Hits = new[] { NmLeadIn }; v.Souls = new[] { 0 }; v.Strength = new[] { 1f };
            v.Wake = NmFirstWake; v.Settle = NmFirstWake + .15f;
            v.RunYaw = v.Yaw;
            return v;
        }

        private static void ReadBones(Transform holder, NmVictim v)
        {
            foreach (var t in holder.GetComponentsInChildren<Transform>(true))
            {
                var bone = new NmBone { T = t, Rot = t.localRotation, Pos = t.localPosition, Scale = t.localScale };
                switch (t.name)
                {
                    case "root": if (v.Root.T == null) v.Root = bone; break;
                    case "torso": if (v.Torso.T == null) v.Torso = bone; break;
                    case "head": if (v.HeadBone.T == null) v.HeadBone = bone; break;
                    case "arm-left": if (v.ArmL.T == null) v.ArmL = bone; break;
                    case "arm-right": if (v.ArmR.T == null) v.ArmR = bone; break;
                    case "leg-left": if (v.LegL.T == null) v.LegL = bone; break;
                    case "leg-right": if (v.LegR.T == null) v.LegR = bone; break;
                }
            }
        }

        // ------------------------------------------------------------------ the clock's shape

        private Vector3 NmW(Vector3 local) => _root.transform.TransformPoint(local);
        private Vector3 NmWDir(Vector3 local) => _root.transform.TransformDirection(local);
        private static float NmBump(float t, float at, float width) { float u = 1f - Mathf.Abs(t - at) / width; return u <= 0f ? 0f : u * u * (3f - 2f * u); }

        /// <summary>Pulled up off the court like a puppet: in three tugs, each a pull and a sag (0 lying, 1 upright); v9's four
        /// snaps read as a glitch (owner: *"sharp and rigid"*).</summary>
        private static float NmPuppetRise(float x)
        {
            float r = Mathf.Clamp01(x / .5f) * 3f;
            float whole = Mathf.Floor(r), part = r - whole;
            float tug = Ease(.2f, .85f, part) - .08f * NmBump(part, .95f, .2f);
            return Mathf.Clamp01((whole + (whole >= 3f ? 0f : tug)) / 3f);
        }

        /// <summary>
        /// Where a player's body is at <paramref name="s"/> (scene space): its feet, the turn of the whole body, and how far it is
        /// lying down. A stagger back as the court splits; running from the first one; the one who falls trips, goes down on his
        /// face, rolls onto his back, and is pulled up again stiff; the others are thrown on a little as they are caught.
        /// </summary>
        private void NmBody(NmVictim v, float s, out Vector3 pos, out Quaternion rot, out float lying)
        {
            NmBodyPath(v, s, out pos, out rot, out lying);
            // THE STRINGS is its own shot: they hang in an arc before her, turned away (film r57: scattered by the hunt, only
            // one was in her eyes).
            if (s >= NmCrane && v.Mark != Vector3.zero) { pos = v.Mark; rot = Quaternion.Euler(0f, v.MarkYaw, 0f); lying = 0f; }
            // THE STRINGS: after the tug they come for her in stop-motion lurches, each a snap and a hold, half the way in
            // (never nearer than 2.8 m: film r58, nearer their heads were cut off by the frame), hanging on their strings.
            float lurch = NmLurch(s);
            if (lurch > 0f)
            {
                Vector3 flat = new Vector3(pos.x, 0f, pos.z);
                float far = flat.magnitude;
                if (far > 2.8f) pos -= flat / far * (Mathf.Min(far - 2.8f, far * .4f) * lurch);
            }
        }

        /// <summary>THE STRINGS' lurches toward her: three snaps, each held (0 none, 1 all the way).</summary>
        private static float NmLurch(float s)
        {
            float k = (s - (NmTug + .15f)) / .16f;
            if (k <= 0f) return 0f;
            float step = Mathf.Floor(k), part = k - step;
            return step >= 3f ? 1f : Mathf.Clamp01((step + Ease(.6f, 1f, part)) / 3f);
        }

        private void NmBodyPath(NmVictim v, float s, out Vector3 pos, out Quaternion rot, out float lying)
        {
            // A stumble back from the gate as it tears open; then, for a runner, a step back before he bolts.
            Vector3 away = v.Home - _nmGateAt; away.y = 0f; away = away.sqrMagnitude > 1e-4f ? away.normalized : v.Flee;
            Vector3 at = v.Home + away * (.3f * Ease(NmGateFrom + .15f, NmGateOpen + .1f, s));
            if (v.RunAt < 90f) at -= v.Flee * (.18f * NmBump(s, v.RunAt - .05f, .2f));
            float yaw = Mathf.LerpAngle(v.Yaw, v.RunYaw, Ease(v.RunAt - .15f, v.RunAt + .2f, s));
            rot = Quaternion.Euler(0f, yaw, 0f); lying = 0f; pos = at;
            if (s <= v.RunAt) return;
            float stop = v.Role == 1 ? NmTrip : v.Hits[0];
            float x = Mathf.Min(s, stop) - v.RunAt;
            float d = NmRunSpeed * x * x / (x + .3f);
            if (v.Role == 1)
            {
                // THE FALL: a stumble, down on his face sliding, over onto his back, then hauled up by nothing.
                float fall = Ease(NmTrip + .04f, NmTrip + .3f, s);
                float stumble = NmBump(s, NmTrip + .04f, .12f);
                d += .6f * Ease(NmTrip, NmTrip + .45f, s);
                float roll = Ease(NmRoll, NmRoll + .32f, s);
                float rise = s > NmRise ? NmPuppetRise(s - NmRise) : 0f;
                Quaternion down = rot * Quaternion.Euler(90f * fall + 18f * stumble, 0f, 0f) * Quaternion.AngleAxis(180f * roll, Vector3.up);
                Quaternion stood = Quaternion.Euler(0f, v.RunYaw + 180f, 0f);
                rot = rise > 0f ? Quaternion.Slerp(down, stood, rise) : down;
                lying = fall * (1f - rise);
                pos = at + v.Flee * d + Vector3.up * (v.Width * .3f * lying + .08f * rise) + Vector3.up * (.06f * NmBump(s, NmTrip + .25f, .06f));
                return;
            }
            if (s > v.Hits[0]) d += .5f * Ease(v.Hits[0], v.Hits[0] + .35f, s);
            pos = at + v.Flee * d;
        }

        private Vector3 NmChestAt(NmVictim v, float s)
        {
            NmBody(v, s, out var p, out var r, out _);
            return p + r * Vector3.up * (v.Top * .55f);
        }

        /// <summary>Where a soul circling the storm is at <paramref name="s"/> (scene space), and which way it is going.</summary>
        private Vector3 NmOrbit(in NmSoul soul, float s, out Vector3 velocity)
        {
            float r = soul.Radius * (1f + .08f * Mathf.Sin(s * 1.1f + soul.Phase));
            float a = soul.Phase + soul.Speed * s;
            Vector3 round = new Vector3(Mathf.Sin(a), 0f, Mathf.Cos(a));
            Vector3 along = new Vector3(Mathf.Cos(a), 0f, -Mathf.Sin(a));
            float h = soul.Height + .35f * Mathf.Sin(s * 1.7f + soul.Phase * 3f);
            velocity = along * (soul.Speed * r) + Vector3.up * (.6f * Mathf.Cos(s * 1.7f + soul.Phase * 3f));
            return (soul.Lead || soul.Centre == Vector3.zero ? _nmEyeOfStorm : soul.Centre) + round * r + Vector3.up * h;
        }

        /// <summary>
        /// A soul that is not diving: out of the gate, then circling in the storm. THE FIRST ONE crawls out slowly (head first,
        /// wriggling), glides down over the players and circles them low, alone; the rest stream out of it one after another
        /// and fan out to their places high round the court.
        /// </summary>
        private Vector3 NmFree(in NmSoul soul, float s)
        {
            float r = s - soul.Born;
            if (soul.Lead)
            {
                Vector3 toward = -_nmWay;
                float crawl = Ease(0f, .5f, r);
                Vector3 side = Vector3.Cross(Vector3.up, toward);
                Vector3 outOf = soul.From + toward * (1.1f * crawl) + Vector3.down * (.3f * crawl)
                              + side * (.12f * Mathf.Sin(r * 9f) * (1f - crawl));
                float a = soul.Phase + Mathf.Max(0f, r - .55f) * soul.Speed;
                Vector3 ring = _nmLeadRing + new Vector3(Mathf.Sin(a), .18f * Mathf.Sin(a * 2f), Mathf.Cos(a)) * soul.Radius;
                return Vector3.Lerp(outOf, ring, Ease(.4f, .9f, r));
            }
            Vector3 orbit = NmOrbit(soul, s, out _);
            float rise = r / soul.Rise;
            if (rise >= 1f) return orbit;
            // Shot out of the gate toward its place, the stream bending into the storm's turn.
            Vector3 from = soul.From;
            Vector3 aim = NmOrbit(soul, soul.Born + soul.Rise, out _);
            Vector3 push = (aim - from); push.y = 0f;
            push = push.sqrMagnitude > 1e-4f ? push.normalized : -_nmWay;
            float u = rise * rise * (3f - 2f * rise);
            Vector3 path = Vector3.Lerp(from + push * (1.6f * rise), orbit, u);
            return path;
        }

        /// <summary>
        /// Where a soul is at <paramref name="s"/> (scene space). A chaser swoops down behind its player and gains on them; the
        /// one hunting the faller overshoots as he goes down, wheels round to hang over him, and plunges. The rest of the divers
        /// drop in. False before it is born and once it has sunk in.
        /// </summary>
        private bool NmSoulAt(in NmSoul soul, int index, float s, out Vector3 at)
        {
            at = default;
            if (s < soul.Born) return false;
            if (soul.Victim < 0 || s < soul.Out) { at = NmFree(soul, s); return true; }
            var victim = _nmVictims[soul.Victim];
            if (s >= soul.In)
            {
                float p = (s - soul.In) / Mathf.Max(.01f, soul.Sink);
                if (p >= 1f) return false;
                at = NmDoor(soul, victim, s) + soul.Dir * ((victim.Width * .42f + .12f) * (1f - (1f - p) * (1f - p)));
                return true;
            }
            if (soul.Chase)
            {
                Vector3 up = Vector3.up, side = Vector3.Cross(up, victim.Flee);
                var hunter = soul;
                Vector3 Behind(float time)
                {
                    float end = victim.Role == 1 ? NmTrip : hunter.In;
                    float u = Mathf.Clamp01((time - hunter.Out) / (end - hunter.Out));
                    float gap = Mathf.Lerp(2.8f, victim.Role == 1 ? 1.1f : victim.Width * .42f, Mathf.Pow(u, .75f));
                    Vector3 chase = NmChestAt(victim, time) - victim.Flee * gap + up * (.7f * (1f - u) * (1f - u))
                                  + side * (Mathf.Sin(time * 6f + index) * .3f * (1f - u));
                    return Vector3.Lerp(NmFree(hunter, time), chase, Ease(0f, .35f, u));
                }
                if (victim.Role == 1 && s > NmTrip)
                {
                    // It overshoots him as he goes down, wheels round, hangs over him as he rolls onto his back, then plunges.
                    Vector3 chest = NmChestAt(victim, s);
                    Vector3 hover = chest + up * 1.5f + victim.Flee * .15f + side * (Mathf.Sin(s * 5f) * .12f);
                    float k = Ease(NmTrip, NmTrip + .5f, s);
                    Vector3 swing = Vector3.Lerp(Behind(NmTrip), hover, k) + victim.Flee * (1.2f * Mathf.Sin(k * Mathf.PI)) + up * (.5f * Mathf.Sin(k * Mathf.PI));
                    float plunge = Ease(soul.In - .2f, soul.In, s);
                    at = Vector3.Lerp(swing, NmDoor(soul, victim, s), plunge * plunge);
                    return true;
                }
                at = Behind(s);
                return true;
            }
            // A DROP: a curve from its flight into them along its own line.
            float span = soul.In - soul.Out;
            float w = (s - soul.Out) / span;
            Vector3 from = NmFree(soul, soul.Out);
            Vector3 door = NmDoor(soul, victim, soul.In);
            Vector3 vel = (NmFree(soul, soul.Out + .02f) - from) / .02f;
            Vector3 m0 = vel * (span * (soul.Lead ? .3f : .5f)), m1 = soul.Dir * ((door - from).magnitude * 1.2f);
            if (soul.Lead) w = w * w * (3f - 2f * w);
            float w2 = w * w, w3 = w2 * w;
            at = (2 * w3 - 3 * w2 + 1) * from + (w3 - 2 * w2 + w) * m0 + (-2 * w3 + 3 * w2) * door + (w3 - w2) * m1;
            return true;
        }

        /// <summary>Where a diver meets the body at <paramref name="s"/> (scene space): back along its own line from their chest.</summary>
        private Vector3 NmDoor(in NmSoul soul, NmVictim v, float s)
        {
            Vector3 side = Vector3.Cross(Vector3.up, v.Flee);
            return NmChestAt(v, s) + side * soul.Offset.x + Vector3.up * soul.Offset.y - soul.Dir * (v.Width * .42f);
        }

        // ------------------------------------------------------------------ sample

        private void SampleNemu(float t)
        {
            _nmSmoke.Begin();
            _nmSmoke.SetView(NmW(_nmEye), NmW(_nmLook));

            float lit = Ease(NmIgnite, NmIgnite + .06f, t);
            _nmHerEyes?.Set(lit, t);
            if (_head != null && lit > .01f)
                _nmSmoke.Light(_head.TransformPoint(_nmEyesLocal) + (_head.rotation * _nmFaceLocal) * .04f, .3f, lit * (.35f + .8f * Flash(t, NmIgnite + .05f, .12f)), NemuSmokeBatch.Violet);

            DrawPrayer(t);
            DrawGate(t);
            float calling = Ease(NmIgnite, NmIgnite + .3f, t) * (1f - Ease(NmDive, NmDive + .4f, t));
            if (calling > .01f)
            {
                var state = new NemuAura.State
                {
                    Feet = NmW(Vector3.zero), Forward = _root.transform.forward, Stream = Vector3.zero, Top = _nmHerChest.y / .55f, Width = .55f,
                    Clock = t, Amount = calling, Swell = Mathf.Max(Flash(t, NmGateFrom, .4f), Flash(t, NmBurst, .4f)), Drag = 0f, Thin = 0f, Seat = 7, Reduced = _reducedEffects,
                };
                NemuAura.Draw(_nmSmoke, state);
            }

            for (int i = 0; i < _nmVictims.Count; i++)
            {
                var v = _nmVictims[i];
                NmAct(v, i, t);
                v.Chest = v.Torso.T != null ? _root.transform.InverseTransformPoint(v.Torso.T.position) + v.Holder.transform.localRotation * Vector3.up * (v.Top * .12f) : NmChestAt(v, t);
            }

            Vector3 eye = NmW(_nmEye);
            for (int i = 0; i < _nmSouls.Count; i++) DrawSoul(_nmSouls[i], i, t, eye);
            for (int i = 0; i < _nmVictims.Count; i++) SampleVictim(_nmVictims[i], i, t);
            _nmSmoke.End(t);
            // The gate stays open to the end (play keeps it open, `NemuWorld`).
            _nmGate?.Set(NmGateAmount(t), 1f, Mathf.Max(Flash(t, NmGateOpen, .35f), .8f * NmBump(t, NmBurst + .4f, .6f), .6f * Flash(t, NmLead + .1f, .3f)), t);
        }

        /// <summary>
        /// THE PRAYER: violet gathering between her clasped palms, swelling on each beat of the chant, a ring going out across
        /// the court with each beat; at THE SPLIT it drops out of her palms into the court under her feet.
        /// </summary>
        private void DrawPrayer(float t)
        {
            float on = NmSplit <= 0f ? 0f : Ease(.05f, .4f, t) * (1f - Ease(NmSplit, NmSplit + .15f, t));
            Vector3 palms = _nmHerChest + _nmHerFace * .3f + Vector3.up * .05f;
            float beat = Mathf.Repeat(t, NmChantBeat) / NmChantBeat;
            if (on > .01f)
            {
                float grow = Ease(0f, NmSplit, t);
                _nmSmoke.Light(NmW(palms), .18f + .3f * grow + .08f * (1f - beat), on * (.5f + .9f * grow) * (.7f + .3f * (1f - beat)), NemuSmokeBatch.Violet);
                for (int j = 0; j < 3; j++)
                {
                    float q = Mathf.Repeat(t * 1.3f + j / 3f, 1f);
                    Vector3 p = palms + Vector3.up * (.1f + .5f * q) + new Vector3(Mathf.Sin(t * 3f + j * 2f), 0f, Mathf.Cos(t * 2.4f + j)) * (.06f * q);
                    _nmSmoke.Wisp(NmW(p), Vector3.up, .05f, .22f, on * .6f * (1f - q), j * .3f, q * .6f, 1f);
                }
                if (!_reducedEffects)
                {
                    float r = .4f + 2.6f * beat;
                    const int ring = 24;
                    for (int j = 0; j < ring; j++)
                    {
                        float a = j * Mathf.PI * 2f / ring;
                        _nmSmoke.Light(NmW(new Vector3(Mathf.Cos(a) * r, .12f, Mathf.Sin(a) * r)), .2f, on * .55f * (1f - beat) * grow, NemuSmokeBatch.Violet, default, 1f);
                    }
                }
            }
            // At THE GATE the light leaves her palms and climbs into the sky where the gate tears open.
            float climb = Ease(NmSplit - .02f, NmGateFrom + .1f, t);
            float rising = Ease(NmSplit - .06f, NmSplit, t) * (1f - Ease(NmGateFrom + .08f, NmGateFrom + .2f, t));
            if (rising > .01f)
                for (int j = 0; j < 4; j++)
                {
                    float u = Mathf.Clamp01(climb - j * .07f);
                    Vector3 p = Vector3.Lerp(palms + Vector3.up * .3f, _nmGateAt, u * u) + Vector3.up * (Mathf.Sin(u * Mathf.PI) * 1.2f);
                    _nmSmoke.Light(NmW(p), .3f - .05f * j, rising * (1.4f - .3f * j), NemuSmokeBatch.Violet);
                }
        }

        /// <summary>
        /// Round the gate (its body is `NemuPortal`): a hot line of light where the air first splits, black smoke dragged in
        /// round its lip and a glow under it on the court; as the souls pour out, smoke blown out with them.
        /// </summary>
        private void DrawGate(float t)
        {
            float open = NmGateAmount(t);
            if (open <= .001f) return;
            Vector3 at = _nmGateAt;
            float tall = NmGateSize * .5f * (.3f + .7f * Mathf.Sqrt(open)), wide = NmGateSize * .5f * (.05f + .65f * open);
            // The split: a seam of violet along it while it is still a slit.
            float seam = Flash(t, NmGateFrom + .05f, .3f) * (1f - open * .6f);
            if (seam > .01f) _nmSmoke.Light(NmW(at), .5f, 1.8f * seam, NemuSmokeBatch.Violet, NmWDir(Vector3.up * tall * 1.8f));
            _nmSmoke.Light(NmW(at), NmGateSize * .9f, .35f * open, NemuSmokeBatch.Violet);
            if (_reducedEffects) return;
            // Smoke sucked into its lip, round and round.
            for (int j = 0; j < 14; j++)
            {
                float q = Mathf.Repeat(t * .8f + j / 14f, 1f);
                float ang = j * 2.4f + t * 1.3f;
                Vector3 rim = new Vector3(Mathf.Cos(ang) * wide, Mathf.Sin(ang) * tall, 0f) * (1.5f - .5f * q);
                Vector3 p = at + Quaternion.LookRotation(-_nmWay) * rim;
                Vector3 inward = at - p;
                _nmSmoke.Wisp(NmW(p), NmWDir(inward), .14f, .7f, .55f * open * Ease(0f, .2f, q) * (1f - q), j * .17f, q * .6f, .9f);
            }
            // On the court under it, its light.
            _nmSmoke.Light(NmW(new Vector3(at.x, .12f, at.z)), 2.2f, .6f * open, NemuSmokeBatch.Violet, default, 1.5f);
        }

        /// <summary>
        /// One wailing soul: a black head lit violet from inside with a face on its front (`NemuSouls.Face`), and a long black
        /// tail drawn along its own path. As it comes out of the gate the gate flares round it.
        /// </summary>
        private void DrawSoul(in NmSoul soul, int index, float t, Vector3 eye)
        {
            if (t > soul.Born - .2f && t < soul.Born + .35f)
            {
                float flare = NmBump(t, soul.Born, soul.Lead ? .35f : .2f);
                _nmSmoke.Light(NmW(soul.From), soul.Lead ? 1.0f : .5f, (soul.Lead ? 1.4f : .6f) * flare, NemuSmokeBatch.Violet);
            }
            if (t < soul.Born) return;
            // The souls go on circling to the end and on into play (owner on v9: *"continue showing for the whole 15 seconds"*).
            float fade = Ease(soul.Born, soul.Born + .2f, t);
            // THE FIRST ONE comes out of the gate's dark, not sitting in the middle of it (owner on v10: *"this looks really
            // awkward ... the ghost here"*): it fades up as it crosses the lip.
            if (soul.Lead) fade = Ease(soul.Born + .08f, soul.Born + .4f, t);
            // The storm is the background: dimmer than the soul the story is about (owner on v9: *"too cluttered"*).
            if (!soul.Lead && !soul.Chase) fade *= .75f;
            // While the lens is on the hunt, the rest of the storm steps back so the hunter is the one you follow.
            if (_nmChaser >= 0 && !soul.Hero && !soul.Chase)
                fade *= 1f - .65f * Ease(NmTripRun, NmTripRun + .3f, t) * (1f - Ease(NmTripIn + .2f, NmTripIn + .6f, t));
            if (fade <= .01f) return;
            for (int k = 0; k < NmTail; k++) _nmTailOn[k] = NmSoulAt(soul, index, t - k * NmTailStep, out _nmTailAt[k]);
            // Thinned as it nears the lens (film r39: souls brushing past it were violet walls).
            if (_nmTailOn[0]) fade *= Mathf.Clamp01((Vector3.Distance(NmW(_nmTailAt[0]), eye) - 1.2f) / 1.4f);
            if (fade <= .01f) return;

            float headSize = soul.Lead ? .62f : soul.Hero ? .52f : soul.Chase ? .46f : .32f + .1f * NmHash(index, 21);   // (film r38: the first one was a speck)
            bool entering = soul.Victim >= 0 && t > soul.In - .35f;
            Vector3 door = entering ? NmDoor(soul, _nmVictims[soul.Victim], t) : default;

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

            if (!_nmTailOn[0]) return;
            Vector3 head = NmW(_nmTailAt[0]);
            float dist = Vector3.Distance(head, eye);
            if (dist < 1.1f) return;   // (film r38: a soul brushing the lens was a violet wall)
            Vector3 heading = _nmTailOn[1] ? head - NmW(_nmTailAt[1]) : Vector3.zero;
            if (heading.sqrMagnitude < 1e-6f && NmSoulAt(soul, index, t + .02f, out var ahead)) heading = NmW(ahead) - head;
            // Hanging over the fallen one, it looks down at him.
            if (soul.Hero && t > NmRoll && t < soul.In) heading = Vector3.Lerp(heading.normalized, Vector3.down, .7f);
            heading = heading.sqrMagnitude > 1e-6f ? heading.normalized : (eye - head) / dist;
            float sink = soul.Victim >= 0 && t >= soul.In ? Mathf.Clamp01((t - soul.In) / Mathf.Max(.01f, soul.Sink)) : -1f;
            float squash = sink >= 0f ? 1f - .45f * Mathf.Sqrt(sink) : 1f;
            Vector3 axis = sink >= 0f ? NmWDir(soul.Dir) : heading;
            _nmSmoke.Wisp(head, axis, headSize * (sink >= 0f ? 1f + .5f * sink : 1f), headSize * (sink >= 0f ? squash : 1.18f), fade * .95f, index * .113f, 0f, 1f);
            float glow = soul.Lead ? Mathf.Lerp(.55f, 1.1f, Ease(soul.Born + .3f, soul.Born + .8f, t)) : soul.Hero ? 1.15f : .95f;
            _nmSmoke.Light(head, headSize * (soul.Hero || soul.Lead ? 1.1f : 1.6f) * (sink >= 0f ? 1f - .3f * sink : 1f), glow * fade, NemuSmokeBatch.Violet);
            NemuSouls.Face(_nmSmoke, index, t, head, heading, headSize, squash, fade, eye, dist, soul.Lead || soul.Hero);
        }

        /// <summary>
        /// What shows on a player as the souls go in: a thread linking each soul to them; at the hit a ring bursting round the
        /// spot (and, the hardest, round them on the court), a flash, smoke punched out; then its tail COILS ROUND THEM,
        /// tightening and sinking in (owner on v8: the black hole on the chest *"is weird af"*: nothing round sits on them); the
        /// aura stepping up with every soul; the violet left in their chest climbing into their eyes.
        /// </summary>
        private void SampleVictim(NmVictim v, int index, float t)
        {
            Vector3 up = Vector3.up;
            NmBody(v, t, out var pos, out var rot, out float lying);
            Vector3 bodyUp = rot * Vector3.up;
            Vector3 middle = pos + bodyUp * (v.Top * .45f);
            Vector3 ground = new Vector3(middle.x, pos.y - v.Width * .3f * lying, middle.z);
            Vector3 chest = v.Chest;
            int n = v.Hits.Length;
            float tailSeconds = NmTail * NmTailStep;

            for (int h = 0; h < n; h++)
            {
                var soul = _nmSouls[v.Souls[h]];
                float k = v.Strength[h];
                Vector3 dir = soul.Dir;
                Vector3 door = NmDoor(soul, v, t);
                Vector3 a = Vector3.Cross(dir, up); a = a.sqrMagnitude > 1e-4f ? a.normalized : Vector3.Cross(up, v.Flee).normalized;
                Vector3 b = Vector3.Cross(dir, a);


                float since = t - v.Hits[h];
                if (since < 0f) continue;

                if (since < .32f)
                {
                    float e = 1f - Mathf.Pow(1f - since / .32f, 3f), gone = 1f - since / .32f;
                    // (owner on v11, the entry: *"possession vfx wtf"*: rings of light dots read as pink balls, the chest flash a
                    // blob.) One shockwave of smoke round them on the court, torn strands blown outward, and a short hot flash.
                    if (k >= 1f)
                    {
                        const int strands = 16;
                        float rr = .35f + 1.6f * e;
                        for (int j = 0; j < strands; j++)
                        {
                            float ang = j * Mathf.PI * 2f / strands + h;
                            Vector3 round = new Vector3(Mathf.Cos(ang), 0f, Mathf.Sin(ang));
                            Vector3 tangent = new Vector3(-round.z, 0f, round.x);
                            Vector3 p = ground + round * rr + up * (.08f + .1f * e);
                            _nmSmoke.Wisp(NmW(p), NmWDir(tangent + round * .3f), .09f, rr * .55f, .75f * gone, j * .09f + h * .3f, e * .6f, 1f);
                        }
                        _nmSmoke.Light(NmW(ground + up * .1f), .6f + 1.4f * e, .7f * gone, NemuSmokeBatch.Violet, default, 1.5f);
                    }
                    _nmSmoke.Light(NmW(chest), v.Width * .7f, .8f * Mathf.Min(1f, k) * Flash(t, v.Hits[h] + .02f, .08f), NemuSmokeBatch.Violet);
                    float burst = since / .32f;
                    for (int j = 0; j < 7; j++)
                    {
                        float ang = (j * 51f + h * 23f + index * 17f) * Mathf.Deg2Rad;
                        Vector3 outDir = (a * Mathf.Cos(ang) + bodyUp * (.4f + .6f * Mathf.Abs(Mathf.Sin(ang))) + dir * .9f).normalized;
                        float reach = .25f + 1.0f * burst;
                        _nmSmoke.Wisp(NmW(chest + outDir * reach * .6f), NmWDir(outDir), .07f * Mathf.Min(1f, k), reach * .8f,
                                      Mathf.Min(1f, k) * .8f * (1f - burst), j * .07f + h * .3f, burst * .7f, 1f);
                    }
                }

                // THE COIL round the body's own length, standing or lying.
                float coil = soul.Sink + tailSeconds;
                if (since < coil)
                {
                    float p = since / coil;
                    Vector3 sd = Vector3.Cross(bodyUp, v.Flee); if (sd.sqrMagnitude < 1e-4f) sd = Vector3.Cross(bodyUp, Vector3.right);
                    sd.Normalize();
                    Vector3 fl = Vector3.Cross(sd, bodyUp);
                    int strands = k >= 1f ? 12 : 7;
                    for (int j = 0; j < strands; j++)
                    {
                        float u = j / (float)(strands - 1);
                        float ang = u * Mathf.PI * 3.2f + since * 14f + h;
                        float rad = v.Width * (.8f - .5f * p) * (1f - .25f * u);
                        float hgt = v.Top * (.2f + .5f * u) * (1f - .3f * p);
                        Vector3 round = sd * Mathf.Cos(ang) + fl * Mathf.Sin(ang), spin = -sd * Mathf.Sin(ang) + fl * Mathf.Cos(ang);
                        Vector3 point = pos + bodyUp * hgt + round * rad;
                        _nmSmoke.Wisp(NmW(point), NmWDir(spin - round * .4f + bodyUp * .25f), .07f * (1f - .5f * p), .45f * (1f - .4f * p),
                                      .85f * (1f - p) * Ease(0f, .05f, since), j * .13f + h * .3f, p * .6f, .9f);
                    }
                }
            }

            float auraOn = 0f, swell = Flash(t, v.Wake, .3f);
            for (int h = 0; h < n; h++)
            {
                auraOn += Ease(v.Hits[h] - .02f, v.Hits[h] + .1f, t) * (h == 0 ? .4f : .6f / Mathf.Max(1, n - 1));
                swell = Mathf.Max(swell, Mathf.Min(1f, v.Strength[h]) * Flash(t, v.Hits[h] + .04f, .22f));
            }
            auraOn = Mathf.Min(1f, auraOn);
            if (auraOn > 0f)
            {
                // Lying down, the aura burns up off the length of the body on the court.
                var state = new NemuAura.State
                {
                    Feet = NmW(Vector3.Lerp(pos, ground, lying)), Forward = v.Holder.transform.forward, Stream = Vector3.zero,
                    Top = Mathf.Lerp(v.Top, v.Width * .9f, lying), Width = Mathf.Lerp(v.Width, v.Top * .9f, lying), Clock = t,
                    Amount = auraOn, Swell = swell, Drag = 0f, Thin = 0f, Seat = v.Seat, Reduced = _reducedEffects,
                };
                NemuAura.Draw(_nmSmoke, state);
            }

            Vector3 face = default;
            if (v.Head != null)
            {
                Vector3 look = v.Head.rotation * v.FaceLocal;
                face = v.Head.position + v.Head.up * (v.FaceTop * .28f) + look.normalized * (v.FaceDepth + .03f);
            }
            if (t > v.Hits[0] && t < v.Wake + .05f)
            {
                Vector3 front = NmW(chest);
                float leak = auraOn * (.35f + .15f * Mathf.Sin(t * 13f + index)) * (1f - Ease(v.Wake - .3f, v.Wake - .1f, t));
                _nmSmoke.Light(front, v.Width * .6f, leak, NemuSmokeBatch.Violet, default, .3f);
                float climbFrom = Mathf.Max(v.Hits[0] + .02f, v.Wake - .3f);
                float climb = Ease(climbFrom, v.Wake, t);
                if (climb > 0f && climb < 1f && v.Head != null)
                    for (int j = 0; j < 5; j++)
                    {
                        float u = Mathf.Clamp01(climb - j * .06f);
                        _nmSmoke.Light(Vector3.Lerp(front, face, u), .16f - .02f * j, .9f * (1f - j * .18f), NemuSmokeBatch.Violet, default, .3f);
                    }
            }
            float eyes = Ease(v.Wake, v.Wake + .06f, t) * (1f + .8f * Flash(t, NmFlare, .25f));
            v.Eyes?.Set(eyes, t + v.Seat);
            if (eyes > 0f && v.Head != null)
                _nmSmoke.Light(face, v.FaceTop * .9f, eyes * (.3f + .9f * Flash(t, v.Wake + .05f, .15f)), NemuSmokeBatch.Violet);
        }

        // ------------------------------------------------------------------ their acting

        /// <summary>
        /// A player who does not know, then does. Their own bones, turned in the world from the pose they had when the cutscene
        /// began: staggered as the court splits; following THE FIRST ONE with their heads as it circles; staring in horror at
        /// the first possessed; flinching as the rest erupt; running for it, looking back over the shoulder; the one who falls
        /// flailing as he goes down and scrabbling on his back; thrown and arched by each soul; then WRONG (owner: *"they move
        /// unnatural as fuh"*): a puppet with cut strings, snapping between broken poses a few frames at a time; then the
        /// possessed stance, floating, head tipped too far, and the turn to the lens, head first.
        /// </summary>
        private void NmAct(NmVictim v, int index, float t)
        {
            v.Root.Reset(); v.Torso.Reset(); v.HeadBone.Reset(); v.ArmL.Reset(); v.ArmR.Reset(); v.LegL.Reset(); v.LegR.Reset();
            float seat = v.Seat * 1.7f;
            float first = v.Hits[0];
            NmBody(v, t, out var pos, out var rot, out float lying);
            float standing = 1f - lying;

            // What they look at: up at the gate as it tears open; THE FIRST ONE as it comes out and circles; then (the others)
            // the first possessed; (him) the others. The head leads and the chest follows a beat behind it, so a turn travels
            // down the body instead of the whole body swivelling at once (owner on v9: *"rigid"*).
            void Watch(float at, out float yaw, out float pitch, out float weight)
            {
                yaw = 0f; pitch = 0f; weight = 0f;
                Vector3? target = null;
                float gate = Ease(NmGateFrom + .1f, NmGateFrom + .45f, at) * (1f - Ease(NmLead + .1f, NmLead + .45f, at));
                float follow = Ease(NmLead + .1f, NmLead + .45f, at) * (1f - Ease(v.Role == 0 ? NmLeadIn - .05f : NmLeadIn, NmLeadIn + .15f, at));
                if (gate > 0f) { target = _nmGateAt; weight = gate; }
                if (follow > 0f && NmSoulAt(_nmSouls[0], 0, at, out var lead)) { target = lead; weight = Mathf.Max(weight, follow); }
                else if (v.Role > 0 && at > NmLeadIn && at < v.RunAt + .1f) { target = NmChestAt(_nmVictims[0], at) + Vector3.up * .3f; weight = Ease(NmLeadIn, NmLeadIn + .2f, at) * (1f - Ease(v.RunAt - .1f, v.RunAt + .1f, at)); }
                else if (v.Role == 0 && _nmVictims.Count > 1 && at > v.Settle && at < NmDive + .4f) { target = NmChestAt(_nmVictims[1], at) + Vector3.up * .3f; weight = Ease(v.Settle, v.Settle + .2f, at); }
                if (!target.HasValue) return;
                NmBody(v, at, out var p, out var r, out _);
                Vector3 to = target.Value - (p + Vector3.up * v.Top * .8f);
                Vector3 flat = new Vector3(to.x, 0f, to.z);
                Vector3 facing = r * Vector3.forward; facing.y = 0f;
                yaw = Mathf.Clamp(Vector3.SignedAngle(facing, flat, Vector3.up), -150f, 150f);
                pitch = -Mathf.Clamp(Mathf.Atan2(to.y, Mathf.Max(.2f, flat.magnitude)) * Mathf.Rad2Deg, -20f, 55f);
            }
            Watch(t, out float watchYaw, out float watchPitch, out float watch);
            Watch(t - .12f, out float chestYaw, out _, out float chestWatch);

            // The turn to the lens at the end.
            Vector3 fwdBase = NmWDir(rot * Vector3.forward); fwdBase.y = 0f;
            Vector3 toLens = NmW(_nmEye) - NmW(pos); toLens.y = 0f;
            float turn = toLens.sqrMagnitude > 1e-4f && fwdBase.sqrMagnitude > 1e-4f ? Vector3.SignedAngle(fwdBase, toLens, Vector3.up) : 0f;
            // THE STRINGS: all of them together, slowly, the head first (no offset between them: the unison is the point).
            float turnAt = Mathf.Max(NmTurnAll, v.Settle + .1f);
            float headTurn = Ease(turnAt, turnAt + .5f, t), chestTurn = Ease(turnAt + .2f, turnAt + .7f, t), bodyTurn = Ease(turnAt + .35f, turnAt + .85f, t);
            // The tug: yanked up on their strings at once, and left hanging.
            float jolt = t < NmTug ? 0f : Mathf.Clamp01((t - NmTug) / .07f) * (1f - .25f * Ease(NmTug + .1f, NmTug + .5f, t));
            // THE STRINGS (owner on v11: *"show the possession more or explore how fked up or scary it is"*): the body stays turned
            // away and the head alone comes all the way round to stare at her, past where a neck can go.
            float bodyYaw = (turn * .1f * bodyTurn + chestYaw * .4f * chestWatch) * standing;
            v.Holder.transform.localPosition = pos;
            v.Holder.transform.localRotation = Quaternion.Euler(0f, bodyYaw, 0f) * rot;
            Vector3 up = v.Holder.transform.up, fwd = v.Holder.transform.forward, right = v.Holder.transform.right;

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
                shove += d * (.12f * k * j);
                if (k > 1f) lift += .09f * j;
            }
            jerk = Mathf.Min(jerk, 1.5f);

            // Before the soul they are people (owner on v11: *"they move so fast ... it feels unhuman masyado kahit di pa
            // possessed"*): every scared move is slower and smaller; only the possessed move wrong.
            float stagger = NmBump(t, NmGateFrom + .4f, .5f);
            float flinch = NmBump(t, NmBurst + .15f, .3f);
            float scared = Ease(NmGateFrom + .1f, NmGateFrom + .5f, t) * (1f - Ease(v.RunAt - .1f, v.RunAt + .1f, t)) * (1f - watch) * (v.Role == 0 ? 1f - Ease(first - .1f, first, t) : 1f);
            float running = Ease(v.RunAt - .05f, v.RunAt + .32f, t) * (1f - Ease(v.Role == 1 ? NmTrip : first - .02f, v.Role == 1 ? NmTrip + .1f : first + .05f, t));
            float down = v.Role == 1 ? Ease(NmTrip, NmTrip + .15f, t) * (1f - Ease(first - .02f, first + .05f, t)) : 0f;   // flailing on the court
            float wrong = Ease(first + .3f, first + .5f, t) * (1f - Ease(v.Settle + .4f, v.Settle + .8f, t)) * (v.Role == 1 ? 1f - .6f * NmBump(t, NmRise + .2f, .25f) : 1f);
            float stance = Ease(v.Settle + .25f, v.Settle + .8f, t);

            // Darting looks, but a look takes a quarter of a second to get there (v9's tenth of a second read as a twitch).
            int glance = Mathf.FloorToInt((t + seat) / 1.1f);
            float gu = Ease(0f, .5f, Mathf.Repeat(t + seat, 1.1f));
            float glanceYaw = Mathf.Lerp((NmHash(glance - 1, v.Seat + 40) - .5f) * 70f, (NmHash(glance, v.Seat + 40) - .5f) * 70f, gu);
            float glancePitch = Mathf.Lerp(-6f - 12f * NmHash(glance - 1, v.Seat + 41), -6f - 12f * NmHash(glance, v.Seat + 41), gu);
            float lost = (NmHash(glance, v.Seat + 42) - .5f) * 26f;
            // Breathing hard, shoulders heaving, rather than a buzz.
            float tremble = Mathf.Sin(t * 6f + seat) * 1.8f * scared;

            float stride = (t - v.RunAt) * 10.5f;
            float swing = Mathf.Sin(stride) * running;
            float bob = Mathf.Abs(Mathf.Cos(stride)) * .07f * running;
            float lookBack = running * Mathf.Max(NmBump(t, v.RunAt + .4f, .25f), NmBump(t, (v.Role == 1 ? NmTrip : first) - .3f, .2f));
            float backSide = (v.Seat & 1) == 0 ? 1f : -1f;
            float scrabble = Mathf.Sin(t * 13f + seat) * down;

            // WRONG: still a puppet on cut strings, but each broken pose is reached in a jolt and held, a few a second (v9's
            // ten a second strobed: *"sharp"*).
            float step = .17f + .07f * NmHash(v.Seat, 50);
            float clock = t + seat * .13f;
            int key = Mathf.FloorToInt(clock / step);
            float snap = Ease(.45f, .9f, Mathf.Repeat(clock, step) / step);
            float Broken(int slot, float range) => Mathf.Lerp(NmHash(key * 7 + v.Seat, slot) - .5f, NmHash((key + 1) * 7 + v.Seat, slot) - .5f, snap) * 2f * range * wrong;

            float tip = ((v.Seat & 1) == 0 ? 1f : -1f) * 30f * stance;
            float sway = Mathf.Sin(t * 1.4f + seat) * 3f * stance;

            if (v.Root.T != null)
                v.Root.T.position += NmWDir(shove) * standing + Vector3.up * ((lift + .28f * jolt + .1f * stagger + .05f * flinch + bob + (.08f + .06f * NmHash(key, v.Seat + 51)) * wrong * standing + .07f * stance + Mathf.Sin(t * 2f + seat) * .015f * stance))
                                   + right * (Mathf.Sin(t * 13f) * .03f * stagger);

            float torsoTwist = scared * glanceYaw * .2f + chestYaw * .25f * chestWatch - swing * 8f + scrabble * 10f + Broken(1, 35f) + (turn * .25f * chestTurn - turn * .1f * bodyTurn) * standing;
            float torsoPitch = scared * 10f + running * 14f + flinch * 12f - jerk * 22f + Broken(2, 26f) - stance * 4f - down * 12f;
            float torsoRoll = tremble + Broken(3, 22f) + sway + stagger * 6f * Mathf.Sin(t * 12f) + Mathf.Sin(stride) * 7f * running;
            if (v.Torso.T != null)
            {
                Turn(v.Torso.T, up, torsoTwist);
                Turn(v.Torso.T, right, torsoPitch);
                Turn(v.Torso.T, fwd, torsoRoll);
            }

            float headYaw = scared * glanceYaw * .8f + (watchYaw * .6f - chestYaw * .1f) * watch + lookBack * 100f * backSide + scrabble * 25f + Broken(4, 80f)
                          + (turn * headTurn - turn * .25f * chestTurn) * standing;
            float headPitch = scared * glancePitch + watchPitch * watch - Mathf.Min(jerk, 1.2f) * 26f + Broken(5, 35f) + stance * 5f + stagger * 10f - down * 20f - jolt * 14f;
            float headRoll = scared * lost * .5f + Broken(6, 55f) + tip + ((v.Seat & 1) == 0 ? 1f : -1f) * 22f * headTurn;
            if (v.HeadBone.T != null)
            {
                Turn(v.HeadBone.T, up, headYaw);
                Turn(v.HeadBone.T, v.HeadBone.T.parent != null ? Vector3.Cross(up, v.HeadBone.T.parent.forward).normalized : right, headPitch);
                Turn(v.HeadBone.T, fwd, headRoll);
            }

            float guard = scared * 60f + flinch * 40f + down * 70f - jolt * 35f;
            float fling = jerk * 70f;
            float hang = stance * 20f;
            if (v.ArmL.T != null) { Turn(v.ArmL.T, right, -guard + swing * 55f - jerk * 30f + scrabble * 40f + Broken(7, 110f) - stance * 10f); Turn(v.ArmL.T, fwd, fling + Broken(8, 60f) + hang); }
            if (v.ArmR.T != null) { Turn(v.ArmR.T, right, -guard * .9f - swing * 55f - jerk * 30f - scrabble * 40f + Broken(9, 110f) - stance * 10f); Turn(v.ArmR.T, fwd, -fling - Broken(10, 60f) - hang); }
            float dangle = (wrong + stance) * 10f;
            if (v.LegL.T != null) Turn(v.LegL.T, right, swing * 42f + scrabble * 30f + Broken(11, 28f) + dangle);
            if (v.LegR.T != null) Turn(v.LegR.T, right, -swing * 42f - scrabble * 30f + Broken(12, 28f) + dangle);
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

        /// <summary>Which player the frame is about (-1 none): the first possessed through THE FIRST ONE, then the one who falls.</summary>
        private int NmShotVictim(float t)
        {
            if (_nmVictims.Count == 0 || t < NmLead) return -1;
            if (t < NmDive) return 0;
            if (t >= NmCrane) return -1;
            return _nmTripper >= 0 ? _nmTripper : 0;
        }

        /// <summary>THE FALL's lens on the one who falls, from running beside him to his rise.</summary>
        private void NmFallFrame(NmVictim v, float t, out Vector3 eye, out Vector3 look, out float fov)
        {
            Vector3 up = Vector3.up;
            NmBody(v, t, out var p, out _, out float lying);
            Vector3 fl = v.Flee, sd = Vector3.Cross(up, fl) * v.Side;
            Vector3 chest = NmChestAt(v, t);
            Vector3 mid = new Vector3(chest.x, 0f, chest.z);
            float fallen = Ease(NmTrip, NmTrip + .5f, t);
            float hover = Ease(NmRoll, v.Hits[0], t) * (1f - Ease(v.Hits[0], v.Hits[0] + .3f, t));
            // (film r38: the hunter rode the frame's edge: the lens looks further back, at the room behind him.)
            eye = Vector3.Lerp(p + sd * 3.3f + fl * .4f + up * 1.0f, mid + sd * 3.3f + fl * .2f + up * 1.5f, fallen);   // (film r44: lying, he was cut off at the edge)
            look = Vector3.Lerp(p - fl * 1.3f + up * (v.Top * .65f), chest + up * (.3f + .9f * hover), fallen);
            fov = Mathf.Lerp(Mathf.Lerp(64f, 58f, Ease(NmDive, NmTripRun + .3f, t)), 58f + 4f * hover, fallen);
            // His rise: round in front of his face as he is hauled up.
            float rise = Ease(NmRise - .05f, NmRise + .6f, t);
            if (rise > 0f)
            {
                // (film r48: at 2.5 m the back of his head filled the lens as he came up: further out, higher, wider.)
                Vector3 front = p - fl * 3.4f + sd * 1.9f + up * 1.25f;
                eye = Vector3.Lerp(eye, front, rise);
                look = Vector3.Lerp(look, p + up * (v.Top * .55f), rise);
                fov = Mathf.Lerp(fov, 56f, rise);
            }
            if (!_reducedEffects)
            {
                float shove = Mathf.Clamp01((t - v.Hits[0]) / .05f) * (1f - Ease(v.Hits[0] + .2f, v.Hits[0] + .45f, t));
                eye += (look - eye).normalized * (.22f * shove);
            }
        }

        /// <summary>
        /// Computed from her face and from where the players really are. THE PRAYER her whole body, a slow push; THE SPLIT high
        /// behind her; THE FIRST ONE low over her shoulder, the soul circling and diving into the first; SUDDENLY MANY craning
        /// up into the storm; THE FALL beside the one who falls (the others behind him); ALL OF THEM the crane up and back.
        /// </summary>
        private void NemuFrame(int index, float t, ref Vector3 eye, ref Vector3 look, ref float fov)
        {
            Vector3 up = Vector3.up;
            Vector3 face = _nmHerFace, across = Vector3.Cross(up, face);
            if (t < NmSplit)
            {
                // THE PRAYER: low, looking up under her bowed head so her face is in it (film r39: from chest height a bowed
                // chibi head is all hair).
                float u = Ease(0f, NmSplit, t);
                eye = face * Mathf.Lerp(2.4f, 2.0f, u) + across * .5f + up * .42f;
                look = up * Mathf.Lerp(.72f, .76f, u);
                fov = Mathf.Lerp(50f, 46f, u);
            }
            else if (_nmVictims.Count == 0)
            {
                float u = Ease(NmSplit, Seconds, t);
                eye = -face * 3f + across * 1.4f + up * Mathf.Lerp(3.4f, 4.4f, u);
                look = face * 3f + up * Mathf.Lerp(.2f, 1.8f, u);
                fov = 60f;
            }
            else
            {
                Vector3 way = _nmWay, right = Vector3.Cross(up, way);
                var first = _nmVictims[0];
                if (t < NmLead)
                {
                    // THE GATE: low behind her, looking up past her at the sky over the court as the air tears open, easing
                    // back and tipping up with it.
                    float u = Ease(NmSplit, NmLead, t);
                    float tear = Ease(NmGateFrom, NmGateOpen + .2f, t);
                    // (film r44: from straight behind her, her back was half the frame: she stands on the left third now.)
                    eye = -way * Mathf.Lerp(2.3f, 2.8f, u) + right * 1.5f + up * Mathf.Lerp(.9f, .8f, u);
                    look = Vector3.Lerp(way * 3.2f + up * 1.4f, Vector3.Lerp(_nmCentre + up * 1.0f, _nmGateAt, .7f), tear);
                    fov = Mathf.Lerp(58f, 62f, u);
                }
                else if (t < NmDive)
                {
                    // THE FIRST ONE: low on the court in front of the first player. It opens on the gate as the soul crawls out
                    // of it, follows it down as it glides over them and circles, then settles on him as it dives in.
                    // (film r38: from over her shoulder her head filled a third of the frame and the soul was a speck.)
                    Vector3 leadAt = t < NmLeadIn && NmSoulAt(_nmSouls[0], 0, t, out var l) ? l : NmChestAt(first, t);
                    float down = Ease(NmLead + .35f, NmLead + 1.0f, t);
                    float onHim = Ease(NmLeadOut - .15f, NmLeadIn, t);
                    float push = Ease(NmLead, NmDive, t);
                    Vector3 toward = first.Home; toward.y = 0f; toward = toward.sqrMagnitude > 1e-4f ? toward.normalized : way;
                    eye = first.Home - toward * Mathf.Lerp(4.4f, 3.4f, push) + Vector3.Cross(up, toward) * 1.2f + up * Mathf.Lerp(1.0f, 1.1f, push);
                    // The soul is followed with a little lag, as a hand on a camera would.
                    Vector3 trail = t < NmLeadIn && NmSoulAt(_nmSouls[0], 0, t - .1f, out var lt) ? lt : leadAt;
                    // (film r44: on the gate alone he was cut off at the bottom: it opens on the gate and him together.)
                    Vector3 onSoul = Vector3.Lerp(Vector3.Lerp(NmChestAt(first, t), _nmGateAt, .45f), Vector3.Lerp(trail, NmChestAt(first, t), .35f), down);
                    look = Vector3.Lerp(onSoul, NmChestAt(first, t) + up * .25f, onHim);
                    fov = Mathf.Lerp(Mathf.Lerp(62f, 56f, push), 52f, onHim);
                    if (!_reducedEffects)
                    {
                        float shove = Mathf.Clamp01((t - NmLeadIn) / .06f) * (1f - Ease(NmLeadIn + .2f, NmLeadIn + .5f, t));
                        eye += (look - eye).normalized * (.2f * shove);
                    }
                }
                else
                {
                    var v = _nmVictims[_nmTripper >= 0 ? _nmTripper : 0];
                    if (t < NmCrane) NmFallFrame(v, t, out eye, out look, out fov);
                    else
                    {
                        // THE STRINGS, through her eyes: just in front of her face, at her height, on all of them; wide enough for
                        // every one, then a slow push in as they turn to stare at her, a jolt with the tug.
                        float u = Ease(NmCrane, Seconds, t);
                        Vector3 toThem = _nmCentre; toThem.y = 0f; toThem = toThem.sqrMagnitude > 1e-4f ? toThem.normalized : way;
                        eye = toThem * (.55f + .5f * u) + up * (_nmHerChest.y / .55f * .85f);
                        Vector3 mid = Vector3.zero; float spread = 0f;
                        foreach (var o in _nmVictims) { NmBody(o, t, out var at, out _, out _); mid += at; }
                        mid /= Mathf.Max(1, _nmVictims.Count);
                        Vector3 gaze = (mid + up * .9f - eye).normalized;
                        foreach (var o in _nmVictims) { NmBody(o, t, out var at, out _, out _); spread = Mathf.Max(spread, Vector3.Angle(gaze, at + up * .9f - eye)); }
                        look = eye + gaze * 4f;
                        fov = Mathf.Clamp(spread * 2.3f + 10f, 34f, 62f) * Mathf.Lerp(1f, .94f, u) - 3f * NmBump(t, NmTug + .04f, .12f);
                    }
                }
                // Off every player where they are now (they run, fall and float: `KeepLensOffOthers` knows where they stood).
                for (int i = 0; i < _nmVictims.Count; i++)
                {
                    NmBody(_nmVictims[i], t, out var at, out _, out _);
                    Vector3 away = eye - (at + up * .6f); float gap = new Vector2(away.x, away.z).magnitude;
                    // (film r52, Arena: players stood closer and a head filled the frame's edge at 1.3 m.)
                    if (gap < 1.8f && eye.y < at.y + _nmVictims[i].Top + .4f) { away.y = 0f; eye += (away.sqrMagnitude > 1e-4f ? away.normalized : -way) * (1.8f - gap); }
                }
            }
            // A hand on the camera: a slow drift, never locked off (owner on v9: *"rigid"*).
            if (!_reducedEffects && t > NmSplit)
            {
                eye += new Vector3(Mathf.Sin(t * .9f), .6f * Mathf.Sin(t * .7f + 1f), Mathf.Sin(t * 1.1f + 2f)) * .04f;
                look += new Vector3(Mathf.Sin(t * .8f + 3f), .6f * Mathf.Sin(t * 1.2f + 4f), Mathf.Sin(t * .6f + 5f)) * .06f;
            }
            Vector3 flatEye = new Vector3(eye.x, 0f, eye.z);
            if (flatEye.magnitude < .9f) eye += (flatEye.sqrMagnitude > 1e-4f ? flatEye.normalized : -_nmWay) * (.9f - flatEye.magnitude);
            eye = NmClearLens(eye, look);
            _nmEye = eye; _nmLook = look;
        }

        private readonly RaycastHit[] _nmLensHits = new RaycastHit[16];

        /// <summary>
        /// ⚠️ THE LENS NEVER STANDS IN THE MAP. The shots are computed round the players, and maps are not flat plazas: on
        /// Arena (film r50) the raised court, its rails and the stands put the lens inside a wall or under the floor for most of
        /// the cutscene. A sphere is swept from what the shot looks at back to the eye; the first piece of the map in the way
        /// (not a player, a slipper, the can or a trigger) brings the eye in front of it. Scene space in and out.
        /// </summary>
        private Vector3 NmClearLens(Vector3 eye, Vector3 look)
        {
            Vector3 from = NmW(look), to = NmW(eye);
            Vector3 ray = to - from;
            float length = ray.magnitude;
            if (length < .3f) return eye;
            ray /= length;
            const float radius = .3f;
            int count = Physics.SphereCastNonAlloc(from, radius, ray, _nmLensHits, length, ~0, QueryTriggerInteraction.Ignore);
            float nearest = length;
            for (int i = 0; i < count; i++)
            {
                var hit = _nmLensHits[i];
                if (hit.collider == null || hit.distance <= 0f) continue;
                if (hit.collider.GetComponentInParent<CharacterMotor>() != null || hit.collider.GetComponentInParent<Slipper>() != null
                    || hit.collider.GetComponentInParent<Lata>() != null) continue;
                nearest = Mathf.Min(nearest, hit.distance);
            }
            if (nearest >= length) return eye;
            return _root.transform.InverseTransformPoint(from + ray * Mathf.Max(.6f, nearest - .1f));
        }

        /// <summary>The air shakes as the gate rips open and as it pours; knocks as each of the two is entered and as he hits the court.</summary>
        private Vector3 NemuShake(float t)
        {
            float amp = 0f;
            if (t > NmGateOpen - .1f) amp += .05f * Mathf.Exp(-(t - NmGateOpen + .1f) / .3f);
            if (t > NmBurst) amp += .035f * Mathf.Exp(-(t - NmBurst) / .35f);
            if (_nmVictims.Count > 0 && t > NmLeadIn) amp += .04f * Mathf.Exp(-(t - NmLeadIn) / .12f);
            if (_nmTripper >= 0)
            {
                if (t > NmTrip + .25f) amp += .025f * Mathf.Exp(-(t - NmTrip - .25f) / .1f);
                if (t > NmTripIn) amp += .04f * Mathf.Exp(-(t - NmTripIn) / .12f);
            }
            return new Vector3(Mathf.Sin(t * 71f), Mathf.Sin(t * 53f + 1f), Mathf.Sin(t * 61f + 2f)) * amp;
        }

        /// <summary>
        /// The ink (`Shaders/NemuInk`): the eerie dusk from the start of the prayer to the end (play carries it on, `NemuWorld`);
        /// the frame's edges sinking into ink from her eyes on; one impact frame as the first soul enters the first player; focus
        /// lines on her eyes, the gate ripping open, the pouring and those hits, closing on whoever is being hit. The ink and the
        /// lines let go as play returns.
        /// </summary>
        private void NemuPostProcess(RenderTexture frame, Camera camera, float t)
        {
            float leave = 1f - Ease(Seconds - .6f, Seconds, t);
            float blackoutInk = Ease(NmDark, NmDark + .25f, t) * (1f - Ease(Seconds - .3f, Seconds - .05f, t));
            float eerie = Ease(0f, .5f, t);
            float ink = Mathf.Max(Ease(NmIgnite, NmIgnite + .3f, t) * .7f * leave, blackoutInk);
            // THE STRINGS: the lights die, keeping only violet (the eyes, the gate's lip, the souls).
            // ...and come back, dusk, for the hand-back's dissolve into play (film r57: from black straight into day was a cut).
            float blackout = Ease(NmDark, NmDark + .25f, t) * (1f - Ease(Seconds - .3f, Seconds - .05f, t));
            float impact = 0f;
            if (!_reducedEffects && _nmVictims.Count > 0)
            {
                // One frame, on the first soul going in only (v9's two inverted flashes read as the picture breaking).
                if (t >= NmLeadIn && t < NmLeadIn + NmImpact * .5f) impact = .85f;
            }
            float lines = 0f;
            if (!_reducedEffects)
            {
                lines = Mathf.Max(Flash(t, NmIgnite + .08f, .25f), Mathf.Max(.7f * Flash(t, NmGateOpen, .35f), .5f * Flash(t, NmBurst + .15f, .35f)));
                if (_nmVictims.Count > 0) lines = Mathf.Max(lines, Flash(t, NmLeadIn + .1f, .3f));
                if (_nmTripper >= 0) lines = Mathf.Max(lines, Flash(t, NmTripIn + .1f, .3f));
            }
            var focus = new Vector2(.5f, .55f);
            int shot = NmShotVictim(t);
            if (shot >= 0 && camera != null)
            {
                Vector3 vp = camera.WorldToViewportPoint(NmW(_nmVictims[shot].Chest));
                if (vp.z > 0f) focus = new Vector2(Mathf.Clamp(vp.x, .2f, .8f), Mathf.Clamp(vp.y, .2f, .8f));
            }
            if (eerie <= .01f && ink <= .01f && impact <= 0f && lines <= .01f && blackout <= .01f) return;
            if (_nmInk == null)
            {
                var shader = Resources.Load<Shader>("Shaders/NemuInk");
                if (shader == null) return;
                _nmInk = new Material(shader) { name = "NemuInk" };
            }
            _nmInk.SetFloat("_Ink", ink);
            _nmInk.SetFloat("_Eerie", eerie);
            _nmInk.SetFloat("_Black", blackout);
            _nmInk.SetFloat("_Night", 0f);
            _nmInk.SetFloat("_Impact", impact);
            _nmInk.SetFloat("_Invert", 0f);
            _nmInk.SetFloat("_Lines", lines);
            _nmInk.SetVector("_Focus", new Vector4(focus.x, focus.y, 0f, 0f));
            _nmInk.SetFloat("_Clock", t);
            _nmInk.SetFloat("_Aspect", frame.height > 0 ? frame.width / (float)frame.height : 16f / 9f);
            var tmp = RenderTexture.GetTemporary(frame.descriptor);
            Graphics.Blit(frame, tmp, _nmInk);
            Graphics.Blit(tmp, frame);
            RenderTexture.ReleaseTemporary(tmp);
        }

        /// <summary>The ink's dusk does the colour; this only steps the world back a little so the violet reads.</summary>
        private void NemuGrade(float t, out float brightness, out float saturation)
        {
            float dim = Ease(0f, .8f, t);
            brightness = Mathf.Lerp(1f, .88f, dim);
            saturation = Mathf.Lerp(1f, .88f, dim);
        }

        private void NemuDispose()
        {
            _nmSmoke?.Dispose(); _nmSmoke = null;
            _nmGate?.Dispose(); _nmGate = null;
            foreach (var g in _nmEyeGlows) g.Remove();
            _nmEyeGlows.Clear(); _nmHerEyes = null;
            if (_nmInk != null) { ObjectDestroy(_nmInk); _nmInk = null; }
        }
    }
}
