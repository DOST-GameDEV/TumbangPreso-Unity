using TumbangPreso.Core;
using UnityEngine;

namespace TumbangPreso.Visual
{
    /// <summary>
    /// LIVE WIRE's aim (plan section 3, the tell): while he holds, a dotted gold line runs from his feet to where post B will
    /// drop, with a small ring there, sagging as the wire will. Doubled when overcharged. Ink-dark and broken when the spot
    /// cannot take a wire, so a refused release is never a surprise. Only the caster's own screen draws it.
    /// </summary>
    public sealed class ZackWireAim : MonoBehaviour
    {
        private const int Dashes = 9, RingPoints = 12;
        private readonly LineRenderer[] _dashes = new LineRenderer[Dashes];
        private LineRenderer _ring, _ringInk;

        public static ZackWireAim Create()
        {
            var go = new GameObject("ZackWireAim");
            var aim = go.AddComponent<ZackWireAim>();
            for (int i = 0; i < Dashes; i++) aim._dashes[i] = ZackFx.Line(go.transform, "Dash" + i, 2, 0.04f, ZackFx.Body, 1.2f);
            aim._ringInk = ZackFx.Line(go.transform, "SpotInk", RingPoints, 0.07f, ZackFx.Ink, 0.0f);
            aim._ring = ZackFx.Line(go.transform, "Spot", RingPoints, 0.035f, ZackFx.Core, 1.3f);
            aim._ring.loop = true; aim._ringInk.loop = true;
            go.SetActive(false);
            return aim;
        }

        public void Show(CharacterMotor caster, Vector3 at, bool valid, bool overcharged)
        {
            if (this == null || caster == null) return;
            gameObject.SetActive(true);
            var a = caster.transform.position;
            var colour = valid ? (overcharged ? ZackFx.Core : ZackFx.Body) : ZackFx.Ink;
            float wobble = valid ? 0.0f : 0.05f;
            for (int i = 0; i < Dashes; i++)
            {
                float t0 = i / (float)Dashes, t1 = t0 + 0.6f / Dashes;
                var p0 = Vector3.Lerp(a, at, t0) + Vector3.up * ZackRules.LiveWireHeightAt(t0);
                var p1 = Vector3.Lerp(a, at, t1) + Vector3.up * ZackRules.LiveWireHeightAt(t1);
                if (!valid) { p0.y -= wobble * (i % 2); p1.y += wobble * (i % 2); }
                _dashes[i].SetPosition(0, p0);
                _dashes[i].SetPosition(1, p1);
                _dashes[i].widthMultiplier = overcharged && valid ? 0.06f : 0.04f;
                ZackFx.Tint(_dashes[i], new Color(colour.r, colour.g, colour.b, 0.9f));
            }
            for (int i = 0; i < RingPoints; i++)
            {
                float ang = i / (float)RingPoints * Mathf.PI * 2.0f;
                var p = at + new Vector3(Mathf.Cos(ang) * 0.3f, 0.05f, Mathf.Sin(ang) * 0.3f);
                _ring.SetPosition(i, p);
                _ringInk.SetPosition(i, p + Vector3.down * 0.01f);
            }
            ZackFx.Tint(_ring, new Color(colour.r, colour.g, colour.b, 0.95f));
        }

        public void Release() { if (this != null) Destroy(gameObject); }
    }
}
