// The Arena's big screens in the opening (Runtime/Map/ArenaIntroScreen.cs). The taya's card is drawn
// once, off to one side, into a texture; every screen in the bowl is a quad wearing that texture
// through this shader, which is what lets the PICTURE bend: a wave that slides each line of it
// sideways by its own small amount (owner, 2026-10-06: "wave distort", then of rows of the card moved
// about by code: "it just looks like the elements are just moving side to side"; a wave has to bend
// the picture line by line, and only a shader sees lines).
// _Clock is given by the script from unscaled time: the opening plays with the game's clock held.
// In Resources so that a build carries it: it is loaded by path, not referenced by any scene.
Shader "TumbangPreso/ArenaScreen"
{
    Properties
    {
        _MainTex ("Picture", 2D) = "black" {}
        _Alpha ("Alpha", Range(0, 1)) = 1
        _Wave ("Wave (0 none)", Range(0, 2)) = 1
        _Gain ("Brightness", Range(0, 3)) = 1.15
        _Clock ("Clock (seconds, unscaled)", Float) = 0
    }
    SubShader
    {
        Tags { "Queue" = "Transparent+30" "IgnoreProjector" = "True" "RenderType" = "Transparent" }
        Cull Off
        Lighting Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata { float4 vertex : POSITION; float2 texcoord : TEXCOORD0; };
            struct v2f { float4 vertex : SV_POSITION; float2 uv : TEXCOORD0; };

            sampler2D _MainTex;
            half _Alpha, _Wave, _Gain;
            float _Clock;

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.texcoord;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 uv = i.uv;
                float y = uv.y;
                // Two slow waves down the picture, a long one and a short one, moving against each other.
                float wave = sin(y * 9.0 - _Clock * 1.6) * 0.0050 + sin(y * 31.0 + _Clock * 2.9) * 0.0022;
                // A tear: one narrow band that travels down the screen and shivers as it goes.
                float band = frac(_Clock * 0.21);
                float d = y - (1.0 - band);
                float tear = exp(-d * d * 1400.0) * 0.022 * sin(_Clock * 37.0 + y * 160.0);
                uv.x += (wave + tear) * _Wave;

                fixed4 c = tex2D(_MainTex, saturate(uv));
                // The glass: a little darker toward the corners.
                float2 q = i.uv * 2.0 - 1.0;
                c.rgb *= (1.0 - 0.22 * dot(q * q, q * q)) * _Gain;
                c.a = _Alpha;
                return c;
            }
            ENDCG
        }
    }
}
