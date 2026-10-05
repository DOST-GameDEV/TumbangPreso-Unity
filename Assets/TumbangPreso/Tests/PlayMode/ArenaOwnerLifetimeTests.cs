using System.Collections;
using NUnit.Framework;
using TumbangPreso.Abilities;
using TumbangPreso.Map;
using UnityEngine;
using UnityEngine.TestTools;

namespace TumbangPreso.PlayTests
{
    public sealed class ArenaOwnerLifetimeTests
    {
        private GameObject _old, _current;
        private bool _edgeSense;
        private float _floor;
        private CharacterMotor.FallRule _fall;
        private PaeteVine.CatchRule _catch;

        [UnitySetUp] public IEnumerator Before()
        {
            yield return PlayModeWorld.Reset();
            _edgeSense = AIController.EdgeSense;
            _floor = Net.MatchRpc.MoveFloorY;
            _fall = CharacterMotor.MapFall; _catch = PaeteVine.MapCatch;
        }

        [UnityTearDown] public IEnumerator After()
        {
            if (_old != null) Object.Destroy(_old);
            if (_current != null) Object.Destroy(_current);
            yield return null;
            yield return PlayModeWorld.Reset();
            AIController.EdgeSense = _edgeSense;
            Net.MatchRpc.MoveFloorY = _floor;
            CharacterMotor.MapFall = _fall; PaeteVine.MapCatch = _catch;
        }

        private ArenaStage Stage(ref GameObject root, string name, float floor)
        {
            root = new GameObject(name); root.SetActive(false);
            var stage = root.AddComponent<ArenaStage>(); stage.MoveFloor = floor;
            root.SetActive(true); return stage;
        }

        [UnityTest] public IEnumerator DisablingRetiredStagePreservesCurrentOwnerGlobals()
        {
            Stage(ref _old, "Retired Arena owner", -40);
            var current = Stage(ref _current, "Current Arena owner", -44);
            Assert.AreSame(current, ArenaStage.Instance); Assert.IsTrue(AIController.EdgeSense);
            Assert.AreEqual(-44, Net.MatchRpc.MoveFloorY);
            _old.SetActive(false); yield return null;
            Assert.AreSame(current, ArenaStage.Instance);
            Assert.IsTrue(AIController.EdgeSense, "Retired stage disabled the current owner's edge sensing.");
            Assert.AreEqual(-44, Net.MatchRpc.MoveFloorY, "Retired stage reset the current owner's movement floor.");
        }

        [UnityTest] public IEnumerator DestroyingRetiredStagePreservesCurrentOwnerGlobals()
        {
            Stage(ref _old, "Retired Arena destroy owner", -40);
            var current = Stage(ref _current, "Current Arena destroy owner", -44);
            Object.Destroy(_old); yield return null;
            Assert.AreSame(current, ArenaStage.Instance);
            Assert.IsTrue(AIController.EdgeSense);
            Assert.AreEqual(-44, Net.MatchRpc.MoveFloorY);
        }

        [UnityTest] public IEnumerator DisablingCurrentStageRestoresOrdinaryMapGlobals()
        {
            Stage(ref _current, "Sole Arena owner", -44);
            _current.SetActive(false); yield return null;
            Assert.IsNull(ArenaStage.Instance); Assert.IsFalse(AIController.EdgeSense);
            Assert.AreEqual(Net.MatchRpc.DefaultMoveFloorY, Net.MatchRpc.MoveFloorY);
        }

        [UnityTest] public IEnumerator DisablingRetiredRecoveryPreservesCurrentOwnerHooks()
        {
            _old = new GameObject("Retired Arena recovery");
            _old.AddComponent<ArenaFallRecovery>();
            _current = new GameObject("Current Arena recovery");
            var current = _current.AddComponent<ArenaFallRecovery>();
            var fall = CharacterMotor.MapFall; var catchRule = PaeteVine.MapCatch;
            Assert.IsNotNull(fall); Assert.IsNotNull(catchRule);
            _old.SetActive(false); yield return null;
            Assert.AreSame(current, ArenaFallRecovery.Instance);
            Assert.AreEqual(fall, CharacterMotor.MapFall, "Retired recovery removed the new owner's fall rule.");
            Assert.AreEqual(catchRule, PaeteVine.MapCatch, "Retired recovery removed the new owner's vine rule.");
        }

        [UnityTest] public IEnumerator DisablingCurrentRecoveryRetiresItsHooks()
        {
            _current = new GameObject("Sole Arena recovery");
            _current.AddComponent<ArenaFallRecovery>();
            Assert.IsNotNull(CharacterMotor.MapFall); Assert.IsNotNull(PaeteVine.MapCatch);
            _current.SetActive(false); yield return null;
            Assert.IsNull(ArenaFallRecovery.Instance);
            Assert.IsNull(CharacterMotor.MapFall); Assert.IsNull(PaeteVine.MapCatch);
        }
    }
}
