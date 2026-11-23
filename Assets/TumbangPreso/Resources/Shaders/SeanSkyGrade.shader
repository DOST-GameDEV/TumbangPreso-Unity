// ⚠️⚠️ THE STREET'S LIGHT GOES RED UNDER RAGO'S SKY (MAYON AWAKENS v4, 2026-11-23). The owner: *"the sky should turn red,
// with dark clouds etc"*; a red sky over an afternoon-lit court reads as a sticker behind the street. So the map's picture
// is graded toward the sky's light: a multiply in the sky's own red, deepest at the top of the frame (the light comes from
// below now, from the fire), lifted a little toward the bottom, where the lava is.
//
// Why a multiply over the picture and not the map's lights or `SkyEvent`: both are global state that outlives a cutscene on
// any exit, and play's own Emberfall restarts its curve from the street at the hand-back, which would jump. This touches
// nothing but the cutscene's own frame.
// ⚠️ IT IS DRAWN AFTER THE MAP, THE BODIES AND THE CLOUDS (`FxStudyCloud` is AlphaTest+51) AND BEFORE THE FAR SKY (+53), so
// the street, the bodies, the lava and every ball of dust and ash take the red light (film v4r1: drawn before the clouds, the
// pale dust stayed afternoon-white under a red sky and read as cotton), while the far sky (`SeanSky`), the falling ash, the
// lightning and the glows keep their own colours.
// ⚠️ 2x multiply (`Blend DstColor SrcColor`): 0.5 is no change, so the bottom of the frame can be lifted toward the fire as
// well as the top sunk. The stage switches the renderer off at no strength, so the last frame of the cutscene is the
// street exactly.
// Drawn in clip space like `SpiritVeil`: its vertices are the screen's corners, so it covers whatever camera draws it.
Shader "TumbangPreso/SeanSkyGrade"
{
    Properties
    {
        _Strength ("Strength", Range(0, 1)) = 0
        _Top ("Light at the top of the frame", Color) = (0.44, 0.22, 0.21, 1)
        _Bottom ("Light at the bottom of the frame", Color) = (0.86, 0.50, 0.38, 1)
        _Edge ("How far the frame's edges sink", Range(0, 1)) = 0.45
    }
    SubShader
    {
        Tags { "Queue" = "AlphaTest+52" "RenderType" = "Transparent" "IgnoreProjector" = "True" }
        Pass
        {
            ZWrite Off
            ZTest Always
            Cull Off
            Blend DstColor SrcColor

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            float _Strength, _Edge;
            fixed4 _Top, _Bottom;

            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct v2f { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = float4(v.vertex.x, v.vertex.y, 0.5, 1.0);
                o.uv = v.uv;
                #if UNITY_UV_STARTS_AT_TOP
                if (_ProjectionParams.x < 0) o.uv.y = 1.0 - o.uv.y;
                #endif
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed3 light = lerp(_Bottom.rgb, _Top.rgb, smoothstep(0.05, 0.95, i.uv.y));
                float2 d = (i.uv - 0.5) * 2.0;
                float edge = smoothstep(0.55, 1.45, length(d));
                light *= 1.0 - _Edge * edge;
                // 2x multiply: 0.5 leaves the pixel as it was.
                return fixed4(lerp(fixed3(0.5, 0.5, 0.5), light * 0.5, _Strength), 1.0);
            }
            ENDCG
        }
    }
    FallBack Off
}
