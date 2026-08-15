using System.Collections.Generic;
using System.IO;
using TumbangPreso;
using TumbangPreso.Core;
using UnityEditor;
using UnityEngine;

namespace TumbangPreso.EditorTools
{
    /// <summary>
    /// Builds the <see cref="RosterBook"/> asset that maps every roster id to its model.
    ///
    /// ⚠️⚠️ THE MAPPING IS BY ID, NEVER BY LIST POSITION. `character_index`, `can_index` and
    /// `slipper_index` all cross the wire as bare ints, so index order is a network contract.
    /// Resolving art by id means dragging assets around in the Inspector cannot change who is
    /// sitting in seat 3, and it means adding a model cannot silently repaint the cast.
    ///
    /// ⚠️ THE TRAITS ARE NOT COPIED HERE AND MUST NEVER BE. They live in `Roster.cs` where they
    /// are unit tested. A designer-editable copy on a ScriptableObject is the most tempting
    /// change in this whole port and it would end the verification strategy, because the tests
    /// would be asserting something other than what ships.
    ///
    /// Run:
    ///   Unity.exe -batchmode -quit -nographics -projectPath . \
    ///             -executeMethod TumbangPreso.EditorTools.RosterBookBuilder.Build
    /// </summary>
    public static class RosterBookBuilder
    {
        private const string ArtRoot = "Assets/TumbangPreso/Art";
        private const string EntryDir = "Assets/TumbangPreso/Resources/Roster";
        private const string BookPath = "Assets/TumbangPreso/Resources/RosterBook.asset";

        /// <summary>
        /// ⚠️ A CHARACTER IS A RIG PLUS A PALETTE, NOT A NEW MODEL. The twelve people wear
        /// twelve CC0 rigs and differ by their palette, so this table is the id-to-rig pairing
        /// exactly as the Godot roster declares it. Get one wrong and two characters render as
        /// the same person, which is invisible on the select screen because the meters still
        /// look right.
        /// </summary>
        private static readonly Dictionary<string, string> PersonModels = new Dictionary<string, string>
        {
            { "berto",       "characters/persons/character-male-f.glb" },
            { "maring",      "characters/persons/character-female-f.glb" },
            { "totoy",       "characters/persons/character-male-a.glb" },
            { "inday",       "characters/persons/character-female-a.glb" },
            { "kuya_boy",    "characters/persons/character-male-b.glb" },
            { "ate_girlie",  "characters/persons/character-female-b.glb" },
            { "tikboy",      "characters/persons/character-male-c.glb" },
            { "bebang",      "characters/persons/character-female-c.glb" },
            { "jun_jun",     "characters/persons/character-male-d.glb" },
            { "lola_pacing", "characters/persons/character-female-d.glb" },
            { "mang_kanor",  "characters/persons/character-male-e.glb" },
            { "aling_nena",  "characters/persons/character-female-e.glb" },
        };

        private static readonly Dictionary<string, string> CanModels = new Dictionary<string, string>
        {
            { "pasip",   "models/lata_pasip.obj" },
            { "boyben",  "models/lata_boyben.obj" },
            { "decades", "models/lata_decades.obj" },
            { "metal",   "models/lata_metal.obj" },
        };

        private static readonly Dictionary<string, string> SlipperModels = new Dictionary<string, string>
        {
            { "tsinelas", "models/tsinelas_classic.obj" },
            { "crocs",    "models/tsinelas_crocs.obj" },
            { "pantulog", "models/tsinelas_pantulog.obj" },
            { "sike",     "models/tsinelas_sike.obj" },
        };

        [MenuItem("Tumbang Preso/Build Roster Book")]
        public static void BuildFromMenu() => Execute();

        public static void Build() => EditorApplication.Exit(Execute() ? 0 : 1);

        private static bool Execute()
        {
            Directory.CreateDirectory(EntryDir);

            var book = AssetDatabase.LoadAssetAtPath<RosterBook>(BookPath);
            if (book == null)
            {
                book = ScriptableObject.CreateInstance<RosterBook>();
                AssetDatabase.CreateAsset(book, BookPath);
            }

            book.People.Clear();
            book.Cans.Clear();
            book.Slippers.Clear();

            bool ok = true;
            ok &= Fill(book.People, Roster.People, PersonModels, "person");
            ok &= Fill(book.Cans, Roster.Cans, CanModels, "can");
            ok &= Fill(book.Slippers, Roster.Slippers, SlipperModels, "slipper");

            EditorUtility.SetDirty(book);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            if (book.Validate(out string report))
            {
                Debug.Log($"[RosterBook] OK. {book.People.Count} people, {book.Cans.Count} cans, " +
                          $"{book.Slippers.Count} slippers.\n{report}");
            }
            else
            {
                Debug.LogError($"[RosterBook] validation failed:\n{report}");
                ok = false;
            }

            return ok;
        }

        private static bool Fill(List<RosterEntryAsset> into,
                                 IReadOnlyList<RosterEntry> truth,
                                 Dictionary<string, string> models,
                                 string kind)
        {
            bool ok = true;

            foreach (var entry in truth)
            {
                string assetPath = $"{EntryDir}/{kind}_{entry.Id}.asset";

                var asset = AssetDatabase.LoadAssetAtPath<RosterEntryAsset>(assetPath);
                if (asset == null)
                {
                    asset = ScriptableObject.CreateInstance<RosterEntryAsset>();
                    AssetDatabase.CreateAsset(asset, assetPath);
                }

                asset.Id = entry.Id;
                asset.Tint = Color.white;

                if (models.TryGetValue(entry.Id, out var rel))
                {
                    string full = $"{ArtRoot}/{rel}";
                    asset.Model = AssetDatabase.LoadAssetAtPath<GameObject>(full);

                    if (asset.Model == null)
                    {
                        Debug.LogError($"[RosterBook] no model at {full} for '{entry.Id}'.");
                        ok = false;
                    }

                    // ⚠️⚠️ THE CLIPS ARE REFERENCED HERE OR THEY DO NOT SHIP. They are
                    // sub-assets of the `.glb`, and an asset nothing points at is stripped from
                    // the player. Nothing pointed at them: the animator looked them up through
                    // the AssetDatabase, which does not exist in a build, so every character in
                    // every build stood still. See RosterEntryAsset.Clips.
                    var clips = new System.Collections.Generic.List<AnimationClip>();

                    foreach (var sub in AssetDatabase.LoadAllAssetsAtPath(full))
                    {
                        if (sub is AnimationClip clip && !clip.name.StartsWith("__preview"))
                            clips.Add(clip);
                    }

                    asset.Clips = clips.ToArray();

                    if (clips.Count == 0 && kind == "person")
                    {
                        Debug.LogError($"[RosterBook] '{entry.Id}' has a model with no clips. " +
                                       "That character will not animate.");
                        ok = false;
                    }
                }
                else
                {
                    Debug.LogError($"[RosterBook] no model mapped for {kind} '{entry.Id}'.");
                    ok = false;
                }

                EditorUtility.SetDirty(asset);
                into.Add(asset);
            }

            return ok;
        }
    }
}
