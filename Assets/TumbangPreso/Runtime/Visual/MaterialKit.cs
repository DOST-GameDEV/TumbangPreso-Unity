using UnityEngine;

namespace TumbangPreso.Visual
{
    /// <summary>
    /// The one material anything built from code renders with.
    ///
    /// ⚠️⚠️ A MeshRenderer ADDED IN CODE HAS NO MATERIAL, AND UNITY DRAWS THAT IN BRIGHT PINK.
    /// The first-person arms and the slipper in the viewmodel hand were built exactly that way,
    /// so the thing sitting in the middle of the bottom of the screen for the entire match was
    /// a magenta error blob. It also silently breaks every MaterialPropertyBlock written at it:
    /// a block sets a property ON a material, and with none there the tint is discarded.
    ///
    /// ⚠️ AND THE SHADER IS RESOLVED, NOT ASSUMED. The project carries the URP package but
    /// renders on the built-in pipeline, so "Standard" is right today and "Universal Render
    /// Pipeline/Lit" becomes right the moment a pipeline asset is assigned. Asking for whichever
    /// exists costs one lookup at startup and survives that switch.
    /// </summary>
    public static class MaterialKit
    {
        private static Material _lit;

        public static Material Lit
        {
            get
            {
                if (_lit != null) return _lit;

                var shader = Shader.Find("Universal Render Pipeline/Lit")
                             ?? Shader.Find("Standard")
                             ?? Shader.Find("Diffuse");

                _lit = new Material(shader) { name = "RuntimeLit" };
                return _lit;
            }
        }

        /// <summary>Gives a renderer the shared material, so a property block has something
        /// to write to.</summary>
        public static void Dress(Renderer renderer, Color tint)
        {
            if (renderer == null) return;

            renderer.sharedMaterial = Lit;

            var block = new MaterialPropertyBlock();
            renderer.GetPropertyBlock(block);
            block.SetColor("_Color", tint);
            block.SetColor("_BaseColor", tint);
            renderer.SetPropertyBlock(block);
        }
    }
}
