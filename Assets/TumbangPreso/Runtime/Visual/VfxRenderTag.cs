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
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class VfxRenderTag : MonoBehaviour
    {
        public static void Attach(GameObject go)
        {
            if (go == null) return;
            if (go.GetComponent<VfxRenderTag>() == null) go.AddComponent<VfxRenderTag>();
        }
    }
}
