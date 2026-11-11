Shader "TumbangPreso/AimGuide"
{
    Properties { [HideInInspector] _OutlinedRibbon ("Outlined ribbon", Float) = 0 }
    SubShader
    {
        Tags { "Queue"="Transparent+120" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Pass
        {
            Cull Off
            ZWrite Off
            ZTest LEqual
            Offset -1, -1
            Blend SrcAlpha OneMinusSrcAlpha
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            float _OutlinedRibbon;
            struct Input { float4 vertex : POSITION; fixed4 colour : COLOR; float2 uv : TEXCOORD0; };
            struct Output { float4 vertex : SV_POSITION; fixed4 colour : COLOR; float across : TEXCOORD0; };
            Output vert(Input input)
            {
                Output output;
                output.vertex = UnityObjectToClipPos(input.vertex);
                output.colour = input.colour;
                output.across = input.uv.x;
                return output;
            }
            fixed4 frag(Output input) : SV_Target
            {
                if (_OutlinedRibbon < .5) return input.colour;
                float edge = abs(input.across);
                float fill = 1 - smoothstep(.55, .65, edge);
                float coverage = 1 - smoothstep(.87, 1, edge);
                return fixed4(lerp(fixed3(.032, .024, .018), input.colour.rgb, fill),
                              input.colour.a * coverage);
            }
            ENDCG
        }
    }
}
