using UnityEngine;

namespace TumbangPreso.Visual
{
    internal static class ConfettiPaperMaterial
    {
        private static Material _material;
        private static readonly MaterialPropertyBlock Tint = new MaterialPropertyBlock();
        private static readonly int ColorId = Shader.PropertyToID("_Color");
        internal static void SetOpacity(Renderer renderer,float opacity)
        {
            renderer.GetPropertyBlock(Tint);Tint.SetFloat("_Opacity",opacity);renderer.SetPropertyBlock(Tint);
        }

        internal static void Apply(Renderer renderer, Color color)
        {
            if (_material == null)
            {
                var shader = Resources.Load<Shader>("Vfx/ConfettiPaper");
                if (shader == null || !shader.isSupported)
                {
                    VfxMaterial.Solid(renderer, color);
                    return;
                }
                _material = new Material(shader) { name = "ConfettiPaper", hideFlags = HideFlags.DontSave };
            }
            renderer.sharedMaterial = _material;
            Tint.Clear(); Tint.SetColor(ColorId, color); Tint.SetFloat("_Opacity",1);renderer.SetPropertyBlock(Tint);
        }
    }
}
