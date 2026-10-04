using System.Collections;
using System.Reflection;
using NUnit.Framework;
using TumbangPreso.CameraSystem;
using TumbangPreso.Diagnostics;
using UnityEngine;
using UnityEngine.TestTools;

namespace TumbangPreso.PlayTests
{
    public sealed class ReplayProbeCatchContactTests
    {
        [UnitySetUp] public IEnumerator Before() => PlayModeWorld.Reset();
        [UnityTearDown] public IEnumerator After() => PlayModeWorld.Reset();
        [UnityTest] public IEnumerator ProbeContactRetainsAnActualCatchClip()
        {
            yield return MapRetrievalProbe.Load("Eskinita");
            var round=GameServices.Round;
            foreach(var actor in round.Players)actor.Teleport(new Vector3(6,.12f,-6+actor.PlayerSlot*3));
            const BindingFlags hidden=BindingFlags.Static|BindingFlags.NonPublic;
            typeof(NetReplayProbe).GetMethod("PlaceCatchParticipants",hidden).Invoke(null,new object[]{round});
            yield return new WaitForSeconds(2.5f);
            Assert.IsTrue((bool)typeof(NetReplayProbe).GetMethod("ResolveCatchContact",hidden).Invoke(null,new object[]{round}));
            yield return new WaitForSeconds(1.6f);
            var archive=Object.FindAnyObjectByType<MatchReplayArchive>();
            Assert.AreEqual(1,archive.Clips.Count,archive.LastSkip);
            Assert.IsTrue(RecordedMatchClip.TryDecode(archive.Clips[0].Bytes,out var clip,out var error),error);
            Assert.AreEqual("CATCH",clip.Reason);Assert.AreEqual(0,clip.Actor);Assert.AreEqual(1,clip.Subject);
            Assert.AreEqual(1,clip.Round);Assert.Greater(archive.Clips[0].Bytes.Length,0);
        }
    }
}
