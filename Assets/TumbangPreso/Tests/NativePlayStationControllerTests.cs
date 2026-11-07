using System.Runtime.InteropServices;
using NUnit.Framework;
using TumbangPreso.InputLayer;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.DualShock;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.Utilities;

namespace TumbangPreso.Tests
{
    [ExecuteAlways]
    public sealed class NativeControllerTestEventSystem : EventSystem { }

    public sealed class NativePlayStationControllerTests : InputTestFixture
    {
        private InputSettings.UpdateMode _updateMode;
        private Gamepad _previousPad;
        private InputDevice _device;
        private InputActionAsset _actions;
        private GameObject _events;
        private EventSystem _previousEvents;

        [SetUp]
        public void SetUp()
        {
            _updateMode = InputSystem.settings.updateMode;
            _previousPad = Gamepad.current;
            _previousEvents = EventSystem.current;
            _events = new GameObject("Native PlayStation input test");
            var system = _events.AddComponent<NativeControllerTestEventSystem>();
            EventSystem.current = system;
            InputSystem.settings.updateMode = InputSettings.UpdateMode.ProcessEventsManually;
        }

        [TearDown]
        public void Cleanup()
        {
            if (_events != null)
            {
                _events.GetComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>()?.actionsAsset?.Disable();
                Object.DestroyImmediate(_events);
            }
            if (_previousEvents != null) EventSystem.current = _previousEvents;
            if (_actions != null) { _actions.Disable(); Object.DestroyImmediate(_actions); }
            if (_device != null && _device.added) InputSystem.RemoveDevice(_device);
            if (_previousPad != null && _previousPad.added) _previousPad.MakeCurrent();
            InputSystem.settings.updateMode = _updateMode;
        }

        private Gamepad Connect(int productId)
        {
            _device = InputSystem.AddDevice(new InputDeviceDescription
            {
                interfaceName = "HID",
                manufacturer = "Sony Interactive Entertainment",
                product = "Wireless Controller",
                capabilities = "{\"vendorId\":1356,\"productId\":" + productId
                    + ",\"usagePage\":1,\"usage\":5,\"inputReportSize\":78,\"outputReportSize\":48}"
            });
            Assert.That(_device, Is.InstanceOf<DualSenseGamepadHID>(),
                "Official Sony VID/PID must select the native DualSense decoder, not a guessed mapping.");
            return (Gamepad)_device;
        }

        [TestCase(0x0ce6)]
        [TestCase(0x0df2)]
        public void OfficialDualSenseAndEdgeUseTheirNativeLayout(int productId)
        {
            GenericPadBridge.Sync();
            int count = Gamepad.all.Count;
            var pad = Connect(productId);
            GenericPadBridge.Sync();
            Assert.That(Gamepad.all.Count, Is.EqualTo(count + 1), "A native pad must not get a virtual twin.");
            Assert.That(pad.buttonSouth.displayName, Is.EqualTo("Cross"));
            Assert.That(pad.buttonEast.displayName, Is.EqualTo("Circle"));
        }

        [TestCase(0x0ce6, 0)]
        [TestCase(0x0ce6, 1)]
        [TestCase(0x0ce6, 2)]
        [TestCase(0x0ce6, 3)]
        [TestCase(0x0df2, 0)]
        [TestCase(0x0df2, 1)]
        [TestCase(0x0df2, 2)]
        [TestCase(0x0df2, 3)]
        public void NativeReportsDriveGameBindingsAndMenuWithoutAnExternalMapper(int productId, int transport)
        {
            var pad = Connect(productId);
            _actions = Object.Instantiate(Resources.Load<InputActionAsset>("TumbangPreso"));
            Assert.That(_actions, Is.Not.Null);
            var map = _actions.FindActionMap("Player", true);
            map.Enable();
            _events = UiInputModule.Ensure().gameObject;
            _events.GetComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>().actionsAsset.Enable();

            Report(pad, transport, 0);
            Report(pad, transport, 1);
            Assert.That(map.FindAction("Move", true).ReadValue<Vector2>().x, Is.GreaterThan(.9f));
            Assert.That(map.FindAction("Look", true).ReadValue<Vector2>().x, Is.GreaterThan(.9f));
            Assert.That(map.FindAction("Jump", true).IsPressed(), Is.True);
            Assert.That(map.FindAction("SpecialAbility", true).IsPressed(), Is.True);
            Assert.That(MenuNav.PadSubmitPressed, Is.True, "Cross must submit in the actual menu reader.");
            Assert.That(MenuNav.PadAnyPressed, Is.True);

            Report(pad, transport, 0);
            Assert.That(map.FindAction("Jump", true).IsPressed(), Is.False);
            Assert.That(map.FindAction("SpecialAbility", true).IsPressed(), Is.False);
            Report(pad, transport, 2);
            Assert.That(MenuNav.PadCancelPressed, Is.True, "Circle must cancel in the actual menu reader.");

            InputSystem.RemoveDevice(pad);
            _device = null;
            pad = Connect(productId);
            Report(pad, transport, 0);
            Report(pad, transport, 1);
            Assert.That(map.FindAction("Jump", true).IsPressed(), Is.True, "Reconnecting must restore bindings.");
            Assert.That(MenuNav.PadSubmitPressed, Is.True);
        }

        [Test]
        public void DisabledMenuActionsStillAllowNativeControllerConfirmAndBack()
        {
            var pad = Connect(0x0ce6);
            var module = UiInputModule.Ensure().GetComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
            module.actionsAsset.Disable();
            Report(pad, 0, 0);
            Report(pad, 0, 1);
            Assert.That(MenuNav.PadSubmitPressed, Is.True);
            Assert.That(MenuNav.SubmitHeld, Is.True);
            Report(pad, 0, 0);
            Report(pad, 0, 2);
            Assert.That(MenuNav.PadCancelPressed, Is.True);
        }

        [TestCase("Gamepad")]
        [TestCase("XInputControllerWindows")]
        public void ExistingGamepadsKeepTheirMenuFallback(string layout)
        {
            _device = InputSystem.AddDevice(layout);
            var pad = (Gamepad)_device;
            var module = UiInputModule.Ensure().GetComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
            module.actionsAsset.Disable();
            Press(pad.buttonSouth);
            Assert.That(MenuNav.PadSubmitPressed, Is.True);
            Release(pad.buttonSouth);
            Press(pad.buttonEast);
            Assert.That(MenuNav.PadCancelPressed, Is.True);
        }

        [Test]
        public void KeyboardStillDrivesTheSameGameplayAndMenuActions()
        {
            _device = InputSystem.AddDevice<Keyboard>();
            var keyboard = (Keyboard)_device;
            _actions = Object.Instantiate(Resources.Load<InputActionAsset>("TumbangPreso"));
            var map = _actions.FindActionMap("Player", true);
            map.Enable();
            var module = UiInputModule.Ensure().GetComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
            module.actionsAsset.Enable();
            Press(keyboard.wKey);
            Assert.That(map.FindAction("Move", true).ReadValue<Vector2>().y, Is.GreaterThan(.9f));
            Press(keyboard.spaceKey);
            Assert.That(map.FindAction("Jump", true).IsPressed(), Is.True);
            Press(keyboard.enterKey);
            Assert.That(MenuNav.PadSubmitPressed, Is.True);
            Assert.That(MenuNav.SubmitHeld, Is.True);
        }

        private static void Report(Gamepad pad, int transport, int press)
        {
            byte x = press == 1 ? (byte)255 : (byte)128;
            byte buttons = (byte)(8 | (press == 1 ? 0x20 : press == 2 ? 0x40 : 0));
            byte trigger = press == 1 ? (byte)255 : (byte)0;
            switch (transport)
            {
                case 0:
                    InputSystem.QueueStateEvent(pad, new UsbReport
                    { id=1, lx=x, ly=128, rx=x, ry=128, rt=trigger, b0=buttons }); break;
                case 1:
                    InputSystem.QueueStateEvent(pad, new BluetoothReport
                    { id=0x31, lx=x, ly=128, rx=x, ry=128, rt=trigger, b0=buttons }); break;
                case 2:
                    InputSystem.QueueStateEvent(pad, new SimpleReport
                    { id=1, lx=x, ly=128, rx=x, ry=128, rt=trigger, b0=buttons }); break;
                case 3:
                    InputSystem.QueueStateEvent(pad, new SimpleBluetoothReport
                    { id=1, lx=x, ly=128, rx=x, ry=128, rt=trigger, b0=buttons }); break;
            }
            InputSystem.Update();
        }

        [StructLayout(LayoutKind.Explicit, Size=64)]
        private struct UsbReport : IInputStateTypeInfo
        {
            public FourCC format => new FourCC('H','I','D');
            [FieldOffset(0)] public byte id;
            [FieldOffset(1)] public byte lx;
            [FieldOffset(2)] public byte ly;
            [FieldOffset(3)] public byte rx;
            [FieldOffset(4)] public byte ry;
            [FieldOffset(6)] public byte rt;
            [FieldOffset(8)] public byte b0;
        }

        [StructLayout(LayoutKind.Explicit, Size=78)]
        private struct BluetoothReport : IInputStateTypeInfo
        {
            public FourCC format => new FourCC('H','I','D');
            [FieldOffset(0)] public byte id;
            [FieldOffset(2)] public byte lx;
            [FieldOffset(3)] public byte ly;
            [FieldOffset(4)] public byte rx;
            [FieldOffset(5)] public byte ry;
            [FieldOffset(7)] public byte rt;
            [FieldOffset(9)] public byte b0;
        }

        [StructLayout(LayoutKind.Explicit, Size=10)]
        private struct SimpleReport : IInputStateTypeInfo
        {
            public FourCC format => new FourCC('H','I','D');
            [FieldOffset(0)] public byte id;
            [FieldOffset(1)] public byte lx;
            [FieldOffset(2)] public byte ly;
            [FieldOffset(3)] public byte rx;
            [FieldOffset(4)] public byte ry;
            [FieldOffset(5)] public byte b0;
            [FieldOffset(9)] public byte rt;
        }

        [StructLayout(LayoutKind.Explicit, Size=78)]
        private struct SimpleBluetoothReport : IInputStateTypeInfo
        {
            public FourCC format => new FourCC('H','I','D');
            [FieldOffset(0)] public byte id;
            [FieldOffset(1)] public byte lx;
            [FieldOffset(2)] public byte ly;
            [FieldOffset(3)] public byte rx;
            [FieldOffset(4)] public byte ry;
            [FieldOffset(5)] public byte b0;
            [FieldOffset(9)] public byte rt;
        }
    }
}
