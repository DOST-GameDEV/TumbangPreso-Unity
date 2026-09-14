Shader "TumbangPreso/AimGuide"
{
    SubShader
    {
        Tags { "Queue"="Transparent+120" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Pass
        {
            Cull Off
            ZWrite Off
            ZTest LEqual
            Blend SrcAlpha OneMinusSrcAlpha
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            struct Input { float4 vertex : POSITION; fixed4 colour : COLOR; };
            struct Output { float4 vertex : SV_POSITION; fixed4 colour : COLOR; };
            Output vert(Input input)
            {
                Output output;
                output.vertex = UnityObjectToClipPos(input.vertex);
                output.colour = input.colour;
                return output;
            }
            fixed4 frag(Output input) : SV_Target { return input.colour; }
            ENDCG
        }
    }
}
