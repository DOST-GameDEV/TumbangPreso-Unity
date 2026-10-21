using System.Collections;
using System.Reflection;
using NUnit.Framework;
using TumbangPreso.CameraSystem;
using TumbangPreso.Visual;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace TumbangPreso.PlayTests
{
    public sealed class RecordedWorldOwnerSpaceTests
    {
        [UnitySetUp] public IEnumerator Before() => PlayModeWorld.Reset();
        [UnityTearDown] public IEnumerator After() => PlayModeWorld.Reset();

        [UnityTest]
        public IEnumerator TransformedAndCollapsedOwnersCannotChangeRetainedWorldPoses()
        {
            yield return MapRetrievalProbe.Load("Eskinita");
            Assert.AreNotEqual(UnityEngine.Rendering.GraphicsDeviceType.Null, SystemInfo.graphicsDeviceType,
                "This regression requires a real replay rendering device.");
            var round = GameServices.Round; var actor = round.PlayerAt(0);
            var model = actor.GetComponent<CharacterVisual>().Model;
            var can = MatchReplayArchive.PropModel(round.Lata.gameObject);
            RecordedObjectTrack Track(GameObject source, RecordedObjectKind kind, int seat, int skin, string person)
            {
                var history = new MatchPoseHistory.Track(actor, source); history.Record(10); history.Record(11);
                return new RecordedObjectTrack { Kind = kind, Seat = seat, Skin = skin, Person = person,
                    VisualKey = MatchReplayArchive.VisualKey(source), Pose = history.Retain(10, 11) };
            }
            var bodyTrack = Track(model, RecordedObjectKind.Player, 0, actor.CharacterIndex,
                Core.Roster.PersonIdAt(actor.Mode, actor.CharacterIndex));
            var canTrack = Track(can, RecordedObjectKind.Can, -1, round.Lata.SkinIndex, "");
            var clip = new RecordedMatchClip { MatchId = 1, Id = 1, Round = 1, Actor = 0, Subject = -1,
                Mode = actor.Mode, Map = "Eskinita", Reason = "Recorded world owner regression", Start = 10, End = 11,
                Contact = 10.5f, Objects = new[] { bodyTrack, canTrack } };
            foreach (var scale in new[] { new Vector3(2, 3, 4), new Vector3(0, 2, 3) })
            {
                var owner = new GameObject("Transformed replay overlay owner");
                owner.transform.SetPositionAndRotation(new Vector3(8, 2, -4), Quaternion.Euler(20, 65, 10));
                owner.transform.localScale = scale;
                GameObject stage = null;
                try
                {
                    using (var view = new RecordedWorldView(owner.transform, clip))
                    {
                        Assert.IsTrue(view.Ready, view.UnavailableReason);
                        stage = (GameObject)typeof(RecordedWorldView).GetField("_stage", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(view);
                        Assert.IsNotNull(stage); Assert.IsNull(stage.transform.parent, "Recorded roots require a world-identity stage.");
                        Assert.Less(Vector3.Distance(Vector3.zero, stage.transform.position), .0001f);
                        Assert.Less(Quaternion.Angle(Quaternion.identity, stage.transform.rotation), .001f);
                        Assert.Less(Vector3.Distance(Vector3.one, stage.transform.lossyScale), .0001f);
                        view.Draw(clip.Contact, false);
                        var recordedBody = stage.transform.Find("RecordedBody-P1"); Assert.IsNotNull(recordedBody);
                        Assert.Less(Vector3.Distance(bodyTrack.Pose.Samples[0].Positions[0], recordedBody.position), .0001f);
                        Assert.Less(Quaternion.Angle(bodyTrack.Pose.Samples[0].Rotations[0], recordedBody.rotation), .001f);
                        Assert.Less(Vector3.Distance(bodyTrack.Pose.Samples[0].Scales[0], recordedBody.lossyScale), .0001f);
                        Assert.IsTrue(view.Target.IsCreated());
                        Assert.AreEqual(scale, owner.transform.localScale, "Playback must not normalize its live overlay owner.");
                    }
                    yield return null;
                    Assert.IsTrue(stage == null, "Dispose must retire the detached stage.");
                }
                finally { Object.DestroyImmediate(owner); if (stage != null) Object.DestroyImmediate(stage); }
            }
        }
    }
}
