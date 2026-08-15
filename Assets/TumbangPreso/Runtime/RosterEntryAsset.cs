using UnityEngine;

namespace TumbangPreso
{
    /// <summary>
    /// The Unity-side half of one roster entry: the mesh, the material, the tint.
    ///
    /// ⚠️⚠️ THE TRAITS ARE NOT HERE AND MUST NOT BE COPIED HERE. They live in
    /// `Roster.cs`, in engine-free C#, where they are unit tested. A designer-editable copy on
    /// a ScriptableObject is the single most tempting change in this whole port and it would
    /// end the verification strategy: the moment the numbers are editable in the Inspector, the
    /// tests are asserting something other than what ships.
    ///
    /// What this holds is everything the tests do not care about, which is the art.
    ///
    /// ⚠️ THE FILE NAME MUST MATCH THE CLASS NAME. Unity resolves a ScriptableObject's script
    /// by file name, and when it cannot, the asset deserialises with a null script and every
    /// field silently reads as default. That looks exactly like "the artist forgot to assign
    /// the model".
    /// </summary>
    [CreateAssetMenu(menuName = "Tumbang Preso/Roster Entry", fileName = "RosterEntry")]
    public sealed class RosterEntryAsset : ScriptableObject
    {
        [Tooltip("Must match the Id in Roster.cs exactly. The link is by id, never by array " +
                 "position, so reordering these assets cannot silently repaint the cast.")]
        public string Id;

        public GameObject Model;
        public Material Material;
        public Color Tint = Color.white;

        /// <summary>
        /// The model's own animation clips, referenced so they survive into a build.
        ///
        /// ⚠️⚠️ WITHOUT THIS REFERENCE THE CLIPS DO NOT SHIP AND NOTHING SAYS SO. The 32 clips
        /// live inside the `.glb`, and `CharacterAnimator` used to find them by asking the
        /// AssetDatabase in the editor and `Resources.FindObjectsOfTypeAll` otherwise. Both
        /// fail: the model is instantiated at runtime so it has no prefab link to trace back to
        /// the asset, and an asset nothing references is stripped from the player entirely. The
        /// result was a whole cast standing perfectly still in the T-pose of their idle frame,
        /// with one warning in a log nobody reads during a match.
        ///
        /// ⚠️ FILLED BY `RosterBookBuilder`, NOT BY HAND. Thirty-two clips times twelve people
        /// is not a list to maintain in the Inspector.
        /// </summary>
        public AnimationClip[] Clips;

        [TextArea(2, 4)]
        public string Tagline;
    }
}
