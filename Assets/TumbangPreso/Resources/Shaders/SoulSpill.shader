// ⚠️⚠️ THE LIGHT THAT FALLS OUT OF PHAISTER'S VOODOO DOLL (HERO-10 v3, model v11). Owner, 2026-09-27: *"i want it to actually
// look like its coming out of the holes"*.
//
// `SoulGlow` paints the light down inside each opening; this paints what that light throws OUT: a wash over the torn lips and
// onto the cloth round each opening, fading with distance (`spill-mesh` in `phaister-doll.glb`, every vertex's colour and alpha
// typed by `tools/build_phaister_doll_voxel.py`), and the tongues of light licking up out of the torn crown between the straw.
// ADDITIVE, so over lit burlap it reads as the cloth lit from inside rather than as a colour laid on it; depth-tested and pulled a
// hair toward the camera so it never fights the cloth it lies on.
//
//  * Colour times alpha is how much light a vertex adds. `_Intensity` scales all of it.
//  * It breathes on `SoulGlow`'s wave (same `_Rate`, same climb up the body), so the light and its spill rise and fall together.
//  * TEXCOORD0: x is a flame's phase, y how far a vertex is up a flame (0 at its foot, 1 at its tip; 0 for every spill). Where
//    y is above 0 the tongue flickers and its tip sways, each flame out of step with the others.
// Loaded through Resources (`Shaders/SoulSpill`), which keeps it in the player.
Shader "TumbangPreso/SoulSpill"
{
    Properties
    {
        _Intensity ("Intensity", Float) = 0.9
        _Pulse ("Pulse", Float) = 0.22
        _Rate ("Pulse rate", Float) = 2.4
        _Sway ("Flame sway (model units)", Float) = 0.006
        _Flicker ("Flame flicker", Float) = 0.35
    }
    SubShader
    {
        Tags { "Queue" = "Transparent+10" "RenderType" = "Transparent" "IgnoreProjector" = "True" }
        Pass
        {
            ZWrite Off
            ZTest LEqual
            Cull Off
            Blend One One
            Offset -1, -1

            CGPROGRAM
            #include "RecordedShaderTime.cginc"
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            float _Intensity, _Pulse, _Rate, _Sway, _Flicker;

            struct appdata { float4 vertex : POSITION; float4 colour : COLOR; float2 uv : TEXCOORD0; };
            struct v2f { float4 pos : SV_POSITION; float4 colour : TEXCOORD0; float height : TEXCOORD1; float2 flame : TEXCOORD2; };

            v2f vert(appdata v)
            {
                v2f o;
                float4 p = v.vertex;
                if (v.uv.y > 0.0)
                {
                    float phase = v.uv.x * 6.2832;
                    p.x += sin(TumpShaderTime() * 3.1 + phase) * _Sway * v.uv.y;
                    p.z += cos(TumpShaderTime() * 2.3 + phase * 1.7) * _Sway * 0.7 * v.uv.y;
                }
                o.pos = UnityObjectToClipPos(p);
                o.height = mul(unity_ObjectToWorld, v.vertex).y;
                o.colour = v.colour;
                o.flame = v.uv;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float t = TumpShaderTime() * _Rate - i.height * 3.1;
                float breath = 0.6 * sin(t) + 0.4 * sin(t * 2.37 + 1.3);
                float flicker = 1.0;
                if (i.flame.y > 0.0)
                {
                    float phase = i.flame.x * 6.2832;
                    flicker = 1.0 - _Flicker * (0.5 + 0.5 * sin(TumpShaderTime() * 11.0 + phase * 3.0) * sin(TumpShaderTime() * 7.3 + phase));
                }
                float3 light = i.colour.rgb * i.colour.a * _Intensity * (1.0 + _Pulse * breath) * flicker;
                return fixed4(light, 0.0);
            }
            ENDCG
        }
    }
    FallBack Off
}
