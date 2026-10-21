using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using TumbangPreso.CameraSystem;
using TumbangPreso.Visual;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace TumbangPreso.PlayTests
{
    public sealed class RecordedFrameCompositionTests
    {
        private bool _bots, _spectator, _pinned;
        private int _seat;
        private Core.CustomRules _rules;
        [UnitySetUp] public IEnumerator Before()
        {
            _bots = GameLaunch.AllBots; _spectator = GameLaunch.Spectator; _seat = GameLaunch.SoloSeat;
            _pinned = UI.SceneFlow.RulesPinned; _rules = UI.SceneFlow.SelectedRules.Clone();
            yield return PlayModeWorld.Reset();
        }
        [UnityTearDown] public IEnumerator After()
        {
            yield return PlayModeWorld.Reset();
            GameLaunch.AllBots = _bots; GameLaunch.Spectator = _spectator; GameLaunch.SoloSeat = _seat;
            UI.SceneFlow.AdoptRemoteRules(_rules);
            if (_pinned) UI.SceneFlow.PinSelectedRules(_rules); else UI.SceneFlow.UnpinSelectedRules();
        }
        [UnityTest] public IEnumerator TransparentCameraAlphaCannotRevealTheLiveView() => Compose(0);
        [UnityTest] public IEnumerator PartialCameraAlphaCannotMixTheLiveViewIntoPlayback() => Compose(.25f);
        [UnityTest] public IEnumerator OpaqueCameraInputAndIntentionalUiFadeStillCompose() => Compose(1);

        private IEnumerator Compose(float sourceAlpha)
        {
            yield return MapRetrievalProbe.Load("Eskinita");
            Assert.AreNotEqual(UnityEngine.Rendering.GraphicsDeviceType.Null, SystemInfo.graphicsDeviceType);
            var actor = GameServices.Round.PlayerAt(0);
            RecordedObjectTrack Track(GameObject model, RecordedObjectKind kind, int seat, int skin, string person)
            {
                var history = new MatchPoseHistory.Track(actor, model); history.Record(10); history.Record(11);
                return new RecordedObjectTrack { Kind = kind, Seat = seat, Skin = skin, Person = person,
                    VisualKey = MatchReplayArchive.VisualKey(model), Pose = history.Retain(10, 11) };
            }
            var clip = new RecordedMatchClip { MatchId = 1, Id = 1, Round = 1, Actor = 0, Subject = -1,
                Mode = actor.Mode, Map = "Eskinita", Start = 10, End = 11, Contact = 10.5f, Reason = "Frame composition",
                Objects = new[] { Track(actor.GetComponent<CharacterVisual>().Model, RecordedObjectKind.Player,
                    0, actor.CharacterIndex, Core.Roster.PersonIdAt(actor.Mode, actor.CharacterIndex)),
                    Track(MatchReplayArchive.PropModel(GameServices.Round.Lata.gameObject), RecordedObjectKind.Can,
                        -1, GameServices.Round.Lata.SkinIndex, "") } };
            var owner = new GameObject("Recorded frame composition owner");
            var cameraRoot = new GameObject("Recorded composition UI camera", typeof(Camera));
            var source = new Texture2D(2, 2, TextureFormat.RGBA32, false, true);
            var target = new RenderTexture(128, 128, 24); Texture2D read = null;
            Material owned = null; bool hadOwnedMaterial = false;
            try
            {
                using (var view = new RecordedWorldView(owner.transform, clip))
                {
                    Assert.IsTrue(view.Ready, view.UnavailableReason);
                    var canvas = (Canvas)typeof(RecordedWorldView).GetField("_canvas",
                        BindingFlags.Instance | BindingFlags.NonPublic).GetValue(view); Assert.IsNotNull(canvas);
                    var picture = canvas.GetComponentsInChildren<RawImage>(true).Single();
                    owned = picture.material != picture.defaultMaterial ? picture.material : null;
                    hadOwnedMaterial = owned != null;
                    foreach (Transform child in canvas.transform)
                        if (child != picture.transform) child.gameObject.SetActive(false);
                    var camera = cameraRoot.GetComponent<Camera>(); camera.enabled = false;
                    camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = Color.red;
                    camera.cullingMask = 1 << 31; camera.targetTexture = target;
                    canvas.GetComponent<CanvasScaler>().enabled = false;
                    canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = camera; canvas.planeDistance = 1;
                    foreach (var child in canvas.GetComponentsInChildren<Transform>(true)) child.gameObject.layer = 31;
                    source.SetPixels(Enumerable.Repeat(new Color(0, 0, 1, sourceAlpha), 4).ToArray()); source.Apply();
                    picture.texture = source; picture.color = Color.white;
                    Canvas.ForceUpdateCanvases(); camera.Render(); read = Read(target);
                    var full = read.GetPixel(64, 64); TestContext.WriteLine($"camera alpha={sourceAlpha}; full={full}");
                    Assert.Greater(full.b, .9f, "The recorded RGB frame must remain visible independently of camera alpha.");
                    Assert.Less(full.r, .05f, "The present-time red view leaked into the recorded frame.");
                    Object.DestroyImmediate(read); read = null;
                    picture.color = new Color(1, 1, 1, .5f);
                    Canvas.ForceUpdateCanvases(); camera.Render(); read = Read(target);
                    var fade = read.GetPixel(64, 64); TestContext.WriteLine($"intentional UI fade={fade}");
                    Assert.Greater(fade.r, .4f); Assert.Greater(fade.b, .4f);
                    Assert.Less(Mathf.Abs(fade.r - fade.b), .1f);
                    if (hadOwnedMaterial) Assert.That(fade.a, Is.EqualTo(1).Within(.01f));
                }
                yield return null;
                if (hadOwnedMaterial) Assert.IsTrue(owned == null, "Playback must release its owned frame material.");
            }
            finally
            {
                cameraRoot.GetComponent<Camera>().targetTexture = null;
                if (read != null) Object.DestroyImmediate(read);
                Object.DestroyImmediate(owner); Object.DestroyImmediate(cameraRoot); Object.DestroyImmediate(source);
                target.Release(); Object.DestroyImmediate(target);
            }
        }
        private static Texture2D Read(RenderTexture target)
        {
            var previous = RenderTexture.active;
            try
            {
                RenderTexture.active = target; var image = new Texture2D(128, 128, TextureFormat.RGBA32, false);
                image.ReadPixels(new Rect(0, 0, 128, 128), 0, 0); image.Apply(); return image;
            }
            finally { RenderTexture.active = previous; }
        }
    }
}
