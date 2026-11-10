using System;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using TumbangPreso.CameraSystem;
using TumbangPreso.Visual;
using UnityEngine;
using Object = UnityEngine.Object;

namespace TumbangPreso.Tests
{
    public sealed class PickupMotionRenderTests
    {
        private static void Field(object target, string name, object value)
            => target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);

        [Test]
        public void RenderSixContextualPickupAnimations()
        {
            var root = new GameObject("Isolated pickup motion study");
            var actor = new GameObject("Pickup presentation source"); actor.transform.SetParent(root.transform);
            var carrier = actor.AddComponent<Carrier>();
            var source = new GameObject("Pickup street shoe"); source.transform.SetParent(root.transform);
            var shoe = source.AddComponent<Slipper>(); source.transform.position = new Vector3(0, .07f, .65f);
            var art = RosterBook.Load().Slippers[0]; Assert.IsNotNull(art.Model);
            Object.Instantiate(art.Model, source.transform);
            var camera = new GameObject("Pickup review eye").AddComponent<Camera>(); camera.transform.SetParent(root.transform);
            camera.enabled = false; camera.nearClipPlane = .03f; camera.fieldOfView = 85; camera.aspect = 16f / 9;
            camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(.24f, .18f, .12f);
            var handRoot = new GameObject("Pickup review arms"); handRoot.transform.SetParent(camera.transform, false);
            handRoot.transform.localScale = Vector3.one * CameraRig.ViewmodelScale;
            var arms = handRoot.AddComponent<ViewmodelArms>(); arms.EnsureBuilt(); arms.SetCharacter("bayan");
            arms.SetPickupSource(carrier); arms.MatchSkin(shoe);
            var light = new GameObject("Pickup review sunlight").AddComponent<Light>(); light.transform.SetParent(root.transform);
            light.type = LightType.Directional; light.intensity = 1.2f; light.transform.rotation = Quaternion.Euler(45, -30, 0);
            var floor = GameObject.CreatePrimitive(PrimitiveType.Cube); floor.transform.SetParent(root.transform);
            floor.transform.position = new Vector3(0, -.15f, 2); floor.transform.localScale = new Vector3(20, .2f, 20);
            MaterialKit.Dress(floor.GetComponent<Renderer>(), new Color(.48f, .40f, .29f));
            var target = new RenderTexture(768, 432, 24);
            var pixels = new Texture2D(768, 432, TextureFormat.RGB24, false);
            var previous = RenderTexture.active;
            bool reduced = Settings.SettingsStore.Current.ReducedUiMotion;
            var surfaces = source.GetComponentsInChildren<Renderer>();
            try
            {
                Settings.SettingsStore.Current.ReducedUiMotion = false;
                foreach (PickupStyle style in Enum.GetValues(typeof(PickupStyle)))
                {
                    string output = Path.Combine(Environment.GetEnvironmentVariable("TUMP_EVIDENCE") ?? "Logs/feel-pickup-revision-20261107", "variants", style.ToString());
                    Directory.CreateDirectory(output);
                    for (int frame = 0; frame < 28; frame++)
                    {
                        float time = frame / 24f;
                        float duration = PickupMotion.Seconds(style);
                        float left = Mathf.Max(0, duration - time);
                        typeof(Carrier).GetProperty("Held").SetValue(carrier, shoe);
                        Field(carrier, "_pickupStyle", style); Field(carrier, "_tossDuration", duration);
                        Field(carrier, "_tossLeft", left); Field(carrier, "_tossFrom", source.transform.position);
                        Field(carrier, "_tossFromRotation", source.transform.rotation);
                        foreach (var surface in surfaces) surface.forceRenderingOff = true;
                        float bend = carrier.PickupBend;
                        float posture = style == PickupStyle.Sliding ? .65f : style == PickupStyle.Crouched ? .42f : 0;
                        camera.transform.SetPositionAndRotation(new Vector3(0, 1.6f - posture - PickupMotion.EyeDrop(style) * bend, .10f * bend),
                            Quaternion.Euler(PickupMotion.LookDown(style) * bend, 0, 0));
                        arms.SetHolding(true); arms.StepVisuals(1f / 24); arms.MatchSkin(shoe);
                        arms.RefreshPickupPresentation();
                        camera.targetTexture = target; camera.Render(); RenderTexture.active = target;
                        pixels.ReadPixels(new Rect(0, 0, 768, 432), 0, 0); pixels.Apply();
                        File.WriteAllBytes(Path.Combine(output, $"f{frame:D3}.png"), pixels.EncodeToPNG());
                        Assert.IsFalse(float.IsNaN(arms.transform.Find("RightPivot").position.x));
                    }
                }
            }
            finally
            {
                Settings.SettingsStore.Current.ReducedUiMotion = reduced;
                RenderTexture.active = previous; camera.targetTexture = null;
                Object.DestroyImmediate(pixels); Object.DestroyImmediate(target); Object.DestroyImmediate(root);
            }
        }
    }
}
