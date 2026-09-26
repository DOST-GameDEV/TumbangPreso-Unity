using System.Collections.Generic;
using UnityEngine;

namespace TumbangPreso.Visual
{
    /// <summary>
    /// ⚠️⚠️ MARIANG MAKILING'S MEADOW (owner, 2026-09-26 night: *"when maria makiling starts coming into the pic flowers start sprouting
    /// and lushh greenery and plants and shit (pls dotn reuse existing models)"*, *"and they disappear slowly as she disappears thoroughly
    /// direct it"*). direction.md 5.13 and 5.14. Where the mountain's spirit stands, the mountain comes up through the plaza: moss runs over
    /// the stone, grass springs up, fern fiddleheads push up and UNROLL, sampaguita (her wreath's own flower) open white, and makahiya
    /// spread their feathery leaves and pink puffs. All of it new and typed plant by plant (`tools/build_paete_props.py` `meadow`,
    /// `Resources/Models/PaeteProps/meadow.glb`); nothing reuses the tree's, the pitcher's or the rattan's parts.
    ///
    /// Directed, beat by beat, all from the cutscene's clock (the world is paused under it, so nothing here runs on `Update`):
    ///  * GROW: a wave out from where she rises, about 3 m/s from 0.1 s; moss spreads, grass springs, ferns push up curled and unroll,
    ///    the sampaguita buds open, the makahiya leaves unfold;
    ///  * FLINCH: his palms hit the court (the slam): everything near his hands bows away from them, and the makahiya FOLD SHUT at the
    ///    touch, as the real plant does (the one every Filipino child has touched), then slowly open again;
    ///  * LEAN: each of the tree's hauls pushes a gust across the court and the meadow leans away from it;
    ///  * WILT: as she goes, from the OUTER EDGE IN: the makahiya close first, the blossoms shut, the fronds curl back into fiddleheads,
    ///    and grass and moss sink into the court.
    /// </summary>
    public sealed class PaeteMeadow
    {
        /// <summary>
        /// Its own sixteen colours, the MD_* slots of `tools/build_paete_props.py`: 0 moss, 1 moss dark, 2 grass, 3 grass dark, 4 fern,
        /// 5 fern light, 6 petal, 7 flower heart, 8 ink, 9 stem, 10 makahiya pink, 11 makahiya tip, 12 makahiya leaf, 13 gumamela red,
        /// 14 bud, 15 leaf dark. A younger green than his moss (so her meadow and his body do not read as one mass), sampaguita white
        /// with a warm heart, the makahiya's pink and the gumamela's red.
        /// ⚠️ v2 (the first render, `paete_meadow_v4.png`): the moss was a lime (`7FAE3A`) that glared off the plaza like paper;
        /// it is two deeper greens now that sit UNDER the grass, and slot 13 ("soil", never used) is the gumamela's red.
        /// </summary>
        public static readonly Color[] Palette =
        {
            Hex(0x5E8A2E), Hex(0x44701F), Hex(0x78B040), Hex(0x4E8A2C), Hex(0x4E8E34), Hex(0x72AA40), Hex(0xF8F5EA), Hex(0xEAD27E),
            Hex(0x1E140C), Hex(0x5E7A2C), Hex(0xE88AB8), Hex(0xFAD8EA), Hex(0x6E9E36), Hex(0xD2344A), Hex(0xE8E2C6), Hex(0x2F5E22),
        };

        private static Color Hex(int rgb) => new Color(((rgb >> 16) & 255) / 255f, ((rgb >> 8) & 255) / 255f, (rgb & 255) / 255f, 1f);

        private enum Kind { Moss, Grass, Fern, Frond, Bush, Blossom, Mimosa, MimosaLeaf, Puff }

        private sealed class Part
        {
            public Transform T;
            public Kind Kind;
            public Vector3 Scale, Position;
            public Quaternion Rotation;
            public float FromHer, FromHands, Segment;
            public Vector3 Ground;   // where its plant stands, in the meadow's space
        }

        private readonly GameObject _model;
        private readonly List<Part> _parts = new List<Part>();
        private readonly float _farthest;
        private readonly Vector3 _hands;

        /// <summary>The meadow under <paramref name="parent"/> (the cutscene's stage), laid on the court at <paramref name="court"/>.</summary>
        public PaeteMeadow(Transform parent, Vector3 her, Vector3 hands, float court)
        {
            _hands = new Vector3(hands.x, 0f, hands.z);
            _model = PaeteProp.Spawn("meadow", parent, Palette, ToonSkin.PersonOutlineWidth * 0.8f);
            if (_model == null) return;
            _model.transform.localPosition = new Vector3(0f, court, 0f);
            foreach (var t in _model.GetComponentsInChildren<Transform>(true))
            {
                string n = t.name;
                Kind kind;
                float segment = 0f;
                if (n.StartsWith("moss-")) kind = Kind.Moss;
                else if (n.StartsWith("grass-")) kind = Kind.Grass;
                else if (n.StartsWith("fern-") && n.Contains("-f")) { kind = Kind.Frond; segment = n.EndsWith("-a") ? 0 : n.EndsWith("-b") ? 1 : 2; }
                else if (n.StartsWith("fern-")) kind = Kind.Fern;
                else if ((n.StartsWith("samp-") || n.StartsWith("gum-")) && n.Contains("-b")) kind = Kind.Blossom;
                else if (n.StartsWith("samp-") || n.StartsWith("gum-")) kind = Kind.Bush;
                else if (n.StartsWith("maka-") && n.Contains("-l")) kind = Kind.MimosaLeaf;
                else if (n.StartsWith("maka-") && n.Contains("-p")) kind = Kind.Puff;
                else if (n.StartsWith("maka-")) kind = Kind.Mimosa;
                else continue;
                // Its plant's ground point: the node's own origin for a plant, its plant's for a part of one.
                var plant = t;
                while (plant.parent != null && plant.parent != _model.transform && (plant.parent.name.StartsWith("fern-") || plant.parent.name.StartsWith("samp-")
                       || plant.parent.name.StartsWith("gum-") || plant.parent.name.StartsWith("maka-")))
                    plant = plant.parent;
                var ground = _model.transform.InverseTransformPoint(plant.position); ground.y = 0f;
                var p = new Part
                {
                    T = t, Kind = kind, Scale = t.localScale, Position = t.localPosition, Rotation = t.localRotation, Segment = segment, Ground = ground,
                    FromHer = Vector2.Distance(new Vector2(ground.x, ground.z), new Vector2(her.x, her.z)),
                    FromHands = Vector2.Distance(new Vector2(ground.x, ground.z), new Vector2(hands.x, hands.z)),
                };
                _parts.Add(p);
                _farthest = Mathf.Max(_farthest, p.FromHer);
            }
            Pose(0f, 99f, null, 99f, 100f, Vector3.forward * 5f);
        }

        private static float Ease(float from, float to, float t) { float u = Mathf.InverseLerp(from, to, t); return u * u * (3f - 2f * u); }

        /// <summary>
        /// Pose at scene time <paramref name="t"/>: <paramref name="slamAt"/> his palms hitting the court, <paramref name="gusts"/> the
        /// tree's hauls, <paramref name="wiltFrom"/> to <paramref name="wiltTo"/> the wilt from the edge in, <paramref name="landing"/>
        /// where the tree comes up (the gusts blow away from it), all in the meadow's parent space.
        /// </summary>
        public void Pose(float t, float slamAt, IList<float> gusts, float wiltFrom, float wiltTo, Vector3 landing)
        {
            if (_model == null) return;
            bool any = false;
            foreach (var p in _parts)
            {
                // GROW: the wave out from her, about 3 m/s from 0.1 s.
                float start = 0.10f + p.FromHer / 3.0f;
                float grow = Mathf.Clamp01((t - start) / 0.40f);
                // WILT: from the outer edge in, each plant over 0.35 s.
                float wiltStart = wiltFrom + (1f - p.FromHer / Mathf.Max(0.1f, _farthest)) * Mathf.Max(0f, wiltTo - wiltFrom - 0.35f);
                float wilt = Ease(wiltStart, wiltStart + 0.35f, t);
                // FLINCH from his palms (the nearer, the harder), and the makahiya's fold at the touch.
                float since = t - slamAt;
                float flinch = since >= 0f && since < 0.7f ? Mathf.Sin(Mathf.Clamp01(since / 0.7f) * Mathf.PI) * (1f - since / 0.7f) * 2.2f : 0f;
                flinch *= Mathf.Clamp01(1f - p.FromHands / 2.4f);
                float foldDelay = p.FromHands / 6f;
                float fold = since >= foldDelay ? Mathf.Clamp01((since - foldDelay) / 0.08f) * (1f - Ease(foldDelay + 0.5f, foldDelay + 1.9f, since)) : 0f;
                // LEAN from the gusts of the tree's hauls, away from where it comes up.
                float gust = 0f;
                if (gusts != null) foreach (float g in gusts) gust = Mathf.Max(gust, GrowthVfx.Envelope(t, g, 0.08f, g + 0.7f, 0.5f));
                var away = p.Ground - new Vector3(landing.x, 0f, landing.z); away.y = 0f;
                var fromHands = p.Ground - _hands; fromHands.y = 0f;
                Quaternion bend = Quaternion.identity;
                if (away.sqrMagnitude > 1e-4f) bend = Quaternion.AngleAxis(9f * gust * (1f - wilt), Vector3.Cross(Vector3.up, away.normalized));
                float shown = grow * (1f - (p.Kind == Kind.Moss || p.Kind == Kind.Grass || p.Kind == Kind.Fern || p.Kind == Kind.Bush || p.Kind == Kind.Mimosa ? wilt : 0f));
                switch (p.Kind)
                {
                    case Kind.Moss:
                        // It spreads over the stone and settles, then sinks flat into the court.
                        float spread = GrowthVfx.Pop(grow);
                        p.T.localScale = new Vector3(p.Scale.x * Mathf.Lerp(0.15f, 1f, spread), p.Scale.y * Mathf.Max(0.001f, grow * (1f - wilt)), p.Scale.z * Mathf.Lerp(0.15f, 1f, spread))
                                         * (wilt >= 1f ? 0.001f : 1f);
                        break;
                    case Kind.Grass:
                    case Kind.Bush:
                    case Kind.Mimosa:
                    case Kind.Fern:
                        // Springs up with an overshoot; bows away from his hands and from the gusts; sinks as it wilts.
                        float pop = GrowthVfx.Pop(grow);
                        float sink = 1f - wilt;
                        p.T.localScale = new Vector3(p.Scale.x * Mathf.Lerp(0.6f, 1f, pop) * Mathf.Lerp(0.6f, 1f, sink), p.Scale.y * Mathf.Max(0.001f, pop * sink), p.Scale.z * Mathf.Lerp(0.6f, 1f, pop) * Mathf.Lerp(0.6f, 1f, sink));
                        Quaternion flinchTurn = Quaternion.identity;
                        if (fromHands.sqrMagnitude > 1e-4f && flinch > 0f) flinchTurn = Quaternion.AngleAxis(16f * flinch, Vector3.Cross(Vector3.up, fromHands.normalized));
                        p.T.localRotation = flinchTurn * bend * p.Rotation;
                        if (shown > 0.01f) any = true;
                        break;
                    case Kind.Frond:
                        // A fiddlehead: each segment curled up and back over the one before, the tip most; it unrolls after the plant
                        // is up, and curls back as it wilts.
                        float unroll = Ease(start + 0.12f, start + 0.75f, t);
                        float curl = Mathf.Max(1f - unroll, wilt);
                        float angle = -(35f + 40f * p.Segment) * curl - 6f * flinch * (1f + p.Segment);
                        p.T.localRotation = p.Rotation * Quaternion.Euler(angle, 0f, 0f);
                        p.T.localScale = p.Scale * Mathf.Lerp(0.55f, 1f, unroll) * (1f - 0.3f * wilt);
                        break;
                    case Kind.Blossom:
                        // Buds open white after the bush is up; they shut again as it wilts.
                        float open = Ease(start + 0.30f, start + 0.70f, t) * (1f - wilt);
                        p.T.localScale = p.Scale * Mathf.Max(0.001f, Mathf.Lerp(0.25f, 1f, open) * Mathf.Clamp01(grow * 3f) * (1f - 0.7f * wilt));
                        break;
                    case Kind.MimosaLeaf:
                        // Unfolds as the plant comes up; FOLDS SHUT at his touch and slowly opens; closes first of all as she goes.
                        float unfold = Ease(start + 0.15f, start + 0.55f, t);
                        float shut = Mathf.Max(1f - unfold, Mathf.Max(fold, Ease(wiltStart - 0.15f, wiltStart + 0.05f, t)));
                        p.T.localRotation = p.Rotation * Quaternion.Euler(-55f * shut, 0f, 0f);
                        p.T.localScale = new Vector3(p.Scale.x * Mathf.Lerp(1f, 0.25f, shut), p.Scale.y, p.Scale.z * Mathf.Lerp(1f, 0.7f, shut));
                        break;
                    case Kind.Puff:
                        float puff = Ease(start + 0.35f, start + 0.65f, t) * (1f - Ease(wiltStart - 0.1f, wiltStart + 0.15f, t));
                        p.T.localScale = p.Scale * Mathf.Max(0.001f, GrowthVfx.Pop(puff));
                        break;
                }
            }
            if (_model.activeSelf != (any || t < 0.2f)) _model.SetActive(any || t < 0.2f);
        }

        public void Dispose() { if (_model != null) PaeteProp.Kill(_model); }
    }
}
