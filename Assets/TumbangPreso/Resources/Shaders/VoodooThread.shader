// ⚠️⚠️ PHAISTER'S SOUL THREAD (HERO-10 v3, plan 9.3 and 9.5). Owner: *"whiels he's holding towards them it shows like an eerie
// vfx connecitng the two"*. One graphic for the whole kit, THE STITCH: a thin wavering cord of dark smoke with a bright core in
// the curse's colour (DRAIN crimson, HEX violet) and dashed stitches along it, so it reads at 14 m on Low where a soft glow does
// not. Drawn on `LineRenderer`s (`Visual.VoodooCursePresenter`): the reach's thread, the X it pierces with, the marks over a
// cursed body.
//
//  * PREMULTIPLIED (Blend One OneMinusSrcAlpha): the smoke at the edges DARKENS what is behind it and the core ADDS light, in
//    one pass, so the cord is legible over the bright plaza and over the night of an ultimate alike.
//  * Vertex colour is the curse's colour; its alpha fades the whole thread (a snap, a fray).
//  * TEXCOORD0.x runs along the line in METRES (`LineTextureMode.Tile`), so the stitches keep their size however long the
//    thread is; they crawl toward her at `_Crawl` metres a second (from them TO her: the soul is being drawn in).
//  * `_Tight` (0 to 1) is the reach's progress: the core thickens and brightens and the stitches sharpen as it fills.
// Loaded through Resources (`Shaders/VoodooThread`), which keeps it in the player.
Shader "TumbangPreso/VoodooThread"
{
    Properties
    {
        _Intensity ("Core intensity", Float) = 1.6
        _Smoke ("Smoke opacity", Float) = 0.55
        _Stitch ("Stitches per metre", Float) = 3.2
        _Crawl ("Stitch crawl (m/s, toward the line's start)", Float) = 1.1
        _Tight ("Tightness 0..1", Float) = 0.0
        _StitchOn ("Stitches 0..1", Float) = 1.0
    }
    SubShader
    {
        Tags { "Queue" = "Transparent+12" "RenderType" = "Transparent" "IgnoreProjector" = "True" }
        Pass
        {
            ZWrite Off
            ZTest LEqual
            Cull Off
            Blend One OneMinusSrcAlpha

            CGPROGRAM
            #include "RecordedShaderTime.cginc"
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            float _Intensity, _Smoke, _Stitch, _Crawl, _Tight, _StitchOn;

            struct appdata { float4 vertex : POSITION; float4 colour : COLOR; float2 uv : TEXCOORD0; };
            struct v2f { float4 pos : SV_POSITION; float4 colour : TEXCOORD0; float2 uv : TEXCOORD1; };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.colour = v.colour;
                o.uv = v.uv;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float across = abs(i.uv.y * 2.0 - 1.0);
                // The core: a hot line down the middle, wider and brighter as the reach tightens.
                float coreWidth = lerp(0.22, 0.42, _Tight);
                float core = saturate(1.0 - across / coreWidth);
                core *= core;
                // The stitches: dashes along the cord, crawling toward its start (her hand).
                float along = frac(i.uv.x * _Stitch + TumpShaderTime() * _Crawl * _Stitch);
                float dash = smoothstep(0.0, 0.08, along) * (1.0 - smoothstep(0.42, 0.5, along));
                float stitchBand = saturate(1.0 - across / 0.7) * dash * _StitchOn;
                // The smoke: darkening toward the edges of the cord, soft at its very rim.
                float smoke = (1.0 - smoothstep(0.55, 1.0, across)) * _Smoke;
                float3 hue = i.colour.rgb;
                float3 hot = lerp(hue, float3(1.0, 0.96, 0.92), 0.55);
                float3 light = hue * stitchBand * (0.8 + 0.6 * _Tight) + hot * core * _Intensity * (0.7 + 0.6 * _Tight);
                float cover = saturate(smoke + core + stitchBand * 0.6);
                float fade = i.colour.a;
                return fixed4(light * fade, cover * fade);
            }
            ENDCG
        }
    }
    FallBack Off
}
