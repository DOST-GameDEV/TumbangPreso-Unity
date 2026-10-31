using System;
using UnityEngine;

namespace TumbangPreso.Net
{
    // Android Wi-Fi may filter discovery packets unless the active browser holds a lease.
    // No Wi-Fi configuration changes. The lease exists only while listening and foregrounded.
    internal static class AndroidLanDiscovery
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        private static AndroidJavaObject _lease;
#endif
        public static void Acquire()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            if (_lease != null) return;
            try
            {
                using var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
                using var activity = player.GetStatic<AndroidJavaObject>("currentActivity");
                using var context = activity.Call<AndroidJavaObject>("getApplicationContext");
                using var wifi = context.Call<AndroidJavaObject>("getSystemService", "wifi");
                _lease = wifi.Call<AndroidJavaObject>("createMulticastLock", "tump-lan-discovery");
                _lease.Call("setReferenceCounted", false); _lease.Call("acquire");
            }
            catch (Exception error) { Release(); Debug.LogWarning("[Lan] Wi-Fi discovery lease unavailable: " + error.Message); }
#endif
        }
        public static void Release()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            if (_lease == null) return;
            try { if (_lease.Call<bool>("isHeld")) _lease.Call("release"); }
            catch (Exception error) { Debug.LogWarning("[Lan] Wi-Fi discovery lease release: " + error.Message); }
            finally { _lease.Dispose(); _lease = null; }
#endif
        }
    }
}
