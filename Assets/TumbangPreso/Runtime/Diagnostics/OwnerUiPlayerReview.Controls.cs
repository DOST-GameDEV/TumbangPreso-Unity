using System;
using System.Collections;
using TumbangPreso.InputLayer;
using TumbangPreso.UI;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace TumbangPreso.Diagnostics
{
    public sealed partial class OwnerUiPlayerReview
    {
        private IEnumerator ReviewGenericControllerSwitch()
        {
            const string key = "tumbangpreso.genericpad";
            bool existed = PlayerPrefs.HasKey(key); int saved = PlayerPrefs.GetInt(key, 1);
            bool before = GenericPadBridge.Enabled;
            int padCount = Gamepad.all.Count;
            Joystick joystick = null;
            var view = Object.FindFirstObjectByType<TumpSettingsView>();
            bool completed = false;
            try
            {
                Stage("native synthetic controller hotplug and settings save/discard");
                joystick = InputSystem.AddDevice<Joystick>("TumpNativeReviewUnrecognisedController");
                yield return WaitFor(() => Find("GenericControllerValue") is Toggle, 5);
                if (Gamepad.all.Count != padCount + (before ? 1 : 0))
                    throw new InvalidOperationException("Hotplug created the wrong number of fallback gamepads.");
                var control = (Toggle)Find("GenericControllerValue");
                if (control.isOn != before) throw new InvalidOperationException("Generic controller switch shows the wrong state.");
                yield return Shot("Settings-unrecognised-controller");
                yield return Click("GenericControllerValue");
                if (GenericPadBridge.Enabled == before || !view.Session.Dirty)
                    throw new InvalidOperationException("The native switch did not enter the settings transaction.");
                yield return Click("TumpSettingsBack");yield return Click("DiscardAndBack");
                if (GenericPadBridge.Enabled != before) throw new InvalidOperationException("Native discard kept a controller change.");
                yield return Click("SettingsButton");yield return Click("GenericControllerValue");
                yield return Click("TumpSaveSettings");yield return Click("TumpSettingsBack");
                yield return Click("SettingsButton");
                if (((Toggle)Find("GenericControllerValue")).isOn == before)
                    throw new InvalidOperationException("The explicitly saved controller setting did not survive reopening.");
                // Return both the open session snapshot and the persistent value
                // to their original state before removing the synthetic device.
                yield return Click("GenericControllerValue");yield return Click("TumpSaveSettings");
                if (GenericPadBridge.Enabled != before) throw new InvalidOperationException("Native controller preference restore failed.");
                Stage("native controller hotplug/save/discard passed; synthetic device, not physical-pad certification");
                completed = true;
            }
            finally
            {
                if (!completed && view != null) view.enabled = false;
                if (joystick != null && joystick.added) InputSystem.RemoveDevice(joystick);
                if (existed) PlayerPrefs.SetInt(key, saved); else PlayerPrefs.DeleteKey(key);
                PlayerPrefs.Save(); GenericPadBridge.Sync();
            }
        }
    }
}
