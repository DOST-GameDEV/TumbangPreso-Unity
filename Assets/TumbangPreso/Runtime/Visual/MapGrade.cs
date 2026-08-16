using UnityEngine;

namespace TumbangPreso.Visual
{
    /// <summary>
    /// The `adjustment_*` numbers off a map's Godot Environment, left in the scene by
    /// `TscnImporter` so a camera can find them at runtime.
    ///
    /// ⚠️⚠️ IT EXISTS BECAUSE RenderSettings HAS NOWHERE TO PUT THEM. Ambient, fog and the sky
    /// all have a Unity counterpart the importer can write; the colour grade has none in the
    /// built-in pipeline, so without somewhere to record it the values were simply dropped on
    /// import and the whole port ran ungraded. A three-float component in the scene is the
    /// smallest thing that survives the trip.
    ///
    /// ⚠️ IT IS DATA, NOT BEHAVIOUR. <see cref="ColourGrade"/> is what actually renders. Keeping
    /// them apart is what lets the preview screens choose whether to adopt a map's grade, which
    /// they must: the character portrait is its own Environment and must not wear Eskinita's.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class MapGrade : MonoBehaviour
    {
        [SerializeField] private float _brightness = 1.0f;
        [SerializeField] private float _contrast = 1.0f;
        [SerializeField] private float _saturation = 1.0f;

        /// <summary>`tonemap_exposure`. ZERO MEANS NO TONEMAP, matching a Godot Environment whose
        /// `tonemap_mode` is 0 (LINEAR). Bayan Plaza is one of those.</summary>
        [SerializeField] private float _exposure;
        [SerializeField] private float _white = 1.9f;

        public float Brightness => _brightness;
        public float Contrast => _contrast;
        public float Saturation => _saturation;
        public float Exposure => _exposure;
        public float White => _white;

        public void Set(float brightness, float contrast, float saturation,
                        float exposure, float white)
        {
            _brightness = brightness;
            _contrast = contrast;
            _saturation = saturation;
            _exposure = exposure;
            _white = white;
        }
    }
}
