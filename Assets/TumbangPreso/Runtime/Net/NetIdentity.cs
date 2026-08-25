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
    /// <summary>
    /// Why online play is or is not available. Three distinguishable answers, because
    /// "online sign-in unavailable" was one sentence covering three unrelated situations and
    /// none of them could be told apart while debugging a live Relay connection.
    /// </summary>
    public enum OnlineState
    {
        /// <summary>Sign-in has not been attempted yet.</summary>
        Unknown,

        /// <summary>A UGS session is live. Relay and Lobby are usable.</summary>
        SignedIn,

        /// <summary>The build carries no cloudProjectId, so it belongs to no UGS project.</summary>
        NotLinked,

        /// <summary>There is no network, or UGS itself did not answer.</summary>
        Unreachable,
    }

    public static class NetIdentity
    {
        public const string DefaultProfile = "default";

        private static string _customProfile;
        private static string _overrideTokenForTesting;

        // ⚠ ONE ATTEMPT PER SESSION, AND THE TASK ITSELF IS THE CACHE. Sign-in used to re-run
        // the whole initialise-and-sign-in path on every call. Five call sites ask for it, two
        // of which fire per host and two per join, so a session that could not reach UGS paid
        // for UnityServices.InitializeAsync and logged the same warning 21 times. Caching the
        // Task rather than a bool also means a caller arriving while the first attempt is still
        // in flight awaits THAT attempt instead of starting a second one beside it.
        private static Task<bool> _attempt;

        /// <summary>Which of the three online situations this session is in.</summary>
        public static OnlineState State { get; private set; } = OnlineState.Unknown;

        /// <summary>The one sentence explaining <see cref="State"/>, safe to show a player.</summary>
        public static string StateReason { get; private set; } = "";

        /// <summary>True only when Relay and Lobby can actually be used.</summary>
        public static bool IsOnline => State == OnlineState.SignedIn;

        /// <summary>
        /// How many times the sign-in path has actually run. Exists so a probe can assert the
        /// once-per-session rule.
        /// </summary>
        /// ⚠ A COUNTER, NOT THE IDENTITY OF THE RETURNED TASK. The obvious probe, asserting
        /// that two callers get the same Task back, passes even when caching is broken: an
        /// `async Task&lt;bool&gt;` that finishes synchronously hands back a SHARED cached Task
        /// for false, so two genuinely separate attempts compare as the same object. The first
        /// version of this probe was green for that reason and proved nothing.
        public static int SignInAttempts { get; private set; }

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
            string next = string.IsNullOrWhiteSpace(profile) ? null : profile.Trim();
            if (next == _customProfile) return;

            _customProfile = next;

            // ⚠ THE CACHED ATTEMPT BELONGED TO THE OLD PROFILE. -tp-profile is read at
            // BeforeSceneLoad and the boot attempt fires at AfterSceneLoad, so in practice this
            // never triggers. It exists so that a switch made later cannot report the previous
            // profile's session as this one's.
            _attempt = null;
            State = OnlineState.Unknown;
            StateReason = "";
        }

        /// <summary>
        /// Runs the one sign-in attempt this session gets, at boot, before any menu can ask for
        /// it. Every later caller awaits the same attempt.
        /// </summary>
        /// ⚠ AfterSceneLoad, NOT BeforeSceneLoad. NetBootstrap reads -tp-profile at
        /// BeforeSceneLoad, and two hooks of the same load type run in an undefined order, so
        /// signing in at BeforeSceneLoad would race the profile that decides which UGS session
        /// this instance gets. That race is exactly what §12's two-instances-collide warning is
        /// about.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void SignInAtBoot()
        {
            _ = EnsureSignedInAsync();
        }

        /// <summary>
        /// Authenticate with UGS anonymously for online play.
        /// </summary>
        /// ⚠ THE LOCAL TOKEN IS A LAN FALLBACK AND NOTHING ELSE. Returning false here does NOT
        /// mean "carry on with the offline token": Relay and Lobby need a real signed-in
        /// session and cannot be made to work without one. Every online caller aborts on false.
        /// The offline token is what LAN hosting and joining key off, and that path is N1
        /// working as designed rather than a defect to remove.
        public static Task<bool> EnsureSignedInAsync(string profile = null)
        {
            if (!string.IsNullOrEmpty(profile)) SetProfile(profile);

            return _attempt ??= AttemptSignInAsync();
        }

        private static async Task<bool> AttemptSignInAsync()
        {
            SignInAttempts++;

            // ⚠️ IN BATCH MODE, UGS network sign-in is bypassed. Headless test runs and probes
            // run without a display or interactive session and operate on offline tokens.
            if (Application.isBatchMode)
            {
                Settle(OnlineState.Unreachable,
                    "UGS sign-in is disabled in batch mode. LAN hosting and joining are unaffected.");
                return false;
            }

            // ⚠ ASKED BEFORE ANYTHING IS ATTEMPTED, because an unlinked build is not a failure
            // to reach UGS. It is a build that was never told which UGS project it belongs to,
            // and no amount of network will fix it. This reads the same value UGS itself tests:
            // CorePackageInitializer throws UnityProjectNotLinkedException when
            // CloudProjectId.GetCloudProjectId(), which returns Application.cloudProjectId,
            // comes back empty. That exception type is internal to the package and cannot be
            // caught by type from here, so the question is asked directly instead.
            if (string.IsNullOrEmpty(Application.cloudProjectId))
            {
                Settle(OnlineState.NotLinked,
                    "This build is not linked to a UGS project, so Relay and Lobby cannot be used. " +
                    "LAN hosting and joining are unaffected.");
                return false;
            }

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

                Settle(OnlineState.SignedIn,
                    $"Signed in to UGS as {AuthenticationService.Instance.PlayerId} on profile " +
                    $"{Profile}. Relay and Lobby are available.");
                return true;
            }
            catch (ServicesInitializationException e)
            {
                // ⚠ THIS IS NOT THE UNLINKED CASE, AND IT WAS FIRST WRITTEN AS IF IT WERE.
                // UnityProjectNotLinkedException derives from this type, so catching it here and
                // calling it NotLinked looks right. It is not: Unity throws that exception
                // BECAUSE Application.cloudProjectId is empty, and that exact value is tested
                // above before anything is attempted. So every exception that reaches here has
                // already passed the link check and is something else. A probe caught this
                // reporting "not linked to a UGS project" for an editor that had simply refused
                // to initialise services outside Play Mode, which is the opposite of useful when
                // the whole point is telling the three situations apart. The type carries the
                // real reason and is logged in full.
                Settle(OnlineState.Unreachable,
                    "UGS refused to initialise, so Relay and Lobby cannot be used right now. " +
                    "LAN hosting and joining are unaffected.", e);
                return false;
            }
            catch (RequestFailedException e)
            {
                Settle(OnlineState.Unreachable,
                    $"UGS could not be reached ({Describe(e.ErrorCode)}), so Relay and Lobby cannot " +
                    "be used right now. LAN hosting and joining are unaffected.", e);
                return false;
            }
            catch (Exception e)
            {
                Settle(OnlineState.Unreachable,
                    "UGS sign-in failed for an unrecognised reason, so Relay and Lobby cannot be " +
                    "used right now. LAN hosting and joining are unaffected.", e);
                return false;
            }
        }

        /// <summary>
        /// The third of the three situations: the session is live and a Relay or Lobby call
        /// failed anyway. Reported through here so it never reads as a sign-in problem.
        /// </summary>
        /// ⚠ THIS DELIBERATELY DOES NOT CHANGE State. The session is still signed in and the
        /// next call may well succeed, so downgrading to Unreachable would make one refused
        /// lobby query look like the network had gone. Callers gate on State, so a wrong answer
        /// here would take online hosting down for the rest of the session.
        public static void ReportServiceCallFailed(string service, Exception e)
        {
            string session = State == OnlineState.SignedIn
                ? $"signed in as {SignedInPlayerId()}"
                : $"online state {State}";

            Debug.LogWarning(
                $"[NetIdentity] {service} failed while the session was live ({session}). This is " +
                $"not a sign-in problem. [{e.GetType().Name}: {e.Message}]");
        }

        /// <summary>Records the settled state and logs it exactly once.</summary>
        private static void Settle(OnlineState state, string reason, Exception e = null)
        {
            State = state;
            StateReason = reason;

            // ⚠ THE EXCEPTION TYPE IS PART OF THE MESSAGE, NOT SWALLOWED. The message alone was
            // what made these three indistinguishable in the first place.
            string detail = e == null ? "" : $" [{e.GetType().Name}: {e.Message}]";

            if (state == OnlineState.SignedIn) Debug.Log($"[NetIdentity] {reason}{detail}");
            else Debug.LogWarning($"[NetIdentity] {reason}{detail}");
        }

        private static string Describe(int errorCode)
        {
            switch (errorCode)
            {
                case CommonErrorCodes.TransportError: return "no network route";
                case CommonErrorCodes.Timeout: return "timed out";
                case CommonErrorCodes.ServiceUnavailable: return "service unavailable";
                default: return $"error code {errorCode}";
            }
        }

        private static string SignedInPlayerId()
        {
            try
            {
                if (UnityServices.State == ServicesInitializationState.Initialized &&
                    AuthenticationService.Instance != null &&
                    AuthenticationService.Instance.IsSignedIn)
                {
                    return AuthenticationService.Instance.PlayerId;
                }
            }
            catch
            {
                // Falls through to the unknown answer below.
            }

            return "unknown";
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

            // ⚠ THE CACHED ATTEMPT IS NOW A LIE. It reports a live session that has just been
            // torn down, and State drives whether online hosting is even offered.
            _attempt = null;
            State = OnlineState.Unknown;
            StateReason = "";
            SignInAttempts = 0;
        }

        /// <summary>Testing seam to simulate specific tokens without network or settings I/O.</summary>
        public static void OverrideForTesting(string token) => _overrideTokenForTesting = token;

        public static void ResetForTesting()
        {
            _overrideTokenForTesting = null;
            _customProfile = null;
            _attempt = null;
            State = OnlineState.Unknown;
            StateReason = "";
            SignInAttempts = 0;
        }
    }
}
