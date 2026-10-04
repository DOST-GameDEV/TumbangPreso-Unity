using UnityEngine;
using UnityEngine.Rendering;

namespace TumbangPreso.Visual
{
    // One short mark at the host-resolved powered shoe contact. Never a collider.
    public sealed class BankShotContact : MonoBehaviour
    {
        public const float FlairStrength = 1f;
        public const float Lifetime = .22f;
        private readonly LineRenderer[] _strokes = new LineRenderer[3];
        private float _age;
        private bool _reduced;

        public static BankShotContact Spawn(Vector3 at, bool low, bool reduced)
        {
            if (!float.IsFinite(at.x) || !float.IsFinite(at.y) || !float.IsFinite(at.z)) return null;
            var go = new GameObject("ZackBankContact"); go.transform.position = at;
            var cue = go.AddComponent<BankShotContact>(); cue._reduced = reduced;
            var shader = Shader.Find("Sprites/Default");
            if (shader == null) { Destroy(go); return null; }
            var material = new Material(shader) { name = "Bank contact gold" };
            VfxRenderTag.Own(go, material);
            cue._strokes[0] = cue.Stroke("Toe snap", material, new[] {
                new Vector3(-.04f,.015f,0), new Vector3(.06f,.08f,.025f), new Vector3(.20f,.035f,.055f) });
            cue._strokes[1] = cue.Stroke("Heel catch", material, new[] {
                new Vector3(.015f,-.015f,0), new Vector3(-.055f,-.075f,-.045f), new Vector3(-.16f,-.045f,-.11f) });
            if (!low) cue._strokes[2] = cue.Stroke("Side tick", material, new[] {
                new Vector3(0,.025f,-.015f), new Vector3(-.025f,.105f,.065f), new Vector3(.015f,.16f,.13f) });
            cue.StepTo(0);
            return cue;
        }

        private LineRenderer Stroke(string label, Material material, Vector3[] points)
        {
            var go = new GameObject(label); go.transform.SetParent(transform, false);
            VfxRenderTag.Attach(go);
            var line = go.AddComponent<LineRenderer>(); line.useWorldSpace = false;
            line.positionCount = points.Length; line.SetPositions(points);
            line.sharedMaterial = material; line.widthMultiplier = .025f;
            line.numCapVertices = 0; line.numCornerVertices = 0;
            line.shadowCastingMode = ShadowCastingMode.Off; line.receiveShadows = false;
            return line;
        }

        public void StepTo(float age)
        {
            _age = float.IsFinite(age) ? Mathf.Max(0, age) : Lifetime;
            float t = Mathf.Clamp01(_age / Lifetime);
            float alpha = (1 - t) * (1 - t);
            transform.localScale = Vector3.one * (_reduced ? 1 : Mathf.Lerp(.8f, 1.35f, t));
            var head = new Color(1, .86f, .16f, alpha);
            var tail = new Color(1, .59f, .08f, alpha * .7f);
            foreach (var line in _strokes)
            {
                if (line == null) continue;
                line.enabled = t < 1; line.startColor = head; line.endColor = tail;
            }
        }

        private void Update()
        {
            StepTo(_age + Time.deltaTime);
            if (_age >= Lifetime) Destroy(gameObject);
        }
    }
}
