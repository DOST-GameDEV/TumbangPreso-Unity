using UnityEngine;

namespace TumbangPreso.Visual
{
    /// <summary>
    /// ⚠️ THE ROSTER REWORK'S MODELLED PROPS (ABILITY-2): `tools/build_rework_props.py` types them part by part
    /// and writes `Resources/Models/ReworkProps`; this loads one and dresses it the way the cast is dressed
    /// (`ToonSkin`, the ink outline) in its power's own palette. The hex values match the builder's tables.
    /// </summary>
    public static class ReworkProp
    {
        public static readonly Color[] GeoPalette =
        {
            Hex(0x8A7A66), Hex(0x5E5244), Hex(0xB09C80), Hex(0xE8B43A), Hex(0xA87A1E), Hex(0xC8B89A), Hex(0x4A4036), Hex(0x7A6C5A),
            Hex(0x1E140C), Hex(0x3A3028), Hex(0xD8C8A8), Hex(0x6E6252), Hex(0xF2CE64), Hex(0x8C6440), Hex(0x553A22), Hex(0xB08450),
        };

        public static readonly Color[] VoodooPalette =
        {
            Hex(0xA9785F), Hex(0x74503F), Hex(0x2A1620), Hex(0xE24FA6), Hex(0xD8D0DA), Hex(0x1C1014), Hex(0xC2398F), Hex(0x7A1F5A),
            Hex(0x1E140C), Hex(0xF2E6DA), Hex(0x5A2A48), Hex(0x3A1830), Hex(0xFF8FD0), Hex(0x8C6440), Hex(0x553A22), Hex(0xB08450),
        };

        private static Color Hex(int rgb) => new Color(((rgb >> 16) & 255) / 255f, ((rgb >> 8) & 255) / 255f, (rgb & 255) / 255f, 1f);

        /// <summary>The prop <paramref name="name"/> under <paramref name="parent"/>, dressed; null if missing.</summary>
        public static GameObject Spawn(string name, Transform parent, Color[] palette)
        {
            var source = Resources.Load<GameObject>("Models/ReworkProps/" + name);
            if (source == null) { Debug.LogWarning("[ReworkProp] Models/ReworkProps/" + name + " is missing; run tools/build_rework_props.py."); return null; }
            var go = Object.Instantiate(source, parent, false);
            go.name = "ReworkProp-" + name;
            go.transform.localPosition = Vector3.zero; go.transform.localRotation = Quaternion.identity; go.transform.localScale = Vector3.one;
            foreach (var c in go.GetComponentsInChildren<Collider>(true)) { c.enabled = false; Object.Destroy(c); }
            foreach (var r in go.GetComponentsInChildren<Renderer>(true)) { VfxRenderTag.Attach(r.gameObject); r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; }
            ToonSkin.Apply(go, ToonSkin.PersonOutlineWidth, palette);
            return go;
        }

        public static Transform Find(GameObject model, string name) => PaeteProp.Find(model, name);
    }

    /// <summary>The barrier's slabs grind up out of the road one after another (0.1 s apart), overshoot and settle.</summary>
    public sealed class BarrierRise : MonoBehaviour
    {
        private Transform[] _slabs;
        private Vector3[] _rest;
        private float _age;
        public void Bind(GameObject model)
        {
            _slabs = new[] { ReworkProp.Find(model, "slab-0"), ReworkProp.Find(model, "slab-1"), ReworkProp.Find(model, "slab-2"), ReworkProp.Find(model, "seams") };
            _rest = new Vector3[_slabs.Length];
            for (int i = 0; i < _slabs.Length; i++) if (_slabs[i] != null) _rest[i] = _slabs[i].localPosition;
            Update();
        }
        private void Update()
        {
            if (_slabs == null) return;
            _age += Time.deltaTime;
            for (int i = 0; i < _slabs.Length; i++)
            {
                if (_slabs[i] == null) continue;
                float up = GrowthVfx.Pop((_age - (i == 3 ? 0.3f : 0.1f * i)) / 0.25f);
                _slabs[i].localPosition = _rest[i] + Vector3.down * 2.2f * (1f - up);
            }
        }
    }
}
