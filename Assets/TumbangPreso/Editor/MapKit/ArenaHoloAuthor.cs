using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Newtonsoft.Json.Linq;
using TumbangPreso.Map;
using UnityEngine;

namespace TumbangPreso.EditorTools.MapKit
{
    /// <summary>
    /// Bakes what moves in the Arena's HOLO kit (tools/arena_holo_motion.json, written by
    /// tools/author_arena_holo.py) onto one <see cref="ArenaHoloMotion"/> on the "Holo" group that
    /// <see cref="ArenaArtPlacer"/> placed. `ArenaSceneBuilder` calls <see cref="Attach"/> once,
    /// after the art is placed. With no file, or no Holo group, it does nothing and says so.
    ///
    /// The file names each thing by its object in the kit, which is the name its placement wears
    /// in the scene. A placement's own transform carries the kit's pivot (the middle of the globe,
    /// the balloon's tether point), so the transform is what turns, sways and rides.
    /// </summary>
    internal static class ArenaHoloAuthor
    {
        public const string MotionPath = "tools/arena_holo_motion.json";
        private const string Tag = "[Arena] ";

        /// <summary>Returns the moves baked.</summary>
        public static int Attach(Transform root)
        {
            if (!File.Exists(MotionPath)) return 0;

            var group = root.Find("Dressing/Holo");
            if (group == null) { Debug.Log($"{Tag}No Holo group in the art: {MotionPath} is not used."); return 0; }

            var moves = JToken.Parse(File.ReadAllText(MotionPath))["moves"] as JArray;
            if (moves == null) { Debug.LogWarning($"{Tag}{MotionPath} has no list of moves"); return 0; }

            var byName = new Dictionary<string, Transform>();
            foreach (Transform child in group) byName[child.name] = child;

            var baked = new List<ArenaHoloMotion.Move>();
            var missing = new List<string>();
            int index = 0;
            foreach (var entry in moves)
            {
                string name = (string)entry["object"];
                string kind = (string)entry["kind"];
                if (string.IsNullOrEmpty(name) || !byName.TryGetValue(name, out var body)) { missing.Add(name ?? "(no name)"); continue; }

                var move = new ArenaHoloMotion.Move
                {
                    Body = body,
                    Skin = body.GetComponentInChildren<Renderer>(true),
                    // Golden-ratio steps: no two neighbours start their cycle together.
                    Phase = Mathf.Repeat(index * 0.618034f, 1.0f),
                };
                index++;

                switch (kind)
                {
                    case "rotate":
                        move.Kind = ArenaHoloMotion.Kind.Rotate;
                        move.Amount = Number(entry["degrees_per_second"], 6.0f);
                        break;
                    case "scroll":
                        move.Kind = ArenaHoloMotion.Kind.Scroll;
                        move.Amount = Number(entry["v_per_second"], 0.02f);
                        move.Material = MaterialIndex(move.Skin, (string)entry["material"]);
                        break;
                    case "sway":
                        move.Kind = ArenaHoloMotion.Kind.Sway;
                        move.Amount = Number(entry["degrees"], 1.0f);
                        move.Period = Number(entry["period"], 8.0f);
                        break;
                    case "bob":
                        move.Kind = ArenaHoloMotion.Kind.Bob;
                        move.Amount = Number(entry["metres"], 1.0f);
                        move.Period = Number(entry["period"], 10.0f);
                        break;
                    case "pulse":
                        move.Kind = ArenaHoloMotion.Kind.Pulse;
                        move.Low = Number(entry["low"], 0.85f);
                        move.Amount = Number(entry["high"], 1.0f);
                        move.Period = Number(entry["period"], 4.0f);
                        break;
                    default:
                        Debug.LogWarning($"{Tag}{MotionPath}: '{name}' has an unknown kind '{kind}'; skipped");
                        continue;
                }

                baked.Add(move);
            }

            if (missing.Count > 0)
                Debug.LogWarning($"{Tag}{MotionPath} names things the Holo group does not hold (export the kit again?): {string.Join(", ", missing)}");
            if (baked.Count == 0) return 0;

            var motion = group.gameObject.GetComponent<ArenaHoloMotion>();
            if (motion == null) motion = group.gameObject.AddComponent<ArenaHoloMotion>();
            motion.Moves = baked.ToArray();
            Debug.Log($"{Tag}Holo motion: {baked.Count} moves baked onto {group.name}.");
            return baked.Count;
        }

        private static int MaterialIndex(Renderer skin, string material)
        {
            if (skin == null || string.IsNullOrEmpty(material)) return 0;
            var materials = skin.sharedMaterials;
            for (int i = 0; i < materials.Length; i++)
                if (materials[i] != null && materials[i].name == material) return i;
            return 0;
        }

        private static float Number(JToken token, float fallback)
        {
            if (token == null || token.Type == JTokenType.Null) return fallback;
            return token.Type == JTokenType.Float || token.Type == JTokenType.Integer
                ? token.Value<float>()
                : float.TryParse((string)token, NumberStyles.Float, CultureInfo.InvariantCulture, out float value) ? value : fallback;
        }
    }
}
