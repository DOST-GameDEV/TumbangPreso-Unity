using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace TumbangPreso.Map
{
    /// <summary>
    /// PAYO, THE STAGE AS DATA: `Art/Payo/payo_layout.json` (docs/PAYO_MAP_BRIEF.md). Unity axes, metres, the can at
    /// the origin. The file is the art draft's own (`tools/payo_layouts_draft.json`), copied whole, so this reads
    /// that file's words: a layout is a list of BLOCKS (a fixed plan `x`, `z`, `sx`, `sz`, a walking height `top`, a
    /// shaft down to `shaftTo`, and for a flight of stairs `stairDown` and `low`), jump pads (the geysers), speed
    /// pads, stamina pickups and MOVERS (small slabs that travel a straight line during a round), each placed by
    /// x and z. A block keeps its id and its plan in every layout: it only rises and sinks.
    ///
    /// `Editor/MapKit/PayoSceneBuilder.cs` builds the scene from it (the blocks become `ArenaStage` pieces of kind
    /// Block), and the play probe reads its checks from it. Nothing at run time needs the file: what the game uses
    /// is saved in the scene.
    /// </summary>
    public sealed class PayoLayout
    {
        /// <summary>How a block is reached: on foot from the can, by a running jump, by a sprint jump, or only by a geyser or a lift.</summary>
        public enum Reach { Walk, Jump, Sprint, Bonus }

        public sealed class Block
        {
            public string Id;
            public float X, Z, SX, SZ, Top, Low, ShaftTo;
            /// <summary>0 level, 1 down toward +x, 2 toward -x, 3 toward +z, 4 toward -z (`ArenaStage.Shape.Stair`).</summary>
            public int Stair;
            public Reach Reach;
            public Vector3 Centre => new Vector3(X, Stair != 0 ? (Top + Low) * 0.5f : Top, Z);
        }

        public sealed class Pad { public Vector3 At; public float Speed; public string On, Serves; }
        public sealed class SpeedPad { public Vector3 At; public Vector3 Along; public Vector2 HalfSize; public string On; }
        public sealed class Pickup { public Vector3 At; public string On; }
        public sealed class Mover { public string Id, Serves, Note; public float SX, SZ, Period, Phase, Dwell; public Vector3 From, To; }

        public sealed class Layout
        {
            public string Name, Blurb;
            public float CanHeight;
            public readonly List<Block> Blocks = new List<Block>();
            public readonly List<Pad> Pads = new List<Pad>();
            public readonly List<SpeedPad> SpeedPads = new List<SpeedPad>();
            public readonly List<Pickup> Pickups = new List<Pickup>();
            public readonly List<Mover> Movers = new List<Mover>();

            public Block Find(string id)
            {
                foreach (var b in Blocks) if (b.Id == id) return b;
                return null;
            }

            /// <summary>The highest block top over a plan point, and which block (null over the pit).</summary>
            public Block At(float x, float z, out float y)
            {
                Block best = null; y = float.NegativeInfinity;
                foreach (var b in Blocks)
                {
                    if (Mathf.Abs(x - b.X) > b.SX * 0.5f || Mathf.Abs(z - b.Z) > b.SZ * 0.5f) continue;
                    float top = HeightAt(b, x, z);
                    if (top > y) { y = top; best = b; }
                }
                return best;
            }
        }

        public float StageTop, WallHalf = 22.0f, PitHalf = 30.0f, PitLipHalf = 33.0f, PitFloor = -45.0f, MistY = -26.0f, CatchY = -22.0f, SunkTop = -30.0f;
        public Vector3 Taya = new Vector3(0.0f, 0.0f, -2.5f);
        public readonly List<Vector3> Attackers = new List<Vector3>();
        public readonly List<Layout> Layouts = new List<Layout>();
        /// <summary>What the file says of itself (`gameCopy`): which draft it was taken from.</summary>
        public string Source = "";

        public static float HeightAt(Block b, float x, float z)
        {
            switch (b.Stair)
            {
                case 1: return Mathf.Lerp(b.Top, b.Low, Mathf.Clamp01((x - (b.X - b.SX * 0.5f)) / b.SX));
                case 2: return Mathf.Lerp(b.Top, b.Low, Mathf.Clamp01(((b.X + b.SX * 0.5f) - x) / b.SX));
                case 3: return Mathf.Lerp(b.Top, b.Low, Mathf.Clamp01((z - (b.Z - b.SZ * 0.5f)) / b.SZ));
                case 4: return Mathf.Lerp(b.Top, b.Low, Mathf.Clamp01(((b.Z + b.SZ * 0.5f) - z) / b.SZ));
                default: return b.Top;
            }
        }

        /// <summary>The unit vector of a direction word of the file: "+x", "-x", "+z", "-z".</summary>
        public static Vector3 Way(string word)
        {
            switch ((word ?? "").Trim())
            {
                case "+x": return Vector3.right;
                case "-x": return Vector3.left;
                case "-z": return Vector3.back;
                default: return Vector3.forward;
            }
        }

        private static int StairOf(string word)
        {
            switch ((word ?? "").Trim())
            {
                case "+x": return 1;
                case "-x": return 2;
                case "+z": return 3;
                case "-z": return 4;
                default: return 0;
            }
        }

        private static float F(JToken token, float fallback) =>
            token != null && (token.Type == JTokenType.Float || token.Type == JTokenType.Integer) ? (float)token : fallback;

        private static string S(JToken token) => token != null && token.Type == JTokenType.String ? (string)token : null;

        private static Vector3 V(JToken token, Vector3 fallback)
        {
            var a = token as JArray;
            return a != null && a.Count >= 3 ? new Vector3(F(a[0], 0.0f), F(a[1], 0.0f), F(a[2], 0.0f)) : fallback;
        }

        /// <summary>Reads the file's text. Throws, naming the place, on a layout the game could not stand on.</summary>
        public static PayoLayout Parse(string json)
        {
            var doc = JObject.Parse(json);
            var layout = new PayoLayout
            {
                Source = S(doc["gameCopy"]) ?? "",
                StageTop = F(doc["stageTop"], 0.0f), WallHalf = F(doc["wallHalf"], 22.0f), PitHalf = F(doc["pitHalf"], 30.0f),
                PitLipHalf = F(doc["pitLipHalf"], 33.0f), PitFloor = F(doc["pitFloor"], -45.0f), MistY = F(doc["mistY"], -26.0f),
                CatchY = F(doc["catchY"], -22.0f), SunkTop = F(doc["sunkTop"], -30.0f),
            };
            var spawns = doc["spawns"];
            if (spawns != null)
            {
                layout.Taya = V(spawns["taya"], layout.Taya);
                foreach (var token in spawns["attackers"] as JArray ?? new JArray()) layout.Attackers.Add(V(token, Vector3.zero));
            }

            foreach (var entry in doc["layouts"] as JArray ?? new JArray())
            {
                var one = new Layout { Name = S(entry["name"]) ?? "layout" + layout.Layouts.Count, Blurb = S(entry["blurb"]) ?? "", CanHeight = F(entry["canHeight"], 0.0f) };
                var bonus = new HashSet<string>();
                foreach (var id in entry["bonus"] as JArray ?? new JArray()) bonus.Add((string)id);
                var reach = entry["reach"] as JObject;

                foreach (var p in entry["pieces"] as JArray ?? new JArray())
                {
                    string id = S(p["id"]);
                    if (string.IsNullOrEmpty(id)) throw new InvalidOperationException($"Payo layout '{one.Name}' has a piece with no id");
                    string kind = S(p["kind"]) ?? "block";
                    if (kind != "block") throw new InvalidOperationException($"Payo layout '{one.Name}', piece '{id}': unknown kind '{kind}' (only block)");
                    var b = new Block
                    {
                        Id = id, X = F(p["x"], 0.0f), Z = F(p["z"], 0.0f), SX = F(p["sx"], 0.0f), SZ = F(p["sz"], 0.0f),
                        Top = F(p["top"], 0.0f), ShaftTo = F(p["shaftTo"], layout.PitFloor), Stair = StairOf(S(p["stairDown"])),
                    };
                    b.Low = b.Stair != 0 ? F(p["low"], b.Top) : b.Top;
                    if (b.Stair != 0 && b.Low >= b.Top - 0.01f) { b.Stair = 0; b.Low = b.Top; }
                    if (b.SX <= 0.0f || b.SZ <= 0.0f) throw new InvalidOperationException($"Payo layout '{one.Name}', block '{id}' has no size");
                    if (one.Find(id) != null) throw new InvalidOperationException($"Payo layout '{one.Name}' names the block '{id}' twice");
                    string word = reach != null ? S(reach[id]) : null;
                    b.Reach = bonus.Contains(id) || word == "bonus" ? Reach.Bonus : word == "jump" ? Reach.Jump : word == "sprint" ? Reach.Sprint : Reach.Walk;
                    one.Blocks.Add(b);
                }

                foreach (var p in entry["jumpPads"] as JArray ?? new JArray())
                    one.Pads.Add(new Pad { At = new Vector3(F(p["x"], 0.0f), F(p["y"], 0.0f), F(p["z"], 0.0f)), Speed = F(p["speed"], 15.5f), On = S(p["on"]), Serves = S(p["serves"]) });
                foreach (var p in entry["speedPads"] as JArray ?? new JArray())
                {
                    var half = p["halfSize"] as JArray;
                    one.SpeedPads.Add(new SpeedPad
                    {
                        At = new Vector3(F(p["x"], 0.0f), F(p["y"], 0.0f), F(p["z"], 0.0f)), Along = Way(S(p["along"])), On = S(p["on"]),
                        HalfSize = half != null && half.Count >= 2 ? new Vector2(F(half[0], 0.75f), F(half[1], 1.5f)) : new Vector2(0.75f, 1.5f),
                    });
                }
                foreach (var p in entry["pickups"] as JArray ?? new JArray())
                    one.Pickups.Add(new Pickup { At = new Vector3(F(p["x"], 0.0f), F(p["y"], 0.0f), F(p["z"], 0.0f)), On = S(p["on"]) });
                foreach (var p in entry["movers"] as JArray ?? new JArray())
                    one.Movers.Add(new Mover
                    {
                        Id = S(p["id"]) ?? "mover" + one.Movers.Count, Serves = S(p["serves"]), Note = S(p["note"]) ?? "",
                        SX = F(p["sx"], 3.0f), SZ = F(p["sz"], 3.0f), From = V(p["from"], Vector3.zero), To = V(p["to"], Vector3.zero),
                        Period = Mathf.Max(1.0f, F(p["period"], 16.0f)), Phase = F(p["phase"], 0.0f), Dwell = Mathf.Max(0.0f, F(p["dwell"], 3.0f)),
                    });

                layout.Layouts.Add(one);
            }

            if (layout.Layouts.Count == 0) throw new InvalidOperationException("The Payo layout names no layouts");
            return layout;
        }
    }
}
