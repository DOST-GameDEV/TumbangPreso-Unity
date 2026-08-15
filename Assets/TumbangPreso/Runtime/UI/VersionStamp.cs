using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace TumbangPreso.UI
{
    /// <summary>
    /// The version stamp every screen carries in the corner.
    ///
    /// ⚠️ IT IS READ FROM THE PROJECT, NOT TYPED. The Godot build prints the version from
    /// `application/config/version`, and a hand-typed copy is wrong the first time somebody
    /// bumps one and not the other. Screenshots get sent to sponsors with that number on them.
    /// </summary>
    public sealed class VersionStamp : MonoBehaviour
    {
        private void Start()
        {
            var text = GetComponent<Text>();
            if (text != null) text.text = "v" + Application.version;
        }
    }
}
