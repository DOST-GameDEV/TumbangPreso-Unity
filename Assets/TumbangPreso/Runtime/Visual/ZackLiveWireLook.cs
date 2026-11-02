using TumbangPreso.Core;
using TumbangPreso.Net;
using UnityEngine;

namespace TumbangPreso.Visual
{
    /// <summary>
    /// What LIVE WIRE looks like: two compact floor cleats and insulated reels at `LiveWirePostHeight`, a gold wire on an ink
    /// edge sagging to `LiveWireSagHeight` (the same `ZackRules.LiveWireHeightAt` the rules use), tangles of sparks at the
    /// anchors and a hum. Owner, 2026-10-26: it STAYS on the court after a trip, so it never sinks. Its states:
    ///
    /// * LIT: gold, buzzing, tangles striking at both sockets. Overcharged (strung while Overclocked, he still is) it doubles.
    /// * DARK (after a trip): the gold drains out to a dull grey copper line on its ink, the sockets go cold, and over the last
    ///   part of the dark window a single spark crawls in from both sockets to the middle; when they meet it relights.
    /// * REELED: the wire whips back into his hand and both cleats unbolt and follow it.
    ///
    /// Render only: no collider (the primitives' are stripped).
    ///
    /// PAINTED (2026-10-29, the owner: "its pretty poorly made so eventually we'll need to do a vfx revamp again"). With the
    /// switch on, the wire, its ink, the overcharged double, the tangles and the rearm crawl are drawn by `ZackWirePaint` in
    /// the approved Zack pieces (b13d's silk at a wire's width, z4's bolt inside it, b13d's spark bursts, z4's strike at a
    /// trip) on prebuilt meshes, instead of seven line renderers rewritten point by point every frame. Every state above
    /// reads the same; the cleats, the hum and the reel's timing are this class's own either way.
    /// </summary>
    public sealed class ZackLiveWireLook : MonoBehaviour, IVfxTimeline
    {
        /// <summary>
        /// ⚠️ THE SWITCH. On, the wire is painted (`ZackWirePaint`). Off, it is the rework's line renderers. Read when a wire
        /// is built (a live one, a replay's and a prewarm's alike). TO GO BACK, MAKE THE LINE BELOW `false`.
        /// </summary>
        public static bool Painted = true;
        private bool _painted;
        private ZackWirePaint _paint;
        /// <summary>Whether this wire draws the painted pieces (the switch as it stood when it was built).</summary>
        public bool DrawnPainted => _painted;
        private const int Points = 17;
        private static readonly Color DarkCopper = new Color(.42f, .36f, .3f);
        private WorldEffectSnapshot.Field _state;
        private Transform _postA, _postB;
        private Renderer[] _sockets;
        private LineRenderer _ink, _wire, _second, _tangleA, _tangleB, _crawlA, _crawlB;
        private float _age, _reelAge = -1.0f, _dischargeAge = -1.0f;
        private bool _renderOnly, _presented, _dark, _overcharged, _aimed;
        private float _rearm = 1.0f;
        private Vector3 _hand;
        private ZackCurrentAudio _current;

        public float LifeSeconds => _state.Duration;
        public bool DrawnDark => _dark;
        public bool DrawnOvercharged => _overcharged;
        public bool Reeling => _reelAge >= 0.0f;

        public static ZackLiveWireLook Build(Transform parent, WorldEffectSnapshot.Field state, bool renderOnly = false)
            => Build(parent, state, renderOnly, Painted);

        /// <summary>The same with the look named, for a film that draws both side by side.</summary>
        public static ZackLiveWireLook Build(Transform parent, WorldEffectSnapshot.Field state, bool renderOnly, bool painted)
        {
            var go = new GameObject("LiveWireLook");
            go.transform.SetParent(parent, false);
            go.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            var look = go.AddComponent<ZackLiveWireLook>();
            look._state = state;
            look._renderOnly = renderOnly;
            look._painted = painted;
            look._hand = (state.Path[0] + state.Path[1]) * 0.5f + Vector3.up;
            if (!renderOnly) look._current = ZackCurrentAudio.Attach(go.transform, "sfx_zack_livewire_loop");
            var socketA = new System.Collections.Generic.List<Renderer>();
            look._postA = Anchor(go.transform, state.Path[0], state.Forward, "AnchorA", socketA);
            look._postB = Anchor(go.transform, state.Path[1], -state.Forward, "AnchorB", socketA);
            look._sockets = socketA.ToArray();
            look._overcharged = state.SecondScale > 0.5f;
            if (painted) { look._paint = new ZackWirePaint(go.transform); return look; }
            look._ink = ZackFx.Line(go.transform, "WireInk", Points, 0.075f, ZackFx.Ink, 0.0f);
            look._wire = ZackFx.Line(go.transform, "Wire", Points, 0.04f, ZackFx.Body, 1.4f);
            look._second = ZackFx.Line(go.transform, "WireOvercharged", Points, 0.03f, ZackFx.Core, 1.6f);
            look._second.enabled = false;
            look._tangleA = ZackFx.Line(go.transform, "TangleA", 5, 0.025f, ZackFx.Core, 1.6f);
            look._tangleB = ZackFx.Line(go.transform, "TangleB", 5, 0.025f, ZackFx.Core, 1.6f);
            look._crawlA = ZackFx.Line(go.transform, "RearmCrawlA", 4, 0.03f, ZackFx.Core, 1.8f);
            look._crawlB = ZackFx.Line(go.transform, "RearmCrawlB", 4, 0.03f, ZackFx.Core, 1.8f);
            look._overcharged = state.SecondScale > 0.5f;
            return look;
        }

        private static Transform Anchor(Transform parent, Vector3 foot, Vector3 facing, string name, System.Collections.Generic.List<Renderer> sockets)
        {
            var root = new GameObject(name).transform; root.SetParent(parent, false);
            root.SetPositionAndRotation(foot, Quaternion.LookRotation(facing));
            Renderer Part(string label, PrimitiveType type, Vector3 at, Vector3 scale, Color colour, Quaternion turn)
            {
                var go = GameObject.CreatePrimitive(type); go.name = label; VfxMaterial.StripCollider(go);
                go.transform.SetParent(root, false); go.transform.localPosition = at;
                go.transform.localScale = scale; go.transform.localRotation = turn;
                var r = go.GetComponent<Renderer>(); VfxMaterial.Solid(r, colour); return r;
            }
            var steel = new Color(.25f,.23f,.24f); var porcelain = new Color(.74f,.67f,.45f);
            Part("GroundCleat", PrimitiveType.Cube, new Vector3(0,.035f,0), new Vector3(.38f,.07f,.30f), steel, Quaternion.identity);
            Part("YokeLeft", PrimitiveType.Cube, new Vector3(-.11f,.14f,0), new Vector3(.06f,.22f,.13f), ZackFx.Ink, Quaternion.Euler(0,0,-18));
            Part("YokeRight", PrimitiveType.Cube, new Vector3(.11f,.14f,0), new Vector3(.06f,.22f,.13f), ZackFx.Ink, Quaternion.Euler(0,0,18));
            Part("InsulatorReel", PrimitiveType.Cylinder, new Vector3(0,.24f,0), new Vector3(.19f,.055f,.19f), porcelain, Quaternion.Euler(90,0,0));
            sockets.Add(Part("LiveSocket", PrimitiveType.Cylinder, new Vector3(0,ZackRules.LiveWirePostHeight,.065f), new Vector3(.07f,.018f,.07f), ZackFx.Body, Quaternion.Euler(90,0,0)));
            for(int i=0;i<2;i++)Part("FootBolt"+i,PrimitiveType.Cylinder,new Vector3(i==0?-.13f:.13f,.08f,-.09f),new Vector3(.045f,.015f,.045f),ZackFx.Ink,Quaternion.identity);
            return root;
        }

        /// <summary>The live wire's state this frame, from `ZackLiveWire` (dark, rearm progress, overcharged, his hand).</summary>
        public void Present(bool dark, float rearm, bool overcharged, Vector3 hand, bool aimed = false)
        {
            _presented = true; _dark = dark; _rearm = Mathf.Clamp01(rearm); _overcharged = overcharged; _hand = hand; _aimed = aimed;
        }

        /// <summary>His own screen: he is looking at it within reel reach, so it is outlined and its sockets glint.</summary>
        public bool DrawnAimed => _aimed;

        /// <summary>It just tripped someone: a burst at the middle, then the gold drains out.</summary>
        public void Discharge()
        {
            _dischargeAge = _age;
            if (!_renderOnly && !_painted) ZackFx.Sparks(Point(0.5f, 0.0f), 0.45f, Settings.SettingsStore.Current.ReducedEffects ? 2 : 5, _state.EventId);
        }

        /// <summary>He reeled it in: the wire whips back to his hand and the cleats follow it.</summary>
        public void Reel()
        {
            if (_reelAge >= 0.0f) return;
            _reelAge = _age;
            if (!_renderOnly && !_painted) ZackFx.Sparks(_state.Path[0] + Vector3.up * .25f, .3f, 3, _state.EventId + 11);
        }

        /// <summary>A point on the wire at <paramref name="t"/> (0 post A, 1 post B).</summary>
        private Vector3 Point(float t, float slack)
        {
            var p = Vector3.Lerp(_state.Path[0], _state.Path[1], t);
            float height = ZackRules.LiveWireHeightAt(t) * (1.0f - slack);
            return p + Vector3.up * Mathf.Max(0.03f, height);
        }

        public void StepTo(float seconds)
        {
            _age = Mathf.Max(0.0f, seconds);
            if (!_presented)
            {
                // A recorded or render-only copy reads the field alone: dark from its discharge age, reeled from its flag.
                _dark = ZackRules.LiveWireDark(_state.FirstScale, _age) && _state.SecondScale < 0.5f;
                _rearm = _dark ? Mathf.Clamp01((_age - _state.FirstScale) / ZackRules.LiveWireDarkSeconds) : 1.0f;
                if (_state.Split && _reelAge < 0.0f) _reelAge = _age;
            }
            bool reduced = Settings.SettingsStore.Current.ReducedEffects;
            float reel = _reelAge >= 0.0f ? Mathf.Clamp01((_age - _reelAge) / Abilities.ZackLiveWire.ReelSeconds) : 0.0f;
            float reelEase = reel * reel * (3.0f - 2.0f * reel);
            _current?.Step((_state.Path[0] + _state.Path[1]) * .5f + Vector3.up * .25f, !_dark && reel <= 0.0f, _overcharged ? 1f : .75f);
            // Gold drains out over the first 0.25 s of dark; a rearm brightens back in its last 0.15 s.
            float lit = _dark ? Mathf.Clamp01(1.0f - (_dischargeAge >= 0.0f ? (_age - _dischargeAge) / 0.25f : 1.0f)) : 1.0f;
            float whip = Mathf.Clamp01(_age / 0.12f);
            if (_painted) { StepPainted(reduced, reel, reelEase, lit, whip); return; }
            var colour = Color.Lerp(DarkCopper, ZackFx.Body, lit);
            for (int i = 0; i < Points; i++)
            {
                float t = i / (float)(Points - 1);
                // Dark wires hang a touch slacker so the change reads even without colour.
                var p = Point(t, _dark ? 0.25f : 0.0f);
                if (whip < 1.0f) p = Vector3.Lerp(Vector3.Lerp(_state.Path[0], _state.Path[1], t) + Vector3.up * ZackRules.LiveWirePostHeight, p, whip);
                if (reel > 0.0f)
                {
                    // Reeling: every point is drawn toward his hand, the far end first, so it whips in like a measuring tape.
                    float pull = Mathf.Clamp01(reelEase * 1.6f - (1.0f - t) * 0.6f);
                    p = Vector3.Lerp(p, _hand, pull);
                }
                float buzz = reduced || _dark ? 0.0f : (_overcharged ? 0.02f : 0.012f) * Mathf.Sin(_age * 100.0f + i * 1.7f);
                _wire.SetPosition(i, p + Vector3.up * buzz);
                _ink.SetPosition(i, p + Vector3.up * buzz + Vector3.down * 0.01f);
                _second.SetPosition(i, p + Vector3.up * (0.07f - buzz));
            }
            float alpha = 1.0f - Mathf.Clamp01((reel - 0.8f) / 0.2f);
            ZackFx.Tint(_wire, new Color(colour.r, colour.g, colour.b, alpha));
            ZackFx.Tint(_ink, new Color(ZackFx.Ink.r, ZackFx.Ink.g, ZackFx.Ink.b, alpha * 0.85f));
            // Aimed at for a reel-in (his own screen only): a wider, pulsing ink outline so "this is the thing you will pick up" reads.
            float pulse = _aimed && reel <= 0.0f ? 0.5f + 0.5f * Mathf.Sin(_age * 14.0f) : 0.0f;
            _ink.widthMultiplier = 0.075f * (1.0f + 0.9f * pulse);
            if (_aimed) ZackFx.Tint(_wire, Color.Lerp(new Color(colour.r, colour.g, colour.b, alpha), new Color(1, .96f, .8f, alpha), 0.35f * pulse));
            _second.enabled = _overcharged && !_dark && reel <= 0.0f;
            if (_second.enabled) ZackFx.Tint(_second, new Color(ZackFx.Core.r, ZackFx.Core.g, ZackFx.Core.b, 1.0f));
            foreach (var socket in _sockets) ZackFx.Tint(socket, Color.Lerp(DarkCopper, ZackFx.Body, lit));
            // The tangles: a short re-striking arc at each socket while lit.
            float period = _overcharged ? 0.06f : 0.09f;
            int strike = reduced ? 0 : Mathf.FloorToInt(_age / period);
            float tangle = _dark || reel > 0.0f ? 0.0f : (Mathf.Repeat(_age, period) < 0.04f ? 0.95f : 0.25f);
            ZackFx.Jag(_tangleA, Point(0.0f, 0.0f) + Vector3.up * 0.05f, Point(0.08f, 0.0f), 0.06f, strike);
            ZackFx.Jag(_tangleB, Point(1.0f, 0.0f) + Vector3.up * 0.05f, Point(0.92f, 0.0f), 0.06f, strike + 7);
            ZackFx.Tint(_tangleA, new Color(ZackFx.Core.r, ZackFx.Core.g, ZackFx.Core.b, tangle));
            ZackFx.Tint(_tangleB, new Color(ZackFx.Core.r, ZackFx.Core.g, ZackFx.Core.b, tangle));
            // The rearm crawl: over the last 60% of the dark window one spark runs in from each socket; they meet at relight.
            float crawl = _dark ? Mathf.Clamp01((_rearm - 0.4f) / 0.6f) : 0.0f;
            bool crawling = crawl > 0.0f && reel <= 0.0f;
            _crawlA.enabled = crawling; _crawlB.enabled = crawling;
            if (crawling)
            {
                float u = 0.5f * crawl;
                ZackFx.Jag(_crawlA, Point(Mathf.Max(0, u - .05f), .25f) + Vector3.up * .02f, Point(u, .25f) + Vector3.up * .05f, .03f, strike + 3);
                ZackFx.Jag(_crawlB, Point(Mathf.Min(1, 1 - u + .05f), .25f) + Vector3.up * .02f, Point(1 - u, .25f) + Vector3.up * .05f, .03f, strike + 5);
            }
            StepCleats(reel);
        }

        // The cleats: planted, or unbolting and flying after the wire into his hand while it reels.
        private void StepCleats(float reel)
        {
            float follow = Mathf.Clamp01((reel - 0.35f) / 0.65f);
            float shrink = 1.0f - Mathf.Clamp01((reel - 0.7f) / 0.3f);
            _postA.position = Vector3.Lerp(_state.Path[0], _hand, follow * follow);
            _postB.position = Vector3.Lerp(_state.Path[1], _hand, follow);
            _postA.localScale = _postB.localScale = Vector3.one * Mathf.Max(0.001f, shrink);
        }

        // The painted wire: the same states as the lines above, from the same numbers, posed on `ZackWirePaint`.
        private void StepPainted(bool reduced, float reel, float reelEase, float lit, float whip)
        {
            // Dark wires hang a touch slacker so the change reads even without colour (the lines' 25 per cent).
            float slack = _dark ? 0.25f : 0.0f;
            float post = ZackRules.LiveWirePostHeight * (1.0f - slack);
            float sag = (ZackRules.LiveWirePostHeight - ZackRules.LiveWireSagHeight) * (1.0f - slack) * whip * (1.0f - reelEase);
            // Reeling: the far end first, so it whips in like a measuring tape (the lines' own pull, at their two ends).
            Vector3 a = Vector3.Lerp(_state.Path[0] + Vector3.up * post, _hand, Mathf.Clamp01(reelEase * 1.6f - 0.6f));
            Vector3 b = Vector3.Lerp(_state.Path[1] + Vector3.up * post, _hand, Mathf.Clamp01(reelEase * 1.6f));
            float alpha = 1.0f - Mathf.Clamp01((reel - 0.8f) / 0.2f);
            float pulse = _aimed && reel <= 0.0f ? 0.5f + 0.5f * Mathf.Sin(_age * 14.0f) : 0.0f;
            float crawl = _dark && reel <= 0.0f ? Mathf.Clamp01((_rearm - 0.4f) / 0.6f) : 0.0f;
            // A replay knows when it tripped from the field alone.
            float trippedAt = _dischargeAge >= 0.0f ? _dischargeAge : !_presented && _dark ? _state.FirstScale : -1.0f;
            float tripAgo = trippedAt >= 0.0f ? _age - trippedAt : -1.0f;
            var middle = (_state.Path[0] + _state.Path[1]) * 0.5f;
            bool live = !_dark && reel <= 0.0f;
            _paint.Pose(_age, a, b, sag, reel > 0.0f ? lit * alpha : lit, crawl, _overcharged && !_dark && reel <= 0.0f, pulse, alpha,
                        tripAgo, middle, live, reduced);
            foreach (var socket in _sockets) ZackFx.Tint(socket, Color.Lerp(DarkCopper, ZackFx.Body, lit));
            StepCleats(reel);
        }
    }
}
