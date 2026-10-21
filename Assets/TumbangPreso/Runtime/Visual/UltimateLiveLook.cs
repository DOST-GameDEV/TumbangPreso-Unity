using System.Collections.Generic;
using UnityEngine;

namespace TumbangPreso.Visual
{
    /// <summary>
    /// ⚠️ A QUICK REWORK OF FOUR ULTIMATES' IN-MATCH LOOK, OFF BY DEFAULT. Each switch below defaults to false, and the
    /// one hook line per ultimate does nothing while it is false, so the game is unchanged until the owner turns one on.
    ///
    /// Every piece is a mesh built ONCE (cached in a static), drawn with one of a few shared materials of
    /// `TumbangPreso/UltLiveBands`, and animated entirely in that shader from `_Born`: it reveals outward only as far
    /// as the rule has travelled, then erodes in place. Nothing here runs per frame; a piece is a GameObject destroyed
    /// on a timer, and its property block is written once at spawn.
    ///
    /// ⚠️ What is drawn is the rule's footprint: Airburst's half-plane fan out to the court range plus the ring around
    /// her, Absolute Zero's map-wide frost front, Baha's lane (half width 3 m) to its measured stop distance, and
    /// Overclock's 4.5 m ring under the strike. Airburst's fan is drawn only inside the court's playable box (the rule
    /// itself reaches 40 m).
    /// ⚠️ q2, 2026-10-18: Baha, Overclock and the rime wall were near invisible on the sunlit court in the q1 film.
    /// They now carry a solid painted body (`_Cover` in the shader) in near-opaque, darker, saturated bands, because
    /// pale see-through colour is what a bright court with no bloom washes out.
    /// ⚠️ These pieces are drawn IN ADDITION to the current live effects; the switch does not hide the old ones.
    /// </summary>
    public static class UltimateLiveLook
    {
        public static bool AmihanAirburst = false;
        public static bool CheskaAbsoluteZero = false;
        public static bool RafiBaha = false;
        public static bool ZackOverclock = false;

        private static Mesh _sectorHalf, _disc, _annulus, _shell, _strip, _crest;
        private static readonly Dictionary<string, Material> Materials = new Dictionary<string, Material>();
        private static MaterialPropertyBlock _block;
        private static readonly int BornId = Shader.PropertyToID("_Born");
        private static readonly int ExtentId = Shader.PropertyToID("_Extent");
        private static readonly int TravelId = Shader.PropertyToID("_Travel");
        private static readonly int FadeId = Shader.PropertyToID("_Fade");
        private static readonly int ClipId = Shader.PropertyToID("_Clip");

        // ------------------------------------------------------------------ the four ultimates

        /// <summary>Airburst: a wind sheet over the front half-plane to `range`, and a ring of it around her.</summary>
        public static void Amihan(Vector3 at, Vector3 forward, float range, float aroundRadius)
        {
            var silk = Mat("amihan_silk", "erode_streak", C(0xF4FFF8, .95f), C(0x8FE3CF, .55f), C(0x5CB8B0, .20f),
                new Vector4(1.6f, 7f, 2.6f, 11f), new Vector4(-1.1f, .04f, -.55f, -.07f));
            var veil = Mat("amihan_veil", "erode_cloud", C(0xFFFFFF, .85f), C(0xA6EBDA, .40f), C(0x6FC6BB, .14f),
                new Vector4(2.2f, 5f, 3.3f, 3.1f), new Vector4(-.7f, -.06f, -.3f, .05f));
            // ⚠️ The rule reaches `range` (40 m, the court's diagonal at least), far past the plaza: q1 drew the fan over the
            // houses. What a player can stand in is the court's playable box, so the drawn fan is clipped to it and its
            // radius stops at the farthest corner from her. The rule, its reach and its timing are unchanged.
            var clip = new Vector4(AIController.PlayableMinX, AIController.PlayableMaxX, AIController.PlayableMinZ, AIController.PlayableMaxZ);
            silk.SetVector(ClipId, clip); veil.SetVector(ClipId, clip);
            float corner = 0f;
            for (int k = 0; k < 4; k++)
            {
                float cx = (k & 1) == 0 ? clip.x : clip.y, cz = (k & 2) == 0 ? clip.z : clip.w;
                corner = Mathf.Max(corner, new Vector2(cx - at.x, cz - at.z).magnitude);
            }
            float drawn = Mathf.Clamp(corner, aroundRadius, range);
            float fly = range / 30f;
            var rot = Look(forward);
            Piece(SectorHalf, silk, at + Vector3.up * .05f, rot, new Vector3(drawn, 1, drawn), drawn,
                new Vector4(0, 30, drawn, 0), new Vector2(fly + .3f, .9f));
            Piece(SectorHalf, veil, at + Vector3.up * .45f, rot, new Vector3(drawn * .96f, 1, drawn * .96f), drawn * .96f,
                new Vector4(.08f, 26, drawn * .96f, 0), new Vector2(fly + .15f, .8f));
            Piece(Disc, silk, at + Vector3.up * .08f, rot, new Vector3(aroundRadius, 1, aroundRadius), aroundRadius,
                new Vector4(0, 30, aroundRadius, 0), new Vector2(.6f, .7f));
        }

        /// <summary>Absolute Zero: a frost front that rolls over the whole map, and a rising rime wall round her.</summary>
        public static void Cheska(Vector3 at)
        {
            var rime = Mat("cheska_rime", "crystal_grunge", C(0xFFFFFF, .95f), C(0xC9F1FF, .58f), C(0x86CDE8, .22f),
                new Vector4(5f, 9f, 8f, 5.5f), new Vector4(-.35f, .02f, -.12f, -.03f));
            // ⚠️ q1's wall was frost_fern at 0.5 alpha with the shell fading out above its lower third: thin white
            // scribbles. Now a chunky crystal mask over a solid floor: white and pale ice cells over a deep blue band
            // for value contrast on the pale road, the blue kept part see-through so a player inside still shows,
            // and the shell's fall-off flattened so the wall stands its full height with a jagged, mask-eaten top.
            var wall = Mat("cheska_wall", "crystal_grunge", C(0xFFFFFF, 1f), C(0xA8E2FA, .84f), C(0x3F93CC, .68f),
                new Vector4(2.6f, 3.2f, 3.9f, 2.4f), new Vector4(-.12f, .06f, .08f, -.05f),
                new Vector4(.3f, 2.4f, .75f, 0), new Vector4(.2f, .5f, .9f, 0));
            const float map = 26f;
            Piece(Disc, rime, at + Vector3.up * .03f, Quaternion.identity, new Vector3(map, 1, map), map,
                new Vector4(0, 20, map, 0), new Vector2(2.1f, 1.0f));
            Piece(Shell, wall, at, Quaternion.identity, new Vector3(4.6f, 2.6f, 4.6f), 2.6f,
                new Vector4(0, 7, 2.6f, 0), new Vector2(1.6f, .9f));
        }

        /// <summary>Baha: the lane fills behind a travelling crest, from the rule's own origin, forward and distance.</summary>
        public static void Rafi(Transform parent, Vector3 origin, Vector3 forward, float halfWidth, float distance,
                                float warning, float speed, float duration, float age)
        {
            // ⚠️ q1 was pale flecks on the sunlit road: the mask alone rarely cleared the veil cut and every band was pale
            // and half transparent. Now the lane is a painted water BODY: a floor under the mask fills the whole 6 m
            // lane, the body band is a dark saturated blue at 0.9 alpha (the road is pale, so the water is darker than
            // it), lighter blue patches and white foam cells move through it, and the crest is a tall near-opaque foam
            // wall at the front with its fall-off flattened so it stands its full height.
            var flood = Mat("rafi_flood", "water_flow", C(0xF4FCFF, 1f), C(0x1D6FB8, .9f), C(0x47A3DC, .82f),
                new Vector4(1.8f, 1.2f, 2.9f, 1.9f), new Vector4(-.9f, .03f, -.45f, -.06f),
                new Vector4(.36f, 2.2f, .35f, 0), new Vector4(.16f, .44f, .98f, 0));
            var crest = Mat("rafi_crest", "water_foam", C(0xFFFFFF, 1f), C(0xBDE8FA, .96f), C(0x2C82C6, .92f),
                new Vector4(1.3f, 2.2f, 2.1f, 3.4f), new Vector4(-.8f, .1f, -.35f, -.15f),
                new Vector4(.42f, 2.2f, .7f, 0), new Vector4(.16f, .4f, .78f, 0));
            float born = Time.time - age;
            float length = Mathf.Max(.1f, distance);
            var rot = Look(forward);
            var travel = new Vector4(warning, speed, length, 0);
            var fade = new Vector2(Mathf.Max(warning, duration - .6f), .6f);
            Piece(Strip, flood, origin + Vector3.up * .02f, rot, new Vector3(halfWidth, 1, length), length,
                travel, fade, parent, born);
            Piece(Crest, crest, origin, rot, new Vector3(halfWidth, 1.4f, 1.2f), 1, travel, fade, parent, born);
        }

        /// <summary>Overclock: a bolt column on the strike point, and the ring it scorches into the ground.</summary>
        public static void Zack(Vector3 at, float radius)
        {
            // ⚠️ q1 showed a faint orange ring for 0.2 s and almost no column: 0.26 to 0.72 alpha, a mask that rarely
            // cleared the cut, and a shell that faded out above its lower third. Now the column is a solid painted
            // gold bolt (white core cells, Quick Circuit's gold body, a deep orange band for contrast) standing its full
            // 10 m, held 0.7 s before it erodes; the ground gets a solid gold band at the rule's 4.5 m edge and a
            // scorched crackle inside it, both held about as long.
            var bolt = Mat("zack_bolt", "zack_crackle", C(0xFFFFFF, 1f), C(0xFFC61A, .96f), C(0xE06A0E, .9f),
                new Vector4(1f, 1.6f, 1.9f, 2.4f), new Vector4(-3.2f, .3f, -1.7f, -.5f),
                new Vector4(.58f, 2.4f, .85f, 0), new Vector4(.18f, .5f, .95f, 0));
            var ring = Mat("zack_ring", "zack_crackle", C(0xFFFFFF, 1f), C(0xFFC21A, .95f), C(0xD9620C, .9f),
                new Vector4(1.4f, 9f, 2.3f, 6f), new Vector4(-1.6f, .2f, -.8f, -.3f),
                new Vector4(.58f, 2.4f, .8f, 0), new Vector4(.18f, .5f, .95f, 0));
            var scorch = Mat("zack_scorch", "zack_crackle", C(0xFFF4C0, 1f), C(0xFFB21A, .9f), C(0xB04A0C, .78f),
                new Vector4(3f, 6f, 4.6f, 3.4f), new Vector4(-1.6f, .2f, -.8f, -.3f),
                new Vector4(0, 3.2f, .5f, 0), new Vector4(.25f, .5f, .85f, 0));
            Piece(Shell, bolt, at, Quaternion.identity, new Vector3(.7f, 10f, .7f), 10f,
                new Vector4(0, 60, 10, 0), new Vector2(.7f, .3f));
            Piece(Annulus, ring, at + Vector3.up * .05f, Quaternion.identity, new Vector3(radius, 1, radius), radius,
                new Vector4(.03f, 28, radius, 0), new Vector2(.75f, .35f));
            Piece(Disc, scorch, at + Vector3.up * .04f, Quaternion.identity, new Vector3(radius, 1, radius), radius,
                new Vector4(.03f, 28, radius, 0), new Vector2(.6f, .4f));
        }

        // ------------------------------------------------------------------ pieces

        private static void Piece(Mesh mesh, Material mat, Vector3 at, Quaternion rot, Vector3 scale, float extent,
                                  Vector4 travel, Vector2 fade, Transform parent = null, float born = -1)
        {
            if (mat == null) return;
            var go = new GameObject("UltLive-" + mat.name);
            go.transform.SetPositionAndRotation(at, rot);
            go.transform.localScale = scale;
            if (parent != null) go.transform.SetParent(parent, true);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var r = go.AddComponent<MeshRenderer>();
            r.sharedMaterial = mat;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
            // The crest rides forward in the vertex program, so its box must cover the whole lane.
            if (mesh == _crest) r.localBounds = new Bounds(new Vector3(0, .5f, (travel.z + 1) * .5f), new Vector3(2, 1, travel.z + 2));
            _block ??= new MaterialPropertyBlock();
            _block.Clear();
            float b = born >= 0 ? born : Time.time;
            _block.SetFloat(BornId, b);
            _block.SetFloat(ExtentId, extent);
            _block.SetVector(TravelId, travel);
            _block.SetVector(FadeId, new Vector4(fade.x, fade.y, 0, 0));
            r.SetPropertyBlock(_block);
            Object.Destroy(go, Mathf.Max(.1f, fade.x + fade.y + .1f - (Time.time - b)));
        }

        // `cover` = (floor under the mask, mask gain, flatten vertex alpha, unused) and `cuts` = (veil, body, core); both
        // default to the shader's own defaults, which are the q1 look. The shader's header says why cover exists.
        private static Material Mat(string name, string mask, Color core, Color body, Color veil, Vector4 tile, Vector4 pan,
                                    Vector4? cover = null, Vector4? cuts = null)
        {
            if (Materials.TryGetValue(name, out var m) && m != null) return m;
            var shader = Shader.Find("TumbangPreso/UltLiveBands");
            if (shader == null) return null;
            m = new Material(shader) { name = name };
            var tex = Resources.Load<Texture2D>("Textures/UltimateFx/" + mask);
            if (tex != null) m.SetTexture("_Mask", tex);
            m.SetColor("_Core", core); m.SetColor("_Body", body); m.SetColor("_Veil", veil);
            m.SetVector("_Tile", tile); m.SetVector("_Pan", pan);
            if (cover.HasValue) m.SetVector("_Cover", cover.Value);
            if (cuts.HasValue) m.SetVector("_Cuts", cuts.Value);
            Materials[name] = m;
            return m;
        }

        private static Color C(int hex, float a)
            => new Color(((hex >> 16) & 255) / 255f, ((hex >> 8) & 255) / 255f, (hex & 255) / 255f, a);

        private static Quaternion Look(Vector3 forward)
        {
            forward.y = 0;
            return forward.sqrMagnitude < 1e-4f ? Quaternion.identity : Quaternion.LookRotation(forward.normalized, Vector3.up);
        }

        // ------------------------------------------------------------------ meshes, built once
        // Colour channels: a = how much of the mask shows (falls off to rims and tips), g = 1 rides the travelling front.

        private static Mesh SectorHalf => _sectorHalf != null ? _sectorHalf : (_sectorHalf = Sector(90f, 48, 10));
        private static Mesh Disc => _disc != null ? _disc : (_disc = Sector(180f, 72, 10));
        private static Mesh Annulus => _annulus != null ? _annulus : (_annulus = BuildAnnulus(.8f, 72, 3));
        private static Mesh Shell => _shell != null ? _shell : (_shell = BuildShell(40, 8));
        private static Mesh Strip => _strip != null ? _strip : (_strip = BuildStrip(24, 8));
        private static Mesh Crest => _crest != null ? _crest : (_crest = BuildCrest(16, 4));

        private static Mesh Sector(float halfAngle, int segA, int segR)
        {
            var v = new List<Vector3>(); var uv = new List<Vector2>(); var col = new List<Color>(); var tri = new List<int>();
            bool full = halfAngle >= 179.9f;
            for (int i = 0; i <= segR; i++)
            {
                float r = i / (float)segR;
                for (int j = 0; j <= segA; j++)
                {
                    float f = j / (float)segA;
                    float ang = Mathf.Deg2Rad * Mathf.Lerp(-halfAngle, halfAngle, f);
                    v.Add(new Vector3(Mathf.Sin(ang) * r, 0, Mathf.Cos(ang) * r));
                    uv.Add(new Vector2(r, f * (full ? 6f : 3f)));
                    float side = full ? 1f : Mathf.SmoothStep(0, 1, Mathf.Min(f, 1 - f) * 8f);
                    float rim = Mathf.SmoothStep(0, 1, (1 - r) * 5f) * Mathf.SmoothStep(0, 1, r * 6f + .2f);
                    col.Add(new Color(1, 0, 1, side * rim));
                }
            }
            Grid(tri, segR, segA);
            return Make("UltLiveSector", v, uv, col, tri);
        }

        // A flat band from `inner` to 1 with crisp edges (alpha 1 across it), for a ring that must read as a line on the
        // ground. u is the true radius fraction, so the shader's outward reveal reaches it at the same moment as a disc.
        private static Mesh BuildAnnulus(float inner, int segA, int segR)
        {
            var v = new List<Vector3>(); var uv = new List<Vector2>(); var col = new List<Color>(); var tri = new List<int>();
            for (int i = 0; i <= segR; i++)
            {
                float r = Mathf.Lerp(inner, 1f, i / (float)segR);
                for (int j = 0; j <= segA; j++)
                {
                    float f = j / (float)segA, ang = f * Mathf.PI * 2f;
                    v.Add(new Vector3(Mathf.Sin(ang) * r, 0, Mathf.Cos(ang) * r));
                    uv.Add(new Vector2(r, f * 6f));
                    col.Add(new Color(1, 0, 1, 1));
                }
            }
            Grid(tri, segR, segA);
            return Make("UltLiveAnnulus", v, uv, col, tri);
        }

        private static Mesh BuildShell(int segA, int segH)
        {
            var v = new List<Vector3>(); var uv = new List<Vector2>(); var col = new List<Color>(); var tri = new List<int>();
            for (int i = 0; i <= segH; i++)
            {
                float h = i / (float)segH;
                for (int j = 0; j <= segA; j++)
                {
                    float f = j / (float)segA, ang = f * Mathf.PI * 2f;
                    // A slight flare toward the foot, so the shell reads as rising out of the ground and not as a pipe.
                    float r = 1f + (1 - h) * (1 - h) * .25f;
                    v.Add(new Vector3(Mathf.Cos(ang) * r, h, Mathf.Sin(ang) * r));
                    uv.Add(new Vector2(h, f * 4f));
                    col.Add(new Color(1, 0, 1, Mathf.SmoothStep(0, 1, (1 - h) * 3f)));
                }
            }
            Grid(tri, segH, segA);
            return Make("UltLiveShell", v, uv, col, tri);
        }

        private static Mesh BuildStrip(int segL, int segW)
        {
            var v = new List<Vector3>(); var uv = new List<Vector2>(); var col = new List<Color>(); var tri = new List<int>();
            for (int i = 0; i <= segL; i++)
            {
                float z = i / (float)segL;
                for (int j = 0; j <= segW; j++)
                {
                    float f = j / (float)segW;
                    v.Add(new Vector3(Mathf.Lerp(-1, 1, f), 0, z));
                    uv.Add(new Vector2(z, f * 1.5f));
                    col.Add(new Color(1, 0, 1, Mathf.SmoothStep(0, 1, Mathf.Min(f, 1 - f) * 6f)));
                }
            }
            Grid(tri, segL, segW);
            return Make("UltLiveStrip", v, uv, col, tri);
        }

        private static Mesh BuildCrest(int segW, int segH)
        {
            var v = new List<Vector3>(); var uv = new List<Vector2>(); var col = new List<Color>(); var tri = new List<int>();
            for (int i = 0; i <= segH; i++)
            {
                float h = i / (float)segH;
                for (int j = 0; j <= segW; j++)
                {
                    float f = j / (float)segW;
                    // Leans forward at the top, like a breaking lip.
                    v.Add(new Vector3(Mathf.Lerp(-1, 1, f), h, h * h * .35f));
                    uv.Add(new Vector2(h, f * 2f));
                    col.Add(new Color(1, 1, 1, Mathf.SmoothStep(0, 1, Mathf.Min(f, 1 - f) * 6f) * Mathf.SmoothStep(0, 1, (1 - h) * 2.5f)));
                }
            }
            Grid(tri, segH, segW);
            return Make("UltLiveCrest", v, uv, col, tri);
        }

        private static void Grid(List<int> tri, int rows, int cols)
        {
            int w = cols + 1;
            for (int i = 0; i < rows; i++)
                for (int j = 0; j < cols; j++)
                {
                    int a = i * w + j, b = a + 1, c = a + w, d = c + 1;
                    tri.Add(a); tri.Add(c); tri.Add(b); tri.Add(b); tri.Add(c); tri.Add(d);
                }
        }

        private static Mesh Make(string name, List<Vector3> v, List<Vector2> uv, List<Color> col, List<int> tri)
        {
            var m = new Mesh { name = name };
            m.SetVertices(v); m.SetUVs(0, uv); m.SetColors(col); m.SetTriangles(tri, 0);
            m.RecalculateBounds();
            m.UploadMeshData(true);
            return m;
        }
    }
}
