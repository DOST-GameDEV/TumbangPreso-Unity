// AMIHAN'S CARD WORD (Airburst v4, 2026-10-03, `docs/reports/amihan-presentation-2026-10-02/cutscene-v4-plan.md`). A font
// atlas drawn as world text that is DEPTH TESTED: the stock "GUI/Text Shader" draws over everything, so AIRBURST painted
// across her face; behind her shoulders it must sit behind her. Coverage from the atlas's alpha, colour from the vertex.
// Loaded through Resources (`Shaders/AmihanCardText`), which keeps it in the player.
Shader "TumbangPreso/AmihanCardText"
{
    Properties
    {
        _MainTex ("Font atlas", 2D) = "white" {}
        _Color ("Tint", Color) = (1, 1, 1, 1)
    }
    SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" "IgnoreProjector" = "True" }
        Pass
        {
            ZWrite Off
            ZTest LEqual
            Cull Off
            Blend SrcAlpha OneMinusSrcAlpha

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _Color;

            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; fixed4 colour : COLOR; };
            struct v2f { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; fixed4 colour : COLOR; };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.colour = v.colour * _Color;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 c = i.colour;
                c.a *= tex2D(_MainTex, i.uv).a;
                return c;
            }
            ENDCG
        }
    }
}
