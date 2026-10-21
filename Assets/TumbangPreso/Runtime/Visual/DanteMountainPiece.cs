using UnityEngine;

namespace TumbangPreso.Visual
{
    /// <summary>
    /// Marks one plate or column of Dante's MOUNTAIN FORM armour (`DanteMountainArmour`), which is parented to his bones for
    /// the 30 s of the form. It is a temporary look, not his model, so the replay recorder leaves it out of the model's
    /// fingerprint and pose tracks (`CameraSystem.MatchPoseHistory.StableTransforms`), as it does Carapace and Ignition.
    /// ⚠️ Without it a halftime highlight recorded before he cast the form was refused at playback as "Changed recorded art"
    /// (owner 2026-10-21: *"the halftime replay aint working again"*), and one recorded during it the same once it shed.
    /// </summary>
    public sealed class DanteMountainPiece : MonoBehaviour { }
}
