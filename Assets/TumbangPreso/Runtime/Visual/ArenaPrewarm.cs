using System.Collections;
using UnityEngine;

namespace TumbangPreso.Visual
{
    /// <summary>
    /// Draws a freshly loaded arena from several viewpoints, offscreen, while the loading curtain
    /// is still up, so the first frames the player actually sees are not the frames that compile
    /// shaders and upload meshes and textures.
    ///
    /// ⚠️⚠️ WHY RENDERING AND NOT ONLY THE VARIANT COLLECTION. Boot already warms
    /// `Resources/ShaderWarmup` a slice per frame, and that is still worth doing. But a
    /// `ShaderVariantCollection` warms each variant against a dummy vertex layout and a dummy
    /// render target. Metal, D3D12 and Vulkan build the real pipeline state on the first draw
    /// with the real mesh layout, blend state and target format, and that is the hitch a player
    /// feels on the first turn of the camera. The only way to pay it early is to draw the real
    /// scene through the real camera, with its real effects (`WorldOutline`, `ColourGrade`, the
    /// ambient occlusion), into a target of the same format. Owner, 2026-09-27: "make optimized
    /// loading so every shader and every shit will render and load in the loading screen".
    ///
    /// ⚠️ ONE VIEW PER FRAME. The loading screen's own animation and percentage run on the same
    /// main thread, so a burst of twenty renders in one frame would freeze the one thing telling
    /// the player that work is happening.
    ///
    /// ⚠️ THE CAMERA IS PUT BACK EXACTLY. Its pose, target and aspect are restored after every
    /// view, and `CameraRig` places it again on its next LateUpdate anyway.
    /// </summary>
    public static class ArenaPrewarm
    {
        /// <summary>How many offscreen views <see cref="Run"/> draws. Used for progress.</summary>
        public const int Views = 20;

        /// <param name="progress">Called with 0..1 after each view.</param>
        public static IEnumerator Run(System.Action<float> progress)
        {
            var camera = FindCamera();
            if (camera == null) { progress?.Invoke(1); yield break; }

            float floor = WorldLookPresentation.Current != null ? WorldLookPresentation.Current.Floor : 0f;
            var centre = new Vector3(0, floor, 0);

            // ⚠️ THE TARGET MATCHES WHAT THE CAMERA DRAWS INTO ON SCREEN: HDR when the camera is
            // HDR and the project's MSAA count, because a pipeline state is built per target format
            // and sample count, and a view warmed into the wrong format warms nothing the player uses.
            int width = Mathf.Max(320, Screen.width / 2), height = Mathf.Max(180, Screen.height / 2);
            var format = camera.allowHDR ? RenderTextureFormat.DefaultHDR : RenderTextureFormat.Default;
            var target = new RenderTexture(width, height, 24, format)
            {
                name = "ArenaPrewarm",
                antiAliasing = camera.allowMSAA ? Mathf.Max(1, QualitySettings.antiAliasing) : 1,
            };

            var pose = camera.transform;
            Vector3 position = pose.position;
            Quaternion rotation = pose.rotation;
            var previousTarget = camera.targetTexture;
            float aspect = camera.aspect;

            float began = Time.realtimeSinceStartup;
            try
            {
                for (int i = 0; i < Views; i++)
                {
                    if (camera == null) break;
                    Place(camera.transform, i, centre);
                    camera.targetTexture = target;
                    camera.aspect = width / (float)height;
                    camera.Render();
                    camera.targetTexture = previousTarget;
                    camera.aspect = aspect;
                    camera.transform.SetPositionAndRotation(position, rotation);
                    progress?.Invoke((i + 1) / (float)Views);
                    yield return null;
                }
            }
            finally
            {
                if (camera != null)
                {
                    camera.targetTexture = previousTarget;
                    camera.aspect = aspect;
                    camera.transform.SetPositionAndRotation(position, rotation);
                }
                target.Release();
                Object.Destroy(target);
                Debug.Log($"[ArenaPrewarm] drew {Views} views in {Time.realtimeSinceStartup - began:F2} s.");
            }
        }

        /// <summary>
        /// The views: eight directions from the court's centre at eye height (what a player turns
        /// to see in the first seconds), eight from the corners of the street looking back in and
        /// out, and four high views that take in the far buildings, the sky and the clouds.
        /// </summary>
        private static void Place(Transform pose, int index, Vector3 centre)
        {
            if (index < 8)
            {
                pose.SetPositionAndRotation(centre + Vector3.up * 1.6f, Quaternion.Euler(6, index * 45f, 0));
                return;
            }
            if (index < 16)
            {
                int k = index - 8;
                float angle = (k / 2) * 90f + 45f;
                var corner = centre + Quaternion.Euler(0, angle, 0) * Vector3.forward * 11f + Vector3.up * 1.6f;
                float yaw = angle + (k % 2 == 0 ? 180f : 0f);
                pose.SetPositionAndRotation(corner, Quaternion.Euler(4, yaw, 0));
                return;
            }
            float around = (index - 16) * 90f;
            var high = centre + Quaternion.Euler(0, around, 0) * Vector3.back * 16f + Vector3.up * 9f;
            pose.SetPositionAndRotation(high, Quaternion.LookRotation(centre - high));
        }

        private static Camera FindCamera()
        {
            var main = Camera.main;
            if (main != null && main.isActiveAndEnabled && main.targetTexture == null) return main;
            foreach (var camera in Camera.allCameras)
                if (camera.targetTexture == null) return camera;
            return null;
        }
    }
}
