using UnityEngine;
using UnityEngine.UI;

namespace TumbangPreso.UI.Hub
{
    /// <summary>
    /// Where each hub screen IS. Every screen used to be built on the same call, a maroon fill with
    /// the same scattered chalk marks, so HERO, LOADOUT, TASKS, SKILL TREE and JOIN were one
    /// template with different stickers on it.
    ///
    /// ⚠️⚠️ THE OWNER, 2026-09-24: "refine the look of all other ui ... give them all more
    /// personality", then "each screen shoudl feel like their own screen". `CLAUDE.md` § 6.5 already
    /// named the cause a month earlier: "everything feels repetitive bcz i think u use the same code
    /// to generate them all", and "make sure all ui isnt generated in the same way but follows a
    /// central theme". So the central theme is ONE STREET and every screen is a different place or
    /// object on it, the way Slay the Spire 2 makes each menu a physical thing (a parchment map,
    /// carved boards, painted cards) and Splatoon makes each one a sign in its city:
    ///
    ///   SKILL TREE        chalk on the asphalt, drawn the way kids draw the court
    ///   GAMEMODE          posters pasted on a corrugated yero wall
    ///   JOIN              a hollow-block wall
    ///   TASKS             the listahan: a cardboard sign taped to a chipped painted wall
    ///   LOADOUT           the sari-sari store: plank wall, striped awning, shelves
    ///   CHARACTER SELECT  the court at night: one warm light on the chalk circle
    ///   HERO              a collector's poster: rays behind the figure
    ///   MATCH FOUND       the fiesta arriving: a burst, a slam, the bunting dropping in
    ///
    /// ⚠️ WHAT STAYS COMMON IS WHAT MAKES IT ONE GAME: the ink outline, the six logo colours, the
    /// pressable sticker, and the maroon evening family the 2026-09-23 pass chose so a player who
    /// leaves the HOME sunset is still in the same evening. Every ground below is derived from
    /// `HubStyle` with the arithmetic at the definition, and none has more blue than red (§ 6.4).
    ///
    /// ⚠️ ALL OF IT IS MESH, LIKE `HubShape` AND `HubPattern`: crisp at every canvas scale, no atlas,
    /// and nothing a player reads is drawn on it. Every texture here is under about 1.5 : 1
    /// against its own ground (`Front_End_Design.md` § 1.3: decoration is free where nothing has
    /// to be read). Motion is slow, and REDUCED UI MOTION holds every piece still.
    /// </summary>
    public static class HubScenery
    {
        /// <summary>Warm asphalt: <see cref="HubStyle.Night"/> taken 22 per cent toward
        /// <see cref="HubStyle.Maroon"/>, so the road is the same evening as every wall.</summary>
        public static readonly Color Asphalt = Color.Lerp(HubStyle.Night, HubStyle.Maroon, 0.22f);

        /// <summary>Cardboard: <see cref="HubStyle.Honey"/> taken 30 per cent to
        /// <see cref="HubStyle.Night"/>, about (189, 154, 115). Ink on it measures near 7 : 1.</summary>
        public static readonly Color Kraft = Color.Lerp(HubStyle.Honey, HubStyle.Night, 0.30f);

        /// <summary>Store planks: <see cref="HubStyle.Maroon"/> taken 35 per cent to
        /// <see cref="HubStyle.Night"/>, a stained wood rather than a new brown.</summary>
        public static readonly Color Plank = Color.Lerp(HubStyle.Maroon, HubStyle.Night, 0.35f);

        /// <summary>Chalk: the paper ramp's lightest tint, slightly transparent so it sits IN the road.</summary>
        public static Color Chalk => new Color(HubStyle.Paper.r, HubStyle.Paper.g, HubStyle.Paper.b, 0.82f);

        // ------------------------------------------------------------------ grounds

        private static RectTransform Fill(RectTransform root, Color colour)
        {
            var fill = HubKit.Stretch(HubKit.Rect(root, "Ground")).gameObject.AddComponent<Image>();
            fill.color = colour;
            fill.raycastTarget = false;
            return fill.rectTransform;
        }

        private static HubSurface Surface(RectTransform root, HubSurface.Kind kind, int seed)
        {
            var surface = HubKit.Stretch(HubKit.Rect(root, "Surface")).gameObject.AddComponent<HubSurface>();
            surface.Texture = kind;
            surface.Seed = seed;
            surface.raycastTarget = false;
            return surface;
        }

        /// <summary>The road: warm asphalt with its aggregate, and a court someone chalked long ago.</summary>
        public static void AsphaltGround(RectTransform root, int seed)
        {
            Fill(root, Asphalt);
            Surface(root, HubSurface.Kind.Aggregate, seed);
        }

        /// <summary>Corrugated yero sheets, painted maroon, streaked where the rain runs.</summary>
        public static void YeroGround(RectTransform root, int seed)
        {
            Fill(root, HubStyle.Maroon);
            Surface(root, HubSurface.Kind.Corrugated, seed);
        }

        /// <summary>A hollow-block wall, blocks and mortar, painted over once.</summary>
        public static void BlockGround(RectTransform root, int seed)
        {
            Fill(root, HubStyle.Maroon);
            Surface(root, HubSurface.Kind.Blocks, seed);
        }

        /// <summary>A painted plaster wall with the paint chipping off it and dirt along its foot.</summary>
        public static void PaintedGround(RectTransform root, int seed)
        {
            Fill(root, HubStyle.Maroon);
            Surface(root, HubSurface.Kind.Chipped, seed);
        }

        /// <summary>The sari-sari store: a stained plank front with the striped awning over it.</summary>
        public static void StoreGround(RectTransform root, int seed, float awningHeight)
        {
            Fill(root, Plank);
            Surface(root, HubSurface.Kind.Planks, seed);
            var awning = HubKit.Rect(root, "Awning").gameObject.AddComponent<HubAwning>();
            awning.raycastTarget = false;
            var rect = awning.rectTransform;
            rect.anchorMin = new Vector2(0, 1); rect.anchorMax = Vector2.one; rect.pivot = new Vector2(0.5f, 1);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(0, awningHeight);
        }

        /// <summary>A printed poster's ground: the evening maroon with a halftone screen over it.</summary>
        public static void PosterGround(RectTransform root)
        {
            Fill(root, HubStyle.Maroon);
            Surface(root, HubSurface.Kind.Halftone, 1);
        }

        /// <summary>The court at night: dark road, one warm pool of light. ⚠️ No bunting: on CHARACTER
        /// SELECT it hung straight through the hero's name, which is the one thing that screen says.</summary>
        public static void NightCourtGround(RectTransform root, int seed, Vector2 lightAnchor, Vector2 lightSize)
        {
            Fill(root, HubStyle.Night);
            Surface(root, HubSurface.Kind.Aggregate, seed).color = new Color(1, 1, 1, 0.7f);
            var glow = HubKit.Rect(root, "Light").gameObject.AddComponent<HubGlow>();
            glow.color = new Color(HubStyle.Persimmon.r, HubStyle.Persimmon.g, HubStyle.Persimmon.b, 0.30f);
            glow.raycastTarget = false;
            glow.rectTransform.anchorMin = glow.rectTransform.anchorMax = lightAnchor;
            glow.rectTransform.sizeDelta = lightSize;
        }

        // ------------------------------------------------------------------ props

        /// <summary>Fiesta bunting strung across the top of <paramref name="root"/>. It moves on
        /// the title street's own breeze (`OwnerMenuWind`), so the hub and the title are one air.</summary>
        public static HubBunting Bunting(RectTransform root, int seed, float drop, float delay = 0)
        {
            var bunting = HubKit.Rect(root, "Bunting").gameObject.AddComponent<HubBunting>();
            bunting.Seed = seed;
            bunting.raycastTarget = false;
            var rect = bunting.rectTransform;
            rect.anchorMin = new Vector2(0, 1); rect.anchorMax = Vector2.one; rect.pivot = new Vector2(0.5f, 1);
            rect.anchoredPosition = new Vector2(0, -drop);
            rect.sizeDelta = new Vector2(0, 170);
            bunting.Arrive(delay);
            return bunting;
        }

        /// <summary>Rays from a centre, in two alternating tints, turning very slowly.</summary>
        public static HubBurst Burst(Transform parent, string name, Color ray, float degreesPerSecond, int rays = 18)
        {
            var burst = HubKit.Rect(parent, name).gameObject.AddComponent<HubBurst>();
            burst.color = ray;
            burst.Rays = rays;
            burst.Spin = degreesPerSecond;
            burst.raycastTarget = false;
            return burst;
        }

        /// <summary>A strip of masking tape, centred at <paramref name="at"/> on <paramref name="parent"/>'s
        /// top edge by default, tilted <paramref name="degrees"/>.</summary>
        public static RectTransform Tape(Transform parent, string name, Vector2 anchor, Vector2 at, float degrees, float length = 150)
        {
            var tape = HubKit.Rect(parent, name).gameObject.AddComponent<HubTape>();
            tape.raycastTarget = false;
            var rect = tape.rectTransform;
            rect.anchorMin = rect.anchorMax = anchor;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = at;
            rect.sizeDelta = new Vector2(length, 46);
            rect.localRotation = Quaternion.Euler(0, 0, degrees);
            return rect;
        }

        /// <summary>A hand-chalked ring, drawn in as the screen opens.</summary>
        public static HubChalk ChalkRing(Transform parent, string name, float delay = 0.15f)
        {
            var chalk = HubKit.Rect(parent, name).gameObject.AddComponent<HubChalk>();
            chalk.color = Chalk;
            chalk.raycastTarget = false;
            chalk.DrawIn(delay);
            return chalk;
        }

        /// <summary>A clipboard's clip: a golden metal plate with its lever and a rivet, at the top.</summary>
        public static RectTransform Clip(RectTransform board)
        {
            var clip = HubKit.Rect(board, "Clip");
            HubKit.Place(clip, HubKit.Top, new Vector2(0, 34), new Vector2(260, 86));
            var plate = HubKit.Shape(clip, "Plate", HubStyle.Golden, false, 77, 5, 14);
            HubKit.Place(plate.rectTransform, HubKit.Bottom, Vector2.zero, new Vector2(260, 64));
            plate.ShadowOffset = new Vector2(5, -6);
            plate.ShadowColor = new Color(HubStyle.Ink.r, HubStyle.Ink.g, HubStyle.Ink.b, 0.6f);
            var lever = HubKit.Shape(clip, "Lever", HubStyle.Lit(HubStyle.Golden, 0.6f), false, 78, 5, 30);
            HubKit.Place(lever.rectTransform, HubKit.Top, Vector2.zero, new Vector2(150, 44));
            var rivet = HubKit.Glyph(clip, "Rivet", HubGlyph.Mark.Dot, HubStyle.Ink, 0.2f);
            HubKit.Place(rivet.rectTransform, HubKit.Bottom, new Vector2(0, 18), new Vector2(30, 30));
            return clip;
        }

        /// <summary>A cardboard sign: kraft board with its fibres, taped to the wall at the corners.</summary>
        public static RectTransform Cardboard(Transform parent, string name, int seed)
        {
            var board = HubKit.Shape(parent, name, Kraft, false, seed, 3, 6);
            board.ShadowOffset = new Vector2(10, -12);
            board.ShadowColor = new Color(HubStyle.Ink.r, HubStyle.Ink.g, HubStyle.Ink.b, 0.55f);
            var fibres = HubKit.Stretch(HubKit.Rect(board.transform, "Fibres"), 8).gameObject.AddComponent<HubSurface>();
            fibres.Texture = HubSurface.Kind.Fibres;
            fibres.Seed = seed;
            fibres.raycastTarget = false;
            var rect = board.rectTransform;
            Tape(rect, "TapeTopLeft", new Vector2(0, 1), new Vector2(34, -6), 38);
            Tape(rect, "TapeTopRight", new Vector2(1, 1), new Vector2(-34, -6), -35);
            Tape(rect, "TapeBottomLeft", new Vector2(0, 0), new Vector2(30, 8), -40);
            Tape(rect, "TapeBottomRight", new Vector2(1, 0), new Vector2(-30, 8), 42);
            return rect;
        }

        // ------------------------------------------------------------------ titles

        /// <summary>
        /// A wall's title, sprayed rather than printed: the logo on the title screen is graffiti on
        /// a wall, so a title painted on a hub wall is the same act. An ink outline, a soft overspray
        /// in persimmon, and paint running down from a few letters.
        /// </summary>
        public static void SprayTitle(Text title)
        {
            if (title == null) return;
            foreach (var old in title.GetComponents<Shadow>()) Object.Destroy(old);
            title.color = HubStyle.Honey;
            var outline = title.gameObject.AddComponent<Outline>();
            outline.effectColor = HubStyle.Ink; outline.effectDistance = new Vector2(3, -3);
            var spray = title.gameObject.AddComponent<Outline>();
            spray.effectColor = new Color(HubStyle.Persimmon.r, HubStyle.Persimmon.g, HubStyle.Persimmon.b, 0.28f);
            spray.effectDistance = new Vector2(7, -7);
            var drips = HubKit.Stretch(HubKit.Rect(title.transform, "Drips")).gameObject.AddComponent<HubDrips>();
            drips.Source = title;
            // ⚠️ A SUBTITLE SITS 21 UNITS UNDER THE LETTERS (`HubChrome.Title`), and the first capture
            // ran paint straight through "EARN TANSAN" and "SELECT". Drips stop short of it.
            if (title.transform.parent != null && title.transform.parent.Find("SubTitle") != null) drips.MaxLength = 16;
            drips.color = HubStyle.Honey;
            drips.raycastTarget = false;
            drips.transform.SetAsFirstSibling();
        }

        /// <summary>A road's title, chalked: paper-white, a rough second pass, a scrawled underline.</summary>
        public static void ChalkTitle(Text title)
        {
            if (title == null) return;
            foreach (var old in title.GetComponents<Shadow>()) Object.Destroy(old);
            title.color = Chalk;
            var rough = title.gameObject.AddComponent<Shadow>();
            rough.effectColor = new Color(HubStyle.Paper.r, HubStyle.Paper.g, HubStyle.Paper.b, 0.22f);
            rough.effectDistance = new Vector2(3, 2);
            var line = HubKit.Rect(title.transform, "Underline").gameObject.AddComponent<HubChalk>();
            line.Shape = HubChalk.Stroke.Underline;
            line.Source = title;
            line.color = Chalk;
            line.raycastTarget = false;
            HubKit.Stretch(line.rectTransform);
            line.DrawIn(0.05f);
        }
    }

    /// <summary>The texture of a ground, drawn as a mesh from a seed so it never tiles visibly.</summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class HubSurface : MaskableGraphic
    {
        public enum Kind { Aggregate, Corrugated, Blocks, Chipped, Planks, Fibres, Halftone }
        public Kind Texture;
        public int Seed = 1;

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            var r = GetPixelAdjustedRect();
            var rng = new System.Random(Seed * 7919 + (int)Texture);
            float tint = color.a;
            switch (Texture)
            {
                case Kind.Aggregate: Aggregate(vh, r, rng, tint); break;
                case Kind.Corrugated: Corrugated(vh, r, rng, tint); break;
                case Kind.Blocks: Blocks(vh, r, rng, tint); break;
                case Kind.Chipped: Chipped(vh, r, rng, tint); break;
                case Kind.Planks: Planks(vh, r, rng, tint); break;
                case Kind.Fibres: Fibres(vh, r, rng, tint); break;
                case Kind.Halftone: Halftone(vh, r, tint); break;
            }
        }

        private static Color A(Color c, float a) => new Color(c.r, c.g, c.b, a);
        private static float R(System.Random rng, float a, float b) => a + (float)rng.NextDouble() * (b - a);

        // Asphalt: stones in two values and a few worn chalk marks of an old court.
        private static void Aggregate(VertexHelper vh, Rect r, System.Random rng, float tint)
        {
            int count = Mathf.RoundToInt(r.width * r.height / 2600f);
            for (int i = 0; i < count; i++)
            {
                var at = new Vector2(R(rng, r.xMin, r.xMax), R(rng, r.yMin, r.yMax));
                float size = R(rng, 1.6f, 4.8f);
                bool light = rng.NextDouble() < 0.55;
                var c = light ? A(HubStyle.Honey, R(rng, 0.04f, 0.10f) * tint) : A(HubStyle.Ink, R(rng, 0.18f, 0.35f) * tint);
                HubMesh.Blob(vh, at, size, size * R(rng, 0.6f, 1.0f), R(rng, 0, 6.28f), c, 5);
            }
            // Tar seams: long soft darker bands where the road was patched.
            for (int i = 0; i < 3; i++)
            {
                float y = R(rng, r.yMin, r.yMax), h = R(rng, 60, 140);
                HubMesh.Quad(vh, new Vector2(r.xMin, y), new Vector2(r.xMax, y + R(rng, -60, 60)), h, A(HubStyle.Ink, 0.10f * tint));
            }
            // A faded court, the chalk of games long finished.
            var chalk = A(HubStyle.Paper, 0.05f * tint);
            var centre = new Vector2(R(rng, r.xMin + r.width * 0.2f, r.xMax - r.width * 0.2f), R(rng, r.yMin, r.center.y));
            HubMesh.Ring(vh, centre, 150, 110, 0.1f, 5, chalk, 40);
            HubMesh.Quad(vh, new Vector2(r.xMin + 80, centre.y + 420), new Vector2(r.xMax - 80, centre.y + 400), 6, chalk);
        }

        // Yero: vertical ridges every 64 units, lit on one flank and shaded on the other, with rust
        // runs hanging from a few nail holes.
        private static void Corrugated(VertexHelper vh, Rect r, System.Random rng, float tint)
        {
            const float pitch = 64;
            var lit = A(HubStyle.Lit(HubStyle.Maroon, 0.6f), 0.35f * tint);
            var deep = A(HubStyle.Deep(HubStyle.Maroon, 0.8f), 0.45f * tint);
            var clear = A(HubStyle.Maroon, 0);
            for (float x = r.xMin; x < r.xMax; x += pitch)
            {
                HubMesh.Gradient(vh, new Rect(x, r.yMin, pitch * 0.5f, r.height), clear, lit);
                HubMesh.Gradient(vh, new Rect(x + pitch * 0.5f, r.yMin, pitch * 0.5f, r.height), lit, deep);
            }
            // Sheet overlaps: a hard seam every sheet width.
            for (float x = r.xMin + R(rng, 200, 500); x < r.xMax; x += R(rng, 560, 760))
                HubMesh.Quad(vh, new Vector2(x, r.yMin), new Vector2(x, r.yMax), 5, A(HubStyle.Ink, 0.35f * tint));
            // Rust runs.
            int runs = Mathf.RoundToInt(r.width / 150f);
            for (int i = 0; i < runs; i++)
            {
                float x = Mathf.Round(R(rng, r.xMin, r.xMax) / pitch) * pitch + pitch * 0.5f;
                float top = R(rng, r.yMin + r.height * 0.3f, r.yMax);
                float length = R(rng, 90, 360);
                HubMesh.GradientV(vh, new Rect(x - 5, top - length, 10, length),
                                  A(HubStyle.Persimmon, 0), A(HubStyle.Persimmon, R(rng, 0.08f, 0.16f) * tint));
                HubMesh.Blob(vh, new Vector2(x, top), 4, 4, 0, A(HubStyle.Ink, 0.45f * tint), 6);
            }
        }

        // Hollow blocks: 40 x 20 cm, drawn at 200 x 100 units, each a touch different, in
        // running bond, with mortar between.
        private static void Blocks(VertexHelper vh, Rect r, System.Random rng, float tint)
        {
            const float w = 200, h = 100, mortar = 7;
            var line = A(HubStyle.Deep(HubStyle.Maroon, 0.9f), 0.55f * tint);
            int row = 0;
            for (float y = r.yMin; y < r.yMax; y += h, row++)
            {
                float offset = row % 2 == 0 ? 0 : -w * 0.5f;
                for (float x = r.xMin + offset; x < r.xMax; x += w)
                {
                    float v = R(rng, -1, 1);
                    var face = v > 0 ? A(HubStyle.Lit(HubStyle.Maroon, 0.5f), v * 0.16f * tint)
                                     : A(HubStyle.Deep(HubStyle.Maroon, 0.6f), -v * 0.20f * tint);
                    HubMesh.Rect(vh, new Rect(x + mortar * 0.5f, y + mortar * 0.5f, w - mortar, h - mortar), face);
                    // The top lip of each block catches the light.
                    HubMesh.Rect(vh, new Rect(x + mortar * 0.5f, y + h - mortar * 0.5f - 5, w - mortar, 5),
                                 A(HubStyle.Lit(HubStyle.Maroon, 0.8f), 0.14f * tint));
                    HubMesh.Rect(vh, new Rect(x - mortar * 0.5f, y, mortar, h), line);
                }
                HubMesh.Rect(vh, new Rect(r.xMin, y - mortar * 0.5f, r.width, mortar), line);
            }
        }

        // Painted plaster: chips where the maroon has flaked to the plaster underneath, a few
        // hairline cracks, and the dirt a street throws up along the foot of a wall.
        private static void Chipped(VertexHelper vh, Rect r, System.Random rng, float tint)
        {
            var plaster = A(Color.Lerp(HubStyle.Maroon, HubStyle.Honey, 0.32f), 0.45f * tint);
            var edge = A(HubStyle.Deep(HubStyle.Maroon, 0.8f), 0.5f * tint);
            int chips = Mathf.RoundToInt(r.width * r.height / 90000f);
            for (int i = 0; i < chips; i++)
            {
                var at = new Vector2(R(rng, r.xMin, r.xMax), R(rng, r.yMin, r.yMax));
                float size = R(rng, 10, 46);
                float angle = R(rng, 0, 6.28f);
                HubMesh.Blob(vh, at + new Vector2(2, -2), size * 1.08f, size * 0.62f, angle, edge, 9, rng);
                HubMesh.Blob(vh, at, size, size * 0.56f, angle, plaster, 9, rng);
            }
            int cracks = Mathf.RoundToInt(r.width / 420f);
            for (int i = 0; i < cracks; i++)
            {
                var a = new Vector2(R(rng, r.xMin, r.xMax), R(rng, r.yMin, r.yMax));
                for (int k = 0; k < 5; k++)
                {
                    var b = a + new Vector2(R(rng, -60, 60), R(rng, -70, -20));
                    HubMesh.Quad(vh, a, b, 2.2f, A(HubStyle.Ink, 0.30f * tint));
                    a = b;
                }
            }
            HubMesh.GradientV(vh, new Rect(r.xMin, r.yMin, r.width, r.height * 0.16f),
                              A(HubStyle.Ink, 0.30f * tint), A(HubStyle.Ink, 0));
        }

        // The store front: vertical planks, alternate ones a shade lighter, grain lines and nails.
        private static void Planks(VertexHelper vh, Rect r, System.Random rng, float tint)
        {
            float x = r.xMin;
            int i = 0;
            while (x < r.xMax)
            {
                float w = R(rng, 150, 210);
                if (i++ % 2 == 1)
                    HubMesh.Rect(vh, new Rect(x, r.yMin, w, r.height), A(HubStyle.Lit(ScenePlank, 0.5f), 0.22f * tint));
                for (int g = 0; g < 4; g++)
                {
                    float gx = x + R(rng, 14, w - 14);
                    HubMesh.Quad(vh, new Vector2(gx, r.yMin), new Vector2(gx + R(rng, -18, 18), r.yMax), R(rng, 1.4f, 2.6f),
                                 A(HubStyle.Ink, R(rng, 0.10f, 0.20f) * tint));
                }
                HubMesh.Quad(vh, new Vector2(x, r.yMin), new Vector2(x, r.yMax), 5, A(HubStyle.Ink, 0.55f * tint));
                for (float ny = r.yMin + 120; ny < r.yMax; ny += 380)
                {
                    HubMesh.Blob(vh, new Vector2(x + 16, ny), 4.5f, 4.5f, 0, A(HubStyle.Ink, 0.5f * tint), 6);
                    HubMesh.Blob(vh, new Vector2(x + w - 16, ny + 30), 4.5f, 4.5f, 0, A(HubStyle.Ink, 0.5f * tint), 6);
                }
                x += w;
            }
        }

        private static Color ScenePlank => HubScenery.Plank;

        // Print: a comic-poster halftone, dots swelling toward the lower right, on a 34-unit
        // screen at 30 degrees, the way a cheap printed poster is.
        private static void Halftone(VertexHelper vh, Rect r, float tint)
        {
            const float pitch = 34;
            var dot = A(HubStyle.Deep(HubStyle.Maroon, 0.9f), 0.55f * tint);
            float cs = Mathf.Cos(0.52f), sn = Mathf.Sin(0.52f);
            float reach = r.size.magnitude;
            for (float u = -reach; u < reach; u += pitch)
            for (float v = -reach; v < reach; v += pitch)
            {
                var p = r.center + new Vector2(u * cs - v * sn, u * sn + v * cs);
                if (!r.Contains(p)) continue;
                float f = Mathf.Clamp01(((p.x - r.xMin) / r.width * 0.6f + (r.yMax - p.y) / r.height * 0.4f - 0.35f) / 0.65f);
                float radius = pitch * 0.42f * f;
                if (radius < 1.2f) continue;
                HubMesh.Blob(vh, p, radius, radius, 0, dot, 8);
            }
        }

        // Cardboard: fine fibres and the ghost of a printed carton flap.
        private static void Fibres(VertexHelper vh, Rect r, System.Random rng, float tint)
        {
            int count = Mathf.RoundToInt(r.width * r.height / 1800f);
            for (int i = 0; i < count; i++)
            {
                var a = new Vector2(R(rng, r.xMin, r.xMax), R(rng, r.yMin, r.yMax));
                var b = a + new Vector2(R(rng, 8, 26), R(rng, -3, 3));
                bool light = rng.NextDouble() < 0.5;
                HubMesh.Quad(vh, a, b, 1.2f, light ? A(HubStyle.Honey, 0.16f * tint) : A(HubStyle.Ink, 0.08f * tint));
            }
            float fold = R(rng, r.yMin + r.height * 0.2f, r.yMax - r.height * 0.2f);
            HubMesh.Quad(vh, new Vector2(r.xMin, fold), new Vector2(r.xMax, fold + 4), 3, A(HubStyle.Ink, 0.10f * tint));
        }
    }

    /// <summary>Small mesh helpers shared by the scenery graphics.</summary>
    internal static class HubMesh
    {
        public static void Rect(VertexHelper vh, Rect r, Color32 c)
        {
            int i = vh.currentVertCount;
            vh.AddVert(new Vector2(r.xMin, r.yMin), c, Vector2.zero); vh.AddVert(new Vector2(r.xMin, r.yMax), c, Vector2.zero);
            vh.AddVert(new Vector2(r.xMax, r.yMax), c, Vector2.zero); vh.AddVert(new Vector2(r.xMax, r.yMin), c, Vector2.zero);
            vh.AddTriangle(i, i + 1, i + 2); vh.AddTriangle(i, i + 2, i + 3);
        }

        /// <summary>A rect shading left to right.</summary>
        public static void Gradient(VertexHelper vh, Rect r, Color32 left, Color32 right)
        {
            int i = vh.currentVertCount;
            vh.AddVert(new Vector2(r.xMin, r.yMin), left, Vector2.zero); vh.AddVert(new Vector2(r.xMin, r.yMax), left, Vector2.zero);
            vh.AddVert(new Vector2(r.xMax, r.yMax), right, Vector2.zero); vh.AddVert(new Vector2(r.xMax, r.yMin), right, Vector2.zero);
            vh.AddTriangle(i, i + 1, i + 2); vh.AddTriangle(i, i + 2, i + 3);
        }

        /// <summary>A rect shading bottom to top.</summary>
        public static void GradientV(VertexHelper vh, Rect r, Color32 bottom, Color32 top)
        {
            int i = vh.currentVertCount;
            vh.AddVert(new Vector2(r.xMin, r.yMin), bottom, Vector2.zero); vh.AddVert(new Vector2(r.xMin, r.yMax), top, Vector2.zero);
            vh.AddVert(new Vector2(r.xMax, r.yMax), top, Vector2.zero); vh.AddVert(new Vector2(r.xMax, r.yMin), bottom, Vector2.zero);
            vh.AddTriangle(i, i + 1, i + 2); vh.AddTriangle(i, i + 2, i + 3);
        }

        /// <summary>A straight stroke of <paramref name="width"/> from a to b.</summary>
        public static void Quad(VertexHelper vh, Vector2 a, Vector2 b, float width, Color32 c)
        {
            var d = b - a;
            if (d.sqrMagnitude < 0.01f) return;
            var n = new Vector2(-d.y, d.x).normalized * width * 0.5f;
            int i = vh.currentVertCount;
            vh.AddVert(a + n, c, Vector2.zero); vh.AddVert(b + n, c, Vector2.zero);
            vh.AddVert(b - n, c, Vector2.zero); vh.AddVert(a - n, c, Vector2.zero);
            vh.AddTriangle(i, i + 1, i + 2); vh.AddTriangle(i, i + 2, i + 3);
        }

        /// <summary>A filled irregular ellipse. With <paramref name="rng"/> its rim wobbles.</summary>
        public static void Blob(VertexHelper vh, Vector2 at, float rx, float ry, float angle, Color32 c, int sides,
                                System.Random rng = null)
        {
            int centre = vh.currentVertCount;
            vh.AddVert(at, c, Vector2.zero);
            float cs = Mathf.Cos(angle), sn = Mathf.Sin(angle);
            for (int s = 0; s < sides; s++)
            {
                float a = s * Mathf.PI * 2 / sides;
                float wobble = rng != null ? 0.7f + (float)rng.NextDouble() * 0.5f : 1;
                var p = new Vector2(Mathf.Cos(a) * rx * wobble, Mathf.Sin(a) * ry * wobble);
                vh.AddVert(at + new Vector2(p.x * cs - p.y * sn, p.x * sn + p.y * cs), c, Vector2.zero);
            }
            for (int s = 0; s < sides; s++) vh.AddTriangle(centre, centre + 1 + s, centre + 1 + (s + 1) % sides);
        }

        /// <summary>An elliptical ring of stroke <paramref name="width"/>, optionally only
        /// <paramref name="portion"/> of it, starting at <paramref name="start"/> radians.</summary>
        public static void Ring(VertexHelper vh, Vector2 at, float rx, float ry, float start, float width, Color32 c,
                                int steps, float portion = 1)
        {
            int count = Mathf.Max(1, Mathf.RoundToInt(steps * portion));
            for (int s = 0; s < count; s++)
            {
                float a = start + s * Mathf.PI * 2 / steps, b = start + (s + 1) * Mathf.PI * 2 / steps;
                Quad(vh, at + new Vector2(Mathf.Cos(a) * rx, Mathf.Sin(a) * ry), at + new Vector2(Mathf.Cos(b) * rx, Mathf.Sin(b) * ry), width, c);
            }
        }
    }

    /// <summary>The store's awning: stripes, a scalloped hem, and the shadow it throws on the planks.</summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class HubAwning : MaskableGraphic
    {
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            var r = GetPixelAdjustedRect();
            const float stripe = 110, scallop = 34;
            float body = r.height - scallop;
            var shade = new Color(HubStyle.Ink.r, HubStyle.Ink.g, HubStyle.Ink.b, 0.45f);
            var clear = new Color(HubStyle.Ink.r, HubStyle.Ink.g, HubStyle.Ink.b, 0);
            // Its shadow on the wall first, falling below the hem.
            HubMesh.GradientV(vh, new Rect(r.xMin, r.yMin - 70, r.width, 70 + scallop), clear, shade);
            int i = 0;
            for (float x = r.xMin; x < r.xMax; x += stripe, i++)
            {
                var fill = i % 2 == 0 ? HubStyle.RimRed : HubStyle.Honey;
                var lit = HubStyle.Lit(fill, 0.4f);
                HubMesh.GradientV(vh, new Rect(x, r.yMin + scallop, stripe, body), fill, lit);
                // Each stripe ends in a half-round tongue: the scalloped hem every awning has.
                HubMesh.Blob(vh, new Vector2(x + stripe * 0.5f, r.yMin + scallop), stripe * 0.5f, scallop, 0, fill, 16);
            }
            // The ink line along the top and the hem's outline, so it reads as one sticker.
            HubMesh.Rect(vh, new Rect(r.xMin, r.yMin + scallop - 3, r.width, 5), new Color(HubStyle.Ink.r, HubStyle.Ink.g, HubStyle.Ink.b, 0.35f));
            HubMesh.Rect(vh, new Rect(r.xMin, r.yMax - 8, r.width, 8), HubStyle.Ink);
        }
    }

    /// <summary>
    /// A store shelf under every row of a grid: a plank with its lit front edge and the shadow it
    /// casts, and a bracket at each end. It reads the rows from the grid's own height, so a grid
    /// that grows or scrolls keeps a shelf under every row without being told.
    /// </summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class HubShelves : MaskableGraphic
    {
        public float Cell = 250, Gap = 30, Top = 16;

        protected override void OnRectTransformDimensionsChange()
        {
            base.OnRectTransformDimensionsChange();
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            var r = GetPixelAdjustedRect();
            int rows = Mathf.RoundToInt((r.height - Top * 2 + Gap) / (Cell + Gap));
            var wood = HubStyle.Lit(HubScenery.Plank, 0.9f);
            var edge = HubStyle.Lit(HubScenery.Plank, 1.6f);
            var shade = new Color(HubStyle.Ink.r, HubStyle.Ink.g, HubStyle.Ink.b, 0.5f);
            var clear = new Color(HubStyle.Ink.r, HubStyle.Ink.g, HubStyle.Ink.b, 0);
            for (int row = 0; row < rows; row++)
            {
                // The tiles sit ON the plank: its top is a few units above the bottom of the row.
                float y = r.yMax - Top - Cell - row * (Cell + Gap) + 10;
                HubMesh.GradientV(vh, new Rect(r.xMin, y - 58, r.width, 30), clear, shade);
                HubMesh.Rect(vh, new Rect(r.xMin, y - 28, r.width, 28), HubStyle.Ink);
                HubMesh.Rect(vh, new Rect(r.xMin, y - 24, r.width, 20), wood);
                HubMesh.Rect(vh, new Rect(r.xMin, y - 10, r.width, 6), edge);
                foreach (float x in new[] { r.xMin + 60, r.xMax - 60 })
                {
                    int i = vh.currentVertCount;
                    Color32 c = HubStyle.Ink;
                    vh.AddVert(new Vector2(x - 14, y - 28), c, Vector2.zero);
                    vh.AddVert(new Vector2(x + 14, y - 28), c, Vector2.zero);
                    vh.AddVert(new Vector2(x, y - 70), c, Vector2.zero);
                    vh.AddTriangle(i, i + 1, i + 2);
                }
            }
        }
    }

    /// <summary>A soft pool of light: the colour at the centre, clear at the rim.</summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class HubGlow : MaskableGraphic
    {
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            var r = GetPixelAdjustedRect();
            Color32 inner = color, outer = new Color(color.r, color.g, color.b, 0);
            const int sides = 48;
            int centre = vh.currentVertCount;
            vh.AddVert(r.center, inner, Vector2.zero);
            for (int s = 0; s < sides; s++)
            {
                float a = s * Mathf.PI * 2 / sides;
                vh.AddVert(r.center + new Vector2(Mathf.Cos(a) * r.width * 0.5f, Mathf.Sin(a) * r.height * 0.5f), outer, Vector2.zero);
            }
            for (int s = 0; s < sides; s++) vh.AddTriangle(centre, centre + 1 + s, centre + 1 + (s + 1) % sides);
        }
    }

    /// <summary>Rays from the centre. The whole graphic turns; nothing is rebuilt per frame.</summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class HubBurst : MaskableGraphic
    {
        public int Rays = 18;
        public float Spin = 3;
        /// <summary>How far the rays run from the centre. A burst is placed as a small rect at its
        /// centre and masked by its container, so its length cannot come from its own size.</summary>
        public float Reach = 2600;

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            var r = GetPixelAdjustedRect();
            float reach = Reach;
            Color32 c = color;
            for (int k = 0; k < Rays; k++)
            {
                float a = k * Mathf.PI * 2 / Rays, b = a + Mathf.PI / Rays;
                int i = vh.currentVertCount;
                vh.AddVert(r.center, c, Vector2.zero);
                vh.AddVert(r.center + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * reach, c, Vector2.zero);
                vh.AddVert(r.center + new Vector2(Mathf.Cos(b), Mathf.Sin(b)) * reach, c, Vector2.zero);
                vh.AddTriangle(i, i + 1, i + 2);
            }
        }

        private void Update()
        {
            if (HubStyle.ReducedMotion || Spin == 0) return;
            transform.localRotation = Quaternion.Euler(0, 0, Time.unscaledTime * Spin % 360);
        }
    }

    /// <summary>Masking tape: a translucent cream strip with torn ends.</summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class HubTape : MaskableGraphic
    {
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            var r = GetPixelAdjustedRect();
            Color32 c = new Color(HubStyle.Honey.r, HubStyle.Honey.g, HubStyle.Honey.b, 0.78f);
            // The torn ends are zigzags, so the strip is a strip of thin columns of varying height.
            const int teeth = 7;
            float tear = 7, step = r.height / teeth;
            int i = vh.currentVertCount;
            vh.AddVert(new Vector2(r.xMin + tear, r.yMin), c, Vector2.zero);
            vh.AddVert(new Vector2(r.xMin + tear, r.yMax), c, Vector2.zero);
            vh.AddVert(new Vector2(r.xMax - tear, r.yMax), c, Vector2.zero);
            vh.AddVert(new Vector2(r.xMax - tear, r.yMin), c, Vector2.zero);
            vh.AddTriangle(i, i + 1, i + 2); vh.AddTriangle(i, i + 2, i + 3);
            for (int k = 0; k < teeth; k++)
            {
                float y0 = r.yMin + k * step, y1 = y0 + step;
                float left = r.xMin + (k % 2 == 0 ? 0 : tear * 0.6f), right = r.xMax - (k % 2 == 0 ? tear * 0.5f : 0);
                HubMesh.Rect(vh, new Rect(left, y0, r.xMin + tear - left, step), c);
                HubMesh.Rect(vh, new Rect(r.xMax - tear, y0, right - (r.xMax - tear), step), c);
            }
            // A faint crease down its length.
            HubMesh.Rect(vh, new Rect(r.xMin + tear, r.center.y - 1, r.width - tear * 2, 2),
                         new Color(HubStyle.Ink.r, HubStyle.Ink.g, HubStyle.Ink.b, 0.06f));
        }
    }

    /// <summary>
    /// Paint running down from a sprayed title. Three or four drips under hashed letters, sized to
    /// the title's real width, so a longer word gets its drips under its own letters.
    /// </summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class HubDrips : MaskableGraphic
    {
        public Text Source;
        public float MaxLength = 48;

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            if (Source == null) return;
            var r = GetPixelAdjustedRect();
            float width = Mathf.Min(Source.preferredWidth, r.width);
            float left = Source.alignment == TextAnchor.MiddleCenter ? r.center.x - width * 0.5f : r.xMin;
            float baseline = r.center.y - Source.fontSize * 0.34f;
            var rng = new System.Random(Source.text.GetHashCode());
            Color32 c = color;
            int drips = 2 + Mathf.Min(3, Source.text.Length / 3);
            for (int k = 0; k < drips; k++)
            {
                float x = left + width * (0.1f + 0.8f * (float)rng.NextDouble());
                float length = Mathf.Min(MaxLength, 14 + (float)rng.NextDouble() * 34);
                float w = 5 + (float)rng.NextDouble() * 3;
                HubMesh.Rect(vh, new Rect(x - w * 0.5f, baseline - length, w, length), c);
                HubMesh.Blob(vh, new Vector2(x, baseline - length), w * 0.75f, w * 0.9f, 0, c, 10);
            }
        }
    }

    /// <summary>
    /// Chalk: a doubled, broken, slightly wandering stroke that draws itself in. A ring, an
    /// underline under a title, or any polyline a screen hands it.
    /// </summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class HubChalk : MaskableGraphic
    {
        public enum Stroke { Ring, Underline, Path }
        public Stroke Shape = Stroke.Ring;
        public Text Source;
        public Vector2[] Points;
        public float Width = 6;

        private float _start = -1, _progress = 1;
        private const float Duration = 0.55f;

        public void DrawIn(float delay)
        {
            if (HubStyle.ReducedMotion) { _progress = 1; enabled = true; SetVerticesDirty(); return; }
            _start = Time.unscaledTime + delay;
            _progress = 0;
            SetVerticesDirty();
        }

        private void Update()
        {
            if (_start < 0 || _progress >= 1) return;
            float t = Mathf.Clamp01((Time.unscaledTime - _start) / Duration);
            _progress = 1 - (1 - t) * (1 - t);
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            var r = GetPixelAdjustedRect();
            var path = new System.Collections.Generic.List<Vector2>();
            switch (Shape)
            {
                case Stroke.Ring:
                    for (int s = 0; s <= 40; s++)
                    {
                        // A hand does not close a circle: it overshoots by a tenth.
                        float a = 0.4f + s * Mathf.PI * 2.2f / 40;
                        float wobble = 1 + 0.03f * Mathf.Sin(s * 1.7f);
                        path.Add(r.center + new Vector2(Mathf.Cos(a) * r.width * 0.5f, Mathf.Sin(a) * r.height * 0.5f) * wobble);
                    }
                    break;
                case Stroke.Underline:
                {
                    float width = Source != null ? Mathf.Min(Source.preferredWidth, r.width) : r.width;
                    float y = r.center.y - (Source != null ? Source.fontSize * 0.48f : r.height * 0.4f);
                    for (int s = 0; s <= 12; s++)
                        path.Add(new Vector2(r.xMin + width * s / 12f + 6, y + Mathf.Sin(s * 0.9f) * 3 - s * 0.6f));
                    break;
                }
                case Stroke.Path:
                    if (Points != null) foreach (var p in Points) path.Add(p);
                    break;
            }
            if (path.Count < 2) return;

            float total = 0;
            for (int k = 1; k < path.Count; k++) total += Vector2.Distance(path[k - 1], path[k]);
            float drawn = total * _progress, walked = 0;
            var rng = new System.Random(path.Count * 31 + (int)Shape);
            for (int pass = 0; pass < 2; pass++)
            {
                Color32 c = pass == 0 ? color : new Color(color.r, color.g, color.b, color.a * 0.45f);
                var offset = pass == 0 ? Vector2.zero : new Vector2(2.5f, -2);
                walked = 0;
                for (int k = 1; k < path.Count && walked < drawn; k++)
                {
                    var a = path[k - 1] + offset;
                    var b = path[k] + offset;
                    float length = Vector2.Distance(a, b);
                    if (walked + length > drawn) b = a + (b - a) * ((drawn - walked) / length);
                    walked += length;
                    // Chalk skips on a rough road: now and then a short gap in the stroke.
                    if (rng.NextDouble() < 0.12) continue;
                    HubMesh.Quad(vh, a, b, Width * (pass == 0 ? 1 : 0.6f), c);
                }
            }
        }
    }

    /// <summary>
    /// Fiesta bunting: two strings of small triangular flags in the logo's colours, sagging between
    /// the edges of the screen and stirring on the title street's breeze.
    /// </summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class HubBunting : MaskableGraphic
    {
        public int Seed = 1;
        private float _arrive = -1;
        private float _nextDraw;

        private static readonly Color[] Flags =
            { HubStyle.Chartreuse, HubStyle.Persimmon, HubStyle.Golden, HubStyle.RimRed, HubStyle.Honey };

        /// <summary>Drop in from above after <paramref name="delay"/> seconds.</summary>
        public void Arrive(float delay)
        {
            _arrive = HubStyle.ReducedMotion ? -1 : Time.unscaledTime + delay;
            SetVerticesDirty();
        }

        private void Update()
        {
            if (HubStyle.ReducedMotion) return;
            // A breeze does not need sixty rebuilds a second: thirty reads as the same motion.
            if (Time.unscaledTime < _nextDraw) return;
            _nextDraw = Time.unscaledTime + 1 / 30f;
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            var r = GetPixelAdjustedRect();
            bool still = HubStyle.ReducedMotion;
            float t = OwnerMenuWind.Now;
            float gust = still ? 0 : OwnerMenuWind.Gust(t);
            float lift = 0;
            if (!still && _arrive >= 0)
            {
                float u = Mathf.Clamp01((Time.unscaledTime - _arrive) / 0.45f);
                // Falls in from above and bounces once on its strings.
                lift = (1 - u) * (1 - u) * 190 - Mathf.Sin(u * Mathf.PI) * 16 * (1 - u);
            }
            var rng = new System.Random(Seed);
            Color32 cord = new Color(HubStyle.Ink.r, HubStyle.Ink.g, HubStyle.Ink.b, 0.85f);
            for (int line = 0; line < 2; line++)
            {
                float top = r.yMax - 18 - line * 46 + lift;
                float sag = 60 + line * 24;
                const int segments = 32;
                Vector2 Point(float f) => new Vector2(Mathf.Lerp(r.xMin - 20, r.xMax + 20, f),
                                                      top - sag * 4 * f * (1 - f) + (still ? 0 : Mathf.Sin(t * 0.8f + line + f * 3) * (2 + 6 * gust)));
                for (int s = 0; s < segments; s++) HubMesh.Quad(vh, Point(s / (float)segments), Point((s + 1) / (float)segments), 3, cord);

                float spacing = 74 + line * 10;
                int count = Mathf.CeilToInt(r.width / spacing);
                for (int k = 0; k < count; k++)
                {
                    float f = (k + 0.5f + line * 0.5f) / count;
                    var at = Point(f);
                    var colour = Flags[(k + line * 2 + Seed) % Flags.Length];
                    // Each flag swings on its own phase; a gust pushes them all the same way.
                    float swing = still ? 0 : Mathf.Sin(t * (1.6f + (float)rng.NextDouble()) + k) * (0.05f + 0.18f * gust) - 0.12f * gust;
                    var tip = at + new Vector2(Mathf.Sin(swing) * 52, -Mathf.Cos(swing) * 52);
                    int i = vh.currentVertCount;
                    vh.AddVert(at + new Vector2(-21, 0), (Color32)colour, Vector2.zero);
                    vh.AddVert(at + new Vector2(21, 0), (Color32)colour, Vector2.zero);
                    vh.AddVert(tip, (Color32)HubStyle.Deep(colour, 0.35f), Vector2.zero);
                    vh.AddTriangle(i, i + 1, i + 2);
                    HubMesh.Quad(vh, at + new Vector2(-21, 0), tip, 2.2f, cord);
                    HubMesh.Quad(vh, at + new Vector2(21, 0), tip, 2.2f, cord);
                }
            }
        }
    }

    /// <summary>
    /// The slam: a stamp arriving far too big and hitting the screen, which jolts once. MATCH
    /// FOUND's one beat, and the only shake in the hub.
    /// </summary>
    public sealed class HubImpact : MonoBehaviour
    {
        public RectTransform Shake;
        private float _start;
        private Vector2 _home;

        public static void On(RectTransform stamp, RectTransform shake, float delay)
        {
            var impact = stamp.gameObject.AddComponent<HubImpact>();
            impact.Shake = shake;
            impact._start = Time.unscaledTime + delay;
            impact._home = shake != null ? shake.anchoredPosition : Vector2.zero;
            if (HubStyle.ReducedMotion) impact.enabled = false;
            else impact.Update();
        }

        private void Update()
        {
            float t = (Time.unscaledTime - _start) / 0.18f;
            if (t < 0) { transform.localScale = Vector3.one * 2.2f; SetAlpha(0); return; }
            SetAlpha(1);
            float ease = Mathf.Clamp01(t);
            transform.localScale = Vector3.one * Mathf.Lerp(2.2f, 1, ease * ease);
            float after = (Time.unscaledTime - _start - 0.18f) / 0.32f;
            if (Shake != null)
                Shake.anchoredPosition = _home + (after > 0 && after < 1
                    ? new Vector2(Mathf.Sin(after * 60) * 12, Mathf.Cos(after * 47) * 8) * (1 - after)
                    : Vector2.zero);
            if (after >= 1) { transform.localScale = Vector3.one; enabled = false; }
        }

        private void SetAlpha(float a) => HubKit.Ensure<CanvasGroup>(gameObject).alpha = a;
    }
}
