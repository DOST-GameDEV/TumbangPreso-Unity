using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using NUnit.Framework;
using TumbangPreso.CameraSystem;
using UnityEngine;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;

namespace TumbangPreso.PlayTests
{
    public sealed class LocalReplayStreamingTests
    {
        private static readonly BindingFlags Flags=BindingFlags.Instance|BindingFlags.NonPublic;
        private LocalReplayPlayback _viewer;
        [UnitySetUp]public IEnumerator Before()=>PlayModeWorld.Reset();
        [UnityTearDown]public IEnumerator After()
        {
            if(_viewer!=null)Object.Destroy(_viewer.gameObject);
            yield return PlayModeWorld.Reset();
        }
        private static LocalReplayEntry Entry()
        {
            string path=Environment.GetEnvironmentVariable("TUMP_REPLAY_STREAM");
            Assert.IsFalse(string.IsNullOrEmpty(path),"Supply the actual completed Arena replay fixture.");
            return LocalReplayStore.List(Path.GetDirectoryName(path)).Single(e=>e.Directory==path);
        }
        private IEnumerator Open(LocalReplayEntry entry)
        {
            Assert.IsTrue(LocalReplayPlayback.Open(entry));_viewer=Object.FindAnyObjectByType<LocalReplayPlayback>();
            float until=Time.realtimeSinceStartup+25;
            while(_viewer.Frame==null&&_viewer.Error==null&&Time.realtimeSinceStartup<until)yield return null;
            Assert.IsNull(_viewer.Error,_viewer.Error);Assert.IsNotNull(_viewer.Frame);
            while(Pending(_viewer)&&Time.realtimeSinceStartup<until)yield return null;
            Assert.IsFalse(Pending(_viewer));
        }
        private static bool Pending(LocalReplayPlayback viewer)=>typeof(LocalReplayPlayback).GetField("_loading",Flags).GetValue(viewer)!=null;
        [UnityTest]public IEnumerator ContinuousOneTimesPlaybackDoesNotFreezeAtSavedSegmentSeams()=>Continuous(1);
        [UnityTest]public IEnumerator ContinuousFourTimesPlaybackDoesNotFreezeAtSavedSegmentSeams()=>Continuous(4);
        private IEnumerator Continuous(float speed)
        {
            var entry=Entry();Assert.GreaterOrEqual(entry.Manifest.Segments.Count,4);
            yield return Open(entry);
            float target=entry.Manifest.Segments[3].Offset+.1f;
            _viewer.SetSpeed(speed);_viewer.TogglePause();
            float missed=0,largest=0;int frozen=0;float until=Time.realtimeSinceStartup+35;
            while(_viewer.Position<target&&_viewer.Error==null&&Time.realtimeSinceStartup<until)
            {
                float before=_viewer.Position;yield return null;
                float deficit=Mathf.Max(0,Time.unscaledDeltaTime-(_viewer.Position-before)/speed);
                missed+=deficit;largest=Mathf.Max(largest,deficit);
                if(_viewer.Position==before)frozen++;
            }
            _viewer.TogglePause();
            Debug.Log("[ReplayStreaming] speed="+speed+" position="+_viewer.Position+" missingWallSeconds="+missed+" largest="+largest+" frozenFrames="+frozen);
            Assert.IsNull(_viewer.Error,_viewer.Error);Assert.GreaterOrEqual(_viewer.Position,target);
            Assert.Less(missed,.1f,"Continuous viewing must not lose hundreds of milliseconds at disk/decode seams.");
        }
        [UnityTest]public IEnumerator AFailedObsoleteSeekCannotPoisonTheLatestValidPosition()
        {
            var entry=Entry();Assert.Greater(entry.Manifest.Segments.Count,8);yield return Open(entry);
            // Only this detached manifest is changed; recorded files are untouched.
            entry.Manifest.Segments[8].Sha256="invalid-test-digest";
            _viewer.Seek(entry.Manifest.Segments[8].Offset+.1f);
            float wanted=entry.Manifest.Segments[4].Offset+.1f;_viewer.Seek(wanted);
            float until=Time.realtimeSinceStartup+12;
            while(Pending(_viewer)&&_viewer.Error==null&&Time.realtimeSinceStartup<until)yield return null;
            for(int n=0;n<3;n++)yield return null;
            Assert.IsNull(_viewer.Error,"An obsolete failed read must not override the latest valid seek.");
            Assert.AreEqual(wanted,_viewer.Position,.001f);Assert.IsNotNull(_viewer.Frame);
            var view=(RecordedWorldView)typeof(LocalReplayPlayback).GetField("_view",Flags).GetValue(_viewer);
            var clip=(RecordedMatchClip)typeof(RecordedWorldView).GetField("_clip",Flags).GetValue(view);
            Assert.AreEqual(LocalReplayStore.Read(entry,4).Id,clip.Id,"The actual rendered view must belong to the final requested segment.");
        }
        [UnityTest]public IEnumerator ClosingDuringARealReadCannotRestoreTheOldViewer()
        {
            var entry=Entry();yield return Open(entry);
            _viewer.Seek(entry.Manifest.Segments[8].Offset+.1f);
            var pending=(Task)typeof(LocalReplayPlayback).GetField("_loading",Flags).GetValue(_viewer);Assert.IsNotNull(pending);
            Object.Destroy(_viewer.gameObject);yield return null;
            float until=Time.realtimeSinceStartup+10;while(!pending.IsCompleted&&Time.realtimeSinceStartup<until)yield return null;
            Assert.IsTrue(pending.IsCompleted);for(int n=0;n<3;n++)yield return null;
            Assert.IsFalse(LocalReplayPlayback.Active);Assert.IsNull(Object.FindAnyObjectByType<LocalReplayPlayback>());
        }
    }
}
