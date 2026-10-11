using System;
using System.Collections.Generic;
using TumbangPreso.CameraSystem;
using UnityEngine;

namespace TumbangPreso.Visual
{
    public sealed partial class HeroIntroductionScene
    {
        // =========================================================================================
        // NEMU AND KURO, LIGHTS OUT, 7.2 s (was DEVOURING SEANCE, 3.8 s; plan.md § 4 is the first one).
        //
        // ⚠️ RESTAGED 2026-10-09, UNPITCHED. The owner was away and asked for "something for the rest of the heroes ...
        // surprise me with something really good", so this one was not chosen from options as Dante's, Cheska's and
        // Sean's were. It is BLOCKING until he says the idea stays. The first cutscene is whole in git history and in
        // `tools/cutscene_rework/old/`.
        //
        // What is kept, because it is the part that is hers: "Looks distracted. Already knows your next move." She gazes
        // at nothing, Kuro nudges her, she offers a hand and he nuzzles it, then she looks straight down the lens
        // (0 to 2.45, unchanged). What is new is everything the look sets off, and it now shows what the ultimate DOES
        // ("Kuro becomes a monster and chases every player he sees"):
        //   1. THE DIVE (2.45 to 3.0), from high over the court: the ink runs out from her feet across the whole floor
        //      and the walls go up. Little Kuro hops once and plunges into it like a fish.
        //   2. THE RISE (3.0 to 4.0), same shot: far too big, he comes up out of the ink behind her, taller than the
        //      court is wide. She does not turn round.
        //   3. HE SEES YOU (4.0 to 4.95): his head turns to each other player where they really stand, one at a time,
        //      and a pair of eyes opens in the floor under each of them.
        //   4. THE SILHOUETTE (4.95 to 6.0), low and close: she is small and calm with her hands folded, he is the whole
        //      sky behind her. This is the plan's signature picture, finally at its real scale.
        //   5. THE SEND (6.0 to 7.2): she points, he rears and comes over her head at the lens mouth first, and the
        //      picture is swallowed. It ENDS BLACK, so every screen returns to its own eyes out of the dark with him
        //      already loose.
        //
        // ⚠️ NOTHING FADES AT THE END. The stage is whole to the last frame and the last frames are black: the going
        // dark is the hand-back. A stage that left early would show the court for a moment before he arrives.
        //
        // ⚠️ THE RETAINED KURO RENDER COPY AND HIS CALM/RAGE FORMS ARE UNCHANGED. He is the same copy the whole way: calm
        // until he dives, the rage form from the rise, at `GhostPetCompanion.DevourScale` times `NmGiant`.
        //
        // ⚠️ EVERY TIME BELOW IS ON THE TABLE'S CLOCK (`tools/author_ultimate_intros.py`, `nemu`): her look, her folded
        // hands and her point are typed there.
        // =========================================================================================
        private const float NmDark = 2.3f, NmDive = 2.5f, NmRise = 3.0f, NmRisen = 4.0f, NmLean = 4.9f, NmRear = 5.95f, NmLunge = 6.2f, NmGulp = 6.95f;
        // How many times his devouring size he is here, where he stands, and where the last shot's lens is authored.
        private const float NmGiant = 2.4f;
        private static readonly Vector3 NmGiantAt = new Vector3(0, 0, -7.5f), NmLastLens = new Vector3(0, 1.3f, 6.5f);

        /// <summary>⚠️ A FILM OUTSIDE PLAY HAS NO KURO: its caster is a bare motor with no `CharacterVisual`. The editor's cutscene
        /// film stands one beside her and names it here; in a match this is null and unread (as `FilmStandIns`).</summary>
        public static GhostPetCompanion FilmCompanion;
        private MatchPoseHistory.Copy _kuro;
        private KuroRagePresentation _rage;
        private Vector3 _kuroScale, _nmLens;
        private Quaternion _kuroFront;
        private Renderer[] _kuroRenderers;
        private int _inkWall, _inkSpill, _inkRim, _nmDiveRing, _nmRiseRing, _nmShroud;
        private readonly List<int> _inkEyes = new List<int>(8), _nmSeen = new List<int>(16);
        private static readonly Vector3 KuroSeat = new Vector3(-.95f, .65f, .15f);

        private void BuildNemu(CharacterMotor source)
        {
            var companion = source.GetComponent<CharacterVisual>()?.Companion ?? FilmCompanion;
            if (companion == null) throw new InvalidOperationException("Nemu's introduction requires retained Kuro.");
            var track = new MatchPoseHistory.Track(source, companion.gameObject);
            track.Record(0); track.Record(.05f); _kuro = track.Clone(_root.transform);
            if (_kuro == null) throw new InvalidOperationException("Kuro exceeded the render-copy contract.");
            track.Apply(_kuro, .05f); _kuro.Root.SetActive(true);
            _kuro.Root.transform.localPosition = KuroSeat;
            _kuro.Root.transform.localRotation = Quaternion.Euler(0, -18, 0);
            // The live pet's idle fidget can stretch its current scale.
            // Use the same canonical base as the actual devour, not that transient pose.
            _kuroScale = companion.RestScale;
            Vector3 face = companion.MouthPosition - companion.transform.position; face.y = 0;
            Vector3 localFace = companion.transform.InverseTransformDirection(face.normalized);
            _kuroFront = localFace.sqrMagnitude > .01f ? Quaternion.FromToRotation(localFace, Vector3.forward) : Quaternion.identity;
            var calm = GhostPetCompanion.FindForm(_kuro.Root.transform, "CalmForm");
            var rage = GhostPetCompanion.FindForm(_kuro.Root.transform, "RageForm");
            if (calm == null || rage == null) throw new InvalidOperationException("Retained Kuro calm/rage forms are missing.");
            // The live helper has already made six inactive eye wisps.
            // Copies of those have no timeline; the private helper below
            // owns fresh ones. Do not grow dormant source spheres too.
            foreach (var bone in _kuro.Bones)
                if (bone.name == "KuroEyeWisp")
                { bone.gameObject.SetActive(false); bone.SetParent(_root.transform, false); ObjectDestroy(bone.gameObject); }
            _rage = new KuroRagePresentation(_kuro.Root, calm, rage);
            _kuroRenderers = _kuro.Root.GetComponentsInChildren<Renderer>(true);

            _inkWall = Add("InkRising", WallMesh(30, true), new Color(.05f, .02f, .08f, .9f), .04f);
            _inkRim = Add("InkRisingEdge", WallMesh(30), new Color(.42f, .2f, .62f, .7f), .5f);
            _inkSpill = Add("InkSpill", VfxShapes.Splat(18, .3f, 44), new Color(.04f, .01f, .06f, .92f), .02f);
            for (int i = 0; i < 8; i++)
                _inkEyes.Add(Add("InkEye" + i, VfxShapes.TwoSided(VfxShapes.Splat(10, .05f, 100 + i)), new Color(.93f, .86f, 1, .95f), .8f));
            _nmDiveRing = Add("InkDiveRing", VfxShapes.Collar(24, .02f, .82f), new Color(.62f, .4f, .85f, .8f), .5f);
            _nmRiseRing = Add("InkRiseRing", VfxShapes.Collar(32, .02f, .9f), new Color(.62f, .4f, .85f, .8f), .5f);

            // Everyone else, where they stand, brought near enough for the high shot to hold them. Each gets a pair of
            // eyes in the floor under them: two whites and two slit pupils.
            StageOthers(3f, 11f);
            for (int i = 0; i < _others.Count && i < 4; i++)
                for (int k = 0; k < 2; k++)
                {
                    _nmSeen.Add(Add("SeenEye" + i + "-" + k, VfxShapes.Splat(10, .05f, 140 + i * 2 + k), new Color(.93f, .86f, 1, .95f), .8f));
                    _nmSeen.Add(Add("SeenPupil" + i + "-" + k, VfxShapes.Splat(8, .05f, 150 + i * 2 + k), new Color(.1f, .02f, .16f, 1f), .02f));
                }
            // The last shot looks along the court from 6.5 m out: keep it off whoever stands there, once, so it does not slide.
            _nmLens = KeepLensOffOthers(NmLastLens, 1.8f);

            // The swallow: black all round the last lens. Unlit, so it is black and not a dark grey surface in the sun.
            _nmShroud = Add("Swallowed", VfxShapes.Prism(12, 1, 1), new Color(.02f, 0, .03f, 1f), 0f, plain: true);
            ZackUnlitBackdrop(_nmShroud);
        }

        /// <summary>When he looks at the i-th other player, and their eyes in the floor open.</summary>
        private static float NemuSeenAt(int i) => NmRisen + .02f + i * .3f;

        private void SampleNemu(float t)
        {
            // ------------------------------------------------------------- little Kuro, up to the dive
            float bob = Mathf.Sin(t * 2.4f) * .06f;
            Vector3 atHand = FreePalm + new Vector3(-.2f, .12f, .12f);
            Vector3 nudge = new Vector3(-.55f, .95f, .25f);
            Vector3 behind = new Vector3(-.75f, .8f, -.35f);
            Vector3 at = KuroSeat + Vector3.up * bob;
            at = Vector3.Lerp(at, nudge, Ease(.52f, .7f, t) * (1 - Ease(.78f, .98f, t)));
            at = Vector3.Lerp(at, atHand + Vector3.up * bob * .5f, Ease(1.2f, 1.45f, t) * (1 - Ease(1.72f, 1.95f, t)));
            at = Vector3.Lerp(at, behind + Vector3.up * bob, Ease(1.8f, 2.1f, t));
            // One hop, then down into the ink. The plunge accelerates: he falls, he is not lowered.
            float hop = Mathf.Sin(Mathf.Clamp01((t - (NmDive - .24f)) / .34f) * Mathf.PI);
            float dive = Mathf.Clamp01((t - (NmDive + .06f)) / .3f); dive *= dive;
            at += Vector3.up * hop * .3f;
            at = Vector3.Lerp(at, new Vector3(-.75f, -1.3f, -.35f), dive);
            float turn = Mathf.Lerp(-18, 20, Ease(.5f, .7f, t) * (1 - Ease(.8f, 1f, t)));
            float nuzzle = Ease(1.42f, 1.5f, t) * (1 - Ease(1.55f, 1.7f, t));
            var squash = new Vector3(1 + nuzzle * .12f - dive * .3f, 1 - nuzzle * .1f + dive * .55f, 1 + nuzzle * .12f - dive * .3f);

            bool giant = t >= NmRise - .04f;
            if (!giant)
            {
                _kuro.Root.transform.localPosition = at;
                _kuro.Root.transform.localRotation = Quaternion.Euler(0, turn, 0) * _kuroFront;
                _kuro.Root.transform.localScale = Vector3.Scale(_kuroScale, squash);
                _rage.Sample(0, t, .12f);
            }
            else
            {
                // --------------------------------------------------------- the giant
                float rise = Ease(NmRise, NmRisen, t);
                // He comes up fast and settles: a little too far, then back.
                float over = Mathf.Sin(Mathf.Clamp01((t - (NmRisen - .25f)) / .7f) * Mathf.PI) * .45f;
                var pos = NmGiantAt + Vector3.up * (Mathf.Lerp(-9.5f, 0f, rise) + over + Mathf.Sin(t * 1.3f) * .12f * rise);
                // His head goes to each of them in turn, then comes back to her.
                float yaw = 0;
                for (int i = 0; i < _others.Count && i < 4; i++)
                {
                    float s = NemuSeenAt(i);
                    float want = Mathf.Clamp(Mathf.Atan2(_others[i].Home.x - NmGiantAt.x, _others[i].Home.z - NmGiantAt.z) * Mathf.Rad2Deg, -70f, 70f);
                    yaw = Mathf.Lerp(yaw, want, Ease(s - .12f, s + .04f, t));
                }
                yaw = Mathf.Lerp(yaw, 0, Ease(NmLean - .25f, NmLean + .1f, t));
                // He leans over her for the silhouette, rears back, and goes.
                float lean = Ease(NmLean, NmLean + .7f, t) * 12f;
                float rear = Ease(NmRear, NmLunge, t);
                pos += new Vector3(0, .7f, -1.1f) * rear;
                lean -= rear * 16f;
                float go = Mathf.Clamp01((t - NmLunge) / (NmGulp - NmLunge)); go = go * go * (.4f + .6f * go);
                lean += go * 22f;
                var root = _kuro.Root.transform;
                root.localRotation = Quaternion.Euler(lean, yaw, 0) * _kuroFront;
                root.localScale = _kuroScale * (GhostPetCompanion.DevourScale * NmGiant * (1 + go * .25f));
                root.localPosition = pos;
                _rage.Sample(1, t, 0);
                if (go > 0)
                {
                    // His MOUTH goes to the lens, wherever the mouth is on him at this size and lean, and on through it.
                    Vector3 maw = _root.transform.InverseTransformPoint(_rage.MawPosition) - pos;
                    Vector3 through = (_nmLens - (pos + maw)).normalized * 2.2f;
                    root.localPosition = Vector3.Lerp(pos, _nmLens - maw + through, go);
                }
            }

            // ------------------------------------------------------------- the ink
            // It pools under Kuro the moment she looks at the viewer, then runs out over the whole court.
            float spread = Ease(1.88f, NmRise, t);
            Place(_inkSpill, Vector3.Lerp(new Vector3(-1.2f, .02f, -.2f), new Vector3(0, .02f, 0), spread), Vector3.one * Mathf.Lerp(.3f, 17.5f, spread),
                Quaternion.Euler(0, 20, 0), Ease(1.88f, 2.3f, t));
            // ⚠️⚠️ THE STAGE ENCLOSES THE CAMERA. At 8 m the old fitted shot left the wall and filmed its outside (solid black
            // frames, `Logs/review-v3/nemu-introduction-scene`, 2026-09-24). At 16 m every lens here, the high one at 12.5 m
            // out included, is inside it.
            const float StageRadius = 16;
            float climb = Ease(NmDark - .2f, NmRise, t);
            float height = Mathf.Max(.01f, climb * 18);
            Place(_inkWall, Vector3.zero, new Vector3(StageRadius, height, StageRadius), Quaternion.identity, Ease(NmDark - .2f, NmDark, t));
            Place(_inkRim, Vector3.up * Mathf.Max(0, height - .12f), new Vector3(StageRadius - .05f, .12f, StageRadius - .05f), Quaternion.identity,
                Ease(NmDark - .2f, NmDark, t) * (1 - Ease(NmRise - .1f, NmRise + .05f, t)));

            // Where he went in, and where he comes out.
            float ringAge = t - (NmDive + .3f), ringU = Mathf.Clamp01(ringAge / .7f);
            Place(_nmDiveRing, new Vector3(-.75f, .05f, -.35f), Vector3.one * Mathf.Lerp(.25f, 2.6f, ringU), Quaternion.identity, ringAge >= 0 ? (1 - ringU) * .9f : 0);
            float outAge = t - NmRise, outU = Mathf.Clamp01(outAge / 1.3f);
            Place(_nmRiseRing, NmGiantAt + Vector3.up * .05f, Vector3.one * Mathf.Lerp(2.5f, 11f, outU), Quaternion.identity, outAge >= 0 ? (1 - outU) * .9f : 0);

            // His eyes open in the dark behind him, in pairs, and blink.
            for (int i = 0; i < _inkEyes.Count; i++)
            {
                int pair = i / 2; float side = i % 2 == 0 ? -.28f : .28f;
                float angle = (pair < 2 ? 112 + pair * 30 : 218 + (pair - 2) * 30) * Mathf.Deg2Rad;
                var eye = new Vector3(Mathf.Sin(angle) * 15.2f + side * 2 * Mathf.Cos(angle), 3.4f + (pair % 2) * 2.6f, Mathf.Cos(angle) * 15.2f - side * 2 * Mathf.Sin(angle));
                var face = Quaternion.LookRotation(-new Vector3(eye.x, 0, eye.z).normalized, Vector3.up) * Quaternion.Euler(90, 0, 0);
                float open = Ease(NmRise + .25f + pair * .14f, NmRise + .4f + pair * .14f, t);
                float blink = _reducedEffects ? 1 : 1 - .9f * Mathf.Clamp01(1 - Mathf.Abs(Mathf.Repeat(t + pair * .37f, 1.3f) - .65f) / .05f);
                Place(_inkEyes[i], eye, new Vector3(.55f, 1, .25f * blink + .01f), face, open);
            }

            // The eyes in the floor under each player he has seen. They open with a snap and stay.
            for (int i = 0; i < _others.Count && i < 4; i++)
            {
                float s = NemuSeenAt(i), open = Mathf.Clamp01((t - s) / .1f);
                float pop = 1 + .35f * Mathf.Sin(Mathf.Clamp01((t - s) / .3f) * Mathf.PI);
                // Laid across the line from him to them, so from behind her they read as a pair looking up at the lens.
                var home = _others[i].Home;
                for (int k = 0; k < 2; k++)
                {
                    var spot = home + new Vector3((k == 0 ? -1.05f : 1.05f), .04f, .05f);
                    Place(_nmSeen[i * 4 + k * 2], spot, new Vector3(.95f * pop, 1, .5f * open * pop + .001f), Quaternion.identity, open);
                    Place(_nmSeen[i * 4 + k * 2 + 1], spot + Vector3.up * .01f, new Vector3(.16f, 1, .44f * open + .001f), Quaternion.identity, open);
                }
            }

            // Swallowed: black all round the lens as his mouth closes over it, and it stays black.
            float gone = Ease(NmGulp - .09f, NmGulp + .02f, t);
            // A shell a hand's breadth from the lens: at 6 m he was inside it, and the last frames were the lit inside of his
            // throat instead of black (film n2).
            Place(_nmShroud, _nmLens - Vector3.up * .3f, new Vector3(.3f, .6f, .3f), Quaternion.identity, gone);
        }

        /// <summary>The last shot's lens is the one the build kept off the others, and it trembles as he comes.</summary>
        private void NemuFrame(int index, float t, ref Vector3 eye, ref Vector3 look, ref float fov)
        {
            if (index != 4) return;
            eye = _nmLens;
            if (_reducedEffects) return;
            float go = Mathf.Clamp01((t - NmLunge) / (NmGulp - NmLunge));
            var tremble = new Vector3(Mathf.Sin(t * 71f), Mathf.Sin(t * 83f + 1.3f), 0) * (.035f * go * go);
            eye += tremble; look += tremble;
        }

        /// <summary>The lights go down with the ink, and out when he swallows the picture.</summary>
        private void NemuGrade(float t, out float brightness, out float saturation)
        {
            float dark = Ease(NmDark, NmRise, t);
            brightness = Mathf.Lerp(1f, .8f, dark) * (1 - Ease(NmGulp - .09f, NmGulp + .02f, t));
            saturation = Mathf.Lerp(1f, .78f, dark);
        }
    }
}
