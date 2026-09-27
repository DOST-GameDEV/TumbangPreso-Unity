using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace TumbangPreso.EditorTools.MapKit
{
    /// <summary>
    /// The cove's LIFE: seabird flocks and reef fish schools (`LagoonFlocks`) and the ambient
    /// waves, wind and bird calls (`LagoonSoundscape`). Owner, 2026-09-27: *"add birds and fish
    /// (via boids)"* and *"add wind, birds and waves sfx (environmental sounds, so being
    /// near/facing the water you hear more waves, same with wind when facing/nearer into the
    /// land)"*. Called by `LagoonCoveSceneBuilder.Build` after the dressing, water and seabed
    /// exist, because both components are fed from what is already in the scene:
    ///   * the SHORELINE is traced from the ground's own mesh collider (a 3 m grid of downward
    ///     rays; a dry cell with a wet neighbour is shore, its normal points at the wet side),
    ///     so it can never drift from the terrain the way a hand-kept list would;
    ///   * the SCHOOLS gather over the reef heads nearest the court (the reefs the players can
    ///     see), spaced at least 16 m apart, floating above the colony's crown.
    /// ⚠️ Cosmetic and local: neither component is networked or touches gameplay.
    /// </summary>
    internal static class LagoonCoveLife
    {
        private const string Root = "Assets/TumbangPreso/Art/LagoonCove";
        private const string Tag = "[LagoonCove] ";

        internal struct Reef { public Vector3 At; public float Top; }

        public static void Build(Transform root, float waterY, IList<Reef> reefs)
        {
            var life = new GameObject("Life").transform;
            life.SetParent(root, false);
            var templates = new GameObject("Templates").transform;
            templates.SetParent(life, false);

            var flocks = life.gameObject.AddComponent<LagoonFlocks>();
            flocks.WaterY = waterY;
            flocks.SkyCentre = new Vector3(0, waterY, 0);
            flocks.CourtCentre = Vector3.zero;
            flocks.BirdTemplate = Template("fauna_seabird", templates);
            flocks.FishTemplates = new[] { "fauna_fish_a", "fauna_fish_b", "fauna_fish_c", "fauna_fish_d" }
                .Select(n => Template(n, templates)).Where(t => t != null).ToArray();

            // Schools over the reef heads the players can see: nearest the court first, 16 m apart.
            var centres = new List<Vector3>(); var floors = new List<float>();
            foreach (var r in reefs.OrderBy(r => new Vector2(r.At.x, r.At.z).magnitude))
            {
                if (centres.Count >= 8) break;
                if (centres.Any(c => Vector2.Distance(new Vector2(c.x, c.z), new Vector2(r.At.x, r.At.z)) < 16f)) continue;
                float floor = Mathf.Min(r.Top + 0.3f, waterY - 1.2f);
                centres.Add(new Vector3(r.At.x, (floor + waterY - 0.5f) * 0.5f, r.At.z));
                floors.Add(floor);
            }
            flocks.SchoolCentres = centres.ToArray();
            flocks.SchoolFloor = floors.ToArray();

            var sound = life.gameObject.AddComponent<LagoonSoundscape>();
            sound.WaterY = waterY;
            sound.Flocks = flocks;
            sound.Waves = Clip("lagoon_waves");
            sound.Wind = Clip("lagoon_wind");
            sound.BirdCalls = Enumerable.Range(1, 8).Select(i => Clip("lagoon_gull_" + i)).Where(c => c != null).ToArray();
            Shoreline(root, waterY, out sound.Shoreline, out sound.ShoreNormals);

            Debug.Log($"{Tag}Life: {(flocks.BirdTemplate != null ? 1 : 0)} bird and {flocks.FishTemplates.Length} fish templates, " +
                      $"{centres.Count} schools, {sound.Shoreline.Length} shoreline points, " +
                      $"clips waves={sound.Waves != null} wind={sound.Wind != null} calls={sound.BirdCalls.Length}.");
        }

        /// <summary>⚠️ The two beds import as PCM, never the default Vorbis: a compressed loop can
        /// carry a few milliseconds of encoder padding at its seam, a click every 32 s that the
        /// synthesis took care to remove (seam step 0.0008 against 0.093 inside the loop).</summary>
        private static AudioClip Clip(string name)
        {
            string path = $"Assets/TumbangPreso/Art/audio/ambience/{name}.wav";
            if (name == "lagoon_waves" || name == "lagoon_wind")
            {
                var importer = AssetImporter.GetAtPath(path) as AudioImporter;
                if (importer != null)
                {
                    var s = importer.defaultSampleSettings;
                    if (s.compressionFormat != AudioCompressionFormat.PCM)
                    {
                        s.compressionFormat = AudioCompressionFormat.PCM;
                        s.loadType = AudioClipLoadType.DecompressOnLoad;
                        importer.defaultSampleSettings = s;
                        importer.SaveAndReimport();
                    }
                }
            }
            return AssetDatabase.LoadAssetAtPath<AudioClip>(path);
        }

        /// <summary>An inactive copy of a fauna model with its flat glTF colours moved onto the
        /// cove's own painted shader, so the animals take the same light, fog and ink as the
        /// props around them.</summary>
        private static Transform Template(string name, Transform parent)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{Root}/Fauna/{name}.glb");
            if (prefab == null) { Debug.LogWarning(Tag + "No fauna model " + name); return null; }
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            go.name = "model";
            var painted = Shader.Find("TumbangPreso/LagoonPainted");
            string folder = Root + "/Fauna/Materials";
            if (!AssetDatabase.IsValidFolder(folder)) AssetDatabase.CreateFolder(Root + "/Fauna", "Materials");
            foreach (var r in go.GetComponentsInChildren<Renderer>())
            {
                var mats = r.sharedMaterials;
                for (int i = 0; i < mats.Length; i++)
                {
                    if (mats[i] == null) continue;
                    string key = mats[i].name.Replace(" (Instance)", "").Trim();
                    string path = $"{folder}/{key}.mat";
                    var m = AssetDatabase.LoadAssetAtPath<Material>(path);
                    // glTF colour factors are LINEAR; a Color property in this linear project is
                    // decoded from sRGB, so the factor is re-encoded (.gamma) to land unchanged.
                    var src = mats[i];
                    Color c = src.HasProperty("baseColorFactor") ? src.GetColor("baseColorFactor") : src.color;
                    if (m == null) { m = new Material(painted); AssetDatabase.CreateAsset(m, path); }
                    m.shader = painted;
                    m.SetColor("_Color", c.gamma);
                    m.SetTexture("_MainTex", null);
                    m.SetFloat("_Glossiness", 0.1f);
                    m.enableInstancing = true;
                    EditorUtility.SetDirty(m);
                    mats[i] = m;
                }
                r.sharedMaterials = mats;
                r.shadowCastingMode = ShadowCastingMode.On;
            }
            foreach (var c in go.GetComponentsInChildren<Collider>()) Object.DestroyImmediate(c);
            // ⚠️ FEET AT THE ORIGIN. The models are pivoted at their centre of mass (right for a
            // bird in flight banking about its middle), but LagoonFlocks sets a landed bird's
            // ORIGIN on the sand, which sank it to the waist. The model goes under a holder whose
            // origin is the model's lowest point; flight rolls the holder, so a banking bird now
            // pivots a few centimetres below its middle, which reads the same.
            var holder = new GameObject(name).transform;
            holder.SetParent(parent, false);
            float low = float.MaxValue;
            foreach (var r in go.GetComponentsInChildren<Renderer>()) low = Mathf.Min(low, r.bounds.min.y);
            go.transform.SetParent(holder, true);
            if (low < float.MaxValue) go.transform.localPosition += new Vector3(0, go.transform.position.y - low, 0);
            holder.gameObject.SetActive(false);
            return holder;
        }

        /// <summary>Shore points every 3 m where dry ground meets the water, with a horizontal
        /// normal pointing out to sea. Traced from the ground's own collider.</summary>
        private static void Shoreline(Transform root, float waterY, out Vector3[] points, out Vector3[] normals)
        {
            var ground = root.GetComponentsInChildren<MeshCollider>().FirstOrDefault(c => c.gameObject.name == "ground");
            var pts = new List<Vector3>(); var nrm = new List<Vector3>();
            if (ground == null) { Debug.LogWarning(Tag + "No ground collider; no shoreline"); points = pts.ToArray(); normals = nrm.ToArray(); return; }
            const float step = 3f, half = 150f;
            int n = Mathf.RoundToInt(2 * half / step) + 1;
            var wet = new bool[n, n];
            for (int j = 0; j < n; j++)
                for (int i = 0; i < n; i++)
                {
                    var p = new Vector3(-half + i * step, 200f, -half + j * step);
                    wet[i, j] = !ground.Raycast(new Ray(p, Vector3.down), out var hit, 400f) || hit.point.y < waterY;
                }
            for (int j = 1; j < n - 1; j++)
                for (int i = 1; i < n - 1; i++)
                {
                    if (wet[i, j]) continue;
                    var sea = Vector3.zero;
                    if (wet[i + 1, j]) sea += Vector3.right;
                    if (wet[i - 1, j]) sea += Vector3.left;
                    if (wet[i, j + 1]) sea += Vector3.forward;
                    if (wet[i, j - 1]) sea += Vector3.back;
                    if (sea == Vector3.zero) continue;
                    pts.Add(new Vector3(-half + i * step, waterY, -half + j * step));
                    nrm.Add(sea.normalized);
                }
            points = pts.ToArray(); normals = nrm.ToArray();
        }
    }
}
