using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace TumbangPreso.EditorTools
{
    public static class FppArmsSnapshotTool
    {
        private const string OutDir = "Logs/shots-fpp";

        /// <summary>
        /// ⚠️⚠️ BUMP THIS EVERY CAPTURE. `CLAUDE.md` § 6.1: chat clients cache images by
        /// filename, so overwriting a render leaves the previous one on screen and the whole
        /// review is conducted against an image that is not on disk any more. This tool wrote
        /// `fpp_nemu_showcase.png` unversioned for its whole life, which is why nobody could
        /// review two iterations of an arm in one sitting.
        /// </summary>
        private const string Version = "v18";
        private const int Width = 1280;
        private const int Height = 720;

        /// <summary>
        /// The tsinelas the held shots are taken with.
        ///
        /// ⚠️⚠️ THIS TOOL RENDERED A BROWN BLOB FOR ITS WHOLE LIFE AND EVERY JUDGEMENT ABOUT THE
        /// FIRST-PERSON SLIPPER WAS MADE AGAINST IT. `docs/TODO.md` § 79.8: *"AND THE SNAPSHOT
        /// TOOL CANNOT JUDGE THIS. `FppArmsSnapshotTool` builds the viewmodel with its default
        /// placeholder mesh dressed in `PropFoam`, so every review of the held slipper so far has
        /// been against a flat brown blob rather than the real skin. Fix the tool first, or the
        /// next iteration is blind too."*
        ///
        /// `ViewmodelArms.Build` dresses `HeldSlipper` in `UiTheme.PropFoam` (#7a5741) as a
        /// stand-in and `MatchSkin` is what replaces it with the real mesh and materials in play.
        /// Nothing here ever called it, so three recorded attempts at the carry pose were
        /// reviewed against a shape that is not the shape the player holds.
        ///
        /// ⚠️ SIKE IS THE DEFAULT BECAUSE IT IS THE HARDEST ONE. It is two submeshes whose first
        /// is the near-black body, so it catches both the placeholder colour and the
        /// one-material-per-submesh fault § 79.7 records, where a multi-surface skin drew its
        /// first surface and nothing else.
        /// </summary>
        private const string HeldSlipperId = "sike";

        /// <summary>
        /// ⚠️ THE WHOLE ROSTER GETS ONE SHOT EACH, ON ONE CHARACTER, because "does the held shoe
        /// wear its own skin" is a question about the SHOE and asking it eighteen times per shoe
        /// is eighteen times the render for no more information.
        /// </summary>
        private static readonly string[] SlipperSweep =
        {
            "sike", "classic", "spartan", "pantulog", "crocs",
            "heels", "loafers", "sandals", "pambahay", "alpombra",
        };

        private static readonly string[] Characters =
        {
            "sean", "zack", "dante", "cheska", "nemu", "phaister",
            "bayan", "maring", "totoy", "inday", "kuya_boy", "ate_girlie",
            "tikboy", "bebang", "jun_jun", "lola_pacing", "mang_kanor", "aling_nena",
        };

        [MenuItem("Tumbang Preso/Capture FPP Arms Screenshots")]
        public static void CaptureAllFromMenu() => Execute();

        public static void CaptureAll()
        {
            InspectHeroModels();
            Execute();
            EditorApplication.Exit(0);
        }

        private static void InspectHeroModels()
        {
            var book = Resources.Load<RosterBook>("RosterBook");
            if (book == null) return;

            foreach (var heroId in Characters)
            {
                var person = book.People.Find(p => p.Id == heroId);
                if (person == null || person.Model == null) continue;

                Debug.Log($"=== HERO MODEL: {heroId} ===");
                var smrs = person.Model.GetComponentsInChildren<SkinnedMeshRenderer>(true);
                foreach (var smr in smrs)
                {
                    Debug.Log($"  SMR: {smr.name}, mesh={smr.sharedMesh?.name}, verts={smr.sharedMesh?.vertexCount}, bones={smr.bones?.Length}");
                    if (smr.bones != null)
                    {
                        for (int i = 0; i < smr.bones.Length; i++)
                        {
                            var b = smr.bones[i];
                            Debug.Log($"    Bone [{i}]: {b.name}, pos={b.localPosition}, rot={b.localRotation.eulerAngles}");
                        }
                    }
                }
            }
        }

        private static void Execute()
        {
            Directory.CreateDirectory(OutDir);

            // Open Eskinita or create isolated scene
            if (File.Exists("Assets/TumbangPreso/Scenes/Arena/Eskinita.unity"))
            {
                EditorSceneManager.OpenScene("Assets/TumbangPreso/Scenes/Arena/Eskinita.unity", OpenSceneMode.Single);
            }
            else
            {
                EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            }

            var camGo = new GameObject("~FppCaptureCamera");
            var cam = camGo.AddComponent<Camera>();
            cam.fieldOfView = 58.0f;
            cam.nearClipPlane = 0.01f;
            cam.farClipPlane = 100.0f;

            // Create lighting matching in-game toon shading balance
            var lightGo = new GameObject("~FppCaptureLight");
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.05f;
            light.color = new Color(1.0f, 0.98f, 0.95f);
            light.transform.rotation = Quaternion.Euler(45.0f, -30.0f, 0);

            var fillLightGo = new GameObject("~FppFillLight");
            var fillLight = fillLightGo.AddComponent<Light>();
            fillLight.type = LightType.Directional;
            fillLight.intensity = 0.35f;
            fillLight.color = new Color(0.75f, 0.78f, 0.85f);
            fillLight.transform.rotation = Quaternion.Euler(30.0f, 150.0f, 0);

            var armsMount = new GameObject("~ViewmodelMount");
            armsMount.transform.position = Vector3.zero;
            armsMount.transform.localScale = Vector3.one * 0.72f;
            armsMount.transform.localRotation = Quaternion.identity;

            var arms = armsMount.AddComponent<CameraSystem.ViewmodelArms>();
            arms.EnsureBuilt();

            var rt = new RenderTexture(Width, Height, 24, RenderTextureFormat.ARGB32);
            cam.targetTexture = rt;

            // ⚠️ BUILT ONCE, OUTSIDE THE LOOP, AND PARKED OUT OF FRAME. `MatchSkin` only READS the
            // source renderer's mesh and materials, so the object it reads from never has to be
            // visible; leaving it at the origin would put a second shoe in every shot beside the
            // arms. This is the same trap the stashed `PropPreviewProbe` records about lights
            // that outlive their iteration.
            var heldSource = BuildSlipperSource(HeldSlipperId);

            foreach (string charId in Characters)
            {
                arms.SetCharacter(charId);

                // ⚠️⚠️ AFTER `SetCharacter`, NEVER BEFORE. `SetCharacter` rebuilds the arms and
                // re-dresses what hangs off them, so a skin applied first is overwritten by the
                // placeholder and the tool goes back to photographing a brown blob silently.
                if (heldSource != null) arms.MatchSkin(heldSource);

                // --- 1. In-Game First Person Perspective ---
                cam.transform.position = new Vector3(0.0f, 0.12f, -0.16f);
                cam.transform.rotation = Quaternion.Euler(22.0f, 0, 0);

                arms.SetHolding(true);
                arms.StepVisuals(0.016f, snap: true);
                cam.Render();
                SaveTexture(rt, Path.Combine(OutDir, $"fpp_{charId}_holding_{Version}.png"));

                arms.SetHolding(false);
                arms.StepVisuals(0.016f, snap: true);
                cam.Render();
                SaveTexture(rt, Path.Combine(OutDir, $"fpp_{charId}_empty_{Version}.png"));

                // --- 2. Full Arm Showcase Inspect View (Showing sleeves, pauldron, markings, bracelets) ---
                cam.transform.position = new Vector3(0.0f, 0.42f, -0.82f);
                cam.transform.rotation = Quaternion.Euler(28.0f, 0, 0);

                arms.SetHolding(true);
                arms.StepVisuals(0.016f, snap: true);
                cam.Render();
                SaveTexture(rt, Path.Combine(OutDir, $"fpp_{charId}_showcase_{Version}.png"));
            }

            // --- 3. One frame per tsinelas, on one character, so the whole roster is reviewable
            //        in a sitting. This is the sweep § 79.8 needs to judge the carry against the
            //        real skins rather than against the placeholder.
            arms.SetCharacter("dante");
            cam.transform.position = new Vector3(0.0f, 0.42f, -0.82f);
            cam.transform.rotation = Quaternion.Euler(28.0f, 0, 0);

            foreach (string slipperId in SlipperSweep)
            {
                var source = BuildSlipperSource(slipperId);
                if (source == null) continue;

                arms.MatchSkin(source);
                arms.SetHolding(true);
                arms.StepVisuals(0.016f, snap: true);
                cam.Render();
                SaveTexture(rt, Path.Combine(OutDir, $"fpp_held_{slipperId}_{Version}.png"));

                UnityEngine.Object.DestroyImmediate(source.gameObject);
            }

            // --- 4. THE WIND-UP, FROM THE SIDE, AT THREE CHARGE LEVELS -----------------------
            //
            // ⚠️⚠️ THIS ANSWERS "DOES THE ARM PULL BACK OR PUSH FORWARD" WITH A PICTURE, WHICH IS
            // THE ONLY WAY IT SHOULD BE ANSWERED. 🧑 2026-08-29: *"is wind up even in the irght
            // direction? Usually when u wind up btw u pull BACK not put arm forward"*.
            //
            // The sign convention IS documented in `ViewmodelArms` (godot -x becomes unity +x
            // becomes cock back and up, and `SetCharge` writes `+WindupRad`), and a previous bug
            // B-131 is recorded as having got it backwards. But this session found two faults
            // that a careful source read had already "ruled out", so a convention written in a
            // comment is a claim, not evidence.
            //
            // ⚠️ THE CAMERA IS SIDE-ON, BECAUSE THE FPP CAMERA CANNOT SEE THIS AT ALL. Down the
            // barrel, an arm rotating about its local X moves mostly toward and away from the
            // lens, which is the one axis a straight-on shot flattens. Back-versus-forward is a
            // profile question and needs a profile camera.
            foreach (float charge in new[] { 0.0f, 0.5f, 1.0f })
            {
                arms.SetCharge(charge);
                arms.StepVisuals(0.016f, snap: true);

                cam.transform.position = new Vector3(1.15f, 0.30f, -0.30f);
                cam.transform.rotation = Quaternion.Euler(8.0f, -74.0f, 0.0f);
                cam.Render();
                SaveTexture(rt, Path.Combine(
                    OutDir, $"fpp_windup_side_{Mathf.RoundToInt(charge * 100.0f):D3}_{Version}.png"));

                cam.transform.position = new Vector3(0.0f, 0.42f, -0.82f);
                cam.transform.rotation = Quaternion.Euler(28.0f, 0, 0);
                cam.Render();
                SaveTexture(rt, Path.Combine(
                    OutDir, $"fpp_windup_front_{Mathf.RoundToInt(charge * 100.0f):D3}_{Version}.png"));
            }

            // ⚠️ RELEASED, or every capture after this one is posed mid-charge.
            arms.SetCharge(-1.0f);
            arms.StepVisuals(0.016f, snap: true);

            if (heldSource != null) UnityEngine.Object.DestroyImmediate(heldSource.gameObject);

            cam.targetTexture = null;
            RenderTexture.active = null;
            UnityEngine.Object.DestroyImmediate(rt);
            UnityEngine.Object.DestroyImmediate(camGo);
            UnityEngine.Object.DestroyImmediate(lightGo);
            UnityEngine.Object.DestroyImmediate(fillLightGo);

            Debug.Log("[FppArmsSnapshotTool] Captured FPP arms screenshots for all characters successfully!");
        }

        /// <summary>
        /// A real world tsinelas, dressed exactly as `MatchInstaller.BuildSlipper` dresses the
        /// one in a match, for <see cref="CameraSystem.ViewmodelArms.MatchSkin"/> to read.
        ///
        /// ⚠️ IT GOES THROUGH `ToonSkin.ApplySlipper`, THE SAME ENTRY POINT THE MATCH USES. The
        /// point of this tool is to show what the player holds, so a shoe dressed here by any
        /// other path is a picture of a build that does not exist. That is the fault the report
        /// README records about `ModelSheet.Run` rendering the cast with no palette.
        ///
        /// ⚠️ PARKED 40 M AWAY RATHER THAN DEACTIVATED. `MatchSkin` reads the source through
        /// `GetComponentInChildren&lt;MeshFilter&gt;()`, which skips inactive objects, so
        /// switching it off would make it silently invisible to the very method it exists for
        /// and the tool would fall back to the placeholder with no error.
        /// </summary>
        private static Slipper BuildSlipperSource(string slipperId)
        {
            var book = Resources.Load<RosterBook>("RosterBook");
            if (book == null || book.Slippers == null) return null;

            var art = book.Slippers.Find(s => s != null && s.Id == slipperId);
            if (art == null || art.Model == null)
            {
                Debug.LogWarning($"[FppArmsSnapshotTool] no slipper art for '{slipperId}'");
                return null;
            }

            // ⚠⚠ THE SOURCE IS REIMPORTED BEFORE IT IS PHOTOGRAPHED, AND WITHOUT THIS THE TOOL
            // CAN PHOTOGRAPH A BUILD THAT DOES NOT EXIST — which is the exact failure § 79.8
            // records this tool having once, from a different cause. On 2026-08-30 the three
            // `baseColorFactor` values in `tsinelas_heels.glb` were rewritten and the capture came
            // back **byte-identical**, which is not a result, it is a stale artifact wearing one.
            //
            // ⚠ AND IT PRINTS WHAT IT IS ABOUT TO SHOOT. `docs/VISION.md` § 5: verify by
            // measuring. A tool whose whole job is to answer "what does the player actually hold"
            // should say which albedo it resolved, so a render that looks wrong can be told apart
            // from a render of the wrong thing in one line instead of three sessions.
            string assetPath = AssetDatabase.GetAssetPath(art.Model);
            if (!string.IsNullOrEmpty(assetPath))
                AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);

            var go = new GameObject($"~FppSlipperSource_{slipperId}");
            go.transform.position = new Vector3(0.0f, -40.0f, 0.0f);

            var model = UnityEngine.Object.Instantiate(art.Model, go.transform);

            foreach (var r in model.GetComponentsInChildren<Renderer>(true))
            {
                var mats = r.sharedMaterials;
                for (int i = 0; i < mats.Length; i++)
                {
                    var mat = mats[i];
                    if (mat == null) continue;

                    string colour = "no colour property";
                    foreach (string name in new[] { "_BaseColor", "_Color", "baseColorFactor", "_TintColor" })
                    {
                        if (!mat.HasProperty(name)) continue;
                        colour = $"{name}={mat.GetColor(name)}";
                        break;
                    }

                    string tex = "no texture";
                    foreach (string name in new[] { "_BaseMap", "_MainTex", "baseColorTexture" })
                    {
                        if (!mat.HasProperty(name) || mat.GetTexture(name) == null) continue;
                        tex = $"{name}={mat.GetTexture(name).name}";
                        break;
                    }

                    Debug.Log($"[FppSkin] {slipperId} slot {i} '{mat.name}' shader={mat.shader.name} {colour} {tex}");
                }
            }
            model.name = "Visual";

            foreach (var c in model.GetComponentsInChildren<Collider>(true))
                UnityEngine.Object.DestroyImmediate(c);

            Visual.ToonSkin.ApplySlipper(model, Visual.ToonSkin.PropOutlineWidth);

            return go.AddComponent<Slipper>();
        }

        private static void SaveTexture(RenderTexture rt, string filePath)
        {
            RenderTexture.active = rt;
            var tex = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
            tex.Apply();
            RenderTexture.active = null;

            byte[] bytes = tex.EncodeToPNG();
            File.WriteAllBytes(filePath, bytes);
            UnityEngine.Object.DestroyImmediate(tex);
        }
    }
}

