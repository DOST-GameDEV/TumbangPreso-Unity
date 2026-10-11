using TumbangPreso.Visual;
using UnityEngine;

namespace TumbangPreso.CameraSystem
{
    /// <summary>
    /// The modelled things that live on the heroes' first-person hands (`ViewmodelArms.HandLife`): one model a hero in
    /// `Resources/Models/HandCompanions/<hero>.glb`, typed part by part by `tools/build_hand_<hero>.py` with
    /// `tools/hand_companion_kit.py`. This loads one and dresses it the way the cast is dressed (`ToonSkin`, the ink line)
    /// in the sixteen colours the hero's own class gives. Every part is a named node: find it with `Find` and pose it.
    /// </summary>
    public static class HandCompanionProp
    {
        public const string ResourceFolder = "Models/HandCompanions";
        /// <summary>The ink round a thing the size of a fist, a hand's length from the eye: finer than a body's.</summary>
        public const float InkWidth = ToonSkin.PersonOutlineWidth * .3f;

        public static Color Hex(int rgb) => new Color(((rgb >> 16) & 255) / 255f, ((rgb >> 8) & 255) / 255f, (rgb & 255) / 255f, 1f);

        /// <summary>
        /// The model <paramref name="hero"/> under <paramref name="parent"/>, dressed, shadowless, on the arms' layer; null
        /// when it is missing (the hands then play without it). Scale and place the returned object yourself.
        /// </summary>
        public static GameObject Spawn(string hero, Transform parent, Color[] palette, float inkWidth = InkWidth)
        {
            var source = HeroPropAssets.Load(ResourceFolder, hero);
            if (source == null)
            {
                Debug.LogWarning("[HandLife] " + ResourceFolder + "/" + hero + " is missing; run tools/build_hand_" + hero + ".py.");
                return null;
            }
            var go = Object.Instantiate(source, parent, false);
            go.name = "HandCompanion-" + hero;
            go.transform.localPosition = Vector3.zero; go.transform.localRotation = Quaternion.identity; go.transform.localScale = Vector3.one;
            foreach (var behaviour in go.GetComponentsInChildren<Behaviour>(true)) behaviour.enabled = false;
            foreach (var collider in go.GetComponentsInChildren<Collider>(true)) collider.enabled = false;
            ToonSkin.Apply(go, inkWidth, palette);
            foreach (var renderer in go.GetComponentsInChildren<Renderer>(true))
            {
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                renderer.receiveShadows = false;
                renderer.gameObject.layer = parent.gameObject.layer;
            }
            return go;
        }

        /// <summary>The part named <paramref name="name"/>, or null.</summary>
        public static Transform Find(GameObject model, string name)
        {
            if (model == null) return null;
            foreach (var t in model.GetComponentsInChildren<Transform>(true)) if (t.name == name) return t;
            return null;
        }

        /// <summary>A copy of one part (for a pool of pieces that fly off and come back), dressed as its source is.</summary>
        public static Transform Copy(Transform part, string name, Transform parent)
        {
            if (part == null) return null;
            var copy = Object.Instantiate(part.gameObject, parent, false);
            copy.name = name;
            return copy.transform;
        }

        public static void Kill(Object what)
        {
            if (what == null) return;
            if (Application.isPlaying) Object.Destroy(what); else Object.DestroyImmediate(what);
        }
    }
}
