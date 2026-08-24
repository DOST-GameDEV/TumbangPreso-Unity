using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace TumbangPreso.EditorTools.MapKit
{
    /// <summary>
    /// Re-authors the PC Express fascia on `env_pc_express_store.obj` so it reads as the shop it
    /// is named after.
    ///
    /// ⚠️⚠️ THE SIGN WAS THE WRONG COLOUR AND CARRIED NO WORDMARK. It shipped as a plain GREEN
    /// lightbox with two blank white slabs on it and a green-white-red awning, which is a
    /// sari-sari palette, not this one. The real fascia is a RED lightbox with a white rim, a
    /// BLUE tile carrying "PC", and "EXPRESS" in white across the rest. From five metres away
    /// in-game the colour blocking is the whole recognition: red board, blue tile, white
    /// wordmark. That is what this builds.
    ///
    /// ⚠️ IT REWRITES THE `.obj` IN PLACE AND IS IDEMPOTENT. Every block it emits is keyed to a
    /// material name in <see cref="SignMaterials"/>, and the first thing it does is delete every
    /// face that uses one of those names. Running it twice produces the same file, so it can be
    /// re-run after a tweak instead of being a one-shot mutation nobody can repeat.
    ///
    /// ⚠️ THE OLD VERTICES ARE LEFT IN THE FILE ON PURPOSE. Face lines in this `.obj` are
    /// ABSOLUTE vertex indices, so deleting the 24 vertices the old sign used would silently
    /// renumber every face in the other nineteen material blocks. The importer drops vertices no
    /// face references, so they cost nothing at runtime, and leaving them is the only edit that
    /// cannot corrupt the rest of the mesh.
    ///
    ///     Unity.exe -batchmode -quit -nographics -projectPath . \
    ///               -executeMethod TumbangPreso.EditorTools.MapKit.PcExpressSignAuthor.Run
    /// </summary>
    public static class PcExpressSignAuthor
    {
        private const string ObjPath = "Assets/TumbangPreso/Art/models/env_pc_express_store.obj";
        private const string MtlPath = "Assets/TumbangPreso/Art/models/env_pc_express_store.mtl";

        /// <summary>Everything past this line in the `.obj` belongs to this tool and is rewritten
        /// wholesale. See the note in <see cref="RewriteObj"/>.</summary>
        private const string Sentinel = "# --- PC EXPRESS FASCIA, AUTHORED ---";

        /// <summary>
        /// Every material this tool owns, including the two it replaced. ⚠️ THE DEAD NAMES STAY
        /// IN THE LIST. They are what makes a re-run on an already-converted file a no-op rather
        /// than a second sign stacked on the first.
        /// </summary>
        private static readonly string[] SignMaterials =
        {
            "pcex_sign_green", "pcex_sign_red", "pcex_sign_white",  // the shipped set
            "pcex_sign_rim", "pcex_sign_field", "pcex_sign_tile",
            "pcex_sign_text", "pcex_sign_edge",
        };

        // ------------------------------------------------------------------
        // The fascia, in the model's own local space. The shop front is the -Z face: the glass
        // sits at z = -2.75, the shutter at -3.15..-2.81 and the awning reaches -4.10, all
        // measured out of the shipped file.
        // ------------------------------------------------------------------

        private const float BoardMinX = -2.15f, BoardMaxX = 2.15f;
        private const float BoardMinY = 3.15f, BoardMaxY = 3.95f;

        /// <summary>Each layer stands 12 mm proud of the one behind it, which is enough for the
        /// rim, the tile and the letters to catch the sun separately without z-fighting.</summary>
        private const float LayerStep = 0.012f;

        private const float RimFront = -3.130f;
        private const float FieldFront = RimFront - LayerStep;
        private const float TileFront = FieldFront - LayerStep;
        private const float TextFront = TileFront - LayerStep;

        /// <summary>How far back each layer reaches. One depth for all of them; only the front
        /// face is ever seen.</summary>
        private const float BoardBack = -2.990f;

        // The blue tile that carries "PC".
        private const float TileMinX = -2.00f, TileMaxX = -0.55f;
        private const float TileMinY = 3.22f, TileMaxY = 3.88f;

        // "PC", inside the tile.
        private const float PcGlyphHeight = 0.54f;
        private const float PcGlyphWidth = 0.42f;
        private const float PcGlyphGap = 0.10f;

        // "EXPRESS", across the rest of the board.
        private const float WordMinX = -0.38f, WordMaxX = 2.02f;
        private const float WordGlyphHeight = 0.46f;
        private const float WordGap = 0.055f;

        /// <summary>
        /// A 5 by 7 blocky face, top row first.
        ///
        /// ⚠️ EMITTED AS VERTICAL RUNS, NOT AS ONE BOX PER LIT CELL. A cell-per-box "P" is 15
        /// boxes; the same glyph as column runs is 8. Across nine glyphs that is the difference
        /// between roughly 2,700 and 1,400 quads on a prop that stands at the edge of the arena,
        /// for a silhouette that is identical because the runs are contiguous.
        /// </summary>
        /// <remarks>
        /// ⚠️ INTERNAL RATHER THAN PRIVATE BECAUSE `IlalimNgTulayBuilder` PAINTS THE UNDER-BRIDGE
        /// SIGNAGE FROM THE SAME FACE. Two blocky fonts in one map is two things to keep in step
        /// and one of them will drift; the alphabet below is the map's, not this sign's.
        /// </remarks>
        internal static readonly Dictionary<char, string[]> Font = new Dictionary<char, string[]>
        {
            ['A'] = new[] { "01110", "10001", "10001", "11111", "10001", "10001", "10001" },
            ['B'] = new[] { "11110", "10001", "10001", "11110", "10001", "10001", "11110" },
            ['C'] = new[] { "01110", "10001", "10000", "10000", "10000", "10001", "01110" },
            ['D'] = new[] { "11110", "10001", "10001", "10001", "10001", "10001", "11110" },
            ['E'] = new[] { "11111", "10000", "10000", "11110", "10000", "10000", "11111" },
            ['H'] = new[] { "10001", "10001", "10001", "11111", "10001", "10001", "10001" },
            ['I'] = new[] { "11111", "00100", "00100", "00100", "00100", "00100", "11111" },
            ['L'] = new[] { "10000", "10000", "10000", "10000", "10000", "10000", "11111" },
            ['M'] = new[] { "10001", "11011", "10101", "10101", "10001", "10001", "10001" },
            ['O'] = new[] { "01110", "10001", "10001", "10001", "10001", "10001", "01110" },
            ['P'] = new[] { "11110", "10001", "10001", "11110", "10000", "10000", "10000" },
            ['R'] = new[] { "11110", "10001", "10001", "11110", "10100", "10010", "10001" },
            ['S'] = new[] { "01111", "10000", "10000", "01110", "00001", "10001", "01110" },
            ['T'] = new[] { "11111", "00100", "00100", "00100", "00100", "00100", "00100" },
            ['U'] = new[] { "10001", "10001", "10001", "10001", "10001", "10001", "01110" },
            ['W'] = new[] { "10001", "10001", "10001", "10101", "10101", "11011", "10001" },
            ['X'] = new[] { "10001", "10001", "01010", "00100", "01010", "10001", "10001" },
        };

        private readonly struct Box
        {
            public readonly string Material;
            public readonly Vector3 Min;
            public readonly Vector3 Max;

            public Box(string material, Vector3 min, Vector3 max)
            {
                Material = material;
                Min = min;
                Max = max;
            }
        }

        [MenuItem("Tumbang Preso/Author PC Express Sign")]
        public static void RunFromMenu() => Execute();

        public static void Run() => EditorApplication.Exit(Execute() ? 0 : 1);

        public static bool Execute()
        {
            if (!File.Exists(ObjPath))
            {
                Debug.LogError($"[PcExpressSign] missing {ObjPath}");
                return false;
            }

            var boxes = new List<Box>();

            // 1. The white rim, slightly proud of the board on every side. A frame drawn as four
            // strips would be four times the geometry for the same read at this distance.
            boxes.Add(new Box("pcex_sign_rim",
                new Vector3(BoardMinX - 0.07f, BoardMinY - 0.07f, RimFront),
                new Vector3(BoardMaxX + 0.07f, BoardMaxY + 0.07f, BoardBack)));

            // 2. The red field.
            boxes.Add(new Box("pcex_sign_field",
                new Vector3(BoardMinX, BoardMinY, FieldFront),
                new Vector3(BoardMaxX, BoardMaxY, BoardBack)));

            // 3. The blue tile behind "PC".
            boxes.Add(new Box("pcex_sign_tile",
                new Vector3(TileMinX, TileMinY, TileFront),
                new Vector3(TileMaxX, TileMaxY, BoardBack)));

            // 4. "PC" on the tile, centred in it.
            float pcWidth = PcGlyphWidth * 2.0f + PcGlyphGap;
            float pcX = (TileMinX + TileMaxX) * 0.5f - pcWidth * 0.5f;
            float pcY = (TileMinY + TileMaxY) * 0.5f - PcGlyphHeight * 0.5f;

            foreach (char c in "PC")
            {
                EmitGlyph(boxes, c, pcX, pcY, PcGlyphWidth, PcGlyphHeight);
                pcX += PcGlyphWidth + PcGlyphGap;
            }

            // 5. "EXPRESS" across the rest of the board.
            const string word = "EXPRESS";
            float glyphWidth = (WordMaxX - WordMinX - WordGap * (word.Length - 1)) / word.Length;
            float wordX = WordMinX;
            float wordY = (BoardMinY + BoardMaxY) * 0.5f - WordGlyphHeight * 0.5f;

            foreach (char c in word)
            {
                EmitGlyph(boxes, c, wordX, wordY, glyphWidth, WordGlyphHeight);
                wordX += glyphWidth + WordGap;
            }

            // ⚠️⚠️ THE WHOLE FASCIA IS MIRRORED IN LOCAL X, AND WITHOUT THIS THE SHOP SAYS
            // "SSERPXE CP". The shop front is the model's -Z face. A viewer reading a -Z-facing
            // surface stands at -Z looking toward +Z, and for that viewer "right" is local
            // MINUS X, not plus. Laying the glyphs out along +X the way you would on paper
            // therefore renders the wordmark backwards, and it renders backwards no matter how
            // the builder yaws the building, because the mirror is between the text and the face
            // it is painted on rather than between the building and the street.
            //
            // Mirroring here rather than at each layout site also moves the blue PC tile to the
            // viewer's left, which is the side it is on in the real fascia.
            for (int i = 0; i < boxes.Count; i++)
            {
                var b = boxes[i];
                boxes[i] = new Box(b.Material,
                                   new Vector3(-b.Max.x, b.Min.y, b.Min.z),
                                   new Vector3(-b.Min.x, b.Max.y, b.Max.z));
            }

            bool ok = RewriteObj(boxes);
            ok &= RewriteMtl();

            AssetDatabase.ImportAsset(ObjPath, ImportAssetOptions.ForceUpdate);
            AssetDatabase.ImportAsset(MtlPath, ImportAssetOptions.ForceUpdate);
            AssetDatabase.Refresh();

            Debug.Log(ok
                ? $"[PcExpressSign] rewrote the fascia: {boxes.Count} boxes across " +
                  $"{SignMaterials.Length - 3} materials."
                : "[PcExpressSign] FAILED");

            return ok;
        }

        /// <summary>
        /// One box per contiguous vertical run in a column. The letters stand on the tile or the
        /// field, so their back face is the layer behind them and only the front is ever lit.
        /// </summary>
        private static void EmitGlyph(List<Box> boxes, char c, float x, float y, float w, float h)
        {
            if (!Font.TryGetValue(c, out var rows)) return;

            float cell = w / 5.0f;
            float rowHeight = h / 7.0f;

            for (int col = 0; col < 5; col++)
            {
                int run = -1;

                for (int row = 0; row <= 7; row++)
                {
                    bool lit = row < 7 && rows[row][col] == '1';

                    if (lit && run < 0) run = row;
                    if (lit || run < 0) continue;

                    // Rows run top to bottom, so row 0 is the TOP of the glyph.
                    float top = y + h - run * rowHeight;
                    float bottom = y + h - row * rowHeight;

                    // ⚠️ THE BACK FACE GOES ALL THE WAY TO `BoardBack`, NOT TO THE LAYER BEHIND
                    // IT. "PC" sits on the blue tile and "EXPRESS" sits on the red field, and
                    // those two are 12 mm apart in Z. Ending every letter at the tile's front
                    // left the seven EXPRESS glyphs hovering 12 mm off the board with a shadow
                    // line under each one. Buried depth costs nothing: no side of a letter is
                    // ever seen.
                    boxes.Add(new Box("pcex_sign_text",
                        new Vector3(x + col * cell, bottom, TextFront),
                        new Vector3(x + (col + 1) * cell, top, BoardBack)));

                    run = -1;
                }
            }
        }

        private static bool RewriteObj(List<Box> boxes)
        {
            string[] lines = File.ReadAllLines(ObjPath);

            var kept = new List<string>();
            int vertexCount = 0;
            bool dropping = false;

            foreach (string raw in lines)
            {
                string line = raw.TrimEnd();

                // ⚠️⚠️ THE SENTINEL IS WHAT MAKES THIS IDEMPOTENT, AND DROPPING FACES ALONE IS
                // NOT ENOUGH. The first version deleted the old sign's `usemtl` blocks and left
                // its `v` lines behind, which is correct for the SHIPPED sign (its vertices are
                // interleaved with everything else's and renumbering them would corrupt the
                // mesh) and wrong for its own output, whose vertices are all past this line.
                // Re-running it therefore grew the file by 776 vertices each time: 364, then
                // 1140, then 1916, with the faces still pointing at the newest copy so nothing
                // looked broken. Everything from here down is this tool's and is regenerated.
                if (line.StartsWith(Sentinel, StringComparison.Ordinal)) break;

                if (line.StartsWith("v ", StringComparison.Ordinal)) vertexCount++;

                if (line.StartsWith("usemtl ", StringComparison.Ordinal))
                {
                    string mat = line.Substring(7).Trim();
                    dropping = Array.IndexOf(SignMaterials, mat) >= 0;
                    if (dropping) continue;
                }
                else if (dropping)
                {
                    // Everything up to the next usemtl belongs to the block being replaced.
                    if (line.StartsWith("f ", StringComparison.Ordinal) ||
                        line.StartsWith("s ", StringComparison.Ordinal) ||
                        line.Length == 0)
                    {
                        continue;
                    }

                    dropping = false;
                }

                kept.Add(line);
            }

            var sb = new StringBuilder();
            foreach (string line in kept) sb.AppendLine(line);

            sb.AppendLine();
            sb.AppendLine(Sentinel);
            sb.AppendLine("# Everything below is regenerated. Edit PcExpressSignAuthor, not this.");
            sb.AppendLine("# Red lightbox, white rim, blue PC tile, white EXPRESS wordmark.");

            // Vertices first, so every face below can be written against one running base.
            var verts = new StringBuilder();
            var faces = new StringBuilder();
            string current = null;
            int baseIndex = vertexCount;

            foreach (var box in boxes)
            {
                Vector3 a = box.Min, b = box.Max;

                // Same winding as the rest of the file: the -Z quad first, then +Z, then the
                // four sides. Copied rather than invented so the normals match its neighbours.
                Vector3[] corners =
                {
                    new Vector3(a.x, a.y, a.z), new Vector3(b.x, a.y, a.z),
                    new Vector3(b.x, b.y, a.z), new Vector3(a.x, b.y, a.z),
                    new Vector3(a.x, a.y, b.z), new Vector3(b.x, a.y, b.z),
                    new Vector3(b.x, b.y, b.z), new Vector3(a.x, b.y, b.z),
                };

                foreach (var v in corners)
                {
                    verts.AppendLine(string.Format(CultureInfo.InvariantCulture,
                        "v {0:F5} {1:F5} {2:F5}", v.x, v.y, v.z));
                }

                if (current != box.Material)
                {
                    faces.AppendLine();
                    faces.AppendLine($"usemtl {box.Material}");
                    faces.AppendLine("s 1");
                    current = box.Material;
                }

                int i = baseIndex + 1;
                faces.AppendLine($"f {i} {i + 3} {i + 2} {i + 1}");
                faces.AppendLine($"f {i + 5} {i + 6} {i + 7} {i + 4}");
                faces.AppendLine($"f {i + 3} {i + 7} {i + 6} {i + 2}");
                faces.AppendLine($"f {i} {i + 1} {i + 5} {i + 4}");
                faces.AppendLine($"f {i + 4} {i + 7} {i + 3} {i}");
                faces.AppendLine($"f {i + 1} {i + 2} {i + 6} {i + 5}");

                baseIndex += 8;
            }

            sb.Append(verts);
            sb.Append(faces);

            File.WriteAllText(ObjPath, sb.ToString());
            return true;
        }

        /// <summary>
        /// ⚠️ THE AWNING GOES WITH THE SIGN. It shipped green, white and red, which is the
        /// palette of the sari-sari store two maps over. A shop whose fascia is red and blue and
        /// whose canopy is red and green does not read as one building, it reads as two props
        /// that happen to touch.
        /// </summary>
        private static readonly (string name, Color diffuse, Color emission)[] Materials =
        {
            ("pcex_sign_rim", new Color(0.980f, 0.980f, 0.975f), new Color(0.42f, 0.42f, 0.42f)),
            ("pcex_sign_field", new Color(0.855f, 0.098f, 0.129f), new Color(0.40f, 0.04f, 0.05f)),
            ("pcex_sign_tile", new Color(0.086f, 0.220f, 0.596f), new Color(0.04f, 0.10f, 0.30f)),
            ("pcex_sign_text", new Color(0.985f, 0.985f, 0.980f), new Color(0.62f, 0.62f, 0.62f)),
            ("pcex_awning_green", new Color(0.086f, 0.220f, 0.596f), Color.clear),
            ("pcex_awning_red", new Color(0.855f, 0.098f, 0.129f), Color.clear),
            ("pcex_awning_white", new Color(0.960f, 0.960f, 0.955f), Color.clear),
        };

        private static bool RewriteMtl()
        {
            string text = File.Exists(MtlPath) ? File.ReadAllText(MtlPath) : "";
            var blocks = new List<string>();

            foreach (string chunk in text.Split(new[] { "newmtl " }, StringSplitOptions.None))
            {
                if (chunk.Length == 0) continue;

                string name = chunk.Split('\n')[0].Trim();

                // Drop the shipped sign materials and anything this tool owns; they are all
                // re-emitted below.
                if (Array.IndexOf(SignMaterials, name) >= 0) continue;

                bool replaced = false;
                foreach (var m in Materials)
                    if (m.name == name) { replaced = true; break; }

                if (replaced) continue;

                blocks.Add(chunk.StartsWith("#") ? chunk : "newmtl " + chunk);
            }

            var sb = new StringBuilder();
            foreach (string b in blocks) sb.Append(b.TrimEnd()).AppendLine().AppendLine();

            foreach (var m in Materials)
            {
                sb.AppendLine($"newmtl {m.name}");
                sb.AppendLine(string.Format(CultureInfo.InvariantCulture, "Kd {0:F5} {1:F5} {2:F5}",
                                            m.diffuse.r, m.diffuse.g, m.diffuse.b));

                if (m.emission.a > 0.0f || m.emission.r + m.emission.g + m.emission.b > 0.0f)
                {
                    sb.AppendLine(string.Format(CultureInfo.InvariantCulture, "Ke {0:F5} {1:F5} {2:F5}",
                                                m.emission.r, m.emission.g, m.emission.b));
                }

                sb.AppendLine("Ks 0.00000 0.00000 0.00000");
                sb.AppendLine("Ns 10.0");
                sb.AppendLine("d 1.0000");
                sb.AppendLine("illum 2");
                sb.AppendLine();
            }

            File.WriteAllText(MtlPath, sb.ToString());
            return true;
        }
    }
}
