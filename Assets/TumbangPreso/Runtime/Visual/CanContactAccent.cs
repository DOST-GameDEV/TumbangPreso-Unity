using UnityEngine;
using UnityEngine.Rendering;

namespace TumbangPreso.Visual
{
    // Open strokes around the object, never a filled safe-area ring. Existing
    // can state/physical motion, material SFX and HUD attribution remain the lead.
    public sealed class CanContactAccent : MonoBehaviour
    {
        private readonly LineRenderer[] _strokes = new LineRenderer[3];
        private bool _restore;
        private float _began;
        private int _round;
        private Color _colour;
        private const float Life = .38f;
        public static void Play(Vector3 at, bool restored)
        {
            var round = GameServices.Round;
            if (round == null || !round.RoundActive || Settings.SettingsStore.Current.EffectiveFlashIntensity <= .001f) return;
            var go = new GameObject(restored ? "~CanRestoreAccent" : "~TinContactAccent");
            go.transform.position = at;
            var effect = go.AddComponent<CanContactAccent>();
            effect._restore = restored; effect._began = Time.unscaledTime;
            effect._round = GameServices.Match != null ? GameServices.Match.RoundNumber : 0;
            effect._colour = restored ? new Color(1, .77f, .26f) : new Color(.91f, .94f, .84f);
            for (int i = 0; i < effect._strokes.Length; i++)
            {
                var child = new GameObject("ObjectStroke" + i); child.transform.SetParent(go.transform, false);
                var line = child.AddComponent<LineRenderer>(); effect._strokes[i] = line;
                line.useWorldSpace = false; line.positionCount = 5; line.widthMultiplier = .022f;
                line.numCapVertices = 2; line.shadowCastingMode = ShadowCastingMode.Off; line.receiveShadows = false;
                var material = new Material(Shader.Find("Sprites/Default")) { name = "Can object stroke" };
                line.sharedMaterial = material; VfxRenderTag.Own(child, material);
            }
            effect.Draw(0);
        }
        private void Update()
        {
            float age = Time.unscaledTime - _began;
            if (age >= Life || GameServices.Round == null || !GameServices.Round.RoundActive ||
                GameServices.Match == null || GameServices.Match.RoundNumber != _round)
            { Destroy(gameObject); return; }
            Draw(age / Life);
        }
        private void Draw(float u)
        {
            // The can stays visible in the centre. Restore brackets lift into the
            // upright shape; contact strokes spread a short distance and dissolve.
            float alpha = (1 - u) * (1 - u) * .72f * Settings.SettingsStore.Current.EffectiveFlashIntensity;
            Color colour = _colour; colour.a = alpha;
            for (int i = 0; i < _strokes.Length; i++)
            {
                var line = _strokes[i]; if (line == null) continue;
                line.startColor = line.endColor = colour;
                Quaternion around = Quaternion.Euler(0, i * 120, 0);
                float side = _restore ? .30f + .05f * u : .27f + .32f * u;
                float lift = _restore ? .05f + .12f * u : .16f;
                for (int k = 0; k < 5; k++)
                {
                    float angle = (k / 4f - .5f) * 120 * Mathf.Deg2Rad;
                    line.SetPosition(k, around * new Vector3(Mathf.Cos(angle) * side,
                        lift + (_restore ? k * .075f : Mathf.Sin(angle) * .13f), Mathf.Sin(angle) * .12f));
                }
            }
        }
    }
}
