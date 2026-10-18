using UnityEngine;

namespace TumbangPreso.Map
{
    /// <summary>One fire bowl of the Payo map: its tongues of flame (children the night's life stretches and leans) and its light.
    /// `Order` is its turn when the bowls are lit one after another at dusk, 0 to 1.</summary>
    public sealed class PayoFlame : MonoBehaviour
    {
        public Transform[] Tongues = System.Array.Empty<Transform>();
        public Light Glow;
        public float Order, Height = 0.9f, Brightness = 2.2f;
        /// <summary>An ART flame (a model, not the stand-in's unit tongues): each tongue's own scale and turn as placed.
        /// The night's life then breathes that size instead of setting one of its own. Empty for the stand-in.</summary>
        public Vector3[] Rest = System.Array.Empty<Vector3>();
        public float[] RestYaw = System.Array.Empty<float>();
    }
}
