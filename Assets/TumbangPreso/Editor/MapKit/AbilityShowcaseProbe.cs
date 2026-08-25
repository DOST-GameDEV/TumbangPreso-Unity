using System.IO;
using TumbangPreso.Abilities;
using TumbangPreso.Core;
using TumbangPreso.Visual;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace TumbangPreso.EditorTools.MapKit
{
    /// <summary>
    /// In-engine capture of what the Hero Strike abilities actually put on the floor.
    ///
    /// ⚠️⚠️ IT EXISTS BECAUSE THE READABILITY BUDGET HAD NEVER BEEN PHOTOGRAPHED. `docs/VISION.md`
    /// § 2 is the most argued-over page in this repository and its rule 5 is a picture test:
    /// *"A screenshot taken mid-fight must still show the lata, the chalk and every player. If
    /// it does not, the effect is too big however good it looks alone."* Nothing in the harness
    /// took that screenshot, so the rule was enforced by opinion for months while the numbers
    /// underneath it drifted.
    ///
    /// The measured state on 2026-08-25, before the footprint pass: the worst credible frame
    /// painted **81.9 per cent of the 14 by 14 box**, before props, tsinelas and nameplates.
    /// `docs/Hero_Strike_Balance.md` § 1.5 has the arithmetic and § 3.2 has what it fell to.
    ///
    /// ⚠️ THE HAZARDS ARE SPAWNED DIRECTLY RATHER THAN CAST. An ability cast needs a motor, a
    /// round, a match and an input intent, none of which exist in edit mode, and the thing being
    /// judged here is the GEOMETRY each ability leaves rather than the code path that leaves it.
    /// `HeroHazards` is the single place every footprint is built, so calling it is calling the
    /// real thing. What this cannot show is timing, and it does not claim to.
    ///
    /// ⚠️⚠️ THE FILENAMES CARRY A VERSION AND `Version` MUST BE BUMPED EVERY TIME. `CLAUDE.md`
    /// § 6.1: chat clients cache by name, so overwriting a render leaves the previous image on
    /// screen and the review is conducted against a picture that is not on disk any more.
    /// </summary>
    public static class AbilityShowcaseProbe
    {
        private const string OutDir = "Logs/shots-abilities";
        private const int ShotWidth = 1280;
        private const int ShotHeight = 720;

        /// <summary>Bump on every capture. See the class note.</summary>
        private const string Version = "v7";

        [MenuItem("Tumbang Preso/Capture Ability Showcase")]
        public static void RunFromMenu() => Execute();

        public static void Run() => EditorApplication.Exit(Execute() ? 0 : 1);

        public static bool Execute()
        {
            Directory.CreateDirectory(OutDir);
            EditorSceneManager.OpenScene(IlalimNgTulayBuilder.ScenePath, OpenSceneMode.Single);

            // Same reason the map probe does it: `EnvColourPass` runs from `Start()`, which never
            // happens in an edit-mode capture, and without it every surface is a raw .mtl colour.
            foreach (var pass in Object.FindObjectsByType<EnvColourPass>(
                         FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                pass.Apply();
            }

            var spawned = new System.Collections.Generic.List<GameObject>();

            try
            {
                // ---------------------------------------------------------------
                // 1. EACH FLOOR EFFECT ALONE, so a single silhouette can be judged on
                //    its own before anything overlaps it. `docs/VISION.md` § 2 rule 3
                //    is about DETAIL, and detail is what a lone frame shows.
                // ---------------------------------------------------------------

                Solo(spawned, "fire_trail",
                     () => HeroHazards.SpawnFireTrail(Vector3.zero, 1.0f, 60.0f, 0));

                Solo(spawned, "shock_trail",
                     () => HeroHazards.SpawnShockTrail(Vector3.zero, 1.0f, 60.0f, 1));

                Solo(spawned, "ice_sheet",
                     () => HeroHazards.SpawnIceSheet(Vector3.zero, 2.3f, 60.0f, 2));

                Solo(spawned, "seance_void",
                     () => HeroHazards.SpawnSeanceVoid(Vector3.zero, 2.8f, 60.0f, 3));

                Solo(spawned, "barricade",
                     () => HeroHazards.SpawnIceBarricade(Vector3.zero, Vector3.forward, 60.0f));

                Solo(spawned, "lava_decal",
                     () => HeroHazards.SpawnCrackedLavaDecal(Vector3.zero, 2.2f, 60.0f));

                // ---------------------------------------------------------------
                // 2. A DASH CORRIDOR, which is the shape that was actually wrong and
                //    which no single-disc frame can show. Six drops at the capped
                //    count, laid along a run the way `OnTick` lays them.
                // ---------------------------------------------------------------

                Clear(spawned);
                for (int i = 0; i < 6; i++)
                {
                    spawned.Add(HeroHazards.SpawnFireTrail(
                        new Vector3(-3.0f + i * 1.2f, 0.0f, -1.0f), 1.0f, 60.0f, 0));
                    spawned.Add(HeroHazards.SpawnShockTrail(
                        new Vector3(-3.0f + i * 1.2f, 0.0f, 2.0f), 1.0f, 60.0f, 1));
                }
                Shot("corridors", new Vector3(0.0f, 5.4f, -8.6f),
                     Quaternion.Euler(28.0f, 0.0f, 0.0f), 62.0f);
                Shot("corridors_eye", new Vector3(0.0f, 1.65f, -7.4f),
                     Quaternion.Euler(4.0f, 0.0f, 0.0f), 72.0f);

                // ---------------------------------------------------------------
                // 3. THE WORST FRAME. Every persistent floor effect in the game live
                //    at once, which is the § 2 rule 5 test. The lata, the chalk and
                //    the throwing line all have to survive it.
                //
                // ⚠️ IT IS DELIBERATELY WORSE THAN A REAL ROUND. Four seats cannot
                //    realistically hold all of this live simultaneously under the
                //    charge economy, which is the point: if the budget holds here it
                //    holds anywhere.
                // ---------------------------------------------------------------

                Clear(spawned);
                spawned.Add(HeroHazards.SpawnIceSheet(new Vector3(-3.4f, 0.0f, 2.2f), 2.3f, 60.0f, 2));
                spawned.Add(HeroHazards.SpawnCrackedLavaDecal(new Vector3(3.2f, 0.0f, 2.6f), 2.2f, 60.0f));
                spawned.Add(HeroHazards.SpawnSeanceVoid(new Vector3(3.6f, 0.0f, -3.0f), 2.8f, 60.0f, 3));
                spawned.Add(HeroHazards.SpawnIceBarricade(new Vector3(-2.6f, 0.0f, -2.4f), Vector3.forward, 60.0f));

                for (int i = 0; i < 6; i++)
                {
                    spawned.Add(HeroHazards.SpawnFireTrail(
                        new Vector3(-1.0f + i * 0.9f, 0.0f, -5.2f), 1.0f, 60.0f, 0));
                    spawned.Add(HeroHazards.SpawnShockTrail(
                        new Vector3(-5.4f + i * 0.9f, 0.0f, 5.0f), 1.0f, 60.0f, 1));
                }

                Shot("worstframe", new Vector3(0.0f, 7.2f, -11.0f),
                     Quaternion.Euler(30.0f, 0.0f, 0.0f), 64.0f);

                // The same frame from the two positions a match is actually played from.
                float ring = Confinement.AttackerSpawnRing();
                Shot("worstframe_thrower", new Vector3(-1.4f, 1.65f, -ring),
                     Quaternion.Euler(5.0f, 6.0f, 0.0f), 72.0f);
                Shot("worstframe_taya", new Vector3(0.0f, 1.65f, 0.4f),
                     Quaternion.Euler(6.0f, 180.0f, 0.0f), 72.0f);

                Debug.Log($"[AbilityShowcaseProbe] wrote the {Version} set to {OutDir}.");
                return true;
            }
            finally
            {
                Clear(spawned);
            }
        }

        /// <summary>One effect, one overhead frame and one at eye height.</summary>
        private static void Solo(System.Collections.Generic.List<GameObject> live,
                                 string name, System.Func<GameObject> make)
        {
            Clear(live);

            var go = make();
            if (go != null) live.Add(go);

            // ⚠️ THE OVERHEAD FRAME IS THE ONE THAT SHOWS FOOTPRINT AND THE EYE FRAME IS THE ONE
            // THAT SHOWS WHETHER IT READS. Both are needed and they disagree constantly: a disc
            // that is obviously 2.3 m from above is two pixels tall from a metre away, which is
            // the asymmetry `Visual.DamageVignette` exists to answer.
            Shot(name, new Vector3(0.0f, 4.6f, -3.4f), Quaternion.Euler(46.0f, 0.0f, 0.0f), 55.0f);
            Shot(name + "_eye", new Vector3(0.0f, 1.65f, -4.6f), Quaternion.Euler(6.0f, 0.0f, 0.0f), 70.0f);
        }

        private static void Clear(System.Collections.Generic.List<GameObject> live)
        {
            foreach (var go in live)
                if (go != null) Object.DestroyImmediate(go);

            live.Clear();
        }

        private static void Shot(string name, Vector3 pos, Quaternion rot, float fov)
        {
            var camGo = new GameObject("~AbilityShowcaseCam");
            camGo.transform.position = pos;
            camGo.transform.rotation = rot;

            var cam = camGo.AddComponent<Camera>();
            cam.fieldOfView = fov;
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 260.0f;
            cam.clearFlags = CameraClearFlags.Skybox;

            // Adopts the loaded map's grade rather than a literal, for the reason written up on
            // `IlalimNgTulayShowcaseProbe.Shot`: hard-coding one there is what let a map that
            // rendered black pass eight rounds of showcase review.
            camGo.AddComponent<ColourGrade>().AdoptFromScene();

            var rt = new RenderTexture(ShotWidth, ShotHeight, 24, RenderTextureFormat.ARGB32)
            {
                antiAliasing = 4,
            };

            cam.targetTexture = rt;
            cam.Render();

            RenderTexture.active = rt;
            var tex = new Texture2D(ShotWidth, ShotHeight, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, ShotWidth, ShotHeight), 0, 0);
            tex.Apply();

            RenderTexture.active = null;
            cam.targetTexture = null;

            string path = Path.Combine(OutDir, $"ability_{name}_{Version}.png");
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Debug.Log($"[AbilityShowcaseProbe] {path} ({new FileInfo(path).Length / 1024} KB)");

            Object.DestroyImmediate(tex);
            rt.Release();
            Object.DestroyImmediate(rt);
            Object.DestroyImmediate(camGo);
        }
    }
}
