using UnityEngine;

namespace TumbangPreso.Visual
{
    // Two contact tracks and one intermittent cross-discharge mark skate motion.
    // The warning stays at full reach while its brightness decays.
    public sealed class ZackSkateWake : MonoBehaviour
    {
        private readonly LineRenderer[] _lines = new LineRenderer[3];
        private float _duration, _age;

        public static void Build(Transform parent, float radius, float duration, Vector3 forward)
        {
            var go = new GameObject("SkateContactWake"); go.transform.SetParent(parent, false);
            forward.y = 0; go.transform.localRotation = forward.sqrMagnitude > .001f ? Quaternion.LookRotation(forward) : Quaternion.identity;
            var wake = go.AddComponent<ZackSkateWake>(); wake._duration = duration;
            float seed = parent.position.x * 13.7f + parent.position.z * 4.1f;
            for (int lane = 0; lane < 3; lane++)
            {
                var child = new GameObject(lane < 2 ? "SkateTrack_" + lane : "CrossDischarge"); child.transform.SetParent(go.transform, false);
                var line = child.AddComponent<LineRenderer>(); line.useWorldSpace = true;
                line.positionCount = lane < 2 ? 9 : 7; line.widthMultiplier = lane < 2 ? .025f : .012f;
                line.numCornerVertices = 0; line.numCapVertices = 0;
                for (int i = 0; i < line.positionCount; i++)
                {
                    float t = i / (float)(line.positionCount - 1);
                    Vector3 local = lane < 2
                        ? new Vector3((lane == 0 ? -1 : 1) * (.55f + .09f * Mathf.Sin(t * 3 + seed)), 0, Mathf.Lerp(-.62f, .62f, t))
                        : new Vector3(Mathf.Lerp(-.93f, .93f, t), 0, .08f * Mathf.Sin(i * 2.3f + seed));
                    var point = VfxShapes.GroundPoint(go.transform.TransformPoint(local * radius));
                    line.SetPosition(i, point + Vector3.up * .014f);
                }
                VfxMaterial.Ghost(line, Color.white, .25f); wake._lines[lane] = line;
            }
        }

        private void Update() => StepTo(_age + Time.deltaTime);
        public void StepTo(float seconds)
        {
            _age = Mathf.Max(0, seconds);
            float fade = Mathf.Clamp01((_duration - _age) / .45f);
            for (int i = 0; i < _lines.Length; i++)
            {
                float pulse = i == 2 ? Mathf.Pow(Mathf.Max(0, Mathf.Sin(_age * 15)), 6) * .65f : .55f;
                var colour = new Color(1, .86f, .16f, fade * pulse);
                _lines[i].sharedMaterial.color = colour;
                _lines[i].sharedMaterial.SetColor("_BaseColor", colour);
            }
        }
    }
}
