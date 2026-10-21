using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace TumbangPreso.Visual
{
    /// <summary>
    /// ⚠️ THE POSSESSED EYES, ON THE PAINTED EYES (LET THEM IN, 2026-10-14, `docs/reports/nemu-ultimate-2026-10-11/direction.md`
    /// section 16). The owner liked glowing eyes but not the glows that floated in front of the face, so this draws a hero's
    /// head a second time with `Shaders/NemuEyes`, which lights their own painted eyes violet. Which texels are the eyes comes
    /// from `Resources/Visual/eye-rects.txt` (`tools/author_eye_rects.py`), keyed by the atlas's name. A head whose atlas is not
    /// listed gets nothing. <see cref="Remove"/> puts the head's materials back.
    /// </summary>
    public sealed class NemuEyeGlow
    {
        private struct Rects { public Vector4 A, B; public Color Skin; }

        private static Dictionary<string, Rects> _rects;
        private readonly Renderer _head;
        private readonly Material[] _was;
        private readonly Material _glow;

        private NemuEyeGlow(Renderer head, Material[] was, Material glow) { _head = head; _was = was; _glow = glow; }

        /// <summary>The glow for the head among <paramref name="renderers"/> (the one named `head-mesh`), or null.</summary>
        public static NemuEyeGlow On(IEnumerable<Renderer> renderers)
        {
            if (renderers == null) return null;
            Renderer head = null;
            foreach (var r in renderers) if (r != null && r.name == "head-mesh") { head = r; break; }
            if (head == null || head.sharedMaterial == null) return null;
            var atlas = head.sharedMaterial.HasProperty("_MainTex") ? head.sharedMaterial.mainTexture : null;
            if (atlas == null || !Load().TryGetValue(atlas.name, out var rects)) return null;
            var shader = Resources.Load<Shader>("Shaders/NemuEyes");
            if (shader == null) return null;
            var glow = new Material(shader) { name = "NemuEyes" };
            glow.mainTexture = atlas;
            glow.SetVector("_EyeA", rects.A);
            glow.SetVector("_EyeB", rects.B);
            glow.SetColor("_Skin", rects.Skin);
            glow.SetFloat("_Amount", 0f);
            var was = head.sharedMaterials;
            var now = new Material[was.Length + 1];
            was.CopyTo(now, 0);
            now[was.Length] = glow;
            head.sharedMaterials = now;
            return new NemuEyeGlow(head, was, glow);
        }

        /// <summary>0 to 1: how much the eyes burn; <paramref name="clock"/> drives the pulse.</summary>
        public void Set(float amount, float clock)
        {
            _glow.SetFloat("_Amount", Mathf.Clamp01(amount));
            _glow.SetFloat("_Clock", clock);
        }

        public void Remove()
        {
            if (_head != null) _head.sharedMaterials = _was;
            if (_glow != null) { if (Application.isPlaying) Object.Destroy(_glow); else Object.DestroyImmediate(_glow); }
        }

        private static Dictionary<string, Rects> Load()
        {
            if (_rects != null) return _rects;
            _rects = new Dictionary<string, Rects>();
            var text = Resources.Load<TextAsset>("Visual/eye-rects");
            if (text == null) return _rects;
            foreach (var line in text.text.Split('\n'))
            {
                var f = line.Trim().Split(' ');
                if (f.Length < 12 || f[0].StartsWith("#")) continue;
                float N(int i) => float.Parse(f[i], CultureInfo.InvariantCulture);
                _rects[f[0]] = new Rects
                {
                    A = new Vector4(N(1), N(2), N(3), N(4)),
                    B = new Vector4(N(5), N(6), N(7), N(8)),
                    Skin = new Color(N(9), N(10), N(11), 1f),
                };
            }
            return _rects;
        }
    }
}
