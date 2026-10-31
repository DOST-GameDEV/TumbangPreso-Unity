using System.Collections.Generic;
using TumbangPreso.Core;
using UnityEngine;

namespace TumbangPreso.Visual
{
    /// <summary>
    /// CURSE: DRAIN, "MOTH TO A FLAME". Their stamina is a magenta flame lit on their head, and her moths eat it. Chosen over a
    /// wrung-cloth look and a swarm feeding on the body because it reads in one glance from every camera, stands above the body so
    /// no model hides it, and stays out of the victim's own lens.
    ///
    /// Every beat is the ability's own (`VoodooRules`, `StatusRules`), on one timeline from the start of her reach:
    ///
    /// | Beat | Time | What a player reads |
    /// |---|---|---|
    /// | REACH | 0 to 2 | the flame is drawn up out of their head, its light streams along her teleport's own arc to her hand, moths leave her hand one by one and circle it |
    /// | MARK | 2 to 3.5 | the circle closes on the flame and spins up, wings filling with its colour: that spiral IS the 1.5 s clock |
    /// | SNUFF | 3.5 | stamina to zero: the flame is pinched out in three frames, a puff of her dark cloud, the moths burst off it and curve home with its light |
    /// | DRAINED | 3.5 to 6 | no refill for 2.5 s: a dead wick smoking on their head, an ember that cannot catch |
    /// | REFILL | 6 | a small flame catches and is gone: their stamina recovers again |
    /// | BROKEN | any time in the reach | the flame sinks back into them, the moths lose the pull and drift off, eaten |
    ///
    /// No mesh is rebuilt after it is made; each frame sets a few material floats and moves transforms. Cost is measured in the
    /// round log (`Triangles` below adds it up).
    /// </summary>
    public sealed class PhaisterDrainLook
    {
        public const float ReachEnd = VoodooRules.ReachSeconds, SnapAt = ReachEnd + VoodooRules.DrainDelaySeconds,
                           RefillAt = SnapAt + StatusRules.DrainedSeconds, End = RefillAt + 0.45f;

        /// <summary>The flame's height and width (m) at full size.</summary>
        public const float FlameHeight = 1.45f, FlameWidth = 0.50f;

        private static Mesh _flameMesh, _mothMesh;

        // Typed by hand: wide lanes first, so the first moth out is the one that draws the circle.
        // Delay, radius share, height, phase (degrees), size, beats a second.
        private static readonly float[,] Rows =
        {
            { 0.10f, 1.00f,  0.10f,   0f, 0.86f, 4.6f },
            { 0.32f, 0.86f,  0.42f, 128f, 0.74f, 5.2f },
            { 0.52f, 1.08f, -0.18f, 262f, 0.82f, 4.1f },
            { 0.74f, 0.90f,  0.30f,  52f, 0.70f, 5.6f },
            { 0.94f, 1.02f, -0.06f, 188f, 0.78f, 4.8f },
            { 1.16f, 0.82f,  0.52f, 316f, 0.66f, 5.0f },
            { 1.36f, 1.12f,  0.20f,  96f, 0.72f, 4.4f },
        };
        private const int Motes = 24;

        private readonly GameObject _root;
        private readonly Material _moths, _flame;
        private readonly GameObject _mothGo, _flameGo;
        private readonly FxPiece _halo, _flash, _handFlash;
        private readonly List<FxPiece> _stream = new List<FxPiece>();
        private readonly Transform _streamAt;
        private Vector3 _streamFrom;
        private readonly FxCloud _wick, _puff;
        private readonly List<Object> _owned = new List<Object>();

        public GameObject Root => _root;

        public PhaisterDrainLook(Transform parent)
        {
            _root = new GameObject("PhaisterDrainLook");
            _root.transform.SetParent(parent, false);

            if (_mothMesh == null) _mothMesh = BuildMoths();
            _mothGo = PhaisterLook.Draw("Moths", _root.transform, _mothMesh);
            _moths = PhaisterLook.Paint(_mothGo, "PhaisterDrainMoths");
            _moths?.SetFloat("_Mode", 0f);

            if (_flameMesh == null) _flameMesh = BuildFlame();
            _flameGo = new GameObject("Flame");
            _flameGo.transform.SetParent(_root.transform, false);
            _flameGo.AddComponent<MeshFilter>().sharedMesh = _flameMesh;
            _flameGo.AddComponent<MeshRenderer>();
            VfxRenderTag.Attach(_flameGo);
            _flame = PhaisterLook.Paint(_flameGo, "PhaisterDrainFlame");
            if (_flame != null) { _flame.SetTexture("_NoiseTex", FxStudyKit.Mask("erode_streak")); _flame.SetTexture("_CloudTex", FxStudyKit.Mask("erode_cloud")); }

            _halo = PhaisterLook.Glow(_root.transform, "Halo", "glow_soft", new Color(0.95f, 0.25f, 0.75f), new Color(0.50f, 0.08f, 0.62f), 6, false, _owned);
            _flash = PhaisterLook.Glow(_root.transform, "Flash", "flare_star", new Color(1f, 0.82f, 0.95f), new Color(0.85f, 0.18f, 0.72f), 14, false, _owned);
            _handFlash = PhaisterLook.Glow(_root.transform, "HandFlash", "glow_soft", new Color(1f, 0.55f, 0.88f), new Color(0.72f, 0.12f, 0.70f), 14, false, _owned);

            // The stream: her teleport's own path material (`PhaisterMothRibbon.BuildPath`), the same crossed pair and the same
            // colours, here running from the flame to her hand. A unit along z, a fixed bow, stretched by its transform.
            var streamGo = new GameObject("Stream");
            streamGo.transform.SetParent(_root.transform, false);
            _streamAt = streamGo.transform;
            var knots = new List<FxStudyKit.Knot>();
            for (int i = 0; i <= 8; i++)
            {
                float u = i / 8f;
                knots.Add(new FxStudyKit.Knot(new Vector3(0f, Mathf.Sin(u * Mathf.PI) * 0.62f, u), Mathf.Lerp(0.18f, 0.52f, Mathf.Sin(u * Mathf.PI)), u * 160f));
            }
            int order = 0;
            foreach (var face in new[] { Vector3.up, Vector3.right })
            {
                var mesh = FxStudyKit.Sweep(knots, face, 10, 6, 0.15f);
                var piece = new FxPiece("Path", _streamAt, mesh, FxStudyKit.Ribbon("trail_smoke", "erode_cloud"), 4 + order++);
                piece.Colours(new Color(0.16f, 0.05f, 0.26f), new Color(0.46f, 0.17f, 0.68f), new Color(0.98f, 0.62f, 0.95f), new Color(0.50f, 0.20f, 0.78f))
                     .Set("_Scroll", 1.7f, 0f).Set("_Tile", 1.6f, 1f).Set("_Cut", 0.26f, 0.58f).Set("_Stops", 0.40f, 0.68f, 0.95f)
                     .Set("_Ends", 0.22f, 0.22f, 0.36f).Set("_Add", 0.30f).Set("_Halo", 0.5f).Set("_Soft", 0.14f).Set("_Sweep", 1.2f)
                     .Set("_ErodeTile", 1.4f, 1f).Set("_ErodeScroll", 0.4f, 0f).WindowSoft(0.12f);
                _stream.Add(piece);
                _owned.Add(mesh); _owned.Add(piece.Material);
            }

            // Smoke is the approved cloud, called and not copied, in her dark violet, with no ink line.
            Color lit = new Color(0.34f, 0.11f, 0.50f), shade = new Color(0.14f, 0.045f, 0.25f), line = new Color(0.08f, 0.02f, 0.14f);
            _wick = FxCloud.Build(_root.transform, "WickSmoke", 31, 56, new Color(0.56f, 0.32f, 0.70f), new Color(0.46f, 0.24f, 0.62f), line, 0f, 0.022f, 0.052f);
            _puff = FxCloud.Build(_root.transform, "SnuffPuff", 37, 36, new Color(0.50f, 0.26f, 0.66f), new Color(0.36f, 0.16f, 0.52f), line, 0f, 0.05f, 0.13f);
            _root.SetActive(false);
        }

        private static Mesh BuildMoths()
        {
            var v = new List<Vector3>(); var uv0 = new List<Vector4>(); var uv1 = new List<Vector4>(); var uv2 = new List<Vector4>(); var t = new List<int>();
            void Card(float part, int row, float size, float hz, Vector4 lane)
            {
                int at = v.Count;
                for (int c = 0; c < 4; c++)
                {
                    v.Add(Vector3.zero);
                    uv0.Add(new Vector4(c == 1 || c == 2 ? 1f : 0f, c >= 2 ? 1f : 0f, 0f, 0f));
                    uv1.Add(new Vector4(part, row, size, hz));
                    uv2.Add(lane);
                }
                t.Add(at); t.Add(at + 2); t.Add(at + 1); t.Add(at); t.Add(at + 3); t.Add(at + 2);
            }
            for (int r = 0; r < Rows.GetLength(0); r++)
            {
                var lane = new Vector4(Rows[r, 0], Rows[r, 1], Rows[r, 2], Rows[r, 3] * Mathf.Deg2Rad);
                foreach (float part in new[] { -1f, 1f, 0f }) Card(part, r, Rows[r, 4], Rows[r, 5], lane);
            }
            for (int m = 0; m < Motes; m++)
                Card(2f, m, 0.7f + 0.8f * FxStudyKit.Hash(m, 5), 0f, new Vector4(0f, 0f, 0f, (m + FxStudyKit.Hash(m, 9) * 0.6f) / Motes));
            var mesh = new Mesh { name = "PhaisterDrainMoths", hideFlags = HideFlags.HideAndDontSave };
            mesh.SetVertices(v); mesh.SetUVs(0, uv0); mesh.SetUVs(1, uv1); mesh.SetUVs(2, uv2); mesh.SetTriangles(t, 0);
            mesh.bounds = new Bounds(Vector3.zero, Vector3.one * 2000f);
            return mesh;
        }

        /// <summary>
        /// Three teardrop skins, one height: uv (round, up), vertex alpha the layer (`PhaisterDrainFlame.shader`). Foot at the
        /// origin, tip at 1. Drawn outer first, so the heart is on top of everything from every side.
        /// </summary>
        private static Mesh BuildFlame()
        {
            var v = new List<Vector3>(); var n = new List<Vector3>(); var uv = new List<Vector2>(); var col = new List<Color>(); var t = new List<int>();
            const int Round = 28, Up = 16;
            float[] width = { 0.50f, 0.36f, 0.13f }, tall = { 1.00f, 0.86f, 0.42f }, layer = { 0f, 0.5f, 1f };
            for (int s = 0; s < 3; s++)
            {
                int start = v.Count;
                for (int j = 0; j <= Up; j++)
                {
                    float h = j / (float)Up;
                    // A teardrop: full low down, drawn out to a point.
                    float r = width[s] * Mathf.Pow(Mathf.Sin(Mathf.Pow(h, 0.62f) * Mathf.PI), 0.85f) * (1f - 0.25f * h);
                    if (j == Up) r = 0f;
                    for (int i = 0; i <= Round; i++)
                    {
                        float a = i / (float)Round * Mathf.PI * 2f;
                        var radial = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
                        v.Add(radial * r + Vector3.up * h * tall[s]);
                        n.Add((radial + Vector3.up * (h - 0.35f) * 0.9f).normalized);
                        uv.Add(new Vector2(i / (float)Round, h));
                        col.Add(new Color(1f, 1f, 1f, layer[s]));
                    }
                }
                for (int j = 0; j < Up; j++)
                    for (int i = 0; i < Round; i++)
                    {
                        int a = start + j * (Round + 1) + i, b = a + 1, c = a + Round + 1, d = c + 1;
                        t.Add(a); t.Add(c); t.Add(b); t.Add(b); t.Add(c); t.Add(d);
                    }
            }
            var mesh = new Mesh { name = "PhaisterDrainFlame", hideFlags = HideFlags.HideAndDontSave };
            mesh.SetVertices(v); mesh.SetNormals(n); mesh.SetUVs(0, uv); mesh.SetColors(col); mesh.SetTriangles(t, 0);
            mesh.bounds = new Bounds(Vector3.up * 0.5f, new Vector3(2.6f, 2.6f, 2.6f));
            return mesh;
        }

        /// <summary>
        /// Draws the look at <paramref name="t"/> on its timeline. <paramref name="head"/> is the crown of the victim's head,
        /// <paramref name="hand"/> her hand; <paramref name="brokeAt"/> is when the reach broke, negative if it did not.
        /// <paramref name="ownEyes"/> is true on the victim's own screen: their head is the lens, so no flame or smoke, and the
        /// moths' circle stops wide of the camera, which is what they get to see.
        /// </summary>
        public void Pose(float t, float brokeAt, Vector3 head, Vector3 hand, Vector3 eye, bool ownEyes)
        {
            if (t < 0f || t >= End) { Hide(); return; }
            if (!_root.activeSelf) _root.SetActive(true);
            bool broke = brokeAt >= 0f;

            if (_moths != null)
            {
                _moths.SetFloat("_T", t);
                _moths.SetFloat("_BreakAt", brokeAt);
                _moths.SetVector("_Head", ownEyes ? head + Vector3.down * (head.y - eye.y + 0.45f) : head);
                _moths.SetVector("_Hand", hand);
                _moths.SetFloat("_KeepOut", ownEyes ? 1.5f : 0f);
            }
            bool mothsOn = t < SnapAt + 1.1f;
            if (_mothGo.activeSelf != mothsOn) _mothGo.SetActive(mothsOn);

            PoseFlame(t, brokeAt, head, hand, eye, ownEyes);
            _streamFrom = head + Vector3.up * 0.55f;
            if (ownEyes) foreach (var p in _stream) p.Frame(t, 0f);
            else PoseStream(t, brokeAt, head, hand);

            // The snuff's flash: three frames, a star over the head. There is no bloom to do it for us.
            float s = t - SnapAt;
            float pop = !broke && s >= 0f ? 1f - FxStudyKit.Ease(s / 0.14f) : 0f;
            _flash.At.position = head + Vector3.up * 0.42f;
            _flash.At.localScale = Vector3.one * Mathf.Lerp(1.1f, 2.2f, FxStudyKit.EaseOut(s / 0.14f));
            _flash.Face(eye);
            _flash.Frame(t, ownEyes ? 0f : pop);
            // And where the stolen light lands in her hand.
            float h = t - (SnapAt + 0.55f);
            float land = !broke && h >= 0f ? FxStudyKit.Envelope(h, 0f, 0.05f, 0.08f, 0.26f) : 0f;
            _handFlash.At.position = hand;
            _handFlash.At.localScale = Vector3.one * Mathf.Lerp(0.5f, 1.05f, FxStudyKit.EaseOut(h / 0.2f));
            _handFlash.Face(eye);
            _handFlash.Frame(t, land * 0.8f);

            // The smoke: the snuff's puff and the wick's thin column until refill.
            bool smoke = !ownEyes && !broke;
            Vector3 wickAt = head + Vector3.up * 0.05f;
            var puff = FxCloud.Emitter.Puff(wickAt + Vector3.up * 0.10f, Vector3.up * 1.05f, SnapAt, 0.14f, 0.78f);
            puff.Spread = 0.16f; puff.Widen = 0.36f; puff.Weave = 0.06f; puff.Small = 0.05f; puff.Large = 0.13f; puff.Gone = 0.04f;
            _puff.Piece.Frame(t, smoke && s >= -0.02f && s < 1f ? 1f : 0f);
            _puff.Pose(t, puff);
            var wick = FxCloud.Emitter.Column(wickAt, new Vector3(0.30f, 1.25f, 0.12f));
            wick.Spread = 0.008f; wick.Life = 1.4f; wick.Widen = 0.02f; wick.Weave = 0.13f; wick.Small = 0.022f; wick.Large = 0.052f; wick.Gone = 0.012f;
            wick.PeakLow = 0.12f; wick.PeakHigh = 0.22f; wick.SharpLow = 0.8f; wick.SharpHigh = 1.3f;
            wick.From = SnapAt + 0.08f; wick.To = RefillAt - 0.15f;
            _wick.Piece.Frame(t, smoke && s > 0f ? 1f : 0f);
            _wick.Pose(t, wick);
        }

        private void PoseFlame(float t, float brokeAt, Vector3 head, Vector3 hand, Vector3 eye, bool ownEyes)
        {
            bool broke = brokeAt >= 0f;
            float s = t - SnapAt;
            float size, stretch = 1f, eaten = 0f, spent = 0f, lean = 0f;
            if (broke)
            {
                // It sinks back into them.
                float grown = FxStudyKit.EaseOut(FxStudyKit.Run(brokeAt, 0.05f, 1.15f));
                size = grown * (1f - FxStudyKit.Ease((t - brokeAt) / 0.32f));
            }
            else if (s < 0f)
            {
                // Drawn up out of the head, then bitten down as the circle closes, then the last held breath.
                float grown = FxStudyKit.EaseOut(FxStudyKit.Run(t, 0.05f, 1.15f));
                float bite = FxStudyKit.Run(t, ReachEnd, SnapAt);
                size = grown * Mathf.Lerp(1f, 0.72f, bite * bite);
                eaten = 0.26f * bite;
                lean = Mathf.Lerp(0.16f * grown, 0.02f, FxStudyKit.Ease(bite * 2.5f));
                stretch = 1f + 0.35f * FxStudyKit.Ease((t - (SnapAt - 0.14f)) / 0.14f);
            }
            else if (s < 0.09f)
            {
                // The pinch: thin and tall for two frames, then nothing.
                size = Mathf.Lerp(0.72f, 0.32f, s / 0.09f);
                stretch = Mathf.Lerp(1.35f, 2.3f, s / 0.09f);
                eaten = Mathf.Lerp(0.26f, 0.85f, s / 0.09f);
            }
            else if (t < RefillAt)
            {
                // The ember: it tries, every so often, and cannot catch.
                float attempt = Mathf.Max(0f, Mathf.Sin(s * 5.3f) * Mathf.Sin(s * 2.1f + 1.0f));
                size = FxStudyKit.Run(s, 0.25f, 0.5f) * (0.13f + 0.08f * attempt);
                spent = 1f;
                eaten = 0.15f;
            }
            else
            {
                // It catches, small and clean, and goes: stamina comes back.
                float r = t - RefillAt;
                size = 0.42f * FxStudyKit.Pop(r / 0.16f) * (1f - FxStudyKit.Ease((r - 0.16f) / 0.28f));
            }
            bool show = size > 0.01f && !ownEyes;
            if (_flameGo.activeSelf != show) _flameGo.SetActive(show);
            if (show && _flame != null)
            {
                _flameGo.transform.position = head;
                float w = FlameWidth / 0.62f * size / Mathf.Sqrt(stretch);
                _flameGo.transform.localScale = new Vector3(w, FlameHeight * size * stretch, w);
                Vector3 toward = hand - head; toward.y = 0f;
                _flame.SetVector("_Lean", toward.sqrMagnitude > 0.01f ? toward.normalized * lean : Vector3.zero);
                _flame.SetFloat("_Age", t);
                _flame.SetFloat("_Eaten", eaten);
                _flame.SetFloat("_Spent", spent);
            }
            _halo.At.position = head + Vector3.up * 0.42f * Mathf.Max(0.3f, size);
            _halo.At.localScale = Vector3.one * (2.1f * size);
            _halo.Face(eye);
            _halo.Frame(t, show ? 0.55f * (1f - spent) : 0f);
        }

        private void PoseStream(float t, float brokeAt, Vector3 head, Vector3 hand)
        {
            bool broke = brokeAt >= 0f;
            Vector3 from = _streamFrom, to = hand, along = to - from;
            float len = along.magnitude;
            bool show = len > 0.2f && t > 0.15f && t < SnapAt + 0.8f;
            if (!show) { foreach (var p in _stream) p.Frame(t, 0f); return; }
            _streamAt.position = from;
            Vector3 flat = new Vector3(along.x, 0f, along.z);
            _streamAt.rotation = Quaternion.LookRotation(along, flat.sqrMagnitude > 0.01f ? Vector3.up : Vector3.forward);
            _streamAt.localScale = new Vector3(1f, 1f, len);
            // Drawn out of the flame to her hand through the reach, thin through the mark (the moths are the read then), and
            // one fat gulp at the snuff that the tail chases home.
            float head01 = FxStudyKit.EaseOut(FxStudyKit.Run(t, 0.15f, 0.85f)) * 1.15f;
            float gulp = t - SnapAt;
            if (gulp > 0f) head01 = FxStudyKit.Ease(FxStudyKit.Run(gulp, 0f, 0.5f)) * 1.15f;
            float s = t - SnapAt;
            float tail = s > 0f ? FxStudyKit.Ease(FxStudyKit.Run(s, 0.14f, 0.72f)) * 1.15f - 0.1f : -0.15f;
            float opacity = t < ReachEnd ? 0.72f : Mathf.Lerp(0.72f, 0f, FxStudyKit.Ease((t - ReachEnd) / 0.35f));
            if (s > 0f) opacity = FxStudyKit.Envelope(s, 0f, 0.10f, 0.38f, 0.36f) * 0.9f;
            float erode = s > 0f ? FxStudyKit.Run(s, 0.45f, 0.78f) : 0f;
            if (broke) { opacity *= 1f - FxStudyKit.Ease((t - brokeAt) / 0.3f); erode = FxStudyKit.Run(t - brokeAt, 0f, 0.3f); }
            foreach (var p in _stream)
            {
                p.Open(tail, head01);
                p.Frame(t, opacity, erode);
            }
        }

        public void Hide() { if (_root != null && _root.activeSelf) _root.SetActive(false); }

        public void Free()
        {
            _wick?.Dispose(); _puff?.Dispose();
            foreach (var thing in _owned) PhaisterLook.Free(thing);
            PhaisterLook.Free(_root);
        }
    }
}
