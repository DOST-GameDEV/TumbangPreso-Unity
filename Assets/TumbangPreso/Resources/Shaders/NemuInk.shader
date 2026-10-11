// NEMU'S LAST FRAMES (PRUSISYON, `HeroIntroductionScene.Nemu.cs`, `NemuPostProcess`): the world sinks into ink and only the
// violet light of the possessed players' eyes is left burning. A full-frame pass over the cutscene's own render: every pixel
// is darkened by `_Ink`, except light that is bright AND violet (the eye glows), which is kept.
Shader "TumbangPreso/NemuInk"
{
    Properties
    {
        _MainTex ("Frame", 2D) = "white" {}
        _Ink ("Ink", Range(0, 1)) = 0
        _InkColour ("Ink colour", Color) = (0.0015, 0.0, 0.004, 1)
    }
    SubShader
    {
        Cull Off ZWrite Off ZTest Always
        Pass
        {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag
            #include "UnityCG.cginc"
            sampler2D _MainTex;
            float _Ink;
            fixed4 _InkColour;
            fixed4 frag (v2f_img i) : SV_Target
            {
                fixed4 c = tex2D(_MainTex, i.uv);
                float bright = max(c.r, max(c.g, c.b));
                // Violet: blue and red above green. Bright violet is an eye; it survives the ink.
                float violet = saturate((c.b + c.r * .5 - c.g * 1.5) * 3.0);
                float keep = saturate(violet * smoothstep(.35, .7, bright));
                float3 inked = lerp(c.rgb, _InkColour.rgb, _Ink * (1.0 - keep));
                return fixed4(inked, c.a);
            }
            ENDCG
        }
    }
}
