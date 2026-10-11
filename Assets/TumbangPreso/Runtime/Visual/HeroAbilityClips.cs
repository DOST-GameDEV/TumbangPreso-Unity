using System.Collections.Generic;
using UnityEngine;

namespace TumbangPreso.Visual
{
    /// <summary>
    /// Builds bespoke procedural AnimationClips for all 15 hero abilities on the 7-bone skeleton:
    /// root · torso · head · arm-left · arm-right · leg-left · leg-right.
    ///
    /// ⚠️ PROCEDURAL ANIMATION FOR THE 7-BONE RIG:
    /// Like `DanceClip`, these clips are built at runtime directly from mathematical curves and
    /// bone hierarchies, fitting all voxel and character models without needing external clip assets.
    ///
    /// ⚠️ AXES AND SIGNS:
    /// Matches the rig conventions established in `DanceClip.cs`:
    /// - arm-left: +Z swings outward/up, -X swings forward.
    /// - arm-right: -Z swings outward/up, -X swings forward.
    /// - head: +X tilts down, -X tilts up, +Y turns left, +Z tilts left.
    /// - torso: +X leans forward, -X leans back, +Y twists left, +Z leans left.
    /// - leg-left/leg-right: -X swings forward, +X swings back.
    /// - root position: Y lifts/crouches, Z moves forward/back in model units.
    /// </summary>
    public static partial class HeroAbilityClips
    {
        private static readonly string[] Bones =
        {
            "root", "torso", "head", "arm-left", "arm-right", "leg-left", "leg-right",
        };

        private static Transform FindDeep(Transform root, string name)
        {
            if (root.name == name) return root;

            for (int i = 0; i < root.childCount; i++)
            {
                var found = FindDeep(root.GetChild(i), name);
                if (found != null) return found;
            }

            return null;
        }

        private static string RelativePath(Transform root, Transform child)
        {
            var parts = new List<string>();
            for (var t = child; t != null && t != root; t = t.parent)
                parts.Insert(0, t.name);
            return string.Join("/", parts);
        }

        private static Dictionary<string, string> ResolvePaths(Transform animatorRoot)
        {
            // A `RigPaths` so the elbows can ride along without changing what any builder is handed.
            // The dictionary itself holds the same seven entries as before.
            var paths = new RigPaths();
            foreach (string bone in Bones)
            {
                var t = FindDeep(animatorRoot, bone);
                if (t == null) return null;
                paths[bone] = RelativePath(animatorRoot, t);
            }
            // The elbows are optional. A rig without them resolves exactly as it always did.
            paths.Left = ResolveForearm(animatorRoot, "arm-left", "forearm-left", -1f);
            paths.Right = ResolveForearm(animatorRoot, "arm-right", "forearm-right", 1f);
            return paths;
        }

        // -------------------------------------------------------------------
        // THE ELBOWS (2026-10-07). The redesign rigs carry `forearm-left` and `forearm-right` under
        // the arm bones (`tools/author_character_redesign_paete.py`, `EXTRA_BONES`); the Classic
        // cast and the old models do not. A clip may key them, and a clip that does not is built
        // exactly as before: no forearm curve is written unless a key asks for one, and never on a
        // rig that has no such bone.
        //
        // A key carries two numbers per forearm (`Fore(fold, stretch)`, see `PoseKey`):
        //  * FOLD, in degrees. 0 is the straight forearm every clip has had until now. Positive
        //    closes the elbow the way a real one closes: the fist comes toward the FRONT of the
        //    upper arm. With the arm hanging that is forward, with the arm pointed ahead it is up
        //    toward the face, in the T-pose it is forward. It is the same forward the arm tables'
        //    negative pitch swings to (the rig's +Z; `CharacterVisual.PersonModelYaw` is 0).
        //  * STRETCH, a scale along the forearm. 1 is the modelled length. Paete's vines stretch.
        //
        // WHICH AXIS, AND WHICH WAY. The author script lays every joint down with no rotation and
        // the arms T-posed along X, the elbow at (+-0.360, 0.470, 0), so the forearm's own axes are
        // the model's axes at rest and the forearm runs along its local X. Forward is +Z, so the
        // hinge that carries the fist forward is the local Y axis: the curve written is
        // `localEulerAnglesRaw.y`, with x and z held at 0. The stretch is `localScale.x`.
        //
        // The SIGN of that y angle depends on which way along X the forearm runs, and that is not
        // knowable from the bone's name: the importer mirrors glTF's X, which is why
        // `CharacterAnimator.AlongArm` measures it off the bind pose instead of assuming it. The
        // same is done here, from the rig itself: the elbow joint sits further along the arm than
        // the shoulder, so the sign of the forearm's rest `localPosition.x` IS the direction from
        // shoulder to fist in the bone's own frame. A positive turn about Y carries +X toward -Z,
        // so a forearm running along +X needs a NEGATIVE y to fold forward and one running along
        // -X needs a positive y: y = -sign(localPosition.x) * fold. On today's import the left
        // forearm runs along -X (y = +fold) and the right along +X (y = -fold), which is also the
        // fallback by name if a rig gives no usable offset. A table never writes a sign: it writes
        // the fold, for the left or the right forearm, and both close the same way.
        //
        // This is a plain hinge in the upper arm's frame. The walk's `PoseElbow` also splays the
        // forearm 30 degrees outward in the character's frame; a cast gets that from the upper
        // arm's twist instead, because a curve cannot see the character's frame.
        //
        // A forearm that does not sit directly under its arm bone, or does not rest unrotated, is
        // treated as absent, so "fold 0" can never mean anything but the forearm as modelled.
        // -------------------------------------------------------------------

        private struct ForearmBone
        {
            /// <summary>The curve path, or null on a rig with no elbow.</summary>
            public string Path;
            /// <summary>Degrees of `localEulerAnglesRaw.y` per degree of fold: +1 or -1, measured off the rest pose.</summary>
            public float FoldSign;
            /// <summary>The bone's rest scale, which a stretch multiplies along X.</summary>
            public Vector3 RestScale;
        }

        private sealed class RigPaths : Dictionary<string, string>
        {
            public ForearmBone Left, Right;
        }

        /// <summary>One forearm's part of a pose: how far the elbow is closed and how long the forearm is.</summary>
        private readonly struct Forearm
        {
            public readonly float Fold, Stretch;
            /// <summary>False for `default`, which is how a pose says nothing about this forearm.</summary>
            public readonly bool Keyed;
            public Forearm(float fold, float stretch) { Fold = fold; Stretch = stretch; Keyed = true; }
        }

        /// <summary>A forearm folded `fold` degrees (0 straight) and `stretch` times its modelled length (1 as modelled).</summary>
        private static Forearm Fore(float fold, float stretch = 1f) => new Forearm(fold, stretch);

        private static ForearmBone ResolveForearm(Transform animatorRoot, string armName, string forearmName, float alongByName)
        {
            var none = new ForearmBone();
            // Under the arm this clip already drives, not anywhere in the hierarchy: a body can carry
            // a second, hidden rig with the same bone names (`CharacterAnimator.ResolveSwingBones`).
            var arm = FindDeep(animatorRoot, armName);
            if (arm == null) return none;
            Transform fore = null;
            for (int i = 0; i < arm.childCount && fore == null; i++)
                if (arm.GetChild(i).name == forearmName) fore = arm.GetChild(i);
            if (fore == null || Quaternion.Angle(fore.localRotation, Quaternion.identity) > 0.05f) return none;
            float along = fore.localPosition.x;
            float direction = Mathf.Abs(along) > 1e-4f ? Mathf.Sign(along) : alongByName;
            return new ForearmBone
            {
                Path = RelativePath(animatorRoot, fore),
                FoldSign = -direction,
                RestScale = fore.localScale,
            };
        }

#if UNITY_EDITOR
        private static readonly Dictionary<string, float[]> PunchTimes = new Dictionary<string, float[]>();

        /// <summary>
        /// The instants the last built clip of this name lands on (`ClipBuilder.PunchAt`), for the editor's review
        /// filmstrips, which show those frames as well as the evenly spaced ones. Empty for a clip that has none.
        /// </summary>
        public static float[] PunchTimesOf(string clipName) =>
            clipName != null && PunchTimes.TryGetValue(clipName, out var times) ? (float[])times.Clone() : new float[0];
#endif

        // -------------------------------------------------------------------
        // § TIMING: why fifteen different poses read as one animation.
        //
        // ⚠️⚠️ EVERY CLIP BELOW IS KEYED WELL AND TIMED IDENTICALLY, AND THE TIMING IS WHAT A
        // PLAYER ACTUALLY FEELS. 🧑, 2026-08-26: *"thoroughly plan how to make all animations
        // better and more fun"*, in the same breath as *"the same logic and code was used to
        // generate all of them"* about the effects. It is the same fault one layer down. The
        // poses are bespoke: Sean dives, Dante stomps, Cheska raises, Nemu fades. The
        // INTERPOLATION between them was shared by all fifteen and by every bone in each.
        //
        // ⚠️⚠️ THE CAUSE IS ONE DEFAULT. `AnimationCurve.AddKey(time, value)` gives a key
        // SMOOTH (auto) tangents, so the curve arrives at every pose decelerating and leaves it
        // accelerating. Chain four of those and you get sinusoidal motion: the body drifts from
        // pose to pose at an even speed and never arrives anywhere. That is the correct default
        // for a walk cycle and it is wrong for all fifteen of these, because every one of them is
        // a STRIKE, and a strike is defined by the moment it stops.
        //
        // ⚠️⚠️ SO A KEY CAN NOW BE MARKED AS AN IMPACT, AND THE CURVE IS BUILT AROUND IT.
        // `PunchAt` says "this instant is where the blow lands". Three things follow from it, and
        // they are the whole of what makes an action feel weighty:
        //  * the pose BEFORE it leaves slowly, which is anticipation: the wind-up hangs;
        //  * the impact pose is arrived at ACCELERATING and overshoots slightly, so the last few
        //    degrees are the fastest;
        //  * and the body STOPS DEAD on it rather than easing through, which is the hitstop the
        //    eye reads as force.
        //
        // ⚠️ IT IS ONE LINE PER CLIP, WHICH IS THE REASON IT IS SHAPED THIS WAY. Fifteen clips
        // times seven bones times three axes is 315 curves; nothing that has to be applied per
        // curve would ever be applied consistently. A clip names the instant it lands and every
        // bone in it agrees, which is also correct: a body's limbs all stop on the same frame or
        // the pose falls apart.
        //
        // ⚠️ AND THE TANGENTS ARE WRITTEN, NOT SMOOTHED. `Keyframe(time, value, in, out)` is the
        // constructor that leaves tangents alone; `AddKey` followed by editing `keys` does not,
        // because a key added that way carries an AUTO tangent mode that recomputes and quietly
        // throws the edit away. That is why this builds its keyframes from stored values at the
        // end rather than accumulating an `AnimationCurve` as it goes.
        // -------------------------------------------------------------------

        private sealed class ClipBuilder
        {
            /// <summary>How much faster than linear the body arrives at an impact pose.</summary>
            private const float PunchIn = 2.1f;

            /// <summary>How much SLOWER than linear it leaves the pose before one.</summary>
            private const float AnticipateOut = 0.22f;

            /// <summary>Keys within this many seconds of a punch time count as that impact.</summary>
            private const float PunchEpsilon = 0.001f;

            private readonly string _name;
            private readonly Dictionary<string, string> _paths;

            // Time in x, value in y. Kept raw so `Build` can compute tangents with the
            // neighbours in hand, which is not possible while keys are being added one at a time.
            private readonly List<Vector2> _rootX = new List<Vector2>();
            private readonly List<Vector2> _rootY = new List<Vector2>();
            private readonly List<Vector2> _rootZ = new List<Vector2>();
            private readonly Dictionary<string, List<Vector2>[]> _rot =
                new Dictionary<string, List<Vector2>[]>();

            private readonly List<float> _punches = new List<float>();

            // The elbows, left then right: time in x, fold degrees in y, stretch in z, and whether the
            // pose said anything about this forearm at all. Nothing is written from these unless at
            // least one entry was keyed and the rig has the bone (`BuildForearm`).
            private readonly List<Vector3>[] _fore = { new List<Vector3>(), new List<Vector3>() };
            private readonly List<bool>[] _foreKeyed = { new List<bool>(), new List<bool>() };
            private readonly RigPaths _rig;

            public ClipBuilder(string name, Dictionary<string, string> paths)
            {
                _name = name;
                _paths = paths;
                _rig = paths as RigPaths;
                foreach (string bone in Bones)
                {
                    _rot[bone] = new[]
                    {
                        new List<Vector2>(), new List<Vector2>(), new List<Vector2>(),
                    };
                }
            }

            public void KeyPos(float time, float x, float y, float z)
            {
                _rootX.Add(new Vector2(time, x));
                _rootY.Add(new Vector2(time, y));
                _rootZ.Add(new Vector2(time, z));
            }

            public void KeyRot(string bone, float time, float x, float y, float z)
            {
                _rot[bone][0].Add(new Vector2(time, x));
                _rot[bone][1].Add(new Vector2(time, y));
                _rot[bone][2].Add(new Vector2(time, z));
            }

            /// <summary>
            /// One forearm's part of a pose. A pose that passes `default` says nothing, and in a clip that
            /// keys this forearm anywhere such a pose means the forearm as modelled (straight, length 1),
            /// the same way a pose that leaves out a leg means the leg at rest. So a clip that ends on a
            /// rest pose ends with its elbows open without saying so.
            /// </summary>
            public void KeyForearm(bool right, float time, Forearm pose)
            {
                int side = right ? 1 : 0;
                _fore[side].Add(pose.Keyed ? new Vector3(time, pose.Fold, pose.Stretch) : new Vector3(time, 0f, 1f));
                _foreKeyed[side].Add(pose.Keyed);
            }

            /// <summary>
            /// Writes one forearm's curves, or nothing. Nothing is the case for every clip that keys no
            /// forearm and for every rig without the bone, so those clips are the curves they always were.
            /// All three channels of the rotation and of the scale are written on the same key times,
            /// because a clip binds each as one vector (`GroundIntroduction` has the incident).
            /// </summary>
            private void BuildForearm(AnimationClip clip, int side)
            {
                if (_rig == null || !_foreKeyed[side].Contains(true)) return;
                var bone = side == 0 ? _rig.Left : _rig.Right;
                if (string.IsNullOrEmpty(bone.Path)) return;

                var fold = new List<Vector2>(); var flat = new List<Vector2>();
                var length = new List<Vector2>(); var thickY = new List<Vector2>(); var thickZ = new List<Vector2>();
                foreach (var key in _fore[side])
                {
                    fold.Add(new Vector2(key.x, key.y * bone.FoldSign));
                    flat.Add(new Vector2(key.x, 0f));
                    length.Add(new Vector2(key.x, bone.RestScale.x * Mathf.Max(0.01f, key.z)));
                    thickY.Add(new Vector2(key.x, bone.RestScale.y));
                    thickZ.Add(new Vector2(key.x, bone.RestScale.z));
                }
                clip.SetCurve(bone.Path, typeof(Transform), "localEulerAnglesRaw.x", Curve(flat));
                clip.SetCurve(bone.Path, typeof(Transform), "localEulerAnglesRaw.y", Curve(fold));
                clip.SetCurve(bone.Path, typeof(Transform), "localEulerAnglesRaw.z", Curve(flat));
                clip.SetCurve(bone.Path, typeof(Transform), "localScale.x", Curve(length));
                clip.SetCurve(bone.Path, typeof(Transform), "localScale.y", Curve(thickY));
                clip.SetCurve(bone.Path, typeof(Transform), "localScale.z", Curve(thickZ));
            }

            /// <summary>
            /// Mark an instant as the moment this ability LANDS.
            ///
            /// ⚠️ THE TIME MUST BE ONE THIS CLIP ALREADY KEYS, and it usually is: the impact pose
            /// is the extreme every clip below is built around. A time no curve has a key at is
            /// silently ignored rather than interpolated to, because inventing a key would move a
            /// pose somebody authored.
            /// </summary>
            public void PunchAt(float time) => _punches.Add(time);

            private float _holdAt = -1, _hold;

            /// <summary>
            /// ⚠️ HOLD THE CONTACT POSE, THEN RECOVER (2026-09-24, 🧑 *"the animation of all skill casting"*).
            /// Every key after `time` moves later by `seconds` and the pose at `time` is held flat across the gap:
            /// the research's first rule for a readable action
            /// (`docs/reports/gameplay-animation-2026-09-24/research-and-analysis.md` § 1). Used by Rafi's
            /// authored casts; the other heroes' shipping casts are the glb tables in `tools/author_hero_action.py`.
            /// </summary>
            public void HoldAt(float time, float seconds) { _holdAt = time; _hold = seconds; }

            private List<Vector2> WithHold(List<Vector2> keys)
            {
                if (_hold <= 0 || keys.Count < 2) return keys;
                var sorted = new List<Vector2>(keys); sorted.Sort((a, b) => a.x.CompareTo(b.x));
                float v = sorted[0].y;
                for (int i = 0; i < sorted.Count; i++)
                {
                    if (sorted[i].x <= _holdAt) v = sorted[i].y;
                    if (i + 1 < sorted.Count && sorted[i].x <= _holdAt && sorted[i + 1].x > _holdAt)
                        v = Mathf.Lerp(sorted[i].y, sorted[i + 1].y, Mathf.InverseLerp(sorted[i].x, sorted[i + 1].x, _holdAt));
                }
                var held = new List<Vector2>();
                foreach (var k in sorted) if (k.x < _holdAt - PunchEpsilon) held.Add(k);
                held.Add(new Vector2(_holdAt, v));
                held.Add(new Vector2(_holdAt + _hold, v));
                foreach (var k in sorted) if (k.x > _holdAt + PunchEpsilon) held.Add(new Vector2(k.x + _hold, k.y));
                return held;
            }

            private bool IsPunch(float time)
            {
                for (int i = 0; i < _punches.Count; i++)
                    if (Mathf.Abs(_punches[i] - time) <= PunchEpsilon) return true;

                return false;
            }

            /// <summary>
            /// Turn stored values into a curve whose tangents say where the weight is.
            ///
            /// ⚠️ THE BASELINE IS THE CATMULL-ROM SLOPE `AddKey` WOULD HAVE PRODUCED, so a clip
            /// with no `PunchAt` animates exactly as it did before this change. That is deliberate:
            /// the punch is opt-in per clip, and a clip nobody has re-timed must not silently
            /// change under whoever is looking at it next.
            /// </summary>
            private AnimationCurve Curve(List<Vector2> keys)
            {
                keys = WithHold(keys);
                if (keys.Count == 0) return new AnimationCurve();
                if (keys.Count == 1) return new AnimationCurve(new Keyframe(keys[0].x, keys[0].y));

                var frames = new Keyframe[keys.Count];

                for (int i = 0; i < keys.Count; i++)
                {
                    float slope = Slope(keys, i);
                    float inT = slope, outT = slope;

                    if (IsPunch(keys[i].x))
                    {
                        // Arrive accelerating, then stop dead. The stop is the hit.
                        inT = Segment(keys, i - 1, i) * PunchIn;
                        outT = 0.0f;
                    }
                    else if (i + 1 < keys.Count && IsPunch(keys[i + 1].x))
                    {
                        // The wind-up hangs before it goes, which is anticipation.
                        outT = Segment(keys, i, i + 1) * AnticipateOut;
                    }

                    frames[i] = new Keyframe(keys[i].x, keys[i].y, inT, outT);
                }

                return new AnimationCurve(frames);
            }

            /// <summary>The straight-line slope across one span, or zero if it has no width.</summary>
            private static float Segment(List<Vector2> keys, int from, int to)
            {
                if (from < 0 || to >= keys.Count) return 0.0f;

                float dt = keys[to].x - keys[from].x;
                return Mathf.Abs(dt) < 0.0001f ? 0.0f : (keys[to].y - keys[from].y) / dt;
            }

            /// <summary>What `AddKey` would have chosen: the slope through both neighbours.</summary>
            private static float Slope(List<Vector2> keys, int i)
            {
                if (i == 0) return Segment(keys, 0, 1);
                if (i == keys.Count - 1) return Segment(keys, i - 1, i);

                float dt = keys[i + 1].x - keys[i - 1].x;
                return Mathf.Abs(dt) < 0.0001f ? 0.0f : (keys[i + 1].y - keys[i - 1].y) / dt;
            }

            public AnimationClip Build(bool legacy = false, AnimationCurve[] rootPosition = null)
            {
                var clip = new AnimationClip
                {
                    name = _name,
                    legacy = legacy,
                    wrapMode = WrapMode.Once,
                };

                clip.SetCurve(_paths["root"], typeof(Transform), "localPosition.x", rootPosition != null ? rootPosition[0] : Curve(_rootX));
                clip.SetCurve(_paths["root"], typeof(Transform), "localPosition.y", rootPosition != null ? rootPosition[1] : Curve(_rootY));
                clip.SetCurve(_paths["root"], typeof(Transform), "localPosition.z", rootPosition != null ? rootPosition[2] : Curve(_rootZ));

                foreach (string bone in Bones)
                {
                    clip.SetCurve(_paths[bone], typeof(Transform), "localEulerAnglesRaw.x", Curve(_rot[bone][0]));
                    clip.SetCurve(_paths[bone], typeof(Transform), "localEulerAnglesRaw.y", Curve(_rot[bone][1]));
                    clip.SetCurve(_paths[bone], typeof(Transform), "localEulerAnglesRaw.z", Curve(_rot[bone][2]));
                }

                // The elbows last, and only if this clip keyed one on a rig that has it.
                BuildForearm(clip, 0);
                BuildForearm(clip, 1);
#if UNITY_EDITOR
                PunchTimes[_name] = _punches.ToArray();
#endif

                return clip;
            }
        }

        public static Dictionary<string, AnimationClip> BuildAll(Transform animatorRoot)
        {
            if (animatorRoot == null) return null;

            var paths = ResolvePaths(animatorRoot);
            if (paths == null) return null;

            var dict = new Dictionary<string, AnimationClip>();

            // SEAN
            dict["hero-sean-dash"] = BuildSeanDash(paths);
            dict["hero-sean-ignite"] = BuildSeanIgnite(paths);
            dict["hero-sean-supernova"] = BuildSeanSupernova(paths);

            // ZACK
            dict["hero-zack-sprint"] = BuildZackSprint(paths);
            dict["hero-zack-charge"] = BuildZackCharge(paths);
            dict["hero-zack-summon"] = BuildZackSummon(paths);

            // DANTE
            dict["hero-dante-stomp"] = BuildDanteStomp(paths);
            dict["hero-dante-roar"] = BuildDanteRoar(paths);
            dict["hero-dante-fissure"] = BuildDanteFissure(paths);

            // CHESKA
            dict["hero-cheska-frostwave"] = BuildCheskaFrostwave(paths);
            dict["hero-cheska-raise"] = BuildCheskaRaise(paths);
            dict["hero-cheska-nova"] = BuildCheskaNova(paths);

            // NEMU
            dict["hero-nemu-ghoststep"] = BuildNemuGhoststep(paths);
            dict["hero-nemu-project"] = BuildNemuProject(paths);
            dict["hero-nemu-seance"] = BuildNemuSeance(paths);

            // ⚠️⚠️ PHAISTER, THE SIXTH HERO, ARRIVED WITH NO CAST ANIMATION AT ALL.
            // `PhaisterHeroKit` names `hero-phaister-hex`, `hero-phaister-blink` and
            // `hero-phaister-eclipse` as its cast actions and this dictionary had no entry for
            // any of them, so all three powers fired with the body standing still. It fails
            // silently: a missing key is a lookup that returns nothing, not an error, which is
            // why a whole hero can ship animation-less with every test green.
            //
            // ⚠️ SHE IS SPIRIT, LIKE NEMU, AND THE TWO MUST NOT MOVE ALIKE. Nemu's fiction is
            // ABSENCE (she goes part-ghost and cannot be tagged); Phaister's is CASTING, which
            // is a thing done TO the world with the hands. So Nemu drifts and Phaister points.
            // Two heroes sharing an element is exactly where a kit starts reading as a reskin.
            dict["hero-phaister-hex"] = BuildPhaisterHex(paths);
            dict["hero-phaister-blink"] = BuildPhaisterBlink(paths);
            dict["hero-phaister-eclipse"] = BuildPhaisterEclipse(paths);
            dict["hero-phaister-swarm"] = BuildPhaisterSwarm(paths);
            dict["hero-phaister-manika"] = BuildPhaisterManika(paths);
            dict["hero-phaister-pin"] = BuildPhaisterPin(paths);
            dict["hero-phaister-omen"] = BuildPhaisterOmen(paths);
            dict["hero-phaister-drain"] = BuildPhaisterDrain(paths);
            dict["hero-phaister-wring"] = BuildPhaisterWring(paths);
            dict["hero-phaister-hexreach"] = BuildPhaisterHexReach(paths);
            dict["hero-phaister-hexstab"] = BuildPhaisterHexStab(paths);

            dict["hero-rafi-cut"] = BuildRafiCut(paths);
            dict["hero-rafi-skim"] = BuildRafiSkim(paths);
            dict["hero-rafi-wall"] = BuildRafiWall(paths);
            dict["hero-rafi-feint"] = BuildRafiFeint(paths);
            dict["hero-rafi-breakwater"] = BuildRafiBreakwater(paths);

            // AMIHAN (2026-09-25). Her direction is the spiral; see `HeroAbilityClips.Amihan.cs`.
            dict["hero-amihan-dash"] = BuildAmihanDash(paths);
            dict["hero-amihan-updraft"] = BuildAmihanUpdraft(paths);
            dict["hero-amihan-hover"] = BuildAmihanHover(paths);
            dict["hero-amihan-whirlwind"] = BuildAmihanWhirlwind(paths);
            dict["hero-amihan-storm"] = BuildAmihanStorm(paths);

            // PAETE (HERO-9). His direction is the bend and the snap-back; see `HeroAbilityClips.Paete.cs`.
            dict["hero-paete-vine"] = BuildPaeteVine(paths);
            dict["hero-paete-sprout"] = BuildPaeteSprout(paths);
            dict["hero-paete-command"] = BuildPaeteCommand(paths);
            dict["hero-paete-thorns"] = BuildPaeteThorns(paths);
            dict["hero-paete-sentry"] = BuildPaeteSentry(paths);
            // What any body does against his kit; a player loads the baked `RootedMotion` set instead.
            dict[RootedMotion.Struggle] = BuildRootedStruggle(paths);
            dict[RootedMotion.Heave] = BuildPlantHeave(paths);
            dict[RootedMotion.Breakout] = BuildRootBreakout(paths);
            dict[RootedMotion.Feared] = BuildFearedFlee(paths);
            return dict;
        }

        // ===================================================================
        // § PHAISTER CLIPS
        // ===================================================================

        private static AnimationClip BuildPhaisterHex(Dictionary<string, string> paths)
        {
            var b = new ClipBuilder("hero-phaister-hex", paths);
            // 0.55s Draw the sigil, then STAMP it into the ground.
            // ⚠️ The impact is the stamp, not the drawing. A hex is placed; the arm circling
            // above it is the wind-up and it should hang.
            b.PunchAt(0.34f);

            b.KeyPos(0.00f, 0, 0, 0);
            b.KeyPos(0.16f, 0, 0.03f, -0.02f);
            b.KeyPos(0.34f, 0, -0.06f, 0.05f);
            b.KeyPos(0.55f, 0, 0, 0);

            // Torso rises for the draw, then drops over the sigil.
            b.KeyRot("torso", 0.00f, 0, 0, 0);
            b.KeyRot("torso", 0.16f, -12.0f, -18.0f, 0);
            b.KeyRot("torso", 0.34f, 26.0f, 8.0f, 0);
            b.KeyRot("torso", 0.55f, 0, 0, 0);

            b.KeyRot("head", 0.00f, 0, 0, 0);
            b.KeyRot("head", 0.16f, -10.0f, -12.0f, 0);
            b.KeyRot("head", 0.34f, 24.0f, 6.0f, 0);
            b.KeyRot("head", 0.55f, 0, 0, 0);

            // The casting arm traces a circle overhead and drives down, palm to the floor.
            b.KeyRot("arm-right", 0.00f, 0, 0, -15.0f);
            b.KeyRot("arm-right", 0.16f, -120.0f, 30.0f, -50.0f);
            b.KeyRot("arm-right", 0.34f, 45.0f, -10.0f, -12.0f);
            b.KeyRot("arm-right", 0.55f, 0, 0, -15.0f);

            // The off hand braces across the body, which is what stops it reading as a wave.
            b.KeyRot("arm-left", 0.00f, 0, 0, 15.0f);
            b.KeyRot("arm-left", 0.16f, -30.0f, -20.0f, 48.0f);
            b.KeyRot("arm-left", 0.34f, -14.0f, -30.0f, 30.0f);
            b.KeyRot("arm-left", 0.55f, 0, 0, 15.0f);

            b.KeyRot("leg-left", 0.00f, 0, 0, 0);
            b.KeyRot("leg-left", 0.34f, -18.0f, 0, 6.0f);
            b.KeyRot("leg-left", 0.55f, 0, 0, 0);

            b.KeyRot("leg-right", 0.00f, 0, 0, 0);
            b.KeyRot("leg-right", 0.34f, 14.0f, 0, -6.0f);
            b.KeyRot("leg-right", 0.55f, 0, 0, 0);

            return b.Build();
        }

        private static AnimationClip BuildPhaisterBlink(Dictionary<string, string> paths)
        {
            var b = new ClipBuilder("hero-phaister-blink", paths);
            // 0.42s Collapse inward, then snap out of the far side.
            //
            // ⚠️⚠️ IT PUNCHES ON THE ARRIVAL AND NEMU'S GHOST STEP DOES NOT, WHICH IS THE WHOLE
            // DIFFERENCE BETWEEN THE TWO SPIRIT HEROES. Ghost Step is a state you enter and
            // drift in, so it has no frame where the world stops. A blink is INSTANTANEOUS: the
            // body is gone and then it is somewhere else, and the snap is the read.
            b.PunchAt(0.24f);

            b.KeyPos(0.00f, 0, 0, 0);
            b.KeyPos(0.12f, 0, -0.07f, -0.04f);
            b.KeyPos(0.24f, 0, 0.02f, 0.14f);
            b.KeyPos(0.42f, 0, 0, 0);

            // Folds down and forward, then whips upright on arrival.
            b.KeyRot("torso", 0.00f, 0, 0, 0);
            b.KeyRot("torso", 0.12f, 30.0f, 26.0f, 0);
            b.KeyRot("torso", 0.24f, -16.0f, -22.0f, 0);
            b.KeyRot("torso", 0.42f, 0, 0, 0);

            b.KeyRot("head", 0.00f, 0, 0, 0);
            b.KeyRot("head", 0.12f, 18.0f, 20.0f, 0);
            b.KeyRot("head", 0.24f, -20.0f, -16.0f, 0);
            b.KeyRot("head", 0.42f, 0, 0, 0);

            // Both arms pull IN to the chest and then flare, which is a dissolve rather than a
            // stride. A blink that swings its arms reads as a dash.
            b.KeyRot("arm-right", 0.00f, 0, 0, -15.0f);
            b.KeyRot("arm-right", 0.12f, -50.0f, -35.0f, -8.0f);
            b.KeyRot("arm-right", 0.24f, 20.0f, 25.0f, -56.0f);
            b.KeyRot("arm-right", 0.42f, 0, 0, -15.0f);

            b.KeyRot("arm-left", 0.00f, 0, 0, 15.0f);
            b.KeyRot("arm-left", 0.12f, -50.0f, 35.0f, 8.0f);
            b.KeyRot("arm-left", 0.24f, 20.0f, -25.0f, 56.0f);
            b.KeyRot("arm-left", 0.42f, 0, 0, 15.0f);

            b.KeyRot("leg-left", 0.00f, 0, 0, 0);
            b.KeyRot("leg-left", 0.12f, 22.0f, 0, 0);
            b.KeyRot("leg-left", 0.24f, -30.0f, 0, 0);
            b.KeyRot("leg-left", 0.42f, 0, 0, 0);

            b.KeyRot("leg-right", 0.00f, 0, 0, 0);
            b.KeyRot("leg-right", 0.12f, 22.0f, 0, 0);
            b.KeyRot("leg-right", 0.24f, -22.0f, 0, 0);
            b.KeyRot("leg-right", 0.42f, 0, 0, 0);

            return b.Build();
        }

        private static AnimationClip BuildPhaisterEclipse(Dictionary<string, string> paths)
        {
            var b = new ClipBuilder("hero-phaister-eclipse", paths);
            // 2.12s Raise both arms to the sky, HOLD, then throw the eclipse down over the court.
            //
            // ⚠️ THE HOLD IS THE UPPER BODY AND IT IS THE POINT OF AN ULTIMATE.
            // `Hero_Strike_Balance.md` § 4.3 asks for a wind-up so the payoff has a moment; this
            // is the longest anticipation of the six kits, which is what an arena-wide power
            // should cost to cast.
            b.PunchAt(1.55f);

            b.KeyPos(0.00f, 0, 0, 0);
            b.KeyPos(0.20f, 0, -0.08f, -0.03f);
            b.KeyPos(0.82f, 0, 0.12f, 0);
            b.KeyPos(1.55f, 0, -0.05f, 0.04f);
            b.KeyPos(2.12f, 0, 0, 0);

            // Arches back for the call, then folds forward over the release.
            b.KeyRot("torso", 0.00f, 0, 0, 0);
            b.KeyRot("torso", 0.20f, 14.0f, 0, 0);
            b.KeyRot("torso", 0.82f, -30.0f, 0, 0);
            b.KeyRot("torso", 1.55f, 22.0f, 0, 0);
            b.KeyRot("torso", 2.12f, 0, 0, 0);

            b.KeyRot("head", 0.00f, 0, 0, 0);
            b.KeyRot("head", 0.20f, 8.0f, 0, 0);
            b.KeyRot("head", 0.82f, -40.0f, 0, 0);
            b.KeyRot("head", 1.55f, 18.0f, 0, 0);
            b.KeyRot("head", 2.12f, 0, 0, 0);

            // Both arms go up together, which is the gesture that separates an ultimate from a
            // skill: a skill is one hand, a summons is two.
            b.KeyRot("arm-right", 0.00f, 0, 0, -15.0f);
            b.KeyRot("arm-right", 0.20f, 25.0f, 0, -30.0f);
            b.KeyRot("arm-right", 0.82f, -155.0f, 0, -28.0f);
            b.KeyRot("arm-right", 1.55f, -30.0f, 0, -60.0f);
            b.KeyRot("arm-right", 2.12f, 0, 0, -15.0f);

            b.KeyRot("arm-left", 0.00f, 0, 0, 15.0f);
            b.KeyRot("arm-left", 0.20f, 25.0f, 0, 30.0f);
            b.KeyRot("arm-left", 0.82f, -155.0f, 0, 28.0f);
            b.KeyRot("arm-left", 1.55f, -30.0f, 0, 60.0f);
            b.KeyRot("arm-left", 2.12f, 0, 0, 15.0f);

            b.KeyRot("leg-left", 0.00f, 0, 0, 0);
            b.KeyRot("leg-left", 0.82f, -10.0f, 0, 8.0f);
            b.KeyRot("leg-left", 1.55f, -24.0f, 0, 8.0f);
            b.KeyRot("leg-left", 2.12f, 0, 0, 0);

            b.KeyRot("leg-right", 0.00f, 0, 0, 0);
            b.KeyRot("leg-right", 0.82f, 10.0f, 0, -8.0f);
            b.KeyRot("leg-right", 1.55f, 20.0f, 0, -8.0f);
            b.KeyRot("leg-right", 2.12f, 0, 0, 0);

            return b.Build();
        }

        // ===================================================================
        // § SEAN CLIPS
        // ===================================================================

        private static AnimationClip BuildSeanDash(Dictionary<string, string> paths)
        {
            var b = new ClipBuilder("hero-sean-dash", paths);
            // ⚠️ THIS IS THE FRAME THE ABILITY LANDS ON, and `ClipBuilder.PunchAt` is what makes
            // the difference between a pose and a blow: the wind-up hangs, the last few degrees
            // are the fastest, and the body stops dead here instead of easing through. See the
            // § TIMING note above `ClipBuilder` for why all fifteen used to feel the same.
            // The dive. Everything lands the instant he commits to the line of fire.
            b.PunchAt(0.25f);
            // 0.55s Rocket Jet Charge
            b.KeyPos(0.00f, 0, 0, 0);
            b.KeyPos(0.10f, 0, -0.04f, -0.02f);
            b.KeyPos(0.25f, 0, -0.02f, 0.12f);
            b.KeyPos(0.42f, 0, -0.01f, 0.08f);
            b.KeyPos(0.55f, 0, 0, 0);

            // Torso forward dive
            b.KeyRot("torso", 0.00f, 0, 0, 0);
            b.KeyRot("torso", 0.10f, 25.0f, 0, 0);
            b.KeyRot("torso", 0.25f, 42.0f, 0, 0);
            b.KeyRot("torso", 0.42f, 28.0f, 0, 0);
            b.KeyRot("torso", 0.55f, 0, 0, 0);

            // Head looking forward through the rush
            b.KeyRot("head", 0.00f, 0, 0, 0);
            b.KeyRot("head", 0.10f, -15.0f, 0, 0);
            b.KeyRot("head", 0.25f, -32.0f, 0, 0);
            b.KeyRot("head", 0.42f, -18.0f, 0, 0);
            b.KeyRot("head", 0.55f, 0, 0, 0);

            // Arms swept back like jet wings
            b.KeyRot("arm-left", 0.00f, 0, 0, 15.0f);
            b.KeyRot("arm-left", 0.10f, -20.0f, 0, 25.0f);
            b.KeyRot("arm-left", 0.25f, 65.0f, -10.0f, 45.0f);
            b.KeyRot("arm-left", 0.42f, 40.0f, 0, 30.0f);
            b.KeyRot("arm-left", 0.55f, 0, 0, 15.0f);

            b.KeyRot("arm-right", 0.00f, 0, 0, -15.0f);
            b.KeyRot("arm-right", 0.10f, -20.0f, 0, -25.0f);
            b.KeyRot("arm-right", 0.25f, 65.0f, 10.0f, -45.0f);
            b.KeyRot("arm-right", 0.42f, 40.0f, 0, -30.0f);
            b.KeyRot("arm-right", 0.55f, 0, 0, -15.0f);

            // Legs driving
            b.KeyRot("leg-left", 0.00f, 0, 0, 0);
            b.KeyRot("leg-left", 0.25f, -35.0f, 0, 8.0f);
            b.KeyRot("leg-left", 0.55f, 0, 0, 0);

            b.KeyRot("leg-right", 0.00f, 0, 0, 0);
            b.KeyRot("leg-right", 0.25f, 30.0f, 0, -8.0f);
            b.KeyRot("leg-right", 0.55f, 0, 0, 0);

            return b.Build();
        }

        private static AnimationClip BuildSeanIgnite(Dictionary<string, string> paths)
        {
            var b = new ClipBuilder("hero-sean-ignite", paths);
            // ⚠️ THIS IS THE FRAME THE ABILITY LANDS ON, and `ClipBuilder.PunchAt` is what makes
            // the difference between a pose and a blow: the wind-up hangs, the last few degrees
            // are the fastest, and the body stops dead here instead of easing through. See the
            // § TIMING note above `ClipBuilder` for why all fifteen used to feel the same.
            // The fist comes forward. A loaded throw is a gesture that finishes.
            b.PunchAt(0.28f);
            // 0.45s Fiery Fist Clench Stance
            b.KeyPos(0.00f, 0, 0, 0);
            b.KeyPos(0.15f, 0, -0.02f, -0.01f);
            b.KeyPos(0.28f, 0, 0.03f, 0.02f);
            b.KeyPos(0.45f, 0, 0, 0);

            b.KeyRot("torso", 0.00f, 0, 0, 0);
            b.KeyRot("torso", 0.15f, -5.0f, 20.0f, -5.0f);
            b.KeyRot("torso", 0.28f, 12.0f, -10.0f, 5.0f);
            b.KeyRot("torso", 0.45f, 0, 0, 0);

            b.KeyRot("head", 0.00f, 0, 0, 0);
            b.KeyRot("head", 0.15f, 5.0f, -15.0f, 0);
            b.KeyRot("head", 0.28f, -8.0f, 8.0f, 0);
            b.KeyRot("head", 0.45f, 0, 0, 0);

            // Right arm raises, cocks, then ignites forward
            b.KeyRot("arm-right", 0.00f, 0, 0, -15.0f);
            b.KeyRot("arm-right", 0.15f, -40.0f, 25.0f, -35.0f);
            b.KeyRot("arm-right", 0.28f, -95.0f, -10.0f, -20.0f);
            b.KeyRot("arm-right", 0.45f, 0, 0, -15.0f);

            b.KeyRot("arm-left", 0.00f, 0, 0, 15.0f);
            b.KeyRot("arm-left", 0.15f, 20.0f, 0, 35.0f);
            b.KeyRot("arm-left", 0.28f, 10.0f, 0, 25.0f);
            b.KeyRot("arm-left", 0.45f, 0, 0, 15.0f);

            b.KeyRot("leg-left", 0.00f, 0, 0, 0);
            b.KeyRot("leg-left", 0.28f, -12.0f, 0, 5.0f);
            b.KeyRot("leg-left", 0.45f, 0, 0, 0);

            b.KeyRot("leg-right", 0.00f, 0, 0, 0);
            b.KeyRot("leg-right", 0.28f, 15.0f, 0, -5.0f);
            b.KeyRot("leg-right", 0.45f, 0, 0, 0);

            return b.Build();
        }

        private static AnimationClip BuildSeanSupernova(Dictionary<string, string> paths)
        {
            var b = new ClipBuilder("hero-sean-supernova", paths);
            // ⚠️ THIS IS THE FRAME THE ABILITY LANDS ON, and `ClipBuilder.PunchAt` is what makes
            // the difference between a pose and a blow: the wind-up hangs, the last few degrees
            // are the fastest, and the body stops dead here instead of easing through. See the
            // § TIMING note above `ClipBuilder` for why all fifteen used to feel the same.
            // The ground smash, not the leap. The leap is the wind-up and it should hang.
            b.PunchAt(0.65f);
            // 1.0s Leap -> Meteor Hold -> Ground Smash
            b.KeyPos(0.00f, 0, 0, 0);
            b.KeyPos(0.15f, 0, -0.06f, 0);
            b.KeyPos(0.35f, 0, 0.22f, 0.05f);
            b.KeyPos(0.52f, 0, 0.18f, 0.04f);
            b.KeyPos(0.65f, 0, -0.08f, 0.02f);
            b.KeyPos(0.85f, 0, -0.04f, 0.01f);
            b.KeyPos(1.00f, 0, 0, 0);

            // Torso arches back during hang, slams forward on impact
            b.KeyRot("torso", 0.00f, 0, 0, 0);
            b.KeyRot("torso", 0.15f, 20.0f, 0, 0);
            b.KeyRot("torso", 0.35f, -25.0f, 0, 0);
            b.KeyRot("torso", 0.52f, -15.0f, 0, 0);
            b.KeyRot("torso", 0.65f, 55.0f, 0, 0);
            b.KeyRot("torso", 0.85f, 30.0f, 0, 0);
            b.KeyRot("torso", 1.00f, 0, 0, 0);

            b.KeyRot("head", 0.00f, 0, 0, 0);
            b.KeyRot("head", 0.35f, -35.0f, 0, 0);
            b.KeyRot("head", 0.65f, 30.0f, 0, 0);
            b.KeyRot("head", 1.00f, 0, 0, 0);

            // Both arms overhead during hang, slammed down on ground
            b.KeyRot("arm-left", 0.00f, 0, 0, 15.0f);
            b.KeyRot("arm-left", 0.15f, 30.0f, 0, 20.0f);
            b.KeyRot("arm-left", 0.35f, -135.0f, 0, 55.0f);
            b.KeyRot("arm-left", 0.52f, -120.0f, 0, 50.0f);
            b.KeyRot("arm-left", 0.65f, 75.0f, 0, 25.0f);
            b.KeyRot("arm-left", 0.85f, 40.0f, 0, 20.0f);
            b.KeyRot("arm-left", 1.00f, 0, 0, 15.0f);

            b.KeyRot("arm-right", 0.00f, 0, 0, -15.0f);
            b.KeyRot("arm-right", 0.15f, 30.0f, 0, -20.0f);
            b.KeyRot("arm-right", 0.35f, -135.0f, 0, -55.0f);
            b.KeyRot("arm-right", 0.52f, -120.0f, 0, -50.0f);
            b.KeyRot("arm-right", 0.65f, 75.0f, 0, -25.0f);
            b.KeyRot("arm-right", 0.85f, 40.0f, 0, -20.0f);
            b.KeyRot("arm-right", 1.00f, 0, 0, -15.0f);

            b.KeyRot("leg-left", 0.00f, 0, 0, 0);
            b.KeyRot("leg-left", 0.35f, -25.0f, 0, 12.0f);
            b.KeyRot("leg-left", 0.65f, 20.0f, 0, 18.0f);
            b.KeyRot("leg-left", 1.00f, 0, 0, 0);

            b.KeyRot("leg-right", 0.00f, 0, 0, 0);
            b.KeyRot("leg-right", 0.35f, -25.0f, 0, -12.0f);
            b.KeyRot("leg-right", 0.65f, 20.0f, 0, -18.0f);
            b.KeyRot("leg-right", 1.00f, 0, 0, 0);

            return b.Build();
        }

        // ===================================================================
        // § ZACK CLIPS
        // ===================================================================

        private static AnimationClip BuildZackSprint(Dictionary<string, string> paths)
        {
            var b = new ClipBuilder("hero-zack-sprint", paths);
            // ⚠️⚠️ NO `PunchAt`, AND THE ABSENCE IS THE DESIGN. Bolt Sprint is LOCOMOTION,
            // not a strike: it is a skating cycle held for the whole dash, and there is no
            // instant at which anything lands. Snapping a cycle to a stop would read as the
            // animation breaking. The same goes for the vibration below.
            // 0.60s Aerodynamic Speed Skate Grind
            b.KeyPos(0.00f, 0, 0, 0);
            b.KeyPos(0.15f, -0.03f, -0.04f, 0.06f);
            b.KeyPos(0.30f, 0.03f, -0.03f, 0.09f);
            b.KeyPos(0.45f, -0.02f, -0.04f, 0.06f);
            b.KeyPos(0.60f, 0, 0, 0);

            b.KeyRot("torso", 0.00f, 0, 0, 0);
            b.KeyRot("torso", 0.15f, 32.0f, -12.0f, -14.0f);
            b.KeyRot("torso", 0.30f, 35.0f, 12.0f, 14.0f);
            b.KeyRot("torso", 0.45f, 30.0f, -8.0f, -10.0f);
            b.KeyRot("torso", 0.60f, 0, 0, 0);

            b.KeyRot("head", 0.00f, 0, 0, 0);
            b.KeyRot("head", 0.15f, -20.0f, 10.0f, 8.0f);
            b.KeyRot("head", 0.30f, -22.0f, -10.0f, -8.0f);
            b.KeyRot("head", 0.60f, 0, 0, 0);

            // Pumping arms
            b.KeyRot("arm-left", 0.00f, 0, 0, 15.0f);
            b.KeyRot("arm-left", 0.15f, -65.0f, 0, 30.0f);
            b.KeyRot("arm-left", 0.30f, 50.0f, 0, 25.0f);
            b.KeyRot("arm-left", 0.45f, -55.0f, 0, 30.0f);
            b.KeyRot("arm-left", 0.60f, 0, 0, 15.0f);

            b.KeyRot("arm-right", 0.00f, 0, 0, -15.0f);
            b.KeyRot("arm-right", 0.15f, 55.0f, 0, -25.0f);
            b.KeyRot("arm-right", 0.30f, -65.0f, 0, -30.0f);
            b.KeyRot("arm-right", 0.45f, 45.0f, 0, -25.0f);
            b.KeyRot("arm-right", 0.60f, 0, 0, -15.0f);

            b.KeyRot("leg-left", 0.00f, 0, 0, 0);
            b.KeyRot("leg-left", 0.15f, 25.0f, 0, 10.0f);
            b.KeyRot("leg-left", 0.30f, -30.0f, 0, 8.0f);
            b.KeyRot("leg-left", 0.60f, 0, 0, 0);

            b.KeyRot("leg-right", 0.00f, 0, 0, 0);
            b.KeyRot("leg-right", 0.15f, -30.0f, 0, -8.0f);
            b.KeyRot("leg-right", 0.30f, 25.0f, 0, -10.0f);
            b.KeyRot("leg-right", 0.60f, 0, 0, 0);

            return b.Build();
        }

        private static AnimationClip BuildZackCharge(Dictionary<string, string> paths)
        {
            var b = new ClipBuilder("hero-zack-charge", paths);
            // ⚠️ NO `PunchAt`. A high-frequency vibration is already all attack and no
            // settle; the shape of it is the buzz, and a punch would flatten every second
            // oscillation into a hold.
            // 0.40s High-frequency electric vibration
            for (int i = 0; i <= 8; i++)
            {
                float t = i * (0.40f / 8.0f);
                float vib = (i % 2 == 0 ? 1.0f : -1.0f) * (1.0f - Mathf.Abs(t - 0.20f) / 0.25f);

                b.KeyPos(t, vib * 0.015f, -0.02f * Mathf.Abs(vib), 0);
                b.KeyRot("torso", t, 10.0f + vib * 6.0f, vib * 8.0f, vib * 5.0f);
                b.KeyRot("head", t, -8.0f - vib * 4.0f, -vib * 6.0f, -vib * 4.0f);
                b.KeyRot("arm-right", t, -85.0f + vib * 12.0f, vib * 10.0f, -30.0f + vib * 8.0f);
                b.KeyRot("arm-left", t, 20.0f - vib * 8.0f, 0, 35.0f + vib * 6.0f);
                b.KeyRot("leg-left", t, 5.0f, 0, 5.0f);
                b.KeyRot("leg-right", t, -5.0f, 0, -5.0f);
            }

            return b.Build();
        }

        private static AnimationClip BuildZackSummon(Dictionary<string, string> paths)
        {
            var b = new ClipBuilder("hero-zack-summon", paths);
            // ⚠️ THIS IS THE FRAME THE ABILITY LANDS ON, and `ClipBuilder.PunchAt` is what makes
            // the difference between a pose and a blow: the wind-up hangs, the last few degrees
            // are the fastest, and the body stops dead here instead of easing through. See the
            // § TIMING note above `ClipBuilder` for why all fifteen used to feel the same.
            // The bolt comes DOWN. The raise at 0.28 is the call and it stays smooth.
            b.PunchAt(0.45f);
            // 0.75s Sky Lightning Summon
            b.KeyPos(0.00f, 0, 0, 0);
            b.KeyPos(0.12f, 0, -0.05f, 0);
            b.KeyPos(0.28f, 0, 0.10f, 0.02f);
            b.KeyPos(0.45f, 0, -0.06f, 0.01f);
            b.KeyPos(0.75f, 0, 0, 0);

            b.KeyRot("torso", 0.00f, 0, 0, 0);
            b.KeyRot("torso", 0.12f, 15.0f, 0, 0);
            b.KeyRot("torso", 0.28f, -32.0f, 0, 0);
            b.KeyRot("torso", 0.45f, 25.0f, 0, 0);
            b.KeyRot("torso", 0.75f, 0, 0, 0);

            b.KeyRot("head", 0.00f, 0, 0, 0);
            b.KeyRot("head", 0.12f, 10.0f, 0, 0);
            b.KeyRot("head", 0.28f, -45.0f, 0, 0);
            b.KeyRot("head", 0.45f, 15.0f, 0, 0);
            b.KeyRot("head", 0.75f, 0, 0, 0);

            // Skyward hands invoke thunder, then crash
            b.KeyRot("arm-left", 0.00f, 0, 0, 15.0f);
            b.KeyRot("arm-left", 0.12f, 20.0f, 0, 20.0f);
            b.KeyRot("arm-left", 0.28f, -155.0f, 0, 50.0f);
            b.KeyRot("arm-left", 0.45f, 45.0f, 0, 25.0f);
            b.KeyRot("arm-left", 0.75f, 0, 0, 15.0f);

            b.KeyRot("arm-right", 0.00f, 0, 0, -15.0f);
            b.KeyRot("arm-right", 0.12f, 20.0f, 0, -20.0f);
            b.KeyRot("arm-right", 0.28f, -155.0f, 0, -50.0f);
            b.KeyRot("arm-right", 0.45f, 45.0f, 0, -25.0f);
            b.KeyRot("arm-right", 0.75f, 0, 0, -15.0f);

            b.KeyRot("leg-left", 0.00f, 0, 0, 0);
            b.KeyRot("leg-left", 0.28f, -15.0f, 0, 8.0f);
            b.KeyRot("leg-left", 0.75f, 0, 0, 0);

            b.KeyRot("leg-right", 0.00f, 0, 0, 0);
            b.KeyRot("leg-right", 0.28f, -15.0f, 0, -8.0f);
            b.KeyRot("leg-right", 0.75f, 0, 0, 0);

            return b.Build();
        }

        // ===================================================================
        // § DANTE CLIPS
        // ===================================================================

        private static AnimationClip BuildDanteStomp(Dictionary<string, string> paths)
        {
            var b = new ClipBuilder("hero-dante-stomp", paths);
            // ⚠️ THIS IS THE FRAME THE ABILITY LANDS ON, and `ClipBuilder.PunchAt` is what makes
            // the difference between a pose and a blow: the wind-up hangs, the last few degrees
            // are the fastest, and the body stops dead here instead of easing through. See the
            // § TIMING note above `ClipBuilder` for why all fifteen used to feel the same.
            // The foot hits. This is the clearest impact in the game and it had none.
            b.PunchAt(0.30f);
            // 0.55s High-Knee Ground Stomp
            b.KeyPos(0.00f, 0, 0, 0);
            b.KeyPos(0.18f, -0.04f, 0.06f, -0.02f);
            b.KeyPos(0.30f, 0.01f, -0.08f, 0.03f);
            b.KeyPos(0.42f, 0, -0.03f, 0.01f);
            b.KeyPos(0.55f, 0, 0, 0);

            b.KeyRot("torso", 0.00f, 0, 0, 0);
            b.KeyRot("torso", 0.18f, -16.0f, 12.0f, -14.0f);
            b.KeyRot("torso", 0.30f, 36.0f, -8.0f, 8.0f);
            b.KeyRot("torso", 0.42f, 18.0f, 0, 0);
            b.KeyRot("torso", 0.55f, 0, 0, 0);

            b.KeyRot("head", 0.00f, 0, 0, 0);
            b.KeyRot("head", 0.18f, -12.0f, -8.0f, 0);
            b.KeyRot("head", 0.30f, 22.0f, 4.0f, 0);
            b.KeyRot("head", 0.55f, 0, 0, 0);

            // Fists raised then smashed down
            b.KeyRot("arm-left", 0.00f, 0, 0, 15.0f);
            b.KeyRot("arm-left", 0.18f, -50.0f, 0, 40.0f);
            b.KeyRot("arm-left", 0.30f, 45.0f, 0, 20.0f);
            b.KeyRot("arm-left", 0.55f, 0, 0, 15.0f);

            b.KeyRot("arm-right", 0.00f, 0, 0, -15.0f);
            b.KeyRot("arm-right", 0.18f, -60.0f, 0, -45.0f);
            b.KeyRot("arm-right", 0.30f, 50.0f, 0, -20.0f);
            b.KeyRot("arm-right", 0.55f, 0, 0, -15.0f);

            // Right leg high lift -> stomp
            b.KeyRot("leg-right", 0.00f, 0, 0, 0);
            b.KeyRot("leg-right", 0.18f, -68.0f, 0, -10.0f);
            b.KeyRot("leg-right", 0.30f, 12.0f, 0, -4.0f);
            b.KeyRot("leg-right", 0.55f, 0, 0, 0);

            b.KeyRot("leg-left", 0.00f, 0, 0, 0);
            b.KeyRot("leg-left", 0.18f, 12.0f, 0, 8.0f);
            b.KeyRot("leg-left", 0.30f, -8.0f, 0, 6.0f);
            b.KeyRot("leg-left", 0.55f, 0, 0, 0);

            return b.Build();
        }

        private static AnimationClip BuildDanteRoar(Dictionary<string, string> paths)
        {
            var b = new ClipBuilder("hero-dante-roar", paths);
            // ⚠️ THIS IS THE FRAME THE ABILITY LANDS ON, and `ClipBuilder.PunchAt` is what makes
            // the difference between a pose and a blow: the wind-up hangs, the last few degrees
            // are the fastest, and the body stops dead here instead of easing through. See the
            // § TIMING note above `ClipBuilder` for why all fifteen used to feel the same.
            // The flex. Carapace is armour going on, so it sets rather than swells.
            b.PunchAt(0.32f);
            // 0.65s Carapace Armor Roar Flex
            b.KeyPos(0.00f, 0, 0, 0);
            b.KeyPos(0.15f, 0, -0.04f, -0.01f);
            b.KeyPos(0.32f, 0, 0.06f, 0.01f);
            b.KeyPos(0.50f, 0, 0.02f, 0);
            b.KeyPos(0.65f, 0, 0, 0);

            b.KeyRot("torso", 0.00f, 0, 0, 0);
            b.KeyRot("torso", 0.15f, 18.0f, 0, 0);
            b.KeyRot("torso", 0.32f, -30.0f, 0, 0);
            b.KeyRot("torso", 0.50f, -12.0f, 0, 0);
            b.KeyRot("torso", 0.65f, 0, 0, 0);

            b.KeyRot("head", 0.00f, 0, 0, 0);
            b.KeyRot("head", 0.15f, 12.0f, 0, 0);
            b.KeyRot("head", 0.32f, -38.0f, 0, 0);
            b.KeyRot("head", 0.50f, -15.0f, 0, 0);
            b.KeyRot("head", 0.65f, 0, 0, 0);

            // Wide iron flex
            b.KeyRot("arm-left", 0.00f, 0, 0, 15.0f);
            b.KeyRot("arm-left", 0.15f, -20.0f, 0, 25.0f);
            b.KeyRot("arm-left", 0.32f, -30.0f, 0, 88.0f);
            b.KeyRot("arm-left", 0.50f, -15.0f, 0, 60.0f);
            b.KeyRot("arm-left", 0.65f, 0, 0, 15.0f);

            b.KeyRot("arm-right", 0.00f, 0, 0, -15.0f);
            b.KeyRot("arm-right", 0.15f, -20.0f, 0, -25.0f);
            b.KeyRot("arm-right", 0.32f, -30.0f, 0, -88.0f);
            b.KeyRot("arm-right", 0.50f, -15.0f, 0, -60.0f);
            b.KeyRot("arm-right", 0.65f, 0, 0, -15.0f);

            b.KeyRot("leg-left", 0.00f, 0, 0, 0);
            b.KeyRot("leg-left", 0.32f, -10.0f, 0, 12.0f);
            b.KeyRot("leg-left", 0.65f, 0, 0, 0);

            b.KeyRot("leg-right", 0.00f, 0, 0, 0);
            b.KeyRot("leg-right", 0.32f, -10.0f, 0, -12.0f);
            b.KeyRot("leg-right", 0.65f, 0, 0, 0);

            return b.Build();
        }

        private static AnimationClip BuildDanteFissure(Dictionary<string, string> paths)
        {
            var b = new ClipBuilder("hero-dante-fissure", paths);
            // ⚠️ THIS IS THE FRAME THE ABILITY LANDS ON, and `ClipBuilder.PunchAt` is what makes
            // the difference between a pose and a blow: the wind-up hangs, the last few degrees
            // are the fastest, and the body stops dead here instead of easing through. See the
            // § TIMING note above `ClipBuilder` for why all fifteen used to feel the same.
            // The slam that splits the court. The 0.22 lift is the raise before it.
            b.PunchAt(0.40f);
            // 0.85s Titan Earthbreaker Double Slam
            b.KeyPos(0.00f, 0, 0, 0);
            b.KeyPos(0.22f, 0, 0.08f, -0.03f);
            b.KeyPos(0.40f, 0, -0.09f, 0.04f);
            b.KeyPos(0.60f, 0, -0.06f, 0.03f);
            b.KeyPos(0.85f, 0, 0, 0);

            b.KeyRot("torso", 0.00f, 0, 0, 0);
            b.KeyRot("torso", 0.22f, -35.0f, 0, 0);
            b.KeyRot("torso", 0.40f, 52.0f, 0, 0);
            b.KeyRot("torso", 0.60f, 38.0f, 0, 0);
            b.KeyRot("torso", 0.85f, 0, 0, 0);

            b.KeyRot("head", 0.00f, 0, 0, 0);
            b.KeyRot("head", 0.22f, -28.0f, 0, 0);
            b.KeyRot("head", 0.40f, 32.0f, 0, 0);
            b.KeyRot("head", 0.60f, 20.0f, 0, 0);
            b.KeyRot("head", 0.85f, 0, 0, 0);

            // Overhead double fists smashing earth
            b.KeyRot("arm-left", 0.00f, 0, 0, 15.0f);
            b.KeyRot("arm-left", 0.22f, -145.0f, 0, 35.0f);
            b.KeyRot("arm-left", 0.40f, 78.0f, 0, 18.0f);
            b.KeyRot("arm-left", 0.60f, 60.0f, 0, 15.0f);
            b.KeyRot("arm-left", 0.85f, 0, 0, 15.0f);

            b.KeyRot("arm-right", 0.00f, 0, 0, -15.0f);
            b.KeyRot("arm-right", 0.22f, -145.0f, 0, -35.0f);
            b.KeyRot("arm-right", 0.40f, 78.0f, 0, -18.0f);
            b.KeyRot("arm-right", 0.60f, 60.0f, 0, -15.0f);
            b.KeyRot("arm-right", 0.85f, 0, 0, -15.0f);

            b.KeyRot("leg-left", 0.00f, 0, 0, 0);
            b.KeyRot("leg-left", 0.40f, 15.0f, 0, 12.0f);
            b.KeyRot("leg-left", 0.85f, 0, 0, 0);

            b.KeyRot("leg-right", 0.00f, 0, 0, 0);
            b.KeyRot("leg-right", 0.40f, 15.0f, 0, -12.0f);
            b.KeyRot("leg-right", 0.85f, 0, 0, 0);

            return b.Build();
        }

        // ===================================================================
        // § CHESKA CLIPS
        // ===================================================================

        private static AnimationClip BuildCheskaFrostwave(Dictionary<string, string> paths)
        {
            var b = new ClipBuilder("hero-cheska-frostwave", paths);
            // ⚠️ THIS IS THE FRAME THE ABILITY LANDS ON, and `ClipBuilder.PunchAt` is what makes
            // the difference between a pose and a blow: the wind-up hangs, the last few degrees
            // are the fastest, and the body stops dead here instead of easing through. See the
            // § TIMING note above `ClipBuilder` for why all fifteen used to feel the same.
            // The sweep arrives. Lighter than Dante's by its own key spacing, not by its curve.
            b.PunchAt(0.28f);
            // 0.50s Graceful Frost Sweep Wave
            b.KeyPos(0.00f, 0, 0, 0);
            b.KeyPos(0.14f, 0.02f, -0.02f, 0);
            b.KeyPos(0.28f, -0.02f, -0.03f, 0.04f);
            b.KeyPos(0.50f, 0, 0, 0);

            b.KeyRot("torso", 0.00f, 0, 0, 0);
            b.KeyRot("torso", 0.14f, 8.0f, 22.0f, 6.0f);
            b.KeyRot("torso", 0.28f, 24.0f, -28.0f, -10.0f);
            b.KeyRot("torso", 0.50f, 0, 0, 0);

            b.KeyRot("head", 0.00f, 0, 0, 0);
            b.KeyRot("head", 0.14f, -5.0f, -12.0f, 0);
            b.KeyRot("head", 0.28f, 15.0f, 18.0f, 0);
            b.KeyRot("head", 0.50f, 0, 0, 0);

            // Right arm sweeping downward arc
            b.KeyRot("arm-right", 0.00f, 0, 0, -15.0f);
            b.KeyRot("arm-right", 0.14f, -45.0f, 20.0f, -48.0f);
            b.KeyRot("arm-right", 0.28f, 35.0f, -25.0f, 15.0f);
            b.KeyRot("arm-right", 0.50f, 0, 0, -15.0f);

            b.KeyRot("arm-left", 0.00f, 0, 0, 15.0f);
            b.KeyRot("arm-left", 0.14f, 15.0f, 0, 35.0f);
            b.KeyRot("arm-left", 0.28f, -25.0f, 0, 45.0f);
            b.KeyRot("arm-left", 0.50f, 0, 0, 15.0f);

            b.KeyRot("leg-left", 0.00f, 0, 0, 0);
            b.KeyRot("leg-left", 0.28f, -15.0f, 0, 8.0f);
            b.KeyRot("leg-left", 0.50f, 0, 0, 0);

            b.KeyRot("leg-right", 0.00f, 0, 0, 0);
            b.KeyRot("leg-right", 0.28f, 18.0f, 0, -6.0f);
            b.KeyRot("leg-right", 0.50f, 0, 0, 0);

            return b.Build();
        }

        private static AnimationClip BuildCheskaRaise(Dictionary<string, string> paths)
        {
            var b = new ClipBuilder("hero-cheska-raise", paths);
            // ⚠️ THIS IS THE FRAME THE ABILITY LANDS ON, and `ClipBuilder.PunchAt` is what makes
            // the difference between a pose and a blow: the wind-up hangs, the last few degrees
            // are the fastest, and the body stops dead here instead of easing through. See the
            // § TIMING note above `ClipBuilder` for why all fifteen used to feel the same.
            // The pillars lock. Ice is the one element that STOPS, so it should stop.
            b.PunchAt(0.30f);
            // 0.55s Glacial Barricade Conjuring Raise
            b.KeyPos(0.00f, 0, 0, 0);
            b.KeyPos(0.15f, 0, -0.05f, 0);
            b.KeyPos(0.30f, 0, 0.05f, 0.02f);
            b.KeyPos(0.55f, 0, 0, 0);

            b.KeyRot("torso", 0.00f, 0, 0, 0);
            b.KeyRot("torso", 0.15f, 26.0f, 0, 0);
            b.KeyRot("torso", 0.30f, -14.0f, 0, 0);
            b.KeyRot("torso", 0.55f, 0, 0, 0);

            b.KeyRot("head", 0.00f, 0, 0, 0);
            b.KeyRot("head", 0.15f, 15.0f, 0, 0);
            b.KeyRot("head", 0.30f, -10.0f, 0, 0);
            b.KeyRot("head", 0.55f, 0, 0, 0);

            // Upward palm thrust raising pillars
            b.KeyRot("arm-left", 0.00f, 0, 0, 15.0f);
            b.KeyRot("arm-left", 0.15f, 40.0f, 0, 20.0f);
            b.KeyRot("arm-left", 0.30f, -95.0f, 0, 35.0f);
            b.KeyRot("arm-left", 0.55f, 0, 0, 15.0f);

            b.KeyRot("arm-right", 0.00f, 0, 0, -15.0f);
            b.KeyRot("arm-right", 0.15f, 40.0f, 0, -20.0f);
            b.KeyRot("arm-right", 0.30f, -95.0f, 0, -35.0f);
            b.KeyRot("arm-right", 0.55f, 0, 0, -15.0f);

            b.KeyRot("leg-left", 0.00f, 0, 0, 0);
            b.KeyRot("leg-left", 0.30f, -10.0f, 0, 6.0f);
            b.KeyRot("leg-left", 0.55f, 0, 0, 0);

            b.KeyRot("leg-right", 0.00f, 0, 0, 0);
            b.KeyRot("leg-right", 0.30f, -10.0f, 0, -6.0f);
            b.KeyRot("leg-right", 0.55f, 0, 0, 0);

            return b.Build();
        }

        private static AnimationClip BuildCheskaNova(Dictionary<string, string> paths)
        {
            var b = new ClipBuilder("hero-cheska-nova", paths);
            // ⚠️ THIS IS THE FRAME THE ABILITY LANDS ON, and `ClipBuilder.PunchAt` is what makes
            // the difference between a pose and a blow: the wind-up hangs, the last few degrees
            // are the fastest, and the body stops dead here instead of easing through. See the
            // § TIMING note above `ClipBuilder` for why all fifteen used to feel the same.
            // The blast leaves her.
            b.PunchAt(0.32f);
            // 0.70s Radial Frost Nova Blast
            b.KeyPos(0.00f, 0, 0, 0);
            b.KeyPos(0.16f, 0, -0.04f, 0);
            b.KeyPos(0.32f, 0, 0.06f, 0);
            b.KeyPos(0.50f, 0, 0.02f, 0);
            b.KeyPos(0.70f, 0, 0, 0);

            b.KeyRot("torso", 0.00f, 0, 0, 0);
            b.KeyRot("torso", 0.16f, 15.0f, 0, 0);
            b.KeyRot("torso", 0.32f, -22.0f, 0, 0);
            b.KeyRot("torso", 0.50f, -8.0f, 0, 0);
            b.KeyRot("torso", 0.70f, 0, 0, 0);

            b.KeyRot("head", 0.00f, 0, 0, 0);
            b.KeyRot("head", 0.16f, 10.0f, 0, 0);
            b.KeyRot("head", 0.32f, -25.0f, 0, 0);
            b.KeyRot("head", 0.70f, 0, 0, 0);

            // Inward compression then explosive outward burst
            b.KeyRot("arm-left", 0.00f, 0, 0, 15.0f);
            b.KeyRot("arm-left", 0.16f, -20.0f, 25.0f, 10.0f);
            b.KeyRot("arm-left", 0.32f, 0.0f, 0, 110.0f);
            b.KeyRot("arm-left", 0.50f, 0.0f, 0, 70.0f);
            b.KeyRot("arm-left", 0.70f, 0, 0, 15.0f);

            b.KeyRot("arm-right", 0.00f, 0, 0, -15.0f);
            b.KeyRot("arm-right", 0.16f, -20.0f, -25.0f, -10.0f);
            b.KeyRot("arm-right", 0.32f, 0.0f, 0, -110.0f);
            b.KeyRot("arm-right", 0.50f, 0.0f, 0, -70.0f);
            b.KeyRot("arm-right", 0.70f, 0, 0, -15.0f);

            b.KeyRot("leg-left", 0.00f, 0, 0, 0);
            b.KeyRot("leg-left", 0.32f, -10.0f, 0, 12.0f);
            b.KeyRot("leg-left", 0.70f, 0, 0, 0);

            b.KeyRot("leg-right", 0.00f, 0, 0, 0);
            b.KeyRot("leg-right", 0.32f, -10.0f, 0, -12.0f);
            b.KeyRot("leg-right", 0.70f, 0, 0, 0);

            return b.Build();
        }

        // ===================================================================
        // § NEMU CLIPS
        // ===================================================================

        private static AnimationClip BuildNemuGhoststep(Dictionary<string, string> paths)
        {
            var b = new ClipBuilder("hero-nemu-ghoststep", paths);
            // ⚠️⚠️ NO `PunchAt`, AND THIS ONE IS A CHARACTER DECISION RATHER THAN A TECHNICAL
            // ONE. Nemu going part-ghost is the single power in the game that should have NO
            // weight: she is untaggable while it runs, and the whole read is that the body stops
            // being a body. Every other hero gets a frame where the world stops. Hers does not,
            // and that is what makes it hers.
            // 0.50s Ethereal Spirit Glide
            b.KeyPos(0.00f, 0, 0, 0);
            b.KeyPos(0.15f, 0, 0.06f, 0.05f);
            b.KeyPos(0.35f, 0, 0.08f, 0.08f);
            b.KeyPos(0.50f, 0, 0, 0);

            b.KeyRot("torso", 0.00f, 0, 0, 0);
            b.KeyRot("torso", 0.25f, 14.0f, 0, -6.0f);
            b.KeyRot("torso", 0.50f, 0, 0, 0);

            b.KeyRot("head", 0.00f, 0, 0, 0);
            b.KeyRot("head", 0.25f, -10.0f, 0, 6.0f);
            b.KeyRot("head", 0.50f, 0, 0, 0);

            // Floating weightless arms
            b.KeyRot("arm-left", 0.00f, 0, 0, 15.0f);
            b.KeyRot("arm-left", 0.25f, -15.0f, 0, 42.0f);
            b.KeyRot("arm-left", 0.50f, 0, 0, 15.0f);

            b.KeyRot("arm-right", 0.00f, 0, 0, -15.0f);
            b.KeyRot("arm-right", 0.25f, -15.0f, 0, -42.0f);
            b.KeyRot("arm-right", 0.50f, 0, 0, -15.0f);

            b.KeyRot("leg-left", 0.00f, 0, 0, 0);
            b.KeyRot("leg-left", 0.25f, 16.0f, 0, 6.0f);
            b.KeyRot("leg-left", 0.50f, 0, 0, 0);

            b.KeyRot("leg-right", 0.00f, 0, 0, 0);
            b.KeyRot("leg-right", 0.25f, -12.0f, 0, -6.0f);
            b.KeyRot("leg-right", 0.50f, 0, 0, 0);

            return b.Build();
        }

        private static AnimationClip BuildNemuProject(Dictionary<string, string> paths)
        {
            var b = new ClipBuilder("hero-nemu-project", paths);
            // ⚠️ THIS IS THE FRAME THE ABILITY LANDS ON, and `ClipBuilder.PunchAt` is what makes
            // the difference between a pose and a blow: the wind-up hangs, the last few degrees
            // are the fastest, and the body stops dead here instead of easing through. See the
            // § TIMING note above `ClipBuilder` for why all fifteen used to feel the same.
            // Kuro is released. Everything else about Nemu is soft; the cast is not.
            b.PunchAt(0.26f);
            // 0.50s Astral Projection Cast
            b.KeyPos(0.00f, 0, 0, 0);
            b.KeyPos(0.14f, 0, -0.02f, -0.01f);
            b.KeyPos(0.26f, 0, 0.02f, 0.04f);
            b.KeyPos(0.50f, 0, 0, 0);

            b.KeyRot("torso", 0.00f, 0, 0, 0);
            b.KeyRot("torso", 0.14f, -6.0f, 15.0f, 0);
            b.KeyRot("torso", 0.26f, 16.0f, -15.0f, 0);
            b.KeyRot("torso", 0.50f, 0, 0, 0);

            b.KeyRot("head", 0.00f, 0, 0, 0);
            b.KeyRot("head", 0.14f, 6.0f, -10.0f, 0);
            b.KeyRot("head", 0.26f, -10.0f, 10.0f, 0);
            b.KeyRot("head", 0.50f, 0, 0, 0);

            // Right hand straight forward palm push
            b.KeyRot("arm-right", 0.00f, 0, 0, -15.0f);
            b.KeyRot("arm-right", 0.14f, -30.0f, 15.0f, -30.0f);
            b.KeyRot("arm-right", 0.26f, -90.0f, 0, -18.0f);
            b.KeyRot("arm-right", 0.50f, 0, 0, -15.0f);

            b.KeyRot("arm-left", 0.00f, 0, 0, 15.0f);
            b.KeyRot("arm-left", 0.14f, 25.0f, 0, 35.0f);
            b.KeyRot("arm-left", 0.26f, 10.0f, 0, 45.0f);
            b.KeyRot("arm-left", 0.50f, 0, 0, 15.0f);

            b.KeyRot("leg-left", 0.00f, 0, 0, 0);
            b.KeyRot("leg-left", 0.26f, -14.0f, 0, 5.0f);
            b.KeyRot("leg-left", 0.50f, 0, 0, 0);

            b.KeyRot("leg-right", 0.00f, 0, 0, 0);
            b.KeyRot("leg-right", 0.26f, 16.0f, 0, -5.0f);
            b.KeyRot("leg-right", 0.50f, 0, 0, 0);

            return b.Build();
        }

        private static AnimationClip BuildNemuSeance(Dictionary<string, string> paths)
        {
            var b = new ClipBuilder("hero-nemu-seance", paths);
            // ⚠️ THIS IS THE FRAME THE ABILITY LANDS ON, and `ClipBuilder.PunchAt` is what makes
            // the difference between a pose and a blow: the wind-up hangs, the last few degrees
            // are the fastest, and the body stops dead here instead of easing through. See the
            // § TIMING note above `ClipBuilder` for why all fifteen used to feel the same.
            // The vortex opens.
            b.PunchAt(0.38f);
            // 0.80s Dark Ritual Seance Vortex Invocation
            b.KeyPos(0.00f, 0, 0, 0);
            b.KeyPos(0.18f, 0, 0.08f, 0);
            b.KeyPos(0.38f, 0, 0.10f, 0.03f);
            b.KeyPos(0.58f, 0, 0.07f, 0.02f);
            b.KeyPos(0.80f, 0, 0, 0);

            b.KeyRot("torso", 0.00f, 0, 0, 0);
            b.KeyRot("torso", 0.18f, -15.0f, 0, 0);
            b.KeyRot("torso", 0.38f, 8.0f, 15.0f, 8.0f);
            b.KeyRot("torso", 0.58f, 12.0f, -15.0f, -8.0f);
            b.KeyRot("torso", 0.80f, 0, 0, 0);

            b.KeyRot("head", 0.00f, 0, 0, 0);
            b.KeyRot("head", 0.18f, -20.0f, 0, 0);
            b.KeyRot("head", 0.38f, 18.0f, -10.0f, 0);
            b.KeyRot("head", 0.58f, 18.0f, 10.0f, 0);
            b.KeyRot("head", 0.80f, 0, 0, 0);

            // Channelling ritual hands in circular arc
            b.KeyRot("arm-left", 0.00f, 0, 0, 15.0f);
            b.KeyRot("arm-left", 0.18f, -60.0f, 0, 45.0f);
            b.KeyRot("arm-left", 0.38f, -85.0f, 25.0f, 40.0f);
            b.KeyRot("arm-left", 0.58f, -80.0f, -15.0f, 35.0f);
            b.KeyRot("arm-left", 0.80f, 0, 0, 15.0f);

            b.KeyRot("arm-right", 0.00f, 0, 0, -15.0f);
            b.KeyRot("arm-right", 0.18f, -60.0f, 0, -45.0f);
            b.KeyRot("arm-right", 0.38f, -85.0f, -25.0f, -40.0f);
            b.KeyRot("arm-right", 0.58f, -80.0f, 15.0f, -35.0f);
            b.KeyRot("arm-right", 0.80f, 0, 0, -15.0f);

            b.KeyRot("leg-left", 0.00f, 0, 0, 0);
            b.KeyRot("leg-left", 0.38f, 10.0f, 0, 8.0f);
            b.KeyRot("leg-left", 0.80f, 0, 0, 0);

            b.KeyRot("leg-right", 0.00f, 0, 0, 0);
            b.KeyRot("leg-right", 0.38f, -10.0f, 0, -8.0f);
            b.KeyRot("leg-right", 0.80f, 0, 0, 0);

            return b.Build();
        }
    }
}
