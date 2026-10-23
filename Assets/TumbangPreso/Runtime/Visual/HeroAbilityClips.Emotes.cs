using System.Collections.Generic;
using UnityEngine;

namespace TumbangPreso.Visual
{
    /// <summary>
    /// The emote wheel's clips by name, for the animator's chains (`CharacterAnimator.EmoteClips`) and the bake
    /// (`EmoteMotionAuthor`). A rig that carries them plays these; one that does not falls back down its chain to the
    /// pack's old clip, exactly as before.
    /// </summary>
    public static class EmoteMotion
    {
        public const string Cheer = "emote-cheer", NoWay = "emote-noway", TooEasy = "emote-tooeasy", Laugh = "emote-laugh",
            Dance = "emote-streetdance", ComeOn = "emote-comeon", GoodGame = "emote-goodgame";

        public static readonly string[] All = { Cheer, NoWay, TooEasy, Laugh, Dance, ComeOn, GoodGame };

        /// <summary>The first authored set's clips (2026-10-23 morning), taken off every roster entry by the bake.</summary>
        public static readonly string[] Retired =
            { "emote-nod", "emote-nope", "emote-sitdown", "emote-victory", "emote-groove", "emote-tpose", "emote-bow" };
    }

    /// <summary>
    /// THE EMOTES, SECOND SET (owner, 2026-10-23: "thoroughly research emotes in games like fortnite, pubg, cod and
    /// etc and give our game completely new emotes"; "make sure the animation looks good for each character kasi iba
    /// iba size nila"). The research (docs/reports/emotes-2026-10-23/README.md) says the emotes people actually use
    /// are short, read in silhouette from across a map and carry one feeling each: Fortnite's cheer and taunt
    /// gestures and its dances, PUBG's ROFL and bow of respect, CoD's killcam taunts. This game's own bar adds the
    /// owner's "show emotions excited sad etc and laughing at taya". So the wheel is now one emotion per slice:
    ///
    ///   LET'S GO   (yes)     1.30 s loop   Excited. A crouch, a jump with both fists punched up, a pump down to the
    ///                                      ears and a second jump, then a single fist pumped.
    ///   NO WAY     (no)      1.60 s loop   Disbelief. Both hands clamped on the head, elbows out, the head shaking,
    ///                                      a sag, then both arms thrown out with a look at the sky: "why?!"
    ///   TOO EASY   (sit)     1.74 s hold   Smug. A drop to the court, leaning back on one elbow with the other hand
    ///                                      behind the head, legs out and crossed, a slow look round.
    ///   HA HA      (crouch)  1.40 s loop   Laughing at someone: a point, then doubled over, then the head thrown
    ///                                      back, a hand slapping the knee.
    ///   DANCE      (dance)   1.92 s loop   A street dance: two low, loose-armed bouncing steps, then both arms up
    ///                                      waving side to side, a bounce on every beat.
    ///   COME ON    (tpose)   1.20 s loop   A taunt. Chest out, chin up, both hands beckoning twice, then the arms
    ///                                      spread wide with a boxer's bounce: "bring it".
    ///   GG         (bow)     2.30 s hold   Respect. A fist pressed into the other palm, a deep bow, then upright
    ///                                      with the salute held and a nod.
    ///
    /// ⚠️ THE WIRE IDS ARE THE OLD ONES. `Emotes.All` crosses the network by id, so `crouch` is HA HA and `tpose` is
    /// COME ON; only the words and the clips changed.
    ///
    /// ⚠️ FITTED PER RIG (`EmoteFit`). The five heroes' arms differ a lot against their heads, so a pose keyed for
    /// one leaves a short-armed hero's fist level with the hat and their hand short of the face. Each rig's arm reach
    /// is measured off its own T-posed bind and the fitted poses push the lean, the arch and the head to meet it.
    ///
    /// Grounded like the casts (`GroundIntroduction`): the lowest vertex stays on the court every frame, and the
    /// jumps and bounces are lifts over that.
    /// </summary>
    public static partial class HeroAbilityClips
    {
#if UNITY_EDITOR
        public static AnimationClip[] BuildEmotes(Transform root)
        {
            var paths = ResolvePaths(root);
            if (paths == null) throw new System.InvalidOperationException("The rig is missing a required bone for the emotes.");
            var fit = EmoteFit.Measure(root, paths);
            Debug.Log("[EmoteFit] " + root.name + ": " + fit);
            return new[]
            {
                Grounded(EmoteCheer(paths, fit), root, paths,
                    t => 0.16f * Bump(t, 0.08f, 0.36f) + 0.11f * Bump(t, 0.52f, 0.78f)),
                Grounded(EmoteNoWay(paths, fit), root, paths, t => 0.04f * Bump(t, 1.00f, 1.24f)),
                Grounded(EmoteTooEasy(paths, fit), root, paths, t => 0.05f * Bump(t, 0.04f, 0.28f)),
                Grounded(EmoteLaugh(paths, fit), root, paths, t => 0.025f * Mathf.Abs(Mathf.Sin(Mathf.PI * t / 0.3f))),
                Grounded(EmoteDance(paths, fit), root, paths, t => 0.045f * Mathf.Abs(Mathf.Sin(Mathf.PI * t / DanceBeat))),
                Grounded(EmoteComeOn(paths, fit), root, paths, t => 0.05f * Bump(t, 0.62f, 0.86f) + 0.03f * Bump(t, 0.86f, 1.06f)),
                Grounded(EmoteGoodGame(paths, fit), root, paths, null),
            };
        }

        private static AnimationClip Grounded(AnimationClip clip, Transform root, Dictionary<string, string> paths, System.Func<float, float> lift)
        {
            GroundIntroduction(clip, root, paths["root"], anchorToRest: true, lift: lift);
            return clip;
        }

        /// <summary>
        /// How one rig's arms measure against its head, off its own bind (the rigs are authored T-posed, arms along X).
        /// `Reach` is the arm's length over the shoulder-to-crown height: about 1.3 on a long-armed hero, under 1 on
        /// the short ones. `Short` turns that into 0 (long) to 1 (shortest) for the poses that have to make up for it.
        /// </summary>
        private readonly struct EmoteFit
        {
            public readonly float Reach, Short;
            /// <summary>The left arm and forearm that put the hand on the side of the head (NO WAY's clamp) and behind
            /// it (TOO EASY's pillow), solved on this rig. The right arm is the mirror (`ArmPose.Right`).</summary>
            public readonly ArmPose Clamp, Pillow;
            private EmoteFit(float reach, ArmPose clamp, ArmPose pillow)
            {
                Reach = reach; Short = Mathf.Clamp01((LongReach - reach) / (LongReach - ShortReach));
                Clamp = clamp; Pillow = pillow;
            }

            // Measured on the five heroes (2026-10-23 bake): Paete 1.50, Nemu 0.66, Dante 0.45, Amihan 0.41,
            // Phaister 0.35 (her hat counts as crown). Big heads make every reach small; the band is theirs.
            private const float LongReach = 1.50f, ShortReach = 0.35f;
            private static readonly ArmPose Unsolved = new ArmPose(new Vector3(-90, 0, 122), 124);

            /// <summary>Scale a fitted angle: the long-armed value, pushed by `extra` on the shortest rig.</summary>
            public float Push(float value, float extra) => value + extra * Short;

            public static EmoteFit Measure(Transform root, Dictionary<string, string> paths)
            {
                var shoulder = root.Find(paths["arm-left"]); var head = root.Find(paths["head"]);
                var shoulderOther = root.Find(paths["arm-right"]);
                var fallback = new EmoteFit(LongReach, Unsolved, Unsolved);
                if (shoulder == null || head == null || shoulderOther == null) return fallback;
                var rig = paths as RigPaths;
                var fore = rig != null && rig.Left.Path != null ? root.Find(rig.Left.Path) : null;
                var hand = fore != null ? fore : shoulder;
                var mesh = new Mesh(); var vertices = new List<Vector3>();
                var headPoints = new List<Vector3>(); var handPoints = new List<Vector3>();
                float top = float.NegativeInfinity, left = float.PositiveInfinity, right = float.NegativeInfinity;
                var centre = (shoulder.position + shoulderOther.position) * 0.5f;
                var across = (shoulderOther.position - shoulder.position).normalized;
                try
                {
                    foreach (var skin in root.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                    {
                        if (skin.sharedMesh == null) continue;
                        skin.BakeMesh(mesh); mesh.GetVertices(vertices);
                        var weights = skin.sharedMesh.boneWeights; var bones = skin.bones;
                        var matrix = Matrix4x4.TRS(skin.transform.position, skin.transform.rotation, Vector3.one);
                        for (int i = 0; i < vertices.Count; i++)
                        {
                            var w = matrix.MultiplyPoint3x4(vertices[i]);
                            top = Mathf.Max(top, w.y);
                            float along = Vector3.Dot(w - centre, across);
                            left = Mathf.Min(left, along); right = Mathf.Max(right, along);
                            // Each vertex goes with the bone that carries most of it: the head's (hair, hat and horns
                            // included) and the left hand's.
                            if (i >= weights.Length || weights[i].weight0 < 0.5f || weights[i].boneIndex0 >= bones.Length) continue;
                            var bone = bones[weights[i].boneIndex0];
                            if (bone == null) continue;
                            if (bone == head || bone.IsChildOf(head)) headPoints.Add(w);
                            else if (bone == hand) handPoints.Add(hand.InverseTransformPoint(w));
                        }
                    }
                }
                finally { Object.DestroyImmediate(mesh); }
                float half = Vector3.Distance(shoulder.position, shoulderOther.position) * 0.5f;
                float arm = (right - left) * 0.5f - half, crown = top - shoulder.position.y;
                if (arm <= 0 || crown <= 0 || float.IsInfinity(top) || headPoints.Count == 0 || handPoints.Count == 0) return fallback;
                var solver = new ArmSolver(shoulder, fore, rig != null ? rig.Left.FoldSign : 0, headPoints, handPoints);
                // ⚠️ THE TARGETS ARE ON THIS RIG'S OWN HEAD, in its box: x out to the arm's side (1 is the side face),
                // y from the jaw (0) to the crown (1), z from the back (-1) to the face (1). On the big-headed heroes
                // (Dante, Amihan, Phaister) no arm angle lifts the hand past about a fifth of the head, so their clamp
                // lands on the cheeks: the solver takes the nearest the arm can get, which is the "no way" face.
                var clamp = solver.Solve(new Vector3(1.0f, 0.65f, 0.1f), Unsolved);
                var pillow = solver.Solve(new Vector3(0.35f, 0.35f, -1.1f), Unsolved);
                return new EmoteFit(arm / crown, clamp, pillow);
            }

            public override string ToString() => "reach " + Reach.ToString("0.00") + ", short " + Short.ToString("0.00")
                + ", clamp " + Clamp + ", pillow " + Pillow;
        }

        /// <summary>One arm's pose as `PoseKey` takes it: the left arm's angles and its forearm's fold.</summary>
        private readonly struct ArmPose
        {
            public readonly Vector3 Left; public readonly float Fold;
            public ArmPose(Vector3 left, float fold) { Left = left; Fold = fold; }
            /// <summary>The same pose on the right arm: `PoseKey` mirrors Z, and Y turns the other way.</summary>
            public Vector3 Right => new Vector3(Left.x, -Left.y, -Left.z);
            public Forearm Fore => HeroAbilityClips.Fore(Fold);
            public override string ToString() => "(" + Left.x.ToString("0") + ", " + Left.y.ToString("0") + ", "
                + Left.z.ToString("0") + ") fold " + Fold.ToString("0");
        }

        /// <summary>
        /// ⚠️⚠️ WHERE A HAND LANDS IS MEASURED ON THE RIG, NOT WORKED OUT FROM EULER ANGLES. Four rounds of hand-keyed
        /// "hands on the head" (2026-10-23) all left the hands at the collar, because a raised arm's X, Y and Z do not
        /// do what they do on a hanging one, and the Python previewer got raised arms wrong. This sweeps the left
        /// arm's `PoseKey` angles and the elbow on the rig itself, with the hand's vertices riding the forearm, and
        /// keeps the pose that lands nearest the target, pulled a little toward the reference so the elbow stays in
        /// the intended family (out and up) instead of the nearest contortion.
        /// </summary>
        private sealed class ArmSolver
        {
            private readonly Transform _arm, _fore; private readonly float _foldSign;
            private readonly Bounds _head; private readonly float _side; private readonly Vector3 _hand;

            public ArmSolver(Transform arm, Transform fore, float foldSign, List<Vector3> head, List<Vector3> handLocal)
            {
                _arm = arm; _fore = fore; _foldSign = foldSign;
                _head = new Bounds(head[0], Vector3.zero); foreach (var p in head) _head.Encapsulate(p);
                _side = Mathf.Sign(arm.position.x - _head.center.x);
                // The hand is the third of the forearm farthest from the elbow; it moves rigidly with the forearm, so
                // its centroid stands for it.
                handLocal.Sort((a, b) => b.sqrMagnitude.CompareTo(a.sqrMagnitude));
                int n = Mathf.Max(1, handLocal.Count / 3); var sum = Vector3.zero;
                for (int i = 0; i < n; i++) sum += handLocal[i];
                _hand = sum / n;
            }

            private Vector3 Place(Vector3 pose, float fold)
            {
                _arm.localEulerAngles = new Vector3(pose.x, pose.y, 80 - pose.z);
                if (_fore != null) _fore.localEulerAngles = new Vector3(0, _foldSign * fold, 0);
                var c = (_fore != null ? _fore : _arm).TransformPoint(_hand);
                return new Vector3((c.x - _head.center.x) * _side / _head.extents.x, (c.y - _head.min.y) / _head.size.y,
                    (c.z - _head.center.z) / _head.extents.z);
            }

            public ArmPose Solve(Vector3 target, ArmPose reference)
            {
                var restArm = _arm.localRotation; var restFore = _fore != null ? _fore.localRotation : Quaternion.identity;
                var best = reference; float bestScore = float.PositiveInfinity;
                try
                {
                    for (float x = -180; x < 180; x += 15)
                    for (float y = -90; y <= 90; y += 15)
                    for (float z = 0; z <= 180; z += 10)
                    for (float fold = 0; fold <= (_fore != null ? 150 : 0); fold += 10)
                    {
                        var pose = new Vector3(x, y, z);
                        // A degree off the reference costs a thousandth of a head; near-ties go to the intended shape.
                        float drift = Mathf.Abs(Mathf.DeltaAngle(x, reference.Left.x)) + Mathf.Abs(y - reference.Left.y)
                                    + Mathf.Abs(z - reference.Left.z) + Mathf.Abs(fold - reference.Fold);
                        float score = (Place(pose, fold) - target).magnitude + drift * 0.001f;
                        if (score < bestScore) { bestScore = score; best = new ArmPose(pose, fold); }
                    }
                }
                finally { _arm.localRotation = restArm; if (_fore != null) _fore.localRotation = restFore; }
                return best;
            }
        }
#endif

        /// <summary>A smooth bump: 0 before `from`, up to 1 in the middle, 0 again at `to`.</summary>
        private static float Bump(float t, float from, float to)
        {
            if (t <= from || t >= to) return 0.0f;
            return Mathf.Sin(Mathf.PI * (t - from) / (to - from));
        }

        private static readonly Vector3 EmoteRestLeft = new Vector3(0, 0, 14), EmoteRestRight = new Vector3(0, 0, -14);
        private static readonly Vector3 Zero = Vector3.zero;

#if UNITY_EDITOR
        /// <summary>LET'S GO: a crouch, a jump with both fists punched up, pumped down to the ears, a second jump, then
        /// one fist pumped at the landing. Loops from the crouch.</summary>
        private static AnimationClip EmoteCheer(Dictionary<string, string> paths, EmoteFit fit)
        {
            var b = new ClipBuilder(EmoteMotion.Cheer, paths);
            // Short arms put the fists level with the crown: arch the back and throw the fists out, not straight up.
            float arch = fit.Push(-18, -12), up = fit.Push(162, -14), look = fit.Push(-28, -8);
            // ⚠️ THE ARM'S X IS A TWIST WHEN THE ARM IS UP AT THE SIDE (euler order ZXY on a T-posed bone), and the Y is a
            // twist when it hangs. So the fists by the ears are a level arm twisted -90 (the elbow's forward hinge then
            // folds UP), and the fists at the chest are a hanging arm twisted 60 in (the hinge folds ACROSS).
            Vector3 chestL = V(-30, 60, 12), chestR = V(-30, -60, -12);
            Vector3 earL = V(-90, 0, 84), earR = V(-90, 0, -84);
            // The ready crouch leans only 12 and keeps the chin up: with no knees, a deeper lean reads as a bow.
            PoseKey(b, 0.00f, 0, V(12, 0, 0), V(-6, 0, 0), chestL, chestR, V(-16, 0, 6), V(-16, 0, 6), Fore(122), Fore(122));
            b.PunchAt(0.20f);
            PoseKey(b, 0.20f, 0, V(arch, 0, 0), V(look, 0, 0), V(-14, 0, up), V(-14, 0, -up), V(8, 0, 4), V(8, 0, 4), Fore(8), Fore(8));
            PoseKey(b, 0.40f, 0, V(10, 0, 0), V(-6, 0, 0), earL, earR, V(-10, 0, 6), V(-10, 0, 6), Fore(118), Fore(118));
            b.PunchAt(0.62f);
            PoseKey(b, 0.62f, 0, V(arch + 2, 0, 0), V(look, 0, 0), V(-14, 0, up), V(-14, 0, -up), V(6, 0, 4), V(6, 0, 4), Fore(6), Fore(6));
            // The "YES!": one fist punched high, then yanked down to the chest with a crunch.
            PoseKey(b, 0.80f, 0, V(14, 0, -4), V(4, 0, 0), chestL, earR, V(-14, 0, 6), V(-14, 0, 6), Fore(120), Fore(116));
            b.PunchAt(0.94f);
            PoseKey(b, 0.94f, 0, V(arch + 4, -8, -8), V(look + 6, -10, 0), chestL, V(-14, 0, -up), V(-10, 0, 6), V(-10, 0, 6), Fore(124), Fore(6));
            b.PunchAt(1.14f);
            PoseKey(b, 1.14f, 0, V(18, 6, 4), V(2, 4, 0), chestL, chestR, V(-22, 0, 6), V(-10, 0, 6), Fore(124), Fore(132));
            PoseKey(b, 1.30f, 0, V(12, 0, 0), V(-6, 0, 0), chestL, chestR, V(-16, 0, 6), V(-16, 0, 6), Fore(122), Fore(122));
            return b.Build();
        }

        /// <summary>NO WAY: both hands clamped on the head, elbows flared, the head shaking in disbelief; a sag forward;
        /// then both arms thrown out at the sky, "why?!", and back onto the head.</summary>
        private static AnimationClip EmoteNoWay(Dictionary<string, string> paths, EmoteFit fit)
        {
            var b = new ClipBuilder(EmoteMotion.NoWay, paths);
            // ⚠️ HANDS ON THE HEAD, NOT A HAND ON THE FACE. The first NO WAY was a facepalm, and from the gameplay
            // camera, which looks down on the court, it was the top of a bowed head and nothing else (the reel, 2026-10-23).
            // Hands clamped on the head with the elbows out read from above, the side and the front. The arms are solved
            // on each rig (`ArmSolver`): on Nemu and Paete the hands land on the sides of the head, on the big-headed
            // three they reach the cheeks, which is as high as those arms go. The head dips a little toward them.
            Vector3 clampL = fit.Clamp.Left, clampR = fit.Clamp.Right;
            var grip = fit.Clamp.Fore;
            float dip = fit.Push(0, 8);
            Vector3 stand = V(-4, 0, 4);
            PoseKey(b, 0.00f, 0, V(-8, 0, 0), V(dip - 10, 0, 0), clampL, clampR, stand, stand, grip, grip);
            PoseKey(b, 0.20f, 0, V(-8, 0, -5), V(dip - 8, 22, -4), clampL, clampR, stand, stand, grip, grip);
            PoseKey(b, 0.40f, 0, V(-8, 0, 5), V(dip - 8, -22, 4), clampL, clampR, stand, stand, grip, grip);
            PoseKey(b, 0.60f, 0, V(-8, 0, -5), V(dip - 8, 20, -4), clampL, clampR, stand, stand, grip, grip);
            PoseKey(b, 0.84f, 0, V(16, 0, 0), V(dip + 14, 0, 0), clampL, clampR, V(-6, 0, 4), V(-6, 0, 4), grip, grip);
            b.PunchAt(1.08f);
            PoseKey(b, 1.08f, 0, V(-16, 0, 0), V(-32, 0, 0), V(-34, 0, 70), V(-34, 0, -70), V(-2, 0, 6), V(-2, 0, 6), Fore(30), Fore(30));
            PoseKey(b, 1.32f, 0, V(-10, 0, 0), V(-18, 0, 0), V(-60, 0, 104), V(-60, 0, -104), stand, stand, Fore(100), Fore(100));
            PoseKey(b, 1.60f, 0, V(-8, 0, 0), V(dip - 10, 0, 0), clampL, clampR, stand, stand, grip, grip);
            return b.Build();
        }

        /// <summary>TOO EASY: a hop down onto the court, then lounging: propped back on the left hand, the right behind the
        /// head, legs out with one crossed over, a slow smug look round. Held.</summary>
        private static AnimationClip EmoteTooEasy(Dictionary<string, string> paths, EmoteFit fit)
        {
            var b = new ClipBuilder(EmoteMotion.TooEasy, paths);
            // Legs out at 80 and splayed 30, a lounging V. Any lower and the heels, which the grounding stands on, lift
            // the seat off the court. ⚠️ Paete's soles still render as black slabs whenever they face the camera (this
            // sit and any hop); their atlas is plain brown, so it is his toon/palette shading, an open finding on a
            // finalized hero (docs/reports/emotes-2026-10-23), not something a pose can hide.
            Vector3 legL = V(-80, 0, 30), legR = V(-80, 0, 30);
            // The pillow hand, solved on each rig (`ArmSolver`) to land behind the head; on the big-headed three it
            // reaches the nape.
            Vector3 prop = V(48, 0, fit.Push(34, 8)), pillow = fit.Pillow.Right;
            PoseKey(b, 0.00f, 0, Zero, Zero, EmoteRestLeft, EmoteRestRight);
            PoseKey(b, 0.14f, 0, V(-6, 0, 0), V(-10, 0, 0), V(-24, 0, 40), V(-24, 0, -40), V(-10, 0, 4), V(-10, 0, 4), Fore(20), Fore(20));
            b.PunchAt(0.34f);
            b.HoldAt(0.34f, 0.04f);
            PoseKey(b, 0.34f, 0, V(12, 0, 0), V(14, 0, 0), V(-50, 0, 34), V(-50, 0, -34), legL, legR, Fore(24), Fore(24));
            PoseKey(b, 0.56f, 0, V(-14, 0, 6), V(-6, 0, 0), prop, V(-60, 0, -104), legL, legR, Fore(8), Fore(90));
            PoseKey(b, 0.80f, 0, V(-24, 4, 10), V(-4, -16, -10), prop, pillow, legL, legR, Fore(4), fit.Pillow.Fore);
            PoseKey(b, 1.20f, 0, V(-24, 4, 10), V(-2, 22, -12), prop, pillow, legL, legR, Fore(4), fit.Pillow.Fore);
            PoseKey(b, 1.70f, 0, V(-24, 4, 10), V(-6, -8, -12), prop, pillow, legL, legR, Fore(4), fit.Pillow.Fore);
            return b.Build();
        }

        /// <summary>HA HA: a point at whoever it is for, then doubled over, then the head thrown back, a hand slapping the
        /// knee, and doubled over again.</summary>
        private static AnimationClip EmoteLaugh(Dictionary<string, string> paths, EmoteFit fit)
        {
            var b = new ClipBuilder(EmoteMotion.Laugh, paths);
            // Pushed about 1.35x after the first strips (owner's bar: "push amplitudes 1.3 to 1.4x"): on Dante the
            // doubled-over and head-back beats barely moved, and a face that cannot change has to laugh with its whole
            // body. Each beat also rocks to a side, so the loop shakes instead of nodding.
            float over = fit.Push(52, 8), back = fit.Push(-24, -6), throwHead = -42;
            Vector3 belly = V(-34, 0, 20), point = V(-84, 0, -10), slap = V(-14, 0, -18);
            PoseKey(b, 0.00f, 0, V(4, -14, 0), V(-14, -12, 0), belly, point, V(-4, 0, 4), V(-4, 0, 4), Fore(108), Fore(0));
            PoseKey(b, 0.14f, 0, V(2, -16, 0), V(-18, -14, 4), belly, point, V(-4, 0, 4), V(-4, 0, 4), Fore(110), Fore(0));
            b.PunchAt(0.32f);
            PoseKey(b, 0.32f, 0, V(over, 0, 10), V(16, 0, 8), belly, slap, V(-10, 0, 4), V(-10, 0, 4), Fore(112), Fore(16));
            PoseKey(b, 0.52f, 0, V(back, 0, -8), V(throwHead, 0, -10), V(-24, 0, 36), V(-24, 0, -46), V(-2, 0, 4), V(-2, 0, 4), Fore(110), Fore(70));
            b.PunchAt(0.72f);
            PoseKey(b, 0.72f, 0, V(over + 4, 0, -10), V(18, 0, -8), belly, slap, V(-10, 0, 4), V(-10, 0, 4), Fore(112), Fore(16));
            PoseKey(b, 0.92f, 0, V(back - 2, 0, 8), V(throwHead - 2, 0, 10), V(-24, 0, 36), V(-24, 0, -46), V(-2, 0, 4), V(-2, 0, 4), Fore(110), Fore(70));
            b.PunchAt(1.10f);
            PoseKey(b, 1.10f, 0, V(over - 6, 0, 8), V(14, 0, 6), belly, slap, V(-10, 0, 4), V(-10, 0, 4), Fore(112), Fore(16));
            PoseKey(b, 1.40f, 0, V(4, -14, 0), V(-14, -12, 0), belly, point, V(-4, 0, 4), V(-4, 0, 4), Fore(108), Fore(0));
            return b.Build();
        }

        private const float DanceBeat = 0.48f;

        /// <summary>DANCE: two low, loose street-dance steps, the arms swinging like rope, then both arms up waving side to
        /// side over a hip sway. A bounce on every beat; the feet step.</summary>
        private static AnimationClip EmoteDance(Dictionary<string, string> paths, EmoteFit fit)
        {
            var b = new ClipBuilder(EmoteMotion.Dance, paths);
            float q = DanceBeat * 0.5f, up = fit.Push(150, -10);
            Vector3 stepL = V(-22, 0, 6), stepR = V(-22, 0, 6), back = V(12, 0, 6);
            // Beats 1 and 2: low, leaning in, the arms swung loose and opposite, the head bobbing.
            PoseKey(b, 0 * q, 0, V(24, -12, -8), V(8, -10, -6), V(-46, 0, 52), V(34, 0, -36), stepL, back, Fore(80), Fore(30));
            PoseKey(b, 1 * q, 0, V(18, -4, -2), V(-4, -4, 0), V(-20, 0, 30), V(10, 0, -24), back, back, Fore(50), Fore(20));
            PoseKey(b, 2 * q, 0, V(24, 12, 8), V(8, 10, 6), V(34, 0, 36), V(-46, 0, -52), back, stepR, Fore(30), Fore(80));
            PoseKey(b, 3 * q, 0, V(18, 4, 2), V(-4, 4, 0), V(10, 0, 24), V(-20, 0, -30), back, back, Fore(20), Fore(50));
            // Beats 3 and 4: up tall, both arms high and waving, the hips swaying under them.
            PoseKey(b, 4 * q, 0, V(-8, 10, 12), V(-14, 8, 10), V(-10, 0, up - 14), V(-10, 0, -up - 6), stepL, back, Fore(40), Fore(20));
            PoseKey(b, 5 * q, 0, V(-10, 0, 0), V(-18, 0, 0), V(-10, 0, up), V(-10, 0, -up), back, back, Fore(20), Fore(20));
            PoseKey(b, 6 * q, 0, V(-8, -10, -12), V(-14, -8, -10), V(-10, 0, up + 6), V(-10, 0, -up + 14), back, stepR, Fore(20), Fore(40));
            PoseKey(b, 7 * q, 0, V(8, 0, 0), V(0, 0, 0), V(-30, 0, 70), V(-30, 0, -70), back, back, Fore(60), Fore(60));
            // The seam: the first pose again.
            PoseKey(b, 8 * q, 0, V(24, -12, -8), V(8, -10, -6), V(-46, 0, 52), V(34, 0, -36), stepL, back, Fore(80), Fore(30));
            return b.Build();
        }

        /// <summary>COME ON: chest out, chin up, both hands beckoning twice, then the arms spread wide and a boxer's bounce.</summary>
        private static AnimationClip EmoteComeOn(Dictionary<string, string> paths, EmoteFit fit)
        {
            var b = new ClipBuilder(EmoteMotion.ComeOn, paths);
            float chest = fit.Push(-10, -6);
            Vector3 reachL = V(-56, 0, 40), reachR = V(-56, 0, -40);
            Vector3 wideL = V(-12, 0, 50), wideR = V(-12, 0, -50);
            PoseKey(b, 0.00f, 0, V(chest, 0, 0), V(-14, 0, 0), reachL, reachR, V(-8, 0, 6), V(8, 0, 6), Fore(20), Fore(20));
            b.PunchAt(0.16f);
            PoseKey(b, 0.16f, 0, V(chest - 4, 0, 0), V(-18, 0, 0), reachL, reachR, V(-8, 0, 6), V(8, 0, 6), Fore(118), Fore(118));
            PoseKey(b, 0.30f, 0, V(chest, 0, 0), V(-14, 0, 0), reachL, reachR, V(-8, 0, 6), V(8, 0, 6), Fore(24), Fore(24));
            b.PunchAt(0.44f);
            PoseKey(b, 0.44f, 0, V(chest - 4, 0, 0), V(-18, 0, 0), reachL, reachR, V(-8, 0, 6), V(8, 0, 6), Fore(118), Fore(118));
            PoseKey(b, 0.58f, 0, V(6, 0, 0), V(-4, 0, 0), V(-30, 0, 40), V(-30, 0, -40), V(-8, 0, 6), V(8, 0, 6), Fore(60), Fore(60));
            b.PunchAt(0.76f);
            PoseKey(b, 0.76f, 0, V(chest - 6, 0, 0), V(-20, 0, 10), wideL, wideR, V(-12, 0, 8), V(10, 0, 8), Fore(30), Fore(30));
            PoseKey(b, 0.96f, 0, V(chest - 4, 0, 0), V(-18, 0, -8), wideL, wideR, V(-8, 0, 8), V(8, 0, 8), Fore(36), Fore(36));
            PoseKey(b, 1.20f, 0, V(chest, 0, 0), V(-14, 0, 0), reachL, reachR, V(-8, 0, 6), V(8, 0, 6), Fore(20), Fore(20));
            return b.Build();
        }

        /// <summary>GG: the right fist pressed into the left palm in front of the chest, a deep bow over it, then upright
        /// with the salute held and a nod. Held.</summary>
        private static AnimationClip EmoteGoodGame(Dictionary<string, string> paths, EmoteFit fit)
        {
            var b = new ClipBuilder(EmoteMotion.GoodGame, paths);
            // The fists meet in front of the sternum: hanging arms lifted forward and twisted 70 in, so the elbows'
            // hinges fold the forearms ACROSS the chest (see LET'S GO's note on the twists).
            Vector3 palm = V(-34, 70, 10), fist = V(-34, -70, -10);
            var fold = Fore(fit.Push(96, 8));
            PoseKey(b, 0.00f, 0, Zero, Zero, EmoteRestLeft, EmoteRestRight);
            PoseKey(b, 0.26f, 0, V(-6, 0, 0), V(-8, 0, 0), palm, fist, Zero, Zero, fold, fold);
            b.PunchAt(0.62f);
            b.HoldAt(0.62f, 0.40f);
            PoseKey(b, 0.62f, 0, V(fit.Push(46, 6), 0, 0), V(20, 0, 0), palm, fist, V(-2, 0, 2), V(-2, 0, 2), fold, fold);
            PoseKey(b, 1.12f, 0, V(-2, 0, 0), V(-6, 0, 0), palm, fist, Zero, Zero, fold, fold);
            PoseKey(b, 1.30f, 0, V(0, 0, 0), V(14, 0, 0), palm, fist, Zero, Zero, fold, fold);
            PoseKey(b, 1.50f, 0, V(0, 0, 0), V(-4, 0, 0), palm, fist, Zero, Zero, fold, fold);
            PoseKey(b, 1.90f, 0, V(-2, 0, 0), V(-2, 0, 0), palm, fist, Zero, Zero, fold, fold);
            return b.Build();
        }
#endif
    }
}
