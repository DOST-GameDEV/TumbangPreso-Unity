using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using TumbangPreso.Core;
using TumbangPreso.Visual;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;

namespace TumbangPreso.EditorTools.MapKit
{
    /// <summary>
    /// NEMU'S THREE REWORKED SKILL GHOSTS ON THE COURT, OLD AGAINST NEW (PEEKER the mint mouse, FETCHER the cocoa bear, KEEPER the
    /// butter owl; LET THEM IN is not here). One act a skill, eight seconds each, Eskinita in its own daylight, no bloom, 60
    /// frames a second (`docs/VFX_BENCH_METHOD.md` section 10.1: judged by motion, four views, 1x).
    ///
    /// Each frame is a sheet of eight cells: the TOP row is OLD (what a match drew before the look pass: `PetGhostModels.PetGhosts`
    /// and `NemuGhostLook.Painted` both false, ASTRA's voxel ghosts with their main dot lights), the BOTTOM row is NEW (both
    /// true: the pet ghosts, their unfold, the bear's barrel roll, the hue-keeping shade and the painted puffs). The four
    /// columns are the four views: CASTER (Nemu's own first-person eyes), AFFECTED (the victim's eyes for PEEKER, the taya at
    /// the can for FETCHER, the attacker who knocked the can for KEEPER), SIDE, ABOVE.
    ///
    /// ⚠️ STAGED, AND SAID SO (as `NemuPetGhostFilm` is). No match runs, so the skills' own loops do not drive the bodies. What IS
    /// the game's: the meshes, materials and attach points with each switch as set; every NEW puff, written by the very
    /// `NemuGhostLook` calls the skills make, at the moments the skills make them, into a field of the same kind; the unfold
    /// curve and the barrel roll (`NemuGhostLook.Unfold`, `BearWeave`); the rule numbers (`NecroRules`: arm 1.5 s, lunge
    /// 0.22 s, blind 2.5 s, trigger 2 m, fetch 9 m/s, lift 1.5 s). What is NOT: the choreography is typed here from the
    /// skills' code (their poses copied, not called), the people are roster poses, the can is a stand-in, the trigger ring,
    /// the victim's darkening (`DontLookBlind`) and the dormant globe are not drawn, and the OLD row carries only ASTRA's main
    /// lights (aura and eyes), not every dot.
    ///
    /// k2 (owner 2026-10-29 on k1: the mouse *"to stiff ... a single rigid model"*, the bear *"doesnt look too ghosty"*,
    /// *"looks/acts"*): the NEW row's mouse and bear are the parts rigs (`PetGhostRig`) acted by `PetGhostActing` and flown by
    /// `BearGlide`, the skills' own calls; a FIFTH column is a close-up 1.5 m off the act's ghost, for its face and motion.
    /// `faces &lt;tag&gt;` renders the expression and turnaround sheet of the parts instead (`kit_faces_&lt;tag&gt;.png`).
    ///
    /// Request: lines in `Temp/nemu-kit-film.request`: `ping`, `faces &lt;tag&gt;`, or `film &lt;tag&gt; [all|peeker|fetcher|keeper|a,b]`. Frames go to
    /// `Logs/nemu_ghosts/kit_&lt;tag&gt;_frames/`, then `done_kit_&lt;tag&gt;.txt` (with the costs) or `failed_kit_&lt;tag&gt;.txt`.
    /// </summary>
    [InitializeOnLoad]
    internal static class NemuKitFilmPoll
    {
        private const string Request = "Temp/nemu-kit-film.request";
        private static double _next;
        static NemuKitFilmPoll() { EditorApplication.update += Poll; }

        private static void Poll()
        {
            if (EditorApplication.timeSinceStartup < _next) return;
            _next = EditorApplication.timeSinceStartup + 1.0;
            if (!File.Exists(Request) || EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling) return;
            AssetDatabase.Refresh();
            if (EditorApplication.isCompiling) return;
            string[] lines = File.ReadAllLines(Request);
            File.Delete(Request);
            foreach (string line in lines)
            {
                string[] words = line.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                if (words.Length == 0) continue;
                Directory.CreateDirectory(NemuKitFilm.OutDir);
                if (words[0] == "ping") { File.WriteAllText(Path.Combine(NemuKitFilm.OutDir, "done_kit_ping.txt"), DateTime.Now.ToString("HH:mm:ss")); continue; }
                if (words[0] == "film") NemuKitFilm.Run(words.Length > 1 ? words[1] : "k1", words.Length > 2 ? words[2] : "all");
                if (words[0] == "faces") NemuKitFilm.Run(words.Length > 1 ? words[1] : "f1", "faces");
            }
        }
    }

    internal static class NemuKitFilm
    {
        internal const string OutDir = "Logs/nemu_ghosts";
        private const string Map = "Assets/TumbangPreso/Scenes/Maps/Eskinita.unity";
        private const int CellWidth = 480, CellHeight = 270, Rate = 60, ViewCount = 5;
        private const float Act = 8f, Eyes = 1.55f;
        // `NemuBlindfold`'s private numbers, copied (its victim's head, its lens pose).
        private const float VictimHeadHeight = 1.38f, VictimHeadHalfDepth = .28f, LensDistance = .78f, LensScale = 2f,
                            BlindLeave = .45f, ShimmerSeen = .16f, OwnerSeen = .45f;

        private static float Smooth(float a, float b, float x) { float t = Mathf.Clamp01((x - a) / (b - a)); return t * t * (3f - 2f * t); }
        private static Vector3 Flat(Vector3 v) { v.y = 0f; return v.sqrMagnitude > 1e-6f ? v.normalized : Vector3.forward; }

        /// <summary>One row's ghosts: OLD (both switches false) or NEW (both true), each built and posed under its own switches.</summary>
        private sealed class Set
        {
            public bool New;
            public Transform Root;
            public MeshRenderer Mouse; public MeshFilter MouseFilter; public Mesh MouseWait, MouseSprung; public Material MouseFallback;
            public MaterialPropertyBlock MouseBlock = new MaterialPropertyBlock();
            public NemuGhostBody Bear, Owl, Lantern;
            public NemuGhostBody[] BearArm = new NemuGhostBody[2], OwlArm = new NemuGhostBody[2];
            public PetGhostFlight MouseFlight = new PetGhostFlight(PetGhostFlight.Kind.Mouse), BearFlight = new PetGhostFlight(PetGhostFlight.Kind.Bear),
                                  OwlFlight = new PetGhostFlight(PetGhostFlight.Kind.Owl);
            public NemuSmokeBatch Dots;
            public double PoseMs; public int Posed;
            public PetGhostRig MouseRig, BearRig; public BearGlide Glide = new BearGlide(); public bool MouseLive;
            public Vector3 Close; public bool CloseSet;
            public float MouseFade;
        }

        private static void Use(Set s) { PetGhostModels.PetGhosts = s.New; NemuGhostLook.Painted = s.New; PetGhostRig.Enabled = s.New; }

        internal static void Run(string tag, string which)
        {
            tag = new string((tag ?? "k1").Where(c => char.IsLetterOrDigit(c) || c == '-' || c == '_').ToArray());
            string done = Path.Combine(OutDir, "done_kit_" + tag + ".txt"), failed = Path.Combine(OutDir, "failed_kit_" + tag + ".txt");
            if (File.Exists(done)) File.Delete(done);
            if (File.Exists(failed)) File.Delete(failed);
            var report = new System.Text.StringBuilder();
            var owned = new List<Object>();
            bool wasPets = PetGhostModels.PetGhosts, wasPainted = NemuGhostLook.Painted, wasRig = PetGhostRig.Enabled, lit = false;
            Scene scene = default;
            var active = SceneManager.GetActiveScene();
            bool wasDirty = active.isLoaded && active.isDirty;
            CloudPuffField puffs = null;
            var sets = new Set[2];
            try
            {
                scene = EditorSceneManager.OpenPreviewScene(Map);
                var physics = scene.GetPhysicsScene();
                lit = Unsupported.SetOverrideLightingSettings(scene);
                Shader.SetGlobalFloat("_CharacterSmoothShade", ToonSkin.CharacterSmoothShade);
                Shader.SetGlobalFloat("_FxStudyBloom", 0f);
                report.AppendLine("map " + Map + (lit ? ", its own daylight" : ", lighting NOT put in force") + ", no bloom, " + Rate + " fps, 1x");
                report.AppendLine("rows: TOP = OLD (PetGhosts false, Painted false), BOTTOM = NEW (both true); columns: caster, affected, side, above");

                GameObject In(GameObject go) { SceneManager.MoveGameObjectToScene(go, scene); owned.Add(go); return go; }
                Vector3 Ground(float x, float z)
                {
                    var at = new Vector3(x, 0f, z);
                    if (physics.IsValid() && physics.Raycast(new Vector3(x, 30f, z), Vector3.down, out var hit, 80f, ~0, QueryTriggerInteraction.Ignore)) at.y = hit.point.y;
                    return at;
                }
                GameObject Person(string id, Vector3 at, float yaw)
                {
                    var holder = In(new GameObject("~KitFilm-" + id));
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

                var camGo = In(new GameObject("~KitFilmCamera"));
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
                    if (Mathf.Abs(Vector3.Dot((aim - eye).normalized, Vector3.up)) > .99f) cam.transform.rotation = Quaternion.LookRotation(aim - eye, Vector3.forward);
                    cam.targetTexture = plain; cam.Render();
                    var keep = RenderTexture.active; RenderTexture.active = plain;
                    cell.ReadPixels(new Rect(0, 0, CellWidth, CellHeight), 0, 0); cell.Apply(); RenderTexture.active = keep;
                    return cell.GetPixels();
                }

                // ---- the court: one stretch of Eskinita for all three acts.
                Vector3 spot = Ground(0f, -2f);
                var nemu = Person("nemu", spot + new Vector3(-2.4f, 0f, -.8f), 70f);
                var victim = Person("dante", spot + new Vector3(4f, 0f, .6f), -90f);
                var taya = Person("cheska", spot + new Vector3(1.2f, 0f, 3.2f), 180f);
                var attacker = Person("rafi", spot + new Vector3(3.2f, 0f, -1.8f), -60f);
                Vector3 shoeRest = Ground(spot.x + 3.4f, spot.z + 2.2f) + Vector3.up * .02f;
                Vector3 canAt = Ground(spot.x + .8f, spot.z + 1.6f);

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
                }
                var can = In(GameObject.CreatePrimitive(PrimitiveType.Cylinder));
                Object.DestroyImmediate(can.GetComponent<Collider>());
                can.transform.localScale = new Vector3(.24f, .16f, .24f);
                var canMat = new Material(Shader.Find("Unlit/Color")) { color = new Color(.55f, .58f, .62f) }; owned.Add(canMat);
                can.GetComponent<MeshRenderer>().sharedMaterial = canMat;

                var blindfoldShader = Shader.Find("TumbangPreso/BlindfoldGhost");
                for (int i = 0; i < 2; i++)
                {
                    var s = sets[i] = new Set { New = i == 1 };
                    Use(s);
                    s.Root = In(new GameObject(s.New ? "~KitNew" : "~KitOld")).transform;
                    var mouseGo = new GameObject("BlindfoldBody"); mouseGo.transform.SetParent(s.Root, false);
                    s.MouseFilter = mouseGo.AddComponent<MeshFilter>();
                    s.MouseWait = BlindfoldGhostModel.Waiting; s.MouseSprung = BlindfoldGhostModel.Sprung;
                    s.MouseFilter.sharedMesh = s.MouseWait;
                    s.MouseFallback = blindfoldShader != null ? new Material(blindfoldShader) { name = "BlindfoldGhost-film" } : null;
                    if (s.MouseFallback != null) owned.Add(s.MouseFallback);
                    s.Mouse = mouseGo.AddComponent<MeshRenderer>();
                    s.Mouse.sharedMaterial = PetGhostModels.MaterialFor(s.MouseWait, s.MouseFallback);
                    s.Mouse.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                    var bearRim = new Color(.55f, 1f, .5f); var owlRim = new Color(1f, .8f, .5f);
                    s.Bear = new NemuGhostBody(s.Root, "SkitterBody", NemuGhostModels.Skitter, bearRim);
                    for (int a = 0; a < 2; a++) s.BearArm[a] = new NemuGhostBody(s.Bear.Transform, "SkitterArm" + a, NemuGhostModels.SkitterArm(a), bearRim);
                    s.Owl = new NemuGhostBody(s.Root, "LookoutBody", NemuGhostModels.Lookout, owlRim);
                    for (int a = 0; a < 2; a++) s.OwlArm[a] = new NemuGhostBody(s.Owl.Transform, "LookoutArm" + a, NemuGhostModels.LookoutArm(a), owlRim);
                    s.Lantern = new NemuGhostBody(s.Owl.Transform, "LookoutLantern", NemuGhostModels.Lantern, owlRim);
                    if (!s.New) s.Dots = new NemuSmokeBatch(s.Root, "KitOldDots", 2, 12, 1);
                    if (s.New && PetGhostRig.On)
                    {
                        s.MouseRig = PetGhostRig.Build(s.Mouse.transform, PetGhostRig.Pet.Mouse, Color.white);
                        s.BearRig = PetGhostRig.Build(s.Bear.Transform, PetGhostRig.Pet.Bear, bearRim);
                        if (s.BearRig != null) s.Bear.MeshHidden = true;
                        report.AppendLine("  NEW parts rigs: mouse " + (s.MouseRig != null ? s.MouseRig.Renderers + " parts" : "MISSING")
                            + ", bear " + (s.BearRig != null ? s.BearRig.Renderers + " parts" : "MISSING"));
                    }
                }
                var puffHolder = In(new GameObject("~KitPuffs"));
                puffs = new CloudPuffField(puffHolder.transform, "Nemu kit film puffs");
                report.AppendLine("  NEW puffs: their own CloudPuffField (" + CloudPuffField.Slots + " rows, " + CloudPuffField.Triangles + " triangles if every row were live)");

                // ---- the staged rule state, shared by both rows (the look differs, the rule does not)
                int first = 2, last = 0;
                foreach (var act in (which ?? "all").Split(','))
                {
                    int a = act == "peeker" ? 0 : act == "fetcher" ? 1 : act == "keeper" ? 2 : -1;
                    if (a < 0) { first = 0; last = 2; break; }
                    first = Mathf.Min(first, a); last = Mathf.Max(last, a);
                }
                if (which == "faces") { Faces(tag, sets[1], spot, Shoot, report, done); return; }
                float filmStart = first * Act, filmEnd = (last + 1) * Act;
                // PEEKER: placed at 0.3 s on its spot, facing down Nemu's aim; the victim walks in at 1.4 m/s from 2.0 s and trips at 2 m.
                Vector3 mouseSpot = spot + new Vector3(1.2f, 0f, .3f);
                Vector3 mouseFacing = Flat(mouseSpot - nemu.transform.position);
                Vector3 victimFrom = mouseSpot + new Vector3(4.6f, 0f, .4f);
                Vector3 victimWay = Flat(mouseSpot - victimFrom);
                float tripAt = 2f + (Vector3.Distance(Flat3(victimFrom), Flat3(mouseSpot)) - NecroRules.DontLookTriggerRadius + .05f) / 1.4f;
                // FETCHER: summoned at 0.3 s beside her; 9 m/s to the slipper, grab, 9 m/s back, offers, she takes it at 5.2 s.
                const float FetchCast = .3f, TakeAt = 5.2f;
                // KEEPER: summoned at 0.3 s beside the can; the can goes down at 2.0 s; the 1.5 s lift; up at 3.5 s; it leaves at 6.8 s.
                const float KeepCast = .3f, KnockAt = 2f, KeepLeave = 6.8f;
                float upAt = KnockAt + NecroRules.LookoutRestoreSeconds;

                // The bear's rule state, stepped once a frame like `NemuHeroKit.Skitter.OnTick`.
                Vector3 bearPos = Vector3.zero, bearLast = Vector3.zero; Quaternion bearFacing = Quaternion.identity;
                bool carrying = false, bearLeaving = false; float grabAt = -10f, leftAt = -10f, bearSpeed = 0f, offer = 0f;
                Vector3? reachFor = null;
                // The owl's drawn state (`NemuLookout`'s soft follow and its eased grip).
                Vector3 owlAt = Vector3.zero; float owlLift = 0f, owlEngage = 0f, cheerAt = -10f; bool owlPlaced = false;
                Vector3 owlSide = Flat(nemu.transform.position - canAt);
                Quaternion owlFacing = Quaternion.LookRotation(-owlSide);

                float prevT = filmStart - 1f / Rate;
                bool Crossed(float moment, float t) => prevT < moment && t >= moment;
                void At(float moment, float t, Action spawn)
                {
                    if (!Crossed(moment, t)) return;
                    NemuGhostLook.Stage(puffs, moment);
                    spawn();
                    NemuGhostLook.Stage(null, 0f);
                }

                // ---- one frame of every act, for one row
                void PoseSet(Set s, float t, float dt)
                {
                    var watch = Stopwatch.StartNew();
                    Use(s);
                    s.Dots?.Begin();
                    // =========================== PEEKER
                    float u = t - 0f;
                    bool inA = t < Act;
                    float age = u - .3f;
                    s.Mouse.enabled = false; s.MouseFade = 0f; s.MouseRig?.Visible(false);
                    if (inA && age >= 0f)
                    {
                        float since = u - tripAt;
                        bool sprung = since >= 0f;
                        bool covering = since >= NecroRules.DontLookLungeSeconds && since < NecroRules.DontLookBlindSeconds;
                        bool leaving = since >= NecroRules.DontLookBlindSeconds;
                        float leaveU = leaving ? Mathf.Clamp01((since - NecroRules.DontLookBlindSeconds) / BlindLeave) : 0f;
                        Vector3 ahead = victim.transform.forward; ahead.y = 0f; ahead.Normalize();
                        Vector3 face = victim.transform.position + Vector3.up * VictimHeadHeight + ahead * (VictimHeadHalfDepth + BlindfoldGhostModel.HeadHalfDepth - .03f);
                        Quaternion facing; Vector3 at; float scale = 1f, flash = 0f, sway = .05f, fade;
                        float rise = Mathf.SmoothStep(0f, 1f, age / .35f);
                        Vector3 baseAt = mouseSpot + Vector3.up * (.1f + .05f * Mathf.Sin(age * 2.1f) - .9f * (1f - rise));
                        if (!sprung)
                        {
                            facing = Quaternion.LookRotation(Quaternion.AngleAxis(14f * Mathf.Sin(age * .8f), Vector3.up) * mouseFacing);
                            at = baseAt;
                            if (NemuGhostLook.On) scale *= NemuGhostLook.Unfold(age);
                            fade = 1f; // per view below
                        }
                        else
                        {
                            facing = Quaternion.LookRotation(-ahead);
                            Vector3 on = face - facing * BlindfoldGhostModel.HeadCentre;
                            if (!covering && !leaving)
                            {
                                float lu = Mathf.Clamp01(since / NecroRules.DontLookLungeSeconds), e = lu * lu * (3f - 2f * lu);
                                at = Vector3.Lerp(mouseSpot + Vector3.up * .1f, on, e) + Vector3.up * (.3f * Mathf.Sin(lu * Mathf.PI));
                                flash = .55f * (1f - lu); sway = .12f;
                            }
                            else if (covering)
                            {
                                Vector3 shake = new Vector3(Mathf.Sin(since * 37f), Mathf.Sin(since * 29f + 1f), 0f) * .012f;
                                at = on + facing * shake; sway = .1f;
                            }
                            else at = on + (Vector3.up * 1.1f - ahead * .4f) * (leaveU * (.6f + .4f * leaveU));
                            fade = leaving ? 1f - leaveU * leaveU : 1f;
                        }
                        if (PetGhostModels.PetGhosts)
                        {
                            s.MouseFlight.Apply(ref at, ref facing, age, sprung ? 0f : 1f, dt);
                            s.MouseBlock.SetVector("_Trail", s.MouseFlight.TrailLocal);
                        }
                        var mesh = sprung ? s.MouseSprung : s.MouseWait;
                        if (s.MouseFilter.sharedMesh != mesh) { s.MouseFilter.sharedMesh = mesh; s.Mouse.sharedMaterial = PetGhostModels.MaterialFor(mesh, s.MouseFallback); }
                        if (s.MouseRig != null)
                        {
                            // The game's acting, as `NemuBlindfold.Draw` calls it.
                            var phase = !sprung ? PetGhostActing.MousePhase.Waiting : !covering && !leaving ? PetGhostActing.MousePhase.Lunging
                                : covering ? PetGhostActing.MousePhase.Covering : PetGhostActing.MousePhase.LettingGo;
                            Vector3 victimEyes = victim.transform.position + Vector3.up * 1.25f;
                            s.MouseRig.LookAt = sprung || (victim.transform.position - mouseSpot).sqrMagnitude < 49f ? victimEyes : (Vector3?)null;
                            float phaseAge = !sprung ? age : !covering && !leaving ? since : covering ? since - NecroRules.DontLookLungeSeconds : since - NecroRules.DontLookBlindSeconds;
                            PetGhostActing.Mouse(s.MouseRig, phase, age, phaseAge, facing, ref at);
                        }
                        s.Mouse.transform.SetPositionAndRotation(at, facing);
                        s.Mouse.transform.localScale = Vector3.one * scale;
                        s.MouseFade = sprung ? fade : -1f; // -1: waiting, its visibility is per viewer
                        s.MouseBlock.SetFloat("_Flash", flash); s.MouseBlock.SetFloat("_Clock", age); s.MouseBlock.SetFloat("_Sway", sway);
                        s.MouseBlock.SetFloat("_ShadeKeep", NemuGhostLook.On ? 1f : 0f);
                        s.MouseLive = !(leaving && leaveU >= 1f);
                        s.Mouse.enabled = s.MouseLive && s.MouseRig == null;
                        if (s.MouseRig != null) { s.MouseRig.Visible(s.MouseLive); s.MouseRig.Pose(age, dt, sprung ? fade : 1f, flash); }
                        if (s.Dots != null && s.Mouse.enabled)
                        {
                            float seen = sprung ? fade : Mathf.Max(1f - Smooth(.7f, 1.5f, age), ShimmerSeen);
                            s.Dots.Light(s.Mouse.transform.TransformPoint(BlindfoldGhostModel.HeadCentre * .6f), .9f * scale, .35f * seen, BlindfoldGhostModel.Aura);
                            if (sprung) foreach (var eye in BlindfoldGhostModel.Eyes) s.Dots.Light(s.Mouse.transform.TransformPoint(eye), .22f * scale, 1.4f * fade, BlindfoldGhostModel.Pinpoint);
                        }
                        if (s.New)
                        {
                            At(.3f, t, () => NemuGhostLook.Arrive(NemuGhostLook.Pet.Mouse, mouseSpot));
                            At(.3f + NecroRules.DontLookArmSeconds - .8f, t, () => NemuGhostLook.Hide(baseAt + Vector3.up * BlindfoldGhostModel.HeadCentre.y * .6f));
                            At(tripAt, t, () => NemuGhostLook.Lunge(mouseSpot));
                            At(tripAt + NecroRules.DontLookLungeSeconds, t, () => NemuGhostLook.Latch(victim.transform.position + Vector3.up * (VictimHeadHeight + .42f)));
                            At(tripAt + NecroRules.DontLookBlindSeconds, t, () => NemuGhostLook.Depart(NemuGhostLook.Pet.Mouse, victim.transform.position + Vector3.up * (VictimHeadHeight + .3f)));
                        }
                    }

                    // =========================== FETCHER
                    float ub = t - Act;
                    bool inB = t >= Act && t < 2f * Act;
                    float bAge = ub - FetchCast;
                    bool bearShown = inB && bAge >= 0f && !(bearLeaving && ub - leftAt >= .45f);
                    SetBody(s.Bear, bearShown);
                    if (bearShown)
                    {
                        float run = Mathf.Clamp01(bearSpeed / 3f);
                        float stride = bAge * (9f + bearSpeed * 2.5f), hop = Mathf.Abs(Mathf.Sin(stride));
                        bool ghost = s.BearRig != null;
                        if (ghost) { stride = bAge * 4f; hop = .5f + .5f * Mathf.Sin(bAge * 4f); }
                        float eager = offer * (1f - run) * Mathf.Abs(Mathf.Sin(bAge * 6f));
                        float bob = Mathf.Lerp(.03f * Mathf.Sin(bAge * 3f), .09f * hop, run) + .05f * eager;
                        float p = Mathf.Clamp01(bAge / .3f);
                        float pop = Mathf.Max(0f, p >= 1f ? 1f : 1f + Mathf.Sin(p * Mathf.PI * 1.25f) * (1f - p) * .6f - (1f - p) * (1f - p));
                        float fade = 1f, rise = 0f, spin = 0f, leaveU = 0f;
                        bool painted = NemuGhostLook.On;
                        if (painted) { pop = NemuGhostLook.Unfold(bAge); fade = NemuGhostLook.UnfoldFade(bAge); }
                        if (bearLeaving)
                        {
                            leaveU = Mathf.Clamp01((ub - leftAt) / .45f);
                            fade *= 1f - leaveU * leaveU; rise = 1.4f * leaveU * leaveU; spin = 540f * leaveU * leaveU;
                        }
                        float g = Mathf.Clamp01((ub - grabAt) / .3f), grabPulse = g < 1f ? Mathf.Sin(g * Mathf.PI) * (1f - g) : 0f;
                        float squash = (ghost ? .03f * Mathf.Sin(bAge * 3f) : Mathf.Lerp(.04f * Mathf.Sin(bAge * 3f), (hop - .5f) * .22f, run)) - grabPulse * .45f + (eager - .5f * offer * (1f - run)) * .14f;
                        var scale = new Vector3(1f - squash * .5f, 1f + squash, 1f - squash * .5f) * (pop * Mathf.Lerp(1f, .4f, leaveU));
                        float lean = Mathf.Clamp(bearSpeed * 5f, 0f, 22f) - (carrying ? 8f : 0f);
                        float bank = Mathf.Sin(stride * .5f) * 7f * run;
                        Vector3 toShoe = shoeRest - bearPos; toShoe.y = 0f;
                        if (ghost)
                        {
                            // The game's glide, as `NemuSkitter.Update` calls it.
                            float free = !carrying && !reachFor.HasValue && !bearLeaving ? 1f : 0f;
                            s.Glide.Step(bearPos, Quaternion.LookRotation(Flat(bearFacing * Vector3.forward)), free, reachFor.HasValue || carrying ? 0f : toShoe.magnitude, dt);
                            lean = s.Glide.Lean - (carrying ? 6f : 0f);
                            bank = s.Glide.Bank;
                            fade *= 1f - .45f * s.Glide.Flicker * (.5f + .5f * Mathf.Sin(bAge * 70f));
                        }
                        Quaternion facing = bearFacing * Quaternion.AngleAxis(spin, Vector3.up) * Quaternion.AngleAxis(lean, Vector3.right)
                            * Quaternion.AngleAxis(bank, Vector3.forward);
                        Vector3 at = bearPos + Vector3.up * (bob + rise);
                        if (ghost) at += s.Glide.Offset;
                        float flash = Mathf.Max(grabPulse * 1.2f, (1f - p) * .8f);
                        if (PetGhostModels.PetGhosts)
                        {
                            float free = !carrying && !reachFor.HasValue && !bearLeaving ? 1f - run : 0f;
                            s.BearFlight.Apply(ref at, ref facing, bAge, free, dt);
                            if (g < 1f) facing *= Quaternion.AngleAxis(360f * g * g * (3f - 2f * g), Vector3.up);
                            s.Bear.Trail = s.BearFlight.TrailLocal;
                        }
                        if (painted && !carrying && !bearLeaving && !ghost)
                        {
                            Vector3 d = shoeRest - bearPos; d.y = 0f;
                            at += NemuGhostLook.BearWeave(bAge, run, reachFor.HasValue ? 0f : d.magnitude, Quaternion.LookRotation(Flat(bearFacing * Vector3.forward)), out float roll);
                            facing *= Quaternion.AngleAxis(roll, Vector3.forward);
                        }
                        if (painted) flash = Mathf.Min(flash, grabPulse * .6f);
                        s.Bear.Pose(at, facing, scale, fade, bAge, .08f + bearSpeed * .015f, Mathf.Clamp01(flash));
                        if (ghost)
                        {
                            var phase = bearLeaving ? PetGhostActing.BearPhase.Leaving : offer > .5f ? PetGhostActing.BearPhase.Offering
                                : carrying ? PetGhostActing.BearPhase.Carrying : reachFor.HasValue ? PetGhostActing.BearPhase.Reaching : PetGhostActing.BearPhase.Outbound;
                            s.BearRig.LookAt = phase == PetGhostActing.BearPhase.Outbound || phase == PetGhostActing.BearPhase.Reaching
                                ? shoeRest : nemu.transform.position + Vector3.up * 1.25f;
                            PetGhostActing.Bear(s.BearRig, phase, bAge, carrying ? 1f - Mathf.SmoothStep(0f, 1f, (ub - grabAt) / .7f) : 0f);
                            s.BearRig.Visible(true);
                            s.BearRig.Pose(bAge, dt, fade, Mathf.Clamp01(flash));
                        }
                        Vector3 offerLift = new Vector3(0f, .1f + .035f * Mathf.Sin(bAge * 5f), .08f) * offer;
                        for (int side = 0; side < 2; side++)
                        {
                            float sign = side == 0 ? -1f : 1f;
                            Vector3 shoulder = NemuGhostModels.SkitterShoulder[side], hand;
                            if (carrying) hand = NemuGhostModels.SkitterHands + offerLift + Vector3.right * sign * (shoeLength * .5f + .015f - .01f * Mathf.Abs(Mathf.Sin(stride)));
                            else if (reachFor.HasValue)
                            {
                                Vector3 target = s.Bear.Transform.InverseTransformPoint(reachFor.Value);
                                float snatch = Mathf.Sin(bAge * 22f + side * Mathf.PI);
                                hand = target + Vector3.right * sign * (.08f + .04f * snatch) + Vector3.up * (.05f + .03f * snatch);
                            }
                            else if (ghost && bearLeaving)
                                hand = side == 1 ? shoulder + new Vector3(.10f + .05f * Mathf.Sin(bAge * 16f), .20f, .06f) : shoulder + new Vector3(-.05f, -.10f, .10f);
                            else hand = shoulder + new Vector3(sign * .07f, -.12f - .05f * Mathf.Sin(stride + side * Mathf.PI) * run, .2f + .08f * Mathf.Sin(stride + side * Mathf.PI) * run);
                            Vector3 to = hand - shoulder; float len = Mathf.Max(to.magnitude, 1e-4f);
                            s.BearArm[side].PoseLocal(shoulder, Quaternion.LookRotation(to / len, Vector3.up), fade, bAge, flash);
                            s.BearArm[side].Transform.localScale = new Vector3(1f, 1f, Mathf.Clamp(len / NemuGhostModels.SkitterHand.z, .5f, PetGhostModels.PetGhosts ? PetGhostModels.MaxArmStretch : 3.2f));
                        }
                        if (s.Dots != null && fade > .005f)
                        {
                            foreach (var eye in NemuGhostModels.SkitterEyePoints) s.Dots.Light(s.Bear.Transform.TransformPoint(eye), .09f, (carrying ? 1.1f : .8f) * fade, NemuGhostModels.SkitterEyes);
                            s.Dots.Light(s.Bear.Transform.TransformPoint(new Vector3(0f, .4f, 0f)), .75f, .28f * fade, NemuGhostModels.SkitterBody);
                        }
                        if (s.New)
                        {
                            At(Act + FetchCast, t, () => NemuGhostLook.Arrive(NemuGhostLook.Pet.Bear, bearPos - Vector3.up * .3f));
                            if (Crossed(Act + grabAt, t)) At(Act + grabAt, t, () => NemuGhostLook.Grab(s.Bear.Transform.TransformPoint(NemuGhostModels.SkitterHands)));
                            if (bearLeaving) At(Act + leftAt, t, () => NemuGhostLook.Depart(NemuGhostLook.Pet.Bear, at + Vector3.up * .22f));
                            if (!bearLeaving && run > .45f) { int beat = Mathf.FloorToInt(bAge / .11f); float m = Act + FetchCast + beat * .11f; At(m, t, () => NemuGhostLook.Wake(at + Vector3.up * .12f - Flat(bearFacing * Vector3.forward) * .16f, beat)); }
                            if (offer > .5f && !bearLeaving) { int beat = Mathf.FloorToInt(bAge / .9f); At(Act + FetchCast + beat * .9f, t, () => NemuGhostLook.Offer(s.Bear.Transform.TransformPoint(NemuGhostModels.SkitterHands + offerLift), beat)); }
                        }
                    }
                    else { SetBody(s.BearArm[0], false); SetBody(s.BearArm[1], false); s.BearRig?.Visible(false); }

                    // =========================== KEEPER
                    float uc = t - 2f * Act;
                    bool inC = t >= 2f * Act;
                    float cAge = uc - KeepCast;
                    float cLeaveU = uc >= KeepLeave ? Mathf.Clamp01((uc - KeepLeave) / .6f) : 0f;
                    bool owlShown = inC && cAge >= 0f && cLeaveU < 1f;
                    SetBody(s.Owl, owlShown);
                    if (owlShown)
                    {
                        float p = Mathf.Clamp01(cAge / .35f);
                        float pop = p >= 1f ? 1f : Mathf.Max(0f, 1f + Mathf.Sin(p * Mathf.PI * 1.25f) * (1f - p) * .5f - (1f - p) * (1f - p));
                        float fade = 1f;
                        bool painted = NemuGhostLook.On;
                        if (painted) { pop = NemuGhostLook.Unfold(cAge); fade = NemuGhostLook.UnfoldFade(cAge); }
                        fade *= 1f - cLeaveU * cLeaveU;
                        float cheer = Mathf.Clamp01((uc - cheerAt) / .7f); bool cheering = cheer < 1f;
                        float hopC = cheering ? Mathf.Sin(cheer * Mathf.PI * 2f) * (1f - cheer) : 0f;
                        float heaveClock = cAge * 2.6f * Mathf.PI, heave = Mathf.Max(0f, Mathf.Sin(heaveClock)) * owlEngage;
                        float squash = -heave * .18f + hopC * .2f + .03f * Mathf.Sin(cAge * 2.4f);
                        float size = .85f * pop * Mathf.Lerp(1f, .5f, cLeaveU);
                        var scale = new Vector3(1f - squash * .5f, 1f + squash, 1f - squash * .5f) * size;
                        Quaternion facing = owlFacing * Quaternion.AngleAxis(-12f * owlEngage - 8f * heave, Vector3.right);
                        float flash = painted ? (cheering ? (1f - cheer) * .35f : 0f) : Mathf.Max((1f - p) * .7f, cheering ? (1f - cheer) * .5f : 0f);
                        Vector3 at = owlAt;
                        if (PetGhostModels.PetGhosts)
                        {
                            float free = (1f - owlEngage) * (cheering ? 0f : 1f) * (uc < KnockAt + 4f ? .35f : 1f) * (p >= 1f && cLeaveU <= 0f ? 1f : 0f);
                            s.OwlFlight.Apply(ref at, ref facing, cAge, free, dt);
                            s.Owl.Trail = s.OwlFlight.TrailLocal;
                        }
                        s.Owl.Pose(at, facing, scale, fade, cAge, .05f + .04f * owlEngage, flash);
                        Vector3 top = can.transform.position + can.transform.up * .16f;
                        Vector3 across = Vector3.Cross(Vector3.up, Flat(can.transform.up)); if (across.sqrMagnitude < 1e-4f) across = Vector3.right;
                        for (int side = 0; side < 2; side++)
                        {
                            float sign = side == 0 ? -1f : 1f;
                            Vector3 shoulder = NemuGhostModels.LookoutShoulder[side];
                            Vector3 hand = side == 0 ? shoulder + new Vector3(-.06f, -.14f, .04f + .03f * Mathf.Sin(cAge * 2f)) : shoulder + new Vector3(.14f, .05f + .02f * Mathf.Sin(cAge * 2.4f), .12f);
                            if (owlEngage > .01f) hand = Vector3.Lerp(hand, s.Owl.Transform.InverseTransformPoint(top + across * sign * (.06f - .015f * heave) + Vector3.up * .02f), owlEngage);
                            if (cheering) hand = Vector3.Lerp(hand, shoulder + new Vector3(sign * .12f, .22f, .04f), Mathf.Sin(cheer * Mathf.PI));
                            Vector3 to = hand - shoulder; float len = Mathf.Max(to.magnitude, 1e-4f);
                            s.OwlArm[side].PoseLocal(shoulder, Quaternion.LookRotation(to / len, Vector3.up), fade, cAge, flash);
                            s.OwlArm[side].Transform.localScale = new Vector3(1f, 1f, Mathf.Clamp(len / NemuGhostModels.LookoutHandOnArm.z, .5f, PetGhostModels.PetGhosts ? PetGhostModels.MaxArmStretch : 2.6f));
                        }
                        Vector3 handAt = s.Owl.Transform.InverseTransformPoint(s.OwlArm[1].Transform.TransformPoint(NemuGhostModels.LookoutHandOnArm));
                        s.Lantern.PoseLocal(handAt, Quaternion.Euler(4f * Mathf.Sin(cAge * 1.7f), 0f, 10f * Mathf.Sin(cAge * 3.1f)), fade, cAge, flash);
                        var ls = s.Owl.Transform.lossyScale;
                        s.Lantern.Transform.localScale = new Vector3(.85f / Mathf.Max(.01f, ls.x), .85f / Mathf.Max(.01f, ls.y), .85f / Mathf.Max(.01f, ls.z));
                        Vector3 glass = s.Lantern.Transform.TransformPoint(NemuGhostModels.LookoutGlass);
                        if (s.Dots != null && fade > .005f)
                        {
                            float strength = (.75f + owlLift * .7f) * fade;
                            s.Dots.Light(glass, .55f + owlLift * .3f, strength, NemuGhostModels.LookoutLight);
                            s.Dots.Light(glass, .16f, 1.6f * fade, NemuGhostModels.LookoutLight);
                            s.Dots.Light(new Vector3(glass.x, canAt.y + .02f, glass.z), 1.3f, .22f * strength, NemuGhostModels.LookoutLight, default, 1.2f);
                            foreach (var eye in NemuGhostModels.LookoutEyePoints) s.Dots.Light(s.Owl.Transform.TransformPoint(eye), .07f, .9f * fade, NemuGhostModels.LookoutLight);
                        }
                        if (s.New)
                        {
                            At(2f * Act + KeepCast, t, () => NemuGhostLook.Arrive(NemuGhostLook.Pet.Owl, Ground(owlAt.x, owlAt.z)));
                            if (owlEngage > .4f) { int beat = Mathf.FloorToInt(heaveClock / (Mathf.PI * 2f)); At(2f * Act + KeepCast + beat / 1.3f, t, () => NemuGhostLook.Heave(canAt, beat)); }
                            At(2f * Act + upAt, t, () => NemuGhostLook.Cheer(canAt + Vector3.up * .3f, owlSide));
                            At(2f * Act + KeepLeave, t, () => NemuGhostLook.Depart(NemuGhostLook.Pet.Owl, at + Vector3.up * .25f));
                        }
                    }
                    else { SetBody(s.OwlArm[0], false); SetBody(s.OwlArm[1], false); SetBody(s.Lantern, false); }
                    s.Dots?.End(t);
                    watch.Stop(); s.PoseMs += watch.Elapsed.TotalMilliseconds; s.Posed++;
                }

                // ---- the shared rule state, one step
                void StepRule(float t, float dt)
                {
                    // PEEKER's victim: walks in, slowed 25 % while blinded.
                    float u = t;
                    float walked = Mathf.Max(0f, Mathf.Min(u, tripAt) - 2f) * 1.4f
                        + Mathf.Max(0f, Mathf.Min(u, tripAt + NecroRules.DontLookBlindSeconds) - tripAt) * 1.4f * .75f
                        + Mathf.Max(0f, u - tripAt - NecroRules.DontLookBlindSeconds) * 1.4f;
                    victim.transform.SetPositionAndRotation(victimFrom + victimWay * walked, Quaternion.LookRotation(victimWay));
                    victim.SetActive(t < Act);

                    // FETCHER, `NemuHeroKit.Skitter.OnTick` in miniature.
                    float ub = t - Act, bAge = ub - FetchCast;
                    if (t >= Act && t < 2f * Act && bAge >= 0f)
                    {
                        if (bAge < dt * 1.5f || bearPos == Vector3.zero)
                        {
                            var o = nemu.transform;
                            bearPos = o.position + o.right * .5f + Vector3.up * .3f; bearLast = bearPos;
                            bearFacing = Quaternion.LookRotation(Flat(shoeRest - bearPos)); carrying = false; bearLeaving = false; reachFor = null; offer = 0f;
                        }
                        Vector3 goal; bool towardShoe = !carrying && !bearLeaving;
                        Use(sets[1]);
                        Vector3 hands = NemuGhostModels.SkitterHands;
                        if (towardShoe) goal = shoeRest + Vector3.up * .1f - (Vector3.up * .05f + bearFacing * hands);
                        else goal = nemu.transform.position + nemu.transform.right * .6f + Vector3.up * .15f;
                        if (!bearLeaving)
                        {
                            Vector3 to = goal - bearPos; float d = to.magnitude;
                            if (d > 1e-4f)
                            {
                                bearPos += to * Mathf.Min(1f, NecroRules.FetchSpeed * dt / d);
                                Vector3 ft = Flat3(to);
                                if (ft.sqrMagnitude > 1e-4f) bearFacing = Quaternion.Slerp(bearFacing, Quaternion.LookRotation(ft.normalized), Mathf.Clamp01(dt * 12f));
                            }
                            bool arrived = (goal - bearPos).sqrMagnitude <= .09f;
                            if (towardShoe)
                            {
                                reachFor = (bearPos - shoeRest).sqrMagnitude < 1.4f * 1.4f ? shoeRest : (Vector3?)null;
                                if (arrived) { carrying = true; grabAt = ub; reachFor = null; }
                            }
                            else
                            {
                                bool waiting = (nemu.transform.position - bearPos).sqrMagnitude < 1.4f * 1.4f;
                                if (waiting) bearFacing = Quaternion.Slerp(bearFacing, Quaternion.LookRotation(Flat(nemu.transform.position - bearPos)), Mathf.Clamp01(dt * 8f));
                                offer = Mathf.MoveTowards(offer, waiting ? 1f : 0f, dt * 4f);
                                if (ub >= TakeAt) { bearLeaving = true; leftAt = ub; carrying = false; }
                            }
                        }
                        float measured = dt > 0f ? Flat3(bearPos - bearLast).magnitude / dt : 0f; bearLast = bearPos;
                        bearSpeed = Mathf.Lerp(bearSpeed, Mathf.Min(measured, 8f), Mathf.Clamp01(dt * 10f));
                    }
                    if (shoe != null)
                    {
                        shoe.SetActive(t >= Act && t < 2f * Act && !(bearLeaving && ub > TakeAt + .05f));
                        if (carrying || (ub >= grabAt && ub < TakeAt && grabAt > 0f))
                        {
                            Use(sets[1]);
                            Vector3 lift = new Vector3(0f, .1f + .035f * Mathf.Sin(bAge * 5f), .08f) * offer;
                            Vector3 hold = bearPos + Vector3.up * .05f + bearFacing * (NemuGhostModels.SkitterHands + lift);
                            shoe.transform.SetPositionAndRotation(hold, bearFacing * Quaternion.Euler(-10f, 90f, 0f));
                        }
                        else shoe.transform.SetPositionAndRotation(shoeRest, Quaternion.identity);
                    }

                    // KEEPER, `NemuLookout.Update`'s follow and grip.
                    float uc = t - 2f * Act, cAge = uc - KeepCast;
                    bool down = uc >= KnockAt && uc < upAt;
                    float liftRule = down ? (uc - KnockAt) / NecroRules.LookoutRestoreSeconds : 0f;
                    owlLift = Mathf.MoveTowards(owlLift, liftRule, dt * (liftRule < owlLift ? 3f : 1.5f));
                    owlEngage = Mathf.MoveTowards(owlEngage, liftRule > 0f ? 1f : 0f, dt * 2.5f);
                    if (uc >= upAt && cheerAt < 0f) cheerAt = upAt;
                    float tipped = down ? 1f - Mathf.Clamp01(owlLift * owlEngage) : 0f;
                    Vector3 awayCan = Flat(attacker.transform.position - canAt) * -1f;
                    can.transform.SetPositionAndRotation(canAt + Vector3.up * Mathf.Lerp(.16f, .12f, tipped), Quaternion.FromToRotation(Vector3.up, Vector3.Slerp(Vector3.up, awayCan, tipped)));
                    can.SetActive(t >= 2f * Act || t < Act);
                    if (t >= 2f * Act && cAge >= 0f)
                    {
                        Vector3 top = can.transform.position + can.transform.up * .16f;
                        Vector3 away = Flat(top - canAt); if (tipped < .05f) away = owlSide;
                        float heave = Mathf.Max(0f, Mathf.Sin(cAge * 2.6f * Mathf.PI)) * owlEngage;
                        Vector3 mind = canAt + owlSide * .7f + Vector3.up * (.15f + .05f * Mathf.Sin(cAge * 2.4f));
                        Vector3 liftAt = top + away * (.32f + .05f * heave) + Vector3.up * (-.3f * .85f - .03f * heave);
                        float cheer = Mathf.Clamp01((uc - cheerAt) / .7f), hop = cheer < 1f ? Mathf.Sin(cheer * Mathf.PI * 2f) * (1f - cheer) : 0f;
                        float sink = uc >= KeepLeave ? .5f * Mathf.Pow(Mathf.Clamp01((uc - KeepLeave) / .6f), 2f) : 0f;
                        Vector3 target = Vector3.Lerp(mind, liftAt, owlEngage) + Vector3.up * (hop * .25f - sink);
                        owlAt = owlPlaced ? Vector3.Lerp(owlAt, target, Mathf.Clamp01(dt * 9f)) : target; owlPlaced = true;
                        Vector3 look = owlEngage > .5f ? -away : Flat(attacker.transform.position - owlAt);
                        owlFacing = Quaternion.Slerp(owlFacing, Quaternion.LookRotation(Flat(look)), Mathf.Clamp01(dt * 6f));
                    }
                }

                // ---- the views
                (Vector3 eye, Vector3 aim, float fov)[] Views(float t)
                {
                    Vector3 focus = t < Act ? mouseSpot + Vector3.up * .5f : t < 2f * Act ? Vector3.Lerp(shoeRest, nemu.transform.position, .5f) + Vector3.up * .4f : canAt + Vector3.up * .4f;
                    var me = nemu.transform.position + Vector3.up * Eyes;
                    Vector3 affectedEye, affectedAim;
                    if (t < Act) { affectedEye = victim.transform.position + Vector3.up * Eyes + victim.transform.forward * .12f; affectedAim = affectedEye + victim.transform.forward * 3f - Vector3.up * .9f; }
                    else if (t < 2f * Act) { affectedEye = taya.transform.position + Vector3.up * Eyes; affectedAim = focus; }
                    else { affectedEye = attacker.transform.position + Vector3.up * Eyes; affectedAim = canAt + Vector3.up * .3f; }
                    Vector3 across = Vector3.Cross(Vector3.up, Flat(focus - me));
                    return new[]
                    {
                        (me + Flat(focus - me) * .15f, focus, 60f),
                        (affectedEye, affectedAim, 60f),
                        (focus + across * 3.4f + Vector3.up * .6f, focus, 45f),
                        (focus + new Vector3(.4f, 6.5f, -1.4f), focus, 45f),
                    };
                }

                string frames = Path.Combine(OutDir, "kit_" + tag + "_frames");
                if (Directory.Exists(frames)) foreach (string old in Directory.GetFiles(frames)) File.Delete(old);
                Directory.CreateDirectory(frames);
                var sheet = new Texture2D(CellWidth * ViewCount, CellHeight * 2, TextureFormat.RGB24, false);
                owned.Add(sheet);
                const float dtFrame = 1f / Rate;
                int count = Mathf.RoundToInt((filmEnd - filmStart) * Rate);
                // Fast-forward the rule state to the first act filmed (so a single act starts clean).
                var costs = new System.Text.StringBuilder();
                float[] costAt = { .2f + 2.5f, .2f + Act + 1.3f, .2f + 2f * Act + 2.6f };
                for (int f = 0; f <= count; f++)
                {
                    float t = filmStart + f * dtFrame;
                    StepRule(t, dtFrame);
                    foreach (var s in sets) PoseSet(s, t, dtFrame);
                    puffs.Hold(t);
                    var views = Views(t);
                    for (int row = 0; row < 2; row++)
                    {
                        var s = sets[row];
                        Use(s);
                        sets[0].Root.gameObject.SetActive(row == 0); sets[1].Root.gameObject.SetActive(row == 1);
                        puffs.Show(row == 1);
                        for (int v = 0; v < ViewCount; v++)
                        {
                            if (v == 4)
                            {
                                // THE CLOSE-UP: 1.5 m off this row's ghost, a little to its side, at its own height, eased.
                                Transform ghostT = t < Act ? s.Mouse.transform : t < 2f * Act ? s.Bear.Transform : s.Owl.Transform;
                                Vector3 gp = ghostT.position + Vector3.up * .3f;
                                s.Close = s.CloseSet ? Vector3.Lerp(s.Close, gp, .2f) : gp; s.CloseSet = true;
                                Vector3 ahead = Flat(ghostT.forward);
                                if (t < Act && t - tripAt >= 0f) ahead = Flat(nemu.transform.position - s.Close);
                                Vector3 eyeC = s.Close + Quaternion.AngleAxis(28f, Vector3.up) * ahead * 1.5f + Vector3.up * .15f;
                                PoseMouseFor(s, false, false, eyeC, s.Close, t, tripAt);
                                nemu.SetActive(true);
                                sheet.SetPixels(v * CellWidth, (1 - row) * CellHeight, CellWidth, CellHeight, Shoot(eyeC, s.Close, 40f));
                                continue;
                            }
                            // Each view hides the body it is the eyes of; PEEKER's waiting ghost is seen by Nemu at 45 % and by
                            // everyone else at a 16 % shimmer once it has faded, as `NemuBlindfold.Visibility` draws it.
                            nemu.SetActive(v != 0);
                            bool victimView = v == 1 && t < Act;
                            if (t < Act) victim.SetActive(!victimView);
                            if (t >= Act && t < 2f * Act) taya.SetActive(v != 1);
                            if (t >= 2f * Act) attacker.SetActive(v != 1);
                            PoseMouseFor(s, v == 0, victimView, views[v].eye, views[v].aim, t, tripAt);
                            var pixels = Shoot(views[v].eye, views[v].aim, views[v].fov);
                            sheet.SetPixels(v * CellWidth, (1 - row) * CellHeight, CellWidth, CellHeight, pixels);
                        }
                        nemu.SetActive(true); taya.SetActive(true); attacker.SetActive(true);
                        if (t < Act) victim.SetActive(true);
                    }
                    foreach (float c in costAt)
                        if (t - dtFrame < c && t >= c) costs.AppendLine("  at " + c.ToString("0.0") + " s: OLD " + Cost(sets[0], null) + " | NEW " + Cost(sets[1], puffs));
                    sheet.Apply();
                    File.WriteAllBytes(Path.Combine(frames, "f" + f.ToString("0000") + ".png"), sheet.EncodeToPNG());
                    prevT = t;
                }
                report.AppendLine("  " + (count + 1) + " frames in " + frames.Replace('\\', '/') + ", acts " + which + ": PEEKER 0-8 s (placed 0.3, trips "
                    + tripAt.ToString("0.00") + "), FETCHER 8-16 s (summoned 8.3, taken 13.2), KEEPER 16-24 s (summoned 16.3, can down 18.0, up 19.5, leaves 22.8)");
                report.AppendLine("cost (draw calls = enabled renderers, the puff field counts its two passes; triangles drawn; materials):");
                report.Append(costs);
                report.AppendLine("  CPU to pose a row (this film's copy of the skills' posing, editor): OLD " + (sets[0].PoseMs / Mathf.Max(1, sets[0].Posed)).ToString("0.000")
                    + " ms, NEW " + (sets[1].PoseMs / Mathf.Max(1, sets[1].Posed)).ToString("0.000") + " ms a frame");
                report.AppendLine("  NEW puffs spawned: " + puffs.Spawned + " over " + (filmEnd - filmStart).ToString("0") + " s");
                report.AppendLine("  columns: caster, affected, side, above, CLOSE-UP (1.5 m off the act's ghost)");
                report.AppendLine("NOT drawn: the trigger ring, DontLookBlind's dark, the dormant globe, ASTRA's minor dots (trail, drips, sparks) on the OLD row");
                File.WriteAllText(done, report.ToString());
            }
            catch (Exception failure)
            {
                File.WriteAllText(failed, report + "\n" + failure);
                Debug.LogError("[NemuKitFilm] " + failure);
            }
            finally
            {
                PetGhostModels.PetGhosts = wasPets; NemuGhostLook.Painted = wasPainted; PetGhostRig.Enabled = wasRig;
                NemuGhostLook.Stage(null, 0f);
                foreach (var s in sets) s?.Dots?.Dispose();
                puffs?.Dispose();
                if (lit) Unsupported.RestoreOverrideLightingSettings();
                RenderTexture.active = null;
                foreach (var thing in owned)
                {
                    if (thing == null) continue;
                    if (thing is RenderTexture rt) rt.Release();
                    Object.DestroyImmediate(thing);
                }
                if (scene.IsValid()) EditorSceneManager.ClosePreviewScene(scene);
                if (active.isLoaded && active.isDirty && !wasDirty)
                    typeof(EditorSceneManager).GetMethod("ClearSceneDirtiness", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public)?.Invoke(null, new object[] { active });
            }
        }

        private static Vector3 Flat3(Vector3 v) { v.y = 0f; return v; }

        /// <summary>
        /// THE EXPRESSION AND TURNAROUND SHEET (`faces &lt;tag&gt;`): the NEW mouse and bear rigs on Eskinita in its daylight, each row
        /// one state of the game's own acting (`PetGhostActing`) settled for a second, each column a turn (front, three quarter,
        /// side, back), 1.1 m off. Rows: mouse peeking (sly), mouse hiding under its cloth, mouse ducking, mouse lunging (wide,
        /// grinning), mouse clinging (giggling); bear on its way out, bear reaching (wide), bear offering (beaming), bear leaving.
        /// </summary>
        private static void Faces(string tag, Set s, Vector3 spot, Func<Vector3, Vector3, float, Color[]> shoot, System.Text.StringBuilder report, string done)
        {
            Use(s);
            if (s.MouseRig == null || s.BearRig == null) throw new Exception("the parts rigs did not build (missing nemu_petghost_*_parts.glb?)");
            var rows = new (string name, bool bear, int phase, float age)[]
            {
                ("mouse peeking", false, 0, 1.5f + .6f), ("mouse hiding", false, 0, .5f), ("mouse ducking", false, 0, 2.15f),
                ("mouse lunge", false, 1, .2f), ("mouse cling", false, 2, .7f),
                ("bear outbound", true, 0, 1f), ("bear reaching", true, 1, 1f), ("bear offering", true, 3, 1f), ("bear leaving", true, 4, 1f),
            };
            float[] yaws = { 180f, 135f, 90f, 0f };
            var sheet = new Texture2D(CellWidth * yaws.Length, CellHeight * rows.Length, TextureFormat.RGB24, false);
            Vector3 home = spot + Vector3.up * .55f, eye = home + Vector3.back * 1.1f + Vector3.up * .12f;
            for (int r = 0; r < rows.Length; r++)
            {
                var row = rows[r];
                for (int c = 0; c < yaws.Length; c++)
                {
                    var face = Quaternion.Euler(0f, yaws[c], 0f);
                    s.Mouse.enabled = false; s.MouseRig.Visible(!row.bear); s.BearRig.Visible(row.bear);
                    s.Bear.Pose(home + Vector3.down * 5f, face, 1f, row.bear ? 1f : 0f, 0f);
                    for (int a = 0; a < 2; a++) s.BearArm[a].PoseLocal(NemuGhostModels.SkitterShoulder[a], Quaternion.identity, row.bear ? 1f : 0f, 0f);
                    // Settle the springs over a second of the state, ending at its age.
                    for (int k = 60; k >= 0; k--)
                    {
                        float age = row.age - k / 60f;
                        Vector3 at = home;
                        if (!row.bear)
                        {
                            s.MouseRig.LookAt = eye;
                            PetGhostActing.Mouse(s.MouseRig, (PetGhostActing.MousePhase)row.phase, Mathf.Max(0f, age), Mathf.Max(0f, age), face, ref at);
                            s.Mouse.transform.SetPositionAndRotation(at, face);
                            s.MouseRig.Pose(age, 1f / 60f, 1f, 0f);
                        }
                        else
                        {
                            s.Bear.Transform.SetPositionAndRotation(home - Vector3.up * .15f, face);
                            s.BearRig.LookAt = eye;
                            PetGhostActing.Bear(s.BearRig, (PetGhostActing.BearPhase)row.phase, age, 0f);
                            s.BearRig.Pose(age, 1f / 60f, 1f, 0f);
                        }
                    }
                    sheet.SetPixels(c * CellWidth, (rows.Length - 1 - r) * CellHeight, CellWidth, CellHeight, shoot(eye, home + Vector3.up * .05f, 38f));
                }
            }
            sheet.Apply();
            string path = Path.Combine(OutDir, "kit_faces_" + tag + ".png");
            File.WriteAllBytes(path, sheet.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(sheet);
            report.AppendLine("faces sheet " + path.Replace('\\', '/') + ": rows " + string.Join(", ", Array.ConvertAll(rows, x => x.name)) + "; columns front, three quarter, side, back");
            File.WriteAllText(done, report.ToString());
        }

        private static void SetBody(NemuGhostBody body, bool on)
        {
            if (body == null) return;
            var r = body.Transform.GetComponent<MeshRenderer>();
            if (r != null && !on) r.enabled = false;
        }

        /// <summary>
        /// The mouse's visibility and, in the victim's own eyes, its lens pose (`NemuBlindfold.PoseForLens`, copied): it jumps
        /// at the lens over the lunge and clings with its face filling the view, then lets go up and over.
        /// </summary>
        private static Vector3 _worldAt; private static Quaternion _worldRot; private static Vector3 _worldScale;
        private static void PoseMouseFor(Set s, bool owner, bool victimLens, Vector3 eye, Vector3 aim, float t, float tripAt)
        {
            if (s.Mouse == null) return;
            var body = s.Mouse.transform;
            if (_restore != null) { _restore.SetPositionAndRotation(_worldAt, _worldRot); _restore.localScale = _worldScale; _restore = null; }
            if (s.MouseFade < 0f)
            {
                float age = t - .3f;
                float fadeArm = 1f - Mathf.SmoothStep(0f, 1f, (age - (NecroRules.DontLookArmSeconds - .8f)) / .8f);
                s.MouseBlock.SetFloat("_Fade", Mathf.Clamp01(age / .2f) * Mathf.Max(fadeArm, owner ? OwnerSeen : ShimmerSeen));
            }
            else s.MouseBlock.SetFloat("_Fade", s.MouseFade);
            s.Mouse.SetPropertyBlock(s.MouseBlock);
            if (s.MouseRig != null) s.MouseRig.Refade(s.MouseBlock.GetFloat("_Fade"));
            float since = t - tripAt;
            if (!victimLens || since < 0f || !s.MouseLive) return;
            _worldAt = body.position; _worldRot = body.rotation; _worldScale = body.localScale; _restore = body;
            Vector3 forward = (aim - eye).normalized, right = Vector3.Cross(Vector3.up, forward).normalized, up = Vector3.Cross(forward, right);
            Quaternion facing = Quaternion.LookRotation(-forward, up) * Quaternion.AngleAxis(9f * Mathf.Sin(t * 1.7f), Vector3.forward);
            float scale = LensScale * (1f + .025f * Mathf.Sin(t * 5.3f));
            Vector3 head = eye + forward * LensDistance;
            Vector3 at = head - facing * (BlindfoldGhostModel.HeadCentre * scale);
            if (since < NecroRules.DontLookLungeSeconds)
            {
                float u = since / NecroRules.DontLookLungeSeconds, e = u * u * u;
                at = Vector3.Lerp(_worldAt, at, e); facing = Quaternion.Slerp(_worldRot, facing, Mathf.Clamp01(u * 1.6f)); scale = Mathf.Lerp(_worldScale.x, scale, e);
            }
            else if (since >= NecroRules.DontLookBlindSeconds)
            {
                float u = Mathf.Clamp01((since - NecroRules.DontLookBlindSeconds) / BlindLeave);
                at += (up * 1.6f + forward * .4f) * (u * u);
            }
            body.SetPositionAndRotation(at, facing);
            body.localScale = Vector3.one * scale;
        }
        private static Transform _restore;

        private static string Cost(Set s, CloudPuffField puffs)
        {
            int draws = 0, triangles = 0;
            var materials = new HashSet<Material>();
            foreach (var r in s.Root.GetComponentsInChildren<Renderer>(true))
            {
                if (!r.enabled || !r.gameObject.activeInHierarchy) continue;
                if (r is MeshRenderer mr)
                {
                    var mesh = mr.GetComponent<MeshFilter>()?.sharedMesh;
                    if (mesh == null) continue;
                    int passes = mr.sharedMaterial != null ? Mathf.Max(1, mr.sharedMaterial.passCount) : 1;
                    draws += passes; triangles += mesh.triangles.Length / 3 * passes;
                    if (mr.sharedMaterial != null) materials.Add(mr.sharedMaterial);
                }
                else if (r is ParticleSystemRenderer pr && pr.GetComponent<ParticleSystem>().particleCount > 0)
                {
                    draws++; triangles += pr.GetComponent<ParticleSystem>().particleCount * 2;
                    if (pr.sharedMaterial != null) materials.Add(pr.sharedMaterial);
                }
            }
            if (puffs != null && puffs.Live) { draws += 2; triangles += puffs.DrawnTriangles * 2; materials.Add(null); }
            return draws + " draws, " + triangles + " tris, " + materials.Count + " materials";
        }
    }
}
