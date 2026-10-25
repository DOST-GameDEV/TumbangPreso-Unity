using TumbangPreso.Core;
using TumbangPreso.Net;
using UnityEngine;

namespace TumbangPreso.Visual
{
    /// <summary>
    /// What LIVE WIRE looks like (plan section 3): two compact floor cleats and insulated reels at `LiveWirePostHeight`, a gold wire on an
    /// ink edge sagging to `LiveWireSagHeight` (the same `ZackRules.LiveWireHeightAt` the rules use), tangles of sparks at the
    /// anchors and a hum. Overcharged it is doubled and sparks more. It snaps (the halves drop to the court) or, at the end of
    /// its 10 s, goes slack, sags to the court and the posts sink. Render only: no collider (the primitives' are stripped).
    /// </summary>
    public sealed class ZackLiveWireLook : MonoBehaviour, IVfxTimeline
    {
        private const int Points = 17;
        private WorldEffectSnapshot.Field _state;
        private Transform _postA, _postB;
        private LineRenderer _ink, _wire, _second, _tangleA, _tangleB;
        private float _age, _snapAge = -1.0f;
        private bool _renderOnly;

        public float LifeSeconds => _state.Duration;

        public static ZackLiveWireLook Build(Transform parent, WorldEffectSnapshot.Field state, bool renderOnly = false)
        {
            var go = new GameObject("LiveWireLook");
            go.transform.SetParent(parent, false);
            go.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            var look = go.AddComponent<ZackLiveWireLook>();
            look._state = state;
            look._renderOnly = renderOnly;
            look._postA = Anchor(go.transform, state.Path[0], state.Forward, "AnchorA");
            look._postB = Anchor(go.transform, state.Path[1], -state.Forward, "AnchorB");
            look._ink = ZackFx.Line(go.transform, "WireInk", Points, 0.075f, ZackFx.Ink, 0.0f);
            look._wire = ZackFx.Line(go.transform, "Wire", Points, 0.04f, ZackFx.Body, 1.4f);
            if (state.SecondScale > 0.5f) look._second = ZackFx.Line(go.transform, "WireOvercharged", Points, 0.03f, ZackFx.Core, 1.6f);
            look._tangleA = ZackFx.Line(go.transform, "TangleA", 5, 0.025f, ZackFx.Core, 1.6f);
            look._tangleB = ZackFx.Line(go.transform, "TangleB", 5, 0.025f, ZackFx.Core, 1.6f);
            if (state.Split) look._snapAge = state.FirstScale;
            return look;
        }

        private static Transform Anchor(Transform parent, Vector3 foot, Vector3 facing, string name)
        {
            var root = new GameObject(name).transform; root.SetParent(parent, false);
            root.SetPositionAndRotation(foot, Quaternion.LookRotation(facing));
            void Part(string label, PrimitiveType type, Vector3 at, Vector3 scale, Color colour, Quaternion turn)
            {
                var go = GameObject.CreatePrimitive(type); go.name = label; VfxMaterial.StripCollider(go);
                go.transform.SetParent(root, false); go.transform.localPosition = at;
                go.transform.localScale = scale; go.transform.localRotation = turn;
                VfxMaterial.Solid(go.GetComponent<Renderer>(), colour);
            }
            var steel = new Color(.25f,.23f,.24f); var porcelain = new Color(.74f,.67f,.45f);
            Part("GroundCleat", PrimitiveType.Cube, new Vector3(0,.035f,0), new Vector3(.38f,.07f,.30f), steel, Quaternion.identity);
            Part("YokeLeft", PrimitiveType.Cube, new Vector3(-.11f,.14f,0), new Vector3(.06f,.22f,.13f), ZackFx.Ink, Quaternion.Euler(0,0,-18));
            Part("YokeRight", PrimitiveType.Cube, new Vector3(.11f,.14f,0), new Vector3(.06f,.22f,.13f), ZackFx.Ink, Quaternion.Euler(0,0,18));
            Part("InsulatorReel", PrimitiveType.Cylinder, new Vector3(0,.24f,0), new Vector3(.19f,.055f,.19f), porcelain, Quaternion.Euler(90,0,0));
            Part("LiveSocket", PrimitiveType.Cylinder, new Vector3(0,ZackRules.LiveWirePostHeight,.065f), new Vector3(.07f,.018f,.07f), ZackFx.Body, Quaternion.Euler(90,0,0));
            for(int i=0;i<2;i++)Part("FootBolt"+i,PrimitiveType.Cylinder,new Vector3(i==0?-.13f:.13f,.08f,-.09f),new Vector3(.045f,.015f,.045f),ZackFx.Ink,Quaternion.identity);
            return root;
        }

        /// <summary>The wire snapped: the halves drop from the crossing to the court.</summary>
        public void Snap()
        {
            if (_snapAge >= 0.0f) return;
            _snapAge = _age;
            if (!_renderOnly) ZackFx.Sparks(Point(0.5f, 0.0f), 0.45f, 4, _state.EventId);
            StepTo(_age);
        }

        /// <summary>A point on the wire at <paramref name="t"/> (0 post A, 1 post B), dropped by <paramref name="slack"/> (0 to 1).</summary>
        private Vector3 Point(float t, float slack)
        {
            var a = _state.Path[0]; var b = _state.Path[1];
            var p = Vector3.Lerp(a, b, t);
            float height = ZackRules.LiveWireHeightAt(t) * (1.0f - slack);
            return p + Vector3.up * Mathf.Max(0.03f, height);
        }

        public void StepTo(float seconds)
        {
            _age = Mathf.Max(0.0f, seconds);
            float left = _state.Duration - _age;
            bool snapped = _snapAge >= 0.0f;
            float fall = snapped ? Mathf.Clamp01((_age - _snapAge) / 0.35f) : 0.0f;
            // The last 0.6 s: it goes slack, sags to the court, and the posts sink.
            float slack = snapped ? fall : Mathf.Clamp01(1.0f - left / 0.6f);
            float sink = snapped ? Mathf.Clamp01((_age - _snapAge - 0.2f) / 0.4f) : Mathf.Clamp01(1.0f - left / 0.4f);
            bool reduced = Settings.SettingsStore.Current.ReducedEffects;
            // The taut snap on arrival: the wire whips out flat in the first 0.12 s and settles to its sag.
            float whip = Mathf.Clamp01(_age / 0.12f);
            for (int i = 0; i < Points; i++)
            {
                float t = i / (float)(Points - 1);
                float hang = snapped ? (t < 0.5f ? t / 0.5f : (1.0f - t) / 0.5f) : 1.0f;
                var p = Point(t, slack * hang);
                if (whip < 1.0f) p = Vector3.Lerp(Vector3.Lerp(_state.Path[0], _state.Path[1], t) + Vector3.up * ZackRules.LiveWirePostHeight, p, whip);
                float buzz = reduced ? 0.0f : 0.012f * Mathf.Sin(_age * 100.0f + i * 1.7f);
                _wire.SetPosition(i, p + Vector3.up * buzz);
                _ink.SetPosition(i, p + Vector3.up * buzz + Vector3.down * 0.01f);
                if (_second != null) _second.SetPosition(i, p + Vector3.up * (0.07f - buzz));
            }
            // A snapped wire is two halves: break the line at the middle by fading the two centre points.
            float alpha = snapped ? 1.0f - Mathf.Clamp01((_age - _snapAge - 0.3f) / 0.3f) : 1.0f;
            ZackFx.Tint(_wire, new Color(ZackFx.Body.r, ZackFx.Body.g, ZackFx.Body.b, alpha));
            ZackFx.Tint(_ink, new Color(ZackFx.Ink.r, ZackFx.Ink.g, ZackFx.Ink.b, alpha * 0.85f));
            if (_second != null) ZackFx.Tint(_second, new Color(ZackFx.Core.r, ZackFx.Core.g, ZackFx.Core.b, alpha));
            // The tangles: a short re-striking arc at each post top.
            int strike = reduced ? 0 : Mathf.FloorToInt(_age / (_second != null ? 0.06f : 0.09f));
            float tangle = snapped ? 0.0f : (Mathf.Repeat(_age, _second != null ? 0.06f : 0.09f) < 0.04f ? 0.95f : 0.25f);
            ZackFx.Jag(_tangleA, Point(0.0f, slack) + Vector3.up * 0.05f, Point(0.08f, slack), 0.06f, strike);
            ZackFx.Jag(_tangleB, Point(1.0f, slack) + Vector3.up * 0.05f, Point(0.92f, slack), 0.06f, strike + 7);
            ZackFx.Tint(_tangleA, new Color(ZackFx.Core.r, ZackFx.Core.g, ZackFx.Core.b, tangle));
            ZackFx.Tint(_tangleB, new Color(ZackFx.Core.r, ZackFx.Core.g, ZackFx.Core.b, tangle));
            float postDrop = ZackRules.LiveWirePostHeight * sink;
            _postA.position = _state.Path[0] + Vector3.up * (-postDrop);
            _postB.position = _state.Path[1] + Vector3.up * (-postDrop);
        }
    }
}
