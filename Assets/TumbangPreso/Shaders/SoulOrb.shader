// SOULBOUND's dormant ghost (Nemu, owner 2026-10-21): the glass soul-globe it curls up in, waiting for her.
// A clear bubble tinted the ghost's own colour, brightest at its rim where it turns violet (her one accent), and
// breathing with `_Pulse`. Reached only through `Shader.Find` (`Abilities.NemuDormant`), so `GameBuilder` lists it.
Shader "TumbangPreso/SoulOrb"
{
    Properties
    {
        _Color ("Colour", Color) = (0.6, 0.85, 1, 1)
        _Rim ("Rim", Color) = (0.62, 0.26, 1, 1)
        _Fade ("Fade", Range(0, 1)) = 1
        _Pulse ("Pulse", Range(0, 1)) = 0
    }

    SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" "IgnoreProjector" = "True" }
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha
        Cull Back

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            fixed4 _Color, _Rim;
            half _Fade, _Pulse;

            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 normal : TEXCOORD0;
                float3 view : TEXCOORD1;
                float height : TEXCOORD2;
            };

            v2f vert (appdata_base v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.normal = UnityObjectToWorldNormal(v.normal);
                o.view = WorldSpaceViewDir(v.vertex);
                o.height = v.vertex.y;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                half facing = saturate(dot(normalize(i.normal), normalize(i.view)));
                half edge = pow(1.0h - facing, 2.0h);
                // A soft sheen high on the glass, so it reads as a ball and not a disc.
                half sheen = smoothstep(0.25h, 0.5h, i.height) * pow(facing, 6.0h) * 0.35h;
                fixed3 colour = lerp(_Color.rgb, _Rim.rgb, saturate(edge * 1.3h));
                colour *= 1.0h + 0.5h * _Pulse;
                colour += edge * 0.3h + sheen;
                half alpha = (0.16h + 0.75h * edge + 0.12h * _Pulse + sheen) * _Fade * _Color.a;
                return fixed4(colour, saturate(alpha));
            }
            ENDCG
        }
    }
}
