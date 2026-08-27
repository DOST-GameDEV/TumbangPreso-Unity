using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace TumbangPreso.UI
{
    /// <summary>
    /// The build stamp every screen carries in the corner.
    ///
    /// ⚠️ IT IS READ FROM THE PROJECT, NOT TYPED. The Godot build prints the version from
    /// `application/config/version`, and a hand-typed copy is wrong the first time somebody
    /// bumps one and not the other. Screenshots get sent to sponsors with that number on them.
    ///
    /// ⚠⚠ AND IT IS `GameVersion.ApplyTo` NOW, NOT `"v" + Application.version`. On any branch
    /// other than `main` the stamp is the BRANCH NAME, because a version number is bumped per
    /// change rather than per branch and could not tell two branches in flight apart. `ApplyTo`
    /// also sizes the box, which matters here and did not before: this label is baked into every
    /// converted menu scene at the 132 px an authored "v4.72" needed. `BuildBranch` has the rule.
    /// </summary>
    public sealed class VersionStamp : MonoBehaviour
    {
        private void Start() => GameVersion.ApplyTo(GetComponent<Text>());
    }
}
