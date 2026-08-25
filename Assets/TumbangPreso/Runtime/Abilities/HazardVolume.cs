using UnityEngine;

namespace TumbangPreso.Abilities
{
    /// <summary>
    /// Put this on anything the bots should walk around. One component, one disc.
    ///
    /// ⚠️ THE RADIUS IS THE HAZARD'S GAMEPLAY RADIUS, NOT ITS VISUAL ONE. The extra margin a
    /// body needs is added by the caller at query time, because it belongs to the body.
    ///
    /// ⚠️⚠️ THIS CLASS MUST LIVE IN A FILE CALLED `HazardVolume.cs` AND NOTHING ELSE MAY BE
    /// MOVED INTO IT. It sat inside `HazardMap.cs` until 2026-08-25 and that shipped a build
    /// that HARD CRASHED the moment a player selected Ilalim ng Tulay.
    ///
    /// Unity only binds a MonoBehaviour to a `MonoScript` asset when the class name matches the
    /// file name. `AddComponent&lt;HazardVolume&gt;()` compiles and runs perfectly either way, so
    /// every runtime caster (`HeroHazards`, `StreetTripHazard`) was fine and no test noticed.
    /// But when the component is SAVED INTO A SCENE, Unity has no script asset to point at, so
    /// it writes an inline `MonoScript` stub into the scene file carrying only the class,
    /// namespace and assembly names. The editor tolerates that stub and resolves the type by
    /// name, which is why the whole gate passed. A PLAYER CANNOT. It reads the component with
    /// no layout to read it against, walks off the end of the object, and reports
    /// `Position out of bounds!` and then "The file 'level8' is corrupted!" before dying.
    ///
    /// Ilalim ng Tulay is the first map to bake hazards in at author time
    /// (`IlalimNgTulayBuilder` attaches one per LRT pillar), which is why one map and only one
    /// map crashed. `SceneScriptCheck` now fails any build scene containing such a stub, so the
    /// next class that does this is caught before it reaches a player rather than after.
    /// </summary>
    public sealed class HazardVolume : MonoBehaviour
    {
        public float Radius = 2.0f;

        /// <summary>The slot that cast it. -1 for a hazard nobody owns.</summary>
        public int OwnerSlot = -1;

        public static HazardVolume Attach(GameObject go, float radius, int ownerSlot)
        {
            if (go == null) return null;

            var v = go.GetComponent<HazardVolume>();
            if (v == null) v = go.AddComponent<HazardVolume>();

            v.Radius = radius;
            v.OwnerSlot = ownerSlot;

            // ⚠⚠ REGISTERED HERE AS WELL AS IN OnEnable, AND THAT IS NOT BELT AND BRACES.
            // OUTSIDE PLAY MODE UNITY NEVER CALLS OnEnable on a plain MonoBehaviour, so an
            // EditMode test that attaches a volume gets an object that exists and a map that is
            // empty. `Register` refuses a duplicate, so the two paths cannot double up.
            HazardMap.Register(v);
            return v;
        }

        private void OnEnable() => HazardMap.Register(this);
        private void OnDisable() => HazardMap.Unregister(this);
    }
}
