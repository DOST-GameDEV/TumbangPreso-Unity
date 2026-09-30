using System.Collections;
using NUnit.Framework;
using TumbangPreso.CameraSystem;
using UnityEngine;
using UnityEngine.TestTools;

namespace TumbangPreso.PlayTests
{
    public sealed class LivePoseHistoryTests
    {
        private sealed class Offline : INetProvider
        {
            public bool IsHost => true;
            public bool IsNetworked => false;
            public int LocalSlot => 0;
            public int LocalPeerId => 0;
            public bool IsSeatlessReferee => false;
        }
        private INetProvider _previous;
        private GameObject _owner, _source, _stage;
        private CharacterMotor _motor;
        private MatchPoseHistory.Track _track;

        [UnitySetUp]
        public IEnumerator Before()
        {
            _previous = NetAuthority.Provider;
            NetAuthority.Provider = new Offline();
            _owner = new GameObject("Recorded actor");
            _motor = _owner.AddComponent<CharacterMotor>();
            _motor.enabled = false;
            _source = new GameObject("Recorded model");
            _source.transform.SetParent(_owner.transform, false);
            _stage = new GameObject("Render-only stage"); _stage.SetActive(false);
            _track = new MatchPoseHistory.Track(_motor, _source);
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator After()
        {
            Object.Destroy(_owner); Object.Destroy(_stage);
            NetAuthority.Provider = _previous;
            yield return null;
        }

        private MatchPoseHistory.Copy Copy() => _track.Clone(_stage.transform);
        private void Apply(MatchPoseHistory.Copy copy, float time, bool retained)
        {
            if (retained)
            {
                var clip = _track.Retain(10f, 10.05f);
                Assert.That(clip, Is.Not.Null);
                clip.Apply(clip.Bind(copy.Root), time);
            }
            else _track.Apply(copy, time);
        }

        [TestCase(false), TestCase(true)]
        public void OfflineTeleportNeverBecomesAPhantomWalk(bool retained)
        {
            _track.Record(10f);
            _motor.Teleport(Vector3.right * 4f);
            _track.Record(10.05f);
            var copy = Copy();
            Apply(copy, 10.025f, retained);
            Assert.That(copy.Root.transform.position.x, Is.EqualTo(0f).Within(.001f));
            Apply(copy, 10.05f, retained);
            Assert.That(copy.Root.transform.position.x, Is.EqualTo(4f).Within(.001f));
            Assert.That(_motor.transform.position.x, Is.EqualTo(4f));
        }

        [TestCase(false), TestCase(true)]
        public void AcceptedNetworkEpochNeverInterpolatesThroughTheMap(bool retained)
        {
            _track.Record(10f);
            _motor.AdoptMovementEpoch(2);
            _motor.transform.position = Vector3.right * 4f;
            _track.Record(10.05f);
            var copy = Copy();
            Apply(copy, 10.025f, retained);
            Assert.That(copy.Root.transform.position.x, Is.EqualTo(0f).Within(.001f));
            Apply(copy, 10.05f, retained);
            Assert.That(copy.Root.transform.position.x, Is.EqualTo(4f).Within(.001f));
        }

        [TestCase(false), TestCase(true)]
        public void FastContinuousMotionStillInterpolates(bool retained)
        {
            _track.Record(10f);
            _motor.transform.position = Vector3.right * 4f;
            _track.Record(10.05f);
            var copy = Copy();
            Apply(copy, 10.025f, retained);
            Assert.That(copy.Root.transform.position.x, Is.EqualTo(2f).Within(.001f));
        }

        [Test]
        public void ContactCaptureCarriesAnOfflineTeleportDiscontinuity()
        {
            var before = _track.Capture(10f);
            _motor.Teleport(Vector3.right);
            var after = _track.Capture(10.05f);
            Assert.That(after.Epoch, Is.Not.EqualTo(before.Epoch));
            Assert.That(_track.Capture(10.06f).Epoch, Is.EqualTo(after.Epoch));
        }

        [Test]
        public void LiveVisibilityChangesAtTheRecordedTimeNotHalfwayBeforeIt()
        {
            _source.SetActive(false); _track.Record(10f);
            _source.SetActive(true); _track.Record(10.05f);
            var copy = Copy();
            _track.Apply(copy, 10.04f);
            Assert.That(copy.Root.activeSelf, Is.False);
            _track.Apply(copy, 10.05f);
            Assert.That(copy.Root.activeSelf, Is.True);
        }
    }
}
