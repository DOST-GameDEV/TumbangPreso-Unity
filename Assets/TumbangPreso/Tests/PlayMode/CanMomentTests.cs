using System.Collections;
using System.Linq;
using NUnit.Framework;
using TumbangPreso.Core;
using TumbangPreso.UI;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace TumbangPreso.PlayTests
{
    /// <summary>
    /// The can moment (owner, 2026-10-22: *"panget ren can raise animation men di halata"*): knocked, it clatters and CAN
    /// DOWN slams in; the taya's real channel lifts it under the hands; restored, it hops to its mark, slams, rings the
    /// road and a chalk seal draws round it. The frames in Logs/can-moment are the review; the asserts pin the pieces.
    /// </summary>
    public sealed class CanMomentTests
    {
        [UnitySetUp] public IEnumerator Before() => PlayModeWorld.Reset();
        [UnityTearDown] public IEnumerator After() => PlayModeWorld.Reset();

        private static IEnumerator Open()
        {
            SceneFlow.Networked = false; SceneFlow.SetSelectedRules(CustomGameRules.Defaults(GameMode.Classic));
            GameLaunch.SoloSeat = 1; GameLaunch.GuidedTutorial = false;
            yield return SceneManager.LoadSceneAsync(SceneFlow.Eskinita); yield return new WaitForSeconds(.2f);
            foreach (var ai in Object.FindObjectsByType<AIController>()) ai.enabled = false;
            foreach (var input in Object.FindObjectsByType<PlayerInputReader>()) input.enabled = false;
            Object.FindAnyObjectByType<SliceRunner>().Begin(); yield return null;
            foreach (var actor in GameServices.Round.Players) { actor.Intent.Clear(); actor.ClearStun(); actor.ClearTrip(); }
            Hud.Instance.ShowReadyPrompt(false);
        }

        internal sealed class Shot : System.IDisposable
        {
            private readonly Camera _camera; private readonly RenderTexture _target; private readonly Texture2D _image;
            private readonly string _folder;
            public Shot(Vector3 look, Vector3 from, string folder)
            {
                _camera = new GameObject("CanMomentShot").AddComponent<Camera>();
                _camera.CopyFrom(Camera.main); _camera.enabled = false; _camera.tag = "Untagged";
                _target = new RenderTexture(960, 540, 24, RenderTextureFormat.ARGB32);
                _camera.targetTexture = _target; _camera.fieldOfView = 38; _camera.nearClipPlane = .05f;
                _camera.transform.SetPositionAndRotation(from, Quaternion.LookRotation(look - from));
                _image = new Texture2D(960, 540, TextureFormat.RGB24, false);
                _folder = folder; System.IO.Directory.CreateDirectory(folder);
            }
            public void Take(string name)
            {
                _camera.Render();
                var previous = RenderTexture.active; RenderTexture.active = _target;
                _image.ReadPixels(new Rect(0, 0, 960, 540), 0, 0); _image.Apply(); RenderTexture.active = previous;
                System.IO.File.WriteAllBytes(_folder + "/" + name + ".png", _image.EncodeToPNG());
            }
            public void Dispose() { Object.Destroy(_image); Object.Destroy(_camera.gameObject); _target.Release(); Object.Destroy(_target); }
        }

        [UnityTest]
        public IEnumerator KnockedCanClattersAndStampsThenTheRealChannelLiftsAndSealsIt()
        {
            yield return Open();
            var round = GameServices.Round; var can = round.Lata;
            float until = Time.unscaledTime + 4;
            while (can.IsProtected && Time.unscaledTime < until) yield return null;
            Vector3 mark = can.transform.position;
            var taya = round.Players.First(p => p.IsDefender);
            const string folder = "Logs/can-moment";
            // Side on to the can, with the taya (who stands on its near side to lift it) off to the left of frame.
            using (var shot = new Shot(mark + Vector3.up * .3f + Vector3.back * .25f, mark + new Vector3(2.6f, 1.0f, .5f), folder))
            {
                var late = new GameObject("CanMomentLateShot").AddComponent<LateFrameShot>(); late.Shoot = shot.Take;
                yield return late.Take("0-standing");
                int thrower = (taya.PlayerSlot + 1) % 4;
                can.HostKnockDown(thrower);
                Assert.IsFalse(can.IsUpright);
                float start = Time.unscaledTime;
                foreach (float at in new[] { .04f, .12f, .22f, .34f, .48f, .7f })
                {
                    while (Time.unscaledTime - start < at) yield return null;
                    yield return late.Take("1-down-" + at.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture));
                }
                var stamp = GameObject.Find("~CanDownStamp");
                Assert.IsNotNull(stamp, "CAN DOWN must slam in when the can goes over.");
                yield return TumpUiCapture.Capture("CanMoment-can-down-stamp", stamp.GetComponent<Canvas>(), 1280, 720, false, true);

                // The taya's real channel lifts the drawn can under the hands.
                taya.Intent.Parked = false;
                taya.Teleport(can.transform.position + Vector3.back * .7f);
                taya.transform.forward = Vector3.forward;
                yield return null;
                taya.Intent.Set(Verb.Grab, true);
                start = Time.unscaledTime; until = start + 5; int frame = 0; float nextShot = 0;
                bool sawRingOrHop = false;
                // The drawn can's lift through the channel: the angle between its long axis and the vertical, against
                // the channel and the taya's own raise progress, every frame (Logs/can-moment/raise.csv).
                var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
                var meshField = typeof(Lata).GetField("_raiseMesh", flags);
                var toppleTimer = typeof(Lata).GetField("_toppleTimer", flags);
                var toppleAngle = typeof(Lata).GetField("_toppleAngle", flags);
                var tayaAnimator = taya.GetComponent<Visual.CharacterAnimator>();
                var carrier = taya.GetComponent<Carrier>();
                var trace = new System.Text.StringBuilder("time,channel,raise,toppleTimer,toppleAngle,meshTilt" + System.Environment.NewLine);
                float leastTilt = 180;
                // Sampled after every LateUpdate (`LateFrameShot`), so it is the pose actually drawn this frame.
                System.Action sample = () =>
                {
                    if (can.IsUpright) return;
                    var mesh = (Transform)meshField.GetValue(can);
                    float tilt = mesh != null ? Vector3.Angle(mesh.up, Vector3.up) : -1;
                    if (tilt >= 0 && carrier.ChannelRatio > .5f) leastTilt = Mathf.Min(leastTilt, tilt);
                    trace.AppendLine(System.FormattableString.Invariant($"{Time.unscaledTime - start:F3},{carrier.ChannelRatio:F3},{tayaAnimator.ResetRaiseProgress:F3},{(float)toppleTimer.GetValue(can):F3},{(float)toppleAngle.GetValue(can):F1},{tilt:F1}"));
                };
                late.Each = sample;
                try
                {
                    while (!can.IsUpright && Time.unscaledTime < until)
                    {
                        if (Time.unscaledTime - start >= nextShot) { late.Pending = "2-raise-" + frame++.ToString("D2"); nextShot += .12f; }
                        yield return null;
                    }
                }
                finally { late.Each = null; }
                System.IO.File.WriteAllText(folder + "/raise.csv", trace.ToString());
                taya.Intent.Set(Verb.Grab, false);
                Assert.IsTrue(can.IsUpright, "The real channel must stand the can up.");
                Assert.Less(leastTilt, 45f, "Past half the channel the drawn can must be visibly rising under the hands.");
                start = Time.unscaledTime;
                foreach (float at in new[] { .03f, .1f, .18f, .26f, .34f, .45f, .6f, .8f })
                {
                    while (Time.unscaledTime - start < at) yield return null;
                    if (can.transform.Find("~CanSetRing") != null) sawRingOrHop = true;
                    yield return late.Take("3-restore-" + at.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture));
                }
                Assert.IsTrue(sawRingOrHop, "The landing must ring the road.");
                Assert.IsTrue(can.IsProtected);
                Assert.IsNotNull(can.transform.Find("~CanChalkSeal"), "The chalk seal must draw round the protected can.");
            }
        }
    }
}
