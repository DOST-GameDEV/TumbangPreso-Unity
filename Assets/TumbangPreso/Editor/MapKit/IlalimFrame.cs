using UnityEngine;

namespace TumbangPreso.EditorTools.MapKit
{
    /// <summary>
    /// The Ilalim rebuild's two frames, in one place.
    ///
    /// The Blender kits, and every hard-coded number in the Ilalim authors, are written in the
    /// BLENDER FRAME: x east, z north, y up, origin on Taft under the viaduct, where the court
    /// used to be. The owner moved the court into the campus lot west of the road (2026-10-04:
    /// "can we move the play area to this open space?"), and the game's rules need the can at the
    /// world origin (`Confinement` is a square about (0, 0); `MatchInstaller.MeasureWalls` measures
    /// from "the centre spot"). So the court's code did not move: THE WORLD DID. The export
    /// (tools/export_ilalim_unity.py, GAME_ORIGIN) subtracts the new court's centre from every
    /// placement, collider, anchor and pier, and the lot's height from every height, so the lot is
    /// y = 0 and the can stands at (0, 0, 0).
    ///
    /// ⚠️ Anything read from `ilalim_layout.json`, or measured from the scene, is ALREADY in the game
    /// frame. Only a number TYPED into an author is in the Blender frame: put it through `W`.
    /// </summary>
    public static class IlalimFrame
    {
        /// <summary>The new court's centre in the Blender frame: the lot beside Taft, on the lot's
        /// surface (author_ilalim_street.py LOT_TOP). tools/export_ilalim_unity.py carries the
        /// same three numbers as GAME_ORIGIN; change them together.</summary>
        public const float OriginX = -23.0f, OriginY = 0.24f, OriginZ = 14.2f;

        public static Vector3 Shift => new Vector3(OriginX, OriginY, OriginZ);

        /// <summary>Blender frame to game frame.</summary>
        public static Vector3 W(float x, float y, float z) => new Vector3(x - OriginX, y - OriginY, z - OriginZ);
        public static Vector3 W(Vector3 blenderFrame) => blenderFrame - Shift;

        /// <summary>Taft's centreline, and the viaduct's, in the game frame.</summary>
        public const float RoadX = -OriginX;

        /// <summary>Where a player may go, in the game frame: the lot (its west fence at Blender
        /// x -35, its south fence at z 3.4, the Padre Faura fence, which slants from z 24.07 to 25.19, so the wall is at 24.0), across Taft, to the
        /// shop fronts at Blender x +11.</summary>
        public const float PlayMinX = -12f, PlayMaxX = 34f, PlayMinZ = -10.8f, PlayMaxZ = 9.8f;
    }
}
