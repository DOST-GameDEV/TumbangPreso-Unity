using UnityEngine;

namespace TumbangPreso.Visual
{
    /// <summary>
    /// Sits on a water surface whose shader reads `_CameraDepthTexture` (the Lagoon Cove sample
    /// map's TumbangPreso/LagoonCoveWater) and makes sure whichever camera draws it renders one.
    ///
    /// ⚠️ WHY THIS EXISTS. In the built-in pipeline a camera only renders the depth texture when
    /// something asks for it (`Camera.depthTextureMode`). Forward rendering with no shadows
    /// or post effect asking leaves it unset, and the water then reads an empty texture: its
    /// shoreline foam and its shallow-to-deep colour, both measured as the distance between
    /// the water surface and the seabed behind it, collapse to one flat colour everywhere. The
    /// match camera, the spectator rigs and the emote camera are built in different places, so
    /// the request lives on the WATER instead of on every camera: `OnWillRenderObject` runs
    /// once per camera that is about to draw this renderer, which is exactly the set of
    /// cameras that need the texture.
    ///
    /// ⚠️ It only ever ADDS the Depth flag (a bitwise OR), so a camera that already renders
    /// depth-normals or motion vectors keeps them.
    /// </summary>
    [RequireComponent(typeof(Renderer))]
    public sealed class WaterDepthRequest : MonoBehaviour
    {
        private void OnWillRenderObject()
        {
            var camera = Camera.current;
            if (camera != null && (camera.depthTextureMode & DepthTextureMode.Depth) == 0)
                camera.depthTextureMode |= DepthTextureMode.Depth;
        }
    }
}
