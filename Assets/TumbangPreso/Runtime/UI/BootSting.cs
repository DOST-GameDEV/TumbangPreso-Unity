using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Video;

namespace TumbangPreso.UI
{
    /// <summary>
    /// The boot sting starts before Unity's own logo, and the game warms up behind both.
    ///
    /// ⚠️⚠️ UNITY'S SPLASH IS NOT SKIPPABLE ON THIS LICENCE AND NO SCENE IS LOADED UNDER IT, so
    /// the first thing a player sees is a silent engine logo followed by the studio's. 🧑 asked
    /// for the sound to cover it and for the load to be happening already:
    /// *"add a sound for unity too ig, make it so that stuff are loading already during the
    /// opening UNITY and BH Studios animations"*.
    ///
    /// `BeforeSplashScreen` is the only hook that runs earlier than the engine logo. The sting
    /// is started there through a persistent AudioSource, so it plays ACROSS the Unity logo and
    /// straight into the BH Studios animation as one continuous piece rather than starting over
    /// when the first scene finally loads.
    ///
    /// ⚠️ AND IT IS STARTED EXACTLY ONCE. <see cref="SplashScreen"/> checks this rather than
    /// firing its own copy, or the sting doubles up half a second out of phase with itself.
    /// </summary>
    public static class BootSting
    {
        private static AudioSource _source;

        public static bool Playing => _source != null && _source.isPlaying;
        public static bool Started { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSplashScreen)]
        private static void Play()
        {
            if (Started) return;
            Started = true;

            var clip = Resources.Load<AudioClip>("Sfx/boot_sting");
            if (clip == null) return;

            // ⚠️⚠️ NO LONGER ITS OWN LISTENER. It used to build one here, on the theory that
            // GameServices had not been built yet and a clip with no listener anywhere is silent
            // with no warning. That was true, but the fix created a second, permanent bug: this
            // GameObject and GameServices' were both HideAndDontSave, and FindObjectsByType does
            // not return objects with that flag. So AudioDirector's own "is one already here"
            // check could never see this one, and this listener's dedupe (never written, because
            // this system did not know it needed one) could never see AudioDirector's either.
            // Both existed, both stayed enabled, for the entire process, and Unity's own
            // duplicate-listener warning printed every frame it ran. Measured directly: logging
            // AudioDirector's own FindObjectsByType query at the moment the warning fired showed
            // it returning zero results while two real listeners were alive.
            //
            // Calling Ensure() here, at BeforeSplashScreen, forces GameServices (and its ONE
            // AudioListener) to exist before this line runs, rather than racing BeforeSceneLoad,
            // which is a separate hook of a later load type but was still one call this file used
            // to make an assumption about instead of forcing.
            GameServices.Ensure();

            var go = new GameObject("~BootSting");
            Object.DontDestroyOnLoad(go);
            go.hideFlags = HideFlags.HideAndDontSave;

            _source = go.AddComponent<AudioSource>();
            _source.clip = clip;
            _source.playOnAwake = false;
            _source.spatialBlend = 0.0f;

            var settings = Settings.SettingsStore.Current;
            _source.volume = Mathf.Clamp01(settings.MasterVolume * settings.SfxVolume);

            _source.Play();
        }

        /// <summary>Fades with the picture. A chord left ringing over the title menu is worse
        /// than no sting at all.</summary>
        public static void Stop()
        {
            if (_source == null) return;

            _source.Stop();
            Object.Destroy(_source.gameObject);
            _source = null;
        }
    }
}
