using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace TumbangPreso.InputLayer
{
    /// <summary>
    /// The controller line art, and where each control sits on it.
    ///
    /// ⚠️⚠️ BOTH HALVES COME OUT OF `tools/build_controller_diagram.py` IN ONE PASS, AND THAT IS
    /// THE ONLY REASON A LEADER LINE CAN BE TRUSTED TO END ON THE BUTTON IT NAMES. If the picture
    /// were generated and the arrow-heads typed into C#, moving the d-pad two hundred pixels left
    /// would be a change to one file that silently makes four lines in another point at bare
    /// plastic, with nothing in the repository able to notice. That is `Settings.Rebinding`'s own
    /// two-table warning exactly: *"a stale row in either table is not cosmetic ... a missing
    /// action silently produces a dead row instead, which is worse, because nobody notices."*
    ///
    /// ⚠️ THE MANIFEST IS `name x y` LINES RATHER THAN JSON, because `JsonUtility` cannot
    /// deserialise a dictionary and a three-token line needs no parser and no package.
    ///
    /// ⚠️⚠️ A MISSING FILE IS A NULL AND NEVER AN EXCEPTION, for the reason `UI.InputGlyphs`
    /// gives about its own sheets: the one situation worth designing for is somebody cloning the
    /// repository without the generated art, and a front end that throws is worse than one that
    /// falls back. `ControllerMapScreen` draws its callouts as a plain two-column list when there
    /// is no picture, which is the screen the settings panel already had.
    /// </summary>
    public static class PadDiagram
    {
        public const string ArtPath = "UI/input/pad_diagram_v1";
        /// <summary>
        /// ⚠️⚠️ A DIFFERENT BASENAME FROM THE PNG, AND IT COST THE FIRST RENDER ALL EIGHTEEN OF
        /// ITS LEADER LINES. This was `pad_diagram_v1` for both, and
        /// `Resources.Load&lt;TextAsset&gt;("UI/input/pad_diagram_v1")` resolved the PNG for that
        /// path and answered **null**: no error, no log, and a screen that drew the pad and every
        /// callout perfectly with nothing joining them. `Resources.Load` matches on the PATH first
        /// and the type second.
        /// </summary>
        public const string AnchorPath = "UI/input/pad_diagram_v1_anchors";

        /// <summary>
        /// The picture's aspect, so a caller can size a box for it without loading the texture.
        ///
        /// ⚠️ IT IS DERIVED FROM THE SPRITE WHEN THERE IS ONE AND ONLY FALLS BACK TO THIS. A
        /// constant that disagrees with the art is `CLAUDE.md` § 5's drift rule in miniature, and
        /// the fallback exists for the no-art case above rather than as the answer.
        /// </summary>
        public const float FallbackAspect = 1400.0f / 875.0f;

        private static bool _loaded;
        private static Sprite _art;
        private static Dictionary<string, Vector2> _anchors;

        /// <summary>
        /// The line art, or null when the generated PNG is not in the project.
        ///
        /// ⚠️⚠️ IT IS BUILT FROM A `Texture2D` RATHER THAN LOADED AS A `Sprite`, BECAUSE
        /// `EditorTools.InputGlyphImport` FORCES `textureType = Default` ON EVERYTHING IN THIS
        /// FOLDER. That postprocessor is deliberately scoped by path and its own note says so;
        /// asking `Resources.Load&lt;Sprite&gt;` for a file imported as a plain texture returns
        /// **null**, with no error and nothing in the log, which is a screen that silently loses
        /// its one picture. `UI.InputGlyphs` slices the same folder's sheets the same way for the
        /// same reason.
        /// </summary>
        public static Sprite Art
        {
            get { Load(); return _art; }
        }

        public static float Aspect
            => Art != null && Art.rect.height > 0.0f
                ? Art.rect.width / Art.rect.height
                : FallbackAspect;

        /// <summary>
        /// Where a control sits on the picture, normalised, with Y measured DOWN from the top.
        /// Returns false for a control the drawing does not have.
        ///
        /// ⚠️ Y IS DOWN BECAUSE THE PICTURE'S OWN COORDINATES ARE, and the flip happens once, in
        /// the one place that turns an anchor into a `RectTransform` position. Flipping it here
        /// would mean the generator and this file disagreed about what the numbers mean, which is
        /// the sort of thing that is invisible until every leader line is mirrored.
        /// </summary>
        public static bool TryAnchor(string control, out Vector2 normalised)
        {
            Load();

            normalised = Vector2.zero;
            if (_anchors == null || string.IsNullOrEmpty(control)) return false;

            return _anchors.TryGetValue(control, out normalised);
        }

        /// <summary>Every control the drawing knows, for the test that asserts the two agree.</summary>
        public static IEnumerable<string> KnownControls()
        {
            Load();
            return _anchors != null ? (IEnumerable<string>)_anchors.Keys : new string[0];
        }

        private static void Load()
        {
            if (_loaded) return;
            _loaded = true;

            var texture = Resources.Load<Texture2D>(ArtPath);

            if (texture != null)
            {
                _art = Sprite.Create(texture,
                                     new Rect(0.0f, 0.0f, texture.width, texture.height),
                                     new Vector2(0.5f, 0.5f), 100.0f);
                _art.name = "pad_diagram";
            }

            var manifest = Resources.Load<TextAsset>(AnchorPath);
            if (manifest == null) return;

            _anchors = new Dictionary<string, Vector2>(24);

            foreach (string raw in manifest.text.Split('\n'))
            {
                string line = raw.Trim();
                if (line.Length == 0 || line[0] == '#') continue;

                string[] parts = line.Split(' ');
                if (parts.Length != 3) continue;

                // ⚠️ `InvariantCulture`, AND THIS IS NOT PEDANTRY. The generator writes `0.32812`
                // with a dot; `float.Parse` with the machine's culture reads that as 32812 on a
                // Philippine, German or French locale, and every leader line then points several
                // thousand units off the side of the screen. The failure is invisible on the
                // machine that wrote the file.
                if (!float.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture,
                                    out float x)) continue;

                if (!float.TryParse(parts[2], NumberStyles.Float, CultureInfo.InvariantCulture,
                                    out float y)) continue;

                _anchors[parts[0]] = new Vector2(x, y);
            }
        }
    }
}
