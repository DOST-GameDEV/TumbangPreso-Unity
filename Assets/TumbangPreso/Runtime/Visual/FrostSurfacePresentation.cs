using UnityEngine;
using UnityEngine.Rendering;

namespace TumbangPreso.Visual
{
    /// <summary>Thin authored ice, fractures and a grounded boundary, with formation inside the live footprint.</summary>
    public sealed class FrostSurfacePresentation : MonoBehaviour, IVfxTimeline
    {
        public float Duration = 5.0f;
        public float LifeSeconds => Duration;
        private Material _skin, _veins, _edge;
        private float _elapsed;

        public static FrostSurfacePresentation Build(Transform parent, float radius, float duration)
        {
            var effect = parent.gameObject.AddComponent<FrostSurfacePresentation>();
            effect.Duration = duration;
            effect._skin = Part(parent, "FrozenSkin", "permafrost_skin", radius,
                new Color(.38f,.73f,.82f,.16f), 1, 0);
            effect._veins = Part(parent, "FrostVeins", "permafrost_veins", radius,
                new Color(.68f,.87f,.92f,.48f), 0, 1);
            effect._edge = Part(parent, "FrostEdge", "permafrost_edge", radius,
                new Color(.16f,.49f,.59f,.66f), 0, 2);
            effect.StepTo(0);
            return effect;
        }

        private static Material Part(Transform parent, string name, string asset, float radius,
                                     Color color, float glass, int order)
        {
            var source = Resources.Load<Mesh>("Models/Permafrost/" + asset);
            if (source == null || !source.isReadable)
                throw new System.InvalidOperationException("Missing readable permafrost mesh: " + asset);
            // Drape a private copy. Moving one field over a kerb must not deform
            // the imported source or a second field already elsewhere on the map.
            var mesh = Object.Instantiate(source);
            var go = VfxShapes.Lay(parent, name, mesh, radius, 0);
            VfxShapes.DrapeToGround(go, .003f);
            var shader = Shader.Find("TumbangPreso/FrostSurface");
            if (shader == null) throw new System.InvalidOperationException("Missing FrostSurface shader.");
            var material = new Material(shader);
            material.SetColor("_Color", color);
            material.SetFloat("_Glass", glass);
            var renderer = go.GetComponent<Renderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.sortingOrder = order;
            VfxRenderTag.Own(go, material);
            return material;
        }

        private void Update() => StepTo(_elapsed + Time.deltaTime);

        public void StepTo(float seconds)
        {
            _elapsed = seconds;
            float growth = Mathf.Lerp(.03f,1.12f,Mathf.SmoothStep(0,1,seconds/.28f));
            float remaining = Mathf.Max(0,Duration-seconds);
            float film = Mathf.Clamp01(remaining/.7f);
            Set(_skin,growth,film);
            Set(_veins,growth+.04f,Mathf.Clamp01(remaining/.45f));
            // The hazard is live immediately, so its whole boundary is readable
            // from the first frame while the interior freeze spreads inside it.
            Set(_edge,2,Mathf.Clamp01(remaining/.10f));
        }

        private static void Set(Material material, float growth, float opacity)
        {
            if (material == null) return;
            material.SetFloat("_Growth",growth);
            material.SetFloat("_Opacity",opacity);
        }
    }
}
