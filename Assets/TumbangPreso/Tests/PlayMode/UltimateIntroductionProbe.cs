using System;
using System.Collections;
using System.IO;
using System.Reflection;
using System.Text;
using System.Linq;
using NUnit.Framework;
using TumbangPreso.CameraSystem;
using TumbangPreso.Core;
using TumbangPreso.Visual;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace TumbangPreso.PlayTests
{
    // Art qualification only. The shared accepted-cast phase remains a separate
    // integration gate; this cannot activate an ability or pause the match.
    public sealed class UltimateIntroductionProbe
    {
        [UnitySetUp] public IEnumerator Before() => PlayModeWorld.Reset();
        [UnityTearDown] public IEnumerator After() => PlayModeWorld.Reset();
        [UnityTest, Timeout(90000)]
        public IEnumerator SixDistinctRenderOnlyIntroductionPerformances()
            => Study(new[] { "sean", "phaister", "zack", "nemu", "dante", "cheska", "rafi" }, false);
        // HERO-9: Paete's MAKILING'S EMBRACE introduction (tools/author_ultimate_intros.py `paete`), which had
        // never been filmed. Run with TUMP_INTRO_SCENE=1 for the staged forest and the four shots.
        [UnityTest, Timeout(90000)]
        public IEnumerator PaeteThrowsHisSeedFromTheForest()
            => Study(new[] { "paete" }, false);
        [UnityTest, Timeout(90000)]
        public IEnumerator NemuKeepsBothCharactersFramedAcrossGrowthAndAspect()
            => Study(new[] { "nemu" }, true);
        [UnityTest, Timeout(90000)]
        public IEnumerator CheskaStagesIceAtTheFreeHandWhileKeepingHerSlipper()
            => Study(new[] { "cheska" }, false);
        [UnityTest, Timeout(90000)]
        public IEnumerator CheskaEmptyHandsKeepTheTwoHandGather()
            => Study(new[] { "cheska" }, false, true);
        [UnityTest, Timeout(90000)]
        public IEnumerator RafiStagesHisCurrentBahaPerformance()
            => Study(new[] { "rafi" }, false, false, true);
        // AIRBURST (docs/reports/amihan-presentation-2026-10-02): grounded throughout, held shoe clear of the face.
        // Visual and body study only; no audio acceptance.
        [UnityTest, Timeout(90000)]
        public IEnumerator AmihanGathersHerAirburstWithoutLeavingTheGround()
            => Study(new[] { "amihan" }, false, false, true);
        [UnityTest, Timeout(90000)]
        public IEnumerator ZackDrawsOverclockIntoHimselfWithoutLosingHisShoe()
            => Study(new[] { "zack" }, false, false, true);
        [UnityTest, Timeout(90000)]
        public IEnumerator YasminAbsoluteZeroVisualReview()
            => Study(new[] { "cheska" }, false, false, true);

        [UnityTest, Timeout(90000)]
        public IEnumerator YasminEmptyHandsVisualReview()
            => Study(new[] { "cheska" }, false, true, true);

        [UnityTest, Timeout(90000)]
        public IEnumerator RagoSupernovaVisualReview()
            => Study(new[] { "sean" }, false, false, true);
        [UnityTest, Timeout(90000)]
        public IEnumerator RagoEmptyHandsVisualReview()
            => Study(new[] { "sean" }, false, true, true);

        private static IEnumerator BuildHeroArtStage(string hero)
        {
            GameServices.Ensure(); GameServices.Round.Clear();
            var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.name = "Hero art support"; floor.transform.position = Vector3.down * .5f;
            floor.transform.localScale = new Vector3(30, 1, 30);
            var actorObject = new GameObject("Hero art actor", typeof(CharacterController));
            var cc = actorObject.GetComponent<CharacterController>();
            cc.height = 1.6f; cc.radius = .35f; cc.center = Vector3.up * .8f;
            var actor = actorObject.AddComponent<CharacterMotor>();
            actor.Mode = GameMode.HeroStrike; actor.PlayerSlot = 1; actor.IsBot = true;
            actor.CharacterIndex = Roster.IndexIn(Roster.HeroPeople, hero);
            actorObject.AddComponent<Carrier>(); actorObject.AddComponent<CombatVerbs>();
            actorObject.AddComponent<TumbangPreso.Abilities.HeroAbilitySystem>().BindHero(hero);
            var art = Resources.Load<RosterEntryAsset>("Roster/person_" + hero);
            actorObject.AddComponent<CharacterVisual>().ApplyModel(art.Model, art.Tint, art.Clips, art.Palette, art.PetModel);
            GameServices.Round.Register(actor);
            GameServices.Match.ApplySnapshot(new int[4], 1, true);
            GameServices.Round.ApplySnapshot(100, true, 0, true);
            actor.Teleport(new Vector3(0, .12f, -4));
            var shoe = new GameObject("Hero held shoe").AddComponent<Slipper>();
            var shoeArt = Resources.Load<RosterEntryAsset>("Roster/slipper_loafers");
            var shoeModel = Object.Instantiate(shoeArt.Model, shoe.transform);
            ToonSkin.ApplySlipper(shoeModel, ToonSkin.PropOutlineWidth);
            shoe.OwnerSlot = actor.PlayerSlot;
            Assert.IsTrue(shoe.HostForceEquip(actor), "The art stage must equip its owned slipper.");
            Assert.AreSame(shoe, actor.GetComponent<Carrier>().Held);
            var light = new GameObject("Hero art light").AddComponent<Light>();
            light.type = LightType.Directional; light.transform.rotation = Quaternion.Euler(35, -25, 0);
            var eye = new GameObject("Hero source camera", typeof(Camera)); eye.tag = "MainCamera";
            eye.AddComponent<CameraRig>().Follow(actor);
            yield return null; yield return null;
        }

        private static IEnumerator Study(string[] heroes, bool checkFraming, bool emptyHands = false, bool allowMutedTheme = false)
        {
            // These focused render-copy reviews need their actual rig/equipment, not an entire
            // populated court. Keep other established court probes unchanged.
            if (heroes.Length == 1 && (heroes[0] == "zack" || heroes[0] == "cheska" || heroes[0] == "sean")) yield return BuildHeroArtStage(heroes[0]);
            else yield return MapRetrievalProbe.Load("Eskinita", GameMode.HeroStrike);
            var actor = GameServices.Round.PlayerAt(1);
            var visual = actor.GetComponent<CharacterVisual>();
            if (emptyHands)
            {
                var parkedShoe=actor.GetComponent<Carrier>().Held;
                if(parkedShoe!=null)
                {
                    Assert.IsTrue(parkedShoe.HostDisarm());
                    parkedShoe.transform.position=new Vector3(30,0,30);
                }
                Assert.IsNull(actor.GetComponent<Carrier>().Held);
            }
            else if (heroes.Length == 1 && (heroes[0] == "zack" || heroes[0] == "cheska" || heroes[0] == "sean"))
                Assert.IsNotNull(actor.GetComponent<Carrier>().Held, "Held-shoe review cannot silently become empty-handed.");
            foreach (var other in GameServices.Round.Players)
                if (other != actor) other.Teleport(new Vector3(-9, other.transform.position.y, 8 + other.PlayerSlot * 3));
            var camera = new GameObject("IntroductionArtWitness").AddComponent<Camera>();
            camera.CopyFrom(Camera.main); camera.enabled = false; camera.tag = "Untagged";
            camera.fieldOfView = 45; camera.aspect = 16f / 9; camera.cullingMask &= ~(1 << 5);
            camera.gameObject.AddComponent<ColourGrade>().AdoptFromScene();
            var sourceCamera = Camera.main.GetComponent<CameraRig>(); sourceCamera.SetActive(false);
            Vector3[] offsets = { new Vector3(2.2f, 1.1f, 3.8f), new Vector3(1.7f, 1.2f, 4),
                new Vector3(-2.4f, 1.3f, 3.6f), new Vector3(2.2f, 1.2f, 3.8f),
                new Vector3(-2.7f, 1.0f, 4), new Vector3(1.4f, 1.3f, 3.4f), new Vector3(-2.2f, 1.1f, 3.8f) };
            // Reuse the existing editor motion-strip geometry audit rather than
            // validating root keys against a copy of the new grounding calculation.
            var audit = AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType("TumbangPreso.EditorTools.ClipMotionStrip")).First(t => t != null);
            const BindingFlags privateStatic = BindingFlags.NonPublic | BindingFlags.Static;
            var measure = audit.GetMethod("Measure", privateStatic);
            var report = new StringBuilder("Private body studies. Ground clearance uses the existing motion-strip deformed-vertex audit.\n");
            try
            {
                for (int i = 0; i < heroes.Length; i++)
                {
                    string hero = heroes[i];
                    actor.CharacterIndex = Roster.IndexIn(Roster.GetPeople(GameMode.HeroStrike), hero);
                    var art = RosterBook.Load().People.First(p => p.Id == hero);
                    visual.ApplyModel(art.Model, art.Tint, art.Clips, art.Palette, art.PetModel);
                    actor.Teleport(new Vector3(0, actor.transform.position.y, -4)); actor.transform.rotation = Quaternion.identity;
                    actor.Intent.Clear(); actor.Intent.Parked = true;
                    yield return new WaitForSeconds(.15f);
                    var track = new MatchPoseHistory.Track(actor, visual.Model);
                    track.Record(Time.time); track.Record(Time.time + .05f);
                    var stage = new GameObject("IntroductionRenderCopy"); stage.SetActive(false);
                    var copy = track.Clone(stage.transform); Assert.IsNotNull(copy);
                    track.Apply(copy, track.Newest);
                    if(emptyHands)Assert.IsNull(actor.GetComponent<Carrier>().Held,"The empty-hand study must stay empty-handed.");
                    var clip = HeroAbilityClips.BuildUltimateIntroduction(copy.Root.transform, hero, actor.GetComponent<Carrier>().Held != null);
                    // REFINE-2.11: each hero's introduction has its own authored length and lift.
                    var performance = UltimatePerformance.For(hero, actor.GetComponent<Carrier>().Held != null);
                    Assert.IsNotNull(performance, hero + " has no authored introduction table.");
                    if(hero=="sean"&&!emptyHands)
                        Assert.AreNotSame(UltimatePerformance.For(hero,false),performance,"Rago needs his actual held-equipment performance.");
                    float seconds = performance.Seconds;
                    Assert.IsNotNull(clip); Assert.IsTrue(clip.legacy, "Runtime-authored sampling must work in the native player."); Assert.AreEqual(seconds, clip.length, .01f);
                    int score = GameServices.Match.ScoreFor(1); Vector3 at = actor.transform.position;
                    bool active = visual.Model.activeSelf;
                    var held = actor.GetComponent<Carrier>().Held;
                    bool heldActive = held != null && held.gameObject.activeSelf;
                    if (held != null) held.gameObject.SetActive(false); // Body study only; no floating live prop.
                    if (visual.Companion != null) visual.Companion.gameObject.SetActive(false);
                    visual.Model.SetActive(false); stage.SetActive(true); copy.ShowOnlyForCapture(true);
                    // The study has only this camera. Give the isolated copy a
                    // real contact shadow and place its neutral feet on this street.
                    clip.SampleAnimation(copy.Root, 0);
                    var surfaces = copy.Root.GetComponentsInChildren<Renderer>();
                    float low = surfaces.Min(r => r.bounds.min.y);
                    stage.transform.position += Vector3.up * (Slipper.GroundY(actor.transform.position) - low);
                    foreach (var surface in surfaces) surface.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
                    camera.transform.position = at + offsets[i]; camera.transform.LookAt(at + Vector3.up * .9f);
                    clip.SampleAnimation(copy.Root, 0);
                    var box = (Bounds)audit.GetMethod("WorldBox", privateStatic).Invoke(null, new object[] { copy.Root });
                    audit.GetMethod("Calibrate", privateStatic).Invoke(null, new object[] { copy.Root, box.size.y, report, hero });
                    float Low(float age)
                    {
                        var sample = measure.Invoke(null, new object[] { copy.Root, age });
                        return (float)sample.GetType().GetField("LowestVertex").GetValue(sample);
                    }
                    float floor = Low(0), lowest = 0, highest = 0;
                    HeroIntroductionScene scene = null;
                    Transform copiedGrip = null;
                    MeshFilter[] copiedShoe = null;
                    TumbangPreso.Tests.HeadSurfaceVolume head = default;
                    int shoeInsideHead = 0;
                    bool withScene = checkFraming || Environment.GetEnvironmentVariable("TUMP_INTRO_SCENE") == "1";
                    try
                    {
                        Assert.IsEmpty(stage.GetComponentsInChildren<MonoBehaviour>(true));
                        Assert.IsEmpty(stage.GetComponentsInChildren<Collider>(true));
                        if (withScene)
                        {
                            var random = UnityEngine.Random.state;
                            scene = new HeroIntroductionScene(stage.transform, hero, actor, copy);
                            if (held != null)
                            {
                                copiedGrip = copy.Root.GetComponentsInChildren<Transform>(true).First(t => t.name == "IntroductionHeldSlipper");
                                copiedShoe = copiedGrip.GetComponentsInChildren<MeshFilter>(true);
                                Assert.IsNotEmpty(copiedShoe, hero + " lost its actual held equipment in the scene.");
                                Assert.IsEmpty(copiedGrip.GetComponentsInChildren<MonoBehaviour>(true));
                                Assert.IsEmpty(copiedGrip.GetComponentsInChildren<Collider>(true));
                                head = new TumbangPreso.Tests.HeadSurfaceVolume(copy.Root.transform);
                            }
                            Assert.AreEqual(random, UnityEngine.Random.state, "Scene construction changed gameplay RNG.");
                            Assert.IsEmpty(scene.Root.GetComponentsInChildren<CharacterMotor>(true));
                            Assert.IsEmpty(scene.Root.GetComponentsInChildren<Abilities.HeroAbilitySystem>(true));
                            Assert.IsTrue(scene.Root.GetComponentsInChildren<Collider>(true).All(c => !c.enabled));
                            bool soundStarted=scene.StartSound();
                            if(!allowMutedTheme)Assert.IsTrue(soundStarted, hero + " must resolve its retained theme mapping.");
                            else report.AppendLine("["+hero+"] visual-only study; retained theme started="+soundStarted+". No audio acceptance.");
                            if(hero=="rafi")
                            {
                                var wave=scene.Root.GetComponentsInChildren<MeshFilter>(true).Single(m=>m.sharedMesh.name=="Rafi rolled wave").sharedMesh;
                                Assert.AreEqual(95,wave.vertexCount);Assert.AreEqual(432,wave.triangles.Length);
                            }
                            scene.SetVisibleForCapture(true);
                        }
                        if ((hero == "zack" || hero == "cheska" || hero == "sean") && scene != null)
                        {
                            // Warm all three authored shots before starting a wall-clock film.
                            foreach (float warm in hero == "sean" ? new[] { 0f, .9f, 1.6f, 1.95f, 2.5f, 2.97f, 3.15f, 0f }
                                : new[] { 0f, .9f, 1.1f, 1.6f, 1.95f, 2.5f, 2.9f, 0f })
                            {
                                clip.SampleAnimation(copy.Root, warm); scene.Sample(warm);
                                scene.Shot(warm, out var eye, out var target, out var lens, camera.aspect);
                                camera.transform.position = eye; camera.transform.LookAt(target); camera.fieldOfView = lens;
                                if (hero == "zack" && Mathf.Abs(warm - 1.95f) < .001f)
                                {
                                    var bolt = scene.Root.transform.Find("SnapBolt").GetComponent<MeshFilter>();
                                    var headPoint = copy.Bones.First(b => b.name == "head").position + Vector3.up * .35f;
                                    float contact = bolt.sharedMesh.vertices.Min(v => Vector3.Distance(bolt.transform.TransformPoint(v), headPoint));
                                    Assert.Less(contact, .15f, "Overclock's bolt must reach Zack, not an old distant target.");
                                }
                                if(hero=="sean"&&Mathf.Abs(warm-2.5f)<.001f)
                                {
                                    var palm=(Vector3)typeof(HeroIntroductionScene).GetProperty(emptyHands ? "BothPalms" : "FreePalm",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(scene);
                                    var flame=scene.Root.transform.Find("ParolFlame");
                                    Assert.Less(Vector3.Distance(flame.position,scene.Root.transform.TransformPoint(palm)),.34f,
                                        "The parol must stay in front of its gathering hand through the coil.");
                                }
                                if(hero=="sean"&&Mathf.Abs(warm-2.97f)<.001f)
                                    Assert.Less(Vector3.Distance(scene.Root.transform.Find("RiseSpark0").position,
                                        scene.Root.transform.Find("ParolFlame").position),.02f,"The burst must start at the parol.");
                                if (hero == "cheska" && Mathf.Abs(warm-1.1f)<.001f)
                                {
                                    var palm=(Vector3)typeof(HeroIntroductionScene).GetProperty(emptyHands ? "RightPalm" : "FreePalm",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(scene);
                                    var line=scene.Root.transform.Find("FrostLine");
                                    var tip=line.TransformPoint(Vector3.up);
                                    Assert.Less(Vector3.Distance(tip,scene.Root.transform.TransformPoint(palm)),.16f,
                                        "The drawn frost must end at the actual drawing hand, not across her face.");
                                }
                                if (hero == "cheska" && (Mathf.Abs(warm-1.6f)<.001f || Mathf.Abs(warm-2.5f)<.001f))
                                {
                                    var palm=(Vector3)typeof(HeroIntroductionScene).GetProperty("FreePalm",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(scene);
                                    var crystal=scene.Root.transform.Find("GatheredCrystal");
                                    var headAt=(Vector3)typeof(HeroIntroductionScene).GetProperty("HeadPoint",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(scene);
                                    report.AppendLine($"Yasmin t={warm:F2} empty={emptyHands} palm={palm:F3} crystal={crystal.localPosition:F3} head={headAt:F3}");
                                }
                                if (hero == "cheska" && Mathf.Abs(warm - 2.9f) < .001f)
                                {
                                    var crystal = scene.Root.transform.Find("GatheredCrystal").GetComponent<MeshFilter>();
                                    var shard = scene.Root.transform.Find("SnapShard0");
                                    float thickness = crystal.sharedMesh.bounds.size.y;
                                    float gap = Vector3.Distance(crystal.transform.position, shard.position);
                                    Assert.IsTrue(thickness > .5f && gap < .01f,
                                        $"Yasmin needs a volumetric crystal and connected shatter; mesh height={thickness:F4}, origin gap={gap:F4}m.");
                                }
                                camera.Render(); yield return null;
                            }
                        }
                        yield return ImprovementEvidenceProbe.Record(camera, hero + (withScene ? "-introduction-scene" : "-introduction-body") + (emptyHands ? "-empty" : ""), seconds,
                            drive: age =>
                            {
                                clip.SampleAnimation(copy.Root, Mathf.Min(age, seconds));
                                if (scene != null)
                                {
                                    scene.Sample(age); scene.Shot(age, out var eye, out var target, out var lens, camera.aspect);
                                    camera.transform.position = eye; camera.transform.LookAt(target); camera.fieldOfView = lens;
                                    if (copiedShoe != null)
                                    {
                                        Assert.AreEqual(Vector3.zero, copiedGrip.localPosition, "The shoe must follow its sampled hand, not a frozen world pose.");
                                        foreach (var part in copiedShoe)
                                        {
                                            var toHead = head.Bone.worldToLocalMatrix * part.transform.localToWorldMatrix;
                                            int inside = 0;
                                            foreach (var vertex in part.sharedMesh.vertices)
                                                if (head.Contains(toHead.MultiplyPoint3x4(vertex))) inside++;
                                            shoeInsideHead = Mathf.Max(shoeInsideHead, inside);
                                        }
                                    }
                                    // A close shot crops the body on purpose (a face or the hands).
                                    if (checkFraming && !scene.IsCloseShot(age))
                                    {
                                        Assert.IsTrue(scene.TryCharacterBounds(out var bounds));
                                        foreach (float aspect in new[] { 4f / 3, 16f / 9, 21f / 9 })
                                        {
                                            camera.aspect = aspect;
                                            scene.Shot(age, out eye, out target, out lens, aspect);
                                            camera.transform.position = eye; camera.transform.LookAt(target); camera.fieldOfView = lens;
                                            for (int corner = 0; corner < 8; corner++)
                                            {
                                                var atCorner = bounds.center + Vector3.Scale(bounds.extents, new Vector3(
                                                    (corner & 1) == 0 ? -1 : 1, (corner & 2) == 0 ? -1 : 1, (corner & 4) == 0 ? -1 : 1));
                                                var screen = camera.WorldToViewportPoint(atCorner);
                                                Assert.Greater(screen.z, camera.nearClipPlane);
                                                Assert.That(screen.x, Is.InRange(.075f, .925f), "Horizontal silhouette cropped at " + aspect);
                                                Assert.That(screen.y, Is.InRange(.075f, .925f), "Vertical silhouette cropped at " + aspect);
                                            }
                                        }
                                        camera.aspect = 16f / 9;
                                        scene.Shot(age, out eye, out target, out lens, camera.aspect);
                                        camera.transform.position = eye; camera.transform.LookAt(target); camera.fieldOfView = lens;
                                    }
                                }
                                // Authored lift (Phaister's levitation) is intended height, not float.
                                float clearance = Low(age) - floor - performance.LiftAt(Mathf.Min(age, seconds));
                                lowest = Mathf.Min(lowest, clearance); highest = Mathf.Max(highest, clearance);
                            });
                        report.AppendLine(FormattableString.Invariant($"{hero}: clearance {lowest:F5} to {highest:F5} metres"));
                        report.AppendLine($"{hero}: maximum held-shoe vertices inside rigid head surfaces: {shoeInsideHead}");
                        Assert.AreEqual(0, shoeInsideHead, hero + " put held equipment through its face.");
                        Assert.GreaterOrEqual(lowest, -.015f, hero + " penetrated its standing support plane.");
                        Assert.LessOrEqual(highest, .015f, hero + " floated above its standing support plane.");
                        Assert.AreEqual(at, actor.transform.position);
                        Assert.AreEqual(score, GameServices.Match.ScoreFor(1));
                    }
                    finally
                    {
                        scene?.Dispose();
                        visual.Model.SetActive(active);
                        if (held != null) held.gameObject.SetActive(heldActive);
                        if (visual.Companion != null) visual.Companion.gameObject.SetActive(true);
                        stage.SetActive(false); Object.Destroy(stage); Object.Destroy(clip);
                    }
                }
            }
            finally
            {
                string folder = Environment.GetEnvironmentVariable("TUMP_EVIDENCE") ?? "Logs/improvement-baseline-v1";
                Directory.CreateDirectory(folder); File.WriteAllText(Path.Combine(folder, "introduction-grounding" + (emptyHands ? "-empty" : "") + ".txt"), report.ToString());
                sourceCamera.SetActive(true); Object.Destroy(camera.gameObject);
            }
        }
    }
}
