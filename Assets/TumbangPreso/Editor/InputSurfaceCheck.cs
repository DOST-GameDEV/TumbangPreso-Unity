using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace TumbangPreso.EditorTools
{
    /// <summary>
    /// Refuses a source file that builds a screen without going through the kit that gives it a
    /// controller focus path and thumb-sized hit areas.
    ///
    /// ⚠️⚠️ IT READS THE SOURCE AS TEXT, DELIBERATELY, AND `SceneScriptCheck` IS THE PRECEDENT.
    /// That check reads scenes as text because *"opening the scene is what hides the fault"*; the
    /// same is true here one level up. Every runtime check in this project can only see a screen
    /// that has been OPENED, so a screen nobody opens during a test run is a screen with no
    /// coverage at all, and `docs/TODO.md` § 96, § 114 and § 124.11 are three separate occasions
    /// when exactly that happened. **A grep cannot be fooled by a screen not being reached.**
    ///
    /// ⚠️⚠️ THE RULE IT ENFORCES IS "ONE DOOR INTO A CANVAS", AND IT IS THE STRUCTURAL HALF OF
    /// THE FUTURE-PROOFING. `MenuKit.BuildCanvas` installs `ScreenFocus` and upgrades the input
    /// module; `ConvertedScreen.Start` does the same for the authored screens. A file that calls
    /// `AddComponent&lt;Canvas&gt;()` itself has built a screen those two never saw, and the
    /// failure is silent: it draws perfectly, a mouse works, and a pad and a thumb do nothing.
    /// 🧑: *"anytime we add a feature, make sure all controller and mobile is considered"*. This
    /// is what makes that true of a screen written by somebody who never read this sentence.
    ///
    /// ⚠️ THE ALLOWED LIST IS THREE FILES AND EACH ONE IS ARGUED. Widening it is how the rule
    /// dies, so a new entry needs a reason written beside it, not just a name.
    /// </summary>
    public static class InputSurfaceCheck
    {
        private const string OutPath = "Logs/input-surface-check.txt";

        private const string RuntimeRoot = "Assets/TumbangPreso/Runtime";

        /// <summary>
        /// The files that may build a `Canvas` directly.
        ///
        /// ⚠️ EACH IS HERE FOR A STATED REASON:
        /// - `MenuKit.cs` IS the door. It is the method every other screen calls.
        /// - `AspectSafeCanvas.cs` configures a canvas rather than owning a screen; it never adds
        ///   controls, so there is nothing for a pad to walk.
        /// - `CanvasLifetime.cs` only destroys a canvas somebody else built.
        /// </summary>
        private static readonly string[] Allowed =
        {
            "MenuKit.cs",
            "AspectSafeCanvas.cs",
            "CanvasLifetime.cs",
        };

        /// <summary>
        /// ⚠️ `AddComponent<Canvas>` IN ANY SPACING. `AddComponent < Canvas >` compiles and would
        /// walk straight past a plain string search, and so would a generic called through a
        /// variable. This catches the forms anybody actually writes and the check is a gate
        /// rather than a proof.
        /// </summary>
        private static readonly Regex CanvasBuild =
            new Regex(@"AddComponent\s*<\s*Canvas\s*>", RegexOptions.Compiled);

        /// <summary>
        /// Something a player can press. A canvas with none of these is a DISPLAY, not a screen.
        ///
        /// ⚠️⚠️ THE FIRST VERSION OF THIS CHECK FLAGGED FIFTEEN FILES AND ELEVEN OF THEM WERE
        /// RIGHT ALL ALONG. `DamageVignette`, `ComicPopup`, `OffscreenIndicators`, `SplashScreen`,
        /// `YouCard`, `RoleSwapCard` and the HUD build canvases that carry no control at all, and
        /// a controller focus path on a damage vignette is a focus path onto nothing. Worse, the
        /// fix it demanded would have been actively wrong: `MenuKit.BuildCanvas` adds a
        /// `GraphicRaycaster`, so routing a full-screen vignette through it would have made a
        /// decorative overlay eat every click on the screen behind it, which is `CLAUDE.md`
        /// § 6.2c's *"anything covering the screen is also eating clicks"* introduced by a check
        /// meant to prevent bugs. **A rule that is too wide gets switched off**, so it is narrowed
        /// to the claim actually worth making: a canvas somebody has to PRESS things on needs a
        /// focus path.
        /// </summary>
        private static readonly Regex ControlBuild = new Regex(
            @"AddComponent\s*<\s*(Button|Toggle|Slider|InputField|Dropdown|Scrollbar|ScrollRect|Selectable|GodotButton)\s*>"
            + @"|MenuKit\.WoodButton"
            + @"|UiRows\.(Row|ButtonRow|FieldRow|StepperRow|DropdownRow|RowButton|Field)"
            + @"|PaperKit\.(Chip|Row|MakeAction)",
            RegexOptions.Compiled);

        /// <summary>Either way of getting a focus path installed.</summary>
        private static readonly Regex FocusInstall =
            new Regex(@"ScreenFocus\.Install|MenuKit\.BuildCanvas", RegexOptions.Compiled);

        /// <summary>
        /// ⚠️ A `ConvertedScreen` SUBCLASS ALREADY HAS ONE AND MUST NOT BE ASKED FOR A SECOND.
        /// `ConvertedScreen.Start` installs `ScreenFocus` on the screen's own GameObject, so a
        /// converted screen that adds a nested canvas for SORTING (which is what
        /// `ConvertedMatchSetup.EnsureCharacterOverlayIsolation` does) is already covered by the
        /// component on its root. Demanding a second one would produce two focus paths over one
        /// control set, which is the fault `ScreenFocus.Owns` exists to prevent.
        /// </summary>
        private static readonly Regex ConvertedSubclass =
            new Regex(@":\s*ConvertedScreen\b", RegexOptions.Compiled);

        // ⚠️ COMMENTS AND STRING LITERALS ARE STRIPPED BEFORE ANYTHING IS MATCHED, AND SKIPPING
        // THAT STEP PRODUCED A FALSE POSITIVE ON THE FIRST RUN. `CustomCharacterScreen`'s header
        // comment contains the words `AddComponent<Canvas>()` while EXPLAINING that the screen
        // deliberately uses `BuildCanvas` instead, so the one file whose documentation proves it
        // is correct was reported as the offender. A check that reads prose as code teaches
        // people to distrust it.
        private static readonly Regex BlockComment = new Regex(@"/\*.*?\*/",
            RegexOptions.Compiled | RegexOptions.Singleline);

        private static readonly Regex LineComment = new Regex(@"//[^\n]*", RegexOptions.Compiled);

        private static readonly Regex StringLiteral =
            new Regex("\"(?:\\\\.|[^\"\\\\])*\"", RegexOptions.Compiled);

        /// <summary>
        /// ⚠️ AND A SECOND, NARROWER RULE: NOBODY ELSE MAY CREATE AN EVENT SYSTEM MODULE.
        /// `StandaloneInputModule` cannot be driven by a pad at all (`UiInputModule`'s note has
        /// the reasoning), so a file that adds one has built a screen a controller is locked out
        /// of, however correct its layout is.
        /// </summary>
        private static readonly Regex LegacyModule =
            new Regex(@"AddComponent\s*<\s*StandaloneInputModule\s*>", RegexOptions.Compiled);

        [MenuItem("Tumbang Preso/Checks/Input surface")]
        public static void RunFromMenu() => Execute(gate: false);

        public static bool Execute(bool gate = true)
        {
            var report = new StringBuilder();
            var faults = new List<string>();

            if (!Directory.Exists(RuntimeRoot))
            {
                Debug.LogError($"[InputSurface] {RuntimeRoot} does not exist.");
                return false;
            }

            foreach (string file in Directory.GetFiles(RuntimeRoot, "*.cs",
                                                       SearchOption.AllDirectories))
            {
                string name = Path.GetFileName(file);
                if (Array.IndexOf(Allowed, name) >= 0) continue;

                string raw = File.ReadAllText(file);
                string relative = file.Replace('\\', '/');

                // Prose is not code. See the note on `BlockComment`.
                string text = StringLiteral.Replace(
                    LineComment.Replace(BlockComment.Replace(raw, ""), ""), "\"\"");

                foreach (Match match in LegacyModule.Matches(text))
                {
                    faults.Add($"{relative}:{LineOf(text, match.Index)}: adds a " +
                               "StandaloneInputModule, which reads the LEGACY input manager and " +
                               "cannot be driven by a gamepad. Use `UiInputModule.Ensure`.");
                }

                if (!CanvasBuild.IsMatch(text)) continue;

                // A canvas with nothing to press is a display. See `ControlBuild`.
                if (!ControlBuild.IsMatch(text)) continue;

                // Already covered, by the kit or by the converted base class.
                if (FocusInstall.IsMatch(text)) continue;
                if (ConvertedSubclass.IsMatch(text)) continue;

                var canvas = CanvasBuild.Match(text);

                faults.Add($"{relative}:{LineOf(text, canvas.Index)}: builds a Canvas that " +
                           "carries controls, and installs no focus path. A gamepad cannot walk " +
                           "this screen and a thumb gets whatever hit area the artwork happens " +
                           "to have. Either build it with `MenuKit.BuildCanvas`, or call " +
                           "`InputLayer.ScreenFocus.Install(go)` and " +
                           "`InputLayer.UiInputModule.Ensure()` on the canvas you built.");
            }

            report.AppendLine($"scanned {RuntimeRoot} for screens built outside the kit");
            report.AppendLine($"allowed to build a Canvas: {string.Join(", ", Allowed)}");
            report.AppendLine();

            if (faults.Count == 0) report.AppendLine("OK: every screen comes through the kit.");
            else foreach (string fault in faults) report.AppendLine("FAULT " + fault);

            Directory.CreateDirectory("Logs");
            File.WriteAllText(OutPath, report.ToString(), new UTF8Encoding(false));

            if (faults.Count == 0)
            {
                Debug.Log($"[InputSurface] OK. See {OutPath}.");
                return true;
            }

            Debug.LogError($"[InputSurface] {faults.Count} screen(s) built outside the kit:\n"
                           + string.Join("\n", faults));

            return !gate;
        }

        private static int LineOf(string text, int index)
        {
            int line = 1;

            for (int i = 0; i < index && i < text.Length; i++)
                if (text[i] == '\n') line++;

            return line;
        }
    }
}
