using UnityEngine;
using UnityEngine.UI;

namespace TumbangPreso
{
    /// <summary>
    /// Build metadata remains available to diagnostics and network compatibility.
    /// The owner removed visible version stamps from all game UI on2026-09-15.
    /// </summary>
    public static class GameVersion
    {
        public static string Value => Application.version;
        public static string DisplayString => string.Empty;

        // Shared by current menus/HUD and the retained authored VersionStamp.
        // Keep the Text reference valid for callers that still position it.
        public static void ApplyTo(Text label)
        {
            if(label==null) return;
            label.text=string.Empty;
            label.raycastTarget=false;
            label.enabled=false;
            label.gameObject.SetActive(false);
        }

        // Retain the existing API for legacy HUD callers, without a visible widget.
        public static Text AttachTo(RectTransform parent,bool over3d=false)
        {
            var go=new GameObject("VersionLabel",typeof(RectTransform),typeof(Text));
            go.transform.SetParent(parent,false);
            var label=go.GetComponent<Text>();
            ApplyTo(label);
            return label;
        }
    }
}
