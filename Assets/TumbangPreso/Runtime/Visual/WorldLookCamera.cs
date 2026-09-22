using UnityEngine;

namespace TumbangPreso.Visual
{
    // Explicit world-camera context for editor witnesses and other off-screen
    // views. Ordinary portrait/menu cameras do not inherit map shader globals.
    [DisallowMultipleComponent]
    public sealed class WorldLookCamera : MonoBehaviour { }
}
