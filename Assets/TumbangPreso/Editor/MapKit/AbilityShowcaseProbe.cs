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
    ///
    /// ⚠️⚠️ AND IT NOW PHOTOGRAPHS THE TRANSIENTS TOO, WHICH IS WHAT IT COULD NEVER DO.
    /// `docs/TODO.md` § 8 item 2, open since 2026-08-25: this probe captured *"the persistent
    /// zones only, and every one of these changes is on a transient that lives 0.4 to 1.1 s, so
    /// the v7 captures do not show a single one of them."* Every blast core, every shockwave and
    /// all three ultimates are transients, so the entire § 8 silhouette pass — a nova shell
    /// instead of a sphere, a shockfront with a leading edge, an ion spire instead of a disc —
    /// was reviewed against pictures that could not contain it.
    ///
    /// Two things were in the way and both are fixed rather than worked around:
    ///  * `CreateExplosion` opened with `if (round == null) return;`, so in an edit-mode capture
    ///    it drew nothing at all. `HeroHazards.CreateExplosionVisual` is now the half that puts
    ///    pixels on screen and it does not need a match.
    ///  * `Update` never runs here, so a spawned blast froze on its first frame: scale 0.35, the
    ///    moment before it becomes the thing being argued about. `Visual.VfxTimeline.StepAll`
    ///    winds every effect to the same fraction of its own life, through the same code the
    ///    player's frame comes from.
    /// </summary>
    public static class AbilityShowcaseProbe
    {
        private const string OutDir = "Logs/shots-abilities";
        private const int ShotWidth = 1280;
        private const int ShotHeight = 720;

        /// <summary>
        /// The share of a frame that may sit at or above <see cref="BlownLevel"/> before the
        /// capture is a failure rather than a picture.
        ///
        /// ⚠️⚠️ THIS IS `docs/VISION.md` § 2 RULE 5 AS A NUMBER, AND IT EXISTS BECAUSE THE RULE
        /// WAS BEING BROKEN BY A FACTOR OF SEVEN WITH NOBODY ABLE TO SEE IT. Rule 5 is a picture
        /// test: *"a screenshot taken mid-fight must still show the lata, the chalk and every
        /// player"*. A frame that is 60 per cent white shows none of them, and no amount of
        /// reading the code says so.
        ///
        /// ⚠️ THE BOUND IS MEASURED, NOT PICKED. The v9 set, with Thunderstrike's flash still
        /// wrong: the empty street reads **3.0 per cent**, the worst legitimate effect
        /// (Cheska's frost blast) **8.3 per cent**, the ability corridors **3.0**, the deliberate
        /// worst-frame pile-up **4.1**, and Thunderstrike **62.8**. Everything the team has
        /// already accepted fits under 9; the one defect sits seven times over it. 12 leaves
        /// room for a brighter effect somebody argues for on purpose and still catches this
        /// class of fault on the day it lands.
        /// </summary>
        private const float MaxBlownFraction = 0.12f;

        /// <summary>Luminance at which a pixel counts as blown, out of 255.</summary>
        private const int BlownLevel = 245;

        /// <summary>Frames that broke the bound, reported together at the end of a run.</summary>
        private static readonly System.Collections.Generic.List<string> Blown =
            new System.Collections.Generic.List<string>();

        /// <summary>⚠️ ONLY THE TRANSIENTS ARE GATED, and only while one is on screen. A flash
        /// is the thing that blows a frame; a floor decal cannot. Measuring the persistent shots
        /// too would add nothing and would make the bound answer to a lighting change on the
        /// map rather than to an ability.</summary>
        private static bool _gateBlowout;

        /// <summary>Bump on every capture. See the class note.</summary>
        private const string Version = "v10";

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

                // ---------------------------------------------------------------
                // 4. THE TRANSIENTS: the four blast styles and Zack's ultimate, each
                //    photographed at the moment it is biggest on screen rather than at
                //    the frame it is born on. See the class note for why this could not
                //    be done before.
                // ---------------------------------------------------------------

                Clear(spawned);
                _gateBlowout = true;

                Transient("blast_fire", () => HeroHazards.CreateExplosionVisual(
                    Vector3.zero, 4.8f, null, HeroHazards.ExplosionStyle.Fire));

                Transient("blast_quake", () => HeroHazards.CreateExplosionVisual(
                    Vector3.zero, 4.5f, null, HeroHazards.ExplosionStyle.Quake, Vector3.forward));

                Transient("blast_frost", () => HeroHazards.CreateExplosionVisual(
                    Vector3.zero, 4.2f, null, HeroHazards.ExplosionStyle.Frost));

                Transient("blast_slipper", () => HeroHazards.CreateExplosionVisual(
                    Vector3.zero, 2.2f, null, HeroHazards.ExplosionStyle.Slipper));

                Transient("blast_thunder", () => HeroHazards.CreateThunderstrike(Vector3.zero, 7.0f));

                _gateBlowout = false;

                Debug.Log($"[AbilityShowcaseProbe] wrote the {Version} set to {OutDir}.");

                if (Blown.Count > 0)
                {
                    foreach (string line in Blown)
                        Debug.LogError($"[AbilityShowcaseProbe] FAIL {line}");

                    Debug.LogError("[AbilityShowcaseProbe] see MaxBlownFraction: VISION.md " +
                                   "section 2 rule 5 asks that a mid-fight frame still show the " +
                                   "lata, the chalk and every player.");
                    return false;
                }

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

        /// <summary>
        /// One transient effect, wound to the moment worth looking at, then swept up.
        ///
        /// ⚠️⚠️ IT SWEEPS BY DIFFING THE SCENE ROOTS, NOT BY COLLECTING RETURN VALUES. A blast
        /// is not one object: `CreateExplosionVisual` alone creates a core, a ground wave, up to
        /// 22 debris cubes, a light and a popup, and `CreateThunderstrike` adds three lightning
        /// columns and twelve sparks. None of them are returned, and `Object.Destroy(go, t)`
        /// never comes due in edit mode, so anything not swept here would survive into the NEXT
        /// capture and quietly appear in a frame that is supposed to show one ability.
        ///
        /// ⚠️ AT 0.35 OF EACH EFFECT'S OWN LIFE. `VfxTimeline.StepAll`'s note has the reasoning:
        /// a fraction rather than a time, because a core runs 0.5 s and its wave 0.4 s and
        /// asking both for the same number of seconds photographs them at different moments of
        /// the same event. 0.35 is past the birth frame, before the fade takes the alpha, and it
        /// is where a silhouette is at its most legible.
        /// </summary>
        private static void Transient(string name, System.Action make)
        {
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            var before = new System.Collections.Generic.HashSet<GameObject>(scene.GetRootGameObjects());

            make();

            int stepped = Visual.VfxTimeline.StepAll(0.35f);

            Shot(name, new Vector3(0.0f, 4.6f, -6.2f), Quaternion.Euler(30.0f, 0.0f, 0.0f), 62.0f);
            Shot(name + "_eye", new Vector3(0.0f, 1.65f, -7.4f), Quaternion.Euler(6.0f, 0.0f, 0.0f), 72.0f);

            Debug.Log($"[AbilityShowcaseProbe] {name}: stepped {stepped} timed effect(s).");

            foreach (var go in scene.GetRootGameObjects())
                if (!before.Contains(go)) Object.DestroyImmediate(go);
        }

        /// <summary>
        /// What share of a frame is at or above <see cref="BlownLevel"/>.
        ///
        /// ⚠️ LUMINANCE, NOT THE MAX CHANNEL. A saturated blue at full strength is a colour;
        /// white is an absence of picture. Rec. 601 weights are what separates the two, and the
        /// whole point of this measurement is to catch the second without punishing the first.
        /// </summary>
        private static float BlownFraction(Texture2D tex)
        {
            var pixels = tex.GetPixels32();
            if (pixels.Length == 0) return 0.0f;

            int over = 0;
            foreach (var p in pixels)
            {
                int luma = (p.r * 299 + p.g * 587 + p.b * 114) / 1000;
                if (luma >= BlownLevel) over++;
            }

            return over / (float)pixels.Length;
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

            float blown = BlownFraction(tex);

            string path = Path.Combine(OutDir, $"ability_{name}_{Version}.png");
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Debug.Log($"[AbilityShowcaseProbe] {path} ({new FileInfo(path).Length / 1024} KB, " +
                      $"{blown * 100.0f:F1}% blown)");

            if (_gateBlowout && blown > MaxBlownFraction)
            {
                Blown.Add($"{name}: {blown * 100.0f:F1}% of the frame is at or above " +
                          $"{BlownLevel}/255, over the {MaxBlownFraction * 100.0f:F0}% bound");
            }

            Object.DestroyImmediate(tex);
            rt.Release();
            Object.DestroyImmediate(rt);
            Object.DestroyImmediate(camGo);
        }
    }
}
