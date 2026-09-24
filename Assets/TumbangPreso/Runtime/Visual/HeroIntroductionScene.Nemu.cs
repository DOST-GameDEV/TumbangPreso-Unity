using System;
using TumbangPreso.CameraSystem;
using UnityEngine;

namespace TumbangPreso.Visual
{
    public sealed partial class HeroIntroductionScene
    {
        private MatchPoseHistory.Copy _kuro;
        private KuroRagePresentation _rage;
        private Vector3 _kuroScale;
        private Quaternion _kuroFront;
        private Renderer[] _kuroRenderers;

        // Nemu: retained 2.8 s baseline until her own REFINE-2.11 performance replaces it.
        private void BuildNemu(CharacterMotor source)
        {
            var companion = source.GetComponent<CharacterVisual>()?.Companion;
            if (companion == null) throw new InvalidOperationException("Nemu's introduction requires retained Kuro.");
            var track = new MatchPoseHistory.Track(source, companion.gameObject);
            track.Record(0); track.Record(.05f); _kuro = track.Clone(_root.transform);
            if (_kuro == null) throw new InvalidOperationException("Kuro exceeded the render-copy contract.");
            track.Apply(_kuro, .05f); _kuro.Root.SetActive(true);
            _kuro.Root.transform.localPosition = new Vector3(-.95f, .65f, .15f);
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
        }
        private void SampleNemu(float t)
        {
            float amount = Ease(1.05f, 2.28f, t);
            _kuro.Root.transform.localPosition = new Vector3(-.95f - amount * .6f, (.65f + .06f * Mathf.Sin(t * 2)) * (1 - amount), .15f);
            _kuro.Root.transform.localRotation = Quaternion.Euler(0, Mathf.Lerp(-18, 0, amount), 0) * _kuroFront;
            _kuro.Root.transform.localScale = _kuroScale * Mathf.Lerp(1, GhostPetCompanion.DevourScale, amount);
            _rage.Sample(amount, t, .12f);
        }
    }
}
