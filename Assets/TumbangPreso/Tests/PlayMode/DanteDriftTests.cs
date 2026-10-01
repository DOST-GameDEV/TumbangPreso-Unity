using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using TumbangPreso.Abilities;
using TumbangPreso.Core;
using UnityEngine;
using UnityEngine.TestTools;

namespace TumbangPreso.PlayTests
{
    public sealed class DanteDriftTests
    {
        readonly List<GameObject> _built = new List<GameObject>();
        CharacterMotor _caster, _near, _far, _behind;
        Vector4 _bounds;
        [UnitySetUp] public IEnumerator Before()
        {
            _bounds = new Vector4(AIController.PlayableMinX, AIController.PlayableMaxX,
                AIController.PlayableMinZ, AIController.PlayableMaxZ);
            yield return PlayModeWorld.Reset();
        }
        [UnityTearDown] public IEnumerator After()
        {
            yield return PlayModeWorld.Reset();
            AIController.PlayableMinX = _bounds.x; AIController.PlayableMaxX = _bounds.y;
            AIController.PlayableMinZ = _bounds.z; AIController.PlayableMaxZ = _bounds.w;
        }
        [TearDown] public void Cleanup()
        { foreach (var go in _built) if (go != null) Object.DestroyImmediate(go); _built.Clear(); }
        GameObject Track(GameObject go) { _built.Add(go); return go; }
        CharacterMotor Seat(int slot, Vector3 position)
        {
            var go = Track(new GameObject("Drift seat " + slot, typeof(CharacterController)));
            var motor = go.AddComponent<CharacterMotor>(); motor.PlayerSlot = slot;
            motor.Mode = GameMode.HeroStrike; motor.IsBot = false; motor.enabled = false;
            go.AddComponent<Carrier>(); go.AddComponent<CombatVerbs>();
            go.transform.position = position; GameServices.Round.Register(motor); return motor;
        }
        IEnumerator Open()
        {
            UI.SceneFlow.SetSelectedRules(CustomGameRules.Defaults(GameMode.HeroStrike));
            GameServices.Ensure(); GameServices.Round.Clear();
            AIController.PlayableMinX = -8.6f; AIController.PlayableMaxX = 8.6f;
            AIController.PlayableMinZ = -13; AIController.PlayableMaxZ = 13;
            var floor = Track(GameObject.CreatePrimitive(PrimitiveType.Cube));
            floor.transform.localScale = new Vector3(60, 1, 60); floor.transform.position = Vector3.down * .5f;
            var can = Track(new GameObject("Drift can")); can.transform.position = new Vector3(6, 0, 6);
            GameServices.Round.Lata = can.AddComponent<Lata>();
            _caster = Seat(0, new Vector3(0, 0, -6)); _near = Seat(1, new Vector3(4, 0, -3));
            _far = Seat(2, new Vector3(-4, 0, 11)); _behind = Seat(3, new Vector3(0, 0, -10));
            GameServices.Match.StartMatch(); GameServices.Round.BeginRound(); Physics.SyncTransforms();
            yield return null;
        }
        sealed class Observer : INetProvider
        {
            public bool IsHost => false;
            public bool IsNetworked => true;
            public int LocalSlot => 3;
            public int LocalPeerId => 3;
            public bool IsSeatlessReferee => false;
        }
        [UnityTest] public IEnumerator ObserverAndPauseCannotAdvanceHostOutcomes()
        {
            yield return Open(); var prior = NetAuthority.Provider;
            try
            {
                NetAuthority.Provider = new Observer();
                var wave = DanteDriftWave.Spawn(_caster.transform.position, Vector3.forward, 0);
                yield return new WaitForSeconds(.4f);
                Assert.IsFalse(_near.IsConcussed); Assert.IsFalse(_far.IsConcussed);
                PresentationClock.RequestScale(0); yield return null;
                float age = wave.Age; yield return new WaitForSecondsRealtime(.15f);
                Assert.AreEqual(age, wave.Age, .001f);
                PresentationClock.RequestScale(1);
                yield return new WaitForSeconds(GeoRules.DriftSeconds);
                Assert.IsTrue(wave == null || !wave.gameObject.activeInHierarchy);
            }
            finally { PresentationClock.RequestScale(1); NetAuthority.Provider = prior; }
        }
        [UnityTest] public IEnumerator RecoverySkipsPastBlastsAndRetiresWithRound()
        {
            yield return Open();
            var wave = DanteDriftWave.Spawn(_caster.transform.position, Vector3.forward, 0);
            wave.SendMessage("Advance", .65f); var field = wave.Capture();
            Assert.IsTrue(Net.WorldEffectSnapshot.Valid(field)); _near.CleanseStatuses();
            Assert.IsTrue(Net.WorldEffectSnapshot.Apply(new[] { field }, 0));
            Assert.IsFalse(_near.IsConcussed, "Recovery cannot replay the already-resolved near band.");
            var restored = Object.FindAnyObjectByType<DanteDriftWave>(); Assert.IsNotNull(restored);
            Assert.AreEqual(.65f, restored.Age, .001f);
            restored.SendMessage("Advance", .60f);
            Assert.IsTrue(_far.IsConcussed); Assert.IsFalse(_near.IsConcussed);
            GameServices.Round.EndRound(); Assert.IsFalse(restored.gameObject.activeInHierarchy);
        }
        [UnityTest] public IEnumerator SnapshotBoundsAndReplayRemainRenderOnly()
        {
            yield return Open();
            var wave = DanteDriftWave.Spawn(_caster.transform.position, Vector3.forward, 0);
            var field = wave.Capture(); Assert.IsTrue(Net.WorldEffectSnapshot.Valid(field));
            var bad = field; bad.Forward = Vector3.up; Assert.IsFalse(Net.WorldEffectSnapshot.Valid(bad));
            bad = field; bad.FirstScale = float.NaN; Assert.IsFalse(Net.WorldEffectSnapshot.Valid(bad));
            bad = field; bad.Owner = -1; Assert.IsFalse(Net.WorldEffectSnapshot.Valid(bad));
            _near.CleanseStatuses();
            var parent = Track(new GameObject("Drift recorded stage"));
            int live = Object.FindObjectsByType<DanteDriftWave>(FindObjectsSortMode.None).Length;
            using (var view = new CameraSystem.RecordedFieldView(parent.transform, field))
            {
                view.Sample(field, .8f); yield return null;
                Assert.IsEmpty(view.Root.GetComponentsInChildren<Collider>(true));
                Assert.AreEqual(live, Object.FindObjectsByType<DanteDriftWave>(FindObjectsSortMode.None).Length);
                Assert.IsFalse(_near.IsConcussed); Assert.IsFalse(_far.IsConcussed);
            }
        }
        [UnityTest, Timeout(120000)] public IEnumerator AuthoredCourtCascadeKeepsItsDirection()
        {
            yield return MapRetrievalProbe.Load("Eskinita", GameMode.HeroStrike);
            Object.FindFirstObjectByType<ReadyGate>().StartLocalCountdown();
            yield return new WaitForSeconds(3.6f);
            foreach (var brain in Object.FindObjectsByType<AIController>(FindObjectsSortMode.None)) brain.enabled = false;
            foreach (var input in Object.FindObjectsByType<PlayerInputReader>(FindObjectsSortMode.None)) input.enabled = false;
            foreach (var actor in GameServices.Round.Players)
            { actor.Intent.Clear(); actor.Teleport(new Vector3(5, .12f, -6)); actor.enabled = false; }
            var caster = GameServices.Round.PlayerAt(1); caster.Teleport(new Vector3(0, .12f, -6));
            caster.transform.rotation = Quaternion.identity; caster.AbilitySystem.BindHero("dante");
            var art = RosterBook.Load().FindPersonArt("dante");
            caster.GetComponent<Visual.CharacterVisual>().ApplyModel(art.Model, art.Tint, art.Clips, art.Palette, art.PetModel);
            var victim = GameServices.Round.PlayerAt(0); victim.Teleport(new Vector3(1, .12f, 5));
            var eye = Track(new GameObject("Continental Drift witness")).AddComponent<Camera>(); eye.enabled = false;
            eye.transform.position = new Vector3(5, 4, -7); eye.transform.LookAt(new Vector3(0, .4f, 2)); eye.fieldOfView = 65;
            var context = new AbilityContext(caster, caster.GetComponent<Carrier>(), caster.GetComponent<CombatVerbs>());
            var ultimate = caster.AbilitySystem.Kit.Ultimate;
            ultimate.Activate(context); ultimate.Tick(context, ultimate.Windup + .01f);
            var wave = Object.FindAnyObjectByType<DanteDriftWave>(); Assert.IsNotNull(wave); wave.enabled = false;
            for (int frame = 0; frame < 24; frame++)
            {
                if (frame > 0) wave.SendMessage("Advance", .1f);
                yield return GameplayShots.Render(eye, frame.ToString("D3"), false,
                    "Logs/dante-drift-film-v2", caster, 960, 540);
            }
            Assert.AreEqual(GeoRules.DriftBlasts, wave.ReleasedBands);
            Assert.IsTrue(victim.IsConcussed);
            Assert.Greater(wave.Reach, 12); Assert.Greater(wave.HalfWidth, 7);
        }
        [Test] public void MetadataMatchesContinentalDrift()
        {
            var kit = new DanteHeroKit(); Assert.AreEqual("CONTINENTAL DRIFT", kit.Ultimate.Name);
            Assert.AreEqual(12, kit.UltimateCost); Assert.AreEqual("dante_ultimate", kit.Ultimate.Id);
        }
        [UnityTest] public IEnumerator ReleasedEarthquakeReachesNearThenFarAndNeverBehind()
        {
            yield return Open();
            var kit = new DanteHeroKit();
            var context = new AbilityContext(_caster, _caster.GetComponent<Carrier>(), _caster.GetComponent<CombatVerbs>());
            kit.Ultimate.Activate(context); kit.Ultimate.Tick(context, kit.Ultimate.Windup + .01f);
            yield return null;
            Assert.IsTrue(_near.IsConcussed, "The first forward band must reach the nearby player across its width.");
            Assert.IsFalse(_far.IsConcussed, "The far end must wait for the travelling cascade.");
            Assert.IsFalse(_behind.IsConcussed, "A player behind the accepted origin is outside the forward blast.");
            yield return new WaitForSeconds(1.3f);
            Assert.IsTrue(_far.IsConcussed); Assert.IsFalse(_behind.IsConcussed); Assert.IsFalse(_caster.IsConcussed);
        }
    }
}
