// The soft shade on the court round Rago's volcano (`SeanVolcano`, `FxStudySeanVolcano`): where a heaved piece of the
// court meets the ground, and (on the bench, which has no sun) the shadow it throws.
//
// ⚠️ A MULTIPLY, FEATHERED TO NOTHING (v6; the v5 review: "a soft shadow, not the hard-edged one"). v5 drew the shadow as a
// flat polygon of the court's paving, darkened: correct inside, a hard cut at its edge. This darkens whatever is under it
// (the court's own paving, lit as it is) by the vertex colour's alpha, which the builder runs from full strength at the
// footprint to nothing a hand's width out (`feathered` in tools/build_sean_volcano.py). It writes no depth and draws after
// the opaque world, so it never hides the court, only shades it.
// ⚠️ No `_Time`, no depth texture, no loops.
Shader "TumbangPreso/SeanVolcanoShade"
{
    Properties
    {
        _Shade ("Colour at full strength", Color) = (0.42, 0.38, 0.35, 1)
        _Strength ("Strength", Range(0, 1)) = 1
        _Fade ("Fade (0 gone, 1 drawn): eases it in and out with the pieces", Range(0, 1)) = 1
    }
    SubShader
    {
        Tags { "Queue"="AlphaTest+20" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Pass
        {
            Cull Off
            ZWrite Off
            ZTest LEqual
            Offset -2, -2
            Blend DstColor Zero
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #include "UnityCG.cginc"
            fixed4 _Shade;
            float _Strength, _Fade;
            struct appdata { float4 vertex:POSITION; float4 colour:COLOR; };
            struct v2f { float4 vertex:SV_POSITION; float a:TEXCOORD0; };
            v2f vert(appdata v)
            {
                v2f o;
                o.vertex=UnityObjectToClipPos(v.vertex);
                o.a=v.colour.a;
                return o;
            }
            fixed4 frag(v2f i):SV_Target
            {
                // The feather eased (smooth at both ends) so the falloff has no visible ring where it starts.
                float a=smoothstep(0.0,1.0,saturate(i.a))*_Strength*_Fade;
                return fixed4(lerp(float3(1,1,1),_Shade.rgb,a),1);
            }
            ENDCG
        }
    }
    Fallback Off
}
