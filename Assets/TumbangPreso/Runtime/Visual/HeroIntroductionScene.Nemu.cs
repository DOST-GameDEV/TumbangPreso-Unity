using System.Collections.Generic;
using TumbangPreso.CameraSystem;
using UnityEngine;

namespace TumbangPreso.Visual
{
    public sealed partial class HeroIntroductionScene
    {
        // =========================================================================================
        // NEMU, LET THEM IN v9: THE PRAYER, 7.0 s (owner 2026-10-14; `docs/reports/nemu-ultimate-2026-10-11/direction.md`
        // section 18). He kept the soul storm and asked for the story told better, in his words: *"she clasps her hands tgthr
        // and looks liek she's praying or chantinng then the ground splits open and like one muna lalabas and ppl follow it
        // around with their heads and biglang dadami"*; *"may una munag mapoposssess and its gonna make the other 2 ppl run
        // away"*; *"theyre running away but they trip and fall and while fallenn down they lie on ground and turn aroudn and
        // ghost enters them"*; *"in the bg while focused on one, one by one get possessed"*; *"they move unnatural"*; an eerie
        // sky; 5 to 7 s.
        //
        //   THE PRAYER (0.00-0.85): her whole body, hands clasped, head bowed, chanting (a dip of the head and a ring of violet
        //     across the court on each beat); violet gathering between her palms; the day draining to dusk. Her eyes snap open.
        //   THE SPLIT (0.85-1.35): smash cut high behind her: the light drops out of her palms and the court splits open from
        //     her feet, the fissures racing out under the players, who stagger.
        //   THE FIRST ONE (1.35-2.55): low over her shoulder: ONE soul claws up out of a fissure among them and circles; every
        //     head follows it. It turns and dives into the nearest player (2.20): impact frame; he is thrown back, arched; its
        //     tail coils round him and sinks in; he twitches wrong; his eyes ignite (2.50) and he turns them on the others.
        //   SUDDENLY MANY (2.55-3.25): every fissure erupts at once; the lens cranes up with them into the storm; the others run.
        //   THE FALL (3.25-5.50): alongside one of them as a soul hunts him down; he trips and goes down hard, rolls onto his
        //     back and sees it hanging over him; it plunges into his chest (4.60). Behind him, the other is caught running. Then
        //     he is pulled up off the court like a puppet on strings, his eyes ignite.
        //   ALL OF THEM (5.50-7.00): the lens cranes up and back: every player possessed, floating a little, heads tipped too
        //     far, turning to the lens, the court split open under them (it stays open through the possession, `NemuWorld`).
        //
        // Rules that still hold: black and one violet (no white, no amber); no Kuro; after the entry nothing is drawn on a
        // player but the aura and their own eyes burning violet; medium and wide shots only; nothing chonky; 5 to 7 s. Nothing
        // runs on Update and nothing is random: every soul, fissure, fall and twitch is posed from the scene clock.
        // =========================================================================================
        private const float NmIgnite = .72f, NmSplit = .85f, NmCrackDone = 1.35f, NmLead = 1.35f, NmBurst = 2.55f, NmDive = 3.25f;
        /// <summary>The first soul: when it turns on the first player and when it is in him; his eyes.</summary>
        private const float NmLeadOut = 1.9f, NmLeadIn = 2.2f, NmFirstWake = 2.5f;
        /// <summary>The one who falls: running, the chaser turning on him, the trip, the roll onto his back, the soul in, the pull
        /// up off the court, his eyes.</summary>
        private const float NmTripRun = 2.62f, NmTripOut = 3.0f, NmTrip = 3.95f, NmRoll = 4.2f, NmTripIn = 4.6f, NmRise = 4.95f, NmTripWake = 5.3f;
        /// <summary>The rest: running, the chaser, caught in the background.</summary>
        private const float NmBackRun = 2.7f, NmBackOut = 3.1f, NmBackIn = 4.2f;
        private const float NmCrane = 5.5f;
        /// <summary>The chant: her head dips and a ring goes out across the court on each beat.</summary>
        private const float NmChantBeat = .21f;
        private const float NmRunSpeed = 2.4f;
        /// <summary>How long an impact frame lasts: two film frames (the first inverted).</summary>
        private const float NmImpact = .067f;
        /// <summary>The storm thins out and lifts away over the last of the cutscene.</summary>
        private const float NmThinFrom = 6.0f;
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
            /// <summary>Where it claws up out of the court (scene space): a point on a fissure.</summary>
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
            /// <summary>Which way they run (scene space, flat), when, and its heading.</summary>
            public Vector3 Flee = Vector3.forward;
            public float RunAt = 99f, RunYaw;
            /// <summary>Which side of them the lens tracks (+1 or -1).</summary>
            public float Side = 1f;
            public NemuEyeGlow Eyes;
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
        private NemuCrackField _nmField;
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
            // The fissures: the same court that stays split open through the possession (`NemuWorld`).
            _nmField = new NemuCrackField(_root.transform, NemuWorld.CrackLength);
            BuildSouls();
        }

        private static float NmHash(int i, int k) => NemuCrackField.Hash(i, k);

        /// <summary>
        /// The storm: every soul's typed path. Soul 0 is THE FIRST ONE, clawing up among the players alone and diving into the
        /// first player; the rest erupt out of every fissure at once (SUDDENLY MANY). Three per player are divers: the one that
        /// gets them, then two that drain in after.
        /// </summary>
        private void BuildSouls()
        {
            int count = _reducedEffects ? 36 : 64;
            int victims = _nmVictims.Count;
            Vector3 up = Vector3.up;
            float length = NemuWorld.CrackLength;

            // THE FIRST ONE: out of the fissure point nearest the spot just in front of the first player (toward her).
            {
                Vector3 want = victims > 0 ? _nmVictims[0].Home - _nmWay * 1.3f : _nmCentre;
                int bestC = 0; float bestAlong = 1f, bestGap = float.MaxValue;
                for (int c = 0; c < NemuCrackField.Cracks; c++)
                    for (float along = 1f; along < length * .9f; along += .25f)
                    {
                        float gap = (_nmField.Point(c, along) - want).sqrMagnitude;
                        if (gap < bestGap) { bestGap = gap; bestC = c; bestAlong = along; }
                    }
                _nmSouls.Add(new NmSoul
                {
                    Victim = -1, Lead = true, From = _nmField.Point(bestC, bestAlong), Born = NmLead, Rise = .6f,
                    Height = 2.6f, Radius = _nmStormReach * .6f, Speed = 5.5f / Mathf.Max(1f, _nmStormReach * .6f), Phase = 1.1f,
                });
            }
            // SUDDENLY MANY: every fissure erupts within a third of a second.
            for (int i = 1; i < count; i++)
            {
                var soul = new NmSoul { Victim = -1 };
                float along = Mathf.Lerp(.5f, length * .95f, NmHash(i, 9));
                soul.From = _nmField.Point((i * 5) % NemuCrackField.Cracks, along);
                soul.Born = NmBurst + .3f * NmHash(i, 8);
                soul.Rise = .35f + .2f * NmHash(i, 7);
                soul.Height = .9f + 3.1f * NmHash(i, 1);
                float wide = Mathf.Lerp(.35f, 1f, Mathf.Sqrt(NmHash(i, 2)));
                soul.Radius = _nmStormReach * wide * (.8f + .07f * soul.Height);
                soul.Speed = (5f + 2.5f * NmHash(i, 3)) / Mathf.Max(1f, soul.Radius);
                soul.Phase = NmHash(i, 4) * Mathf.PI * 2f;
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
                victim.Hits = new float[3]; victim.Souls = new int[3]; victim.Strength = new float[3];
                for (int j = 0; j < 3; j++)
                {
                    float inAt, outAt, sink, k;
                    if (victim.Role == 0)
                    {
                        inAt = j == 0 ? NmLeadIn : NmBurst + .1f + .07f * j; outAt = j == 0 ? NmLeadOut : inAt - .3f;
                        sink = j == 0 ? .3f : .16f; k = j == 0 ? 1.4f : .5f;
                    }
                    else if (victim.Role == 1)
                    {
                        inAt = j == 0 ? NmTripIn : NmTripIn + .08f + .07f * j; outAt = j == 0 ? NmTripOut : inAt - .3f;
                        sink = j == 0 ? .3f : .16f; k = j == 0 ? 1.4f : .5f;
                    }
                    else
                    {
                        float at = NmBackIn + .2f * (v - 2);
                        inAt = j == 0 ? at : at + .05f + .07f * j; outAt = j == 0 ? NmBackOut + .05f * (v - 2) : inAt - .3f;
                        sink = j == 0 ? .25f : .16f; k = j == 0 ? 1f : .5f;
                    }
                    int pick;
                    if (victim.Role == 0 && j == 0) pick = 0;
                    else
                    {
                        Vector3 target = j == 0 ? victim.Home - victim.Flee * 2.5f + up * 3f
                                                : victim.Home + victim.Flee * 2f + Vector3.Cross(up, victim.Flee) * (.8f * (j - 1.5f)) + up * 3.2f;
                        pick = NmPick(target, outAt, used, count);
                    }
                    used.Add(pick);
                    var soul = _nmSouls[pick];
                    soul.Victim = v; soul.Out = outAt; soul.In = inAt; soul.Sink = sink;
                    soul.Chase = j == 0 && victim.Role > 0; soul.Hero = j == 0 && victim.Role == 1;
                    soul.Offset = j == 0 ? Vector3.zero : new Vector3((NmHash(pick, 5) - .5f) * .2f, (NmHash(pick, 6) - .3f) * .2f, 0f);
                    if (victim.Role == 0 && j == 0)
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
                victim.Wake = victim.Role == 0 ? NmFirstWake : victim.Role == 1 ? NmTripWake : victim.Hits[2] + .5f;
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
                    if (used.Contains(i) || soul.Victim >= 0) continue;
                    if (pass == 0 && soul.Born + soul.Rise > at - .05f) continue;
                    float gap = (NmOrbit(soul, at, out _) - target).sqrMagnitude;
                    if (gap < bestGap) { bestGap = gap; best = i; }
                }
            return Mathf.Max(best, 1);
        }

        /// <summary>How far the fissures' front has run at <paramref name="t"/> (metres): it bursts out from her feet and slows
        /// as it reaches the far side of the court.</summary>
        private static float NmCrackReach(float t)
        {
            float u = Mathf.Clamp01((t - NmSplit) / (NmCrackDone - NmSplit));
            return t <= NmSplit ? 0f : NemuWorld.CrackLength * (1f - (1f - u) * (1f - u));
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
            v.Wake = NmFirstWake;
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

        /// <summary>Pulled up off the court like a puppet: in four jerks, each a snap and a hang (0 lying, 1 upright).</summary>
        private static float NmPuppetRise(float x)
        {
            float r = Mathf.Clamp01(x / .42f) * 4f;
            float whole = Mathf.Floor(r), part = r - whole;
            return Mathf.Clamp01((whole + Ease(.55f, 1f, part)) / 4f);
        }

        /// <summary>
        /// Where a player's body is at <paramref name="s"/> (scene space): its feet, the turn of the whole body, and how far it is
        /// lying down. A stagger back as the court splits; running from the first one; the one who falls trips, goes down on his
        /// face, rolls onto his back, and is pulled up again stiff; the others are thrown on a little as they are caught.
        /// </summary>
        private void NmBody(NmVictim v, float s, out Vector3 pos, out Quaternion rot, out float lying)
        {
            Vector3 away = v.Home; away.y = 0f; away = away.sqrMagnitude > 1e-4f ? away.normalized : v.Flee;
            Vector3 at = v.Home + away * (.25f * Ease(NmSplit + .1f, NmSplit + .45f, s));
            float yaw = Mathf.LerpAngle(v.Yaw, v.RunYaw, Ease(v.RunAt - .08f, v.RunAt + .15f, s));
            rot = Quaternion.Euler(0f, yaw, 0f); lying = 0f; pos = at;
            if (s <= v.RunAt) return;
            float stop = v.Role == 1 ? NmTrip : v.Hits[0];
            float x = Mathf.Min(s, stop) - v.RunAt;
            float d = NmRunSpeed * x * x / (x + .2f);
            if (v.Role == 1)
            {
                // THE FALL: a stumble, down on his face sliding, over onto his back, then hauled up by nothing.
                float fall = Ease(NmTrip + .05f, NmTrip + .25f, s);
                float stumble = NmBump(s, NmTrip + .02f, .08f);
                d += .6f * Ease(NmTrip, NmTrip + .4f, s);
                float roll = Ease(NmRoll, NmRoll + .28f, s);
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
            float lift = Ease(NmThinFrom, Seconds, s);
            float r = soul.Radius * (1f + .08f * Mathf.Sin(s * 1.1f + soul.Phase) + .35f * lift);
            float a = soul.Phase + soul.Speed * s;
            Vector3 round = new Vector3(Mathf.Sin(a), 0f, Mathf.Cos(a));
            Vector3 along = new Vector3(Mathf.Cos(a), 0f, -Mathf.Sin(a));
            float h = soul.Height + .35f * Mathf.Sin(s * 1.7f + soul.Phase * 3f) + 3f * lift * lift;
            velocity = along * (soul.Speed * r) + Vector3.up * (.6f * Mathf.Cos(s * 1.7f + soul.Phase * 3f));
            return _nmEyeOfStorm + round * r + Vector3.up * h;
        }

        /// <summary>A soul that is not diving: up out of its fissure, then circling in the storm. THE FIRST ONE claws up slowly
        /// and circles low over the players alone.</summary>
        private Vector3 NmFree(in NmSoul soul, float s)
        {
            if (soul.Lead)
            {
                float r = s - soul.Born;
                float claw = Ease(0f, .55f, r) + .05f * Mathf.Sin(r * 26f) * (1f - Ease(0f, .55f, r));
                float a = soul.Phase + Mathf.Max(0f, r - .3f) * 3.2f;
                return soul.From + Vector3.up * (.1f + 2.0f * claw) + new Vector3(Mathf.Sin(a), 0f, Mathf.Cos(a)) * (1.3f * Ease(.25f, .8f, r));
            }
            Vector3 orbit = NmOrbit(soul, s, out _);
            float rise = (s - soul.Born) / soul.Rise;
            if (rise >= 1f) return orbit;
            float spin = soul.Phase + rise * 7f;
            float climb = 1f - (1f - rise) * (1f - rise);
            Vector3 spiral = soul.From + Vector3.up * (.05f + climb * 1.8f) + new Vector3(Mathf.Sin(spin), 0f, Mathf.Cos(spin)) * (.7f * rise * rise);
            return Vector3.Lerp(spiral, orbit, rise * rise * (3f - 2f * rise));
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
            DrawCracks(t);
            float calling = Ease(NmIgnite, NmIgnite + .3f, t) * (1f - Ease(NmDive, NmDive + .4f, t));
            if (calling > .01f)
            {
                var state = new NemuAura.State
                {
                    Feet = NmW(Vector3.zero), Forward = _root.transform.forward, Stream = Vector3.zero, Top = _nmHerChest.y / .55f, Width = .55f,
                    Clock = t, Amount = calling, Swell = Mathf.Max(Flash(t, NmSplit, .3f), Flash(t, NmBurst, .3f)), Drag = 0f, Thin = 0f, Seat = 7, Reduced = _reducedEffects,
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
            // The court stays split open to the end (play keeps it open, `NemuWorld`).
            _nmField?.Set(NmCrackReach(t), 1f, t);
        }

        /// <summary>
        /// THE PRAYER: violet gathering between her clasped palms, swelling on each beat of the chant, a ring going out across
        /// the court with each beat; at THE SPLIT it drops out of her palms into the court under her feet.
        /// </summary>
        private void DrawPrayer(float t)
        {
            float on = Ease(.05f, .4f, t) * (1f - Ease(NmSplit, NmSplit + .12f, t));
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
            float drop = Ease(NmSplit - .06f, NmSplit + .04f, t) * (1f - Ease(NmSplit + .04f, NmSplit + .3f, t));
            if (drop > .01f)
                _nmSmoke.Light(NmW(Vector3.Lerp(palms, new Vector3(0f, .12f, 0f), Ease(NmSplit - .06f, NmSplit + .02f, t))), .5f, 1.6f * drop, NemuSmokeBatch.Violet, default, 1f);
        }

        /// <summary>
        /// Light over the fissures (their bodies are `NemuCrackField`): a bright tip where each is splitting now, a glow at her
        /// feet, and black smoke seeping up out of them. Pulled toward the lens (film r30: the court cut flat glows in half).
        /// </summary>
        private void DrawCracks(float t)
        {
            float reach = NmCrackReach(t);
            if (reach <= .01f || _nmField == null) return;
            Vector3 lift = Vector3.up * .12f;
            for (int c = 0; c < NemuCrackField.Cracks; c++)
            {
                if (reach < _nmField.Along[c, NemuCrackField.Steps])
                    _nmSmoke.Light(NmW(_nmField.Point(c, reach) + lift), .35f, 1.5f, NemuSmokeBatch.Violet, default, 1f);
                if (_reducedEffects) continue;
                for (int k = 1; k <= NemuCrackField.Steps; k += 2)
                {
                    float along = _nmField.Along[c, k] - .3f;
                    if (along >= reach) break;
                    float q = Mathf.Repeat(t * .9f + c * .37f + k * .21f, 1f);
                    _nmSmoke.Wisp(NmW(_nmField.Point(c, along) + Vector3.up * (.35f + .9f * q)), NmWDir(Vector3.up), .12f, .5f,
                                  .45f * (1f - q) * Ease(0f, .3f, q), c * .1f + k * .07f, q * .7f, .8f);
                }
            }
            _nmSmoke.Light(NmW(lift), 1.4f, Ease(NmSplit, NmSplit + .15f, t), NemuSmokeBatch.Violet, default, 1.5f);
        }

        /// <summary>
        /// One wailing soul: a black head lit violet from inside with a face on its front (`DrawSoulFace`), and a long black tail
        /// drawn along its own path. As it bursts out of its fissure the fissure flares under it.
        /// </summary>
        private void DrawSoul(in NmSoul soul, int index, float t, Vector3 eye)
        {
            if (t > soul.Born - .2f && t < soul.Born + .35f)
            {
                float flare = NmBump(t, soul.Born, soul.Lead ? .35f : .2f);
                _nmSmoke.Light(NmW(soul.From + Vector3.up * .12f), soul.Lead ? .8f : .45f, (soul.Lead ? 1.6f : .9f) * flare, NemuSmokeBatch.Violet, default, 1f);
            }
            if (t < soul.Born) return;
            float fade = soul.Victim >= 0 ? 1f : 1f - Ease(NmThinFrom + .3f, Seconds - .05f, t);
            fade *= Ease(soul.Born, soul.Born + .15f, t);
            // While the lens is on the fall, the rest of the storm steps back so the hunter is the one you follow.
            if (_nmChaser >= 0 && !soul.Hero && !soul.Chase)
                fade *= 1f - .5f * Ease(NmDive - .1f, NmDive + .1f, t) * (1f - Ease(NmTripIn + .1f, NmTripIn + .5f, t));
            if (fade <= .01f) return;
            for (int k = 0; k < NmTail; k++) _nmTailOn[k] = NmSoulAt(soul, index, t - k * NmTailStep, out _nmTailAt[k]);
            // Thinned as it nears the lens (film r39: souls brushing past it were violet walls).
            if (_nmTailOn[0]) fade *= Mathf.Clamp01((Vector3.Distance(NmW(_nmTailAt[0]), eye) - 1.2f) / 1.4f);
            if (fade <= .01f) return;

            float headSize = soul.Lead ? .62f : soul.Hero ? .5f : soul.Chase ? .46f : .36f + .12f * NmHash(index, 21);   // (film r38: the first one was a speck)
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
            _nmSmoke.Light(head, headSize * (soul.Hero || soul.Lead ? 1.25f : 1.6f) * (sink >= 0f ? 1f - .3f * sink : 1f), (soul.Hero || soul.Lead ? 1.15f : .95f) * fade, NemuSmokeBatch.Violet);
            DrawSoulFace(index, t, head, heading, headSize, squash, fade, eye, dist);
        }

        /// <summary>
        /// A soul's face, on the front of its head the way it flies: whole as it comes at the lens, in profile as it crosses,
        /// gone as it flies away. Every soul its own (owner on v7: *"faces should be varied for ghosts and not js copy pasted"*):
        /// the size, spacing and slant of its hollow eyes, one sometimes larger, and its mouth (a tall wail, a round scream, a
        /// wide crooked howl), each wailing on its own beat. Nothing random: all from its index.
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
            float slant = (NmHash(index, 24) - .4f) * 1.3f;
            float eyeW = .13f + .06f * NmHash(index, 25), eyeL = .22f + .1f * NmHash(index, 26);
            float odd = NmHash(index, 27) > .65f ? .3f * (NmHash(index, 28) - .5f) : 0f;
            int mouth = (int)(NmHash(index, 29) * 3f);
            float beat = 5f + 6f * NmHash(index, 30);
            float wail = .5f + .5f * Mathf.Sin(t * beat + index * 1.7f);

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
                float show = Mathf.Clamp01(facing * 3f + .65f);
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
                float show = Mathf.Clamp01(facing * 3f + .65f);
                if (show > .01f)
                {
                    Vector3 along = (faceUp + faceRight * crook).normalized;
                    float narrow = Mathf.Lerp(.35f, 1f, Mathf.Clamp01(facing));
                    float across = size * wide * narrow, tall = size * open * squash * 1.2f;
                    if (across > tall) _nmSmoke.Mark(at, Vector3.Cross(heading, along), tall, across, fade * show, NmHash(index, 34));
                    else _nmSmoke.Mark(at, along, across, tall, fade * show, NmHash(index, 34));
                }
            }
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

                if (!_reducedEffects && t > soul.Out - .2f && t < soul.In && NmSoulAt(soul, v.Souls[h], t, out var at))
                {
                    float on = Ease(soul.Out - .2f, soul.Out + .05f, t) * Mathf.Min(1f, .45f + .45f * k);
                    float len = Vector3.Distance(at, door);
                    int dots = Mathf.Clamp(Mathf.CeilToInt(len / .13f), 2, 40);
                    for (int j = 1; j < dots; j++)
                    {
                        float u = j / (float)dots;
                        float pulse = .5f + .5f * Mathf.Sin(u * len * 5f - t * 30f);
                        _nmSmoke.Light(NmW(Vector3.Lerp(at, door, u)), .08f + .03f * pulse, on * (.2f + .4f * pulse), NemuSmokeBatch.Violet);
                    }
                }

                float since = t - v.Hits[h];
                if (since < 0f) continue;

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
                    if (k >= 1f)
                        for (int j = 0; j < ring; j++)
                        {
                            float ang = j * Mathf.PI * 2f / ring;
                            Vector3 p = ground + new Vector3(Mathf.Cos(ang), 0f, Mathf.Sin(ang)) * (.3f + 1.5f * e) + up * .12f;
                            _nmSmoke.Light(NmW(p), .22f, .9f * gone, NemuSmokeBatch.Violet, default, 1f);
                        }
                    _nmSmoke.Light(NmW(chest), v.Width * 1.2f * Mathf.Min(1.2f, k), 1.1f * Mathf.Min(1f, k) * Flash(t, v.Hits[h] + .02f, .1f), NemuSmokeBatch.Violet);
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
                float climb = Ease(v.Wake - .3f, v.Wake, t);
                if (climb > 0f && climb < 1f && v.Head != null)
                    for (int j = 0; j < 5; j++)
                    {
                        float u = Mathf.Clamp01(climb - j * .06f);
                        _nmSmoke.Light(Vector3.Lerp(front, face, u), .16f - .02f * j, .9f * (1f - j * .18f), NemuSmokeBatch.Violet, default, .3f);
                    }
            }
            float eyes = Ease(v.Wake, v.Wake + .06f, t);
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

            // What they look at: THE FIRST ONE while it circles; then (the others) the first possessed; (him) the others.
            float follow = Ease(NmLead + .1f, NmLead + .4f, t) * (1f - Ease(v.Role == 0 ? NmLeadIn - .05f : NmLeadIn, NmLeadIn + .1f, t));
            Vector3? watchAt = null;
            float watch = 0f;
            if (follow > 0f && NmSoulAt(_nmSouls[0], 0, t, out var lead)) { watchAt = lead; watch = follow; }
            else if (v.Role > 0 && t > NmLeadIn && t < v.RunAt + .1f) { watchAt = NmChestAt(_nmVictims[0], t) + Vector3.up * .3f; watch = Ease(NmLeadIn, NmLeadIn + .15f, t) * (1f - Ease(v.RunAt - .05f, v.RunAt + .1f, t)); }
            else if (v.Role == 0 && _nmVictims.Count > 1 && t > v.Wake && t < NmDive) { watchAt = NmChestAt(_nmVictims[1], t) + Vector3.up * .3f; watch = Ease(v.Wake, v.Wake + .1f, t); }
            float watchYaw = 0f, watchPitch = 0f;
            if (watchAt.HasValue)
            {
                Vector3 to = watchAt.Value - (pos + Vector3.up * v.Top * .8f);
                Vector3 flat = new Vector3(to.x, 0f, to.z);
                Vector3 facing = rot * Vector3.forward; facing.y = 0f;
                watchYaw = Mathf.Clamp(Vector3.SignedAngle(facing, flat, Vector3.up), -150f, 150f);
                watchPitch = -Mathf.Clamp(Mathf.Atan2(to.y, Mathf.Max(.2f, flat.magnitude)) * Mathf.Rad2Deg, -20f, 55f);
            }

            // The turn to the lens at the end.
            Vector3 fwdBase = NmWDir(rot * Vector3.forward); fwdBase.y = 0f;
            Vector3 toLens = NmW(_nmEye) - NmW(pos); toLens.y = 0f;
            float turn = toLens.sqrMagnitude > 1e-4f && fwdBase.sqrMagnitude > 1e-4f ? Vector3.SignedAngle(fwdBase, toLens, Vector3.up) : 0f;
            float turnAt = Mathf.Max(NmCrane + .1f, v.Wake + .3f) + .05f * index;
            float headTurn = Ease(turnAt, turnAt + .25f, t), chestTurn = Ease(turnAt + .1f, turnAt + .4f, t), bodyTurn = Ease(turnAt + .2f, turnAt + .5f, t);
            float bodyYaw = (turn * .5f * bodyTurn + watchYaw * .4f * watch) * standing;
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

            float stagger = NmBump(t, NmSplit + .12f, .2f);
            float flinch = NmBump(t, NmBurst + .1f, .15f);
            float scared = Ease(NmSplit + .1f, NmSplit + .4f, t) * (1f - Ease(v.RunAt - .1f, v.RunAt + .1f, t)) * (1f - watch) * (v.Role == 0 ? 1f - Ease(first - .1f, first, t) : 1f);
            float running = Ease(v.RunAt - .05f, v.RunAt + .15f, t) * (1f - Ease(v.Role == 1 ? NmTrip : first - .02f, v.Role == 1 ? NmTrip + .1f : first + .05f, t));
            float down = v.Role == 1 ? Ease(NmTrip, NmTrip + .15f, t) * (1f - Ease(first - .02f, first + .05f, t)) : 0f;   // flailing on the court
            float wrong = Ease(first + .3f, first + .45f, t) * (1f - Ease(v.Wake + .4f, v.Wake + .8f, t)) * (v.Role == 1 ? 1f - .6f * NmBump(t, NmRise + .2f, .25f) : 1f);
            float stance = Ease(v.Wake + .25f, v.Wake + .8f, t);

            int glance = Mathf.FloorToInt((t + seat) / .45f);
            float gu = Ease(0f, .1f, Mathf.Repeat(t + seat, .45f));
            float glanceYaw = Mathf.Lerp((NmHash(glance - 1, v.Seat + 40) - .5f) * 140f, (NmHash(glance, v.Seat + 40) - .5f) * 140f, gu);
            float glancePitch = Mathf.Lerp(-8f - 20f * NmHash(glance - 1, v.Seat + 41), -8f - 20f * NmHash(glance, v.Seat + 41), gu);
            float lost = (NmHash(glance, v.Seat + 42) - .5f) * 26f;
            float tremble = Mathf.Sin(t * 47f + seat) * 1.4f * scared;

            float stride = (t - v.RunAt) * 15f;
            float swing = Mathf.Sin(stride) * running;
            float bob = Mathf.Abs(Mathf.Cos(stride)) * .07f * running;
            float lookBack = running * Mathf.Max(NmBump(t, v.RunAt + .35f, .16f), NmBump(t, (v.Role == 1 ? NmTrip : first) - .3f, .14f));
            float backSide = (v.Seat & 1) == 0 ? 1f : -1f;
            float scrabble = Mathf.Sin(t * 22f + seat) * down;

            float step = .09f + .05f * NmHash(v.Seat, 50);
            float clock = t + seat * .13f;
            int key = Mathf.FloorToInt(clock / step);
            float snap = Ease(.65f, 1f, Mathf.Repeat(clock, step) / step);
            float Broken(int slot, float range) => Mathf.Lerp(NmHash(key * 7 + v.Seat, slot) - .5f, NmHash((key + 1) * 7 + v.Seat, slot) - .5f, snap) * 2f * range * wrong;

            float tip = ((v.Seat & 1) == 0 ? 1f : -1f) * 30f * stance;
            float sway = Mathf.Sin(t * 1.4f + seat) * 3f * stance;

            if (v.Root.T != null)
                v.Root.T.position += NmWDir(shove) * standing + Vector3.up * ((lift + .1f * stagger + .05f * flinch + bob + (.08f + .06f * NmHash(key, v.Seat + 51)) * wrong * standing + .07f * stance + Mathf.Sin(t * 2f + seat) * .015f * stance))
                                   + right * (Mathf.Sin(t * 13f) * .03f * stagger);

            float torsoTwist = scared * glanceYaw * .2f + watchYaw * .2f * watch - swing * 8f + scrabble * 10f + Broken(1, 35f) + (turn * .8f * chestTurn - turn * .5f * bodyTurn) * standing;
            float torsoPitch = scared * 10f + running * 14f + flinch * 12f - jerk * 22f + Broken(2, 26f) - stance * 4f - down * 12f;
            float torsoRoll = tremble + Broken(3, 22f) + sway + stagger * 6f * Mathf.Sin(t * 20f);
            if (v.Torso.T != null)
            {
                Turn(v.Torso.T, up, torsoTwist);
                Turn(v.Torso.T, right, torsoPitch);
                Turn(v.Torso.T, fwd, torsoRoll);
            }

            float headYaw = scared * glanceYaw * .8f + watchYaw * .6f * watch + lookBack * 100f * backSide + scrabble * 25f + Broken(4, 80f)
                          + (turn * headTurn - turn * .8f * chestTurn) * standing;
            float headPitch = scared * glancePitch + watchPitch * watch - Mathf.Min(jerk, 1.2f) * 26f + Broken(5, 35f) + stance * 5f + stagger * 10f - down * 20f;
            float headRoll = scared * lost * .5f + Broken(6, 55f) + tip;
            if (v.HeadBone.T != null)
            {
                Turn(v.HeadBone.T, up, headYaw);
                Turn(v.HeadBone.T, v.HeadBone.T.parent != null ? Vector3.Cross(up, v.HeadBone.T.parent.forward).normalized : right, headPitch);
                Turn(v.HeadBone.T, fwd, headRoll);
            }

            float guard = scared * 60f + flinch * 40f + down * 70f;
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
            if (t < NmBurst) return 0;
            if (t < NmDive) return -1;
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
            float fallen = Ease(NmTrip, NmTrip + .4f, t);
            float hover = Ease(NmRoll, v.Hits[0], t) * (1f - Ease(v.Hits[0], v.Hits[0] + .3f, t));
            // (film r38: the hunter rode the frame's edge: the lens looks further back, at the room behind him.)
            eye = Vector3.Lerp(p + sd * 3.3f + fl * .4f + up * 1.0f, mid + sd * 2.6f + fl * .2f + up * 1.2f, fallen);
            look = Vector3.Lerp(p - fl * 1.3f + up * (v.Top * .65f), chest + up * (.3f + .9f * hover), fallen);
            fov = Mathf.Lerp(58f, 54f + 4f * hover, fallen);
            // His rise: round in front of his face as he is hauled up.
            float rise = Ease(NmRise - .05f, NmRise + .5f, t);
            if (rise > 0f)
            {
                Vector3 front = p - fl * 2.5f + sd * 1.4f + up * .95f;
                eye = Vector3.Lerp(eye, front, rise);
                look = Vector3.Lerp(look, p + up * (v.Top * .58f), rise);
                fov = Mathf.Lerp(fov, 52f, rise);
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
                    float u = Ease(NmSplit, NmLead, t);
                    eye = -way * Mathf.Lerp(2.6f, 3.1f, u) + right * 1.3f + up * Mathf.Lerp(3.3f, 3.9f, u);
                    look = way * Mathf.Lerp(1.6f, 3.6f, u) + up * .1f;
                    fov = Mathf.Lerp(58f, 62f, u);
                }
                else if (t < NmDive)
                {
                    // THE FIRST ONE, low over her shoulder: on the soul as it circles, then on him as it dives in; SUDDENLY
                    // MANY: the lens cranes up and back with the eruption into the storm.
                    Vector3 leadAt = t < NmLeadIn && NmSoulAt(_nmSouls[0], 0, t, out var l) ? l : NmChestAt(first, t);
                    float onHim = Ease(NmLeadOut - .1f, NmLeadIn, t);
                    float push = Ease(NmLead, NmBurst, t);
                    // (film r38: from over her shoulder her head filled a third of the frame and the soul was a speck: the lens
                    // stands out on the court in front of the first player, low, looking up at it among them.)
                    Vector3 toward = first.Home; toward.y = 0f; toward = toward.sqrMagnitude > 1e-4f ? toward.normalized : way;
                    Vector3 low = first.Home - toward * Mathf.Lerp(3.4f, 2.8f, push) + Vector3.Cross(up, toward) * 1.0f + up * 1.0f;
                    Vector3 lowLook = Vector3.Lerp(Vector3.Lerp(_nmCentre + up * 1.0f, leadAt, .55f), NmChestAt(first, t) + up * .25f, onHim);
                    float crane = Ease(NmBurst, NmBurst + .6f, t);
                    Vector3 high = -way * 3.4f + right * 1.6f + up * 4.3f;
                    Vector3 highLook = _nmEyeOfStorm + up * 1.9f;
                    eye = Vector3.Lerp(low, high, crane);
                    look = Vector3.Lerp(lowLook, highLook, crane);
                    fov = Mathf.Lerp(Mathf.Lerp(56f, 50f, push), 64f, crane);
                    if (!_reducedEffects)
                    {
                        float shove = Mathf.Clamp01((t - NmLeadIn) / .05f) * (1f - Ease(NmLeadIn + .2f, NmLeadIn + .45f, t));
                        eye += (look - eye).normalized * (.2f * shove);
                    }
                }
                else
                {
                    var v = _nmVictims[_nmTripper >= 0 ? _nmTripper : 0];
                    float back0 = 4.3f;
                    foreach (var o in _nmVictims)
                    {
                        NmBody(o, Seconds, out var p, out _, out _);
                        Vector3 off = p - _nmCentre;
                        back0 = Mathf.Max(back0, (Mathf.Abs(Vector3.Dot(off, right)) + 1.4f) / .62f - Vector3.Dot(off, way));
                    }
                    Vector3 wide = _nmCentre - way * back0 + right * 2.4f + up * 3.9f;
                    Vector3 lookWide = _nmCentre + up * 1.5f;
                    if (t < NmCrane) NmFallFrame(v, t, out eye, out look, out fov);
                    else
                    {
                        // ALL OF THEM: a cut on the beat to the players low and near, craning up and back to the wide (films r38
                        // to r41: every move out of his close shot passed through his head).
                        float u = Ease(NmCrane, Seconds, t);
                        Vector3 near = _nmCentre - way * (back0 * .55f) + right * 2.6f + up * 1.9f;   // (film r42: from behind her her head filled a quarter of the frame)
                        eye = Vector3.Lerp(near, wide, u);
                        look = Vector3.Lerp(_nmCentre + up * 1.0f, lookWide, u);
                        fov = Mathf.Lerp(54f, 56f, u);
                    }
                }
                // Off every player where they are now (they run, fall and float: `KeepLensOffOthers` knows where they stood).
                for (int i = 0; i < _nmVictims.Count; i++)
                {
                    NmBody(_nmVictims[i], t, out var at, out _, out _);
                    Vector3 away = eye - (at + up * .6f); float gap = new Vector2(away.x, away.z).magnitude;
                    if (gap < 1.3f && eye.y < at.y + _nmVictims[i].Top + .4f) { away.y = 0f; eye += (away.sqrMagnitude > 1e-4f ? away.normalized : -way) * (1.3f - gap); }
                }
            }
            Vector3 flatEye = new Vector3(eye.x, 0f, eye.z);
            if (flatEye.magnitude < .9f) eye += (flatEye.sqrMagnitude > 1e-4f ? flatEye.normalized : -_nmWay) * (.9f - flatEye.magnitude);
            _nmEye = eye; _nmLook = look;
        }

        /// <summary>The ground shakes as it splits and as they erupt; knocks as each of the two is entered and as he hits the court.</summary>
        private Vector3 NemuShake(float t)
        {
            float amp = 0f;
            if (t > NmSplit) amp += .07f * Mathf.Exp(-(t - NmSplit) / .25f);
            if (t > NmBurst) amp += .05f * Mathf.Exp(-(t - NmBurst) / .3f);
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
        /// the frame's edges sinking into ink from her eyes on; an impact frame as the first soul enters the first player and
        /// as the hunter enters the one who fell; focus lines on her eyes, the split, the eruption and those hits, closing on
        /// whoever is being hit. The ink and the lines let go as play returns.
        /// </summary>
        private void NemuPostProcess(RenderTexture frame, Camera camera, float t)
        {
            float leave = 1f - Ease(Seconds - .6f, Seconds, t);
            float eerie = Ease(.05f, .8f, t);
            float ink = Ease(NmIgnite, NmIgnite + .3f, t) * .7f * leave;
            float impact = 0f; bool invert = false;
            if (!_reducedEffects && _nmVictims.Count > 0)
            {
                if (t >= NmLeadIn && t < NmLeadIn + NmImpact) { impact = 1f; invert = t < NmLeadIn + NmImpact * .5f; }
                if (_nmTripper >= 0 && t >= NmTripIn && t < NmTripIn + NmImpact) { impact = 1f; invert = t < NmTripIn + NmImpact * .5f; }
            }
            float lines = 0f;
            if (!_reducedEffects)
            {
                lines = Mathf.Max(Flash(t, NmIgnite + .08f, .2f), Mathf.Max(.8f * Flash(t, NmSplit + .1f, .25f), .8f * Flash(t, NmBurst + .12f, .3f)));
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
            if (eerie <= .01f && ink <= .01f && impact <= 0f && lines <= .01f) return;
            if (_nmInk == null)
            {
                var shader = Resources.Load<Shader>("Shaders/NemuInk");
                if (shader == null) return;
                _nmInk = new Material(shader) { name = "NemuInk" };
            }
            _nmInk.SetFloat("_Ink", ink);
            _nmInk.SetFloat("_Eerie", eerie);
            _nmInk.SetFloat("_Black", 0f);
            _nmInk.SetFloat("_Night", 0f);
            _nmInk.SetFloat("_Impact", impact);
            _nmInk.SetFloat("_Invert", invert ? 1f : 0f);
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
            _nmField?.Dispose(); _nmField = null;
            foreach (var g in _nmEyeGlows) g.Remove();
            _nmEyeGlows.Clear(); _nmHerEyes = null;
            if (_nmInk != null) { ObjectDestroy(_nmInk); _nmInk = null; }
        }
    }
}
