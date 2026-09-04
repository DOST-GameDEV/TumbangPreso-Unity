using System.IO;
using NUnit.Framework;

namespace TumbangPreso.Tests
{
    /// <summary>
    /// Every camera that draws the WORLD installs the same set of image passes, so the arena a
    /// player looks at in a menu is drawn by the same code as the arena they play in.
    ///
    /// ⚠️⚠️ THIS HAS NOW GONE WRONG THREE TIMES AND EACH TIME IT WAS A CAMERA BUILT FROM CODE
    /// THAT WAS GIVEN SOME OF THE PASSES. `CameraRig.Awake` is the reference and adds three:
    /// `ColourGrade`, `PostAntiAlias` and `WorldOutline`.
    ///
    ///  1. `SpectatorCamera` got the first two and never the third. Its own header says so, at
    ///     length, and the fix is twenty lines of comment about why nobody noticed.
    ///  2. `MapPreviewSurface` got the FIRST ONLY, which is the lobby and the map select. 🧑
    ///     2026-09-03: *"i noticed the shader doesnt show up in map select so the map select or
    ///     LOBBY look IS COMPLETELY DIFF to the actual game"*, and **"can u make sure game and
    ///     lobby preview looks exactly the same"**.
    ///  3. `MapPreview`, the older unreferenced class that shared the live one's NAME and
    ///     nothing else, had none of the three. It was deleted on 2026-09-03 (`docs/TODO.md`
    ///     § 130.7) and its camera notes moved onto `MapPreviewSurface` first. ⚠️ It is kept in
    ///     this list because it is the reason the list exists: two classes one letter apart in
    ///     the file browser, one live and one dead, is how a session spends an afternoon fixing
    ///     a preview that nothing constructs. **Do not reintroduce a second preview class.**
    ///
    /// ⚠️⚠️ AND IT READS THE SOURCE AS TEXT, WHICH IS `SceneScriptCheck` AND `InputSurfaceCheck`'s
    /// ARGUMENT ONE LEVEL UP: every runtime probe can only measure a camera that was BUILT during
    /// a test run, and each of these three is built by a screen somebody has to open. A screen
    /// nobody opens during a run has no coverage at all, which is `CLAUDE.md` § 4a's § 96 and
    /// § 124.11 fault. The text is there whether anybody opened the screen or not.
    ///
    /// ⚠️ IT ASSERTS THE INSTALL, NOT THE PICTURE. Whether the ink reads correctly at a distance
    /// is a render question and belongs in a shot, exactly as `RoleMarkerTests` says about the
    /// taya's ring. This asserts the pass is attached and switched on, which is the half a
    /// camera angle cannot defeat.
    /// </summary>
    public sealed class WorldCameraPassParityTests
    {
        private const string Runtime = "Assets/TumbangPreso/Runtime/";

        private static string Read(string relative)
        {
            string path = Path.Combine(Runtime, relative);
            Assert.IsTrue(File.Exists(path), $"{relative} has moved; this test names it by path");
            return File.ReadAllText(path);
        }

        /// <summary>
        /// ⚠️ THE REFERENCE. If this one ever stops installing all three, the two below are
        /// matching the wrong thing and the failure should point HERE first.
        /// </summary>
        [Test]
        public void TheGameplayRigIsStillTheReferenceAndInstallsAllThreePasses()
        {
            string rig = Read("Camera/CameraRig.cs");

            StringAssert.Contains("AddComponent<Visual.ColourGrade>", rig);
            StringAssert.Contains("AddComponent<Visual.PostAntiAlias>", rig);
            StringAssert.Contains("AddComponent<Visual.WorldOutline>", rig);
            StringAssert.Contains("PrototypeEnabled = true", rig);
        }

        [Test]
        public void TheSpectatorCameraInstallsTheOutlineItOnceMissed()
        {
            string spectator = Read("Camera/SpectatorCamera.cs");

            StringAssert.Contains("AddComponent<Visual.WorldOutline>", spectator);
            StringAssert.Contains("PrototypeEnabled = true", spectator);
        }

        /// <summary>
        /// ⚠️⚠️ THE ONE 🧑 REPORTED. The lobby and the map select both draw their arena through
        /// `MapPreviewSurface`, and it carried the grade and not the line, so the preview had the
        /// right COLOUR of the game and none of its shape. `docs/VISION.md` § 6: anything drawn
        /// in a different visual language is the thing that looks broken.
        /// </summary>
        [Test]
        public void TheLobbyAndMapSelectPreviewIsDrawnWithTheSameInkAsTheMatch()
        {
            string preview = Read("UI/MapPreviewSurface.cs");

            StringAssert.Contains("AddComponent<Visual.ColourGrade>", preview);
            StringAssert.Contains("AddComponent<Visual.WorldOutline>", preview);
            StringAssert.Contains("PrototypeEnabled = true", preview);
        }

        /// <summary>
        /// ⚠️ AND `PostAntiAlias` IS DELIBERATELY ABSENT FROM THE PREVIEW, so this asserts the
        /// EXEMPTION rather than leaving it as an accident somebody later "fixes". `CameraRig`
        /// names the reason: the preview renders into a `targetTexture` already built with 4
        /// samples, so filtering it would soften a picture that is not aliased.
        ///
        /// ⚠️⚠️ WRITING THE EXEMPTION DOWN IS THE POINT. `docs/TODO.md` § 126.13 is an entry
        /// about three copies of a local exemption that was never encoded anywhere a test could
        /// see it, and this is the same shape of thing caught before it becomes that.
        /// </summary>
        [Test]
        public void ThePreviewDeliberatelySkipsAntiAliasingBecauseItsTargetIsAlreadyMultisampled()
        {
            string preview = Read("UI/MapPreviewSurface.cs");

            StringAssert.DoesNotContain("AddComponent<Visual.PostAntiAlias>", preview);
        }
    }
}
