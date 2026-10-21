using System.Collections;
using System.Reflection;
using NUnit.Framework;
using TumbangPreso.Core;
using TumbangPreso.InputLayer;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.TestTools;

namespace TumbangPreso.PlayTests
{
    public sealed class ReaderDeviceLossLifetimeTests
    {
        private const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;
        private readonly ReaderSeatPresentationLifetimeTests _world = new();
        private Gamepad _pad, _unused;
        private Mouse _mouse;
        private CharacterMotor _motor;
        private Carrier _carrier;
        private CombatVerbs _combat;
        private PlayerInputReader _reader;
        private Slipper _shoe;
        private bool _touch;
        private InputSettings.BackgroundBehavior _background;
        private InputSettings.EditorInputBehaviorInPlayMode _editor;
        [UnitySetUp] public IEnumerator Before()
        {
            _background = InputSystem.settings.backgroundBehavior;
            _editor = InputSystem.settings.editorInputBehaviorInPlayMode;
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            _touch = TouchInput.Active; TouchInput.ReleaseAll(); TouchInput.Active = false;
            _pad = InputSystem.AddDevice<Gamepad>(); InputSystem.EnableDevice(_pad);
            yield return _world.Before();
            _motor = GameObject.Find("Reader custody original seat").GetComponent<CharacterMotor>();
            _reader = _motor.GetComponent<PlayerInputReader>();
            _carrier = _motor.GetComponent<Carrier>(); _combat = _motor.GetComponent<CombatVerbs>();
            _shoe = _carrier.Held; Assert.IsNotNull(_shoe); Assert.IsTrue(_reader.enabled);
        }
        [UnityTearDown] public IEnumerator After()
        {
            TouchInput.ReleaseAll(); TouchInput.Active = _touch;
            if (_pad != null && _pad.added) InputSystem.RemoveDevice(_pad);
            if (_unused != null && _unused.added) InputSystem.RemoveDevice(_unused);
            if (_mouse != null && _mouse.added) InputSystem.RemoveDevice(_mouse);
            yield return _world.After();
            InputSystem.settings.backgroundBehavior = _background;
            InputSystem.settings.editorInputBehaviorInPlayMode = _editor;
        }
        private static void Step(Component component) => component.GetType().GetMethod("Update", Hidden).Invoke(component, null);
        private void Pad(float right, float left = 0)
        {
            InputSystem.QueueStateEvent(_pad, new GamepadState { rightTrigger = right, leftTrigger = left });
            InputSystem.Update(); Step(_reader);
        }
        private void ThrowCharge()
        {
            Pad(1); Assert.IsTrue(_motor.Intent.Pressed(Verb.SpecialAbility));
            Step(_carrier); Step(_carrier);
            Assert.IsTrue(_carrier.IsCharging); Assert.Greater(_carrier.ChargeRatio, 0);
        }
        private void RemovePad()
        {
            InputSystem.RemoveDevice(_pad); InputSystem.Update(); Step(_reader);
        }
        [Test] public void RemovingTheThrowDeviceCancelsWithoutLaunchingItsShoe()
        {
            ThrowCharge(); RemovePad(); Step(_carrier);
            Assert.AreSame(_shoe, _carrier.Held, "Device loss was interpreted as a deliberate throw release.");
            Assert.IsFalse(_carrier.IsCharging); Assert.Zero(_carrier.ChargeRatio);
        }
        [Test] public void RemovingTheLungeDeviceCancelsWithoutSpendingRecovery()
        {
            Assert.IsTrue(_shoe.HostDisarm());
            GameServices.Match.ApplySnapshot(new int[Balance.PlayerCount], 2, true);
            GameServices.Round.ApplySnapshot(80, true, 1, true);
            _motor.transform.position = Vector3.zero; Assert.IsTrue(_motor.IsDefender); Assert.IsTrue(_motor.CanAct());
            Pad(0, 1); Assert.IsTrue(_motor.Intent.Pressed(Verb.Lunge)); Step(_combat); Step(_combat);
            Assert.Greater(_combat.LungeChargeRatio, 0); Assert.Zero(_combat.LungeCooldownLeft);
            RemovePad(); Step(_combat);
            Assert.Zero(_combat.LungeCooldownLeft, "Device loss committed the pending lunge and spent recovery.");
            Assert.Zero(_combat.LungeChargeRatio);
        }
        [Test] public void AStillHeldTouchThrowSurvivesRemovalOfThePad()
        {
            ThrowCharge(); TouchInput.Active = true; TouchInput.Set(Verb.SpecialAbility, true);
            RemovePad(); Step(_carrier);
            Assert.IsTrue(_motor.Intent.Pressed(Verb.SpecialAbility)); Assert.IsTrue(_carrier.IsCharging);
            Assert.AreSame(_shoe, _carrier.Held);
        }
        [Test] public void RemovingAnUnusedPadPreservesTheDrivingThrow()
        {
            ThrowCharge(); _unused = InputSystem.AddDevice<Gamepad>();
            InputSystem.RemoveDevice(_unused); InputSystem.Update(); Step(_reader); Step(_carrier);
            Assert.IsTrue(_motor.Intent.Pressed(Verb.SpecialAbility)); Assert.IsTrue(_carrier.IsCharging);
            Assert.AreSame(_shoe, _carrier.Held);
        }
        [Test] public void AnOrdinaryTriggerReleaseStillThrows()
        {
            ThrowCharge(); Pad(0); Step(_carrier);
            Assert.IsFalse(_carrier.IsCharging); Assert.IsNull(_carrier.Held);
            Assert.AreEqual(SlipperState.InFlight, _shoe.State);
        }
        [Test] public void AStillHeldMouseThrowSurvivesRemovalOfTheDrivingPad()
        {
            _mouse = InputSystem.AddDevice<Mouse>(); ThrowCharge();
            InputSystem.QueueStateEvent(_mouse, new MouseState { buttons = 1 }); InputSystem.Update();
            Step(_reader); Step(_carrier); RemovePad(); Step(_carrier);
            Assert.IsTrue(_motor.Intent.Pressed(Verb.SpecialAbility)); Assert.IsTrue(_carrier.IsCharging);
            Assert.AreSame(_shoe, _carrier.Held);
            InputSystem.QueueStateEvent(_mouse, new MouseState()); InputSystem.Update(); Step(_reader); Step(_carrier);
            Assert.IsNull(_carrier.Held); Assert.AreEqual(SlipperState.InFlight, _shoe.State);
        }
        [Test] public void AChargedTriggerKeepsItsReleaseThresholdThroughUnusedPadChanges()
        {
            ThrowCharge(); Pad(.45f); Step(_carrier);
            Assert.IsTrue(_motor.Intent.Pressed(Verb.SpecialAbility)); Assert.IsTrue(_carrier.IsCharging);
            _unused = InputSystem.AddDevice<Gamepad>(); InputSystem.RemoveDevice(_unused);
            InputSystem.Update(); Step(_reader); Step(_carrier);
            Assert.IsTrue(_motor.Intent.Pressed(Verb.SpecialAbility), "A held trigger above its release threshold lost its hold.");
            Assert.IsTrue(_carrier.IsCharging); Assert.AreSame(_shoe, _carrier.Held);
            Pad(.3f); Step(_carrier);
            Assert.IsNull(_carrier.Held); Assert.AreEqual(SlipperState.InFlight, _shoe.State);
        }
        [Test] public void LostThrowInputKeepsUnrelatedTouchMovementAndHeroKeys()
        {
            ThrowCharge(); TouchInput.Active = true; TouchInput.Set(Verb.Skill1, true); TouchInput.Move = Vector2.right;
            RemovePad(); Step(_carrier);
            Assert.AreSame(_shoe, _carrier.Held); Assert.IsFalse(_carrier.IsCharging);
            Assert.IsTrue(TouchInput.Pressed(Verb.Skill1)); Assert.IsTrue(_motor.Intent.Pressed(Verb.Skill1));
            Assert.AreEqual(Vector2.right, TouchInput.Move); Assert.AreEqual(Vector2.right, _motor.Intent.MoveAxis);
        }
        [Test] public void DeviceLossKeepsAnAlreadyCommittedLungeWindowAndRecovery()
        {
            Assert.IsTrue(_shoe.HostDisarm());
            GameServices.Match.ApplySnapshot(new int[Balance.PlayerCount], 2, true);
            GameServices.Round.ApplySnapshot(80, true, 1, true); _motor.transform.position = Vector3.zero;
            Pad(0, 1); Assert.IsTrue(_motor.Intent.Pressed(Verb.Lunge));
            Assert.IsTrue(_combat.HostResolveLunge(_motor.transform.position, Vector3.forward, 1));
            float cooldown = _combat.LungeCooldownLeft;
            float contact = (float)typeof(CombatVerbs).GetField("_lungeActiveLeft", Hidden).GetValue(_combat);
            Assert.Greater(cooldown, 0); Assert.Greater(contact, 0);
            RemovePad();
            Assert.AreEqual(cooldown, _combat.LungeCooldownLeft);
            Assert.AreEqual(contact, (float)typeof(CombatVerbs).GetField("_lungeActiveLeft", Hidden).GetValue(_combat));
        }
    }
}
