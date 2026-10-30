using System;
using TumbangPreso.Visual;
using UnityEngine;

namespace TumbangPreso.Map
{
    /// <summary>
    /// PAYO'S BAKED WORLD OCCLUSION, A TEST BEHIND A SWITCH (owner, 2026-10-29: asked whether the world AO can be baked,
    /// then "continue" to a Payo bake test). The screen-space occlusion in `Visual.WorldOutline` was most of Balanced's and
    /// High's frame on the integrated Radeon (Payo Balanced 152 ms with it, about 90 without). The builder step
    /// (Editor/MapKit/PayoSceneBuilder.BakedAO.cs) writes each static vertex's occlusion into UV2 (x the light that
    /// reaches it, y = 1 marks a baked vertex); LagoonPainted, PayoLand, LagoonRock and LagoonFoliage multiply it in
    /// toward the same violet cavity hue the screen pass uses, scaled by the global `_TPBakedAO` (rgb the shade, a the
    /// strength). While this switch is on the global is set and `WorldOutline.BakedWorldAO` stops the screen pass.
    ///
    /// ⚠️ DEFAULT OFF until the owner approves: with it off the global is zero, every baked vertex multiplies by one, and
    /// the screen pass runs as before, so no tier's look changes. `-tp-baked-ao` on the command line (a build benchmark)
    /// or `Test` (the editor's perf probe) turns it on.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PayoBakedAO : MonoBehaviour
    {
        /// <summary>The switch the owner approves. False ships the screen-space occlusion unchanged.</summary>
        public static bool On = true;   // on by default (owner, 2026-10-30: "turn it on by default")
        /// <summary>REVIEW ONLY: 1 forces the bake on, 0 forces it off, -1 (the game) follows `On` and the command line.</summary>
        public static int Test = -1;
        private static readonly bool CommandLine = Array.IndexOf(Environment.GetCommandLineArgs(), "-tp-baked-ao") >= 0;
        private static readonly int GlobalId = Shader.PropertyToID("_TPBakedAO");

        public static bool Active => Test >= 0 ? Test > 0 : On || CommandLine;

        private void Update()
        {
            bool live = Active;
            WorldOutline.BakedWorldAO = live;
            // The screen pass's own numbers: its strength times the look's weight, and the composite's .7 of the cavity hue.
            float strength = 0f;
            Color cavity = Color.white;
            if (live && WorldLookPresentation.Current != null && WorldLookProfile.Current != null)
            {
                strength = WorldLookProfile.Current.AmbientOcclusion * WorldLookPresentation.Current.Weight;
                cavity = WorldLookProfile.Current.CavityHue;
            }
            Shader.SetGlobalVector(GlobalId, new Vector4(cavity.r * .7f, cavity.g * .7f, cavity.b * .7f, strength));
        }

        private void OnDisable()
        {
            WorldOutline.BakedWorldAO = false;
            Shader.SetGlobalVector(GlobalId, Vector4.zero);
        }
    }
}
