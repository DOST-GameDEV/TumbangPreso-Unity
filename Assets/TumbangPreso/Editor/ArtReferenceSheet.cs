using System.Collections.Generic;
using System.IO;
using System.Text;
using TumbangPreso.Abilities;
using TumbangPreso.Core;
using TumbangPreso.InputLayer;
using TumbangPreso.UI;
using UnityEditor;
using UnityEngine;

namespace TumbangPreso.EditorTools
{
    /// <summary>
    /// Writes every icon this game bakes in code out as a PNG, plus a manifest naming what each
    /// one is for, so an artist can be handed the whole vocabulary at once.
    ///
    /// ⚠️⚠️ IT EXISTS BECAUSE NONE OF THIS ART IS A FILE. `AbilityIcons`, `VerbIcons` and
    /// `UltimateMotifs` all bake at runtime into `HideFlags.HideAndDontSave` textures, for the
    /// reason `AbilityIcons`' header gives: *"a baked file that drifts from the code that wanted
    /// it is indistinguishable from a broken conversion."* That is right for the game and useless
    /// for somebody redesigning it, because there is nothing on disk to open. **This is the
    /// export, not a second source.** Nothing reads what it writes; deleting the output folder
    /// costs one menu click.
    ///
    /// ⚠️ THEY COME OUT WHITE ON TRANSPARENT, WHICH IS HOW THEY ARE STORED. Every one is tinted
    /// at its use site (`Image.color`), so a sheet of white glyphs is the honest master and the
    /// hero colour belongs to `UiTheme.ColorForHero`. The manifest names the tint each one gets
    /// in play so a redesign can reproduce it.
    ///
    /// ⚠️ THE MANIFEST IS WHAT MAKES THE FOLDER USABLE. 33 numbered PNGs with no key is a puzzle;
    /// the JSON says which hero owns which ability, what job the icon claims to do
    /// (`AbilityIcons.LabelFor`), what its cooldown is, and which touch control draws it.
    /// </summary>
    public static class ArtReferenceSheet
    {
        private const string Root = "Logs/art-reference";
        private const string IconDir = Root + "/icons";

        /// <summary>
        /// How big each exported icon is, in pixels.
        ///
        /// ⚠️ 256, WHICH IS FOUR TIMES THE BAKE. `AbilityIcons.Size` and `VerbIcons.Size` are
        /// both 128 because that is above the sampling rate at every resolution the game draws
        /// them at. An artist opens these at 100 per cent to trace a silhouette, so the export
        /// is point-sampled up rather than left at the game's own budget. **Upscaling a
        /// signed-distance bake is lossless in the way that matters**: the shape is the
        /// information and the feathering is not.
        /// </summary>
        private const int ExportSize = 256;

        [MenuItem("Tumbang Preso/Art/Export Icon Reference")]
        public static void ExportFromMenu() => Export();

        public static void Export()
        {
            Directory.CreateDirectory(IconDir);

            var manifest = new StringBuilder();
            manifest.AppendLine("{");
            manifest.AppendLine("  \"generated\": \"" + System.DateTime.Now.ToString("s") + "\",");

            // -------------------------------------------------------------------
            // § THE HEROES AND THEIR EIGHTEEN ABILITIES
            // -------------------------------------------------------------------
            manifest.AppendLine("  \"heroes\": [");

            var heroes = new List<string>();
            foreach (var person in Roster.HeroPeople) heroes.Add(person.Id);

            for (int h = 0; h < heroes.Count; h++)
            {
                string heroId = heroes[h];
                var kit = HeroAbilitySystem.CreateKitFor(heroId);
                if (kit == null) continue;

                Color accent = UiTheme.ColorForHero(heroId);

                manifest.AppendLine("    {");
                manifest.AppendLine($"      \"id\": \"{heroId}\",");
                manifest.AppendLine($"      \"name\": \"{Escape(kit.HeroName)}\",");
                manifest.AppendLine($"      \"accent\": \"{Hex(accent)}\",");
                manifest.AppendLine($"      \"accentBright\": \"{Hex(UiTheme.BrightForHero(heroId))}\",");
                manifest.AppendLine($"      \"motif\": \"icons/motif_{heroId}.png\",");

                WriteSprite(UltimateMotifs.For(heroId), $"motif_{heroId}", 512, 64);

                manifest.AppendLine("      \"abilities\": [");

                var slots = new[]
                {
                    ("skill1", kit.Skill1),
                    ("skill2", kit.Skill2),
                    ("ultimate", kit.Ultimate),
                };

                for (int i = 0; i < slots.Length; i++)
                {
                    var (slotName, ability) = slots[i];
                    if (ability == null) continue;

                    string file = $"ability_{heroId}_{slotName}";
                    WriteSprite(AbilityIcons.For(ability.Glyph), file, ExportSize, ExportSize);

                    manifest.AppendLine("        {");
                    manifest.AppendLine($"          \"slot\": \"{slotName}\",");
                    manifest.AppendLine($"          \"name\": \"{Escape(ability.Name)}\",");
                    manifest.AppendLine($"          \"glyph\": \"{ability.Glyph}\",");
                    manifest.AppendLine($"          \"job\": \"{AbilityIcons.LabelFor(ability.Glyph)}\",");
                    manifest.AppendLine($"          \"cooldown\": {ability.Cooldown:F2},");
                    manifest.AppendLine($"          \"telegraphRadius\": {ability.TelegraphRadius:F2},");
                    manifest.AppendLine($"          \"icon\": \"icons/{file}.png\"");
                    manifest.AppendLine("        }" + (i < slots.Length - 1 ? "," : ""));
                }

                manifest.AppendLine("      ]");
                manifest.AppendLine("    }" + (h < heroes.Count - 1 ? "," : ""));
            }

            manifest.AppendLine("  ],");

            // -------------------------------------------------------------------
            // § THE NINE TOUCH CONTROLS
            //
            // ⚠️ THESE ARE NEW ON 2026-09-04 AND ARE THE ONE PART OF THIS SHEET AN ARTIST IS
            // MOST LIKELY TO WANT TO REDRAW. They are procedural stand-ins for what a real icon
            // set would carry: `docs/TODO.md` § 134.1 is why they exist at all.
            // -------------------------------------------------------------------
            manifest.AppendLine("  \"touchControls\": [");

            var verbs = InputCatalogue.All;

            for (int i = 0; i < verbs.Count; i++)
            {
                var entry = verbs[i];
                string file = $"verb_{entry.Verb}";

                WriteSprite(VerbIcons.For(entry.Glyph), file, ExportSize, ExportSize);

                manifest.AppendLine("    {");
                manifest.AppendLine($"      \"verb\": \"{entry.Verb}\",");
                manifest.AppendLine($"      \"label\": \"{Escape(entry.TouchLabel)}\",");
                manifest.AppendLine($"      \"glyph\": \"{entry.Glyph}\",");
                manifest.AppendLine($"      \"describes\": \"{VerbIcons.DescribeFor(entry.Glyph)}\",");
                manifest.AppendLine($"      \"zone\": \"{entry.Zone}\",");
                manifest.AppendLine($"      \"size\": \"{entry.Size}\",");
                manifest.AppendLine($"      \"sizeUnits\": {TouchMetrics.UnitsFor(entry.Size):F0},");
                manifest.AppendLine($"      \"icon\": \"icons/{file}.png\"");
                manifest.AppendLine("    }" + (i < verbs.Count - 1 ? "," : ""));
            }

            manifest.AppendLine("  ],");

            // -------------------------------------------------------------------
            // § THE GENERIC GLYPH VOCABULARY
            //
            // ⚠️ `docs/VISION.md` § 3 RULE 1: *"the icon says what the power does to the WORLD,
            // not what element it is made of."* The nine generic glyphs ARE that vocabulary and
            // every bespoke hero icon still reports one of their job words, so an artist
            // redrawing the set needs the family as well as the eighteen.
            // -------------------------------------------------------------------
            manifest.AppendLine("  \"glyphVocabulary\": [");

            var generic = new[]
            {
                AbilityGlyph.Zone, AbilityGlyph.Wall, AbilityGlyph.Dash, AbilityGlyph.Shield,
                AbilityGlyph.Burst, AbilityGlyph.Projectile, AbilityGlyph.Phase,
                AbilityGlyph.Slam, AbilityGlyph.Empower,
            };

            for (int i = 0; i < generic.Length; i++)
            {
                string file = $"glyph_{generic[i]}";
                WriteSprite(AbilityIcons.For(generic[i]), file, ExportSize, ExportSize);

                manifest.AppendLine("    {");
                manifest.AppendLine($"      \"glyph\": \"{generic[i]}\",");
                manifest.AppendLine($"      \"job\": \"{AbilityIcons.LabelFor(generic[i])}\",");
                manifest.AppendLine($"      \"icon\": \"icons/{file}.png\"");
                manifest.AppendLine("    }" + (i < generic.Length - 1 ? "," : ""));
            }

            manifest.AppendLine("  ],");

            // -------------------------------------------------------------------
            // § THE PALETTE, SO A REDESIGN STARTS FROM THE MEASURED VALUES
            // -------------------------------------------------------------------
            manifest.AppendLine("  \"palette\": {");
            manifest.AppendLine($"    \"brandDeepRed\": \"{Hex(UiTheme.BrandRed)}\",");
            manifest.AppendLine($"    \"brandHoneyQuartz\": \"{Hex(UiTheme.BrandHoney)}\",");
            manifest.AppendLine($"    \"brandChartreuse\": \"{Hex(UiTheme.BrandChartreuse)}\",");
            manifest.AppendLine($"    \"brandPersimmon\": \"{Hex(UiTheme.BrandPersimmon)}\",");
            manifest.AppendLine($"    \"hudCream\": \"{Hex(UiTheme.Cream)}\",");
            manifest.AppendLine($"    \"hudAmber\": \"{Hex(UiTheme.Amber)}\",");
            manifest.AppendLine($"    \"warmInk\": \"{Hex(UiTheme.Ink)}\",");
            manifest.AppendLine($"    \"roleOffense\": \"{Hex(UiTheme.Offense)}\",");
            manifest.AppendLine($"    \"roleDefense\": \"{Hex(UiTheme.Defense)}\"");
            manifest.AppendLine("  }");
            manifest.AppendLine("}");

            Directory.CreateDirectory(Root);
            File.WriteAllText($"{Root}/manifest.json", manifest.ToString());

            Debug.Log($"[ArtReference] wrote {Root}/manifest.json and the icon set to {IconDir}");
        }

        /// <summary>
        /// Reads a baked sprite's texture back out and writes it as a PNG at export size.
        ///
        /// ⚠️⚠️ IT COPIES THROUGH A `RenderTexture` RATHER THAN CALLING `GetPixels` DIRECTLY.
        /// The baked textures are created with `hideFlags = HideAndDontSave` and no `Apply`
        /// with `makeNoLongerReadable`, so they happen to be readable today; relying on that
        /// couples this exporter to an implementation detail of three other files. A blit
        /// through a temporary target works whatever the source's read flags are and does the
        /// upscale in the same operation.
        ///
        /// ⚠️ POINT FILTERING ON THE WAY UP, deliberately. These are signed-distance bakes: the
        /// EDGE is the art, and bilinear upscaling of an already-feathered edge produces a soft
        /// double ramp that an artist would have to undo before tracing it.
        /// </summary>
        private static void WriteSprite(Sprite sprite, string name, int width, int height)
        {
            if (sprite == null || sprite.texture == null) return;

            var previous = RenderTexture.active;
            var target = RenderTexture.GetTemporary(width, height, 0,
                                                    RenderTextureFormat.ARGB32);
            target.filterMode = FilterMode.Point;

            try
            {
                var source = sprite.texture;
                var wasFilter = source.filterMode;
                source.filterMode = FilterMode.Point;

                Graphics.Blit(source, target);
                source.filterMode = wasFilter;

                RenderTexture.active = target;

                var copy = new Texture2D(width, height, TextureFormat.RGBA32, false);
                copy.ReadPixels(new Rect(0, 0, width, height), 0, 0, false);
                copy.Apply(false, false);

                File.WriteAllBytes($"{IconDir}/{name}.png", copy.EncodeToPNG());
                Object.DestroyImmediate(copy);
            }
            finally
            {
                RenderTexture.active = previous;
                RenderTexture.ReleaseTemporary(target);
            }
        }

        private static string Hex(Color c)
            => "#" + ColorUtility.ToHtmlStringRGB(c);

        private static string Escape(string s)
            => string.IsNullOrEmpty(s) ? "" : s.Replace("\\", "\\\\").Replace("\"", "\\\"");
    }
}
