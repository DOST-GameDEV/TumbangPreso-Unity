using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;

namespace TumbangPreso.Tests
{
    /// <summary>
    /// Every `FindActionMap` and `FindAction` in the runtime names something that exists.
    ///
    /// ⚠️⚠️ IT EXISTS BECAUSE A MISSPELLED MAP NAME IS INVISIBLE AT EVERY OTHER GATE. 🧑
    /// 2026-08-29: *"r skip doesnt work too"*. `BufferSkipVote.Awake` asked for a map called
    /// `Gameplay`; the asset has exactly one map and it is called `Player`. It compiled, it ran,
    /// it threw nothing, and it produced a component whose input action was null for its entire
    /// lifetime — so `Update`'s own null guard swallowed every press. The vote it was written to
    /// collect could never be cast by anybody, and every screenshot of it looked correct because
    /// the HUD row it draws is fed from the tally rather than from the key.
    ///
    /// ⚠️ THE SILENCE IS BUILT IN BY DESIGN: `throwIfNotFound: false` is the right argument for
    /// a resolver that must not crash a match, and `PlayerInputReader` is the single caller that
    /// passes true. So the language cannot help here and a text search over the runtime can,
    /// which is the same argument `DeadFeatureAudit`'s header makes at length.
    ///
    /// ⚠️ IT SEARCHES THE SOURCE RATHER THAN THE ASSEMBLY, for that file's reason: the fault is
    /// a STRING, and a string that is never reached is not reachable by reflection either.
    /// </summary>
    public class InputResolverTests
    {
        private const string RuntimeRoot = "Assets/TumbangPreso/Runtime";
        private const string AssetName = "TumbangPreso";

        private static readonly Regex MapCall =
            new Regex(@"FindActionMap\(\s*""([^""]+)""", RegexOptions.Compiled);

        private static readonly Regex ActionCall =
            new Regex(@"FindAction\(\s*""([^""]+)""", RegexOptions.Compiled);

        [Test]
        public void EveryActionMapNamedInTheRuntimeExists()
        {
            var asset = Resources.Load<InputActionAsset>(AssetName);
            Assert.IsNotNull(asset, $"Resources/{AssetName} did not load, so nothing below is a test.");

            var missing = new List<string>();

            foreach (string path in Directory.GetFiles(RuntimeRoot, "*.cs",
                                                       SearchOption.AllDirectories))
            {
                string source = File.ReadAllText(path);

                foreach (Match m in MapCall.Matches(source))
                {
                    string name = m.Groups[1].Value;
                    if (asset.FindActionMap(name, false) == null)
                        missing.Add($"{Path.GetFileName(path)} asks for action map \"{name}\"");
                }
            }

            Assert.IsEmpty(missing,
                "These resolvers name an action map the asset does not have. Each one is a "
                + "control that silently does nothing:\n  " + string.Join("\n  ", missing));
        }

        [Test]
        public void EveryActionNamedInTheRuntimeExistsInSomeMap()
        {
            var asset = Resources.Load<InputActionAsset>(AssetName);
            Assert.IsNotNull(asset);

            var missing = new List<string>();

            foreach (string path in Directory.GetFiles(RuntimeRoot, "*.cs",
                                                       SearchOption.AllDirectories))
            {
                string source = File.ReadAllText(path);

                foreach (Match m in ActionCall.Matches(source))
                {
                    string name = m.Groups[1].Value;

                    // ⚠️ ANY MAP, NOT THE ONE ON THE SAME LINE. The call sites chain
                    // `FindActionMap(...)?.FindAction(...)` across line breaks and through local
                    // variables, and pairing them properly would mean parsing C# to catch a
                    // typo. Asking whether the action exists anywhere in the asset catches the
                    // misspelling, which is the whole failure mode, and the map test above
                    // catches the other half.
                    bool found = false;
                    foreach (var map in asset.actionMaps)
                        if (map.FindAction(name, false) != null) { found = true; break; }

                    if (!found)
                        missing.Add($"{Path.GetFileName(path)} asks for action \"{name}\"");
                }
            }

            Assert.IsEmpty(missing,
                "These resolvers name an action that is in no map:\n  "
                + string.Join("\n  ", missing));
        }

        /// <summary>
        /// The one the report was actually about, asserted by name so a regression reads as
        /// itself rather than as a list.
        /// </summary>
        [Test]
        public void TheSkipVoteResolvesTheReadyKey()
        {
            var asset = Resources.Load<InputActionAsset>(AssetName);
            var map = asset != null ? asset.FindActionMap("Player", false) : null;

            Assert.IsNotNull(map, "the Player map is what every other resolver in the game uses");
            Assert.IsNotNull(map.FindAction("ReadyUp", false),
                             "R is the skip vote and the pre-round ready in one binding");

            string source = File.ReadAllText(Path.Combine(RuntimeRoot, "BufferSkipVote.cs"));
            StringAssert.Contains("FindActionMap(\"Player\"", source,
                "BufferSkipVote asked for \"Gameplay\", which does not exist, and the press was "
                + "swallowed by its own null guard.");
        }
    }
}
