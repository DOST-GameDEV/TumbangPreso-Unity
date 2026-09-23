# Native UI follow-up

Internal Windows build8f81e96bd succeeded:1235MB,87seconds. Its identity honestly
says dirty: four recovery/swim assets regenerate from the current female-a/Rafi
rigs, plus the two original PNG meta whitespace changes. Exact patch is retained
in owned QUAL Logs/ux1-native-build/generated-churn.patch. This is not a clean
release candidate; do not alter its already-built identity.

Two isolated native processes hosted and joined127.0.0.1:19061. Host admitted peer1
and client received seat2. This proves real address/LAN entry, not visual lobby,
UGS queue, rematch or graceful shutdown. Both owned processes were terminated
after that handshake. Logs and launch identities remain in QUAL Logs/ux1-native-peers.
Windows capture failed with SetIsBorderRequired0x80004002, then no screenshot
targets on its one fresh-window retry. No capture-helper debugging or rewrite.

## Actual startup defect

BootSting.Play at BeforeSplashScreen calls GameServices.Ensure, which adds
PlayerAccount and runs its Awake/sign-in. NetBootstrap selects -tp-profile later
at BeforeSceneLoad. NetIdentity started with a null profile, so its first attempt
could use the default authentication cache. Later SetProfile dropped that task,
starting another request while the first was signing in. Both native logs show
error10000, then eventual success. This is a named-profile startup race, not proof
that all ordinary players cannot sign in.

NetIdentity now selects the launch profile on first access using ProfilePaths'
existing parser. The later bootstrap repeats the same value and preserves the
task. Successful logging uses the SDK's actual Profile. Login/title art stays
unchanged. Existing13NetworkMultiProcessProbes pass; native reproduction is pending.

Unity documents ClientInvalidUserState for an already signed-in or signing-in
player in its [authentication service API](https://docs.unity.cn/Packages/com.unity.services.authentication%403.3/api/Unity.Services.Authentication.IAuthenticationService.html).
The [cached-session behavior](https://docs.unity.com/en-us/authentication/session-management)
is preserved.

## Next bounded native check

Reuse NetAutomationProbe's opt-in controls and NetStateReport. Autostart can press
the real hub START GAME after two peers arrive. An explicit review flag selects
the existing automatic room rule; diagnostic READY is disabled in that mode so
it cannot conceal broken intro/quorum/countdown behavior. Reports record whether
the visible lobby, arrival component and automatic countdown were observed.
These are state evidence, not screenshots or visual-quality claims. Ordinary
play is unchanged. Run two internal players, one30second round and the existing
rematch path once. Do not expand into a new capture framework.
