using UnityEngine;

namespace TumbangPreso.Visual
{
    /// <summary>
    /// Editor-baked mathematical clips for one person-rig hierarchy.
    ///
    /// ⚠️ ONE SET PER RIG PATH. The skeleton's seven bone names are shared, but the first node
    /// under the Animator is model-specific and is part of every curve binding. A shared clip
    /// authored for `character-male-f/root` silently moves nothing on `character-female-f`.
    /// </summary>
    public sealed class GeneratedAnimationSet : ScriptableObject
    {
        public AnimationClip[] Clips;
    }
}
