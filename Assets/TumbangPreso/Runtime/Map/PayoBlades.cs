using UnityEngine;

namespace TumbangPreso.Map
{
    /// <summary>
    /// The Payo map's rice and grass: the root of the static blade chunks that PayoBladesBaker (editor) writes. Everything
    /// moves in the PayoBlades shader (TumbangPreso/PayoBlades) from _Time; this component has NO Update. It only hands
    /// the wind to the shader as two globals when it is enabled or edited, and keeps the bake's numbers for measuring.
    /// See docs/PAYO_BLADES.md.
    /// </summary>
    [ExecuteAlways]
    [DisallowMultipleComponent]
    public sealed class PayoBlades : MonoBehaviour
    {
        public const string ShaderName = "TumbangPreso/PayoBlades";
        private static readonly int WindId = Shader.PropertyToID("_PayoWind");
        private static readonly int GustId = Shader.PropertyToID("_PayoGust");

        [Header("Wind (pushed to the shader once, never per frame)")]
        [Tooltip("Where the wind blows TO, in degrees clockwise from north (+z).")]
        public float WindBearing = 35.0f;
        [Tooltip("How far a blade's tip leans, as a share of its height.")]
        [Range(0.0f, 0.6f)] public float WindStrength = 0.22f;
        [Tooltip("The quick sway, radians a second.")]
        [Range(0.0f, 6.0f)] public float SwaySpeed = 1.9f;
        [Tooltip("The size of the gust waves rolling over the paddies: noise cells a metre (0.04 = 25 m waves).")]
        [Range(0.005f, 0.2f)] public float GustScale = 0.04f;
        [Tooltip("How fast the gust waves travel over the ground, metres a second.")]
        [Range(0.0f, 12.0f)] public float GustSpeed = 4.5f;

        [Header("The bake's numbers (written by PayoBladesBaker)")]
        public int Chunks;
        public int RiceClumps, GrassTufts;
        public int Lod0Triangles, Lod1Triangles;
        [Tooltip("LOD0 triangles of the clumps within 120 m of the court.")]
        public int NearTriangles;
        [Tooltip("Triangles drawn with the camera at the court, every direction (LOD0 for chunks whose centre is inside the switch distance, LOD1 beyond, nothing past the cull).")]
        public int CourtViewTriangles;
        [Tooltip("Chunks inside the cull distance of the court: the most draw calls the blades can add there (frustum culling drops most).")]
        public int CourtViewChunks;
        public long MeshBytes;
        [Tooltip("A dense patch of grass near the court, and one of rice: where the review's close-ups look (written by the bake).")]
        public Vector3 GrassSample, RiceSample;

        private void OnEnable() => Push();
        private void OnValidate() => Push();

        /// <summary>Hands the wind to every PayoBlades material as globals (the shader does not list them as properties).</summary>
        public void Push()
        {
            float a = WindBearing * Mathf.Deg2Rad;
            Shader.SetGlobalVector(WindId, new Vector4(Mathf.Sin(a), Mathf.Cos(a), WindStrength, SwaySpeed));
            Shader.SetGlobalVector(GustId, new Vector4(GustScale, GustSpeed * GustScale, 0.0f, 0.0f));
        }
    }
}
