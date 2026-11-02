using System.Collections.Generic;
using UnityEngine;

namespace TumbangPreso.Visual
{
    /// <summary>
    /// ⚠️⚠️ TELEPORT, DIRECTION B, "SLEEPY COSMOS" (`PhaisterLook.Style.B`). A dark pool snaps open under her feet, a violet
    /// star-flecked swirl winds round her and she is swallowed into it; where she lands a second pool opens, the swirl rises
    /// out of it like a bud and blooms open with her standing inside.
    ///
    /// Built from the board's direction B (`drafts/phref/BOARD.md` section 3): Zoe's Sleepy Trouble Bubble for the values
    /// (a dark disc under a lighter swirl, two copies of one swirl running opposite ways and multiplied) and Konstantin
    /// Roshchin's "Teleport VFX LOL" (realtimevfx, Unity, no post) for the beat: dark swirl, then the pop.
    ///
    /// | Beat | Time | What |
    /// |---|---|---|
    /// | Open | 0.00 to 0.08 | the pool snaps open under her, a little past full and back; the swirl wraps her |
    /// | Swallow | 0.03 to 0.40 | her baked pose twists and sinks into the pool; the swirl closes down after her |
    /// | Close | 0.42 to 0.75 | the first pool is eaten away |
    /// | Bud | 0.10 to 0.30 | the second pool opens and the swirl rises out of it to over her head |
    /// | Bloom | 0.30 to 0.75 | its top flares open and is eaten from the top down; her body shows at `Reveal` |
    /// | Settle | 0.85 to 1.20 | the second pool is eaten away |
    ///
    /// Cost: five renderers, one material each, all one shader except the ghost: two pools (2 triangles each), two swirls
    /// (800 triangles each), the ghost (her own triangle count, two passes).
    /// </summary>
    public sealed class PhaisterCosmosPortal : MonoBehaviour, IVfxTimeline
    {
        public const float Reveal = 0.30f, Life = 1.20f, PoolRadius = 1.0f;

        private Material _poolOut, _poolIn, _swirlOut, _swirlIn;
        private GameObject _swirlOutGo, _swirlInGo, _poolOutGo, _poolInGo;
        private PhaisterGhostBody _ghost;
        private Vector3 _from, _to;
        private readonly List<Renderer> _hidden = new List<Renderer>();
        private bool _revealed;
        private float _age;
        public float LifeSeconds => Life;

        public static PhaisterCosmosPortal Play(Transform model, Vector3 from, Vector3 to)
        {
            var go = new GameObject("PhaisterCosmosPortal");
            var fx = go.AddComponent<PhaisterCosmosPortal>();
            fx._from = from; fx._to = to;
            fx._poolOut = fx.Part("PoolOut", PhaisterLook.Quad, 0f, from, out fx._poolOutGo);
            fx._poolIn = fx.Part("PoolIn", PhaisterLook.Quad, 0f, to, out fx._poolInGo);
            fx._swirlOut = fx.Part("SwirlOut", PhaisterLook.Shell, 3f, from, out fx._swirlOutGo);
            fx._swirlIn = fx.Part("SwirlIn", PhaisterLook.Shell, 3f, to, out fx._swirlInGo);
            fx._ghost = PhaisterGhostBody.Bake(model, go.transform, from, to, 1);
            PhaisterLook.Hide(model, fx._hidden);
            fx.StepTo(0f);
            return fx;
        }

        private Material Part(string name, Mesh mesh, float mode, Vector3 at, out GameObject go)
        {
            go = PhaisterLook.Draw(name, transform, mesh);
            var m = PhaisterLook.Paint(go, "PhaisterCosmos");
            if (m == null) return null;
            m.SetFloat("_Mode", mode);
            m.SetVector("_Centre", at);
            m.SetFloat("_Size", PoolRadius);
            return m;
        }

        private void Update()
        {
            StepTo(_age + Time.deltaTime);
            if (_age >= Life) Destroy(gameObject);
        }

        private static float Ease(float u) { u = Mathf.Clamp01(u); return 1f - (1f - u) * (1f - u); }

        // Snaps open a little past full and settles back: the pop that says "something opened".
        private static float Snap(float u)
        {
            u = Mathf.Clamp01(u);
            return u < 0.7f ? Ease(u / 0.7f) * 1.12f : Mathf.Lerp(1.12f, 1f, (u - 0.7f) / 0.3f);
        }

        public void StepTo(float t)
        {
            _age = t;
            Pool(_poolOut, _poolOutGo, t, Snap(t / 0.10f), Mathf.Clamp01((t - 0.42f) / 0.33f));
            Pool(_poolIn, _poolInGo, t, Snap((t - 0.10f) / 0.12f), Mathf.Clamp01((t - 0.85f) / 0.35f));

            // The swirl that swallows her: wraps her at once, then closes down into the pool as she sinks.
            float sink = Mathf.Clamp01((t - 0.03f) / 0.37f); sink *= sink;
            Swirl(_swirlOut, _swirlOutGo, t, t < 0.42f, 2.1f * (1f - sink) + 0.02f, 0.62f, Mathf.Lerp(0.45f, 0.15f, sink), 1f - Mathf.Clamp01(t / 0.06f));

            // The swirl she arrives in: rises like a bud, then its top flares open and is eaten from the top down.
            float rise = Ease((t - 0.10f) / 0.20f), bloom = Mathf.Clamp01((t - 0.30f) / 0.45f);
            Swirl(_swirlIn, _swirlInGo, t, t >= 0.10f && t < 0.78f, 0.02f + 2.2f * rise, Mathf.Lerp(0.62f, 0.85f, bloom), Mathf.Lerp(0.14f, 1.6f, Ease(bloom)), bloom);

            _ghost?.StepTo(t);
            if (!_revealed && t >= Reveal) { PhaisterLook.Show(_hidden); _revealed = true; }
        }

        private static void Pool(Material m, GameObject go, float t, float open, float erode)
        {
            bool on = open > 0.001f && erode < 0.999f;
            if (go != null && go.activeSelf != on) go.SetActive(on);
            if (m == null || !on) return;
            m.SetFloat("_Age", t); m.SetFloat("_Open", open); m.SetFloat("_Erode", erode);
        }

        private static void Swirl(Material m, GameObject go, float t, bool on, float height, float foot, float top, float erode)
        {
            if (go != null && go.activeSelf != on) go.SetActive(on);
            if (m == null || !on) return;
            m.SetFloat("_Age", t); m.SetFloat("_Erode", erode);
            m.SetVector("_Shell", new Vector4(height, foot, top, 3.5f));
        }

        private void OnDestroy()
        {
            if (!_revealed) PhaisterLook.Show(_hidden);
            _ghost?.Free();
        }
    }
}
