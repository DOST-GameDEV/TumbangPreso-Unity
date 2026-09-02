using System;
using System.Collections.Generic;
using System.IO;
using TumbangPreso.Core;
using UnityEngine;

namespace TumbangPreso.UI
{
    /// <summary>
    /// Central registry and sprite provider for the competitive ladder rank emblems and state glyphs.
    ///
    /// ⚠️ HOUSE STYLE: Hand-painted wood panel, amber gold accent, cream body, ink outlines.
    /// ⚠️ TIER ORDER: Follows RatingRules.TierNames (0: BATA, 1: KANTO, 2: BARANGAY, 3: KAMPEON, 4: ALAMAT).
    /// </summary>
    public static class RankIcons
    {
        private const string RankArtDir = "Assets/TumbangPreso/Art/UI/Rank";
        private static readonly Dictionary<int, Sprite> TierSprites = new Dictionary<int, Sprite>();
        private static readonly Dictionary<string, Sprite> GlyphSprites = new Dictionary<string, Sprite>(StringComparer.OrdinalIgnoreCase);

        private static readonly string[] TierFileNames =
        {
            "rank_bata_v1",
            "rank_kanto_v1",
            "rank_barangay_v1",
            "rank_kampeon_v1",
            "rank_alamat_v1",
        };

        /// <summary>
        /// Gets the Sprite for a given rank tier.
        /// </summary>
        public static Sprite ForTier(RankTier tier)
        {
            return ForTierIndex((int)tier);
        }

        /// <summary>
        /// Gets the Sprite for a given tier index (0: BATA, 1: KANTO, 2: BARANGAY, 3: KAMPEON, 4: ALAMAT).
        /// </summary>
        public static Sprite ForTierIndex(int index)
        {
            if (index < 0 || index >= TierFileNames.Length) return null;
            if (TierSprites.TryGetValue(index, out var cached) && cached != null) return cached;

            var sprite = LoadSprite(TierFileNames[index]);
            if (sprite != null) TierSprites[index] = sprite;
            return sprite;
        }

        /// <summary>
        /// Gets the Sprite for a state glyph ("queue", "backfill", "blocked", "disputed", "placing").
        /// </summary>
        public static Sprite ForGlyph(string glyphName)
        {
            if (string.IsNullOrEmpty(glyphName)) return null;
            if (GlyphSprites.TryGetValue(glyphName, out var cached) && cached != null) return cached;

            string fileName = $"glyph_{glyphName.ToLowerInvariant()}_v1";
            if (glyphName.Equals("still_placing", StringComparison.OrdinalIgnoreCase) || glyphName.Equals("placing", StringComparison.OrdinalIgnoreCase))
                fileName = "glyph_still_placing_v1";

            var sprite = LoadSprite(fileName);
            if (sprite != null) GlyphSprites[glyphName] = sprite;
            return sprite;
        }

        private static Sprite LoadSprite(string baseName)
        {
            var res = Resources.Load<Sprite>($"UI/Rank/{baseName}");
            if (res != null) return res;

#if UNITY_EDITOR
            string assetPath = $"{RankArtDir}/{baseName}.png";
            var editorSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
            if (editorSprite != null) return editorSprite;
#endif

            string filePath = Path.Combine(Application.dataPath, "TumbangPreso/Art/UI/Rank", $"{baseName}.png");
            if (File.Exists(filePath))
            {
                try
                {
                    byte[] bytes = File.ReadAllBytes(filePath);
                    var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                    if (tex.LoadImage(bytes))
                    {
                        tex.filterMode = FilterMode.Bilinear;
                        tex.wrapMode = TextureWrapMode.Clamp;
                        return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100.0f);
                    }
                }
                catch (Exception)
                {
                }
            }

            return null;
        }
    }
}
