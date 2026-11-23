using System.Collections.Generic;
using UnityEngine;

namespace TumbangPreso.Visual
{
    /// <summary>
    /// ⚠️ THE MOUNTAIN'S BIRTH, AS ONE OBJECT POSED FROM ONE NUMBER: the seconds since the mountain began to rise. Rago's
    /// cutscene builds it round the mountain it stages, and the live volcano builds it round itself (`SeanVolcano`), so at
    /// the hand-back every ball of ash, every bomb in the air and the wave's front are exactly where the cutscene's last
    /// frame left them, and carry on (`docs/HERO_KIT_METHOD.md` section 6: the hand-back with no jump; rule 7: never show it
    /// twice). Play hands it its age plus `SeanVolcano.HandBackLead`; a fresh cast with no cutscene hands it its age.
    ///
    /// What it is, and why each piece looks the way it does:
    ///   * THE ASH BURST out of the crater: the approved cloud (`FxCloud`, owner 2026-10-30: "we already have cloud smoke to
    ///     use"), warm grey, leaning, fired for 0.7 s, so it is a burst that climbs and thins, not a column that stands.
    ///   * THE DUST round its foot as the court breaks: the same cloud, low and wide, warm dust.
    ///   * THE WAVE: no ring (the owner's ban on flat rings, `docs/VFX_QUALITY_GATE.md`). Clouds of dust RIDE the front out to
    ///     `SeanRules.VolcanoBlastRadius`, born and shrinking the whole way, so the reach is shown by something with body.
    ///   * LAVA BOMBS: MAGMA (`SeanMagma`; owner 2026-11-23, of cutscene v4: *"The lava bombs read as grey balls. it should be
    ///     more orange and magma like"*). They were the lava shader's crust toe on a stone, crust that has stopped, so a dark
    ///     ball with thin seams that read grey under his red sky. Now a mostly molten ball, its crust breaking over a hot core,
    ///     a short tail of embers and two puffs of soot along its fixed arc, and a splat, spatter and dust where it lands. No
    ///     balls of light and no smoke balls.
    /// Built once; posed by moving transforms and setting material values only. Nothing here decides anything.
    /// </summary>
    public sealed class SeanEruption
    {
        /// <summary>How long the wave takes to reach its full radius (it reaches it as the cutscene hands back).</summary>
        public const float WaveSeconds = 2.6f;
        /// <summary>After this many seconds since the rise everything here is over and may be freed.</summary>
        public const float EndSeconds = WaveSeconds + 0.8f;
        private const int WaveClouds = 16, Bombs = 5;
        private const float Crater = 3.05f;

        private static readonly Color AshLit = new Color(.60f, .53f, .50f), AshShade = new Color(.39f, .33f, .315f), AshLine = new Color(.25f, .20f, .19f);
        // Pale warm court dust: at the first tones (0.74 lit) it read as tan stones lying on the court (film r2).
        private static readonly Color DustLit = new Color(.90f, .83f, .74f), DustShade = new Color(.70f, .61f, .53f), DustLine = new Color(.50f, .42f, .36f);

        private readonly Transform _root;
        private FxCloud _ash, _foot;
        private readonly List<FxCloud> _wave = new List<FxCloud>(WaveClouds);
        private readonly List<Vector3> _waveWay = new List<Vector3>(WaveClouds);
        private readonly List<SeanMagma> _bombs = new List<SeanMagma>(Bombs);
        private readonly List<Vector3> _bombLand = new List<Vector3>(Bombs);
        private readonly List<Vector3> _bombSpin = new List<Vector3>(Bombs);

        /// <summary>The wave's front, metres from the foot: fast off the rise, easing as it reaches the full radius.</summary>
        public static float WaveReach(float since)
        {
            float u = Mathf.Clamp01(since / WaveSeconds);
            return Core.SeanRules.VolcanoBlastRadius * (1f - (1f - u) * (1f - u));
        }

        /// <param name="root">At the mountain's foot, turned with the world (play's `SeanVolcano` object, or the cutscene's copy).</param>
        public static SeanEruption Build(Transform root) => new SeanEruption(root);

        private SeanEruption(Transform root)
        {
            _root = root;
            // Line widths in proportion to the ball, as the approved cloud draws its line on a 0.62 m ball.
            _ash = FxCloud.Build(root, "SeanEruption-ash", 31, 80, AshLit, AshShade, AshLine, FxStudyCloud.Line * 1.25f / .62f, .28f, 1.25f);
            _foot = FxCloud.Build(root, "SeanEruption-dust", 37, 40, DustLit, DustShade, DustLine, FxStudyCloud.Line * .42f / .62f, .10f, .42f);
            for (int i = 0; i < WaveClouds; i++)
            {
                float a = (i + .35f * Hash(i, 3)) / WaveClouds * Mathf.PI * 2f;
                _waveWay.Add(new Vector3(Mathf.Sin(a), 0f, Mathf.Cos(a)));
                _wave.Add(FxCloud.Build(root, "SeanEruption-wave" + i, 41 + i % 4, 24, DustLit, DustShade, DustLine, FxStudyCloud.Line * .28f / .62f, .07f, .28f));
            }
            for (int i = 0; i < Bombs; i++)
            {
                _bombs.Add(new SeanMagma(root, "SeanEruption-bomb" + i, i));
                float a = (i * 72f + 25f * Hash(i, 5) + 18f) * Mathf.Deg2Rad;
                _bombLand.Add(new Vector3(Mathf.Sin(a), 0f, Mathf.Cos(a)) * (4.6f + 3.2f * Hash(i, 6)));
                _bombSpin.Add(new Vector3(Hash(i, 7) - .5f, Hash(i, 8) - .5f, Hash(i, 9) - .5f).normalized * (380f + 260f * Hash(i, 10)));
            }
        }

        private static float Hash(int i, int salt) => FxStudyKit.Hash(i, salt);

        /// <summary>Everything at <paramref name="since"/> seconds after the mountain began to rise (negative: nothing yet).</summary>
        public void Pose(float since)
        {
            // The ash: fired out of the crater for 0.7 s, leaning off with the wind, climbing and thinning for 1.5 s.
            var ash = FxCloud.Emitter.Column(new Vector3(0f, Crater, 0f), new Vector3(.9f, 6.2f, .5f));
            ash.Spread = 1.0f; ash.Life = 1.6f; ash.Widen = 1.3f; ash.Weave = .35f; ash.Small = .28f; ash.Large = 1.25f; ash.Gone = .12f;
            ash.From = .02f; ash.To = .80f;
            Place(_ash, since, ash, .02f, .80f + 1.6f);
            // The court breaking round the foot: low, wide, out with the slabs.
            var foot = FxCloud.Emitter.Puff(new Vector3(0f, .1f, 0f), new Vector3(0f, 1.05f, 0f), 0f, .28f, 1.0f);
            foot.Spread = 4.4f; foot.Widen = 1.4f; foot.Weave = .2f; foot.Small = .10f; foot.Large = .45f; foot.Gone = .06f;
            Place(_foot, since, foot, 0f, 1.28f);
            // The wave: each cloud rides the front out along its bearing, born and shrinking all the way; it stops at the
            // full radius and its last balls finish their lives there.
            float reach = WaveReach(since);
            for (int i = 0; i < _wave.Count; i++)
            {
                var way = _waveWay[i];
                // Low and rolling out with the front, not climbing: a wave of dust along the court, not a row of puffs.
                var e = FxCloud.Emitter.Column(way * reach + Vector3.up * .04f, way * 1.1f + Vector3.up * .45f);
                e.Spread = 1.3f; e.Life = .5f; e.Widen = .5f; e.Weave = .12f; e.Small = .07f; e.Large = .28f; e.Gone = .045f;
                e.From = .12f + .05f * Hash(i, 11); e.To = WaveSeconds;
                Place(_wave[i], since, e, e.From, WaveSeconds + .62f);
            }
            // The bombs: out of the crater on the same fixed arcs as before, molten, shedding embers and soot, splatting where
            // they land. Their crust's clock is this one (the world's clock stands still under a cutscene).
            for (int i = 0; i < _bombs.Count; i++)
            {
                float from = .10f + .07f * i, flight = 1.0f + .3f * Hash(i, 12);
                var land = _bombLand[i];
                var start = new Vector3(0f, Crater, 0f) + land.normalized * .25f;
                // A touch bigger than the stones were (0.42 to 0.56): from C1's 11 m a molten ball has to be a shape, not a dot.
                _bombs[i].Pose(_root.TransformPoint(start), _root.TransformPoint(land), flight, 2.6f + 1.2f * Hash(i, 13), since - from,
                               _bombSpin[i], .48f + .14f * Hash(i, 14), 5f);
            }
        }

        // A cloud is posed and drawn only while it can have balls out.
        private static void Place(FxCloud cloud, float since, in FxCloud.Emitter e, float from, float to)
        {
            if (cloud == null || cloud.Piece.Renderer == null) return;
            bool on = since >= from && since <= to;
            cloud.Piece.Renderer.enabled = on;
            if (on) cloud.Pose(since, e);
        }

        public void Dispose()
        {
            _ash?.Dispose(); _foot?.Dispose();
            foreach (var c in _wave) c?.Dispose();
            _wave.Clear();
            foreach (var b in _bombs) b?.Dispose();
            _bombs.Clear();
            _ash = _foot = null;
        }

        private static void Kill(Object o) { if (o == null) return; if (Application.isPlaying) Object.Destroy(o); else Object.DestroyImmediate(o); }

        // ------------------------------------------------------------------ the lava's material, shared with the cutscene

        private static Material _flow, _pool;

        /// <summary>
        /// The live volcano's own lava (`SeanVolcanoLava`, set up as `SeanVolcanoLook` sets up its flows and pools), for lava
        /// the cutscene lays on the court: the cracks round his fists, the river, the court splitting at the spot. One
        /// substance from his fists to the crater. Per piece, `_Grow` and `_Clock` go in a property block.
        /// </summary>
        public static Material Lava(bool pool)
        {
            ref Material m = ref (pool ? ref _pool : ref _flow);
            if (m != null) return m;
            m = NewLava(pool ? "SeanEruption-pool" : "SeanEruption-flow", pool);
            return m;
        }

        private static Material NewLava(string name, bool pool)
        {
            var shader = Shader.Find("TumbangPreso/SeanVolcanoLava");
            if (shader == null) return null;
            var m = new Material(shader) { name = name, hideFlags = HideFlags.HideAndDontSave };
            m.SetTexture("_CrustTex", Resources.Load<Texture2D>(SeanVolcanoLook.Folder + "mayon_b_crust"));
            m.SetTexture("_ErodeTex", FxStudyKit.Mask("erode_cloud"));
            m.SetFloat("_Pool", pool ? 1f : 0f);
            m.SetColor("_Scorch", SeanVolcanoLook.Scorch);
            m.SetVector("_Key", FxStudyDanteBoulder.KeyLight);
            m.SetFloat("_UseTime", 0f);
            return m;
        }
    }
}
