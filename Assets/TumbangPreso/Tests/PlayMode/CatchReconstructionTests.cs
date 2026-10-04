using System.Collections;
using System.Linq;
using NUnit.Framework;
using TumbangPreso.Core;
using TumbangPreso.UI;
using TumbangPreso.Visual;
using TumbangPreso.CameraSystem;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace TumbangPreso.PlayTests
{
    public sealed class CatchReconstructionTests
    {
        [UnitySetUp] public IEnumerator Before() => PlayModeWorld.Reset();
        private static int _previousIdleImportDelay=-1;
        [UnityTearDown] public IEnumerator After()
        {
            yield return PlayModeWorld.Reset();
#if UNITY_EDITOR
            if(_previousIdleImportDelay>=0)UnityEditor.EditorUserSettings.idleImportWorkerShutdownDelayMilliseconds=_previousIdleImportDelay;
#endif
            _previousIdleImportDelay=-1;
        }
        private static IEnumerator Open()
        {
            SceneFlow.Networked = false; SceneFlow.SetSelectedRules(CustomGameRules.Defaults(GameMode.Classic));
            GameLaunch.SoloSeat = 1; GameLaunch.GuidedTutorial = false;
            string map=System.Environment.GetEnvironmentVariable("TUMP_CATCH_REVIEW_MAP")??SceneFlow.Eskinita;
            Assert.Contains(map,SceneFlow.Maps,"Catch review must target an authored playable map.");
            yield return SceneManager.LoadSceneAsync(map); yield return new WaitForSeconds(.2f);
            foreach (var ai in Object.FindObjectsByType<AIController>()) ai.enabled = false;
            foreach (var input in Object.FindObjectsByType<PlayerInputReader>()) input.enabled = false;
            Object.FindAnyObjectByType<SliceRunner>().Begin(); yield return null;
            foreach (var actor in GameServices.Round.Players) { actor.Intent.Clear(); actor.ClearStun(); actor.ClearTrip(); }
            Hud.Instance.ShowReadyPrompt(false);
            float until = Time.unscaledTime + 3;
            while (GameServices.Round.Lata.IsProtected && Time.unscaledTime < until) yield return null;
        }
        private static void Stage()
        {
            var round = GameServices.Round; var can = round.Lata.transform.position;
            var taya = round.PlayerAt(0); var victim = round.PlayerAt(1);
            victim.ClearStun(); victim.ClearTrip(); victim.Teleport(can + Vector3.back * 2.2f);
            taya.Teleport(can + Vector3.back * 3.2f); taya.transform.forward = Vector3.forward;
            victim.transform.forward = Vector3.forward;
            for (int i = 2; i < 4; i++) round.PlayerAt(i).Teleport(can + new Vector3(-5, 0, i * 2));
        }
        [UnityTest]
        public IEnumerator IlalimOverlayTexturesRetainTheirAuthoredRange()
        {
#if UNITY_EDITOR
            var material=UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/TumbangPreso/Art/IlalimRebuild/Materials/heritage_trim_nurses.mat");
            Assert.IsNotNull(material);
            string folder=System.Environment.GetEnvironmentVariable("TUMP_EVIDENCE")??"Logs/ilalim-overlay-textures";
            System.IO.Directory.CreateDirectory(folder);var report=new System.Text.StringBuilder();
            foreach(string property in new[]{"_Ov0Tex","_Ov1Tex"})
            {
                var texture=material.GetTexture(property) as Texture2D;Assert.IsNotNull(texture);
                var target=RenderTexture.GetTemporary(texture.width,texture.height,0,RenderTextureFormat.ARGBFloat,RenderTextureReadWrite.Linear);
                var image=new Texture2D(texture.width,texture.height,TextureFormat.RGBAFloat,false,true);
                var before=RenderTexture.active;
                try
                {
                    Graphics.Blit(texture,target);RenderTexture.active=target;
                    image.ReadPixels(new Rect(0,0,target.width,target.height),0,0);image.Apply();
                    float minimum=1,maximum=0;int invalid=0;
                    foreach(var pixel in image.GetPixels())
                    {
                        if(!float.IsFinite(pixel.r)||!float.IsFinite(pixel.g)||!float.IsFinite(pixel.b))invalid++;
                        minimum=Mathf.Min(minimum,Mathf.Min(pixel.r,Mathf.Min(pixel.g,pixel.b)));
                        maximum=Mathf.Max(maximum,Mathf.Max(pixel.r,Mathf.Max(pixel.g,pixel.b)));
                    }
                    report.AppendLine($"{property}: {texture.name}, {texture.width}x{texture.height}, {texture.format}, mips{texture.mipmapCount}, min{minimum}, max{maximum}, invalid{invalid}");
                    System.IO.File.WriteAllText(folder+"/texture-ranges.txt",report.ToString());
                    Assert.Zero(invalid);Assert.Greater(minimum,.1f,"Subtle authored grime cannot contain black/corrupt imported samples.");
                }
                finally {RenderTexture.active=before;RenderTexture.ReleaseTemporary(target);Object.Destroy(image);}
            }
#endif
            yield return null;
        }

        [UnityTest,Timeout(90000)]
        public IEnumerator CatchCameraAdoptsGameplayShaderContextAndRestoresGlobals()
        {
#if UNITY_EDITOR
            // Release completed import workers before loading the real map in
            // the isolated review. Do not change worker count or memory guards.
            _previousIdleImportDelay=UnityEditor.EditorUserSettings.idleImportWorkerShutdownDelayMilliseconds;
            UnityEditor.EditorUserSettings.idleImportWorkerShutdownDelayMilliseconds=1;
            yield return null;
#endif
            yield return Open();Stage();yield return new WaitForSeconds(1.1f);
            Assert.IsNotNull(WorldLookPresentation.Current);
            Assert.Greater(WorldLookPresentation.Current.Weight,.1f,"Use an active gameplay look as the witness.");
            var view=Object.FindAnyObjectByType<CatchReconstruction>();var actor=GameServices.Round.PlayerAt(0);
            bool reduced=Settings.SettingsStore.Current.ReducedUiMotion;Settings.SettingsStore.Current.ReducedUiMotion=false;
            bool observed=false;float weight=-1,architecture=-1;
            void Observe(Camera camera)
            {
                if(camera.name!="~CatchPlaybackCamera")return;
                observed=true;weight=Shader.GetGlobalFloat("_WorldLookWeight");architecture=Shader.GetGlobalFloat("_WorldArchitecture");
            }
            Camera.onPreRender+=Observe;
            try
            {
                Assert.IsTrue(actor.GetComponent<CombatVerbs>().HostResolvePunch(actor.transform.position,actor.transform.forward));
                yield return new WaitForSecondsRealtime(.2f);Assert.IsTrue(view.Playing);Assert.IsTrue(observed);
                var camera=GameObject.Find("~CatchPlaybackCamera").GetComponent<Camera>();
                string folder=System.Environment.GetEnvironmentVariable("TUMP_EVIDENCE")??"Logs/tagged-world-look";
                System.IO.Directory.CreateDirectory(folder);
                var target=camera.targetTexture;var image=new Texture2D(target.width,target.height,TextureFormat.RGB24,false);
                var old=RenderTexture.active;RenderTexture.active=target;image.ReadPixels(new Rect(0,0,target.width,target.height),0,0);image.Apply();RenderTexture.active=old;
                System.IO.File.WriteAllBytes(folder+"/actual-replay.png",image.EncodeToPNG());
                if(System.Environment.GetEnvironmentVariable("TUMP_CATCH_SURFACE_CHECK")=="1")
                {
                    int dark=0,samples=0;
                    for(int y=Mathf.FloorToInt(image.height*.71f);y<Mathf.FloorToInt(image.height*.79f);y++)
                        for(int x=Mathf.FloorToInt(image.width*.50f);x<Mathf.FloorToInt(image.width*.54f);x++)
                        {var pixel=image.GetPixel(x,y);samples++;if(Mathf.Max(pixel.r,Mathf.Max(pixel.g,pixel.b))<20f/255f)dark++;}
                    System.IO.File.WriteAllText(folder+"/surface-pixels.txt",$"Dark facade pixels: {dark} / {samples}");
                    Object.Destroy(image);
                    Assert.Less(dark,10,"Grime overlays must not create isolated near-black speckles on this pale facade.");
                }
                else Object.Destroy(image);
                System.IO.File.WriteAllText(folder+"/look.txt",$"replay weight={weight}; gameplay={WorldLookPresentation.Current.Weight}; architecture={architecture}");
                Assert.AreEqual(WorldLookPresentation.Current.Weight,weight,.001f,"Replay must adopt the same scoped world shader look as gameplay.");
                Assert.AreEqual(WorldCueProfile.Current.EnvironmentAppeal,architecture,.001f);
                Assert.IsTrue(WorldLookPresentation.HandlesCamera(camera));
                float before=Shader.GetGlobalFloat("_WorldLookWeight");
                typeof(CatchReconstruction).GetMethod("RenderOnlyCopies",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(view,null);
                Assert.AreEqual(before,Shader.GetGlobalFloat("_WorldLookWeight"),.001f,"Off-screen capture must restore surrounding shader state.");
                var portrait=new GameObject("UnrelatedPortraitCamera").AddComponent<Camera>();portrait.enabled=false;
                Assert.IsFalse(WorldLookPresentation.HandlesCamera(portrait),"Do not broaden every camera into the match look.");Object.Destroy(portrait.gameObject);
            }
            finally{Camera.onPreRender-=Observe;view.End();Settings.SettingsStore.Current.ReducedUiMotion=reduced;}
        }

        [UnityTest]
        public IEnumerator CatchCameraHidesUnrecordedAnimalsAndRestoresTheirVisibility()
        {
            yield return Open();Stage();yield return new WaitForSeconds(.4f);
            var life=Object.FindFirstObjectByType<AmbientLife>();Assert.IsNotNull(life);
            var ambient=life.GetComponentsInChildren<Renderer>(true);Assert.IsNotEmpty(ambient);
            ambient[ambient.Length-1].forceRenderingOff=true;
            var flags=ambient.Select(r=>r.forceRenderingOff).ToArray();
            bool observed=false,hidden=true;
            void BeforeRender(Camera camera)
            {
                if(camera.name!="~CatchPlaybackCamera")return;
                observed=true;hidden&=ambient.All(r=>r.forceRenderingOff);
            }
            var view=Object.FindAnyObjectByType<CatchReconstruction>();
            bool reduced=Settings.SettingsStore.Current.ReducedUiMotion;
            Settings.SettingsStore.Current.ReducedUiMotion=false;
            Camera.onPreRender+=BeforeRender;
            try
            {
                var taya=GameServices.Round.PlayerAt(0);
                Assert.IsTrue(taya.GetComponent<CombatVerbs>().HostResolvePunch(taya.transform.position,taya.transform.forward));
                yield return new WaitForSecondsRealtime(.2f);
                Assert.IsTrue(view.Playing);Assert.IsTrue(observed,"Catch camera never rendered");
                CollectionAssert.AreEqual(flags,ambient.Select(r=>r.forceRenderingOff).ToArray(),"Catch camera changed live ambient visibility");
                Assert.IsTrue(life.enabled,"Reconstruction must not disable the live animal simulation");
                Assert.IsTrue(hidden,"Present-time animals leaked into the catch reconstruction");
            }
            finally
            {
                Camera.onPreRender-=BeforeRender;view.End();
                Settings.SettingsStore.Current.ReducedUiMotion=reduced;
                for(int i=0;i<ambient.Length;i++)if(ambient[i]!=null)ambient[i].forceRenderingOff=false;
            }
        }
        [UnityTest]
        public IEnumerator CatchChoosesTheOpenSideAndAvoidsAForcedFaceCloseup()
        {
            yield return Open(); Stage(); yield return new WaitForSeconds(.4f);
            var taya = GameServices.Round.PlayerAt(0);
            var victim = GameServices.Round.PlayerAt(1);
            var view = Object.FindAnyObjectByType<CatchReconstruction>();
            var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name = "CatchShotRightOcclusion";
            wall.transform.position = new Vector3(1.25f, 1.2f, -2.2f);
            wall.transform.localScale = new Vector3(.15f, 3, 4);
            Physics.SyncTransforms();
            Assert.IsTrue(taya.GetComponent<CombatVerbs>().HostResolvePunch(taya.transform.position, taya.transform.forward));
            yield return new WaitForSecondsRealtime(.2f);
            Assert.IsTrue(view.Playing);
            var camera = GameObject.Find("~CatchPlaybackCamera").GetComponent<Camera>();
            Assert.Less(camera.transform.position.x, -.5f, "The wall-facing side must not force a tight camera through the bodies.");
            var otherWall = Object.Instantiate(wall);
            otherWall.transform.position = new Vector3(-1.25f, 1.2f, -2.2f);
            Physics.SyncTransforms(); yield return null;
            Assert.IsFalse(view.Playing, "If neither shot is clear, keep the live recovery view.");
            Assert.IsFalse(victim.CanAct(), "Occlusion fallback cannot alter the tag penalty.");
            Object.Destroy(wall); Object.Destroy(otherWall);
        }

        [UnityTest, Timeout(360000)]
        public IEnumerator RecordedApproachKeepsMovingThroughLateReplayAndSurvivesHistoryWrap()
            => CheckReplayMotion(false);

        [UnityTest]
        public IEnumerator IsolatedNativeCatchRetainsContinuousMotion()
            => CheckReplayMotion(true);

        [UnityTest]
        public IEnumerator ContactCameraShowsTheActualReachingHand() => CheckContactReach(1f);

        [UnityTest]
        public IEnumerator FarAcceptedTagHasVisibleHandContact() => CheckContactReach(1.65f);

        [UnityTest]
        public IEnumerator TagContactMetadataCannotAimTheNextMissOrCreateScores()
        {
            yield return OpenIsolatedCatchWorld(); Stage(); yield return null;
            var actor = GameServices.Round.PlayerAt(0); var victim = GameServices.Round.PlayerAt(1);
            var animator = actor.GetComponent<CharacterAnimator>();
            var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
            bool HasContact() => (bool)typeof(CharacterAnimator).GetField("_tagContactValid", flags).GetValue(animator);
            int tags = 0;
            System.Action<int, ScoreEvent> scored = (slot, kind) => { if (kind == ScoreEvent.Tag) tags++; };
            GameServices.Match.Scored += scored;
            try
            {
                animator.PresentTagContact(victim, victim.transform.position);
                animator.PlayAction("punch");
                Assert.IsTrue(HasContact(), "A preceding accepted-contact callback can pair with its action.");
                yield return new WaitForSeconds(.45f);
                animator.PresentTagContact(victim, victim.transform.position);
                animator.PlayAction("punch");
                Assert.IsFalse(HasContact(), "A late callback for a completed gesture must not aim the next miss.");
                animator.PresentTagContact(victim, new Vector3(float.NaN, 0, 0));
                animator.PlayAction("punch");
                Assert.IsFalse(HasContact());
                Assert.AreEqual(0, tags, "Presentation alone cannot create an accepted tag.");
            }
            finally { GameServices.Match.Scored -= scored; }
        }

        [UnityTest, Timeout(90000)]
        public IEnumerator AuthoredMapCloseTagRetainsVisibleWholeBodyContact() => CheckAuthoredMapContact(1f);

        [UnityTest, Timeout(90000)]
        public IEnumerator AuthoredMapFarTagRetainsVisibleWholeBodyContact() => CheckAuthoredMapContact(1.65f);

        private IEnumerator CheckAuthoredMapContact(float distance)
        {
            int mip = QualitySettings.globalTextureMipmapLimit;
            try
            {
                QualitySettings.globalTextureMipmapLimit = 2;
                yield return CheckContactReach(distance, true);
            }
            finally { QualitySettings.globalTextureMipmapLimit = mip; }
        }

        [UnityTest] public IEnumerator SmallNemuTaggingDanteHasVisibleContact() => CheckContactReach(1.65f, false, "nemu", "dante");
        [UnityTest] public IEnumerator DanteTaggingSmallNemuHasVisibleContact() => CheckContactReach(1.65f, false, "dante", "nemu");

        [UnityTest] public IEnumerator RosterTag_bayan_To_maring() => CheckContactReach(1.65f, false, "bayan", "maring");
        [UnityTest] public IEnumerator RosterTag_maring_To_totoy() => CheckContactReach(1.65f, false, "maring", "totoy");
        [UnityTest] public IEnumerator RosterTag_totoy_To_inday() => CheckContactReach(1.65f, false, "totoy", "inday");
        [UnityTest] public IEnumerator RosterTag_inday_To_kuya_boy() => CheckContactReach(1.65f, false, "inday", "kuya_boy");
        [UnityTest] public IEnumerator RosterTag_kuya_boy_To_ate_girlie() => CheckContactReach(1.65f, false, "kuya_boy", "ate_girlie");
        [UnityTest] public IEnumerator RosterTag_ate_girlie_To_tikboy() => CheckContactReach(1.65f, false, "ate_girlie", "tikboy");
        [UnityTest] public IEnumerator RosterTag_tikboy_To_bebang() => CheckContactReach(1.65f, false, "tikboy", "bebang");
        [UnityTest] public IEnumerator RosterTag_bebang_To_jun_jun() => CheckContactReach(1.65f, false, "bebang", "jun_jun");
        [UnityTest] public IEnumerator RosterTag_jun_jun_To_lola_pacing() => CheckContactReach(1.65f, false, "jun_jun", "lola_pacing");
        [UnityTest] public IEnumerator RosterTag_lola_pacing_To_mang_kanor() => CheckContactReach(1.65f, false, "lola_pacing", "mang_kanor");
        [UnityTest] public IEnumerator RosterTag_mang_kanor_To_aling_nena() => CheckContactReach(1.65f, false, "mang_kanor", "aling_nena");
        [UnityTest] public IEnumerator RosterTag_aling_nena_To_dante() => CheckContactReach(1.65f, false, "aling_nena", "dante");
        [UnityTest] public IEnumerator RosterTag_dante_To_cheska() => CheckContactReach(1.65f, false, "dante", "cheska");
        [UnityTest] public IEnumerator RosterTag_cheska_To_sean() => CheckContactReach(1.65f, false, "cheska", "sean");
        [UnityTest] public IEnumerator RosterTag_sean_To_zack() => CheckContactReach(1.65f, false, "sean", "zack");
        [UnityTest] public IEnumerator RosterTag_zack_To_nemu() => CheckContactReach(1.65f, false, "zack", "nemu");
        [UnityTest] public IEnumerator RosterTag_nemu_To_phaister() => CheckContactReach(1.65f, false, "nemu", "phaister");
        [UnityTest] public IEnumerator RosterTag_phaister_To_rafi() => CheckContactReach(1.65f, false, "phaister", "rafi");
        [UnityTest] public IEnumerator RosterTag_rafi_To_amihan() => CheckContactReach(1.65f, false, "rafi", "amihan");
        [UnityTest] public IEnumerator RosterTag_amihan_To_paete() => CheckContactReach(1.65f, false, "amihan", "paete");
        [UnityTest] public IEnumerator RosterTag_paete_To_bayan() => CheckContactReach(1.65f, false, "paete", "bayan");

        private IEnumerator CheckContactReach(float distance, bool authoredMap = false, string actorArt = null, string victimArt = null)
        {
            if (authoredMap) yield return Open(); else yield return OpenIsolatedCatchWorld();
            if (actorArt != null)
            {
                void SetArt(int seat, string id)
                {
                    var art = RosterBook.Load().FindPersonArt(id);
                    Assert.IsNotNull(art, id);
                    GameServices.Round.PlayerAt(seat).GetComponent<CharacterVisual>().ApplyModel(art.Model, art.Tint, art.Clips, art.Palette, art.PetModel);
                }
                SetArt(0, actorArt); SetArt(1, victimArt);
                yield return null;
            }
            Stage();
            var victim = GameServices.Round.PlayerAt(1);
            var actor = GameServices.Round.PlayerAt(0);
            actor.Teleport(victim.transform.position - Vector3.forward * distance);
            yield return new WaitForSeconds(.4f);
            Assert.IsTrue(actor.IsGrounded); Assert.IsTrue(victim.IsGrounded);
            var view = Object.FindAnyObjectByType<CatchReconstruction>();
            var settings = Settings.SettingsStore.Current;
            bool reduced = settings.ReducedUiMotion, cinematic = settings.CinematicCameraMotion;
            settings.ReducedUiMotion = false; settings.CinematicCameraMotion = true;
            Camera.CameraCallback rendered = null;
            int acceptedTags = 0;
            System.Action<int, ScoreEvent> scored = (slot, kind) => { if (slot == 0 && kind == ScoreEvent.Tag) acceptedTags++; };
            GameServices.Match.Scored += scored;
            try
            {
                var sourceHand = actor.GetComponent<CharacterVisual>().HandAnchor;
                Vector3 restScale = sourceHand.parent.localScale;
                var sourceTorso = actor.GetComponent<CharacterVisual>().TorsoBone;
                var sourceRoot = sourceTorso.parent;
                if (actorArt != null) Debug.Log("[TagGeometry] " + actorArt + " torso=" + sourceTorso.position + " hand=" + sourceHand.position + " victimTorso=" + victim.GetComponent<CharacterVisual>().TorsoBone.position + " victim=" + victim.transform.position);
                Vector3 motorStart = actor.transform.position;
                Vector3 rootStart = sourceRoot.localPosition;
                int beforeScore = GameServices.Match.ScoreFor(0);
                Assert.IsTrue(actor.GetComponent<CombatVerbs>().HostResolvePunch(actor.transform.position, actor.transform.forward));
                Assert.IsTrue(view.Playing);
                Assert.AreEqual(beforeScore + MatchRules.PointsFor(ScoreEvent.Tag), GameServices.Match.ScoreFor(0));
                var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
                object Field(string name) => typeof(CatchReconstruction).GetField(name, flags).GetValue(view);
                float contact = (float)Field("_contact"), start = (float)Field("_clipStart"), end = (float)Field("_clipEnd");
                float began = (float)Field("_began"), duration = (float)Field("_duration");
                var copy = (MatchPoseHistory.Copy)Field("_actorCopy");
                var victimCopy = (MatchPoseHistory.Copy)Field("_victimCopy");
                var hand = Object.FindAnyObjectByType<MatchPoseHistory>().ForSeat(0).CopiedBone(copy, sourceHand);
                Assert.IsNotNull(hand);
                var track = Object.FindAnyObjectByType<MatchPoseHistory>().ForSeat(0);
                var copiedTorso = track.CopiedBone(copy, sourceTorso);
                var copiedRoot = track.CopiedBone(copy, sourceRoot);
                var camera = (Camera)Field("_camera"); var target = (RenderTexture)Field("_target");
                var picture = (UnityEngine.UI.RawImage)Field("_picture");
                string directory = (authoredMap ? "Logs/tag-contact-authored/reach-" : "Logs/tag-contact/reach-") + distance.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture);
                if (actorArt != null) directory = "Logs/tag-contact-silhouettes/" + actorArt + "-to-" + victimArt;
                System.IO.Directory.CreateDirectory(directory);
                float lastTime = -1, lastGap = float.MaxValue, lastPoseTime = 0, lastAlpha = 0;
                float lastShift = 0, lastLean = 0, lastStretch = 0;
                rendered = c =>
                {
                    if (c != camera) return;
                    lastTime = Time.unscaledTime - began;
                    lastPoseTime = Mathf.Lerp(start, end, Mathf.Clamp01(lastTime / CatchReconstruction.AnimationDuration));
                    var bounds = victimCopy.Renderers[0].bounds;
                    foreach (var renderer in victimCopy.Renderers) bounds.Encapsulate(renderer.bounds);
                    lastGap = Vector3.Distance(hand.position, bounds.ClosestPoint(hand.position));
                    lastAlpha = picture.color.a;
                    lastShift = copiedRoot.parent.TransformVector(copiedRoot.localPosition - rootStart).magnitude;
                    lastLean = Vector3.Angle(copiedTorso.up, actor.transform.up);
                    Vector3 armScale = hand.parent.localScale;
                    lastStretch = Mathf.Max(armScale.x / restScale.x, armScale.y / restScale.y, armScale.z / restScale.z);
                };
                Camera.onPostRender += rendered;
                float next = 0, bestDistance = float.MaxValue, bestGap = float.MaxValue, bestAlpha = 0;
                float bestShift = 0, bestLean = 0, bestStretch = 0, bestSurfaceGap = float.MaxValue;
                int frames = 0;
                var times = new System.Collections.Generic.List<string>();
                while (view.Playing && Time.unscaledTime - began < duration + .5f)
                {
                    // Read a completed real frame. Seeking bones and rendering several
                    // times in one frame can reuse stale native skinning matrices.
                    yield return null;
                    float presentationTime = Time.unscaledTime - began;
                    if (!view.Playing || target == null || lastTime < 0 || presentationTime < next) continue;
                    next = presentationTime + .075f;
                    var previous = RenderTexture.active;
                    var image = new Texture2D(target.width, target.height, TextureFormat.RGB24, false);
                    try
                    {
                        RenderTexture.active = target; image.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0); image.Apply();
                        byte[] bytes = image.EncodeToPNG();
                        System.IO.File.WriteAllBytes(directory + "/frame-" + frames.ToString("D3") + ".png", bytes);
                        times.Add(presentationTime.ToString("F6", System.Globalization.CultureInfo.InvariantCulture));
                        float proximity = Mathf.Abs(lastPoseTime - (contact + .17f));
                        if (proximity < bestDistance)
                        {
                            bestDistance = proximity; bestGap = lastGap; bestAlpha = lastAlpha;
                            bestShift = lastShift; bestLean = lastLean; bestStretch = lastStretch;
                            if (authoredMap || actorArt != null) bestSurfaceGap = SkinSurfaceDistance(hand.position, victimCopy.Renderers);
                            System.IO.File.WriteAllBytes(directory + "/contact.png", bytes);
                        }
                        frames++;
                    }
                    finally { RenderTexture.active = previous; Object.Destroy(image); }
                }
                System.IO.File.WriteAllLines(directory + "/times.txt", times);
                string values = "distance=" + distance + " contactGap=" + bestGap + " contactAlpha=" + bestAlpha
                    + " sampleError=" + bestDistance + " frames=" + frames
                    + " bodyShift=" + bestShift + " torsoLean=" + bestLean + " armStretch=" + bestStretch + " skinSurfaceGap=" + bestSurfaceGap;
                System.IO.File.WriteAllText(directory + "/measurements.txt", values); Debug.Log(values);
                Assert.That(frames, Is.GreaterThan(8));
                Assert.That(bestDistance, Is.LessThan(.09f));
                Assert.That(bestGap, Is.LessThan(.08f), "The actual rendered reaching hand must reach the accepted victim's visible body bounds.");
                if (authoredMap || actorArt != null) Assert.That(bestSurfaceGap, Is.LessThan(.08f), "A bounding-box overlap is not visible contact with the actual skin.");
                Assert.That(bestAlpha, Is.GreaterThan(.95f), "Do not fade out while the hand first reaches the target.");
                Assert.That(Vector3.Distance(restScale, sourceHand.parent.localScale), Is.LessThan(.001f), "Temporary limb extension must restore.");
                Assert.That(bestShift, Is.GreaterThan(.1f), "The hips must transfer weight into the step.");
                Assert.That(bestLean, Is.GreaterThan(distance > 1.5f ? 25f : 18f), "The chest must commit to the reach.");
                if (distance < 1.1f) Assert.That(bestLean, Is.LessThan(30f), "Close tags should not dive through the target.");
                Assert.That(bestStretch, Is.LessThanOrEqualTo(1.101f), "Do not substitute an elongated arm for body motion.");
                Assert.That(Vector3.Distance(motorStart, actor.transform.position), Is.LessThan(.03f), "The visual step must not move the gameplay capsule.");
                Assert.That(Vector3.Distance(rootStart, sourceRoot.localPosition), Is.LessThan(.03f), "The visual hip offset must restore after recovery.");
                Assert.AreEqual(1, acceptedTags, "Animation must not create another accepted tag.");
            }
            finally
            {
                GameServices.Match.Scored -= scored;
                Camera.onPostRender -= rendered; view.End();
                settings.ReducedUiMotion = reduced; settings.CinematicCameraMotion = cinematic;
            }
        }

        private static float SkinSurfaceDistance(Vector3 point, Renderer[] renderers)
        {
            float squared = float.PositiveInfinity;
            var mesh = new Mesh();
            try
            {
                foreach (var renderer in renderers)
                {
                    if (!(renderer is SkinnedMeshRenderer skin) || !skin.enabled) continue;
                    skin.BakeMesh(mesh, true);
                    var vertices = mesh.vertices; var triangles = mesh.triangles;
                    for (int i = 0; i < vertices.Length; i++) vertices[i] = skin.transform.TransformPoint(vertices[i]);
                    for (int i = 0; i < triangles.Length; i += 3)
                        squared = Mathf.Min(squared, (point - ClosestTriangle(point, vertices[triangles[i]], vertices[triangles[i + 1]], vertices[triangles[i + 2]])).sqrMagnitude);
                }
            }
            finally { Object.Destroy(mesh); }
            return Mathf.Sqrt(squared);
        }

        private static Vector3 ClosestTriangle(Vector3 p, Vector3 a, Vector3 b, Vector3 c)
        {
            var ab = b - a; var ac = c - a; var ap = p - a;
            float d1 = Vector3.Dot(ab, ap), d2 = Vector3.Dot(ac, ap);
            if (d1 <= 0 && d2 <= 0) return a;
            var bp = p - b; float d3 = Vector3.Dot(ab, bp), d4 = Vector3.Dot(ac, bp);
            if (d3 >= 0 && d4 <= d3) return b;
            float vc = d1 * d4 - d3 * d2;
            if (vc <= 0 && d1 >= 0 && d3 <= 0) return a + ab * (d1 / (d1 - d3));
            var cp = p - c; float d5 = Vector3.Dot(ab, cp), d6 = Vector3.Dot(ac, cp);
            if (d6 >= 0 && d5 <= d6) return c;
            float vb = d5 * d2 - d1 * d6;
            if (vb <= 0 && d2 >= 0 && d6 <= 0) return a + ac * (d2 / (d2 - d6));
            float va = d3 * d6 - d5 * d4;
            if (va <= 0 && d4 - d3 >= 0 && d5 - d6 >= 0)
                return b + (c - b) * ((d4 - d3) / ((d4 - d3) + (d5 - d6)));
            float sum = va + vb + vc;
            if (Mathf.Abs(sum) < .0000001f) return a;
            return a + ab * (vb / sum) + ac * (vc / sum);
        }

        private static IEnumerator OpenIsolatedCatchWorld()
        {
            SceneFlow.Networked = false;
            GameLaunch.Spectator = false;
            var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.name = "Isolated replay floor";
            floor.transform.position = Vector3.down * .1f;
            floor.transform.localScale = new Vector3(30, .2f, 30);
            var light = new GameObject("Isolated replay light").AddComponent<Light>();
            light.type = LightType.Directional; light.transform.rotation = Quaternion.Euler(45, -35, 0);
            var round = GameServices.Round;
            round.Clear();
            round.Lata = new GameObject("Isolated replay can").AddComponent<Lata>();
            var people = Roster.GetPeople(GameMode.Classic);
            for (int i = 0; i < 4; i++)
            {
                var owner = new GameObject("Isolated replay P" + i);
                // MatchInstaller's actual person capsule; leave simulation enabled
                // so grounding and the animator's base state are real.
                var controller = owner.AddComponent<CharacterController>();
                controller.height = 1.6f; controller.radius = .35f;
                controller.center = new Vector3(0, .8f, 0);
                controller.slopeLimit = 45; controller.stepOffset = .3f;
                var motor = owner.AddComponent<CharacterMotor>();
                motor.PlayerSlot = i; motor.Mode = GameMode.Classic;
                motor.SpawnPosition = new Vector3(-5 + i * 3, 0, 7);
                motor.HoldingSlipper = i != 0;
                owner.AddComponent<Carrier>(); owner.AddComponent<CombatVerbs>();
                var art = RosterBook.Load().FindPersonArt(people[i].Id);
                var visual = owner.AddComponent<CharacterVisual>();
                var model = new GameObject("Visual"); model.transform.SetParent(owner.transform, false);
                visual.SetModelRoot(model.transform);
                visual.ApplyModel(art.Model, art.Tint, art.Clips, art.Palette, art.PetModel);
                round.Register(motor);
            }
            GameServices.Match.StartMatch(); round.BeginRound();
            for (int i = 0; i < 4; i++) round.PlayerAt(i).IsDefender = i == 0;
            var cameraOwner = new GameObject("Isolated replay camera"); cameraOwner.tag = "MainCamera";
            var camera = cameraOwner.AddComponent<Camera>(); camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(.3f, .3f, .3f);
            cameraOwner.AddComponent<CameraRig>().Follow(round.PlayerAt(1));
            CatchReconstruction.Attach(cameraOwner);
            yield return null;
        }

        [UnityTest]
        public IEnumerator ReplayHoldsOneCapturedTagFrameThenFadesWithoutPausingPlay()
        {
            yield return OpenIsolatedCatchWorld(); Stage();
            yield return new WaitForSeconds(.4f);
            var actor = GameServices.Round.PlayerAt(0);
            var victim = GameServices.Round.PlayerAt(1);
            var view = Object.FindAnyObjectByType<CatchReconstruction>();
            var settings = Settings.SettingsStore.Current;
            bool reduced = settings.ReducedUiMotion, cinematic = settings.CinematicCameraMotion;
            settings.ReducedUiMotion = false; settings.CinematicCameraMotion = true;
            int rendered = 0;
            Camera.CameraCallback probe = camera => { if (camera.name == "~CatchPlaybackCamera") rendered++; };
            Camera.onPreRender += probe;
            Texture2D image = null;
            try
            {
                int score = GameServices.Match.ScoreFor(0);
                Assert.IsTrue(actor.GetComponent<CombatVerbs>().HostResolvePunch(
                    actor.transform.position, actor.transform.forward));
                Assert.IsTrue(view.Playing);
                Assert.That(view.Remaining, Is.InRange(3.9f, 3.94f),
                    "Play 2.5 seconds, hold 1.25 seconds, then fade for .18 seconds.");
                yield return new WaitForSeconds(.3f);
                var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
                var clock = typeof(CatchReconstruction).GetField("_began", flags);
                var step = typeof(CatchReconstruction).GetMethod("LateUpdate", flags);
                var target = (RenderTexture)typeof(CatchReconstruction).GetField("_target", flags).GetValue(view);
                var picture = (UnityEngine.UI.RawImage)typeof(CatchReconstruction).GetField("_picture", flags).GetValue(view);
                image = new Texture2D(target.width, target.height, TextureFormat.RGBA32, false);
                Color32[] ReadFrame()
                {
                    var previous = RenderTexture.active;
                    try
                    {
                        RenderTexture.active = target;
                        image.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0); image.Apply();
                        return image.GetPixels32();
                    }
                    finally { RenderTexture.active = previous; }
                }
                void Seek(float elapsed) { clock.SetValue(view, Time.unscaledTime - elapsed); step.Invoke(view, null); }
                int before = rendered;
                Seek(2.45f); Assert.Greater(rendered, before, "Animation must still render before its boundary.");
                before = rendered;
                Seek(2.5f); Assert.AreEqual(before + 1, rendered, "Capture the final tag frame at the boundary.");
                var held = ReadFrame(); int captured = rendered;
                float liveTime = Time.time, stun = victim.StunLeft;
                actor.transform.position += Vector3.right;
                yield return null;
                Assert.Greater(Time.time, liveTime, "The live world must advance during the frozen image.");
                Assert.Less(victim.StunLeft, stun, "Recovery must continue behind the image.");
                Seek(3.74f);
                Assert.IsTrue(view.Playing); Assert.AreEqual(1f, picture.color.a, .001f);
                Assert.AreEqual(captured, rendered, "Live backgrounds must not render into the held frame.");
                CollectionAssert.AreEqual(held, ReadFrame(), "The entire rendered image must stay frozen.");
                Seek(3.84f);
                Assert.IsTrue(view.Playing); Assert.That(picture.color.a, Is.InRange(.45f, .55f));
                Assert.AreEqual(captured, rendered);
                Assert.IsTrue(actor.CanAct()); Assert.IsFalse(victim.CanAct());
                Assert.AreEqual(score + MatchRules.PointsFor(ScoreEvent.Tag), GameServices.Match.ScoreFor(0));
                Seek(3.94f); Assert.IsFalse(view.Playing);
                Assert.IsFalse(victim.CanAct(), "Presentation completion must not cancel recovery.");
            }
            finally
            {
                Camera.onPreRender -= probe; if (image != null) Object.Destroy(image);
                view.End(); settings.ReducedUiMotion = reduced; settings.CinematicCameraMotion = cinematic;
            }
        }

        [UnityTest]
        public IEnumerator FrozenReplayStillHonoursRecoveryAndComfortInterruptions()
        {
            yield return OpenIsolatedCatchWorld();
            var view = Object.FindAnyObjectByType<CatchReconstruction>();
            var settings = Settings.SettingsStore.Current;
            bool reduced = settings.ReducedUiMotion, cinematic = settings.CinematicCameraMotion;
            var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
            var clock = typeof(CatchReconstruction).GetField("_began", flags);
            var step = typeof(CatchReconstruction).GetMethod("LateUpdate", flags);
            try
            {
                for (int interruption = 0; interruption < 3; interruption++)
                {
                    settings.ReducedUiMotion = false; settings.CinematicCameraMotion = true;
                    Stage(); yield return new WaitForSeconds(1f);
                    var actor = GameServices.Round.PlayerAt(0); var victim = GameServices.Round.PlayerAt(1);
                    Assert.IsTrue(actor.GetComponent<CombatVerbs>().HostResolvePunch(
                        actor.transform.position, actor.transform.forward));
                    yield return new WaitForSeconds(.3f);
                    clock.SetValue(view, Time.unscaledTime - 2.6f); step.Invoke(view, null);
                    Assert.IsTrue(view.Playing);
                    if (interruption == 0) victim.ClearStun();
                    else if (interruption == 1) settings.ReducedUiMotion = true;
                    else settings.CinematicCameraMotion = false;
                    step.Invoke(view, null);
                    Assert.IsFalse(view.Playing, "A frozen image cannot bypass recovery or comfort exits.");
                    if (interruption != 0) Assert.IsFalse(victim.CanAct());
                }
            }
            finally { view.End(); settings.ReducedUiMotion = reduced; settings.CinematicCameraMotion = cinematic; }
        }

        private IEnumerator CheckReplayMotion(bool isolated)
        {
            // Keep the software-rendered validation scene within its texture budget.
            // This changes texture detail, never geometry or recorded motion.
            int previousMipLimit = QualitySettings.globalTextureMipmapLimit;
            QualitySettings.globalTextureMipmapLimit = 2;
            if (isolated) yield return OpenIsolatedCatchWorld(); else yield return Open();
            Stage();
            var round = GameServices.Round;
            var actor = round.PlayerAt(0); var victim = round.PlayerAt(1);
            var view = Object.FindAnyObjectByType<CatchReconstruction>();
            var settings = Settings.SettingsStore.Current;
            bool reduced = settings.ReducedUiMotion, cinematic = settings.CinematicCameraMotion;
            settings.ReducedUiMotion = false; settings.CinematicCameraMotion = true;
            Camera.CameraCallback captureProbe = null;
            try
            {
                Vector3 actorStart = actor.transform.position, victimStart = victim.transform.position;
                float began = Time.time;
                while (Time.time - began < 3.2f)
                {
                    float fraction = Mathf.Clamp01((Time.time - began) / 3.2f);
                    actor.transform.position = actorStart + Vector3.back * (1 - fraction) * 2f;
                    victim.transform.position = victimStart;
                    yield return null;
                }
                Assert.IsTrue(actor.GetComponent<CombatVerbs>().HostResolvePunch(
                    actor.transform.position, actor.transform.forward));
                Assert.IsTrue(view.Playing);
                yield return new WaitForSeconds(.6f);
                var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
                var clock = typeof(CatchReconstruction).GetField("_began", flags);
                var step = typeof(CatchReconstruction).GetMethod("LateUpdate", flags);
                var copy = (MatchPoseHistory.Copy)typeof(CatchReconstruction).GetField("_actorCopy", flags).GetValue(view);
                var target = (RenderTexture)typeof(CatchReconstruction).GetField("_target", flags).GetValue(view);
                var presentEffect = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                presentEffect.name = "Unrecorded present-time effect";
                presentEffect.GetComponent<Collider>().enabled = false;
                VfxRenderTag.Attach(presentEffect);
                ComicPopup.Spawn(victim.transform.position + Vector3.up, "PRESENT TIME", Color.magenta);
                // The original burst has expired by the longer follow-through capture.
                // A real concurrent burst still must not enter this past recording.
                ImpactBurst.SpawnAt(victim.transform.position);
                var liveParticles = Object.FindObjectsByType<ParticleSystemRenderer>();
                Assert.IsNotEmpty(liveParticles, "Exercise an actual concurrent impact burst.");
                var effectRenderers = Object.FindObjectsByType<VfxRenderTag>()
                    .SelectMany(e => e.GetComponentsInChildren<Renderer>(true))
                    .Concat(liveParticles).Distinct().ToArray();
                var popups = Object.FindObjectsByType<ComicPopup>()
                    .SelectMany(e => e.GetComponentsInChildren<Canvas>(true)).ToArray();
                Assert.IsNotEmpty(effectRenderers); Assert.IsNotEmpty(popups);
                var effectFlags = effectRenderers.Select(r => r.forceRenderingOff).ToArray();
                var popupFlags = popups.Select(c => c.enabled).ToArray();
                bool observed = false, isolatedCapture = true;
                captureProbe = camera =>
                {
                    if (camera.name != "~CatchPlaybackCamera") return;
                    observed = true;
                    isolatedCapture &= effectRenderers.All(r => r == null || r.forceRenderingOff)
                        && popups.All(c => c == null || !c.enabled);
                };
                Camera.onPreRender += captureProbe;
                Vector3 middle = default, late = default;
                foreach (float elapsed in new[] { .4f, 1.3f, 2.1f, 2.4f })
                {
                    clock.SetValue(view, Time.unscaledTime - elapsed);
                    step.Invoke(view, null);
                    Assert.IsTrue(view.Playing);
                    if (elapsed == 1.3f) middle = copy.Root.transform.position;
                    if (elapsed == 2.4f) late = copy.Root.transform.position;
                    var previous = RenderTexture.active;
                    var image = new Texture2D(target.width, target.height, TextureFormat.RGB24, false);
                    try
                    {
                        RenderTexture.active = target;
                        image.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0); image.Apply();
                        System.IO.Directory.CreateDirectory("Logs/catch-replay-motion");
                        System.IO.File.WriteAllBytes("Logs/catch-replay-motion/at-" + elapsed.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture) + ".png", image.EncodeToPNG());
                    }
                    finally { RenderTexture.active = previous; Object.Destroy(image); }
                }
                Assert.IsTrue(observed && isolatedCapture, "Present-time effects/callouts leaked into recorded approach.");
                CollectionAssert.AreEqual(effectFlags, effectRenderers.Select(r => r.forceRenderingOff).ToArray());
                CollectionAssert.AreEqual(popupFlags, popups.Select(c => c.enabled).ToArray());
                Assert.That(Vector3.Distance(middle, late), Is.GreaterThan(.15f),
                    "The late replay must advance through recorded approach, not hold its first short clip.");
                Vector3 live = actor.transform.position;
                var history = Object.FindAnyObjectByType<MatchPoseHistory>();
                float future = Time.time + 20;
                for (int i = 0; i < MatchPoseHistory.Samples + 2; i++)
                {
                    history.ForSeat(0).Record(future + i * MatchPoseHistory.Interval);
                    history.ForSeat(1).Record(future + i * MatchPoseHistory.Interval);
                }
                clock.SetValue(view, Time.unscaledTime - 2.4f); step.Invoke(view, null);
                Assert.That(Vector3.Distance(late, copy.Root.transform.position), Is.LessThan(.001f),
                    "Ongoing live recording must not overwrite this catch's retained clip.");
                Assert.That(actor.transform.position, Is.EqualTo(live));
                Assert.IsFalse(victim.CanAct());
                // Let the same retained clip play on its real unscaled clock too.
                float realStart = Time.unscaledTime;
                clock.SetValue(view, realStart);
                bool haveMiddle = false, haveLate = false;
                Vector3 realMiddle = default, realLate = default;
                while (view.Playing && Time.unscaledTime - realStart < CatchReconstruction.ReplayDuration + .5f)
                {
                    yield return null;
                    if (!view.Playing) break;
                    float elapsed = Time.unscaledTime - realStart;
                    if (!haveMiddle && elapsed >= 1.3f) { realMiddle = copy.Root.transform.position; haveMiddle = true; }
                    if (!haveLate && elapsed >= 2.3f) { realLate = copy.Root.transform.position; haveLate = true; }
                }
                Assert.IsFalse(view.Playing, "The real replay clock must end after its finite tag-frame hold and fade.");
                Assert.IsTrue(haveMiddle && haveLate);
                Assert.That(Vector3.Distance(realMiddle, realLate), Is.GreaterThan(.15f));
            }
            finally
            {
                Camera.onPreRender -= captureProbe;
                view.End(); settings.ReducedUiMotion = reduced; settings.CinematicCameraMotion = cinematic;
                QualitySettings.globalTextureMipmapLimit = previousMipLimit;
            }
        }

        [UnityTest, Timeout(360000)]
        public IEnumerator CatchAnimationAndHoldReturnBeforeControl()
        {
            yield return Open(); Stage(); yield return new WaitForSeconds(.4f);
            var round = GameServices.Round;
            var taya = round.PlayerAt(0); var victim = round.PlayerAt(1);
            var view = Object.FindAnyObjectByType<CatchReconstruction>();
            var settings = Settings.SettingsStore.Current;
            bool reduced = settings.ReducedUiMotion, cinematic = settings.CinematicCameraMotion;
            settings.ReducedUiMotion = false; settings.CinematicCameraMotion = true;
            try
            {
                Vector3 tayaAt = taya.transform.position;
                int score = GameServices.Match.ScoreFor(0);
                Assert.IsTrue(taya.GetComponent<CombatVerbs>().HostResolvePunch(tayaAt, taya.transform.forward));
                Assert.IsTrue(view.Playing);
                Assert.That(view.Remaining, Is.InRange(3.9f, 3.94f));
                Assert.AreEqual(score + MatchRules.PointsFor(ScoreEvent.Tag), GameServices.Match.ScoreFor(0));
                Assert.IsTrue(taya.CanAct());
                Assert.AreEqual(tayaAt, taya.transform.position);
                yield return new WaitForSecondsRealtime(1.3f);
                Assert.IsTrue(view.Playing, "The old 1.1-second cutoff returned too early.");
                Assert.IsFalse(victim.CanAct());
                yield return GameplayShots.Render(Camera.main, "catch-after-one-second", true,
                    outDir: "Logs/catch-replay-duration");
                yield return new WaitForSecondsRealtime(view.Remaining + .1f);
                Assert.IsFalse(view.Playing);
                Assert.IsFalse(victim.CanAct(), "Replay duration must not remove the five-second penalty.");
                Assert.IsTrue(Camera.main.GetComponent<CameraRig>().IsFollowing(victim));
                yield return GameplayShots.Render(Camera.main, "recovery-after-replay", true,
                    outDir: "Logs/catch-replay-duration");
            }
            finally
            {
                settings.ReducedUiMotion = reduced; settings.CinematicCameraMotion = cinematic;
                view.End();
            }
        }

        [UnityTest]
        public IEnumerator TenActualCatchesPreserveTayaAndReturnBeforeControl()
        {
            yield return Open();
            var round = GameServices.Round; var taya = round.PlayerAt(0); var victim = round.PlayerAt(1);
            var view = Object.FindAnyObjectByType<CatchReconstruction>(); Assert.IsNotNull(view);
            var rig = Camera.main.GetComponent<CameraRig>(); Assert.IsTrue(rig.IsFollowing(victim));
            float lens = Camera.main.fieldOfView;
            bool reduced = Settings.SettingsStore.Current.ReducedUiMotion;
            Settings.SettingsStore.Current.ReducedUiMotion = false;
            try
            {
                for (int n = 0; n < 10; n++)
                {
                    Stage(); yield return new WaitForSeconds(.4f);
                    Assert.IsTrue(victim.IsTaggable());
                    Vector3 contact = victim.transform.position, tayaAt = taya.transform.position;
                    int score = GameServices.Match.ScoreFor(0);
                    Assert.IsTrue(taya.GetComponent<CombatVerbs>().HostResolvePunch(tayaAt, taya.transform.forward));
                    Assert.AreEqual(score + MatchRules.PointsFor(ScoreEvent.Tag), GameServices.Match.ScoreFor(0));
                    Assert.IsTrue(view.Playing, "Accepted tag must produce the victim view from actual prior poses.");
                    Assert.AreEqual(tayaAt, taya.transform.position, "The reconstruction must not move the live taya.");
                    Assert.IsTrue(taya.CanAct(), "Presentation must not add taya recovery.");
                    Assert.Greater((victim.transform.position - contact).magnitude, 2, "Authority still teleports immediately.");
                    float remaining = view.Remaining;
                    MatchFlair.Play(MatchFlair.Kind.Tag, 0, 1, contact);
                    Assert.LessOrEqual(view.Remaining, remaining + .01f, "Duplicate delivery must not restart the camera.");
                    yield return new WaitForSecondsRealtime(.18f);
                    var copies = GameObject.Find("~CatchPlaybackCopies"); Assert.IsNotNull(copies);
                    Assert.IsEmpty(copies.GetComponentsInChildren<Collider>(true));
                    Assert.IsEmpty(copies.GetComponentsInChildren<MonoBehaviour>(true), "Recorded bodies cannot execute gameplay scripts.");
                    Assert.IsTrue(copies.GetComponentsInChildren<Renderer>(true).All(r => r.forceRenderingOff),
                        "Copies must stay invisible to ordinary world cameras.");
                    if (n == 0)
                        yield return GameplayShots.Render(Camera.main, "victim-catch", true, outDir: "Logs/catch-reconstruction-v2");
                    if (n == 0)
                    {
                        yield return new WaitForSecondsRealtime(.30f);
                        yield return GameplayShots.Render(Camera.main, "victim-followthrough", true, outDir: "Logs/catch-reconstruction-v2");
                    }
                    yield return new WaitForSecondsRealtime(view.Remaining + .1f);
                    Assert.IsFalse(view.Playing); Assert.IsFalse(victim.CanAct(), "Camera exit does not cancel the tag penalty.");
                    Assert.IsTrue(rig.IsFollowing(victim)); Assert.AreEqual(lens, Camera.main.fieldOfView, .1f);
                    Assert.IsTrue(victim.GetComponent<Carrier>().Held.GetComponentsInChildren<Renderer>(true).All(r => !r.forceRenderingOff),
                        "Held-item render flags must restore even when enumerated through two owners.");
                }
            }
            finally { Settings.SettingsStore.Current.ReducedUiMotion = reduced; view.End(); }
        }
        [UnityTest]
        public IEnumerator EventBeforeRecoveryWaitsForStateAndTimesOutSafely()
        {
            yield return Open(); Stage(); yield return new WaitForSeconds(.4f);
            var round = GameServices.Round; var victim = round.PlayerAt(1);
            var view = Object.FindAnyObjectByType<CatchReconstruction>();
            var contact = victim.transform.position;
            MatchFlair.Play(MatchFlair.Kind.Tag, 0, 1, contact);
            Assert.IsFalse(view.Playing, "A cosmetic event alone cannot create a recovery window.");
            yield return new WaitForSecondsRealtime(.1f);
            victim.ApplyStagger(Balance.TagStunTime);
            victim.Teleport(victim.SpawnPosition);
            yield return null;
            Assert.IsTrue(view.Playing, "Later authoritative recovery should unlock retained contact, not a new penalty.");
            victim.ClearStun(); yield return null; Assert.IsFalse(view.Playing);
            Stage(); yield return new WaitForSeconds(.4f);
            MatchFlair.Play(MatchFlair.Kind.Tag, 0, 1, victim.transform.position);
            yield return new WaitForSecondsRealtime(.8f);
            victim.ApplyStagger(Balance.TagStunTime); victim.Teleport(victim.SpawnPosition);
            yield return null;
            Assert.IsFalse(view.Playing, "An expired event must not attach itself to a later recovery.");
        }

        [UnityTest]
        public IEnumerator IndependentCameraControlKeepsRecoveryInFirstPerson()
        {
            yield return Open(); Stage(); yield return new WaitForSeconds(.4f);
            var victim = GameServices.Round.PlayerAt(1);
            bool previous = Settings.SettingsStore.Current.CinematicCameraMotion;
            try
            {
                Settings.SettingsStore.Current.CinematicCameraMotion = false;
                GameServices.Round.ResolveTag(GameServices.Round.PlayerAt(0), victim);
                yield return new WaitForSecondsRealtime(.3f);
                Assert.IsFalse(Object.FindAnyObjectByType<CatchReconstruction>().Playing);
                Assert.Greater(victim.StunLeft, 4, "Comfort controls cannot shorten recovery.");
                var eye = victim.transform.position + Vector3.up * (CameraRig.PersonCapsuleHeight * .5f + CameraRig.FppEyeHeight);
                Assert.Less(Vector3.Distance(Camera.main.transform.position, eye), .05f,
                    "Disabling cinematic camera movement also suppresses the automatic recovery orbit.");
            }
            finally { Settings.SettingsStore.Current.CinematicCameraMotion = previous; }
        }

        [UnityTest]
        public IEnumerator LateEventsAndReducedMotionNeverExtendRecovery()
        {
            yield return Open(); Stage(); yield return new WaitForSeconds(.4f);
            var victim = GameServices.Round.PlayerAt(1); var view = Object.FindAnyObjectByType<CatchReconstruction>();
            victim.ApplyStagger(.2f); MatchFlair.Play(MatchFlair.Kind.Tag, 0, 1, victim.transform.position);
            Assert.IsFalse(view.Playing, "A late event with no useful recovery window has no takeover.");
            victim.ClearStun();
            bool reduced = Settings.SettingsStore.Current.ReducedUiMotion;
            try
            {
                Settings.SettingsStore.Current.ReducedUiMotion = true;
                GameServices.Round.ResolveTag(GameServices.Round.PlayerAt(0), victim);
                Assert.IsFalse(view.Playing); Assert.Greater(victim.StunLeft, 4);
            }
            finally { Settings.SettingsStore.Current.ReducedUiMotion = reduced; }
        }
    }
}
