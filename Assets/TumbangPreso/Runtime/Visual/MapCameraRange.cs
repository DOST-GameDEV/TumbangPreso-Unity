using UnityEngine;

namespace TumbangPreso.Visual
{
    /// <summary>
    /// How far a map's cameras must see, left in the scene by the map's builder for the cameras
    /// to find, as <see cref="MapGrade"/> is for the grade. A MAP WITH NONE OF THESE IS NOT
    /// TOUCHED: <see cref="Adopt"/> finds nothing and returns, and the camera keeps the far plane
    /// its own Awake gave it (240 m on the game camera, 400 m on a spectator's).
    ///
    /// WHY IT EXISTS (the Arena, 2026-10-05). Every other map fits inside the game camera's
    /// 240 m. The Arena is a stadium 480 m across floating over a city: the upper stands are
    /// 180 m from the can, the towers 440 to 900 m, the city floor 760 m down.
    ///
    /// ⚠️ TWO DISTANCES, BECAUSE THE FAR PLANE IS NOT FREE. `WorldOutline`'s edges and ambient
    /// occlusion read `_CameraDepthNormalsTexture`, which holds depth in 16 bits spread EVENLY
    /// over the frustum: its step is far / 65536 at every distance. 240 m is 4 mm a step; 1300 m
    /// is 2 cm; 2500 m is 3.8 cm, more than the occlusion pass's 3 cm bias, and enough to speckle
    /// an edge within about 2 m of the lens. So:
    ///   * <see cref="PlayFar"/> is for a camera that stays where the players are (the game
    ///     camera). It is set to what can be SEEN from there and no more;
    ///   * <see cref="FreeFar"/> is for a camera that may leave (a spectator's free camera), and
    ///     takes in the whole map.
    ///
    /// ⚠️ THE INK'S DISTANCES. `WorldOutline` fades its edges between the fog's start and end,
    /// so an edge is gone where the haze has taken the thing. This map's fog runs kilometres
    /// out so the city stays visible, which would ink every seat row of every stand. A map may
    /// name the ink's own distances here; negative leaves them to the fog.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class MapCameraRange : MonoBehaviour
    {
        /// <summary>The far plane of a camera that stays on the play area, metres.</summary>
        public float PlayFar = 1300.0f;

        /// <summary>The far plane of a camera that may fly anywhere on the map, metres.</summary>
        public float FreeFar = 2500.0f;

        /// <summary>Where `WorldOutline`'s edges begin to fade and where they are gone, metres
        /// from the eye. Negative: the fog's own distances, as on every other map.</summary>
        public float InkFadeStart = -1.0f, InkFadeEnd = -1.0f;

        /// <summary>
        /// Give the camera this map's range, if the map names one. Called from a camera's `Start`,
        /// by which time the scene's own objects exist (the reason `ColourGrade.AdoptFromScene` is
        /// called there too). The far plane is only ever pushed OUT, never pulled in.
        /// </summary>
        public static bool Adopt(Camera camera, bool free)
        {
            if (camera == null) return false;

            var range = FindFirstObjectByType<MapCameraRange>(FindObjectsInactive.Include);
            if (range == null) return false;

            camera.farClipPlane = Mathf.Max(camera.farClipPlane, free ? range.FreeFar : range.PlayFar);
            if (range.InkFadeStart >= 0.0f && range.InkFadeEnd > range.InkFadeStart)
            {
                var outline = camera.GetComponent<WorldOutline>();
                if (outline != null) outline.SetFade(range.InkFadeStart, range.InkFadeEnd);
            }

            return true;
        }
    }
}
