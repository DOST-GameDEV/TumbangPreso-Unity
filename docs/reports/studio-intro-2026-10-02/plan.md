# Studio intro before loading

The latest Feedback requests the existing BH Studios intro before game loading,
no press-anything-to-skip text, and a white fade when input skips the intro.
Use the already bound opening_animation.mp4 without changing authored pixels.
Create a lightweight intro surface before building the existing loading UI or
starting its heavy preload. Any fresh keyboard, mouse, controller button or
touch press may fade out this presentation, never bypass asset/account/menu
readiness. Natural completion uses the same white handoff. Missing/failed video
must settle to normal loading with a bounded preparation/playback budget.

Claim SplashScreen.cs and a dedicated intro partial, MenuActivation cleanup,
MenuNav's focused studio-skip query, the serialized SplashScreen skip hint and
its importer binding only, plus focused tests and owning loading documentation.
Keep supplied loading art, login/menu routes, source clip, existing sound asset,
preload stages and activation barrier. Test actual native clip playback, input
routes, white fade, missing/failed media fallback and destruction cleanup; reuse
existing menu barrier controls. One isolated native case per process, separate
import, unchanged memory guard. Native checks do not qualify a fresh player or
sound quality. Do not alter unrelated engineering lanes or use the personal PC.
