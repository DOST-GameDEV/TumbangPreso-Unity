using System.Collections.Generic;
using UnityEngine;

namespace TumbangPreso.Visual
{
    public sealed partial class HeroIntroductionScene
    {
        // =========================================================================================
        // PAETE, MAKILING'S EMBRACE, THE SKY SHE STANDS IN (v9, 2026-10-08). The owner, of the first cut with her vast in the sky
        // behind him: *"she needs to feel more divine with glow and sky effects. right now she doesnt look like a god in the
        // sky"*. That cut was her figure and nothing else: the same flat daylight sky behind her as behind a wall. A god in a
        // sky is told by what the SKY does round them, so this is that, and all of it is hers (jade) with his lime in its heart:
        //
        //   THE DUSK    the sky behind her steps down to a deep green dusk, in three rings that darken outward from her head,
        //               so her light has something to be light against.
        //   THE HALO    behind her head: a soft glow, a hot heart, two rings and a toothed crown that turn against each other,
        //               and a sunburst of long rays, each its own length, that opens as she takes her form and beats with him.
        //   THE CLOUDS  chunky clouds along the horizon that lie across her and PART as she comes close, and close again as
        //               she goes: they are what says she is beyond the world, and they hide where the horizon cuts her.
        //   THE SHAFTS  light falling from her halo to the court, slanted, each its own width.
        //   THE STARS   glints in the dusk round her, each on its own beat.
        //   HER OWN     her edges shine brighter as a ghost, and her full form has a rim of light on it (`MakilingSpirit.Shine`).
        //
        // ⚠️ Every piece is posed from the scene clock `t` (the world is paused), every row is typed and none is like another.
        // ⚠️ Everything is placed from her head (`MakilingSpirit.HeadWorld`) and sized by her size on this frame
        // (`SizeNow`), so it rides with her as she arrives far off and small, and as she comes close.
        // ⚠️ Reduced effects keeps every shape, halves the light and drops the shafts.
        // =========================================================================================

        // The dusk: (inner radius, outer radius, how dark), as shares of her size. The outer is far past any frame.
        private static readonly float[,] PsDuskRows = { { 0.00f, 7.0f, .30f }, { 0.95f, 7.0f, .22f }, { 1.75f, 7.0f, .20f } };
        private static readonly Color PsDusk = new Color(0.02f, 0.09f, 0.08f, 1f);

        // The sunburst behind her head: angle (degrees from straight up), length, width, when it opens. Lengths and widths are
        // in the ray mesh's own units (the piece is scaled to her), each typed: long ones up and out, short ones between.
        private static readonly float[,] PsRayRows =
        {
            { 4f, 3.3f, .20f, .00f }, { 31f, 1.9f, .11f, .05f }, { 58f, 2.7f, .16f, .02f }, { 84f, 1.5f, .09f, .08f }, { 112f, 2.2f, .14f, .04f },
            { 143f, 1.2f, .08f, .10f }, { 171f, 1.6f, .10f, .07f }, { 203f, 1.3f, .08f, .11f }, { 229f, 2.0f, .13f, .03f }, { 257f, 1.6f, .10f, .09f },
            { 283f, 2.9f, .17f, .01f }, { 309f, 1.8f, .10f, .06f }, { 338f, 2.4f, .14f, .03f },
        };

        // The clouds: where along the horizon (x), how high, how far in front of her (toward the court), size, which way and
        // how far it parts as she comes close, its drift (metres a second), which of the two shapes. Metres, in his space.
        private static readonly float[,] PsCloudRows =
        {
            { -13f, 3.0f, 13f, 14f, -19f, .50f, 0 }, { 11f, 2.0f, 16f, 16f, 21f, -.40f, 1 }, { -41f, 7.0f, 8f, 19f, -11f, .30f, 1 },
            { 37f, 5.5f, 10f, 21f, 13f, -.35f, 0 }, { -67f, 15f, 2f, 23f, -6f, .25f, 0 }, { 63f, 12f, 4f, 20f, 8f, -.30f, 1 },
            { -25f, .5f, 21f, 11f, -23f, .45f, 1 }, { 26f, 0f, 23f, 10f, 25f, -.50f, 0 }, { -53f, 26f, -6f, 15f, -4f, .20f, 1 },
            { 51f, 29f, -8f, 13f, 5f, -.22f, 0 },
        };
        // A cloud is a handful of balls: (x, y, z, radius), in its own units. Two shapes, each typed.
        private static readonly float[][,] PsPuffs =
        {
            new[,] { { 0f, 0f, 0f, 1.0f }, { -.90f, -.15f, .10f, .75f }, { .85f, -.20f, -.10f, .80f }, { -.40f, .45f, 0f, .70f }, { .40f, .40f, .10f, .62f }, { 1.60f, -.35f, 0f, .50f } },
            new[,] { { 0f, 0f, 0f, .95f }, { -1.0f, -.10f, 0f, .80f }, { -1.9f, -.30f, .10f, .55f }, { .90f, -.15f, 0f, .70f }, { .20f, .50f, 0f, .72f } },
        };
        private static readonly Color PsCloud = new Color(0.78f, 0.92f, 0.83f, 1f);

        // The shafts from her halo to the court: where each lands (x, and how far in front of her), its width, its lean
        // (degrees), its beat. Metres.
        private static readonly float[,] PsShaftRows =
        {
            { -34f, 30f, 7.0f, 14f, .0f }, { -19f, 46f, 4.5f, 6f, .7f }, { -55f, 22f, 9.0f, -4f, 1.9f }, { 27f, 50f, 5.0f, -11f, 1.2f }, { 45f, 26f, 6.5f, -18f, 2.6f },
        };

        // The stars in the dusk: where from her head (x, y, as shares of her size), size (metres), beat, whose light (0 his, 1 hers).
        private static readonly float[,] PsStarRows =
        {
            { -1.55f, .62f, 5.0f, .0f, 1 }, { 1.30f, .88f, 3.6f, 1.3f, 0 }, { -.92f, 1.32f, 4.2f, 2.1f, 1 }, { .70f, 1.46f, 2.8f, .6f, 1 },
            { 1.92f, .26f, 4.6f, 2.7f, 0 }, { -2.10f, -.10f, 3.2f, 1.7f, 1 }, { 2.36f, 1.10f, 2.6f, 3.3f, 1 }, { -1.30f, 1.78f, 3.0f, 3.9f, 0 },
            { .18f, 1.92f, 3.8f, 4.4f, 1 }, { -2.56f, .84f, 2.4f, 5.0f, 1 },
        };

        private readonly List<int> _psDusk = new List<int>(3), _psClouds = new List<int>(10), _psShafts = new List<int>(5), _psStars = new List<int>(10);
        private int _psHaloSoft = -1, _psHaloHot = -1, _psRingIn = -1, _psRingOut = -1, _psCrown = -1, _psRays = -1;
        private Mesh _psRayMesh;

        private void BuildPaeteSky()
        {
            for (int i = 0; i < PsDuskRows.GetLength(0); i++)
            {
                int piece = Add("PaeteSkyDusk" + i, PsAnnulus(PsDuskRows[i, 0], PsDuskRows[i, 1], 56), new Color(PsDusk.r, PsDusk.g, PsDusk.b, PsDuskRows[i, 2]), 0f, plain: true);
                _psDusk.Add(piece);
            }
            _psHaloSoft = AddGlow("PaeteSkyHalo", MakilingJade, falloff: 1.5f, core: 0f);
            _psHaloHot = AddGlow("PaeteSkyHeart", PaeteLight, falloff: 2.2f, core: .3f);
            _psRingIn = AddGlow("PaeteSkyRingIn", PaeteLight, PbRing, billboard: false, band: true, falloff: 1.3f, core: .6f);
            _psRingOut = AddGlow("PaeteSkyRingOut", MakilingJade, PbRing, billboard: false, band: true, falloff: 1.3f, core: .5f);
            _psCrown = Add("PaeteSkyCrown", VfxShapes.Corona(19, .80f, .55f, 7), new Color(MakilingJade.r, MakilingJade.g, MakilingJade.b, .55f), 1f, plain: true);
            _psRayMesh = PbDynamic("PaeteSkyRays");
            _psRays = PvTips(AddGlow("PaeteSkyRays", MakilingJade, _psRayMesh, billboard: true, band: true, falloff: 1.4f, core: .5f), 1.2f);
            for (int i = 0; i < PsCloudRows.GetLength(0); i++)
            {
                int piece = AddSolid("PaeteSkyCloud" + i, PsCloudMesh((int)PsCloudRows[i, 6]), PsCloud);
                // They are far and vast: no shadow of theirs belongs on the court.
                _pieces[piece].Renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                _pieces[piece].Renderer.receiveShadows = false;
                // Film sk6: lit only by the map's sun they were a dull grey-green. They carry her light: their own glow, in her jade.
                var cloud = _pieces[piece].Renderer.sharedMaterial;
                if (cloud != null && cloud.HasProperty("_EmissionColor"))
                {
                    cloud.EnableKeyword("_EMISSION");
                    cloud.SetColor("_EmissionColor", new Color(0.36f, 0.50f, 0.42f, 1f));
                    cloud.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
                }
                _psClouds.Add(piece);
            }
            for (int i = 0; i < PsShaftRows.GetLength(0); i++)
                _psShafts.Add(PvTips(AddGlow("PaeteSkyShaft" + i, MakilingJade, PvStreak, billboard: false, band: true, falloff: 1.3f, core: .3f), .8f));
            for (int i = 0; i < PsStarRows.GetLength(0); i++)
                _psStars.Add(PvTips(AddGlow("PaeteSkyStar" + i, PsStarRows[i, 4] > .5f ? MakilingJade : PaeteLight, PbStar, billboard: true, band: true, falloff: 1.6f, core: .9f), 1.1f));
        }

        private void SamplePaeteSky(float t, float leave)
        {
            if (_makilingSpirit == null || _psHaloSoft < 0) return;
            float court = _paeteCourt;
            float light = _reducedEffects ? .5f : 1f;
            // She is here from her rising to the mist taking her (the same two eases her own `Look` has).
            float here = Ease(.02f, .20f, t) * (1f - Ease(3.30f, 3.95f, t)) * leave;
            float close = Ease(.30f, .52f, t);
            var head = _root.transform.InverseTransformPoint(_makilingSpirit.HeadWorld);
            float size = _makilingSpirit.SizeNow;
            // What beats in the sky when it beats in him: her light given, his eyes, the slam, his three heartbeats, the send.
            float beat = .9f * Decay(t - .70f, .3f) + .6f * Decay(t - PaeteSlamAt, .35f) + .5f * Decay(t - PaeteSendAt, .3f);
            foreach (float p in PaetePulses) beat += .35f * Decay(t - p, .2f);

            // ---------------------------------------------------------------- THE DUSK behind her.
            // ⚠️ Film sk6: sized by her size on the frame, its outer edge was a great arc across the sky while she was far and
            // small; and it is a flat sheet facing the court, so the wide shots from the side (the RISE) showed it edge-on as a
            // dark slab. It is her full size always, and it is gone before the camera leaves these two shots (the dive, 1.93).
            // Close behind her hair, so it stands in front of whatever wall she has been stood in front of (`PaeteSkyRoom`).
            var behind = new Vector3(head.x, head.y, MakilingSky.z - .52f * MakilingSkyScale);
            // ⚠️ IT HOLDS FOR AS LONG AS SHE IS THERE NOW (owner, 2026-10-08: "you can see a mismatch when it cuts back to both paete
            // and the tree"). It used to close before the dive because the RISE was shot from the side, where this flat sheet
            // is edge-on; so the picture went under the court from a dusk with a goddess in it and came back up into plain
            // daylight with neither. The RISE is shot from in front now, with her in the sky behind the tree
            // (`tools/author_ultimate_intros.py`), so the dusk, the open sky and she all stay until the mist takes her.
            float dusk = here;
            for (int i = 0; i < _psDusk.Count; i++)
                Place(_psDusk[i], behind - Vector3.forward * (.03f * MakilingSkyScale * i), Vector3.one * MakilingSkyScale, Quaternion.identity, dusk);

            // ---------------------------------------------------------------- THE MAP, OPENED TO HER (`NearFade.shader`, § THE SKY, OPENED).
            // A window round her breast, wide enough for her hair, her halo and her clouds: everything within 48 degrees of the
            // line to her goes, nothing beyond 62 (film wall2: at 38 and 52 the wall's stippled edge cut across her clouds). The ground stays, up to a knee's height over the court.
            // ⚠️ NOTHING IS KEPT FOR BEING NEAR (owner, 2026-10-08, with a screenshot of a pillar 5 m from the lens standing solid
            // across her: "dissolve doesnt work if the wall is too close"). The first cut kept everything within 8 m of the
            // lens, to protect him and her meadow; but neither wears the map's shader, so the guard protected only walls. It closes with the dusk, before the camera leaves her.
            var window = _makilingSpirit.HeadWorld + Vector3.down * (.2f * size);
            NearFade.OpenSky(window, dusk, 48f, 62f, .3f, _root.transform.position.y + court + .6f);

            // ---------------------------------------------------------------- HER HALO, behind her head.
            float lit = here * Ease(.06f, .26f, t);
            var halo = new Vector3(head.x, head.y + .02f * size, head.z - .45f * size);
            PlaceGlow(_psHaloSoft, halo, Vector3.one * 2.3f * size * (1f + .06f * beat), Quaternion.identity, (.55f + .25f * beat) * lit * light);
            PlaceGlow(_psHaloHot, halo, Vector3.one * 1.0f * size * (1f + .12f * beat), Quaternion.identity, (.40f + .45f * beat) * lit * light);
            PlaceGlow(_psRingIn, halo, Vector3.one * .50f * size, Quaternion.Euler(0f, 0f, t * 14f), (.85f + .6f * beat) * lit * light);
            PlaceGlow(_psRingOut, halo + Vector3.back * (.02f * size), Vector3.one * .68f * size, Quaternion.Euler(0f, 0f, -t * 9f), (.55f + .4f * beat) * lit * light);
            // The toothed crown stands upright behind her (`Corona` is built flat) and turns slowly.
            Place(_psCrown, halo + Vector3.back * (.04f * size), Vector3.one * .86f * size, Quaternion.Euler(0f, 0f, t * 6f) * Quaternion.Euler(90f, 0f, 0f), lit * light);
            float open = t - .08f;
            if (open < 0f || lit <= .002f) { _psRayMesh.Clear(); PvHide(_psRays); }
            else
            {
                PbRays(_psRayMesh, PsRayRows, open, 5f, 1.02f, 1f);
                PlaceGlow(_psRays, halo + Vector3.back * (.06f * size), Vector3.one * .36f * size, Quaternion.identity, (.75f + .5f * beat) * lit * light);
            }

            // ---------------------------------------------------------------- THE CLOUDS along the horizon: they part as she comes close.
            for (int i = 0; i < _psClouds.Count; i++)
            {
                float grown = Ease(.0f + .012f * i, .16f + .012f * i, t) * (1f - Ease(3.40f + .03f * i, 3.95f, t));
                if (grown <= .002f || leave <= .002f) { Place(_psClouds[i], Vector3.zero, Vector3.one * .001f, Quaternion.identity, 0f); continue; }
                // The rows are typed for an open court (her 72 m off); nearer, the whole bank shrinks with her (`_paeteSkyShare`).
                float k = _paeteSkyShare;
                float x = PsCloudRows[i, 0] + PsCloudRows[i, 4] * close + PsCloudRows[i, 5] * PaeteReal(t);
                var at = new Vector3(MakilingSky.x + x * k, court + PsCloudRows[i, 1] * k, MakilingSky.z + PsCloudRows[i, 2] * k);
                Place(_psClouds[i], at, Vector3.one * PsCloudRows[i, 3] * .5f * grown * k, Quaternion.Euler(0f, 8f * (i % 3) - 8f, 0f), leave);
            }

            // ---------------------------------------------------------------- THE SHAFTS from her halo to the court.
            for (int i = 0; i < _psShafts.Count; i++)
            {
                if (_reducedEffects) { PvHide(_psShafts[i]); continue; }
                var foot = new Vector3(MakilingSky.x + PsShaftRows[i, 0] * _paeteSkyShare, court, MakilingSky.z + PsShaftRows[i, 1] * _paeteSkyShare);
                // Each leaves the rim of her halo on its own side (film sk6: leaving its middle, one fell down across her face).
                var top = halo + new Vector3(Mathf.Sign(PsShaftRows[i, 0]) * .62f * size + PsShaftRows[i, 0] * .3f * _paeteSkyShare, .15f * size, .3f * size);
                var along = top - foot;
                float length = along.magnitude;
                float shimmer = .65f + .35f * Mathf.Sin(t * 2.3f + PsShaftRows[i, 4]);
                // The streak's long side is its y: stood from its foot to her halo, its flat side to the court.
                PlaceGlow(_psShafts[i], (top + foot) * .5f, new Vector3(PsShaftRows[i, 2] * (.8f + .4f * close) * _paeteSkyShare, length, 1f),
                          Quaternion.FromToRotation(Vector3.up, along / Mathf.Max(.01f, length)), .22f * shimmer * lit * close * light);
            }

            // ---------------------------------------------------------------- THE STARS in the dusk.
            for (int i = 0; i < _psStars.Count; i++)
            {
                var at = new Vector3(head.x + PsStarRows[i, 0] * size, head.y + (PsStarRows[i, 1] - .5f) * size, head.z - .3f * size);
                float twinkle = Mathf.Max(0f, Mathf.Sin(t * 3.1f + PsStarRows[i, 3]));
                PlaceGlow(_psStars[i], at, Vector3.one * PsStarRows[i, 2] * _paeteSkyShare * (.5f + .5f * close) * (.6f + .4f * twinkle), Quaternion.identity,
                          1.6f * twinkle * twinkle * here * light);
            }

            // ---------------------------------------------------------------- HER OWN LIGHT.
            _makilingSpirit.Shine(.62f, .95f + .5f * beat, .55f);
        }

        /// <summary>A flat ring in XY of unit size (its radii are shares of the size it is scaled to), facing the court.</summary>
        private static Mesh PsAnnulus(float inner, float outer, int sides)
        {
            var v = new Vector3[(sides + 1) * 2]; var tris = new int[sides * 6];
            for (int i = 0; i <= sides; i++)
            {
                float a = i * Mathf.PI * 2f / sides;
                var d = new Vector3(Mathf.Sin(a), Mathf.Cos(a), 0f);
                v[i * 2] = d * inner; v[i * 2 + 1] = d * outer;
                if (i == sides) continue;
                int b = i * 6, k = i * 2;
                tris[b] = k; tris[b + 1] = k + 1; tris[b + 2] = k + 2; tris[b + 3] = k + 2; tris[b + 4] = k + 1; tris[b + 5] = k + 3;
            }
            var mesh = new Mesh { name = "PaeteSkyDusk", hideFlags = HideFlags.DontSave, vertices = v, triangles = tris };
            mesh.RecalculateNormals();
            mesh.bounds = new Bounds(Vector3.zero, new Vector3(outer * 2f, outer * 2f, .2f));
            return VfxShapes.TwoSided(mesh);
        }

        /// <summary>A cloud: its balls (`PsPuffs`) as one mesh, their undersides pressed flat as a cloud's are.</summary>
        private static Mesh PsCloudMesh(int shape)
        {
            var puffs = PsPuffs[Mathf.Clamp(shape, 0, PsPuffs.Length - 1)];
            const int Rings = 8, Sides = 14;
            var v = new List<Vector3>(); var n = new List<Vector3>(); var tris = new List<int>();
            for (int p = 0; p < puffs.GetLength(0); p++)
            {
                var centre = new Vector3(puffs[p, 0], puffs[p, 1], puffs[p, 2]);
                float r = puffs[p, 3];
                int first = v.Count;
                for (int ring = 0; ring <= Rings; ring++)
                {
                    float lat = Mathf.PI * ring / Rings;
                    for (int side = 0; side <= Sides; side++)
                    {
                        float lon = Mathf.PI * 2f * side / Sides;
                        var d = new Vector3(Mathf.Sin(lat) * Mathf.Cos(lon), Mathf.Cos(lat), Mathf.Sin(lat) * Mathf.Sin(lon));
                        var at = centre + d * r;
                        // Pressed flat underneath.
                        if (at.y < -.42f) { at.y = -.42f; d = Vector3.Lerp(d, Vector3.down, .7f).normalized; }
                        v.Add(at); n.Add(d);
                    }
                }
                for (int ring = 0; ring < Rings; ring++)
                    for (int side = 0; side < Sides; side++)
                    {
                        int a = first + ring * (Sides + 1) + side, b = a + Sides + 1;
                        tris.Add(a); tris.Add(a + 1); tris.Add(b);
                        tris.Add(a + 1); tris.Add(b + 1); tris.Add(b);
                    }
            }
            var mesh = new Mesh { name = "PaeteSkyCloud" + shape, hideFlags = HideFlags.DontSave };
            mesh.SetVertices(v); mesh.SetNormals(n); mesh.SetTriangles(tris, 0);
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
