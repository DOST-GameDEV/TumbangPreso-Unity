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

        /// <summary>
        /// ⚠️ ONE BLOCK, REUSED, BECAUSE A MaterialPropertyBlock IS A WRITE BUFFER RATHER THAN A
        /// STATE. `GetPropertyBlock` overwrites its whole contents from the renderer and
        /// `SetPropertyBlock` copies it into the renderer, so nothing survives the call and there
        /// is nothing for a second caller to disturb. `CharacterVisual` already holds one for the
        /// same reason. A fresh one per call was allocating on a path that runs once per
        /// accessory: `ViewmodelArms.ApplyCharacterStyle` dresses the two arms plus about nine
        /// accessories between them on every character change, so eleven blocks a pick.
        /// </summary>
        private static readonly MaterialPropertyBlock Block = new MaterialPropertyBlock();

        /// <summary>
        /// ⚠️ BOTH SPELLINGS, FOR THE REASON `CharacterVisual.ColourIds` STATES AT LENGTH: a
        /// block writes a NAMED property and is silently discarded when the shader has none by
        /// that name. `_Color` is the built-in pipeline's albedo and `_BaseColor` is URP's, and
        /// this project can be rendering on either.
        /// </summary>
        private static readonly int ColorId = Shader.PropertyToID("_Color");
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

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
        /// <summary>
        /// ⚠️⚠️ ONE MATERIAL PER SUBMESH, NOT ONE MATERIAL. A renderer draws submesh `i` with
        /// `sharedMaterials[i]` and simply DOES NOT DRAW the submeshes past the end of that
        /// array, silently. `renderer.sharedMaterial = x` sets an array of length one, so every
        /// multi-surface mesh in the game was rendering its first surface and nothing else.
        ///
        /// It was worst on the thing the local player looks at constantly.
        /// `viewmodel_arm.obj` is two `usemtl` groups — `skin` at y 0.62..0.84, which is the
        /// FIST, and `skin_shade` at y 0.00..0.62, which is the FOREARM — and `.obj` orders the
        /// fist first. So the arm drew as a hand with nothing under it, hanging in mid-air with
        /// clear road between it and the bottom of the screen. Reported exactly as *"the hands
        /// are just floating"*, and measured against the original's own frame of the same pose:
        /// the arm ended 88 px short of where Godot's does on a 1080-tall capture, while its top
        /// edge matched to within 8 px. `ViewmodelArms.tscn` says the same thing from the other
        /// side by setting `surface_material_override/0` AND `/1` on both arms.
        ///
        /// ⚠️ `ToonSkin.Apply` CANNOT FIX IT AFTERWARDS. It maps whatever array it is handed one
        /// for one, so a length-one array in is a length-one array out. The count has to be
        /// right here, at the only place a material is first assigned.
        /// </summary>
        public static void Dress(Renderer renderer, Color tint)
        {
            if (renderer == null) return;

            int surfaces = SubMeshCount(renderer);

            if (surfaces <= 1)
            {
                renderer.sharedMaterial = Lit;
            }
            else
            {
                var slots = new Material[surfaces];
                for (int i = 0; i < surfaces; i++) slots[i] = Lit;
                renderer.sharedMaterials = slots;
            }

            // ⚠️ CLEARED BEFORE IT IS FILLED, AND THAT IS THE PRICE OF SHARING ONE. Nothing here
            // documents whether `GetPropertyBlock` empties its destination first, and a renderer
            // that has no block at all may leave it exactly as it was. Both properties below are
            // written unconditionally so today nothing could bleed, but a third property added on
            // one branch and not another would carry the LAST renderer's value onto this one,
            // which is a tint that follows an accessory around and is not traceable to this line.
            Block.Clear();
            renderer.GetPropertyBlock(Block);
            Block.SetColor(ColorId, tint);
            Block.SetColor(BaseColorId, tint);
            renderer.SetPropertyBlock(Block);
        }

        /// <summary>How many surfaces this renderer's mesh actually has. 1 when there is no mesh
        /// to ask, which is the safe answer: a renderer with no mesh draws nothing either
        /// way.</summary>
        private static int SubMeshCount(Renderer renderer)
        {
            if (renderer is SkinnedMeshRenderer skinned)
                return skinned.sharedMesh != null ? skinned.sharedMesh.subMeshCount : 1;

            var filter = renderer.GetComponent<MeshFilter>();
            return filter != null && filter.sharedMesh != null
                ? filter.sharedMesh.subMeshCount
                : 1;
        }
    }
}
