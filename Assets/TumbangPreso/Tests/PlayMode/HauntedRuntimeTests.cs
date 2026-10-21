using System.Collections;
using System.Reflection;
using NUnit.Framework;
using TumbangPreso.Abilities;
using TumbangPreso.Core;
using TumbangPreso.UI;
using UnityEngine;
using UnityEngine.TestTools;

namespace TumbangPreso.PlayTests
{
    public sealed class HauntedRuntimeTests
    {
        private sealed class Peer : INetProvider
        {
            public bool Host = true;
            public bool IsHost => Host;
            public bool IsNetworked => !Host;
            public int LocalSlot => Host ? 0 : 1;
            public int LocalPeerId => LocalSlot;
            public bool IsSeatlessReferee => false;
        }
        private INetProvider _provider;
        private CharacterMotor _motor;
        private Peer _peer;
        private CustomRules _rules;
        private bool _pinned, _bots, _spectator;
        private int _seat;
        [UnitySetUp] public IEnumerator Before()
        {
            _provider = NetAuthority.Provider; _rules = SceneFlow.SelectedRules.Clone();
            _pinned = SceneFlow.RulesPinned; _bots = GameLaunch.AllBots;
            _spectator = GameLaunch.Spectator; _seat = GameLaunch.SoloSeat;
            yield return PlayModeWorld.Reset();
            _peer = new Peer(); NetAuthority.Provider = _peer;
            _motor = new GameObject("Haunted body").AddComponent<CharacterMotor>();
            _motor.enabled = false; _motor.PlayerSlot = 0; _motor.Mode = GameMode.HeroStrike;
        }
        [UnityTearDown] public IEnumerator After()
        {
            yield return PlayModeWorld.Reset(); NetAuthority.Provider = _provider;
            GameLaunch.AllBots = _bots; GameLaunch.Spectator = _spectator; GameLaunch.SoloSeat = _seat;
            SceneFlow.AdoptRemoteRules(_rules);
            if (_pinned) SceneFlow.PinSelectedRules(_rules); else SceneFlow.UnpinSelectedRules();
        }
        private void Tick(float dt) => typeof(CharacterMotor).GetMethod("StepStatuses",
            BindingFlags.Instance | BindingFlags.NonPublic).Invoke(_motor, new object[] { dt });
        [Test] public void HauntedRefreshesWithoutStackingAndExpiresWithoutBlockingActions()
        {
            int edges = 0; _motor.StatusGained += (who, kind) => { if (kind == StatusKind.Haunted) edges++; };
            _motor.ApplyHaunted(); Assert.AreEqual(7.5f, _motor.HauntedLeft);
            Assert.IsTrue(_motor.CanAct()); Assert.AreEqual(1f, _motor.StatusSpeedScale);
            Tick(2); _motor.ApplyHaunted(); Assert.AreEqual(7.5f, _motor.HauntedLeft);
            Assert.AreEqual(1, edges); Tick(7.51f); Assert.IsFalse(_motor.IsHaunted);
        }
        [Test] public void HauntedIgnoresStatusImmunityButAnExplicitCleanseStillEndsIt()
        {
            var system = _motor.gameObject.AddComponent<HeroAbilitySystem>(); system.BindHero("dante");
            using (NetCue.SuppressRelay()) system.Kit.Skill1.Activate(new AbilityContext(_motor, null, null));
            Assert.IsTrue(system.IsImmuneToStatuses);
            _motor.ApplyHaunted(); Assert.IsTrue(_motor.IsHaunted);
            _motor.CleanseStatuses(); Assert.IsFalse(_motor.IsHaunted);
            _motor.ApplyHaunted(); _motor.ClearStatuses(); Assert.IsFalse(_motor.IsHaunted);
        }
        [Test] public void AnObserverCannotCreateHauntedButCanAdoptAuthoritativeRemainingTime()
        {
            _peer.Host = false;
            _motor.ApplyHaunted(); Assert.IsFalse(_motor.IsHaunted);
            _motor.ApplyNetworkStatuses(0, 0, 0, 3);
            Assert.AreEqual(3f, _motor.HauntedLeft); Assert.IsTrue(_motor.IsHaunted);
            _motor.ApplyNetworkStatuses(0, 0, 0, 0); Assert.IsFalse(_motor.IsHaunted);
        }
        [Test] public void InvalidHauntDurationsCannotPoisonTheTimer()
        {
            _motor.ApplyHaunted(float.NaN); _motor.ApplyHaunted(float.PositiveInfinity); _motor.ApplyHaunted(-1);
            Assert.IsFalse(_motor.IsHaunted);
            _motor.ApplyHaunted(100); Assert.AreEqual(7.5f, _motor.HauntedLeft);
            _motor.ApplyNetworkStatuses(0, 0, 0, float.NaN); Assert.IsFalse(_motor.IsHaunted);
        }
        [UnityTest] public IEnumerator ActualVictimCameraRendersPurpleAndClearsWithStatus()
        {
            GameLaunch.Spectator = false;
            GameServices.Ensure(); GameServices.Round.Register(_motor);
            GameServices.Match.StartMatch(); GameServices.Round.ApplySnapshot(90, true, 0, true);
            var go = new GameObject("Haunt victim camera", typeof(Camera)); go.tag = "MainCamera";
            var rig = go.AddComponent<TumbangPreso.CameraSystem.CameraRig>();
            rig.Follow(_motor); rig.enabled = false;
            foreach (Transform child in go.transform) child.gameObject.SetActive(false);
            var camera = go.GetComponent<Camera>();
            var grade = go.GetComponent<TumbangPreso.Visual.ColourGrade>();
            foreach (var behaviour in go.GetComponents<Behaviour>())
                if (behaviour != camera && behaviour != grade) behaviour.enabled = false;
            camera.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            camera.fieldOfView = 85; camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.gray;
            var wall = GameObject.CreatePrimitive(PrimitiveType.Quad);
            wall.transform.position = new Vector3(0,0,5); wall.transform.localScale = new Vector3(30,20,1);
            var material = new Material(Shader.Find("Standard"));
            material.color = Color.white; material.EnableKeyword("_EMISSION"); material.SetColor("_EmissionColor",Color.white*.7f);
            wall.GetComponent<Renderer>().sharedMaterial = material;
            var target = new RenderTexture(640,360,24); camera.targetTexture = target;
            Texture2D capture = null;
            try
            {
                yield return null;
                _motor.ApplyHaunted(); Assert.IsTrue(grade.HauntedSight);
                camera.Render();
                var prior = RenderTexture.active;
                try
                {
                    RenderTexture.active = target; capture = new Texture2D(640,360,TextureFormat.RGBA32,false);
                    capture.ReadPixels(new Rect(0,0,640,360),0,0); capture.Apply();
                }
                finally { RenderTexture.active = prior; }
                string folder = System.Environment.GetEnvironmentVariable("TUMP_HAUNT_CAPTURE");
                if (!string.IsNullOrEmpty(folder)) System.IO.File.WriteAllBytes(System.IO.Path.Combine(folder,"victim-camera.png"),capture.EncodeToPNG());
                var centre = capture.GetPixel(320,180); var edge = capture.GetPixel(636,180);
                Assert.Greater(centre.grayscale,edge.grayscale+.08f);
                Assert.Greater(edge.b,edge.g+.025f);
                GameLaunch.Spectator = true; Assert.IsFalse(grade.HauntedSight);
                GameLaunch.Spectator = false; _motor.PlayerSlot=1; Assert.IsFalse(grade.HauntedSight);
                _motor.PlayerSlot=0; Assert.IsTrue(grade.HauntedSight);
                Tick(8); Assert.IsFalse(grade.HauntedSight); camera.Render();
            }
            finally
            {
                camera.targetTexture=null; target.Release(); Object.Destroy(target);
                if(capture!=null)Object.Destroy(capture); Object.Destroy(material);
                Object.Destroy(wall); Object.Destroy(go);
            }
        }

        [UnityTest] public IEnumerator HauntedHidesBothRealHudMarkersAndExpiryRestoresThem()
        {
            yield return MapRetrievalProbe.Load(SceneFlow.Eskinita, GameMode.HeroStrike);
            GameServices.Round.BeginRound();
            var who = GameServices.Round.PlayerAt(1); who.enabled = false;
            who.IsDefender = false;
            var shoe = who.GetComponent<Carrier>().Held; Assert.IsNotNull(shoe);
            using (NetCue.SuppressRelay()) shoe.HostDisarm();
            shoe.enabled = false; shoe.transform.position = new Vector3(5, .15f, 5);
            who.Teleport(new Vector3(-5, .12f, -5));
            var can = GameServices.Round.Lata;
            var recall = new GameObject("Haunted recall").AddComponent<SlipperRecall>();
            recall.Build(recall.transform);
            var arrows = new GameObject("Haunted can marker").AddComponent<OffscreenIndicators>();
            Canvas.ForceUpdateCanvases();
            recall.Track(who, shoe); arrows.UpdateArrows(who, can.transform);
            Assert.IsTrue(recall.Drawing); Assert.IsTrue(arrows.CanMarkerVisible);
            who.ApplyHaunted(); recall.Track(who, shoe); arrows.UpdateArrows(who, can.transform);
            Assert.IsFalse(recall.Drawing); Assert.IsFalse(arrows.CanMarkerVisible);
            who.ApplyNetworkStatuses(0, 0, 0, 0);
            recall.Track(who, shoe); arrows.UpdateArrows(who, can.transform);
            Assert.IsTrue(recall.Drawing); Assert.IsTrue(arrows.CanMarkerVisible);
        }
    }
}
