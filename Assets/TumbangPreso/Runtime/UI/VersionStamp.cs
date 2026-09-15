using UnityEngine;
using UnityEngine.UI;

namespace TumbangPreso.UI
{
    /// <summary>
    /// Suppress the retained authored version label before the first rendered frame
    /// and if an older screen tries to reactivate it. Internal metadata remains available.
    /// </summary>
    public sealed class VersionStamp : MonoBehaviour
    {
        private void Awake() => GameVersion.ApplyTo(GetComponent<Text>());
        private void OnEnable() => GameVersion.ApplyTo(GetComponent<Text>());
    }
}
