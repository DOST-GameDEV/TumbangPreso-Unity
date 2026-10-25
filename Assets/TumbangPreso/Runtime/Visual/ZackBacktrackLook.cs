using TumbangPreso.Core;
using UnityEngine;

namespace TumbangPreso.Visual
{
    /// <summary>
    /// BACKTRACK's public half (plan section 3; owner, 2026-10-25: everyone sees the rewind trail and landing ring when he
    /// casts). The snap is instant; this is drawn after it: a gold streak runs BACKWARDS along the path he really ran, in
    /// 0.3 s, into the landing spot, where a ring of sparks holds 0.4 s and breaks into four. Render only; it decides nothing.
    /// </summary>
    public sealed class ZackBacktrackLook : MonoBehaviour
    {
        private const float Rewind = 0.3f, RingHold = 0.4f;
        private const int RingPoints = 13;
        private Vector3[] _path;
        private Vector3 _to;
        private LineRenderer _streak, _streakInk, _ring, _ringInk;
        private float _age;
        private bool _broke;

        public static void Play(CharacterMotor motor, Vector3 from, Vector3 to, Vector3[] path)
        {
            var go = new GameObject("ZackBacktrack");
            var look = go.AddComponent<ZackBacktrackLook>();
            // Newest last in the ring's order; the rewind runs from his old body (`from`) back to the landing (`to`).
            var points = new System.Collections.Generic.List<Vector3>();
            points.Add(from);
            if (path != null) for (int i = path.Length - 1; i >= 0; i--) points.Add(path[i]);
            points.Add(to);
            look._path = points.ToArray();
            look._to = to;
            int n = Mathf.Clamp(look._path.Length, 2, 80);
            look._streakInk = ZackFx.Line(go.transform, "RewindInk", n, 0.12f, ZackFx.Ink, 0.0f);
            look._streak = ZackFx.Line(go.transform, "Rewind", n, 0.06f, ZackFx.Core, 1.5f);
            look._ringInk = ZackFx.Line(go.transform, "LandingRingInk", RingPoints, 0.08f, ZackFx.Ink, 0.0f);
            look._ring = ZackFx.Line(go.transform, "LandingRing", RingPoints, 0.04f, ZackFx.Body, 1.4f);
            look._ring.loop = true; look._ringInk.loop = true;
            if (motor != null) ZackFx.Sparks(from + Vector3.up * 0.9f, 0.35f, 3, motor.PlayerSlot);
            look.Step(0.0f);
        }

        private void Update()
        {
            _age += Time.deltaTime;
            if (_age > Rewind + RingHold + 0.1f) { Destroy(gameObject); return; }
            Step(_age);
        }

        private void Step(float age)
        {
            // The head of the streak runs from `from` to `to` along the path; its tail follows 0.12 s behind.
            float head = Mathf.Clamp01(age / Rewind), tail = Mathf.Clamp01((age - 0.12f) / Rewind);
            int n = _streak.positionCount;
            for (int i = 0; i < n; i++)
            {
                float t = Mathf.Lerp(tail, head, i / (float)(n - 1));
                var p = Sample(t) + Vector3.up * (0.25f + 0.04f * Mathf.Sin(i * 2.9f + age * 60.0f));
                _streak.SetPosition(i, p);
                _streakInk.SetPosition(i, p + Vector3.down * 0.015f);
            }
            float streakAlpha = head >= 1.0f && tail >= 1.0f ? 0.0f : 1.0f;
            ZackFx.Tint(_streak, new Color(ZackFx.Core.r, ZackFx.Core.g, ZackFx.Core.b, streakAlpha));
            ZackFx.Tint(_streakInk, new Color(ZackFx.Ink.r, ZackFx.Ink.g, ZackFx.Ink.b, streakAlpha * 0.8f));
            // The landing ring: it arrives with the head and holds 0.4 s, then breaks into four sparks.
            float ring = age < Rewind ? 0.0f : Mathf.Clamp01(1.0f - (age - Rewind) / RingHold);
            for (int i = 0; i < RingPoints; i++)
            {
                float a = i / (float)RingPoints * Mathf.PI * 2.0f;
                float r = 0.55f + ((i % 2 == 0) ? 0.05f : -0.05f);
                var p = _to + new Vector3(Mathf.Cos(a) * r, 0.05f, Mathf.Sin(a) * r);
                _ring.SetPosition(i, p);
                _ringInk.SetPosition(i, p + Vector3.down * 0.01f);
            }
            ZackFx.Tint(_ring, new Color(ZackFx.Body.r, ZackFx.Body.g, ZackFx.Body.b, ring));
            ZackFx.Tint(_ringInk, new Color(ZackFx.Ink.r, ZackFx.Ink.g, ZackFx.Ink.b, ring * 0.8f));
            if (!_broke && age >= Rewind + RingHold)
            {
                _broke = true;
                ZackFx.Sparks(_to + Vector3.up * 0.1f, 0.35f, 4, _to.x * 7.0f + _to.z);
            }
        }

        private Vector3 Sample(float t)
        {
            if (_path.Length < 2) return _to;
            float f = t * (_path.Length - 1);
            int i = Mathf.Clamp(Mathf.FloorToInt(f), 0, _path.Length - 2);
            return Vector3.Lerp(_path[i], _path[i + 1], f - i);
        }
    }
}
