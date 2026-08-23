using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
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
        /// <remarks>
        /// ⚠️⚠️ `zack` IS THE FIRST ROW OF THE ART REPLACEMENT AND THE ONLY ONE SO FAR. He takes
        /// ATE GIRLIE's index rather than being appended, so no seat moved; see `Roster.cs`.
        /// `docs/Port_Plan.md` section 8.3 says replace one at a time and keep the old mesh until
        /// the new one is measured, so `character-female-b.glb` is still in the repo and still
        /// imports; nothing points at it. Repointing an EXISTING id is what keeps this safe:
        /// `character_index` crosses the wire as a bare int and the mapping here is by id, so a
        /// new model cannot move anybody's seat.
        ///
        /// The replacement is `tools/build_person_voxel.py`, which keeps the CC0 rig's skeleton
        /// and all 32 clips and rebuilds only the mesh. See that file for why the skeleton is
        /// untouchable while the clips are the ones that ship.
        /// </remarks>
        private static readonly Dictionary<string, string> PersonModels = new Dictionary<string, string>
        {
            { "bayan",       "characters/persons/team-bayan.glb" },
            { "maring",      "characters/persons/character-female-f.glb" },
            { "totoy",       "characters/persons/character-male-a.glb" },
            { "inday",       "characters/persons/team-inday.glb" },
            { "kuya_boy",    "characters/persons/team-iggy.glb" },
            { "iggy",        "characters/persons/team-iggy.glb" },
            { "zack",        "characters/persons/team-zack.glb" },
            { "tikboy",      "characters/persons/character-male-c.glb" },
            { "bebang",      "characters/persons/character-female-c.glb" },
            { "jun_jun",     "characters/persons/character-male-d.glb" },
            { "lola_pacing", "characters/persons/character-female-d.glb" },
            { "mang_kanor",  "characters/persons/character-male-e.glb" },
            { "aling_nena",  "characters/persons/character-female-e.glb" },
            { "nemu",        "characters/persons/team-nemu.glb" },
        };

        /// <summary>
        /// ⚠️⚠️ A CHARACTER IS A RIG PLUS A PALETTE, AND THE PALETTE IS HALF OF WHO THEY ARE.
        /// Twelve people share twelve CC0 rigs and differ only by which sixteen colours their
        /// shared atlas is remapped to. Without them the whole cast renders in Kenney's factory
        /// colours: `bayan` and `totoy` are the same man in the same clothes, and the select
        /// screen still shows the right name and the right meters over the top of it.
        ///
        /// ⚠️ THE ORDER OF THIS TABLE DOES NOT MATTER AND THE IDS DO. Matched by id exactly as
        /// the model table above is, because index order is a network contract owned by
        /// `Roster.cs`.
        /// </summary>
        private static readonly Dictionary<string, string> PersonPalettes = new Dictionary<string, string>
        {
            { "bayan",       "person_team-bayan.tres" },
            { "maring",      "person_b.tres" },
            { "totoy",       "person_totoy.tres" },
            { "inday",       "person_team-inday.tres" },
            { "kuya_boy",    "person_team-iggy.tres" },
            { "iggy",        "person_team-iggy.tres" },
            // ⚠️ THE ONE PALETTE IN THIS TABLE THAT IS NOT A COPY OF A GODOT FILE. The other
            // eleven are carried over from `generate_person_palettes.py` in the Godot repo and
            // must not be hand-edited there or here. This one is emitted by
            // `tools/build_person_voxel.py`, in the same run and from the same table that lays
            // out the model's UVs, because a slot number in the mesh and a colour in the palette
            // are two halves of one decision: separate them and a renumbered slot silently
            // repaints a limb with nothing failing.
            { "zack",        "person_team-zack.tres" },
            { "tikboy",      "person_tikboy.tres" },
            { "bebang",      "person_bebang.tres" },
            { "jun_jun",     "person_jun-jun.tres" },
            { "lola_pacing", "person_lola-pacing.tres" },
            { "mang_kanor",  "person_mang-kanor.tres" },
            { "aling_nena",  "person_aling-nena.tres" },
            { "nemu",        "person_team-nemu.tres" },
        };

        private static readonly Dictionary<string, string> PersonPets = new Dictionary<string, string>
        {
            { "nemu", "characters/pets/pet-nemu-ghost.glb" },
        };

        private const string PaletteDir = "MapSource/materials_persons";

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

            // Also build standalone entries like nemu so they have valid .asset files
            foreach (var kvp in PersonModels)
            {
                bool inTruth = false;
                foreach (var p in Roster.People) { if (p.Id == kvp.Key) { inTruth = true; break; } }
                if (!inTruth) BuildSingleEntry(kvp.Key, PersonModels, "person", ref ok);
            }

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

        /// <summary>
        /// The sixteen colours out of a Godot `.tres`, or null for anything that has none.
        ///
        /// ⚠️⚠️ PARSED FROM THE SOURCE, NOT TRANSCRIBED. Twelve times sixteen is 192 numbers,
        /// and a table typed by hand is one that silently drifts from the build it is meant to
        /// match. The `.tres` files are copied into `MapSource/` for the same reason the maps
        /// are: this repo converts the Godot output rather than reimplementing it.
        ///
        /// ⚠️ AND A SHORT ARRAY IS A FAILURE, NOT A PARTIAL SUCCESS. The shader indexes sixteen
        /// slots; fifteen colours would read whatever is past the end for one of them, and the
        /// slot most likely to be missing is 15, which is the lit skin tone on every Person.
        /// </summary>
        private static Color[] ReadPalette(string id, ref bool ok)
        {
            if (!PersonPalettes.TryGetValue(id, out var file)) return null;

            string path = Path.Combine(PaletteDir, file);

            if (!File.Exists(path))
            {
                Debug.LogError($"[RosterBook] no palette at {path} for '{id}'. Copy the .tres " +
                               "out of the Godot repo; that character will wear stock colours.");
                ok = false;
                return null;
            }

            var match = Regex.Match(File.ReadAllText(path),
                                    @"shader_parameter/palette\s*=\s*PackedColorArray\(([^)]*)\)");

            if (!match.Success)
            {
                Debug.LogError($"[RosterBook] {file} carries no palette array.");
                ok = false;
                return null;
            }

            var parts = match.Groups[1].Value.Split(',');
            var colours = new List<Color>();

            for (int i = 0; i + 3 < parts.Length; i += 4)
            {
                colours.Add(new Color(F(parts[i]), F(parts[i + 1]), F(parts[i + 2]), F(parts[i + 3])));
            }

            if (colours.Count != 16)
            {
                Debug.LogError($"[RosterBook] {file} has {colours.Count} palette entries, not 16.");
                ok = false;
                return null;
            }

            return colours.ToArray();
        }

        private static float F(string s) =>
            float.TryParse(s.Trim(), System.Globalization.NumberStyles.Float,
                           System.Globalization.CultureInfo.InvariantCulture, out float v) ? v : 0.0f;

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
                asset.Palette = ReadPalette(entry.Id, ref ok);

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

                if (PersonPets.TryGetValue(entry.Id, out var petRel))
                {
                    string petFull = $"{ArtRoot}/{petRel}";
                    asset.PetModel = AssetDatabase.LoadAssetAtPath<GameObject>(petFull);
                }
                else
                {
                    asset.PetModel = null;
                }

                EditorUtility.SetDirty(asset);
                into.Add(asset);
            }

            return ok;
        }

        private static bool BuildSingleEntry(string id,
                                             Dictionary<string, string> models,
                                             string kind,
                                             ref bool ok)
        {
            string assetPath = $"{EntryDir}/{kind}_{id}.asset";

            var asset = AssetDatabase.LoadAssetAtPath<RosterEntryAsset>(assetPath);
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<RosterEntryAsset>();
                AssetDatabase.CreateAsset(asset, assetPath);
            }

            asset.Id = id;
            asset.Tint = Color.white;
            asset.Palette = ReadPalette(id, ref ok);

            if (models.TryGetValue(id, out var rel))
            {
                string full = $"{ArtRoot}/{rel}";
                asset.Model = AssetDatabase.LoadAssetAtPath<GameObject>(full);

                if (asset.Model == null)
                {
                    Debug.LogError($"[RosterBook] no model at {full} for '{id}'.");
                    ok = false;
                }

                var clips = new System.Collections.Generic.List<AnimationClip>();
                foreach (var sub in AssetDatabase.LoadAllAssetsAtPath(full))
                {
                    if (sub is AnimationClip clip && !clip.name.StartsWith("__preview"))
                        clips.Add(clip);
                }

                asset.Clips = clips.ToArray();

                if (clips.Count == 0 && kind == "person")
                {
                    Debug.LogError($"[RosterBook] '{id}' has a model with no clips. " +
                                   "That character will not animate.");
                    ok = false;
                }
            }
            else
            {
                Debug.LogError($"[RosterBook] no model mapped for {kind} '{id}'.");
                ok = false;
            }

            EditorUtility.SetDirty(asset);
            return ok;
        }
    }
}
