using System;
using System.Collections.Generic;
using TumbangPreso.CameraSystem;
using UnityEngine;

namespace TumbangPreso.Visual
{
    public sealed partial class HeroIntroductionScene
    {
        // =========================================================================================
        // NEMU AND KURO, DEVOURING SEANCE, 3.8 s. plan.md § 4.
        //
        // "Looks distracted. Already knows your next move." Kuro is "a curious little shadow at
        // her shoulder, an alarming presence when a match turns serious" (CHARACTER_ORIGINS.md),
        // so the piece is two characters: she gazes at nothing, Kuro nudges her, she offers him a
        // hand and he nuzzles it, then she opens the hand and looks straight down the lens, and
        // THAT is when the dark comes. Ink seeps up out of the road from where Kuro floats and
        // climbs the whole stage, his eyes open in it, and he swells behind her while she stays
        // calm with her hands folded. She points; the live seance sends him.
        //
        // ⚠️ THE RETAINED KURO RENDER COPY, CALM/RAGE FORMS AND DEVOUR SCALE ARE UNCHANGED. Only
        // his path and timing are new, plus the ink stage, which is drawn only in the overlay.
        // =========================================================================================
        private MatchPoseHistory.Copy _kuro;
        private KuroRagePresentation _rage;
        private Vector3 _kuroScale;
        private Quaternion _kuroFront;
        private Renderer[] _kuroRenderers;
        private int _inkWall, _inkSpill, _inkRim;
        private readonly List<int> _inkEyes = new List<int>(8);
        private static readonly Vector3 KuroSeat = new Vector3(-.95f, .65f, .15f);

        private void BuildNemu(CharacterMotor source)
        {
            var companion = source.GetComponent<CharacterVisual>()?.Companion;
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

            _inkWall = Add("InkRising", WallMesh(30), new Color(.05f, .02f, .08f, .9f), .04f);
            _inkRim = Add("InkRisingEdge", WallMesh(30), new Color(.42f, .2f, .62f, .7f), .5f);
            _inkSpill = Add("InkSpill", VfxShapes.Splat(18, .3f, 44), new Color(.04f, .01f, .06f, .92f), .02f);
            for (int i = 0; i < 8; i++)
                _inkEyes.Add(Add("InkEye" + i, VfxShapes.TwoSided(VfxShapes.Splat(10, .05f, 100 + i)), new Color(.93f, .86f, 1, .95f), .8f));
        }

        private void SampleNemu(float t)
        {
            float leave = 1 - Ease(Seconds - .45f, Seconds - .05f, t);

            // Kuro's path: bobbing at her shoulder, a nudge at her head, to her offered hand,
            // back behind her shoulder for the look, then out behind her as he grows.
            float bob = Mathf.Sin(t * 2.4f) * .06f;
            Vector3 atHand = FreePalm + new Vector3(-.2f, .12f, .12f);
            Vector3 nudge = new Vector3(-.55f, .95f, .25f);
            Vector3 behind = new Vector3(-.75f, .8f, -.35f);
            Vector3 at = KuroSeat + Vector3.up * bob;
            at = Vector3.Lerp(at, nudge, Ease(.52f, .7f, t) * (1 - Ease(.78f, .98f, t)));
            at = Vector3.Lerp(at, atHand + Vector3.up * bob * .5f, Ease(1.2f, 1.45f, t) * (1 - Ease(1.72f, 1.95f, t)));
            at = Vector3.Lerp(at, behind + Vector3.up * bob, Ease(1.8f, 2.1f, t));
            float amount = Ease(2.2f, 3.2f, t);
            at = Vector3.Lerp(at, new Vector3(-1.55f, 0, -.2f), amount);
            _kuro.Root.transform.localPosition = at;
            float turn = Mathf.Lerp(-18, 20, Ease(.5f, .7f, t) * (1 - Ease(.8f, 1f, t)));
            _kuro.Root.transform.localRotation = Quaternion.Euler(0, Mathf.Lerp(turn, 0, amount), 0) * _kuroFront;
            // The nuzzle: a small squash against her hand.
            float nuzzle = Ease(1.42f, 1.5f, t) * (1 - Ease(1.55f, 1.7f, t));
            var squash = new Vector3(1 + nuzzle * .12f, 1 - nuzzle * .1f, 1 + nuzzle * .12f);
            _kuro.Root.transform.localScale = Vector3.Scale(_kuroScale * Mathf.Lerp(1, GhostPetCompanion.DevourScale, amount), squash);
            _rage.Sample(amount, t, .12f);

            // The ink: it pools under Kuro the moment she looks at the viewer, then climbs the stage.
            float spill = Ease(1.88f, 2.4f, t) * leave;
            Place(_inkSpill, new Vector3(-1.2f, .02f, -.2f), Vector3.one * Mathf.Lerp(.3f, 6.5f, Ease(1.88f, 2.7f, t)), Quaternion.Euler(0, 20, 0), spill);
            float climb = Ease(2.05f, 2.9f, t);
            float height = Mathf.Max(.01f, climb * 11);
            Place(_inkWall, Vector3.zero, new Vector3(8, height, 8), Quaternion.identity, Ease(2.05f, 2.2f, t) * leave);
            Place(_inkRim, Vector3.up * Mathf.Max(0, height - .12f), new Vector3(7.95f, .12f, 7.95f), Quaternion.identity,
                Ease(2.05f, 2.2f, t) * (1 - Ease(2.8f, 2.95f, t)) * leave);

            // His eyes open in the dark, in pairs, and blink.
            for (int i = 0; i < _inkEyes.Count; i++)
            {
                int pair = i / 2; float side = i % 2 == 0 ? -.28f : .28f;
                float angle = (-60 + pair * 40) * Mathf.Deg2Rad;
                var eye = new Vector3(Mathf.Sin(angle) * 7.6f + side * Mathf.Cos(angle), 2.6f + (pair % 2) * 1.3f, Mathf.Cos(angle) * 7.6f - side * Mathf.Sin(angle));
                var face = Quaternion.LookRotation(-new Vector3(eye.x, 0, eye.z).normalized, Vector3.up) * Quaternion.Euler(90, 0, 0);
                float open = Ease(2.55f + pair * .12f, 2.7f + pair * .12f, t);
                float blink = _reducedEffects ? 1 : 1 - .9f * Mathf.Clamp01(1 - Mathf.Abs(Mathf.Repeat(t + pair * .37f, 1.3f) - .65f) / .05f);
                Place(_inkEyes[i], eye, new Vector3(.2f, 1, .09f * blink + .005f), face, open * leave);
            }
        }
    }
}
