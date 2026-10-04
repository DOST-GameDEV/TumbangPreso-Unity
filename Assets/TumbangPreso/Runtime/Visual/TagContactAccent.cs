using UnityEngine;
using UnityEngine.Rendering;

namespace TumbangPreso.Visual
{
    // Three short angular ink ticks at the accepted hand contact. No particles,
    // words, gameplay randomness or colliders. The body and hand remain the lead.
    public sealed class TagContactAccent : MonoBehaviour
    {
        public const float Lifetime = .24f;
        private readonly LineRenderer[] _strokes = new LineRenderer[3];
        private readonly Vector3[] _outward = new Vector3[3];
        private Material _material;
        private float _age;
        private int _round;

        public static TagContactAccent Play(CharacterMotor tagger, CharacterMotor victim, Vector3 at)
        {
            if (!Finite(at)) return null;
            var settings = Settings.SettingsStore.Current;
            if (settings.EffectiveFlashIntensity <= .001f || WorldCueProfile.Current.InkEffects <= .001f)
                return null;
            if (GameServices.Round?.RoundActive != true) return null;
            var shader = Shader.Find("TumbangPreso/InkContact") ?? Shader.Find("Sprites/Default");
            if (shader == null) return null;

            // Match CharacterAnimator.PresentTagContact's torso surface, using the
            // accepted position rather than a victim already moved by recovery.
            var capsule = victim != null ? victim.GetComponent<CharacterController>() : null;
            var torso = victim != null ? victim.GetComponent<CharacterVisual>()?.TorsoBone : null;
            float height = torso != null ? torso.position.y - victim.transform.position.y + .12f
                : capsule != null ? capsule.center.y : .8f;
            Vector3 toward = tagger != null ? tagger.transform.position - at : Vector3.back;
            toward.y = 0;
            if (!Finite(toward) || toward.sqrMagnitude < .001f) toward = Vector3.back;
            toward.Normalize();
            float radius = capsule != null ? capsule.radius * .70f : .28f;
            var go = new GameObject("~TagContactAccent");
            go.transform.SetPositionAndRotation(at + Vector3.up * Mathf.Clamp(height, .3f, 1.2f)
                + toward * (radius + .025f), Quaternion.LookRotation(toward, Vector3.up));
            var cue = go.AddComponent<TagContactAccent>();
            cue._round = GameServices.Match != null ? GameServices.Match.RoundNumber : 0;
            cue._material = new Material(shader) { name = "Tag contact ink" };
            VfxRenderTag.Own(go, cue._material);
            cue.Stroke(0, "Upper contact tick", new Vector3(-.12f, .14f, 0), new Vector3(-.06f, .21f, 0), new Vector3(-.075f, .29f, .015f));
            cue.Stroke(1, "Lower contact tick", new Vector3(.12f, -.09f, 0), new Vector3(.20f, -.13f, 0), new Vector3(.24f, -.10f, -.015f));
            if (!settings.ReducedEffects)
                cue.Stroke(2, "Side contact tick", new Vector3(-.15f, -.045f, 0), new Vector3(-.23f, -.035f, 0), new Vector3(-.27f, .015f, .015f));
            cue.Sample(0);
            return cue;
        }

        private void Stroke(int index, string label, Vector3 a, Vector3 b, Vector3 c)
        {
            var go = new GameObject(label);
            go.transform.SetParent(transform, false);
            VfxRenderTag.Attach(go);
            var line = go.AddComponent<LineRenderer>();
            _strokes[index] = line;
            _outward[index] = (a + c).normalized;
            line.useWorldSpace = false;
            line.alignment = LineAlignment.TransformZ;
            line.positionCount = 3;
            line.SetPosition(0, a); line.SetPosition(1, b); line.SetPosition(2, c);
            line.startWidth = .032f; line.endWidth = .014f;
            line.numCapVertices = line.numCornerVertices = 0;
            line.shadowCastingMode = ShadowCastingMode.Off; line.receiveShadows = false;
            line.sharedMaterial = _material;
        }

        // Live catch playback hides the effect through the existing VfxRenderTag.
        // Sampling keeps visual capture independent of simulation time.
        public void Sample(float age)
        {
            _age = float.IsFinite(age) ? Mathf.Max(0, age) : Lifetime;
            float u = Mathf.Clamp01(_age / Lifetime);
            var settings = Settings.SettingsStore.Current;
            bool reduced = settings.ReducedUiMotion || settings.ReducedEffects;
            float weight = Mathf.Clamp01(WorldCueProfile.Current.InkEffects);
            float alpha = .78f * (1 - u) * (1 - u) * settings.EffectiveFlashIntensity * weight;
            if (_material.HasProperty("_Age")) _material.SetFloat("_Age", u);
            if (_material.HasProperty("_InkWeight")) _material.SetFloat("_InkWeight", weight);
            for (int i = 0; i < _strokes.Length; i++)
            {
                var line = _strokes[i]; if (line == null) continue;
                line.enabled = u < 1 && alpha > .001f;
                line.transform.localPosition = reduced ? Vector3.zero : _outward[i] * (.045f * u);
                Color ink = i == 1 ? new Color(.28f, .13f, .045f, alpha) : new Color(.96f, .63f, .18f, alpha);
                line.startColor = line.endColor = ink;
            }
        }

        private void Update()
        {
            if (GameServices.Round?.RoundActive != true || GameServices.Match == null ||
                GameServices.Match.RoundNumber != _round)
            { Destroy(gameObject); return; }
            Sample(_age + (PresentationClock.Held ? 0 : Time.deltaTime));
            if (_age >= Lifetime) Destroy(gameObject);
        }

        private static bool Finite(Vector3 value)
            => float.IsFinite(value.x) && float.IsFinite(value.y) && float.IsFinite(value.z);
    }
}
