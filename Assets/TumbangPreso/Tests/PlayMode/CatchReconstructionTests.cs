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
        [UnityTearDown] public IEnumerator After() => PlayModeWorld.Reset();
        private static IEnumerator Open()
        {
            SceneFlow.Networked = false; SceneFlow.SetSelectedRules(CustomGameRules.Defaults(GameMode.Classic));
            GameLaunch.SoloSeat = 1; GameLaunch.GuidedTutorial = false;
            yield return SceneManager.LoadSceneAsync("Eskinita"); yield return new WaitForSeconds(.2f);
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

        private IEnumerator CheckContactReach(float distance)
        {
            yield return OpenIsolatedCatchWorld(); Stage();
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
                var camera = (Camera)Field("_camera"); var target = (RenderTexture)Field("_target");
                var picture = (UnityEngine.UI.RawImage)Field("_picture");
                string directory = "Logs/tag-contact/reach-" + distance.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture);
                System.IO.Directory.CreateDirectory(directory);
                float lastTime = -1, lastGap = float.MaxValue, lastPoseTime = 0, lastAlpha = 0;
                rendered = c =>
                {
                    if (c != camera) return;
                    lastTime = Time.unscaledTime - began;
                    lastPoseTime = Mathf.Lerp(start, end, Mathf.Clamp01(lastTime / duration));
                    var bounds = victimCopy.Renderers[0].bounds;
                    foreach (var renderer in victimCopy.Renderers) bounds.Encapsulate(renderer.bounds);
                    lastGap = Vector3.Distance(hand.position, bounds.ClosestPoint(hand.position));
                    lastAlpha = picture.color.a;
                };
                Camera.onPostRender += rendered;
                float next = 0, bestDistance = float.MaxValue, bestGap = float.MaxValue, bestAlpha = 0;
                int frames = 0;
                var times = new System.Collections.Generic.List<string>();
                while (view.Playing && Time.unscaledTime - began < duration + .5f)
                {
                    // Read a completed real frame. Seeking bones and rendering several
                    // times in one frame can reuse stale native skinning matrices.
                    yield return null;
                    if (!view.Playing || target == null || lastTime < next) continue;
                    next = lastTime + .075f;
                    var previous = RenderTexture.active;
                    var image = new Texture2D(target.width, target.height, TextureFormat.RGB24, false);
                    try
                    {
                        RenderTexture.active = target; image.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0); image.Apply();
                        byte[] bytes = image.EncodeToPNG();
                        System.IO.File.WriteAllBytes(directory + "/frame-" + frames.ToString("D3") + ".png", bytes);
                        times.Add(lastTime.ToString("F6", System.Globalization.CultureInfo.InvariantCulture));
                        float proximity = Mathf.Abs(lastPoseTime - (contact + .17f));
                        if (proximity < bestDistance)
                        {
                            bestDistance = proximity; bestGap = lastGap; bestAlpha = lastAlpha;
                            System.IO.File.WriteAllBytes(directory + "/contact.png", bytes);
                        }
                        frames++;
                    }
                    finally { RenderTexture.active = previous; Object.Destroy(image); }
                }
                System.IO.File.WriteAllLines(directory + "/times.txt", times);
                string values = "distance=" + distance + " contactGap=" + bestGap + " contactAlpha=" + bestAlpha
                    + " sampleError=" + bestDistance + " frames=" + frames;
                System.IO.File.WriteAllText(directory + "/measurements.txt", values); Debug.Log(values);
                Assert.That(frames, Is.GreaterThan(8));
                Assert.That(bestDistance, Is.LessThan(.09f));
                Assert.That(bestGap, Is.LessThan(.08f), "The actual rendered reaching hand must reach the accepted victim's visible body bounds.");
                Assert.That(bestAlpha, Is.GreaterThan(.95f), "Do not fade out while the hand first reaches the target.");
                Assert.That(Vector3.Distance(restScale, sourceHand.parent.localScale), Is.LessThan(.001f), "Temporary limb extension must restore.");
                Assert.AreEqual(1, acceptedTags, "Animation must not create another accepted tag.");
            }
            finally
            {
                GameServices.Match.Scored -= scored;
                Camera.onPostRender -= rendered; view.End();
                settings.ReducedUiMotion = reduced; settings.CinematicCameraMotion = cinematic;
            }
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
                foreach (float elapsed in new[] { .4f, 1.5f, 2.5f, 2.9f })
                {
                    clock.SetValue(view, Time.unscaledTime - elapsed);
                    step.Invoke(view, null);
                    Assert.IsTrue(view.Playing);
                    if (elapsed == 1.5f) middle = copy.Root.transform.position;
                    if (elapsed == 2.5f) late = copy.Root.transform.position;
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
                clock.SetValue(view, Time.unscaledTime - 2.5f); step.Invoke(view, null);
                Assert.That(Vector3.Distance(late, copy.Root.transform.position), Is.LessThan(.001f),
                    "Ongoing live recording must not overwrite this catch's retained clip.");
                Assert.That(actor.transform.position, Is.EqualTo(live));
                Assert.IsFalse(victim.CanAct());
                // Let the same retained clip play on its real unscaled clock too.
                float realStart = Time.unscaledTime;
                clock.SetValue(view, realStart);
                bool haveMiddle = false, haveLate = false;
                Vector3 realMiddle = default, realLate = default;
                while (view.Playing && Time.unscaledTime - realStart < 3.5f)
                {
                    yield return null;
                    if (!view.Playing) break;
                    float elapsed = Time.unscaledTime - realStart;
                    if (!haveMiddle && elapsed >= 1.3f) { realMiddle = copy.Root.transform.position; haveMiddle = true; }
                    if (!haveLate && elapsed >= 2.3f) { realLate = copy.Root.transform.position; haveLate = true; }
                }
                Assert.IsFalse(view.Playing, "The real replay clock must end without a held tail.");
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
        public IEnumerator CatchLastsAboutThreeSecondsAndReturnsBeforeControl()
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
                Assert.That(view.Remaining, Is.InRange(2.9f, 3.01f));
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
