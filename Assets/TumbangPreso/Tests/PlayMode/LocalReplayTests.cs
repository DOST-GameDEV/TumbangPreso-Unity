using System;
using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using TumbangPreso.CameraSystem;
using TumbangPreso.Core;
using TumbangPreso.UI;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace TumbangPreso.PlayTests
{
    public sealed class LocalReplayTests
    {
        private string _folder;
        private byte[] _oldLocation;
        private string Location => Path.Combine(ProfilePaths.Root, "replay-folder.txt");
        [UnitySetUp] public IEnumerator Before()
        {
            yield return PlayModeWorld.Reset();
            _oldLocation = File.Exists(Location) ? File.ReadAllBytes(Location) : null;
            _folder = Path.GetFullPath(Path.Combine("Logs/local-replay1007", Guid.NewGuid().ToString("N")));
            Assert.IsTrue(LocalReplayStore.SetFolder(_folder, out string error), error);
        }
        [UnityTearDown] public IEnumerator After()
        {
            var playback = Object.FindAnyObjectByType<LocalReplayPlayback>();
            if (playback != null) Object.Destroy(playback.gameObject);
            yield return PlayModeWorld.Reset();
            if (_oldLocation != null) File.WriteAllBytes(Location, _oldLocation);
            else if (File.Exists(Location)) File.Delete(Location); // only this fixture's new isolated preference
        }
        private static RecordedMatchClip Clip(float start, float duration, int round, long id)
        {
            RecordedPoseTrack.Sample Pose(float time) => new RecordedPoseTrack.Sample { Time = time,
                Positions = new[] { new Vector3(time - start, 1, 0) }, Rotations = new[] { Quaternion.identity },
                Scales = new[] { Vector3.one }, Active = new[] { true } };
            return new RecordedMatchClip { MatchId = 123, Id = id, Round = round, Actor = 0, Subject = -1,
                Map = SceneFlow.Eskinita, Mode = GameMode.Classic, Reason = "MATCH", Start = start, End = start + duration, Contact = start,
                Objects = new[] { new RecordedObjectTrack { Kind = RecordedObjectKind.Player, Seat = 0, Skin = 0,
                    Person = "boy", VisualKey = "fixture", Pose = new RecordedPoseTrack(new[] { "" }, new[] { Pose(start), Pose(start + duration) }) } } };
        }
        [UnityTest] public IEnumerator DiskRoundTripSupportsCustomMultipleRoundsAndShortTailWithoutChangingWireMinimum()
        {
            var tail = Clip(200, .1f, 2, 2);
            Assert.IsFalse(RecordedMatchClip.TryDecode(tail.EncodeLocal(), out _, out _), "Transport's highlight minimum remains unchanged.");
            Assert.IsTrue(RecordedMatchClip.TryDecodeLocal(tail.EncodeLocal(), out _, out var decodeError), decodeError);
            var writer = new LocalReplayStore.Writer(_folder, new LocalReplayManifest { MatchId = 123, Map = SceneFlow.Eskinita,
                Mode = "Classic", Custom = true, CreatedUtc = DateTime.UtcNow.ToString("O") });
            Assert.IsTrue(writer.Append(Clip(100, 3, 1, 1))); Assert.IsTrue(writer.Append(tail)); writer.Finish(true);
            while (!writer.Completion.IsCompleted) yield return null;
            Assert.IsNull(writer.Error);
            var entry = LocalReplayStore.List(_folder).Single();
            Assert.IsTrue(entry.Manifest.Custom); Assert.IsTrue(entry.Manifest.Completed);
            Assert.AreEqual(2, entry.Manifest.Segments.Count); Assert.AreEqual(3.1f, entry.Manifest.Duration, .001f);
            Assert.AreEqual(0, entry.Manifest.Segments[0].Offset); Assert.AreEqual(3, entry.Manifest.Segments[1].Offset);
            Assert.AreEqual(2, LocalReplayStore.Read(entry, 1).Round);
            string segment = Path.Combine(entry.Directory, entry.Manifest.Segments[0].File);
            byte[] original = File.ReadAllBytes(segment), damaged = (byte[])original.Clone(); damaged[0] ^= 1;
            File.WriteAllBytes(segment, damaged);
            Assert.Throws<InvalidDataException>(() => LocalReplayStore.Read(entry, 0));
            File.WriteAllBytes(segment, original);
            entry.Manifest.Segments[0].File = "../outside.tps";
            Assert.Throws<InvalidDataException>(() => LocalReplayStore.Read(entry, 0));
        }
        [UnityTest] public IEnumerator InterruptedRecordingRemainsBrowseableAndFolderChangeDoesNotMoveExistingFootage()
        {
            var writer = new LocalReplayStore.Writer(_folder, new LocalReplayManifest { MatchId = 123, Map = SceneFlow.Eskinita, Mode = "Classic" });
            writer.Append(Clip(10, 1, 1, 1)); writer.Finish(false);
            while (!writer.Completion.IsCompleted) yield return null;
            var original = LocalReplayStore.List(_folder).Single(); Assert.IsFalse(original.Manifest.Completed);
            byte[] bytes = File.ReadAllBytes(Path.Combine(original.Directory, original.Manifest.Segments[0].File));
            Assert.IsFalse(LocalReplayStore.SetFolder("relative/folder", out _)); Assert.AreEqual(_folder, LocalReplayStore.Folder);
            Assert.IsTrue(LocalReplayStore.SetFolder(Path.Combine(_folder, "next"), out string error), error);
            Assert.AreEqual(0, LocalReplayStore.List(LocalReplayStore.Folder).Count);
            CollectionAssert.AreEqual(bytes, File.ReadAllBytes(Path.Combine(original.Directory, original.Manifest.Segments[0].File)));
        }
        [UnityTest] public IEnumerator RetainedNativeCustomRecordingCanBeViewedAndScrubbedThroughActualControls()
        {
            string folder=Environment.GetEnvironmentVariable("TUMP_REPLAY_EXISTING");
            if(string.IsNullOrEmpty(folder))Assert.Ignore("Set TUMP_REPLAY_EXISTING to a retained native recording folder.");
            var entry=LocalReplayStore.List(Path.GetDirectoryName(folder)).Single(e=>e.Directory==folder);
            Assert.IsTrue(entry.Manifest.Completed);Assert.IsTrue(entry.Manifest.Custom);
            for(int i=0;i<entry.Manifest.Segments.Count;i++)Assert.IsNotNull(LocalReplayStore.Read(entry,i));
            Assert.IsTrue(LocalReplayPlayback.Open(entry));
            var viewer=Object.FindAnyObjectByType<LocalReplayPlayback>();
            float deadline=Time.realtimeSinceStartup+15;
            while(viewer.Frame==null&&viewer.Error==null&&Time.realtimeSinceStartup<deadline)yield return null;
            Assert.IsNull(viewer.Error,viewer.Error);Assert.IsNotNull(viewer.Frame);
            Assert.IsNull(Object.FindAnyObjectByType<SliceRunner>());Assert.IsNull(Object.FindAnyObjectByType<CharacterMotor>());
            var camera=Object.FindObjectsByType<Camera>().Single(c=>c.name=="RecordedWorldCamera");
            Vector3 beforeEye=camera.transform.position;camera.transform.position+=new Vector3(2,1,0);
            camera.transform.Rotate(0,12,0,Space.World);yield return null;
            Assert.AreNotEqual(beforeEye,camera.transform.position);Assert.AreEqual(0,viewer.Position);
            Capture(viewer.Frame,"free-paused");
            var slider=GameObject.Find("Timeline").GetComponent<UnityEngine.UI.Slider>();slider.value=17;
            for(int i=0;i<25;i++)yield return null;
            Assert.IsNull(viewer.Error,viewer.Error);Assert.AreEqual(17,viewer.Position,.001f);Capture(viewer.Frame,"seek-17");
            slider.value=2;for(int i=0;i<25;i++)yield return null;
            Assert.AreEqual(2,viewer.Position,.001f);Capture(viewer.Frame,"rewind-2");
            GameObject.Find("CameraMode").GetComponent<UnityEngine.UI.Button>().onClick.Invoke();Assert.AreEqual(0,viewer.FollowSeat);
            GameObject.Find("Speed").GetComponent<UnityEngine.UI.Button>().onClick.Invoke();Assert.AreEqual(2,viewer.Speed);
            GameObject.Find("PlayPause").GetComponent<UnityEngine.UI.Button>().onClick.Invoke();float began=viewer.Position;
            yield return new WaitForSecondsRealtime(.2f);
            GameObject.Find("PlayPause").GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
            Assert.IsTrue(viewer.Paused);Assert.Greater(viewer.Position,began);Assert.Less(viewer.Position-began,.7f);
            viewer.SetClean(true);Assert.IsFalse(GameObject.Find("LocalReplayControls").transform.Find("ReplayToolbar").gameObject.activeSelf);
            viewer.SetClean(false);Assert.IsNull(viewer.Error,viewer.Error);
            var controls=GameObject.Find("LocalReplayControls").GetComponent<Canvas>();
            viewer.SetSpeed(.25f);
            var picture=GameObject.Find("CanonicalReplayCanvas").GetComponent<Canvas>();
            yield return TumpUiCapture.Capture("local-replay-toolbar1080",controls,1920,1080,false,underlays:new[]{picture},checkActionBounds:true);
            yield return TumpUiCapture.Capture("local-replay-toolbar720",controls,1280,720,false,underlays:new[]{picture},checkActionBounds:true);
            float beforeNav=viewer.Position;
            var eventSystem=UnityEngine.EventSystems.EventSystem.current;
            eventSystem.SetSelectedGameObject(slider.gameObject);
            var move=new UnityEngine.EventSystems.AxisEventData(eventSystem){moveDir=UnityEngine.EventSystems.MoveDirection.Right};
            UnityEngine.EventSystems.ExecuteEvents.Execute(slider.gameObject,move,UnityEngine.EventSystems.ExecuteEvents.moveHandler);
            Assert.AreEqual(Mathf.Min(entry.Manifest.Duration,beforeNav+5),viewer.Position,.001f,"Focused timeline navigation advances five seconds once.");
            var bar=controls.transform.Find("ReplayToolbar");
            var timeline=(RectTransform)bar.Find("Timeline");var button=(RectTransform)bar.Find("PlayPause");
            var timelineCorners=new Vector3[4];var buttonCorners=new Vector3[4];timeline.GetWorldCorners(timelineCorners);button.GetWorldCorners(buttonCorners);
            Assert.Greater(timelineCorners[0].y,buttonCorners[1].y,"Timeline must not overlap the control row.");
        }
        private static void Capture(RenderTexture target,string name)
        {
            var previous=RenderTexture.active;Texture2D image=null;
            try
            {
                RenderTexture.active=target;image=new Texture2D(target.width,target.height,TextureFormat.RGB24,false);
                image.ReadPixels(new Rect(0,0,target.width,target.height),0,0);image.Apply();
                string folder=Environment.GetEnvironmentVariable("TUMP_REPLAY_CAPTURES")??"Logs/local-replay-frames";
                Directory.CreateDirectory(folder);File.WriteAllBytes(Path.Combine(folder,name+".png"),image.EncodeToPNG());
            }
            finally{RenderTexture.active=previous;if(image!=null)Object.Destroy(image);}
        }
        [UnityTest] public IEnumerator ActualHubMenuShowsReplayLibraryAndChangesTheSameSaveFolder()
        {
            yield return SceneManager.LoadSceneAsync(SceneFlow.MatchSetup);
            float deadline=Time.realtimeSinceStartup+15;
            while(TumbangPreso.UI.Hub.TumpHub.Current==null&&Time.realtimeSinceStartup<deadline)yield return null;
            var hub=TumbangPreso.UI.Hub.TumpHub.Current;Assert.IsNotNull(hub);
            hub.Push<TumbangPreso.UI.Hub.HubMenu>();yield return null;
            var route=GameObject.Find("MenuREPLAYS").GetComponent<UnityEngine.UI.Button>();route.onClick.Invoke();
            yield return null;yield return null;
            Assert.IsNotNull(hub.Find<TumbangPreso.UI.Hub.HubReplays>());
            var field=GameObject.Find("ReplayFolder").GetComponent<UnityEngine.UI.InputField>();Assert.AreEqual(_folder,field.text);
            string next=Path.Combine(_folder,"menu-destination");field.text=next;
            GameObject.Find("ChangeReplayFolder").GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
            Assert.AreEqual(next,LocalReplayStore.Folder);Assert.AreEqual(next,field.text);
            for(int i=0;i<10;i++)yield return null;
            yield return TumpUiCapture.Capture("local-replay-library1080",hub.Canvas,1920,1080,false,checkActionBounds:true);
            yield return TumpUiCapture.Capture("local-replay-library720",hub.Canvas,1280,720,false,checkActionBounds:true);
        }
        [UnityTest] public IEnumerator ContinuousSegmentKeepsPlayingAudioWhileSeekStopsPastCues()
        {
            string folder=Environment.GetEnvironmentVariable("TUMP_REPLAY_EXISTING");
            if(string.IsNullOrEmpty(folder))Assert.Ignore("Set TUMP_REPLAY_EXISTING to a retained native recording folder.");
            var entry=LocalReplayStore.List(Path.GetDirectoryName(folder)).Single(e=>e.Directory==folder);
            Assert.IsTrue(LocalReplayPlayback.Open(entry));var viewer=Object.FindAnyObjectByType<LocalReplayPlayback>();
            float deadline=Time.realtimeSinceStartup+15;
            while(viewer.Frame==null&&viewer.Error==null&&Time.realtimeSinceStartup<deadline)yield return null;
            Assert.IsNull(viewer.Error,viewer.Error);
            using(var view=new RecordedWorldView(Object.FindAnyObjectByType<MatchInstaller>().transform,LocalReplayStore.Read(entry,0),true))
            {
                Assert.IsTrue(view.Ready,view.UnavailableReason);view.ShowOnScreen(false);
                GameServices.Audio.PlayReplayCue("can_knockdown",.25f,1,0);
                yield return null;
                var voice=Object.FindObjectsByType<AudioSource>().FirstOrDefault(v=>v.name.StartsWith("ReplayVoice")&&v.isPlaying);
                if(voice==null)Assert.Ignore("Native audio output/cue is not available; do not claim audio playback.");
                Assert.IsTrue(view.UseClip(LocalReplayStore.Read(entry,1),true));
                Assert.IsTrue(voice.isPlaying,"Continuous data segments must not cut a playing cue.");
                Assert.IsTrue(view.UseClip(LocalReplayStore.Read(entry,0),false));
                Assert.IsFalse(voice.isPlaying,"An explicit seek must stop past audio.");
            }
            viewer.Seek(2.8f);for(int i=0;i<20;i++)yield return null;
            GameServices.Audio.PlayReplayCue("can_knockdown",.25f,1,0);yield return null;
            var continuousVoice=Object.FindObjectsByType<AudioSource>().First(v=>v.name.StartsWith("ReplayVoice")&&v.isPlaying);
            viewer.TogglePause();deadline=Time.realtimeSinceStartup+3;
            while(viewer.Position<3.05f&&Time.realtimeSinceStartup<deadline)yield return null;
            viewer.TogglePause();Assert.GreaterOrEqual(viewer.Position,3.05f);
            Assert.IsTrue(continuousVoice.isPlaying,"The actual viewer's automatic read must preserve a continuing cue.");
            viewer.Seek(1);Assert.IsFalse(continuousVoice.isPlaying);
        }
        [UnityTest] public IEnumerator EveryCurrentHeroAndFamiliarBindsFromCatalogWithoutLiveMatchObjects()
        {
            string folder=Environment.GetEnvironmentVariable("TUMP_REPLAY_EXISTING");
            if(string.IsNullOrEmpty(folder))Assert.Ignore("Set TUMP_REPLAY_EXISTING to a retained native recording folder.");
            var entry=LocalReplayStore.List(Path.GetDirectoryName(folder)).Single(e=>e.Directory==folder);
            Assert.IsTrue(LocalReplayPlayback.Open(entry));var viewer=Object.FindAnyObjectByType<LocalReplayPlayback>();
            float deadline=Time.realtimeSinceStartup+15;
            while(viewer.Frame==null&&viewer.Error==null&&Time.realtimeSinceStartup<deadline)yield return null;
            Assert.IsNull(viewer.Error,viewer.Error);
            var book=RosterBook.Load();int count=Roster.GetPeople(GameMode.HeroStrike).Count;
            for(int skin=0;skin<count;skin++)
            {
                var art=book.PersonArt(skin,GameMode.HeroStrike);Assert.IsNotNull(art.Model);
                var source=new GameObject("Replay source template "+art.Id);source.SetActive(false);
                try
                {
                    var actor=source.AddComponent<CharacterMotor>();actor.enabled=false;actor.Mode=GameMode.HeroStrike;actor.CharacterIndex=skin;
                    var root=new GameObject("Visual");root.transform.SetParent(source.transform,false);
                    var visual=source.AddComponent<TumbangPreso.Visual.CharacterVisual>();visual.SetModelRoot(root.transform);
                    source.SetActive(true); // Awake initializes the material block, as in the real installer.
                    visual.ApplyModel(art.Model,art.Tint,art.Clips,art.Palette,art.PetModel);
                    source.SetActive(false);
                    RecordedObjectTrack Track(GameObject model,RecordedObjectKind kind,int index,string person)
                    {
                        var history=new MatchPoseHistory.Track(actor,model);history.Record(10);history.Record(11);
                        return new RecordedObjectTrack{Kind=kind,Seat=kind==RecordedObjectKind.Can?-1:0,Skin=index,Person=person,
                            VisualKey=MatchReplayArchive.VisualKey(model),Pose=history.Retain(10,11)};
                    }
                    var objects=new System.Collections.Generic.List<RecordedObjectTrack>{Track(visual.Model,RecordedObjectKind.Player,skin,art.Id),
                        Track(book.CanArt(0).Model,RecordedObjectKind.Can,0,null)};
                    if(visual.Companion!=null)objects.Add(Track(visual.Companion.gameObject,RecordedObjectKind.Familiar,0,art.Id));
                    var clip=new RecordedMatchClip{MatchId=123,Id=skin+1,Round=1,Actor=0,Subject=-1,Map=SceneFlow.Eskinita,
                        Mode=GameMode.HeroStrike,Start=10,End=11,Contact=10,Objects=objects.ToArray(),Reason="CATALOG CHECK"};
                    Assert.IsTrue(RecordedMatchClip.TryDecodeLocal(clip.EncodeLocal(),out var decoded,out string error),error);
                    using(var view=new RecordedWorldView(Object.FindAnyObjectByType<MatchInstaller>().transform,decoded,true))
                    {
                        Assert.IsTrue(view.Ready,art.Id+": "+view.UnavailableReason);view.ShowOnScreen(false);view.Draw(10.5f,false);
                        Capture(view.Target,"catalog-"+art.Id);
                    }
                }
                finally{Object.Destroy(source);}
                yield return null;
            }
        }
        [UnityTest] public IEnumerator ActualShortCustomMatchIsSavedAndCanBeReopenedPausedSeekedAndFollowedWithoutNewGameplay()
        {
            var oldRules = SceneFlow.SelectedRules.Clone(); bool oldPinned = SceneFlow.RulesPinned, oldBots = GameLaunch.AllBots;
            try
            {
                var rules = CustomGameRules.Defaults(GameMode.Classic); rules.Rounds = 1; rules.RoundSeconds = 30;
                SceneFlow.PinSelectedRules(rules); GameLaunch.AllBots = true;
                yield return SceneManager.LoadSceneAsync(SceneFlow.Eskinita); yield return null; yield return null;
                // Let the actual ReadyGate begin the match once. Calling Begin
                // here as well creates a second match when its countdown ends.
                float deadline = Time.realtimeSinceStartup + 50;
                while (!GameServices.Match.HasCompleted && Time.realtimeSinceStartup < deadline) yield return null;
                Assert.IsTrue(GameServices.Match.HasCompleted, "The short custom match must end naturally.");
                long identity = GameServices.Match.PresentationMatchId; int score = GameServices.Match.ScoreFor(0);
                LocalReplayEntry entry = null; deadline = Time.realtimeSinceStartup + 5;
                while (Time.realtimeSinceStartup < deadline)
                {
                    entry = LocalReplayStore.List(_folder).FirstOrDefault(e => e.Manifest.MatchId == identity && e.Manifest.Completed);
                    if (entry != null) break;
                    yield return null;
                }
                Assert.IsNotNull(entry, "The completed match must have a committed local replay manifest.");
                Assert.IsTrue(entry.Manifest.Custom); Assert.Greater(entry.Manifest.Duration, 29);
                Assert.GreaterOrEqual(entry.Manifest.Segments.Count, 9);
                foreach (var segment in entry.Manifest.Segments) Assert.AreEqual(1, segment.Round);
                Assert.IsTrue(LocalReplayPlayback.Open(entry));
                deadline = Time.realtimeSinceStartup + 15;
                var viewer = Object.FindAnyObjectByType<LocalReplayPlayback>();
                while (Object.FindAnyObjectByType<SliceRunner>() != null && Time.realtimeSinceStartup < deadline) yield return null;
                for (int i = 0; i < 40; i++) yield return null;
                Assert.IsNull(viewer.Error, viewer.Error); Assert.IsTrue(viewer.Paused);
                Assert.IsNull(Object.FindAnyObjectByType<SliceRunner>());
                Assert.IsNull(Object.FindAnyObjectByType<CharacterMotor>(), "Replay loading must not create live players.");
                Assert.IsFalse(GameServices.Match.MatchInProgress); Assert.AreEqual(identity, GameServices.Match.PresentationMatchId);
                Assert.AreEqual(score, GameServices.Match.ScoreFor(0));
                viewer.Seek(17); for (int i = 0; i < 15; i++) yield return null;
                Assert.AreEqual(17, viewer.Position, .001f); Assert.IsNull(viewer.Error, viewer.Error);
                viewer.Seek(2); viewer.SetSpeed(.5f); viewer.Follow(2);
                for (int i = 0; i < 15; i++) yield return null;
                Assert.AreEqual(2, viewer.Position, .001f); Assert.AreEqual(2, viewer.FollowSeat); Assert.AreEqual(.5f, viewer.Speed);
                viewer.TogglePause(); float before = viewer.Position; yield return new WaitForSecondsRealtime(.3f);
                viewer.TogglePause(); Assert.Greater(viewer.Position, before); Assert.Less(viewer.Position - before, .25f);
                viewer.Follow(-1); viewer.SetClean(true); viewer.SetClean(false);
                Assert.AreEqual(-1, viewer.FollowSeat); Assert.IsNull(viewer.Error, viewer.Error);
            }
            finally
            {
                GameLaunch.AllBots = oldBots; SceneFlow.AdoptRemoteRules(oldRules);
                if (oldPinned) SceneFlow.PinSelectedRules(oldRules); else SceneFlow.UnpinSelectedRules();
            }
        }
        [UnityTest] public IEnumerator HeroReplayKeepsWorldFieldsAndBothSidesOfARoundBoundary()
        {
            var oldRules=SceneFlow.SelectedRules.Clone();bool oldPinned=SceneFlow.RulesPinned;
            try
            {
                yield return MapRetrievalProbe.Load(SceneFlow.Eskinita,GameMode.HeroStrike);
                var rules=SceneFlow.SelectedRules.Clone();rules.Rounds=2;SceneFlow.PinSelectedRules(rules);
                long identity=GameServices.Match.PresentationMatchId;
                TumbangPreso.Abilities.HeroHazards.SpawnIceSheet(new Vector3(3,0,3),2,8,1,1,silent:true);
                yield return new WaitForSeconds(8);
                GameServices.Round.EndRound();GameServices.Match.AdvanceRound();
                Assert.AreEqual(2,GameServices.Match.RoundNumber);
                yield return new WaitForSeconds(7);
                GameServices.Round.EndRound();GameServices.Match.BeginIntermission();
                Assert.IsTrue(GameServices.Match.HasCompleted);
                LocalReplayEntry entry=null;float deadline=Time.realtimeSinceStartup+5;
                while(entry==null&&Time.realtimeSinceStartup<deadline)
                {
                    entry=LocalReplayStore.List(_folder).FirstOrDefault(e=>e.Manifest.MatchId==identity&&e.Manifest.Completed);
                    yield return null;
                }
                Assert.IsNotNull(entry,"Both round tails must be committed without a gap warning.");
                Assert.IsTrue(entry.Manifest.Segments.Any(s=>s.Round==1));Assert.IsTrue(entry.Manifest.Segments.Any(s=>s.Round==2));
                var clips=entry.Manifest.Segments.Select((s,i)=>LocalReplayStore.Read(entry,i)).ToArray();
                Assert.IsTrue(clips.Any(c=>c.FieldFrames.Any(f=>f.Fields.Length>0)),"The actual spawned world field must survive disk reload.");
                Assert.IsTrue(clips.All(c=>c.Objects.Count(o=>o.Kind==RecordedObjectKind.Player)==4));
                Assert.IsTrue(LocalReplayPlayback.Open(entry));var viewer=Object.FindAnyObjectByType<LocalReplayPlayback>();
                deadline=Time.realtimeSinceStartup+15;
                while(viewer.Frame==null&&viewer.Error==null&&Time.realtimeSinceStartup<deadline)yield return null;
                Assert.IsNull(viewer.Error,viewer.Error);Assert.IsNotNull(viewer.Frame);
                viewer.Seek(2);for(int i=0;i<20;i++)yield return null;Assert.IsNull(viewer.Error,viewer.Error);Capture(viewer.Frame,"hero-field");
                viewer.Seek(entry.Manifest.Segments.First(s=>s.Round==2).Offset+.5f);
                for(int i=0;i<20;i++)yield return null;Assert.IsNull(viewer.Error,viewer.Error);Capture(viewer.Frame,"hero-round2");
                viewer.Seek(2);for(int i=0;i<20;i++)yield return null;Assert.IsNull(viewer.Error,viewer.Error);
            }
            finally
            {
                SceneFlow.AdoptRemoteRules(oldRules);if(oldPinned)SceneFlow.PinSelectedRules(oldRules);else SceneFlow.UnpinSelectedRules();
            }
        }
    }
}
