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
    /// ⚠️⚠️ THE OFFICIAL LOGO IS AN ASSET, NOT A FONT TO APPROXIMATE. The first authored pass
    /// rebuilt "PC EXPRESS" in a 5 by 7 block face. It carried the colours and remained the
    /// wrong mark: the real logo has a joined PC monogram, an italic X, a blue shadow and a
    /// slanted red-blue field. `tools/build_pc_express_logo_mesh.py` traces the supplied
    /// official artwork into raised letter geometry; this tool builds the deep lightbox,
    /// frame, backing and halo it mounts on.
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
            "pcex_sign_text", "pcex_sign_edge", "pcex_sign_logo_edge",
            "pcex_awning_green", "pcex_awning_red", "pcex_awning_white",
        };

        // ------------------------------------------------------------------
        // The fascia, in the model's own local space. The shop front is the -Z face: the glass
        // sits at z = -2.75, the shutter at -3.15..-2.81 and the awning reaches -4.10, all
        // measured out of the shipped file.
        // ------------------------------------------------------------------

        private const float BoardMinX = -2.15f, BoardMaxX = 2.15f;
        private const float BoardMinY = 3.00f, BoardMaxY = 4.137f;

        /// <summary>Each layer stands 12 mm proud of the one behind it, which is enough for the
        /// rim, the tile and the letters to catch the sun separately without z-fighting.</summary>
        private const float LayerStep = 0.012f;

        private const float RimFront = -3.130f;
        private const float FieldFront = RimFront - LayerStep;
        private const float LogoEdgeFront = FieldFront - 0.040f;

        /// <summary>How far back each layer reaches. One depth for all of them; only the front
        /// face is ever seen.</summary>
        private const float BoardBack = -2.990f;

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
            ['F'] = new[] { "11111", "10000", "10000", "11110", "10000", "10000", "10000" },
            ['G'] = new[] { "01110", "10001", "10000", "10111", "10001", "10001", "01110" },
            ['H'] = new[] { "10001", "10001", "10001", "11111", "10001", "10001", "10001" },
            ['I'] = new[] { "11111", "00100", "00100", "00100", "00100", "00100", "11111" },
            ['J'] = new[] { "00111", "00010", "00010", "00010", "10010", "10010", "01100" },
            ['K'] = new[] { "10001", "10010", "10100", "11000", "10100", "10010", "10001" },
            ['L'] = new[] { "10000", "10000", "10000", "10000", "10000", "10000", "11111" },
            ['M'] = new[] { "10001", "11011", "10101", "10101", "10001", "10001", "10001" },
            ['N'] = new[] { "10001", "11001", "10101", "10011", "10001", "10001", "10001" },
            ['O'] = new[] { "01110", "10001", "10001", "10001", "10001", "10001", "01110" },
            ['P'] = new[] { "11110", "10001", "10001", "11110", "10000", "10000", "10000" },
            ['Q'] = new[] { "01110", "10001", "10001", "10001", "10101", "10010", "01101" },
            ['R'] = new[] { "11110", "10001", "10001", "11110", "10100", "10010", "10001" },
            ['S'] = new[] { "01111", "10000", "10000", "01110", "00001", "10001", "01110" },
            ['T'] = new[] { "11111", "00100", "00100", "00100", "00100", "00100", "00100" },
            ['U'] = new[] { "10001", "10001", "10001", "10001", "10001", "10001", "01110" },
            ['V'] = new[] { "10001", "10001", "10001", "10001", "10001", "01010", "00100" },
            ['W'] = new[] { "10001", "10001", "10001", "10101", "10101", "11011", "10001" },
            ['X'] = new[] { "10001", "10001", "01010", "00100", "01010", "10001", "10001" },
            ['Y'] = new[] { "10001", "10001", "01010", "00100", "00100", "00100", "00100" },
            ['Z'] = new[] { "11111", "00001", "00010", "00100", "01000", "10000", "11111" },
            ['0'] = new[] { "01110", "10001", "10011", "10101", "11001", "10001", "01110" },
            ['1'] = new[] { "00100", "01100", "00100", "00100", "00100", "00100", "01110" },
            ['2'] = new[] { "01110", "10001", "00001", "00010", "00100", "01000", "11111" },
            ['3'] = new[] { "11110", "00001", "00001", "01110", "00001", "00001", "11110" },
            ['4'] = new[] { "00010", "00110", "01010", "10010", "11111", "00010", "00010" },
            ['5'] = new[] { "11111", "10000", "10000", "11110", "00001", "00001", "11110" },
            ['6'] = new[] { "01110", "10000", "10000", "11110", "10001", "10001", "01110" },
            ['7'] = new[] { "11111", "00001", "00010", "00100", "01000", "01000", "01000" },
            ['8'] = new[] { "01110", "10001", "10001", "01110", "10001", "10001", "01110" },
            ['9'] = new[] { "01110", "10001", "10001", "01111", "00001", "00001", "01110" },
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

            // 1. A dark metal return gives the sign a readable side profile from both lanes.
            boxes.Add(new Box("pcex_sign_edge",
                new Vector3(BoardMinX - 0.12f, BoardMinY - 0.12f, RimFront + 0.025f),
                new Vector3(BoardMaxX + 0.12f, BoardMaxY + 0.12f, BoardBack + 0.05f)));

            // 2. The white illuminated rim from the supplied exterior reference.
            boxes.Add(new Box("pcex_sign_rim",
                new Vector3(BoardMinX - 0.07f, BoardMinY - 0.07f, RimFront),
                new Vector3(BoardMaxX + 0.07f, BoardMaxY + 0.07f, BoardBack)));

            // 3. Red glass backing remains visible around the raised official logo plate.
            boxes.Add(new Box("pcex_sign_field",
                new Vector3(BoardMinX, BoardMinY, FieldFront),
                new Vector3(BoardMaxX, BoardMaxY, BoardBack)));

            // 4. A 40 mm blue glass band carries the separate generated 3D letter mesh. That
            // mesh is derived from the supplied official logo rather than from this block font.
            boxes.Add(new Box("pcex_sign_logo_edge",
                new Vector3(BoardMinX + 0.025f, BoardMinY + 0.025f, LogoEdgeFront),
                new Vector3(BoardMaxX - 0.025f, BoardMaxY - 0.025f, FieldFront)));

            bool ok = RewriteObj(boxes);
            ok &= RewriteMtl();

            AssetDatabase.ImportAsset(ObjPath, ImportAssetOptions.ForceUpdate);
            AssetDatabase.ImportAsset(MtlPath, ImportAssetOptions.ForceUpdate);
            AssetDatabase.Refresh();

            Debug.Log(ok
                ? $"[PcExpressSign] mounted the official logo on {boxes.Count} fascia layers."
                : "[PcExpressSign] FAILED");

            return ok;
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

            // The sentinel is preceded by the blank line this tool emitted last run. Keeping
            // that line and then emitting another one grows the source by one newline per run,
            // which is still non-idempotent even though the vertex count stays fixed.
            while (kept.Count > 0 && string.IsNullOrWhiteSpace(kept[kept.Count - 1]))
                kept.RemoveAt(kept.Count - 1);

            var sb = new StringBuilder();
            foreach (string line in kept) sb.AppendLine(line);

            sb.AppendLine();
            sb.AppendLine(Sentinel);
            sb.AppendLine("# Everything below is regenerated. Edit PcExpressSignAuthor, not this.");
            sb.AppendLine("# Official PC Express logo on a raised red-glass lightbox.");

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
        /// ⚠️ THE OLD STRIPED AWNING IS REMOVED WITH THE SIGN MATERIAL BLOCKS. The supplied
        /// exterior is a glass computer showroom under one red-blue fascia, not a sari-sari
        /// canopy. A thin modern overhang is added by the scene builder instead.
        /// </summary>
        private static readonly (string name, Color diffuse, Color emission)[] Materials =
        {
            ("pcex_sign_edge", new Color(0.120f, 0.135f, 0.155f), Color.clear),
            ("pcex_sign_rim", new Color(0.980f, 0.980f, 0.975f), new Color(0.42f, 0.42f, 0.42f)),
            ("pcex_sign_field", new Color(0.855f, 0.098f, 0.129f), new Color(0.40f, 0.04f, 0.05f)),
            ("pcex_sign_logo_edge", new Color(0.055f, 0.180f, 0.490f), new Color(0.02f, 0.06f, 0.16f)),
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
