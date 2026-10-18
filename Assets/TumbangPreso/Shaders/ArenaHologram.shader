// The Arena stage's hologram: the next layout drawn over the stage before the pieces move
// (owner's reference: a stage built out of holograms). `ArenaStage` writes two floats per renderer:
//   _Appear 0..1  the whole hologram coming up at the start of the break
//   _Solid  0..1  this piece arriving: 0 is pure hologram, 1 is the painted deck standing there
//
// THE LOOK IS THE STAGE KIT'S (tools/author_arena_textures_stage.py, 2026-10-05):
//   * the hologram is arena_stage_holo.png, the deck's own hexagons as bright cyan lines on a
//     nearly clear fill. It is laid top-down at 8 m a tile in WORLD space, which is exactly how the
//     kit maps the deck, so a hologram line lands on the seam the real deck has there. The sides of
//     a piece take the old level scan lines and an edge glow instead;
//   * as _Solid rises the lines THICKEN until the hexagon panels close (the texture's alpha falls
//     away from a seam), then the panel middles fill, and what fills them is the painted deck,
//     arena_stage_deck.png. A bright band runs along the filling edge;
//   * so at _Solid 1 the piece is the deck's own picture, and `ArenaStage` swaps it for the real,
//     lit piece. That swap is from unlit to lit paint; _DeckGain is there to match the two.
//
// Premultiplied blending, so one pass goes from pure light (added) to solid paint (covering).
// Unlit, no depth write, no shadows, no fog. It is pulled a hair toward the camera so it does not
// fight the real piece as that piece arrives inside it. The scan lines stand still in a break
// (`Time.timeScale` is 0 there, which stops _Time) and rise in any use outside one.
//
// ⚠️ UNCOMPILED since this rewrite (2026-10-05), and never run: the first version was not either.
Shader "TumbangPreso/ArenaHologram"
{
    Properties
    {
        // Cyan-white: clear of offence orange #f87020 and defence blue #0080e8.
        [HDR] _Color("Glow", Color) = (0.45, 0.95, 1.0, 1)
        _Alpha("Strength", Range(0, 2)) = 0.9
        [NoScaleOffset] _HoloTex("Hologram (arena_stage_holo, RGBA)", 2D) = "white" {}
        [NoScaleOffset] _DeckTex("The deck it turns into (arena_stage_deck)", 2D) = "white" {}
        _Tile("Tiles per metre (the deck's: 1 / 8)", Float) = 0.125
        _DeckGain("Deck brightness when solid", Range(0, 3)) = 1.1
        _SideColor("Side colour when solid", Color) = (0.07, 0.09, 0.16, 1)
        [HDR] _BandColor("Filling edge", Color) = (0.9, 1.0, 1.0, 1)
        _Band("Filling edge strength", Range(0, 4)) = 1.4
        _LineDensity("Scan lines per metre (sides)", Float) = 9
        _LineSpeed("Scan line rise, metres per second", Float) = 0.35
        _Rim("Edge glow", Range(0, 4)) = 1.6
        _Flash("Brightening as it turns solid", Range(0, 4)) = 1.5
        _Solid("Solid (set per piece)", Range(0, 1)) = 0
        _Appear("Appear (set per piece)", Range(0, 1)) = 1
    }
    SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" "IgnoreProjector" = "True" }
        Blend One OneMinusSrcAlpha
        ZWrite Off
        Cull Back
        Offset -1, -1
        Pass
        {
            CGPROGRAM
            #include "../Resources/Shaders/RecordedShaderTime.cginc"
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #include "UnityCG.cginc"

            sampler2D _HoloTex, _DeckTex;
            half4 _Color, _SideColor, _BandColor;
            float _Alpha, _Tile, _DeckGain, _Band, _LineDensity, _LineSpeed, _Rim, _Flash;
            UNITY_INSTANCING_BUFFER_START(Props)
                UNITY_DEFINE_INSTANCED_PROP(float, _Solid)
                UNITY_DEFINE_INSTANCED_PROP(float, _Appear)
            UNITY_INSTANCING_BUFFER_END(Props)

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 world : TEXCOORD0;
                float3 normal : TEXCOORD1;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            v2f vert(appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_TRANSFER_INSTANCE_ID(v, o);
                o.pos = UnityObjectToClipPos(v.vertex);
                o.world = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.normal = UnityObjectToWorldNormal(v.normal);
                return o;
            }

            half4 frag(v2f i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);
                float solid = saturate(UNITY_ACCESS_INSTANCED_PROP(Props, _Solid));
                float appear = saturate(UNITY_ACCESS_INSTANCED_PROP(Props, _Appear));

                float3 n = normalize(i.normal);
                float3 view = normalize(_WorldSpaceCameraPos - i.world);
                // 1 on a deck (a face looking up), 0 on a side or an underside.
                float deckFace = smoothstep(0.5, 0.9, n.y);
                float2 uv = i.world.xz * _Tile;
                half4 holo = tex2D(_HoloTex, uv);
                half3 deck = tex2D(_DeckTex, uv).rgb * _DeckGain;

                float lines = 0.55 + 0.45 * sin((i.world.y - TumpShaderTime() * _LineSpeed) * _LineDensity * 6.2831853);
                float rim = pow(1.0 - saturate(dot(n, view)), 2.0) * _Rim;

                // The hologram itself: the hexagon lines on a deck, scan lines and edge glow elsewhere.
                float shape = lerp(saturate(lines * 0.6 + rim), saturate(holo.a + rim * 0.25), deckFace);
                half3 glow = lerp(_Color.rgb, holo.rgb * _Color.rgb * 1.6, deckFace);

                // Turning solid. On a deck the lines thicken from the seams over the first 0.6,
                // then the panel middles fill; a side only fills.
                float fromSeams = smoothstep(1.0 - 1.7 * solid, 1.15 - 1.7 * solid, holo.a);
                float fill = smoothstep(0.6, 1.0, solid);
                float cover = saturate(lerp(fill, max(fromSeams, fill), deckFace)) * step(0.001, solid);
                float band = 4.0 * cover * (1.0 - cover) * _Band;

                // Brightest half way to solid.
                float flash = 1.0 + _Flash * sin(solid * 3.14159265);
                float ghost = _Alpha * shape * (1.0 - cover);
                half3 added = glow * flash * ghost + _BandColor.rgb * band;
                half3 painted = lerp(_SideColor.rgb, deck, deckFace);
                // Premultiplied: light is added (alpha 0), paint covers (alpha is the cover).
                return half4((added + painted * cover) * appear, cover * appear);
            }
            ENDCG
        }
    }
    FallBack Off
}
