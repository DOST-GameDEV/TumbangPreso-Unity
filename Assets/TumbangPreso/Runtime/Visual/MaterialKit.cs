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

                // ⚠️⚠️ ASK WHICH PIPELINE IS *RUNNING*, NOT WHICH SHADER *EXISTS*. This project
                // has the URP package installed and NO pipeline asset assigned, so it renders on
                // the built-in pipeline — but `Shader.Find("Universal Render Pipeline/Lit")`
                // still returns that shader, because it is in the project. A URP shader under
                // the built-in pipeline has no matching subshader and draws as the error
                // material, so the first-person arms and the tsinelas in the player's own hand
                // rendered wrong in every build while the lookup "succeeded".
                //
                // `currentRenderPipeline` is null exactly when the built-in pipeline is active,
                // which is the only reliable way to ask.
                bool scriptable = UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline != null;

                var shader = scriptable
                    ? Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard")
                    : Shader.Find("Standard") ?? Shader.Find("Diffuse");

                if (shader == null)
                {
                    Debug.LogError("[Material] no usable lit shader; runtime meshes will be pink.");
                    shader = Shader.Find("Sprites/Default");
                }

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
