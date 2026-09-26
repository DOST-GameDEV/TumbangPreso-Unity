// ⚠️⚠️ THE LIGHT IN PAETE'S ULTIMATE (HERO-9, owner, 2026-09-26: *"make his eyes glow or smth"*). One additive
// glow for every light the cutscene carries: his eyes when they ignite, the light Mariang Makiling gives him,
// the heads of the veins racing under the court, her fireflies, and the pool the veins gather in.
//
// Why a shader of its own: the effects' `VfxMaterial.Ghost` is the lit Standard shader in Fade mode, so a
// "glow" drawn with it is a flat, hard-edged, sun-shaded disc; on the first films his eye light read as two
// lime stickers. Light has no edge. This one is unlit, ADDITIVE (it can only brighten what is behind it) and
// falls off softly from its middle, with a small hot core, so it reads as light at any distance.
//
//  * `_Billboard` 1 turns the quad to face the camera in the vertex shader (the eyes, the light, the heads),
//    so the scene never needs to know where the camera is; 0 keeps the quad as posed (a pool on the ground).
//  * `_Band` 1 fades across the quad's v only: a soft stripe for the glowing veins laid along the court.
//  * `_Lift` pulls a billboard toward the camera, so a glow sitting ON his face is not half-buried in it.
//  * `_Facing` (w > 0) dims a glow seen from behind: his eyes are on the front of his face, and from over his
//    shoulder their light should not shine through his head.
// Loaded through Resources (`Shaders/SpiritGlow`), which is what keeps it in the player (`ToonSkin`'s rule).
Shader "TumbangPreso/SpiritGlow"
{
    Properties
    {
        _Color ("Colour (alpha = strength)", Color) = (0.85, 1.0, 0.42, 1)
        _Intensity ("Intensity", Float) = 1.6
        _Falloff ("Falloff", Float) = 2.2
        _Core ("Hot core", Float) = 0.6
        _Billboard ("Billboard", Float) = 1
        _Band ("Band across v", Float) = 0
        _Lift ("Toward the camera (m)", Float) = 0
        _Facing ("Facing (world xyz, w > 0 enables)", Vector) = (0, 0, 0, 0)
    }
    SubShader
    {
        Tags { "Queue" = "Transparent+20" "RenderType" = "Transparent" "IgnoreProjector" = "True" }
        Pass
        {
            ZWrite Off
            ZTest LEqual
            Cull Off
            Blend One One

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            fixed4 _Color;
            float _Intensity, _Falloff, _Core, _Billboard, _Band, _Lift;
            float4 _Facing;

            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct v2f { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; float fade : TEXCOORD1; };

            v2f vert(appdata v)
            {
                v2f o;
                if (_Billboard > 0.5)
                {
                    // The quad's own x and y, scaled by the object's scale, laid out in VIEW space round its origin.
                    float3 centre = mul(UNITY_MATRIX_MV, float4(0, 0, 0, 1)).xyz;
                    float sx = length(float3(unity_ObjectToWorld[0].x, unity_ObjectToWorld[1].x, unity_ObjectToWorld[2].x));
                    float sy = length(float3(unity_ObjectToWorld[0].y, unity_ObjectToWorld[1].y, unity_ObjectToWorld[2].y));
                    float3 view = centre + float3(v.vertex.x * sx, v.vertex.y * sy, 0.0);
                    // Unity's view space looks down -z: toward the camera is +z.
                    view.z += _Lift;
                    o.pos = mul(UNITY_MATRIX_P, float4(view, 1.0));
                }
                else
                {
                    o.pos = UnityObjectToClipPos(v.vertex);
                }
                o.uv = v.uv;
                o.fade = 1.0;
                if (_Facing.w > 0.5)
                {
                    float3 at = mul(unity_ObjectToWorld, float4(0, 0, 0, 1)).xyz;
                    float3 toCamera = normalize(_WorldSpaceCameraPos - at);
                    o.fade = saturate(dot(normalize(_Facing.xyz), toCamera) * 1.6 + 0.3);
                }
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float d = _Band > 0.5 ? abs(i.uv.y - 0.5) * 2.0 : length(i.uv - 0.5) * 2.0;
                float body = pow(saturate(1.0 - d), _Falloff);
                float core = pow(saturate(1.0 - d), _Falloff * 4.0) * _Core;
                float3 c = _Color.rgb * (body + core) * _Intensity * _Color.a * i.fade;
                return fixed4(c, 0.0);
            }
            ENDCG
        }
    }
    FallBack Off
}
