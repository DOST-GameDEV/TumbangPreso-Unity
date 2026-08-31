using System.Collections;
using System.IO;
using NUnit.Framework;
using TumbangPreso.UI;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace TumbangPreso.PlayTests
{
    /// <summary>
    /// Photographs every screen with its behaviour actually running.
    ///
    /// ⚠️⚠️ THE EDIT-MODE CAPTURES ARE NOT THE GAME. `Start()` never runs there, so every label
    /// still says whatever the `.tscn` authored, no seat row knows who you are, no keybind row
    /// exists, no tutorial page has content and no tab bar has tabs. A screen can therefore look
    /// perfect in a batch capture and be empty in the player. Half the port's UI is populated by
    /// its ported script rather than by the scene, so this suite is the only pass that sees what
    /// a player sees.
    ///
    /// ⚠️ AND IT IS A TEST RATHER THAN AN -executeMethod TOOL because entering play mode is what
    /// makes the behaviours run at all. The assertions are deliberately thin: the value is the
    /// images plus the fact that nothing threw on the way to them.
    ///
    /// ⚠️⚠️ DO NOT MEASURE A UI **COLOUR** OFF THESE PNGs. Every canvas in the game is
    /// `ScreenSpaceOverlay`, which a real frame composites AFTER post, so the HUD a player sees
    /// is ungraded. `Camera.Render` cannot see an overlay canvas at all, so this harness flips
    /// every canvas to `ScreenSpaceCamera` to photograph it — and that puts the UI THROUGH
    /// `ColourGrade` (contrast 1.03 on Eskinita, 1.07 on Bayan Plaza, saturation 1.18 on both).
    /// Amber `ffba00` does not come back as `ffba00` in these files, and an exact-match search
    /// for it finds nothing: that is a property of the capture, not of the build.
    ///
    /// GEOMETRY is unaffected and is what these files are for. For an exact, grade-proof reading
    /// of where each HUD element actually landed, use `HudLayoutProbe`, which dumps rects in the
    /// .tscn's own 1920x1080 space instead of photographing them.
    /// </summary>
    public class UiRuntimeShots
    {
        private const string OutDir = "Logs/shots-runtime";

        /// <summary>
        /// ⚠️ 1920x1080, THE SAME FRAME THE GODOT REFERENCE SHOTS WERE TAKEN AT, AND THAT IS THE
        /// ONLY REASON TO PIN IT. `Logs/shots-godot/*.png` are 1920x1080 captures of the running
        /// Godot build, every HUD number in this port is transcribed from a .tscn authored in
        /// 1920x1080 space, and both canvases match on HEIGHT. Capturing at 1600x900 meant every
        /// comparison against the reference had to be rescaled by hand before it could be
        /// measured, which is how "it looks about right" kept standing in for a measurement. At
        /// the reference size a pixel here is a pixel there and a wrong font size is countable.
        /// </summary>
        private const int Width = 1920;
        private const int Height = 1080;

        [UnityTest]
        public IEnumerator EveryScreenBootsAndDraws()
        {
            Directory.CreateDirectory(OutDir);

            yield return Shoot("MainMenu");
            yield return Overlay("SettingsPanel");

            // ⚠️ NO `TutorialPanel` SHOT: the panel was deleted on 2026-08-28 and TUTORIAL enters
            // the playable route directly. `Arena("Eskinita")` below photographs what replaced it.
            yield return Overlay("CreditsPanel");

            yield return Shoot("ModeSelect");
            yield return Shoot("MatchSetup");

            // Photograph the information-dense hero variant. Classic uses the same shell with
            // simpler trait meters, so the hero picker is the stronger layout stress test.
            SceneFlow.SelectedMode = Core.GameMode.HeroStrike;
            yield return Overlay("CharacterSelectPanel");
            yield return Shoot("MultiplayerSetup");
            yield return Shoot("MatchResult");

            yield return Arena("Eskinita");
            yield return EmoteWheelShot();
            yield return Arena("BayanPlaza");

            // ⚠️ LAST, BECAUSE IT LEAVES A 5 SECOND STUN RUNNING. The frost recedes with the
            // stun rather than being switched off, so a shot taken after this one would carry
            // whatever ice was left.
            yield return StunFrostShot();
        }

        /// <summary>
        /// § THE STUN FROST, photographed.
        ///
        /// ⚠️⚠️ IT SHIPPED WITHOUT ANYBODY EVER LOOKING AT IT. Both halves were ported, three
        /// tests were written for them, and every value in the shader was transcribed from the
        /// .gdshader — and not one frame of it had been rendered when it was handed over. The
        /// tests can say the coverage is 1.0 and the uniform is bound; they cannot say the ice
        /// reads as ice, that the band is even on all four edges, or that the centre is still
        /// clear enough to watch the round through. That is what this picture is for, and there
        /// is a Godot frame of the same effect to hold it against.
        /// </summary>
        private static IEnumerator StunFrostShot()
        {
            var hud = Object.FindFirstObjectByType<UI.Hud>();

            if (hud == null)
            {
                Debug.LogWarning("[Shot] no HUD in the arena to frost.");
                yield break;
            }

            CharacterMotor victim = null;

            foreach (var m in Object.FindObjectsByType<CharacterMotor>(FindObjectsSortMode.None))
            {
                if (!m.IsPerson || m.IsDefender) continue;
                victim = m;
                break;
            }

            if (victim == null)
            {
                Debug.LogWarning("[Shot] no attacker seat to stun.");
                yield break;
            }

            // Drive the HUD from the seat this shot stuns, rather than guessing which one the
            // installer handed the keyboard to: the screen half is the VICTIM's screen only.
            hud.Bind(victim);
            victim.ApplyStagger(Core.Balance.TagStunTime);

            // Past the ramp — `Hud.FrostRampIn` is 0.14 s and the body's is its own — and taken
            // on TIME rather than on a frame count, because both are rates per second.
            yield return new WaitForSecondsRealtime(0.6f);

            yield return CaptureScreen("StunFrost");
        }

        /// <summary>
        /// The match itself: the arena with its own light and haze, four characters, the can,
        /// and the HUD over the top of it.
        ///
        /// ⚠️ THE ARENA IS WHERE THE PORT IS ACTUALLY JUDGED. Every menu can be perfect and the
        /// game still not look like itself, and the maps converted with ZERO lights for the
        /// whole port: no key light, no ambient, no fog, Unity's default skybox. That renders
        /// the same geometry as a flat grey afternoon in a different game.
        /// </summary>
        private static IEnumerator Arena(string map)
        {
            var load = SceneManager.LoadSceneAsync(map, LoadSceneMode.Single);
            yield return ProbeWait.Done(load, "scene load");

            // The installer builds the whole match in Start, and the characters need a frame to
            // land on the ground before they are worth photographing.
            for (int i = 0; i < 12; i++) yield return null;

            yield return CaptureScreen(map);
        }

        /// <summary>
        /// The emote wheel, open, over a live arena.
        ///
        /// ⚠️⚠️ IT IS PHOTOGRAPHED BECAUSE IT SHIPPED AS A PILE OF WHITE SQUARES AND NOTHING
        /// CAUGHT IT. Every slice was an `Image` with a radial fill and no sprite assigned, so
        /// the fill was cutting sectors out of a SQUARE; eight of those rotated 45 degrees apart
        /// is overlapping slabs with the labels crossing each other. Every test passed the whole
        /// time, because a wheel that is built without throwing is a wheel that "works". This is
        /// the only kind of check that can see it.
        ///
        /// ⚠️ OPENED THROUGH `Open()`, NOT BY A KEY. The wheel reads relative mouse motion and
        /// the batch runner has no mouse, so the selection stays at -1 and the shot shows the
        /// resting state, which is the one that was broken.
        /// </summary>
        private static IEnumerator EmoteWheelShot()
        {
            var wheel = Object.FindFirstObjectByType<UI.EmoteWheel>();

            if (wheel == null)
            {
                Debug.LogWarning("[Shot] no EmoteWheel in the arena.");
                yield break;
            }

            wheel.Open();

            for (int i = 0; i < 5; i++) yield return null;

            yield return Capture("EmoteWheel");

            wheel.Close(play: false);
        }

        /// <summary>
        /// ⚠️ THE ARENA IS PHOTOGRAPHED THROUGH THE GAME'S OWN CAMERA, not by re-pointing one.
        /// The match camera is the first-person rig on the local seat, and moving it to frame a
        /// nicer shot would photograph a view the player never has.
        /// </summary>
        private static IEnumerator CaptureScreen(string name)
        {
            var cam = UnityEngine.Camera.main;
            if (cam == null)
            {
                Debug.LogWarning($"[Shot] {name} has no main camera.");
                yield break;
            }

            // ⚠️ THE TARGET GOES ON FIRST. See the note in Capture: a ScreenSpaceCamera canvas
            // lays out against the camera's pixel rect, so assigning the 1600x900 target after
            // the layout photographs the batch runner's own resolution stretched into 16:9.
            var rt = new RenderTexture(Width, Height, 24, RenderTextureFormat.ARGB32);
            var prev = cam.targetTexture;

            cam.targetTexture = rt;

            foreach (var c in Object.FindObjectsByType<Canvas>(FindObjectsInactive.Exclude,
                                                              FindObjectsSortMode.None))
            {
                if (c.renderMode != RenderMode.ScreenSpaceOverlay) continue;

                c.renderMode = RenderMode.ScreenSpaceCamera;
                c.worldCamera = cam;

                // ⚠️ IN FRONT OF THE NEAR PLANE OR THE HUD IS INVISIBLE. A first-person rig
                // clips at 0.05, and the default plane distance of 100 puts the whole HUD
                // behind the street.
                c.planeDistance = cam.nearClipPlane + 0.01f;
            }

            // ⚠️⚠️ TWO REAL FRAMES BEFORE RENDERING, AND WITHOUT THEM EVERY ARENA SHOT WAS
            // HORIZONTALLY STRETCHED. `CanvasScaler` recomputes in its own Update from the
            // canvas's rendering display size, which only becomes the render target once the
            // target has been assigned AND a frame has run. Rendering in the same frame as the
            // assignment lays the HUD out at the BATCH RUNNER's aspect — measured at 1440x1080
            // in reference units, a 4:3 window — and then draws that into a 1920x1080 texture,
            // which is a 1.33x horizontal stretch on the picture and on nothing else.
            //
            // ⚠️ THIS IS THE SECOND TIME THIS EXACT FAULT HAS SHIPPED IN THIS FILE. `Capture`
            // below already carries the fix and its own note about it, and the ledger records
            // that the stretch was read off the captures TWICE as a bug in `ModelPreview`. The
            // arena path was written separately and never got it — and the arena is the shot the
            // port is actually judged on. Measured again 2026-08-16 by comparing the capture's
            // scoreboard, 456 px wide, against `HudLayoutProbe`'s reading of the same panel at
            // 440: exactly 1440/1920.
            yield return null;
            yield return null;

            Canvas.ForceUpdateCanvases();
            cam.Render();

            RenderTexture.active = rt;
            var tex = new Texture2D(Width, Height, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, Width, Height), 0, 0);
            tex.Apply();

            RenderTexture.active = null;
            cam.targetTexture = prev;

            File.WriteAllBytes($"{OutDir}/{name}.png", tex.EncodeToPNG());

            Object.DestroyImmediate(tex);
            rt.Release();
            Object.DestroyImmediate(rt);

            Debug.Log($"[Shot] wrote {OutDir}/{name}.png");
        }

        /// <summary>
        /// The MULTIPLAYER lobby, which `EveryScreenBootsAndDraws` cannot photograph.
        ///
        /// ⚠️⚠️ `Shoot("MatchSetup")` PHOTOGRAPHS THE PRACTICE SCREEN, NOT THE LOBBY, AND THEY ARE
        /// NOW TWO DIFFERENT PICTURES. One scene draws both: offline it is a map picker with a
        /// bots row and the wide 22 m shot of an empty street, and in multiplayer it is a room
        /// with four people standing in it at 32 degrees from 7 m. `SceneFlow.Networked` is the
        /// only thing that decides, and the other test never sets it, so every capture of this
        /// screen ever taken has been of the half that has no cast.
        ///
        /// ⚠️ IT AUTO-HOSTS ON LOAD, WHICH MEANS THIS TEST BINDS A PORT. That is the feature: the
        /// lobby opens a LAN room on arrival, and a capture of it not having done so would be a
        /// picture of the fallback state. It fails soft when the port is taken (see
        /// `ConvertedMatchSetup.AutoHost`), so a machine already running the game gets the
        /// not-connected lobby photographed instead of a hang.
        ///
        /// ⚠️ THE FILENAMES CARRY A VERSION, per `CLAUDE.md` § 6.1: chat clients cache by name, so
        /// overwriting a shot leaves the previous one on screen and the whole review is conducted
        /// against an image that is no longer on disk. Bump `ShotVersion` on every iteration.
        /// </summary>
        private const string ShotVersion = "v43";

        [UnityTest]
        public IEnumerator TheLobbyDraws()
        {
            Directory.CreateDirectory(OutDir);

            bool previousNetworked = SceneFlow.Networked;
            var previousMode = SceneFlow.SelectedMode;

            SceneFlow.Networked = true;

            var load = SceneManager.LoadSceneAsync("MatchSetup", LoadSceneMode.Single);
            yield return ProbeWait.Done(load, "scene load");

            // ⚠️ LONGER THAN `Shoot`'S 1.6 s, AND THE EXTRA IS NOT PADDING. Three things have to
            // finish that no other screen waits on: the transport handshake the auto-host starts,
            // the ADDITIVE load of a dressed arena into the preview surface, and the cast being
            // adopted into it once `MapShown` fires. A capture before that is a photograph of an
            // empty street with the chrome over it, which is indistinguishable from the cast
            // being broken.
            yield return new WaitForSecondsRealtime(4.0f);

            var nameField = Find("PlayerNameEdit")?.GetComponent<InputField>();
            Assert.IsNotNull(nameField, "the lobby must expose its editable player name.");
            Assert.AreEqual(Core.Balance.PlayerNameMax, nameField.characterLimit,
                "the lobby name field must enforce the same hard cap as Settings and the wire.");

            yield return Capture($"Lobby-{ShotVersion}");

            // Both drawers are part of the requested composition checkpoint. Photographing only
            // the clean collapsed state previously let clipped rows and merged network actions
            // survive review unnoticed.
            // ⚠️ BY THE TOGGLE'S OWN NAME, NOT BY ITS HOST'S. It used to look up `SettingsDrawer`
            // and take the first `Button` under it, which stopped finding anything the moment the
            // left-hand furniture became one rail (`LobbyChrome.BuildLeftRail`): the host is
            // `LobbyLeftRail` now and its first button would be whichever the layout ordered first.
            // The failure was silent, because the shot is taken inside an `if`.
            var settingsToggle = Find("SettingsDrawerToggle")?.GetComponent<Button>();
            Assert.IsNotNull(settingsToggle,
                "the match-settings drawer has no toggle to open, so the open state cannot be "
                + "photographed. See LobbyChrome.BuildLeftRail.");

            if (settingsToggle != null)
            {
                settingsToggle.onClick.Invoke();
                yield return new WaitForSecondsRealtime(0.25f);
                yield return Capture($"LobbySettings-{ShotVersion}");
                settingsToggle.onClick.Invoke();
            }

            var lobbyToggle = Find("LobbyDrawerToggle")?.GetComponent<Button>();
            if (lobbyToggle != null)
            {
                lobbyToggle.onClick.Invoke();
                yield return new WaitForSecondsRealtime(0.25f);
                yield return Capture($"LobbyServers-{ShotVersion}");
                lobbyToggle.onClick.Invoke();
            }

            // The join card over the top of it, which is the other half of this screen.
            var open = Find("OpenJoinButton");
            var panel = Find("LobbyJoinPanel");

            if (open != null && panel != null)
            {
                panel.SetActive(true);

                yield return new WaitForSecondsRealtime(0.6f);
                yield return Capture($"LobbyJoin-{ShotVersion}");

                panel.SetActive(false);
            }
            else
            {
                Debug.LogWarning("[Shot] the lobby has no join card to photograph.");
            }

            SceneFlow.Networked = previousNetworked;
            SceneFlow.SelectedMode = previousMode;

            var session = Net.NetSession.Instance;
            if (session != null) session.Stop();
        }

        private static IEnumerator Shoot(string scene)
        {
            var load = SceneManager.LoadSceneAsync(scene, LoadSceneMode.Single);
            yield return ProbeWait.Done(load, "scene load");

            // ⚠️ LONG ENOUGH FOR THE PENNANTS TO FINISH UNFURLING. `arrow_button.gd::animate_in`
            // runs for 0.45 s with a per-button stagger on top, and a capture three frames after
            // load photographs the buttons mid-animation at a fraction of their width. That
            // looks exactly like a layout bug and sent one pass chasing anchors that were right.
            //
            // ⚠️⚠️ AND WAITING 90 FRAMES FOR IT WAS THE SAME MISTAKE IN A THINNER DISGUISE, WHICH
            // THIS LEDGER HAS ALREADY RECORDED ONCE: *"a test can fail for being right. The
            // smoothing test waited ninety FRAMES for a rate expressed per SECOND, and the batch
            // runner renders at over 500 fps."* An empty menu scene runs far faster than that, so
            // 90 frames is a fraction of a second and every menu capture was taken mid-unfurl —
            // which is why the main menu's QUIT pennant photographed half-transparent and
            // oversized, and why the buttons measured taller than `MainMenu.tscn` authors them.
            // WAIT ON TIME. Realtime, so a probe that has left `Time.timeScale` alone and one
            // that has not both get the same wait.
            yield return new WaitForSecondsRealtime(1.6f);
            yield return null;

            yield return Capture(scene);
        }

        /// <summary>Opens an overlay that lives inside the screen already loaded.</summary>
        private static IEnumerator Overlay(string node)
        {
            var target = Find(node);

            if (target == null)
            {
                Debug.LogWarning($"[Shot] no '{node}' in the loaded scene.");
                yield break;
            }

            target.SetActive(true);

            yield return null;
            yield return null;
            yield return null;

            yield return Capture(node);
            target.SetActive(false);
        }

        private static GameObject Find(string name)
        {
            foreach (var root in SceneManager.GetActiveScene().GetRootGameObjects())
            {
                var hit = FindIn(root.transform, name);
                if (hit != null) return hit;
            }

            return null;
        }

        private static GameObject FindIn(Transform t, string name)
        {
            if (t.name == name) return t.gameObject;

            for (int i = 0; i < t.childCount; i++)
            {
                var hit = FindIn(t.GetChild(i), name);
                if (hit != null) return hit;
            }

            return null;
        }

        private static IEnumerator Capture(string name)
        {
            var cam = UnityEngine.Camera.main;
            if (cam == null)
            {
                Debug.LogWarning($"[Shot] {name} has no main camera.");
                yield break;
            }

            // ⚠️⚠️ THE TARGET IS ASSIGNED BEFORE THE CANVAS IS LAID OUT, AND THE OTHER ORDER
            // FAKED A STRETCH THAT COST TWO SESSIONS. A ScreenSpaceCamera canvas sizes itself
            // from its camera's pixel rect, which follows `targetTexture` when there is one and
            // the SCREEN when there is not. Laying out first and assigning the 1600x900 target
            // afterwards means the whole UI, every RenderTexture derived from a panel rect, and
            // every camera aspect derived from that, were all computed at whatever resolution the
            // batch runner happened to open, and then photographed into 16:9. On a 4:3 runner
            // that is a 1.33x horizontal stretch applied to the picture and to nothing else, and
            // it was read off the capture twice as a fault in `ModelPreview`. Measured: the
            // subject's head came out 1.31x wide against the same model on the toon bench.
            var rt = new RenderTexture(Width, Height, 24, RenderTextureFormat.ARGB32);
            var prev = cam.targetTexture;

            cam.targetTexture = rt;

            // ⚠️ AN OVERLAY CANVAS IS INVISIBLE TO Camera.Render. It draws straight to the back
            // buffer, so a capture through a camera photographs an empty scene unless the canvas
            // is flipped to ScreenSpaceCamera first.
            foreach (var c in Object.FindObjectsByType<Canvas>(FindObjectsInactive.Exclude,
                                                              FindObjectsSortMode.None))
            {
                if (c.renderMode != RenderMode.ScreenSpaceOverlay) continue;

                c.renderMode = RenderMode.ScreenSpaceCamera;
                c.worldCamera = cam;
                c.planeDistance = 1.0f;
            }

            Canvas.ForceUpdateCanvases();

            foreach (var c in Object.FindObjectsByType<Canvas>(FindObjectsInactive.Exclude,
                                                              FindObjectsSortMode.None))
            {
                var rect = c.transform as RectTransform;
                if (rect != null) LayoutRebuilder.ForceRebuildLayoutImmediate(rect);
            }

            Canvas.ForceUpdateCanvases();

            // ⚠️ TWO REAL FRAMES, SO THE PREVIEW RIGS SEE THE NEW RECT. `ModelPreview` and
            // `MapPreviewSurface` size their render targets from a panel rect in LateUpdate, so
            // the frame that first lays the canvas out at the capture size is the one that
            // rebuilds them, and only the frame AFTER that draws at the new size. Rendering
            // immediately photographs the previous resolution's target scaled onto the new rect,
            // which is the same class of fault this function's own header describes.
            yield return null;
            yield return null;

            Canvas.ForceUpdateCanvases();
            cam.Render();

            RenderTexture.active = rt;
            var tex = new Texture2D(Width, Height, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, Width, Height), 0, 0);
            tex.Apply();

            RenderTexture.active = null;
            cam.targetTexture = prev;

            File.WriteAllBytes($"{OutDir}/{name}.png", tex.EncodeToPNG());

            Object.DestroyImmediate(tex);
            rt.Release();
            Object.DestroyImmediate(rt);

            Debug.Log($"[Shot] wrote {OutDir}/{name}.png");
        }
    }
}
