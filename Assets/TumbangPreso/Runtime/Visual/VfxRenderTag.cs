using System.Collections.Generic;
using UnityEngine;

namespace TumbangPreso.Visual
{
    /// <summary>
    /// Marks a renderer as a runtime EFFECT rather than as part of a prop's model.
    ///
    /// ⚠️⚠️ IT EXISTS BECAUSE A TEST COULD NOT TELL THE DIFFERENCE, AND IT WAS RIGHT NOT TO.
    /// `InputEdgeTests.EverySlipperAndTheLataWearTheToonOutline` walks every renderer under a
    /// slipper or the lata and demands the toon shader, because the ink outline and the palette
    /// remap are the look and a prop that misses them is the reported bug it was written for.
    /// Then the lata's restore-protection shell landed: a transparent sphere parented to the can,
    /// which is a renderer under the lata by construction and MUST NOT be toon-shaded. A shell
    /// with an ink outline drawn round it is a solid object, and the whole point of it is that
    /// you can see the can through it.
    ///
    /// ⚠️ SO THE RULE IS "IS THIS PART OF THE MODEL", NOT "IS THIS THE LATA'S SHELL". Adding the
    /// one name to a skip list in the test would have to be redone for the next effect anybody
    /// parents to a prop, and the next one would ship red. `VfxMaterial.Ghost` and
    /// `VfxMaterial.Solid` attach this to everything they paint, so an effect written later is
    /// exempt on the day it is written and nothing else ever is.
    ///
    /// ⚠️ IT ALSO OWNS THE MATERIAL THOSE TWO PAINT WITH, which is a second job and is on this
    /// component because it is already attached to exactly the right set of objects. See
    /// <see cref="Own"/> for what was leaking before it did.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class VfxRenderTag : MonoBehaviour
    {
        public static VfxRenderTag Attach(GameObject go)
        {
            if (go == null) return null;

            var tag = go.GetComponent<VfxRenderTag>();
            return tag != null ? tag : go.AddComponent<VfxRenderTag>();
        }

        /// <summary>
        /// The materials this object's effect owns outright, freed when it dies.
        ///
        /// ⚠️⚠️ EVERY HERO EFFECT WAS LEAKING ONE MATERIAL PER RENDERER AND NOTHING WAS EVER
        /// FREEING THEM. `VfxMaterial.Ghost` and `VfxMaterial.Solid` each build a fresh
        /// `new Material` so one effect fading out cannot fade every other effect with it, and
        /// then assign it to `sharedMaterial`. Unity frees a material a renderer created for
        /// ITSELF through the `.material` accessor; it does not free one handed to
        /// `sharedMaterial`, because from its side that is somebody else's asset. So the
        /// material outlived the disc, the shard or the ember it was painted on, and the only
        /// thing that ever cleared them was quitting the game.
        ///
        /// ⚠️ THE COST IS PER CAST, NOT PER MATCH, AND THAT IS WHAT MAKES IT WORTH CLOSING.
        /// `HeroHazards` alone paints through those two functions in 60 places and several of
        /// them sit inside loops, so one ability can strand a dozen at once. Every one of them
        /// survives the effect that owned it, for the rest of the process.
        ///
        /// ⚠️ THIS COMPONENT IS THE RIGHT OWNER BECAUSE IT IS ALREADY THERE. Both painters attach
        /// it to everything they touch, for the unrelated reason in the class note above, so
        /// ownership costs no new object and no new call site. A renderer that acquires its
        /// material some other way is untouched.
        /// </summary>
        private List<Material> _owned;

        /// <summary>
        /// Tag <paramref name="go"/> and hand it a material to free when it is destroyed. Only
        /// for a material the caller built for this object alone.
        /// </summary>
        public static void Own(GameObject go, Material material)
        {
            if (go == null || material == null) return;

            var tag = Attach(go);
            if (tag == null) return;

            tag._owned ??= new List<Material>(1);
            if (!tag._owned.Contains(material)) tag._owned.Add(material);
        }

        /// <summary>
        /// ⚠️ PLAY MODE ONLY, AND THAT IS DELIBERATE RATHER THAN LAZY. `Object.Destroy` outside
        /// play mode logs an error the ability suites would fail on, and `DestroyImmediate` is
        /// not legal from inside `OnDestroy`. An editor session keeping a few effect materials
        /// alive is exactly what happens today, so skipping there changes nothing; the leak this
        /// closes is the one in the running game.
        /// </summary>
        private void OnDestroy()
        {
            if (_owned == null) return;

            if (Application.isPlaying)
            {
                foreach (var material in _owned)
                    if (material != null) Destroy(material);
            }

            _owned.Clear();
        }
    }
}
