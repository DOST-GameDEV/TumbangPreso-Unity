using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using TumbangPreso.Visual;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace TumbangPreso.EditorTools.MapKit
{
    /// <summary>
    /// THE ILALIM REBUILD'S SIDEWALK LIFE (owner 2026-09-30: "could we have some events along the
    /// side walks like kids chasing each other around, some taho vendor, people stopping by to
    /// watch, maybe some beggar that comes sits down that you can interact with to donate to..
    /// just make them match the style of the game"). Called by IlalimLifeAuthor.Build, so every
    /// rebuild reproduces it; the runtime is <see cref="SidewalkLife"/>.
    ///
    /// ⚠️⚠️ THE PEOPLE ARE THE CAST'S OWN PIPELINE, NOT NEW ART: a Classic rig from the RosterBook
    /// (the twelve CC0 Kenney rigs) with an everyday palette of its own. Only the garment slots are
    /// rewritten, MEASURED per rig from the .glb's UVs and heights (torso 0.18 to 0.37, legs 0.03
    /// to 0.18 of the 0.7234 rig): skin, hair and slot 8 (the face) stay the rig's. No colour is
    /// near the offence orange #f87020 or the defence blue #0080e8 (checked below). Kids are the
    /// same rigs at 0.70 to 0.74 of the cast's scale. ⚠️ EXCEPT THE BEGGAR, who has his own
    /// voxel model since 2026-09-30 (see <see cref="BeggarOption"/>): a street beggar must not
    /// read as a playable cast member.
    ///
    /// ⚠️ THE ROUTES ARE MEASURED, NOT TRUSTED. Every route is sampled every 0.25 m at the lateral
    /// offsets the walkers use, and each sample is checked against the art (temporary MeshColliders
    /// on the dressing, removed afterwards): the ground under it must be the street kit's ground
    /// (pavement, lawn, kerb ramp), nothing solid may stand in a 0.22 m capsule from 0.35 to 1.6 m,
    /// no small prop's footprint may cover it, it must lie outside the play area (|x| 11.2, |z| 16.7)
    /// and at least 2 m from every live traffic route's centre line (a bus is 2.5 m wide). The build log's "Sidewalk"
    /// lines count the failures by cause. <see cref="Probe"/> then steps the life for 300 s without
    /// PlayMode beside the traffic and renders each event (sidewalk_*.png).
    /// </summary>
    internal static class IlalimSidewalkAuthor
    {
        private const string Tag = "[IlalimRebuild] ";
        private const string Folder = "Assets/TumbangPreso/Art/IlalimRebuild/Life";
        private const float PlayX = 11.2f, PlayZ = 16.7f, BoxX = 7f, BoxZ = 16.5f;
        private const float WalkLateral = .35f, KidHalfWidth = .7f, TrafficClear = 2f;

        // ------------------------------------------------------------------ where (game metres, x and z)

        /// <summary>Each route's FIRST point is its far end (where a person appears and vanishes).
        ///   * the south-west Taft pavement (PGH side), toward the court's south wall: the
        ///     magtataho's beat and the beggar's way to his spot;
        ///   * Padre Faura's south sidewalk from the west, round the corner onto the north-west
        ///     Taft pavement, to the court's north wall (two spots);
        ///   * Padre Faura's south sidewalk from the east (the Astral podium), round the corner onto
        ///     the north-east Taft pavement, to the north wall (two spots);
        ///   * across the PGH parking lot to its fence behind the west wall (three spots), clear of
        ///     the two vans parked there at x -18.5 to -14.5.
        /// Pre-checked against a 0.25 m ray scan of ArtSource/ilalim/ilalim_city.blend: the NW corner
        /// keeps inside the kerb's curve (the road starts at z 26.5 by x -14) and the fence planters
        /// at x -10.6, then measured again here in Unity (the build log's "Sidewalk" lines).</summary>
        private static readonly (string name, Vector2[] points, float[] width)[] WalkPlan =
        {
            ("SW Taft, the magtataho", new[] { V(-8.5f, -46f), V(-8.5f, -21f) }, null),
            ("SW Taft, the beggar", new[] { V(-8.5f, -46f), V(-8.5f, -19.6f), V(-10.2f, -18.05f) }, null),
            ("NW corner", NorthWest(V(-9.2f, 17.7f)), NorthWestWidth()),
            ("NW corner, beside", NorthWest(V(-7.9f, 18.3f)), NorthWestWidth()),
            ("NE corner", new[] { V(23f, 25.3f), V(10f, 25.3f), V(8.5f, 24.2f), V(8.4f, 17.9f) }, null),
            ("NE corner, beside", new[] { V(23f, 25.3f), V(10f, 25.3f), V(8.5f, 24.2f), V(8.3f, 20.5f), V(8f, 19.4f) }, null),
            ("PGH lot, fence A", new[] { V(-42f, 3.5f), V(-26f, .6f), V(-15f, .4f), V(-12.3f, 1f) }, null),
            ("PGH lot, fence B", new[] { V(-42f, 3.5f), V(-26f, 1.2f), V(-13f, -.4f), V(-12.4f, -1.2f) }, null),
            ("PGH lot, fence C", new[] { V(-42f, 5.8f), V(-24f, 5.8f), V(-12.3f, 6.2f) }, null),
        };

        /// <summary>Padre Faura's south sidewalk from the west (from x -30: the lamp at -34.25 and
        /// the pole at -26 stand on its two edges, Padre Faura's through lane 2.3 m out), past the
        /// ONE WAY post at (-16.52, 25.57), then the one way round the corner, MEASURED in Unity:
        /// single file (the keep-right offset closes to nothing) through the half-metre gaps
        /// between the tx pole (-10.89, 24.63) and the street blade post (-10.79, 25.63), then
        /// between the fence planter and the closure barrier's west end, and down the north-west
        /// Taft pavement.</summary>
        private static Vector2[] NorthWest(Vector2 end) => new[]
        {
            V(-30f, 26.55f), V(-24f, 26.55f), V(-20f, 26.4f), V(-16.5f, 26.3f), V(-13f, 25.35f), V(-10.84f, 25.13f),
            V(-10.05f, 25f), V(-9.75f, 24.2f), V(-9.45f, 23.2f), V(-9.2f, 21f), end,
        };
        // A method, not a field: WalkPlan is initialised first and would read a static field as null.
        private static float[] NorthWestWidth() => new[] { 1f, 1f, 1f, 1f, .3f, 0f, .15f, .25f, .8f, 1f, 1f };
        private const int TahoWalk = 0, BeggarWalk = 1;
        private static readonly (string name, int walk)[] WatchPlan =
        {
            ("the north-west corner", 2), ("the north-west corner", 3), ("the north-east corner", 4), ("the north-east corner", 5),
            ("the PGH fence", 6), ("the PGH fence", 7), ("the PGH fence", 8),
        };
        /// <summary>The kids' pavement: the south-east Taft pavement below the shops, 4.5 m and more
        /// past the south wall; its last point is where they come and go.</summary>
        private static readonly Vector2[] KidPlan = { V(8.5f, -21.2f), V(8.5f, -36.5f) };
        /// <summary>The beggar's spot: on the pavement against the PGH fence, 1.55 m past the south
        /// wall, between the street pole (-10.58, -17.4) and the RABIES tarp on the fence (z -20.6).</summary>
        private static readonly Vector2 Seat = V(-10.2f, -18.05f);
        private static readonly Vector3 Facing = new Vector3(.9f, 0f, .45f);
        private const float Reach = 2.6f;

        private static Vector2 V(float x, float z) => new Vector2(x, z);

        // ------------------------------------------------------------------ who

        /// <summary>Role, roster rig, scale, and the garment slots rewritten (slot, sRGB hex). The
        /// slots are the rig's measured torso/leg/foot cells; see the class note.</summary>
        private static readonly (string role, string id, float scale, (int slot, string hex)[] dress)[] Cast =
        {
            // The magtataho: character-male-b, an off-white shirt (slot 4) and olive shorts (5).
            ("taho", "kuya_boy", 1f, new[] { (4, "e6dfcc"), (5, "6b6444") }),
            // The beggar: character-male-e, a faded brown-grey shirt (2) over his dark trousers.
            ("beggar", "mang_kanor", 1f, new[] { (2, "6d6250") }),
            // Kids: male-a in a yellow shirt, maroon shorts, green tsinelas, his orange headband
            // (slot 11, too close to the offence orange) off-white; female-c in pink and plum;
            // male-c in a maroon shirt and cap, his red-orange trim (2) dark green.
            ("kid", "totoy", .72f, new[] { (1, "e3c84a"), (5, "7a3b3b"), (2, "4f8a4a"), (11, "efe9dc") }),
            ("kid", "bebang", .70f, new[] { (5, "e58fae"), (9, "6a3d52") }),
            ("kid", "tikboy", .74f, new[] { (5, "8a3446"), (2, "3f6a3a") }),
            // Passers-by: female-f in a sage blouse and brown trousers; male-d in his dark suit with
            // the tie (4) muted and his hair (slot 13 on this rig, a light orange) dark; female-e in
            // a cream top (her orange, slot 5, replaced) and a maroon dress (11).
            ("spectator", "maring", 1f, new[] { (2, "7d9a6a"), (5, "5a4636"), (7, "4a4038") }),
            ("spectator", "jun_jun", 1f, new[] { (4, "b8a57a"), (13, "3a2a22") }),
            ("spectator", "aling_nena", 1f, new[] { (5, "e3d6bb"), (11, "7a3446") }),
        };

        // ------------------------------------------------------------------ the beggar's look and the taho's carry

        /// <summary>
        /// THE BEGGAR IS HIS OWN MODEL NOW (owner 2026-09-30, on the beggar who came out as Mang
        /// Kanor: "use a different model to make him look more like a beggar, and better textured
        /// with dirt and stuff whil still maintainiing artstyle"). D is the default: npc-beggar.glb,
        /// built by `tools/build_beggar_voxel.py` (the cast's voxel person pipeline on
        /// character-male-e's own skeleton, so every clip SidewalkLife plays still works; a thin
        /// older man with greying messy hair and a short grey beard, a sun-faded torn shirt,
        /// patched rolled trousers, dusty bare feet on worn tsinelas; his clothes are painted
        /// with drawn dirt on his own atlas). His RosterEntryAsset (npc-beggar.asset, NOT in the
        /// roster) is written here by <see cref="OwnBeggar"/>. The earlier looks stay selectable:
        ///   * A: character-male-e (Mang Kanor's rig) in a faded shirt, the first committed look.
        ///   * B: character-male-f (Bayan's rig): a faded navy cap over the hair, a bimpo over the
        ///     left shoulder, a washed-out shirt and dark worn trousers.
        ///   * C: character-male-d (Jun Jun's rig): grey hair under a boxy buri straw hat, grey
        ///     stubble, a dark worn jacket over a faded shirt, the tie gone.
        /// THE TAHO CARRIES ON HIS SHOULDER (owner 2026-09-30: "taho pole use option B on
        /// shoulder"); Waist is the old look (<see cref="SidewalkLife.TahoCarryStyle"/>).
        /// <see cref="RunOptions"/> still renders A to C into Logs/ilalim-unity/options_v1;
        /// `IlalimSidewalkFilm` renders the new beggar's stills and the events' videos.
        /// </summary>
        internal enum BeggarOption { A_MangKanorRig, B_CapAndBimpo, C_StrawHatGrey, D_OwnModel }
        internal static readonly BeggarOption BeggarChoice = BeggarOption.D_OwnModel;
        internal static readonly SidewalkLife.TahoCarryStyle TahoChoice = SidewalkLife.TahoCarryStyle.Shoulder;

        internal const string BeggarModelPath = Folder + "/npc-beggar.glb";
        private const string BeggarEntryPath = Folder + "/npc-beggar.asset";
        private const string BeggarPalettePath = Folder + "/npc-beggar-palette.json";

        [Serializable] private sealed class PaletteFile { public string[] palette; }

        /// <summary>The beggar's own model as a Look: a RosterEntryAsset outside the roster that
        /// references the .glb, its clips (so they ship, see RosterEntryAsset.Clips) and the
        /// sixteen flat colours the builder wrote beside it.</summary>
        private static SidewalkLife.Look OwnBeggar()
        {
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(BeggarModelPath);
            if (model == null) { Debug.LogWarning(Tag + "Sidewalk: no beggar model at " + BeggarModelPath + " (run tools/build_beggar_voxel.py)"); return null; }
            var entry = AssetDatabase.LoadAssetAtPath<RosterEntryAsset>(BeggarEntryPath);
            if (entry == null) { entry = ScriptableObject.CreateInstance<RosterEntryAsset>(); AssetDatabase.CreateAsset(entry, BeggarEntryPath); }
            entry.Id = "npc_beggar";
            entry.Model = model;
            entry.Tint = Color.white;
            entry.Clips = AssetDatabase.LoadAllAssetsAtPath(BeggarModelPath).OfType<AnimationClip>().Where(c => !c.name.StartsWith("__preview")).ToArray();
            var file = File.Exists(BeggarPalettePath) ? JsonUtility.FromJson<PaletteFile>(File.ReadAllText(BeggarPalettePath)) : null;
            if (file?.palette != null && file.palette.Length == 16) entry.Palette = file.palette.Select(Hex).ToArray();
            else Debug.LogWarning(Tag + "Sidewalk: the beggar's palette is missing or not 16 colours: " + BeggarPalettePath);
            EditorUtility.SetDirty(entry);
            AssetDatabase.SaveAssets();
            return new SidewalkLife.Look { Name = "beggar (npc-beggar)", Art = entry, Palette = entry.Palette, Scale = 1f };
        }

        internal static SidewalkLife.Look BeggarLook(RosterBook book, BeggarOption option)
        {
            switch (option)
            {
                case BeggarOption.D_OwnModel:
                    return OwnBeggar();
                case BeggarOption.B_CapAndBimpo:
                    // male-f: slot 1 shirt (torso and sleeves), 9 trousers; the hair is slot 8 (ink) and stays.
                    return Dressed(book, "beggar", "bayan", 1f, new[] { (1, "b3ac98"), (9, "54503f") }, new SidewalkLife.Wear
                    {
                        Hat = SidewalkLife.HatKind.Cap, HairTop = .671f, HatColour = Hex("3e4756"), HatTrim = Hex("343a46"),
                        Towel = true, TowelColour = Hex("e9e3d2"), TowelStripe = Hex("7d8c6a"),
                    });
                case BeggarOption.C_StrawHatGrey:
                    // male-d: slot 8 is his whole suit AND the face's ink, so it stays dark (a worn
                    // dark jacket); 12 the shirt front, 4 the tie (into the jacket), 13 the hair, grey.
                    return Dressed(book, "beggar", "jun_jun", 1f, new[] { (8, "35302a"), (12, "a39a82"), (4, "35302a"), (13, "aeaaa2") }, new SidewalkLife.Wear
                    {
                        Hat = SidewalkLife.HatKind.StrawHat, HairTop = .722f, HatColour = Hex("cdb57c"), HatTrim = Hex("5e4a32"),
                        Stubble = true, StubbleColour = Hex("8f8a82"),
                    });
                default:
                    var a = Cast.First(c => c.role == "beggar");
                    return Dressed(book, a.role, a.id, a.scale, a.dress, null);
            }
        }

        private static SidewalkLife.Look Dressed(RosterBook book, string role, string id, float scale, (int slot, string hex)[] dress, SidewalkLife.Wear wear)
        {
            var art = book != null ? book.FindPersonArt(id) : null;
            if (art == null || art.Model == null) { Debug.LogWarning(Tag + "Sidewalk: no roster art for " + id); return null; }
            var palette = (art.Palette != null && art.Palette.Length == 16 ? art.Palette : new Color[16]).ToArray();
            foreach (var (slot, hex) in dress)
            {
                var c = Hex(hex);
                if (Near(c, Offence) || Near(c, Defence)) Debug.LogWarning($"{Tag}Sidewalk: {id} slot {slot} #{hex} is near a role hue");
                palette[slot] = c;
            }
            var look = new SidewalkLife.Look { Name = $"{role} ({id})", Art = art, Palette = palette, Scale = scale };
            if (wear != null) look.Wear = wear;
            return look;
        }

        /// <summary>Batch: renders every option from the v12 cameras into
        /// Logs/ilalim-unity/options_v1 (beggar_X_player/side, taho_X_close/court, and review
        /// views under checks/). Opens the saved scene per option and NEVER saves it.</summary>
        public static void RunOptions()
        {
            try
            {
                const string folder = "Logs/ilalim-unity/options_v1";
                Directory.CreateDirectory(Path.Combine(folder, "checks"));
                var book = RosterBook.Load();
                var runs = new (string beggar, BeggarOption b, string taho, SidewalkLife.TahoCarryStyle t)[]
                {
                    ("A", BeggarOption.A_MangKanorRig, "A", SidewalkLife.TahoCarryStyle.Waist),
                    ("B", BeggarOption.B_CapAndBimpo, "B", SidewalkLife.TahoCarryStyle.Shoulder),
                    ("C", BeggarOption.C_StrawHatGrey, null, SidewalkLife.TahoCarryStyle.Shoulder),
                };
                foreach (var run in runs)
                {
                    var rename = new Dictionary<string, string>
                    {
                        ["sidewalk_beggar_player"] = $"beggar_{run.beggar}_player",
                        ["sidewalk_beggar_side"] = $"beggar_{run.beggar}_side",
                        ["check_beggar_front"] = $"checks/beggar_{run.beggar}_front",
                    };
                    if (run.taho != null)
                    {
                        rename["sidewalk_taho_close"] = $"taho_{run.taho}_close";
                        rename["sidewalk_taho_court"] = $"taho_{run.taho}_court";
                        foreach (var view in new[] { "side", "front", "back" }) rename["check_taho_" + view] = $"checks/taho_{run.taho}_{view}";
                    }
                    var look = BeggarLook(book, run.b);
                    Probe(folder, IlalimSceneBuilder.ScenePath, life =>
                    {
                        if (look != null) life.Beggar = look;
                        life.TahoCarry = run.t;
                    }, rename, $"checks/probe_{run.beggar}.txt");
                }
            }
            catch (Exception e) { Debug.LogError(Tag + "OPTIONS FAILED: " + e); EditorApplication.Exit(1); return; }
            EditorApplication.Exit(0);
        }

        private static readonly (string name, string hex, float gloss)[] PropColours =
        {
            ("sidewalk_bamboo", "c9ae6e", .1f), ("sidewalk_aluminium", "c3c8cc", .45f), ("sidewalk_lid", "9aa1a8", .4f),
            ("sidewalk_rope", "8a7a5a", .05f), ("sidewalk_tin", "b5b0a4", .35f), ("sidewalk_carton", "b8956a", .05f),
            ("sidewalk_coin", "e3c04a", .6f),
            // The beggar's things: a faded plum-brown cloth bundle, its darker knot, a white plastic bag.
            ("sidewalk_bundle", "7d5c50", .05f), ("sidewalk_bundle_knot", "5e463d", .05f), ("sidewalk_bag", "dedcd3", .3f),
        };

        private static readonly Color Offence = Hex("f87020"), Defence = Hex("0080e8");

        private static Color Hex(string hex)
        {
            ColorUtility.TryParseHtmlString("#" + hex, out var c);
            return c;
        }

        // ------------------------------------------------------------------ build

        public static void Build(Transform root, Transform dressing, StringBuilder report)
        {
            var go = new GameObject("SidewalkLife");
            go.transform.SetParent(root, false);
            var life = go.AddComponent<SidewalkLife>();
            var book = RosterBook.Load();
            var looks = new List<(string role, SidewalkLife.Look look)>();
            var cast = new StringBuilder();
            foreach (var (role, id, scale, dress) in Cast)
            {
                var art = book != null ? book.FindPersonArt(id) : null;
                if (art == null || art.Model == null) { Debug.LogWarning(Tag + "Sidewalk: no roster art for " + id); continue; }
                var palette = (art.Palette != null && art.Palette.Length == 16 ? art.Palette : new Color[16]).ToArray();
                foreach (var (slot, hex) in dress)
                {
                    var c = Hex(hex);
                    if (Near(c, Offence) || Near(c, Defence)) Debug.LogWarning($"{Tag}Sidewalk: {id} slot {slot} #{hex} is near a role hue");
                    palette[slot] = c;
                }
                looks.Add((role, new SidewalkLife.Look { Name = $"{role} ({id})", Art = art, Palette = palette, Scale = scale }));
                cast.Append($"{role} {id} x{scale:F2}; ");
            }
            life.Taho = looks.FirstOrDefault(l => l.role == "taho").look;
            life.Beggar = looks.FirstOrDefault(l => l.role == "beggar").look;
            life.Kids = looks.Where(l => l.role == "kid").Select(l => l.look).ToArray();
            life.Spectators = looks.Where(l => l.role == "spectator").Select(l => l.look).ToArray();
            // The owner's pending choices (see BeggarOption); A for both is the committed look.
            if (BeggarChoice != BeggarOption.A_MangKanorRig) life.Beggar = BeggarLook(book, BeggarChoice) ?? life.Beggar;
            life.TahoCarry = TahoChoice;
            report.AppendLine($"Sidewalk: the beggar is {BeggarChoice} ({life.Beggar?.Name ?? "none"}), the magtataho carries at the {TahoChoice}.");

            var mats = Materials();
            life.Bamboo = mats["sidewalk_bamboo"]; life.Aluminium = mats["sidewalk_aluminium"]; life.Lid = mats["sidewalk_lid"];
            life.Rope = mats["sidewalk_rope"]; life.Tin = mats["sidewalk_tin"]; life.Cardboard = mats["sidewalk_carton"]; life.Coin = mats["sidewalk_coin"];
            life.Bundle = mats["sidewalk_bundle"]; life.BundleKnot = mats["sidewalk_bundle_knot"]; life.Bag = mats["sidewalk_bag"];

            var traffic = root.GetComponentInChildren<KantoTraffic>();
            var lanes = traffic != null ? traffic.Routes.Select(r => r.Points).ToArray() : new Vector3[0][];
            using (var art = new ArtQuery(dressing))
            {
                life.Walks = WalkPlan.Select(w => new SidewalkLife.Walk { Name = w.name, Points = w.points.Select(p => art.Ground(p)).ToArray(), Width = w.width }).ToArray();
                life.TahoWalk = TahoWalk; life.BeggarWalk = BeggarWalk;
                life.Watches = WatchPlan.Select(w => new SidewalkLife.Watch { Name = w.name, Walk = w.walk, LookAt = new Vector3(0f, 1.2f, 0f) }).ToArray();
                life.KidTrack = KidPlan.Select(p => art.Ground(p)).ToArray();
                life.KidHalfWidth = KidHalfWidth;
                life.BeggarSeat = art.Ground(Seat);
                life.BeggarFacing = Facing.normalized;
                life.BeggarReach = Reach;

                int bad = 0;
                var lines = new StringBuilder();
                for (int i = 0; i < life.Walks.Length; i++)
                    bad += Check(art, lanes, life.Walks[i].Name, life.Walks[i].Points, life.Walks[i].Width, new[] { -WalkLateral, 0f, WalkLateral }, lines);
                bad += Check(art, lanes, "kids' pavement", life.KidTrack, null, new[] { -KidHalfWidth, -KidHalfWidth * .5f, 0f, KidHalfWidth * .5f, KidHalfWidth }, lines);
                bad += CheckSeat(art, life.BeggarSeat, lines);
                report.AppendLine($"Sidewalk: {looks.Count} people ({cast}), {life.Walks.Length} routes, {life.Watches.Length} watch spots, " +
                                  $"kids' pavement {Length(life.KidTrack):F1} m, beggar at {life.BeggarSeat:F2}; {bad} failing samples.");
                report.Append(lines);
            }
        }

        private static bool Near(Color a, Color b)
        {
            Color.RGBToHSV(a, out float ha, out float sa, out float va);
            Color.RGBToHSV(b, out float hb, out float sb, out float vb);
            float dh = Mathf.Abs(ha - hb); dh = Mathf.Min(dh, 1f - dh);
            return dh < .05f && sa > .45f && va > .45f;
        }

        private static float Length(Vector3[] p)
        {
            float s = 0f;
            for (int k = 1; k < p.Length; k++) s += Vector3.Distance(p[k - 1], p[k]);
            return s;
        }

        private static Dictionary<string, Material> Materials()
        {
            if (!AssetDatabase.IsValidFolder(Folder)) AssetDatabase.CreateFolder(Path.GetDirectoryName(Folder).Replace('\\', '/'), Path.GetFileName(Folder));
            var result = new Dictionary<string, Material>();
            foreach (var (name, hex, gloss) in PropColours)
            {
                string path = $"{Folder}/{name}.mat";
                var m = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (m == null) { m = new Material(Shader.Find("Standard")); AssetDatabase.CreateAsset(m, path); }
                m.color = Hex(hex);
                m.SetFloat("_Glossiness", gloss);
                EditorUtility.SetDirty(m);
                result[name] = m;
            }
            AssetDatabase.SaveAssets();
            return result;
        }

        // ------------------------------------------------------------------ measuring the art

        /// <summary>Temporary MeshColliders on the dressing for the checks, removed on Dispose.</summary>
        private sealed class ArtQuery : IDisposable
        {
            private static readonly string[] Solid =
                { "StreetFences", "StreetFurniture", "StreetLife", "Props", "Eastside", "Heritage", "SariSari", "Guideway", "Landmarks",
                  "Stations", "ColumnSigns", "Street", "Tindahan", "Hazards", "RizalHall", "Lilies" };
            private readonly List<MeshCollider> _made = new List<MeshCollider>();
            public readonly HashSet<Collider> GroundColliders = new HashSet<Collider>();
            public readonly HashSet<Collider> Solids = new HashSet<Collider>();
            public readonly List<(Bounds b, string name)> Footprints = new List<(Bounds, string)>();

            public ArtQuery(Transform dressing)
            {
                Add(dressing.Find("StreetGround"), GroundColliders, false);
                foreach (var name in Solid) Add(dressing.Find(name), Solids, true);
                var trees = dressing.Find("Trees");
                if (trees != null)
                    foreach (var f in trees.GetComponentsInChildren<MeshFilter>())
                        if (f.name.Contains("wood") || f.name.Contains("pit") || f.name.Contains("hedge")) Collide(f, Solids, true);
                Physics.SyncTransforms();
            }

            private void Add(Transform group, HashSet<Collider> into, bool footprints)
            {
                if (group == null) return;
                foreach (var f in group.GetComponentsInChildren<MeshFilter>()) Collide(f, into, footprints);
            }

            private void Collide(MeshFilter f, HashSet<Collider> into, bool footprints)
            {
                var r = f.GetComponent<Renderer>();
                if (f.sharedMesh == null || r == null) return;
                var b = r.bounds;
                if (new Vector2(b.center.x, b.center.z).magnitude - Mathf.Max(b.extents.x, b.extents.z) > 70f) return;
                // Only low things block by their whole footprint (a bench, a crate, a planter); a tall
                // one (a pole with its arms, a shelter with its roof, a tree with its crown) blocks by
                // its surface, which the capsule below meets.
                if (footprints && b.min.y < 1f && b.max.y > .3f && b.max.y < 2.4f && b.size.x < 12f && b.size.z < 12f) Footprints.Add((b, f.name));
                var c = f.gameObject.AddComponent<MeshCollider>();
                c.sharedMesh = f.sharedMesh;
                _made.Add(c); into.Add(c);
            }

            /// <summary>The ground's height under (x, z): the first of OUR hits from 2.2 m down.</summary>
            public Vector3 Ground3(Vector2 p, out string surface, out string blocker)
            {
                surface = null; blocker = null;
                var hits = Physics.RaycastAll(new Vector3(p.x, 2.2f, p.y), Vector3.down, 3f, ~0, QueryTriggerInteraction.Ignore);
                foreach (var h in hits.OrderBy(h => h.distance))
                {
                    if (GroundColliders.Contains(h.collider)) { surface = h.collider.name; return h.point; }
                    if (Solids.Contains(h.collider) && h.point.y > .3f) { blocker = h.collider.name; return new Vector3(p.x, h.point.y, p.y); }
                }
                return new Vector3(p.x, .212f, p.y);
            }

            public Vector3 Ground(Vector2 p) => Ground3(p, out _, out _);

            public string Touches(Vector3 at, float radius)
            {
                foreach (var c in Physics.OverlapCapsule(at + Vector3.up * (.35f + radius), at + Vector3.up * 1.6f, radius, ~0, QueryTriggerInteraction.Ignore))
                    if (Solids.Contains(c)) return c.name;
                foreach (var (b, name) in Footprints)
                    if (at.x > b.min.x - radius && at.x < b.max.x + radius && at.z > b.min.z - radius && at.z < b.max.z + radius) return "footprint of " + name;
                return null;
            }

            public void Dispose()
            {
                foreach (var c in _made) if (c != null) Object.DestroyImmediate(c);
                Physics.SyncTransforms();
            }
        }

        private static int Check(ArtQuery art, Vector3[][] lanes, string name, Vector3[] points, float[] width, float[] laterals, StringBuilder log)
        {
            var causes = new Dictionary<string, (int n, Vector3 first)>();
            var surfaces = new Dictionary<string, int>();
            float nearestLane = float.MaxValue, nearestPlay = float.MaxValue;
            int samples = 0;
            void Fail(string why, Vector3 at) { causes[why] = causes.TryGetValue(why, out var v) ? (v.n + 1, v.first) : (1, at); }
            for (int k = 1; k < points.Length; k++)
            {
                var a = points[k - 1]; var b = points[k];
                var d = b - a; d.y = 0f;
                float len = d.magnitude;
                if (len < 1e-4f) continue;
                var side = Vector3.Cross(Vector3.up, d / len);
                for (float s = 0f; s <= len + 1e-3f; s += .25f)
                {
                    var c = Vector3.Lerp(a, b, s / len);
                    // The last metre and a half into the stopping end eases to the centre line, and the
                    // route's width narrows the offset (SidewalkLife.Advance).
                    float ease = k == points.Length - 1 ? Mathf.Clamp01((len - s) / 1.5f) : 1f;
                    float wa = width != null && k - 1 < width.Length ? width[k - 1] : 1f, wb = width != null && k < width.Length ? width[k] : 1f;
                    ease *= Mathf.Lerp(wa, wb, s / len);
                    foreach (float lat in laterals)
                    {
                        var p = c + side * lat * ease;
                        samples++;
                        var ground = art.Ground3(new Vector2(p.x, p.z), out string surface, out string blocker);
                        if (blocker != null) Fail("under or on " + blocker, p);
                        else if (surface == null) Fail("no ground", p);
                        else
                        {
                            surfaces[surface] = surfaces.TryGetValue(surface, out int n) ? n + 1 : 1;
                            if (surface.Contains("road")) Fail("on the road (" + surface + ")", p);
                        }
                        var at = new Vector3(p.x, ground.y, p.z);
                        string touch = art.Touches(at, .22f);
                        if (touch != null) Fail("touches " + touch, p);
                        if (Mathf.Abs(p.x) < PlayX && Mathf.Abs(p.z) < PlayZ) Fail("inside the play area", p);
                        if (Mathf.Abs(p.x) < BoxX && Mathf.Abs(p.z) < BoxZ) Fail("inside the chalk box", p);
                        nearestPlay = Mathf.Min(nearestPlay, OutsideBy(p));
                        foreach (var lane in lanes)
                        {
                            float dl = DistanceToLine(lane, p);
                            nearestLane = Mathf.Min(nearestLane, dl);
                            if (dl < TrafficClear) Fail($"within {TrafficClear} m of a traffic lane", p);
                        }
                    }
                }
            }
            int bad = causes.Values.Sum(v => v.n);
            log.AppendLine(FormattableString.Invariant($"  {name}: {Length(points):F1} m, {samples} samples, {bad} failing; ground {string.Join(", ", surfaces.Select(kv => $"{kv.Key} {kv.Value}"))}; ") +
                           FormattableString.Invariant($"nearest traffic lane {nearestLane:F1} m, {nearestPlay:F2} m outside the play area at the closest."));
            foreach (var kv in causes.OrderByDescending(kv => kv.Value.n))
                log.AppendLine(FormattableString.Invariant($"    FAIL {kv.Key}: {kv.Value.n} samples, first at ({kv.Value.first.x:F2}, {kv.Value.first.z:F2})"));
            return bad;
        }

        private static float OutsideBy(Vector3 p) => Mathf.Max(Mathf.Abs(p.x) - PlayX, Mathf.Abs(p.z) - PlayZ);

        /// <summary>The beggar's spot: clear ground for his seat and carton, outside the play area,
        /// and the nearest spot a player can stand (inside the walls, clear of the props'
        /// colliders) within his reach.</summary>
        private static int CheckSeat(ArtQuery art, Vector3 seat, StringBuilder log)
        {
            int bad = 0;
            var f = Facing.normalized;
            // His seat, his carton, the cup on his left, and his bundle and bag on his right.
            var right = Vector3.Cross(Vector3.up, f);
            foreach (var p in new[] { seat, seat + f * .45f, seat + f * .6f + Vector3.Cross(f, Vector3.up) * .22f,
                                      seat + right * .74f + f * .06f, seat + right * .66f - f * .24f })
            {
                string touch = art.Touches(p, .2f);
                if (touch != null) { bad++; log.AppendLine($"    FAIL the beggar's spot {p:F2} touches {touch}"); }
            }
            if (Mathf.Abs(seat.x) < PlayX && Mathf.Abs(seat.z) < PlayZ) { bad++; log.AppendLine("    FAIL the beggar sits inside the play area"); }
            Vector3 best = Vector3.zero; float bestDistance = float.MaxValue;
            for (float x = -10.65f; x <= 10.65f; x += .1f)
                for (float z = -16.15f; z <= 16.15f; z += .1f)
                {
                    var q = new Vector3(x, .212f, z);
                    float dq = Vector2.Distance(new Vector2(q.x, q.z), new Vector2(seat.x, seat.z));
                    if (dq >= bestDistance || dq > Reach + 1f) continue;
                    bool blocked = false;
                    foreach (var c in Physics.OverlapCapsule(q + Vector3.up * .65f, q + Vector3.up * 1.6f, .3f, ~0, QueryTriggerInteraction.Ignore))
                        if (!art.Solids.Contains(c) && !art.GroundColliders.Contains(c)) { blocked = true; break; }
                    if (blocked) continue;
                    best = q; bestDistance = dq;
                }
            if (bestDistance > Reach - .2f) { bad++; log.AppendLine(FormattableString.Invariant($"    FAIL the beggar is {bestDistance:F2} m from the nearest player spot, reach {Reach}")); }
            log.AppendLine(FormattableString.Invariant($"  beggar's spot {seat:F2}: nearest player spot ({best.x:F2}, {best.z:F2}) is {bestDistance:F2} m away (reach {Reach} m)."));
            return bad;
        }

        private static float DistanceToLine(Vector3[] line, Vector3 p)
        {
            float best = float.MaxValue;
            for (int k = 1; k < line.Length; k++)
            {
                var a = line[k - 1]; var ab = line[k] - a; ab.y = 0f;
                var ap = p - a; ap.y = 0f;
                float t = ab.sqrMagnitude > 1e-8f ? Mathf.Clamp01(Vector3.Dot(ap, ab) / ab.sqrMagnitude) : 0f;
                best = Mathf.Min(best, (ap - ab * t).magnitude);
            }
            return best;
        }

        // ------------------------------------------------------------------ the probe

        [MenuItem("Tumbang Preso/Sample Map/Probe Ilalim Rebuild Sidewalk Life")]
        public static void ProbeFromMenu()
        {
            string folder = NextFolder();
            Directory.CreateDirectory(folder);
            Probe(folder, IlalimSceneBuilder.ScenePath);
        }

        /// <summary>Batch: build the scene, then the sidewalk probe alone (no review renders).</summary>
        public static void RunBuildAndProbe()
        {
            try
            {
                IlalimSceneBuilder.Build();
                string folder = NextFolder();
                Directory.CreateDirectory(folder);
                Probe(folder, IlalimSceneBuilder.ScenePath);
            }
            catch (Exception e) { Debug.LogError(Tag + "FAILED: " + e); EditorApplication.Exit(1); return; }
            EditorApplication.Exit(0);
        }

        private static string NextFolder()
        {
            int v = 1; while (Directory.Exists($"Logs/ilalim-unity/v{v}")) v++; return $"Logs/ilalim-unity/v{v}";
        }

        /// <summary>Opens the saved scene (never saves it) and drives SidewalkLife.Simulate and the
        /// traffic's own step methods at 20 steps a second for 300 simulated seconds. It measures,
        /// per person: time shown, any step inside the play area or the chalk box, any step more
        /// than 0.5 m off an authored route (1 m on the kids' pavement, the seat excepted), and any
        /// overlap with a vehicle's rectangle (with 0.3 m to spare). It gives the beggar a coin from
        /// a player spot at the wall the first time he sits, sends a can-down moment while somebody
        /// watches, and renders each event with the match look.</summary>
        public static void Probe(string folder, string scenePath) => Probe(folder, scenePath, null, null, "sidewalk_probe.txt");

        /// <summary>The probe, optionally with the opened (never saved) scene's SidewalkLife
        /// changed first by `configure`, and with `rename` choosing which shots are written and
        /// under what names (null: every shot under its own name).</summary>
        private static void Probe(string folder, string scenePath, Action<SidewalkLife> configure, Dictionary<string, string> rename, string reportName)
        {
            EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            var sb = new StringBuilder("ILALIM SIDEWALK LIFE PROBE (SidewalkLife.Simulate and the traffic's steps, 0.05 s, no PlayMode)\n");
            var life = Object.FindAnyObjectByType<SidewalkLife>();
            var traffic = Object.FindAnyObjectByType<KantoTraffic>();
            if (life == null)
            {
                sb.AppendLine("No SidewalkLife in the scene.");
                File.WriteAllText(Path.Combine(folder, reportName), sb.ToString());
                return;
            }
            configure?.Invoke(life);
            const BindingFlags F = BindingFlags.Instance | BindingFlags.NonPublic;
            FieldInfo clock = null; MethodInfo routes = null, signals = null;
            if (traffic != null)
            {
                typeof(KantoTraffic).GetMethod("Start", F)?.Invoke(traffic, null);
                clock = typeof(KantoTraffic).GetField("_clock", F);
                routes = typeof(KantoTraffic).GetMethod("UpdateRoutes", F);
                signals = typeof(KantoTraffic).GetMethod("UpdateSignals", F);
            }
            life.Begin();
            var widths = traffic == null ? new float[0] : traffic.Drivers.Select(d =>
            {
                var b = BoundsOf(d.Body);
                var h = traffic.RouteHeading(d.Lane, d.Along);
                return b == null ? 2f : Mathf.Abs(b.Value.size.x * h.z) + Mathf.Abs(b.Value.size.z * h.x);
            }).ToArray();

            var lines = life.Walks.Select(w => w.Points).ToList();
            int n = life.PeopleCount;
            var shown = new int[n]; var inPlay = new int[n]; var inBox = new int[n]; var offRoute = new int[n]; var hitTraffic = new int[n];
            var closestPlay = Enumerable.Repeat(float.MaxValue, n).ToArray(); var closestCar = Enumerable.Repeat(float.MaxValue, n).ToArray();
            var states = Enumerable.Range(0, n).Select(_ => new HashSet<string>()).ToArray();
            var firstShown = Enumerable.Repeat(-1f, n).ToArray();
            string donation = "the beggar never sat down in 300 s", reaction = "nobody watched in 300 s";
            bool donated = false, reacted = false;
            float kidsAt = -1f, beggarThanksAt = -1f;
            var shots = new HashSet<string>();
            var playerSpot = new Vector3(-9.6f, .212f, -16.15f);
            Shooter shooter = null;
            void Take(string name, Vector3 eye, Vector3 target)
            {
                string to = name;
                if (rename == null ? name.StartsWith("check_") : !rename.TryGetValue(name, out to)) return;
                string path = Path.Combine(folder, to);
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                shooter.Shot(Path.GetDirectoryName(path), Path.GetFileName(path), eye, target);
            }
            try
            {
                shooter = new Shooter();
                const float dt = .05f; const int steps = 6000;
                for (int s = 1; s <= steps; s++)
                {
                    float t = s * dt;
                    if (traffic != null)
                    {
                        clock.SetValue(traffic, (float)clock.GetValue(traffic) + dt);
                        routes.Invoke(traffic, new object[] { dt });
                        signals.Invoke(traffic, null);
                    }
                    life.Simulate(dt);
                    for (int i = 0; i < n; i++)
                    {
                        if (!life.PersonShown(i)) continue;
                        var p = life.PersonPosition(i);
                        shown[i]++; states[i].Add(life.PersonState(i));
                        if (firstShown[i] < 0f) firstShown[i] = t;
                        if (Mathf.Abs(p.x) < PlayX && Mathf.Abs(p.z) < PlayZ) inPlay[i]++;
                        if (Mathf.Abs(p.x) < BoxX && Mathf.Abs(p.z) < BoxZ) inBox[i]++;
                        closestPlay[i] = Mathf.Min(closestPlay[i], OutsideBy(p));
                        bool kid = life.PersonRole(i) == "kid";
                        float off = kid ? DistanceToLine(life.KidTrack, p) : lines.Min(l => DistanceToLine(l, p));
                        bool seat = Vector2.Distance(new Vector2(p.x, p.z), new Vector2(life.BeggarSeat.x, life.BeggarSeat.z)) < .7f;
                        if (!seat && off > (kid ? KidHalfWidth + .3f : WalkLateral + .15f)) offRoute[i]++;
                        if (traffic != null)
                            for (int j = 0; j < traffic.Drivers.Length; j++)
                            {
                                var d = traffic.Drivers[j];
                                var q = d.Body.position;
                                if ((q - p).sqrMagnitude > 400f) continue;
                                var nose = Nose(d.Body, d.ModelOffset);
                                float gap = RectGap(q, nose, d.Length, widths[j], p);
                                closestCar[i] = Mathf.Min(closestCar[i], gap);
                                if (gap < .3f) hitTraffic[i]++;
                            }
                    }
                    // The events, once each.
                    if (!donated && life.BeggarSeated)
                    {
                        donated = true;
                        float reachFrom = Vector2.Distance(new Vector2(playerSpot.x, playerSpot.z), new Vector2(life.BeggarSeat.x, life.BeggarSeat.z));
                        bool took = life.Donate(playerSpot + Vector3.up * 1.1f);
                        bool again = life.Donate(playerSpot + Vector3.up * 1.1f);
                        donation = FormattableString.Invariant($"at t={t:F1}s the beggar sat; a coin from the player spot ({playerSpot.x:F2}, {playerSpot.z:F2}), {reachFrom:F2} m away (reach {life.BeggarReach}): accepted {took}, a second press at once accepted {again} (cooldown), donations {life.Donations}");
                        beggarThanksAt = t;
                    }
                    if (beggarThanksAt > 0f && t >= beggarThanksAt + .5f && !shots.Contains("beggar"))
                    {
                        shots.Add("beggar");
                        donation += FormattableString.Invariant($"; 0.5 s later thanking {life.BeggarThanking}, coin landing near the cup {life.CupPosition:F2}");
                        var seat = life.BeggarSeat;
                        Take("sidewalk_beggar_player", playerSpot + Vector3.up * 1.55f, seat + Vector3.up * .55f);
                        Take("sidewalk_beggar_side", seat + new Vector3(2.6f, 1.3f, -.9f), seat + Vector3.up * .5f);
                        Take("sidewalk_beggar_south", seat + new Vector3(.4f, 1.1f, -2.3f), seat + Vector3.up * .35f);
                        // Review-only: straight at his face (written only when an options run asks).
                        Take("check_beggar_front", seat + life.BeggarFacing.normalized * 1.7f + Vector3.up * .8f, seat + Vector3.up * .55f);
                    }
                    if (!reacted && life.Watching > 0)
                    {
                        reacted = true;
                        life.React(MatchFlair.Kind.LataDown, Vector3.zero);
                        reaction = FormattableString.Invariant($"at t={t:F1}s {life.Watching} watching; a can-down moment at the can: {life.Cheering} cheering");
                    }
                    if (life.KidsOut && kidsAt < 0f) kidsAt = t;
                    if (kidsAt > 0f && t >= kidsAt + 14f && !shots.Contains("kids"))
                    {
                        shots.Add("kids");
                        Take("sidewalk_kids_court", new Vector3(5.8f, 1.55f, -15.8f), new Vector3(8.5f, .7f, -27f));
                        Take("sidewalk_kids_close", new Vector3(5.2f, 2.2f, -22f), new Vector3(8.6f, .5f, -28.5f));
                    }
                    for (int i = 0; i < n; i++)
                    {
                        if (!life.PersonShown(i)) continue;
                        string role = life.PersonRole(i), state = life.PersonState(i);
                        var p = life.PersonPosition(i);
                        if (role == "taho" && state == "calling" && p.z > -30f && !shots.Contains("taho"))
                        {
                            shots.Add("taho");
                            Take("sidewalk_taho_court", new Vector3(-6f, 1.55f, -15.8f), p + Vector3.up * .9f);
                            Take("sidewalk_taho_close", p + new Vector3(2.4f, 1.3f, 2.2f), p + Vector3.up * .7f);
                            // Review-only: his right side (the pole's) and his front, low.
                            Take("check_taho_side", p + new Vector3(2.6f, .9f, 0f), p + Vector3.up * .7f);
                            Take("check_taho_front", p + new Vector3(.3f, 1.0f, 2.6f), p + Vector3.up * .7f);
                            Take("check_taho_back", p + new Vector3(-1.6f, 1.1f, -2.2f), p + Vector3.up * .7f);
                        }
                        if (role == "spectator" && state.StartsWith("watching") && !shots.Contains("spectator " + state) && shots.Count(x => x.StartsWith("spectator")) < 3)
                        {
                            shots.Add("spectator " + state);
                            var eye = new Vector3(Mathf.Clamp(p.x * .45f, -6f, 6f), 1.55f, Mathf.Clamp(p.z * .55f, -12f, 12f));
                            string where = state.Contains("PGH") ? "pgh" : p.z > 0f ? (p.x < 0f ? "nw" : "ne") : "s";
                            Take("sidewalk_watch_" + where, eye, p + Vector3.up * 1f);
                        }
                    }
                }
            }
            finally { shooter?.Dispose(); }

            sb.AppendLine($"People: {n}. Steps: 6000 x 0.05 s.");
            int totalPlay = inPlay.Sum(), totalBox = inBox.Sum(), totalOff = offRoute.Sum(), totalCar = hitTraffic.Sum();
            sb.AppendLine($"Steps inside the play area: {totalPlay}. Inside the chalk box: {totalBox}. Off an authored route: {totalOff}. Within 0.3 m of a vehicle: {totalCar}.");
            for (int i = 0; i < n; i++)
                sb.AppendLine(FormattableString.Invariant($"  {life.PersonName(i),-12} {life.PersonRole(i),-9} shown {shown[i] * .05f,6:F1} s (first at {firstShown[i]:F1} s), play {inPlay[i]}, box {inBox[i]}, off-route {offRoute[i]}, ") +
                              FormattableString.Invariant($"nearest the play area {(closestPlay[i] == float.MaxValue ? 0f : closestPlay[i]):F2} m outside, nearest vehicle {(closestCar[i] == float.MaxValue ? -1f : closestCar[i]):F2} m; states: {string.Join(", ", states[i])}"));
            sb.AppendLine("Donation: " + donation + ".");
            sb.AppendLine("Spectators: " + reaction + ".");
            sb.AppendLine(FormattableString.Invariant($"Kids first out at t={kidsAt:F1}s."));
            sb.AppendLine("Renders: " + string.Join(", ", Directory.GetFiles(folder, "sidewalk_*.png").Select(Path.GetFileName)));
            File.WriteAllText(Path.Combine(folder, reportName), sb.ToString());
            Debug.Log(Tag + sb);
            EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
        }

        private static Bounds? BoundsOf(Transform t)
        {
            Bounds? b = null;
            foreach (var r in t.GetComponentsInChildren<Renderer>())
                if (b == null) b = r.bounds; else { var bb = b.Value; bb.Encapsulate(r.bounds); b = bb; }
            return b;
        }

        private static Vector3 Nose(Transform body, Quaternion offset)
        {
            var f = body.rotation * Quaternion.Inverse(offset) * Vector3.forward; f.y = 0f;
            return f.sqrMagnitude > 1e-6f ? f.normalized : Vector3.forward;
        }

        /// <summary>The flat gap between a person (a 0.3 m radius) and a vehicle's rectangle.</summary>
        private static float RectGap(Vector3 centre, Vector3 heading, float length, float width, Vector3 p)
        {
            var d = p - centre; d.y = 0f;
            var side = Vector3.Cross(Vector3.up, heading);
            float along = Mathf.Max(0f, Mathf.Abs(Vector3.Dot(d, heading)) - length * .5f);
            float across = Mathf.Max(0f, Mathf.Abs(Vector3.Dot(d, side)) - width * .5f);
            return new Vector2(along, across).magnitude - .3f;
        }

        /// <summary>The match look on an offscreen camera, as IlalimLifeAuthor's probe renders.</summary>
        private sealed class Shooter : IDisposable
        {
            private readonly Camera _camera;
            private readonly WorldLookPresentation _look;

            public Shooter()
            {
                _camera = new GameObject("Ilalim sidewalk witness").AddComponent<Camera>();
                _camera.enabled = false; _camera.nearClipPlane = .05f; _camera.farClipPlane = 400f; _camera.fieldOfView = 58f;
                _camera.gameObject.AddComponent<ColourGrade>().AdoptFromScene();
                _camera.gameObject.AddComponent<WorldOutline>().PrototypeEnabled = true;
                try
                {
                    _camera.gameObject.AddComponent<WorldLookCamera>();
                    var sun = Object.FindObjectsByType<Light>().FirstOrDefault(l => l.type == LightType.Directional);
                    var sceneRoot = GameObject.Find(IlalimSceneBuilder.SceneName);
                    if (sceneRoot != null) _look = WorldLookPresentation.InstallPreview(sceneRoot.transform, 0f, sun);
                }
                catch (Exception e) { Debug.LogWarning(Tag + "Sidewalk probe look failed: " + e.Message); }
            }

            public void Shot(string folder, string name, Vector3 eye, Vector3 target)
            {
                const int w = 1600, h = 900;
                Physics.SyncTransforms();
                _camera.transform.SetPositionAndRotation(eye, Quaternion.LookRotation(target - eye));
                var rt = new RenderTexture(w, h, 24, RenderTextureFormat.ARGBHalf) { antiAliasing = 4 };
                rt.Create(); _camera.targetTexture = rt;
                _camera.Render();
                var previous = RenderTexture.active;
                var display = RenderTexture.GetTemporary(w, h, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
                bool write = GL.sRGBWrite; GL.sRGBWrite = QualitySettings.activeColorSpace == ColorSpace.Linear;
                Graphics.Blit(rt, display); GL.sRGBWrite = write; RenderTexture.active = display;
                var image = new Texture2D(w, h, TextureFormat.RGB24, false);
                image.ReadPixels(new Rect(0, 0, w, h), 0, 0); image.Apply();
                File.WriteAllBytes(Path.Combine(folder, name + ".png"), image.EncodeToPNG());
                RenderTexture.active = previous; _camera.targetTexture = null;
                RenderTexture.ReleaseTemporary(display); rt.Release();
                Object.DestroyImmediate(rt); Object.DestroyImmediate(image);
            }

            public void Dispose()
            {
                if (_look != null) Object.DestroyImmediate(_look.gameObject);
                if (_camera != null) Object.DestroyImmediate(_camera.gameObject);
            }
        }
    }
}
