using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TumbangPreso.Abilities;
using TumbangPreso.Core;
using TumbangPreso.Visual;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace TumbangPreso.EditorTools.MapKit
{
    /// <summary>
    /// NEMU'S PET GHOSTS ON THE COURT, FILMED (owner, 2026-10-19: the mint mouse, the cocoa bear, the butter owl). They sit behind
    /// `PetGhostModels.PetGhosts`, which is OFF; this turns it on in memory for the film only and back off after. v2 (owner:
    /// *"they should be more lively, make them fly around in loops"*): each flies round its point with `PetGhostFlight`.
    ///
    /// ⚠️ STAGED, AND SAID SO. No match runs, so the skills' own update loops (`NemuBlindfold`, `NemuSkitter`, `NemuLookout`)
    /// do not drive these. What IS the game's: the meshes and attach points through `BlindfoldGhostModel` / `NemuGhostModels`
    /// with the switch on, the drawing through `NemuGhostBody` and `Shaders/PetGhost` (fade, sway, rim, glow), the arms aimed
    /// and stretched exactly as the skills aim them (`Quaternion.LookRotation`, length over hand-on-arm), and Eskinita in its
    /// own daylight. What is NOT: the choreography (when it springs, where it walks) is typed here as the skills describe it,
    /// the people are still roster poses, and the can is a stand-in. ASTRA's smoke and eye lights (`_smoke.Light`) are not drawn.
    ///
    /// Request: lines in `Temp/nemu-petghost-film.request`: `ping`, or `film <tag>`. Frames (two views side by side, 30 a
    /// second of film time) go to `Logs/nemu_ghosts/petghost_<tag>_frames/`, then `done_petghost_<tag>.txt` or `failed_...`.
    /// </summary>
    [InitializeOnLoad]
    internal static class NemuPetGhostFilmPoll
    {
        private const string Request = "Temp/nemu-petghost-film.request";
        private static double _next;
        static NemuPetGhostFilmPoll() { EditorApplication.update += Poll; }

        private static void Poll()
        {
            if (EditorApplication.timeSinceStartup < _next) return;
            _next = EditorApplication.timeSinceStartup + 1.0;
            if (!File.Exists(Request) || EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling) return;
            // Import whatever changed first (new models, a new shader), as `QuickReworkFilmPoll` does; if that starts a compile, wait.
            AssetDatabase.Refresh();
            if (EditorApplication.isCompiling) return;
            string[] lines = File.ReadAllLines(Request);
            File.Delete(Request);
            foreach (string line in lines)
            {
                string[] words = line.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                if (words.Length == 0) continue;
                Directory.CreateDirectory(NemuPetGhostFilm.OutDir);
                if (words[0] == "ping") { File.WriteAllText(Path.Combine(NemuPetGhostFilm.OutDir, "done_petghost_ping.txt"), DateTime.Now.ToString("HH:mm:ss")); continue; }
                if (words[0] == "film") NemuPetGhostFilm.Run(words.Length > 1 ? words[1] : "v1");
            }
        }
    }

    internal static class NemuPetGhostFilm
    {
        internal const string OutDir = "Logs/nemu_ghosts";
        private const string Map = "Assets/TumbangPreso/Scenes/Maps/Eskinita.unity";
        private const int CellWidth = 640, CellHeight = 360, Rate = 30;
        // The victim's face, from `NemuBlindfold` (its private VictimHeadHeight and VictimHeadHalfDepth).
        private const float VictimHeadHeight = 1.38f, VictimHeadHalfDepth = .28f;

        private static float Smooth(float a, float b, float x) { float t = Mathf.Clamp01((x - a) / (b - a)); return t * t * (3f - 2f * t); }

        internal static void Run(string tag)
        {
            tag = new string((tag ?? "v1").Where(c => char.IsLetterOrDigit(c) || c == '-' || c == '_').ToArray());
            string done = Path.Combine(OutDir, "done_petghost_" + tag + ".txt"), failed = Path.Combine(OutDir, "failed_petghost_" + tag + ".txt");
            if (File.Exists(done)) File.Delete(done);
            if (File.Exists(failed)) File.Delete(failed);
            var report = new System.Text.StringBuilder();
            var owned = new List<Object>();
            bool was = PetGhostModels.PetGhosts, lit = false;
            Scene scene = default;
            var active = SceneManager.GetActiveScene();
            bool wasDirty = active.isLoaded && active.isDirty;
            try
            {
                PetGhostModels.PetGhosts = true;
                scene = EditorSceneManager.OpenPreviewScene(Map);
                var physics = scene.GetPhysicsScene();
                lit = Unsupported.SetOverrideLightingSettings(scene);
                Shader.SetGlobalFloat("_CharacterSmoothShade", ToonSkin.CharacterSmoothShade);
                Shader.SetGlobalFloat("_FxStudyBloom", 0f);
                report.AppendLine("map " + Map + (lit ? ", its own daylight" : ", lighting NOT put in force") + "; PetGhosts on for the film only");

                GameObject In(GameObject go) { SceneManager.MoveGameObjectToScene(go, scene); owned.Add(go); return go; }
                Vector3 Ground(float x, float z)
                {
                    var at = new Vector3(x, 0f, z);
                    if (physics.IsValid() && physics.Raycast(new Vector3(x, 30f, z), Vector3.down, out var hit, 80f, ~0, QueryTriggerInteraction.Ignore)) at.y = hit.point.y;
                    return at;
                }
                GameObject Person(string id, Vector3 at, float yaw)
                {
                    var holder = In(new GameObject("~PetGhostFilm-" + id));
                    holder.transform.SetPositionAndRotation(at, Quaternion.Euler(0f, yaw, 0f));
                    var art = RosterBook.Load()?.FindPersonArt(id);
                    if (art == null || art.Model == null) { report.AppendLine("  " + id + " not found"); return holder; }
                    var model = (GameObject)Object.Instantiate(art.Model, holder.transform);
                    model.transform.localRotation = Quaternion.Euler(0f, CharacterVisual.PersonModelYaw, 0f);
                    model.transform.localScale = Vector3.one * CharacterVisual.PersonScale;
                    ToonSkin.Apply(model, ToonSkin.PersonOutlineWidth, art.Palette);
                    foreach (var a in model.GetComponentsInChildren<Animator>(true)) a.enabled = false;
                    return holder;
                }

                var camGo = In(new GameObject("~PetGhostFilmCamera"));
                var cam = camGo.AddComponent<Camera>();
                cam.scene = scene; cam.enabled = false; cam.allowHDR = false; cam.nearClipPlane = .03f; cam.farClipPlane = 400f;
                cam.clearFlags = RenderSettings.skybox != null ? CameraClearFlags.Skybox : CameraClearFlags.SolidColor;
                cam.backgroundColor = new Color(.55f, .70f, .88f);
                var plain = new RenderTexture(CellWidth, CellHeight, 24, RenderTextureFormat.ARGB32) { antiAliasing = 4 };
                var cell = new Texture2D(CellWidth, CellHeight, TextureFormat.RGB24, false);
                owned.Add(plain); owned.Add(cell);
                Color[] Shoot(Vector3 eye, Vector3 aim, float fov)
                {
                    cam.fieldOfView = fov;
                    cam.transform.SetPositionAndRotation(eye, Quaternion.LookRotation(aim - eye, Vector3.up));
                    cam.targetTexture = plain; cam.Render();
                    var keep = RenderTexture.active; RenderTexture.active = plain;
                    cell.ReadPixels(new Rect(0, 0, CellWidth, CellHeight), 0, 0); cell.Apply(); RenderTexture.active = keep;
                    return cell.GetPixels();
                }

                // ---- the cast of the film
                Vector3 spot = Ground(0f, -2f);
                var nemu = Person("nemu", spot + new Vector3(-2.2f, 0f, -.6f), 60f);
                var victim = Person("dante", spot + new Vector3(2.6f, 0f, 0f), -90f);
                float victimHead = VictimHeadHeight;
                foreach (var r in victim.GetComponentsInChildren<Renderer>(true))
                    if (r.name.StartsWith("head")) { victimHead = r.bounds.center.y - victim.transform.position.y; break; }
                report.AppendLine("  the passer-by's head middle: " + victimHead.ToString("0.00") + " m (NemuBlindfold uses " + VictimHeadHeight + " m in a match)");
                var holderGo = In(new GameObject("~PetGhosts"));
                var holder = holderGo.transform;

                // DON'T LOOK: the mouse, drawn exactly as `NemuBlindfold` draws it (its mesh, its material for that mesh).
                var mouseGo = new GameObject("BlindfoldBody"); mouseGo.transform.SetParent(holder, false);
                var mouseFilter = mouseGo.AddComponent<MeshFilter>(); mouseFilter.sharedMesh = BlindfoldGhostModel.Waiting;
                var mouse = mouseGo.AddComponent<MeshRenderer>();
                mouse.sharedMaterial = PetGhostModels.MaterialFor(mouseFilter.sharedMesh, null);
                mouse.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                var block = new MaterialPropertyBlock();

                // SKITTER and LOOKOUT through `NemuGhostBody`, the class the skills use, arms and lantern hung on the body.
                var bear = new NemuGhostBody(holder, "SkitterBody", NemuGhostModels.Skitter, new Color(1f, .93f, .86f));
                var bearArm = new NemuGhostBody[2];
                for (int s = 0; s < 2; s++) bearArm[s] = new NemuGhostBody(bear.Transform, "SkitterArm" + s, NemuGhostModels.SkitterArm(s), new Color(1f, .93f, .86f));
                var owl = new NemuGhostBody(holder, "LookoutBody", NemuGhostModels.Lookout, new Color(1f, .96f, .80f));
                var owlArm = new NemuGhostBody[2];
                for (int s = 0; s < 2; s++) owlArm[s] = new NemuGhostBody(owl.Transform, "LookoutArm" + s, NemuGhostModels.LookoutArm(s), new Color(1f, .96f, .80f));
                var lantern = new NemuGhostBody(owl.Transform, "LookoutLantern", NemuGhostModels.Lantern, new Color(1f, .96f, .80f));

                // The tsinelas Skitter fetches, and a stand-in can for the Lookout.
                GameObject shoe = null; float shoeLength = .26f;
                var slipperArt = RosterBook.Load()?.SlipperArt(0);
                if (slipperArt != null && slipperArt.Model != null)
                {
                    shoe = In((GameObject)Object.Instantiate(slipperArt.Model));
                    foreach (var c in shoe.GetComponentsInChildren<Collider>(true)) Object.DestroyImmediate(c);
                    ToonSkin.ApplySlipper(shoe, ToonSkin.PropOutlineWidth);
                    var b = new Bounds(); bool any = false;
                    foreach (var r in shoe.GetComponentsInChildren<Renderer>(true)) { if (any) b.Encapsulate(r.bounds); else { b = r.bounds; any = true; } }
                    shoeLength = Mathf.Max(b.size.x, b.size.z);
                    report.AppendLine("  tsinelas " + shoeLength.ToString("0.00") + " m (" + (b.size.x > b.size.z ? "long along x" : "long along z") + ")");
                }
                var can = In(GameObject.CreatePrimitive(PrimitiveType.Cylinder));
                Object.DestroyImmediate(can.GetComponent<Collider>());
                can.transform.localScale = new Vector3(.24f, .16f, .24f);
                var canMat = new Material(Shader.Find("Unlit/Color")) { color = new Color(.55f, .58f, .62f) }; owned.Add(canMat);
                can.GetComponent<MeshRenderer>().sharedMaterial = canMat;
                report.AppendLine("  the can is a grey stand-in cylinder; people are still roster poses");

                // ---- the film v2: three acts, each ghost flying round its point (`PetGhostFlight`, as the skills now do with the
                // switch on) until it acts, when it is back on the point; faded out outside its own act.
                const float A0 = 0f, A1 = 6.5f, B1 = 13f, C1 = 20f;
                Vector3 mouseHome = spot + new Vector3(.9f, 0f, .2f);
                Vector3 shoeRest = spot + new Vector3(.4f, .02f, 1.4f);
                Vector3 canAt = spot + new Vector3(.2f, 0f, -.4f);
                var mouseFlight = new PetGhostFlight(PetGhostFlight.Kind.Mouse);
                var bearFlight = new PetGhostFlight(PetGhostFlight.Kind.Bear);
                var owlFlight = new PetGhostFlight(PetGhostFlight.Kind.Owl);
                const float dt = 1f / Rate;
                const float Spring = 3.2f;

                void Pose(float t, out Vector3 focus)
                {
                    focus = spot;
                    // DON'T LOOK: darts round its spot; at 3.0 s it is back on the point, at 3.2 s it springs at the passer-by's
                    // face, clings, lets go.
                    float tm = t - A0;
                    bool inMouse = t < A1;
                    victim.transform.position = spot + new Vector3(2.9f - Mathf.Clamp(tm, 0f, Spring) * .5f, 0f, 0f);
                    // Off the court once the mouse is done, so he does not stand in the next acts' shots.
                    if (victim.activeSelf != inMouse) victim.SetActive(inMouse);
                    // His face at HIS head's middle (the roster pose stands lower than the 1.38 m `NemuBlindfold` measured in a match).
                    Vector3 face = victim.transform.position + Vector3.up * victimHead;
                    // Straight ahead of his face, as `NemuBlindfold.AheadOf` puts it (v2 c3 aimed at the mouse's spot, so it sat to one side).
                    Vector3 toward = victim.transform.forward; toward.y = 0f; toward.Normalize();
                    Vector3 faceFront = face + toward * (VictimHeadHalfDepth + BlindfoldGhostModel.HeadHalfDepth - .03f);
                    var mouseFacing = Quaternion.LookRotation(-toward, Vector3.up);
                    bool sprung = tm >= Spring;
                    var mesh = sprung ? BlindfoldGhostModel.Sprung : BlindfoldGhostModel.Waiting;
                    if (mouseFilter.sharedMesh != mesh) { mouseFilter.sharedMesh = mesh; mouse.sharedMaterial = PetGhostModels.MaterialFor(mesh, null); }
                    Vector3 wait = mouseHome + Vector3.up * (.1f + .03f * Mathf.Sin(tm * 2.4f));
                    Vector3 on = faceFront - mouseFacing * BlindfoldGhostModel.HeadCentre;
                    float leap = Smooth(Spring, Spring + .45f, tm);
                    Vector3 at = Vector3.Lerp(wait, on, leap) + Vector3.up * Mathf.Sin(leap * Mathf.PI) * .35f;
                    if (tm > Spring + .45f && tm < Spring + 1.9f) at += mouseFacing * new Vector3(Mathf.Sin(tm * 37f), Mathf.Sin(tm * 29f), 0f) * .012f;
                    var mFacing = sprung ? mouseFacing : Quaternion.LookRotation(Vector3.back, Vector3.up);
                    mouseFlight.Apply(ref at, ref mFacing, tm, tm < Spring - .2f ? 1f : 0f, dt);
                    float fadeM = inMouse ? Smooth(0f, .35f, tm) * (1f - Smooth(A1 - .9f, A1 - .3f, tm)) : 0f;
                    mouse.transform.SetPositionAndRotation(at, mFacing);
                    mouse.enabled = fadeM > .005f;
                    block.SetFloat("_Fade", fadeM); block.SetFloat("_Clock", tm); block.SetFloat("_Sway", .05f);
                    block.SetFloat("_Flash", sprung && tm < Spring + .15f ? .6f : 0f);
                    block.SetVector("_Trail", mouseFlight.TrailLocal);
                    mouse.SetPropertyBlock(block);
                    if (inMouse) focus = Vector3.Lerp(mouseHome + Vector3.up * .4f, face, leap);

                    // SKITTER: circles by the loose slipper, comes back to it, grabs its two ends (a happy spin), carries it to
                    // Nemu and offers it up.
                    float tb = t - A1;
                    bool inBear = t >= A1 && t < B1;
                    float fadeB = inBear ? Smooth(0f, .35f, tb) * (1f - Smooth(5.9f, 6.5f, tb)) : 0f;
                    const float Grab = 2.6f;
                    Vector3 nemuHand = nemu.transform.position + nemu.transform.forward * .6f;
                    float go = Smooth(Grab + .6f, Grab + 2.8f, tb);
                    Vector3 path = Vector3.Lerp(shoeRest - Vector3.up * .02f + new Vector3(0f, 0f, -.3f), new Vector3(nemuHand.x, spot.y, nemuHand.z), go);
                    Vector3 travel = (new Vector3(nemuHand.x, 0f, nemuHand.z) - new Vector3(shoeRest.x, 0f, shoeRest.z)).normalized;
                    var bearFacing = Quaternion.LookRotation(go > .02f && go < .98f ? travel : (tb < Grab + .6f ? Vector3.forward : -travel), Vector3.up);
                    float bob = .06f + .03f * Mathf.Sin(tb * 7f) * Mathf.Clamp01(go * 4f);
                    Vector3 bAt = path + Vector3.up * (bob - (1f - Smooth(0f, .5f, tb)) * .3f);
                    float offer = Smooth(Grab + 2.8f, Grab + 3.2f, tb);
                    Vector3 lift = new Vector3(0f, .1f, .08f) * offer;
                    float g = Mathf.Clamp01((tb - Grab) / .45f);
                    bearFlight.Apply(ref bAt, ref bearFacing, tb, tb < Grab - .45f ? 1f : 0f, dt);
                    if (tb >= Grab && g < 1f) bearFacing *= Quaternion.AngleAxis(360f * g * g * (3f - 2f * g), Vector3.up);
                    bear.Trail = bearFlight.TrailLocal;
                    bear.Pose(bAt, bearFacing, 1f, fadeB, tb, .06f + go * .03f);
                    float grab = Smooth(Grab - .3f, Grab, tb);
                    for (int s = 0; s < 2; s++)
                    {
                        float sign = s == 0 ? -1f : 1f;
                        Vector3 shoulder = NemuGhostModels.SkitterShoulder[s];
                        Vector3 rest = shoulder + new Vector3(sign * .03f, -.05f + .03f * Mathf.Sin(tb * 6f + s), .07f);
                        Vector3 hand = Vector3.Lerp(rest, NemuGhostModels.SkitterHands + lift + Vector3.right * sign * (shoeLength * .5f + .015f), grab);
                        Vector3 to = hand - shoulder; float len = Mathf.Max(to.magnitude, 1e-4f);
                        bearArm[s].PoseLocal(shoulder, Quaternion.LookRotation(to / len, Vector3.up), fadeB, tb);
                        bearArm[s].Transform.localScale = new Vector3(1f, 1f, Mathf.Clamp(len / NemuGhostModels.SkitterHand.z, .5f, PetGhostModels.MaxArmStretch));
                    }
                    if (shoe != null)
                    {
                        shoe.SetActive(t < B1);
                        Vector3 held = bear.Transform.TransformPoint(NemuGhostModels.SkitterHands + lift);
                        var lie = Quaternion.LookRotation(Vector3.forward, Vector3.up);
                        var across = bear.Transform.rotation * Quaternion.Euler(0f, 90f, 0f);
                        shoe.transform.SetPositionAndRotation(Vector3.Lerp(shoeRest, held, grab), Quaternion.Slerp(lie, across, grab));
                    }
                    if (inBear) focus = path + Vector3.up * .35f;

                    // LOOKOUT: glides round its minding spot with the lantern out, comes back, heaves the can upright, cheers.
                    float tl = t - B1;
                    bool inOwl = t >= B1;
                    float fadeL = inOwl ? Smooth(0f, .35f, tl) * (1f - Smooth(6.4f, 7f, tl)) : 0f;
                    const float Heave = 3.8f;
                    float heave = Smooth(Heave, Heave + .4f, tl) * (1f - Smooth(Heave + 1.8f, Heave + 2.2f, tl));
                    float cheer = Smooth(Heave + 2.2f, Heave + 2.5f, tl) * (1f - Smooth(Heave + 2.9f, Heave + 3.2f, tl));
                    var owlFacing = Quaternion.LookRotation(Vector3.back, Vector3.up);
                    Vector3 oAt = canAt + new Vector3(0f, .05f + .03f * Mathf.Sin(tl * 2.2f), .34f - .12f * heave);
                    owlFlight.Apply(ref oAt, ref owlFacing, tl, tl < Heave - .4f || tl > Heave + 3.3f ? 1f : 0f, dt);
                    owl.Trail = owlFlight.TrailLocal;
                    owl.Pose(oAt, owlFacing, 1f, fadeL, tl, .05f + .04f * heave);
                    float tip = 1f - Smooth(Heave + .4f, Heave + 1.6f, tl);
                    can.transform.SetPositionAndRotation(canAt + Vector3.up * Mathf.Lerp(.16f, .12f, tip), Quaternion.Euler(78f * tip, 0f, 0f));
                    Vector3 top = can.transform.position + can.transform.up * .16f;
                    for (int s = 0; s < 2; s++)
                    {
                        float sign = s == 0 ? -1f : 1f;
                        Vector3 shoulder = NemuGhostModels.LookoutShoulder[s];
                        Vector3 hand = s == 0 ? shoulder + new Vector3(-.05f, -.10f, .04f + .03f * Mathf.Sin(tl * 2f))
                                              : shoulder + new Vector3(.08f, .02f + .02f * Mathf.Sin(tl * 2.4f), .08f);
                        Vector3 grip = owl.Transform.InverseTransformPoint(top + Vector3.right * sign * .06f + Vector3.up * .02f);
                        hand = Vector3.Lerp(hand, grip, heave);
                        hand = Vector3.Lerp(hand, shoulder + new Vector3(sign * .07f, .12f, .03f), cheer);
                        Vector3 to = hand - shoulder; float len = Mathf.Max(to.magnitude, 1e-4f);
                        owlArm[s].PoseLocal(shoulder, Quaternion.LookRotation(to / len, Vector3.up), fadeL, tl);
                        owlArm[s].Transform.localScale = new Vector3(1f, 1f, Mathf.Clamp(len / NemuGhostModels.LookoutHandOnArm.z, .5f, PetGhostModels.MaxArmStretch));
                    }
                    Vector3 handAt = owl.Transform.InverseTransformPoint(owlArm[1].Transform.TransformPoint(NemuGhostModels.LookoutHandOnArm));
                    float swing = Mathf.Sin(tl * 3.1f) * 12f;
                    lantern.PoseLocal(handAt, Quaternion.Euler(0f, 0f, swing), fadeL, tl);
                    if (inOwl) focus = canAt + new Vector3(0f, .45f, .2f);
                }

                string frames = Path.Combine(OutDir, "petghost_" + tag + "_frames");
                if (Directory.Exists(frames)) foreach (string old in Directory.GetFiles(frames)) File.Delete(old);
                Directory.CreateDirectory(frames);
                var sheet = new Texture2D(CellWidth * 2, CellHeight, TextureFormat.RGB24, false);
                owned.Add(sheet);
                int count = Mathf.RoundToInt(C1 * Rate);
                Vector3 eased = spot;
                for (int f = 0; f <= count; f++)
                {
                    float t = f / (float)Rate;
                    Pose(t, out var focus);
                    eased = f == 0 ? focus : Vector3.Lerp(eased, focus, .25f);
                    // A player standing about three metres off at eye height, and a wide view of the court from higher up.
                    // From the front-left, the side the ghosts face and the passer-by's face turns to (v1 stood behind Dante).
                    // Per act: the mouse from in front of the passer-by (so the spring lands on his face), the bear from Nemu's
                    // front, the owl from the can's front.
                    Vector3 side = (t < A1 ? new Vector3(-1f, 0f, -.35f) : t < B1 ? new Vector3(.75f, 0f, -1f) : new Vector3(-.55f, 0f, -1f)).normalized;
                    sheet.SetPixels(0, 0, CellWidth, CellHeight, Shoot(eased + side * 2.6f + Vector3.up * (1.45f - eased.y + spot.y), eased, 50f));
                    sheet.SetPixels(CellWidth, 0, CellWidth, CellHeight, Shoot(spot + new Vector3(4.5f, 4.2f, -6.5f), spot + new Vector3(.1f, .4f, .2f), 42f));
                    sheet.Apply();
                    File.WriteAllBytes(Path.Combine(frames, "f" + f.ToString("0000") + ".png"), sheet.EncodeToPNG());
                }
                report.AppendLine("  " + (count + 1) + " frames at " + Rate + " a second in " + frames.Replace('\\', '/') + "; views: player (left), wide (right)");
                report.AppendLine("  acts: DON'T LOOK 0-6.5 s (springs at 3.2), SKITTER 6.5-13 s (grabs at 9.1), LOOKOUT 13-20 s (heaves at 16.8)");
                File.WriteAllText(done, report.ToString());
            }
            catch (Exception failure)
            {
                File.WriteAllText(failed, report + "\n" + failure);
                Debug.LogError("[NemuPetGhostFilm] " + failure);
            }
            finally
            {
                PetGhostModels.PetGhosts = was;
                if (lit) Unsupported.RestoreOverrideLightingSettings();
                RenderTexture.active = null;
                foreach (var thing in owned)
                {
                    if (thing == null) continue;
                    if (thing is RenderTexture rt) rt.Release();
                    Object.DestroyImmediate(thing);
                }
                if (scene.IsValid()) EditorSceneManager.ClosePreviewScene(scene);
                // The bodies are made in the open scene and moved into the film's at once; take off any changed mark that left.
                if (active.isLoaded && active.isDirty && !wasDirty)
                    typeof(EditorSceneManager).GetMethod("ClearSceneDirtiness", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public)?.Invoke(null, new object[] { active });
            }
        }
    }
}
