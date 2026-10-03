using System.Collections.Generic;
using UnityEngine;

namespace TumbangPreso.Visual
{
    public sealed partial class HeroIntroductionScene
    {
        // =========================================================================================
        // AMIHAN, AIRBURST v3: THE DENSITY LAYER (`docs/reports/amihan-presentation-2026-10-02/airburst-v3.md`). Set beside a
        // Genshin burst her 3.6 s cutscene had no air in it: no glints, no rings, no flash, no veil. This is that air, and every
        // piece rides a beat the body already has (`AmCallOutAt` to `AmFlickAt`):
        //
        //   * GLINTS: her DIAMOND stars (a long rhombus cross and a short one turned 45 degrees: the kasikus's own corner), popping
        //     and twinkling out round whoever the beat is on: the flung hand, her as the monsoon answers, the palms on the cup and
        //     each pack, the emblem, and down the lane along the warp as it is strung.
        //   * DIAMOND RINGS racing over the court: the kasikus outline, not a circle, out from her feet on the call out, the
        //     answer, the cup and each pack; one runs INWARD on the draw (the street's air drawn back into her).
        //   * FLASHES flat on the court under the answer, the cup and the flick.
        //   * THE EDGE VEIL: the frame's edges sink into her ink green while the power is on screen (`SpiritVeil.shader`), deepest
        //     through the weave, thrown on each beat, and gone for the real lane.
        //
        // ⚠️ Typed rows, each its own time, place, size and colour; posed from the scene clock only. Her colours, never white.
        // ⚠️ Reduced effects keeps every shape, halves the light, and drops the flashes and the veil.
        // =========================================================================================

        // Where a row is anchored: 0 her feet on the court, 1 her cupped palms, 2 her right palm, 3 her head, 4 the emblem.
        private Vector3 AbAnchor(int anchor)
        {
            switch (anchor)
            {
                case 1: return BothPalms;
                case 2: return RightPalm;
                case 3: return HeadPoint;
                case 4: return _amEmblemHost != null ? _amEmblemHost.localPosition : BothPalms;
                default: return Vector3.up * _amCourt;
            }
        }

        // The hottest her light gets: her mint pushed toward cream, still green (never white).
        private static readonly Color AbHot = new Color(0.86f, 1.0f, 0.70f, 1f);
        private static Color AbColour(float kind)
            => kind > 2.5f ? WindVfx.Threads[1] : kind > 1.5f ? WindVfx.Gold : kind > .5f ? AbHot : WindVfx.Body;

        // GLINTS: birth, life, anchor, offset (x, y, z, scene space), size, colour (0 mint, 1 hot, 2 gold, 3 abel teal).
        private static readonly float[,] AbGlintRows =
        {
            // Already in the air at the cut.
            { .00f, .40f, 0, -1.30f, 1.70f, .80f, .16f, 0 }, { .04f, .36f, 0, 1.60f, 2.10f, -.40f, .14f, 3 },
            // CALL OUT: the flung hand.
            { .25f, .34f, 2, .10f, .05f, .05f, .26f, 1 }, { .26f, .30f, 2, -.18f, .16f, .10f, .18f, 0 }, { .28f, .32f, 2, .22f, -.12f, -.06f, .16f, 2 },
            { .31f, .28f, 2, .36f, .22f, .14f, .14f, 0 },
            // THE ANSWER: round her as the sheets arrive.
            { .55f, .42f, 0, -.70f, 1.90f, .40f, .24f, 1 }, { .56f, .38f, 0, .85f, 1.40f, .30f, .20f, 0 }, { .58f, .40f, 0, -.40f, .80f, .70f, .18f, 3 },
            { .60f, .36f, 0, 1.10f, 2.30f, -.20f, .22f, 0 }, { .62f, .44f, 0, -1.20f, 1.10f, -.30f, .20f, 2 }, { .64f, .34f, 0, .30f, 2.60f, .50f, .16f, 1 },
            { .68f, .38f, 0, .60f, .60f, .80f, .18f, 0 }, { .72f, .36f, 0, -.90f, 2.50f, .10f, .16f, 3 },
            // Arms open, the sheets orbiting: a slow scatter.
            { .95f, .40f, 0, 1.40f, 1.70f, .20f, .14f, 0 }, { 1.06f, .38f, 0, -1.50f, 2.00f, .40f, .15f, 1 }, { 1.18f, .42f, 0, .50f, 2.80f, -.60f, .13f, 0 },
            { 1.29f, .36f, 0, -.60f, .90f, .90f, .14f, 2 }, { 1.40f, .40f, 3, .30f, .30f, .20f, .16f, 1 }, { 1.52f, .38f, 0, 1.00f, 1.20f, .70f, .14f, 3 },
            // WEAVE: threads pouring into her palms.
            { 1.78f, .32f, 1, .20f, .14f, .10f, .14f, 0 }, { 1.88f, .30f, 1, -.18f, .20f, .06f, .12f, 3 }, { 1.98f, .30f, 1, .10f, -.10f, .18f, .13f, 1 },
            { 2.06f, .28f, 1, -.08f, .26f, -.04f, .12f, 0 },
            // THE CUP: the emblem drawn between her palms.
            { 2.15f, .44f, 4, .00f, .00f, .06f, .34f, 1 }, { 2.16f, .38f, 4, -.30f, .22f, .04f, .22f, 2 }, { 2.17f, .40f, 4, .32f, -.18f, .02f, .24f, 0 },
            { 2.19f, .36f, 4, .14f, .38f, .08f, .18f, 3 }, { 2.21f, .38f, 4, -.24f, -.30f, .10f, .20f, 1 }, { 2.24f, .34f, 0, 1.30f, .50f, .40f, .18f, 0 },
            { 2.26f, .36f, 0, -1.10f, .40f, .90f, .16f, 2 },
            // PACK 1 and PACK 2.
            { 2.50f, .32f, 4, .20f, .20f, .05f, .22f, 1 }, { 2.51f, .30f, 4, -.24f, .06f, .04f, .18f, 0 }, { 2.53f, .30f, 0, .80f, 1.60f, .50f, .16f, 3 },
            { 2.55f, .28f, 0, -.70f, 1.30f, -.40f, .14f, 0 },
            { 2.95f, .34f, 4, -.18f, .24f, .05f, .24f, 1 }, { 2.96f, .30f, 4, .26f, -.08f, .04f, .18f, 2 }, { 2.98f, .30f, 0, -.90f, 1.80f, .30f, .16f, 0 },
            { 3.00f, .28f, 0, .75f, 1.00f, -.50f, .14f, 3 },
            // THE DRAW: a glint held at the emblem, everything else quiet.
            { 3.30f, .40f, 4, .00f, .00f, .08f, .30f, 1 },
            // THE FLICK: a burst at her hands, then glints racing out down the lane on the warp as it is strung.
            { 3.78f, .36f, 1, .00f, .10f, .25f, .32f, 1 }, { 3.79f, .32f, 1, .30f, .24f, .30f, .22f, 0 }, { 3.80f, .32f, 1, -.26f, -.06f, .34f, .20f, 2 },
            { 3.86f, .34f, 0, .20f, .90f, 2.40f, .22f, 1 }, { 3.90f, .32f, 0, -.60f, .50f, 3.40f, .20f, 0 }, { 3.94f, .34f, 0, .90f, .70f, 4.60f, .24f, 3 },
            { 3.98f, .32f, 0, -1.30f, .40f, 5.80f, .22f, 1 }, { 4.02f, .34f, 0, 1.60f, .60f, 7.00f, .26f, 0 }, { 4.06f, .34f, 0, -.40f, .80f, 8.40f, .28f, 2 },
            { 4.10f, .36f, 0, 2.30f, .40f, 9.60f, .28f, 1 }, { 4.14f, .36f, 0, -2.50f, .50f, 10.80f, .30f, 0 },
            // The held lane: the warp shimmering over the players standing in it.
            { 4.40f, .44f, 0, .50f, .70f, 5.20f, .20f, 0 }, { 4.62f, .40f, 0, -1.10f, .50f, 7.60f, .22f, 3 }, { 4.84f, .42f, 0, 1.40f, .60f, 9.00f, .22f, 1 },
            { 4.84f, .40f, 0, -.20f, .80f, 6.40f, .20f, 0 },
            // THE BEATER (v3.2): a burst at her driven palms, then glints racing down the lane with the fronts.
            { 5.05f, .34f, 1, .10f, .05f, .45f, .40f, 1 }, { 5.05f, .30f, 1, -.30f, .20f, .50f, .28f, 2 }, { 5.06f, .30f, 1, .35f, -.10f, .55f, .26f, 0 },
            { 5.07f, .30f, 0, .40f, 1.10f, 2.80f, .30f, 1 }, { 5.09f, .30f, 0, -.90f, .70f, 4.60f, .30f, 3 }, { 5.11f, .30f, 0, 1.30f, .90f, 6.40f, .32f, 1 },
            { 5.13f, .32f, 0, -1.60f, .60f, 8.20f, .34f, 0 }, { 5.15f, .32f, 0, .60f, 1.20f, 10.0f, .34f, 2 }, { 5.17f, .34f, 0, 2.40f, .50f, 11.6f, .36f, 1 },
        };

        // DIAMOND RINGS on the court: time, anchor, radius reached (negative: drawn INWARD from that radius), life, colour.
        private static readonly float[,] AbRingRows =
        {
            { .25f, 0, 2.6f, .40f, 0 },
            { .55f, 0, 4.4f, .50f, 1 }, { .59f, 0, 3.0f, .42f, 0 },
            { 2.15f, 0, 3.6f, .48f, 1 }, { 2.18f, 0, 2.4f, .40f, 2 },
            { 2.50f, 0, 2.2f, .32f, 0 }, { 2.95f, 0, 2.5f, .32f, 3 },
            { 3.30f, 0, -3.4f, .40f, 0 },
            { 3.78f, 0, 3.0f, .36f, 1 },
            // THE BEATER: the biggest ring of the cutscene, and a second behind it.
            { 5.05f, 0, 7.5f, .50f, 1 }, { 5.08f, 0, 4.8f, .44f, 0 },
        };
        // FLASHES flat on the court under the three biggest beats: time, size, life.
        private static readonly float[,] AbFlashRows = { { .55f, 2.6f, .20f }, { 2.15f, 3.0f, .22f }, { 3.78f, 2.4f, .18f }, { 5.05f, 4.2f, .24f } };

        private readonly List<int> _abGlints = new List<int>(64), _abRings = new List<int>(10), _abFlashes = new List<int>(3);
        private int _abVeil = -1;
        // THE GLORY: her kasikus as light behind her (the cut-in glory behind a Genshin caster), on the answer and through the
        // weave. Two diamond outlines, the outer mint and the inner gold, always behind her from the lens.
        private readonly int[] _abGlory = { -1, -1 };
        private static Mesh _abStarMesh, _abRingMesh;

        private void BuildAmihanBurst()
        {
            for (int i = 0; i < AbGlintRows.GetLength(0); i++)
                _abGlints.Add(PvTips(AddGlow("AmihanGlint" + i, AbColour(AbGlintRows[i, 7]), AbStar, billboard: true, band: true, falloff: 1.6f, core: .9f, lift: .1f), 1.1f));
            for (int i = 0; i < AbRingRows.GetLength(0); i++)
                _abRings.Add(AddGlow("AmihanDiamondRing" + i, AbColour(AbRingRows[i, 4]), AbDiamondRing, billboard: false, band: true, falloff: 1.3f, core: .5f));
            for (int i = 0; i < AbFlashRows.GetLength(0); i++)
                _abFlashes.Add(AddGlow("AmihanGroundFlash" + i, WindVfx.Body, billboard: false, falloff: 1.5f, core: .4f));
            _abGlory[0] = AddGlow("AmihanGloryOuter", WindVfx.Body, AbDiamondRing, billboard: true, band: true, falloff: 1.4f, core: .5f, lift: -.4f);
            _abGlory[1] = AddGlow("AmihanGloryInner", WindVfx.Gold, AbDiamondRing, billboard: true, band: true, falloff: 1.5f, core: .6f, lift: -.35f);
            // THE VEIL, in its own shader (it darkens; every glow here only adds). Her ink green, not Paete's forest.
            var veil = Resources.Load<Shader>("Shaders/SpiritVeil");
            if (veil != null)
            {
                var go = VfxShapes.Stand(_root.transform, "AmihanVeil", PbVeilQuad, 1);
                var renderer = go.GetComponent<Renderer>();
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; renderer.receiveShadows = false;
                var m = new Material(veil) { name = "AmihanVeil" };
                m.SetFloat("_Inner", .38f); m.SetFloat("_Outer", 1.04f); m.SetFloat("_Tint", .16f);
                renderer.sharedMaterial = m;
                VfxRenderTag.Own(go, m);
                _pieces.Add(new Piece { Transform = go.transform, Renderer = renderer, Color = new Color(0.03f, 0.10f, 0.04f, 1f) });
                _abVeil = _pieces.Count - 1;
            }
        }

        private void SampleAmihanBurst(float t, float leave)
        {
            float light = _reducedEffects ? .5f : 1f;
            float court = _amCourt;

            // ---------------------------------------------------------------- GLINTS.
            for (int i = 0; i < _abGlints.Count; i++)
            {
                float s = t - AbGlintRows[i, 0], life = AbGlintRows[i, 1];
                if (s < 0f || s > life) { PvHide(_abGlints[i]); continue; }
                var at = AbAnchor((int)AbGlintRows[i, 2]) + new Vector3(AbGlintRows[i, 3], AbGlintRows[i, 4], AbGlintRows[i, 5]) + Vector3.up * .25f * s;
                float pop = GrowthVfx.Pop(s / .07f);
                float fade = 1f - Ease(life * .45f, life, s);
                float twinkle = .72f + .28f * Mathf.Sin(t * 36f + i * 1.9f);
                // Each turns a little as it twinkles, so a field of them never reads as stamped.
                var turn = Quaternion.Euler(0f, 0f, (i % 2 == 0 ? 1f : -1f) * (_reducedEffects ? 0f : 40f * s) + 7f * i);
                // SUBTLE (owner, on v3.6: *"make her vfx in ult as well a bit more subtle"*, *"it looks like shapes are floating"*). Smaller, dimmer glints.
                PlaceGlow(_abGlints[i], at, Vector3.one * AbGlintRows[i, 6] * .6f * pop * (.55f + .45f * fade), turn, 1.0f * fade * twinkle * light * leave);
            }

            // ---------------------------------------------------------------- DIAMOND RINGS and FLASHES on the court.
            for (int i = 0; i < _abRings.Count; i++)
            {
                float s = t - AbRingRows[i, 0], life = AbRingRows[i, 3];
                if (s < 0f || s > life) { PvHide(_abRings[i]); continue; }
                float u = s / life, reach = AbRingRows[i, 2];
                float grow = 1f - Mathf.Pow(1f - u, 3f);
                // An inward ring starts at its full size and is drawn in to her feet.
                float r = reach >= 0f ? reach * grow : -reach * (1f - grow) + .25f;
                var at = AbAnchor((int)AbRingRows[i, 1]); at.y = court + .03f + .004f * (i % 5);
                float strength = reach >= 0f ? Mathf.Pow(1f - u, 1.3f) : Mathf.Sin(Mathf.PI * u);
                // Film v3.5: beside the abel the full-strength line diamonds read as stamped UI; a lighter echo of each beat.
                PlaceGlow(_abRings[i], at, new Vector3(r, r, 1f), Quaternion.Euler(90f, 0f, 0f), .35f * strength * light * leave);
            }
            for (int i = 0; i < _abFlashes.Count; i++)
            {
                float s = t - AbFlashRows[i, 0], life = AbFlashRows[i, 2];
                if (s < 0f || s > life || _reducedEffects) { PvHide(_abFlashes[i]); continue; }
                float u = s / life, size = AbFlashRows[i, 1] * (.6f + .4f * u);
                PlaceGlow(_abFlashes[i], Vector3.up * (court + .05f), new Vector3(size, size, 1f), Quaternion.Euler(90f, 45f, 0f), .6f * (1f - u) * (1f - u) * leave);
            }

            // ---------------------------------------------------------------- THE GLORY behind her.
            {
                float answer = Ease(AmAnswerAt, AmAnswerAt + .1f, t) * (1f - Ease(1.15f, AmWeaveAt, t));
                float weave = Ease(AmCupAt, AmCupAt + .1f, t) * (1f - Ease(AmDrawAt, AmWarpAt, t));
                float beats = Decay(t - AmCupAt, .3f) + .7f * (Decay(t - AmPack1At, .2f) + Decay(t - AmPack2At, .2f));
                float strength = Mathf.Max(answer, weave);
                if (strength <= .002f) { PvHide(_abGlory[0]); PvHide(_abGlory[1]); }
                else
                {
                    AmLens(t, out _, out var eye, out _, out _);
                    // Film r1: 1.1 m behind her at 2.3 m it crossed her face. 2.4 m behind, smaller, fainter, round her head.
                    var centre = new Vector3(0f, court + 1.45f, 0f);
                    var away = centre - eye; away.y = 0f;
                    centre += (away.sqrMagnitude > 1e-4f ? away.normalized : Vector3.forward) * 2.4f;
                    float grow = answer >= weave ? Mathf.Lerp(1.2f, 1.8f, Ease(AmAnswerAt, 1.2f, t)) : 1.55f + .18f * beats;
                    PlaceGlow(_abGlory[0], centre, Vector3.one * grow, Quaternion.identity, (.35f + .25f * beats) * strength * light * leave);
                    PlaceGlow(_abGlory[1], centre, Vector3.one * grow * .62f, Quaternion.identity, (.3f + .25f * beats) * strength * light * leave);
                }
            }

            // ---------------------------------------------------------------- THE VEIL.
            if (_abVeil >= 0)
            {
                AmihanGrade(t, out float brightness, out _);
                float away = Mathf.Clamp01((1f - brightness) / .30f);
                float throb = Mathf.Max(Decay(t - AmAnswerAt, .25f), Mathf.Max(Decay(t - AmCupAt, .3f), Decay(t - AmFlickAt, .25f)));
                throb = Mathf.Max(throb, Mathf.Max(Decay(t - AmPack1At, .2f), Decay(t - AmPack2At, .2f)));
                float deep = Ease(AmWeaveAt, AmCupAt, t) * (1f - Ease(AmDrawAt + .2f, AmWarpAt + .1f, t));
                float strength = _reducedEffects ? 0f : (.40f * away + .16f * deep + .10f * throb) * (1f - Ease(AmWarpAt, AmBraceAt + .2f, t));
                Place(_abVeil, Vector3.zero, Vector3.one, Quaternion.identity, strength);
            }
        }

        /// <summary>
        /// HER GLINT: a long rhombus cross (a tall diamond and a wide one) and a short one turned 45 degrees, each arm's u along
        /// it (so `_Tips` sharpens its points) and v across it (so `_Band` lights its middle). Unit size; billboarded.
        /// </summary>
        private static Mesh AbStar
        {
            get
            {
                if (_abStarMesh != null) return _abStarMesh;
                var v = new List<Vector3>(); var uv = new List<Vector2>(); var tris = new List<int>();
                // A rhombus arm: pointed at both ends, widest at the centre (four triangles round the middle).
                void Arm(float angle, float length, float width)
                {
                    float a = angle * Mathf.Deg2Rad;
                    var dir = new Vector3(Mathf.Sin(a), Mathf.Cos(a), 0f);
                    var across = new Vector3(dir.y, -dir.x, 0f) * width * .5f;
                    int b = v.Count;
                    v.Add(-dir * length * .5f); v.Add(-across); v.Add(dir * length * .5f); v.Add(across);
                    uv.Add(new Vector2(0f, .5f)); uv.Add(new Vector2(.5f, 0f)); uv.Add(new Vector2(1f, .5f)); uv.Add(new Vector2(.5f, 1f));
                    tris.AddRange(new[] { b, b + 1, b + 2, b, b + 2, b + 3 });
                }
                Arm(0f, 1f, .16f);
                Arm(90f, .74f, .14f);
                Arm(45f, .36f, .10f);
                Arm(135f, .36f, .10f);
                _abStarMesh = new Mesh { name = "AmihanGlintStar", hideFlags = HideFlags.DontSave };
                _abStarMesh.SetVertices(v); _abStarMesh.SetUVs(0, uv); _abStarMesh.SetTriangles(tris, 0);
                _abStarMesh.bounds = new Bounds(Vector3.zero, Vector3.one * 2f);
                return _abStarMesh;
            }
        }

        /// <summary>A kasikus outline in XY: a diamond of unit outer corner radius, a fifth of it thick (v across it, so the band
        /// lights its middle). Laid flat it is the court's diamond ring.</summary>
        private static Mesh AbDiamondRing
        {
            get
            {
                if (_abRingMesh != null) return _abRingMesh;
                const int PerSide = 6, Count = PerSide * 4;
                var v = new Vector3[(Count + 1) * 2]; var uv = new Vector2[v.Length]; var tris = new int[Count * 6];
                for (int i = 0; i <= Count; i++)
                {
                    int side = (i / PerSide) % 4; float f = (i % PerSide) / (float)PerSide;
                    if (i == Count) { side = 0; f = 0f; }
                    float a0 = side * Mathf.PI * .5f, a1 = (side + 1) * Mathf.PI * .5f;
                    var d = Vector3.Lerp(new Vector3(Mathf.Sin(a0), Mathf.Cos(a0), 0f), new Vector3(Mathf.Sin(a1), Mathf.Cos(a1), 0f), f);
                    v[i * 2] = d * .8f; v[i * 2 + 1] = d;
                    uv[i * 2] = new Vector2(i / (float)Count, 0f); uv[i * 2 + 1] = new Vector2(i / (float)Count, 1f);
                    if (i == Count) continue;
                    int b = i * 6, k = i * 2;
                    tris[b] = k; tris[b + 1] = k + 1; tris[b + 2] = k + 2; tris[b + 3] = k + 2; tris[b + 4] = k + 1; tris[b + 5] = k + 3;
                }
                _abRingMesh = new Mesh { name = "AmihanDiamondRing", hideFlags = HideFlags.DontSave, vertices = v, uv = uv, triangles = tris };
                _abRingMesh.bounds = new Bounds(Vector3.zero, new Vector3(2.2f, 2.2f, .2f));
                return _abRingMesh;
            }
        }
    }
}
