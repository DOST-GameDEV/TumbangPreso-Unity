using UnityEngine;

namespace TumbangPreso.Visual
{
    /// <summary>
    /// THE CIRCLE IN THE SKY OVER HER DOLL, redrawn for direction A of her kit (`PhaisterLook`). Rule: VOODOO DOLL opens this
    /// over the doll, it hangs open for `VoodooSkyCircle.OpenSeconds` and shuts by 3.8 s; it does nothing to anyone. What
    /// the player must read from it, across the court: something of hers has just been let in, HERE, and it came down out of
    /// that hole. So the fall of light and the moths land on the doll, and the hole is the brightest and darkest thing.
    ///
    /// The owner's references for circles (`Logs/coordinator/refs/hex`): fine lines, a star polygon, nested rings turning
    /// against each other, dense small glyphs, and ref1's dark smoke with a wound of magenta round a black hole. Moons and
    /// crosses were rejected, so the script is built from moth parts (`tools/paint_phaister_seal.py`).
    ///
    /// | Age | What |
    /// |---|---|
    /// | 0 to 0.55 | the outer ring writes itself round, smoke gathers behind it |
    /// | 0.3 to 0.8 | the star strikes, the wings and inner script write after it |
    /// | 0.58 | the circle flashes as it closes (no bloom: this is the brightness and the halo) |
    /// | 0.5 to 1 | the lens in the middle opens into a well, its own light down inside |
    /// | 0.7 on | moths pour out and spiral down the fall of light into the doll |
    /// | shut | the hole closes, the lines are eaten from their edges, the smoke goes last |
    ///
    /// Static meshes moved in the vertex program from one clock; a pure function of what it is handed.
    /// </summary>
    public sealed class PhaisterSkySeal
    {
        private const int MothCount = 16;
        private static Mesh _moths;
        // How far each layer hangs under the circle (m at full size): the hole deepest, so the layers slide past it.
        private static readonly float[] Lift = { 0.55f, 0f, -0.28f, -0.55f };
        private static readonly Vector4[] Chan = { Vector4.zero, new Vector4(1, 0, 0, 0), new Vector4(0, 1, 0, 0), new Vector4(0, 0, 1, 0) };
        // Each layer's turn against the circle's own (radians a second): outer with it, the star back, the inner quick.
        private static readonly float[] Rate = { 0f, 0.13f, -0.21f, 0.40f };

        private readonly GameObject _root;
        private readonly Material[] _layer = new Material[4];
        private readonly Material _shaft, _pour;

        public PhaisterSkySeal(Transform parent, int layer)
        {
            _root = new GameObject("PhaisterSkySeal") { hideFlags = HideFlags.DontSave };
            if (parent != null) _root.transform.SetParent(parent, false);
            var seal = FxStudyKit.Mask("phaister_seal");
            for (int i = 0; i < 4; i++)
            {
                var go = PhaisterLook.Draw("Seal" + i, _root.transform, PhaisterLook.Quad);
                go.layer = layer;
                var m = PhaisterLook.Paint(go, "PhaisterSeal");
                if (m == null) continue;
                m.SetTexture("_SealTex", seal);
                m.SetFloat("_Mode", i == 0 ? 1f : 0f);
                m.SetVector("_Chan", Chan[i]);
                m.renderQueue = 3020 + i;
                _layer[i] = m;
            }
            var shaft = PhaisterLook.Draw("SealFall", _root.transform, PhaisterLook.Shell);
            shaft.layer = layer;
            _shaft = PhaisterLook.Paint(shaft, "PhaisterSeal");
            if (_shaft != null) { _shaft.SetFloat("_Mode", 2f); _shaft.renderQueue = 3019; }

            if (_moths == null) _moths = PhaisterDollLook.MothMesh("PhaisterSealMoths", MothCount, 0.95f, 1.30f, 301);
            var pour = PhaisterLook.Draw("SealMoths", _root.transform, _moths);
            pour.layer = layer;
            _pour = PhaisterLook.Paint(pour, "PhaisterDollMoths");
            if (_pour != null) _pour.SetFloat("_Mode", 0f);
        }

        /// <summary>Poses it. `feet` is the ground the fall of light lands on (the doll); `opening` 0 shut to 1 wide, negative for
        /// the circle's own timing; `size` is the caller's shrink (1 full, toward 0 shutting), which eats it rather than scales it.</summary>
        public void Pose(float age, Vector3 centre, Vector3 feet, float size, float spin, float alpha, float opening, float burst, float detail)
        {
            bool on = age > 0f && alpha > 0.01f;
            if (_root.activeSelf != on) _root.SetActive(on);
            if (!on) return;
            float gone = 1f - Mathf.Clamp01(size);
            float write0 = FxStudyKit.Ease(FxStudyKit.Run(age, 0f, 0.55f));
            float write1 = FxStudyKit.Ease(FxStudyKit.Run(age, 0.30f, 0.80f));
            float write2 = FxStudyKit.Ease(FxStudyKit.Run(age, 0.42f, 0.92f));
            float flash = Mathf.Exp(-Mathf.Pow((age - 0.58f) / 0.11f, 2f));
            float burstGlow = burst >= 0f ? Mathf.Exp(-burst * 5f) : 0f;
            float bright = 1f + 1.1f * flash + 1.6f * burstGlow + 0.06f * Mathf.Sin(age * 5f);
            float open = opening >= 0f ? opening : FxStudyKit.Ease(FxStudyKit.Run(age, 0.50f, 1.0f));
            float radius = SkyCircle.Radius * Mathf.Lerp(0.93f, 1f, FxStudyKit.EaseOut(FxStudyKit.Run(age, 0f, 0.8f))) * Mathf.Lerp(1f, 0.88f, gone);

            for (int i = 0; i < 4; i++)
            {
                var m = _layer[i];
                if (m == null) continue;
                m.SetVector("_Centre", centre + Vector3.up * Lift[i]);
                m.SetFloat("_Size", radius);
                m.SetFloat("_Spin", spin + Rate[i] * age);
                m.SetFloat("_Age", age);
                m.SetFloat("_Write", i == 1 ? write0 : i == 2 ? write1 : write2);
                m.SetFloat("_Bright", bright);
                m.SetFloat("_Erode", i == 0 ? 0f : Mathf.Clamp01(gone * 1.25f));
                // Eaten, and thinning as they are, so no island of line outlives the circle.
                m.SetFloat("_Alpha", (i == 0 ? FxStudyKit.Ease(FxStudyKit.Run(age, 0f, 0.45f)) * (1f - gone) * (1f - gone) : 1f - FxStudyKit.Ease(FxStudyKit.Run(gone, 0.22f, 0.55f))) * alpha);
                if (i == 0) m.SetFloat("_Open", open);
            }

            float fall = FxStudyKit.Ease(FxStudyKit.Run(age, 0.72f, 1.25f)) * (1f - gone) * detail;
            if (_shaft != null)
            {
                _shaft.SetVector("_Centre", centre + Vector3.down * 1.1f);
                _shaft.SetVector("_Bottom", new Vector4(feet.x, feet.y + 0.02f, feet.z, 0.95f));
                _shaft.SetFloat("_Size", radius * 0.32f * Mathf.Max(open, 0.05f));
                _shaft.SetFloat("_Age", age);
                _shaft.SetFloat("_Bright", 1f);
                _shaft.SetFloat("_Alpha", fall * alpha);
            }
            if (_pour != null)
            {
                _pour.SetFloat("_Age", age);
                _pour.SetVector("_A", new Vector4(centre.x, centre.y + 0.2f, centre.z, radius * 0.22f));
                _pour.SetVector("_B", new Vector4(feet.x, feet.y + 0.8f, feet.z, 0.6f));
                _pour.SetFloat("_Life", 1.25f);
                _pour.SetFloat("_From", 0.65f);
                // They stop leaving as the hole shuts; those already out finish their fall.
                if (open >= 0.3f || age < 1.2f) _pour.SetFloat("_To", 1000f);
                else if (_pour.GetFloat("_To") > 999f) _pour.SetFloat("_To", age);
                // A detail of 0 is the cutscene: its own close shots of the doll are not to be buried in moths.
                _pour.SetFloat("_Scale", detail > 0f ? 1f : 0f);
            }
        }

        public void Hide() { if (_root.activeSelf) _root.SetActive(false); }

        public void Destroy()
        {
            PhaisterLook.Free(_root);
        }
    }
}
