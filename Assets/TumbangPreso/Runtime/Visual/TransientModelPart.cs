using UnityEngine;

namespace TumbangPreso.Visual
{
    /// <summary>
    /// Marks something parented into a character's model for a while that is not the model itself: the taya's
    /// headband and its tails (`TayaIdentity`), a hand anchor an effect adds and later removes. The replay recorder
    /// leaves it out of the model's fingerprint and pose tracks (`CameraSystem.MatchPoseHistory.StableTransforms`),
    /// as it does `DanteMountainPiece`.
    /// ⚠️ Without it the headband, which moves to the next taya every round, broke every replay from round 2 on:
    /// destroying it under a recorded seat emptied that seat's pose history for the rest of the round (no tag replay,
    /// no highlight, "Player pose history is incomplete" for saved replays), and the halftime highlight of the round-1
    /// taya was refused as "Changed recorded art" once the band had moved on.
    /// </summary>
    public sealed class TransientModelPart : MonoBehaviour { }
}
