// ⚠️⚠️ THE LIGHT INSIDE PHAISTER'S VOODOO DOLL (HERO-10 v3, owner 2026-09-27: *"make it look liek its glowing inside and it
// stitched tgthr"*). The doll's split seams, its grin, the slit under its X eye and the ring round its button are one mesh of
// their own (`glow-mesh` in `phaister-doll.glb`, `tools/build_phaister_doll_voxel.py`), and this paints it: OPAQUE and UNLIT, so
// the light in a split is never shaded like the cloth round it, with a slow uneven throb along the body so it reads as
// something alive in there rather than paint. `SpiritGlow` is additive and soft-edged, made for halos; a seam needs a hard,
// solid light held in by the stitches, which is this.
//
//  * Two lights, picked by which palette cell the part was built in (the builder's `GLOW` and `GLOW_CORE` slots): `_Color` for
//    the edge of a split, `_Core` for the hot line down its middle. v6's one flat colour read as pink paint, not light.
//  * `_Intensity` above 1 lifts it past the lit cloth. `_Pulse` how far it breathes (0 is steady), `_Rate` how fast; the phase
//    runs up the body (world y) so a throb climbs the doll rather than blinking all at once. The core breathes harder.
// Loaded through Resources (`Shaders/SoulGlow`), which is what keeps it in the player (`ToonSkin`'s rule).
Shader "TumbangPreso/SoulGlow"
{
    Properties
    {
        _Color ("Soul light, edge", Color) = (1.0, 0.33, 0.9, 1)
        _Core ("Soul light, core", Color) = (1.0, 0.86, 0.97, 1)
        _Intensity ("Intensity", Float) = 1.3
        _Pulse ("Pulse", Float) = 0.22
        _Rate ("Pulse rate", Float) = 2.4
        _CoreBelowU ("Parts left of this u are core", Float) = 0.9
    }
    SubShader
    {
        Tags { "Queue" = "Geometry" "RenderType" = "Opaque" }
        Pass
        {
            ZWrite On
            ZTest LEqual
            Cull Back

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            fixed4 _Color, _Core;
            float _Intensity, _Pulse, _Rate, _CoreBelowU;

            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct v2f { float4 pos : SV_POSITION; float height : TEXCOORD0; float core : TEXCOORD1; };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.height = mul(unity_ObjectToWorld, v.vertex).y;
                o.core = v.uv.x < _CoreBelowU ? 1.0 : 0.0;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // Two waves out of step, so the throb is uneven, and climbing: the phase falls with height.
                float t = _Time.y * _Rate - i.height * 3.1;
                float breath = 0.6 * sin(t) + 0.4 * sin(t * 2.37 + 1.3);
                float3 edge = _Color.rgb * _Intensity * (1.0 + _Pulse * breath);
                float3 core = _Core.rgb * (1.0 + _Pulse * 1.6 * breath);
                return fixed4(lerp(edge, core, i.core), 1.0);
            }
            ENDCG
        }
    }
    FallBack Off
}
