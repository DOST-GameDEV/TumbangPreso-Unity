using System;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Core;
using UnityEngine;

namespace TumbangPreso.Net
{
    /// <summary>
    /// Player identity management supporting both UGS Authentication and offline LAN play.
    ///
    /// ⚠️⚠️ TWO INSTANCES ON ONE MACHINE COLLIDE ON IDENTITY WITHOUT PROFILES. Two builds
    /// sharing one machine share the auth cache and receive the identical PlayerId from UGS,
    /// and offline they read the same settings.json token. That causes them to collide on the
    /// same seat, one overwriting the other's role. Passing a profile (via -tp-profile) isolates
    /// both the UGS session and the offline fallback token.
    ///
    /// ⚠️ OFFLINE IS A FIRST-CLASS CITIZEN. A LAN match in a venue with no internet must work
    /// seamlessly. When UGS Authentication cannot be reached, identity falls back to the minted
    /// token in GameSettings without interrupting the flow or throwing unhandled errors.
    ///
    /// ⚠️ ONE OWNER PER FACT. Seating and reconnection in LobbySession continue to key off this
    /// single token string. Seating logic does not learn a second concept for online vs offline.
    /// </summary>
    public static class NetIdentity
    {
        public const string DefaultProfile = "default";

        private static string _customProfile;
        private static string _overrideTokenForTesting;

        /// <summary>The active profile name for UGS and multi-instance separation.</summary>
        public static string Profile => _customProfile ?? DefaultProfile;

        /// <summary>
        /// The current stable player token. Returns UGS PlayerId when signed in online,
        /// or the validated settings token (with profile isolation) offline.
        /// </summary>
        public static string Token
        {
            get
            {
                if (!string.IsNullOrEmpty(_overrideTokenForTesting))
                    return _overrideTokenForTesting;

                try
                {
                    if (UnityServices.State == ServicesInitializationState.Initialized &&
                        AuthenticationService.Instance != null &&
                        AuthenticationService.Instance.IsSignedIn)
                    {
                        string ugsId = AuthenticationService.Instance.PlayerId;
                        if (!string.IsNullOrEmpty(ugsId)) return ugsId;
                    }
                }
                catch
                {
                    // UGS not initialized or not available. Fall through to local token.
                }

                return LocalToken;
            }
        }

        /// <summary>
        /// The local fallback token, salted with the profile if a non-default profile is active.
        /// </summary>
        public static string LocalToken
        {
            get
            {
                string baseToken = Settings.SettingsStore.Current.PlayerToken;
                if (string.IsNullOrEmpty(baseToken))
                {
                    baseToken = Settings.GameSettings.MintToken();
                    Settings.SettingsStore.Current.PlayerToken = baseToken;
                }

                if (string.IsNullOrEmpty(_customProfile) || _customProfile == DefaultProfile)
                    return baseToken;

                return $"{baseToken}_{_customProfile}";
            }
        }

        /// <summary>Sets the profile name before UGS initialization.</summary>
        public static void SetProfile(string profile)
        {
            if (string.IsNullOrWhiteSpace(profile))
            {
                _customProfile = null;
                return;
            }

            _customProfile = profile.Trim();
        }

        /// <summary>
        /// Authenticate with UGS anonymously for online play. Falls back to offline token on failure.
        /// </summary>
        public static async Task<bool> EnsureSignedInAsync(string profile = null)
        {
            if (!string.IsNullOrEmpty(profile)) SetProfile(profile);

            try
            {
                if (UnityServices.State == ServicesInitializationState.Uninitialized)
                {
                    var options = new InitializationOptions();
                    if (!string.IsNullOrEmpty(_customProfile))
                    {
                        options.SetProfile(_customProfile);
                    }

                    await UnityServices.InitializeAsync(options);
                }

                if (!AuthenticationService.Instance.IsSignedIn)
                {
                    if (!string.IsNullOrEmpty(_customProfile) &&
                        AuthenticationService.Instance.Profile != _customProfile)
                    {
                        AuthenticationService.Instance.SwitchProfile(_customProfile);
                    }

                    await AuthenticationService.Instance.SignInAnonymouslyAsync();
                }

                Debug.Log($"[NetIdentity] Signed in online with PlayerId: {AuthenticationService.Instance.PlayerId} (profile: {Profile})");
                return true;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[NetIdentity] Online sign-in unavailable, using local token: {e.Message}");
                return false;
            }
        }

        /// <summary>Signs out of UGS authentication if signed in.</summary>
        public static void SignOut()
        {
            try
            {
                if (UnityServices.State == ServicesInitializationState.Initialized &&
                    AuthenticationService.Instance != null &&
                    AuthenticationService.Instance.IsSignedIn)
                {
                    AuthenticationService.Instance.SignOut();
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[NetIdentity] Sign out warning: {e.Message}");
            }
        }

        /// <summary>Testing seam to simulate specific tokens without network or settings I/O.</summary>
        public static void OverrideForTesting(string token) => _overrideTokenForTesting = token;

        public static void ResetForTesting()
        {
            _overrideTokenForTesting = null;
            _customProfile = null;
        }
    }
}
