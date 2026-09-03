// The one pass every sourced ability sheet draws through.
//
// ⚠️⚠️ IT EXISTS BECAUSE `Sprites/Default` SILENTLY IGNORES TILING AND OFFSET, AND THE FIRST
// RENDER OF THE WHOLE PASS IS THE RECEIPT. `Logs/shots-abilities/ability_ice_sheet_eye_v51.png`
// shows Cheska's formation nova as FIVE novas in a row at five different stages, standing on the
// ice in a strip: that is the entire 5 x 3 sprite sheet drawn on one quad at once.
// `VfxFlipbook.ShowCell` sets `mainTextureScale` and `mainTextureOffset`, which is `_MainTex_ST`,
// and Unity's sprite shader passes `v.texcoord` straight to the sampler without ever applying it.
// Nothing warns; the animation simply is not there and the effect reads as a bad texture.
//
// ⚠️ THE ALTERNATIVE WAS REWRITING FOUR MESH UVs TWENTY TIMES A SECOND PER LIVE EFFECT, which
// is a mesh upload per frame per effect on a game that ships to phones. One `TRANSFORM_TEX` is
// free.
//
// ⚠️⚠️ IT IS UNLIT ON PURPOSE, WHICH IS `VfxMaterial`'s OWN MEASUREMENT REUSED: *"the arena is
// lit for characters and not for these. A frost sheet shaded by the scene's key light goes dark
// on the shadowed half of the court, which is exactly where a player most needs to see it."*
// Ilalim ng Tulay is half in the shadow of a flyover and an ability has to read on both sides.
//
// ⚠️ ALPHA BLEND, NOT ADDITIVE. Additive is how an effect ends up white: it adds to whatever is
// behind it, so two overlapping frames blow past 255 and `docs/VISION.md` § 2 rule 5's 12 per
// cent gate starts failing on the pile-up frame rather than on any single ability.
// `AbilityShowcaseProbe` measures exactly that.
//
// ⚠️ `Cull Off` BECAUSE A BILLBOARD IS SEEN FROM BOTH SIDES AND A GROUND DECAL FROM UNDERNEATH.
// `VfxFlipbook` turns its quads in `OnWillRenderObject`, and a one-frame lag on a fast camera
// would otherwise make an effect vanish.
//
// ⚠️ `ZWrite Off` AND THE TRANSPARENT QUEUE + 50. These are the layer on top of the floor
// decals by definition, and `VfxMaterial` puts every decal at `Queue = Transparent`. Writing
// depth would also make one frame of a flipbook occlude the next effect drawn behind it.
//
// ⚠️⚠️ IT MUST BE NAMED IN `GameBuilder.EnsureRuntimeShaders`. Nothing in any scene references
// it: `VfxFlipbook.NewMaterial` reaches it through `Shader.Find`, which is exactly the case that
// list exists for. Stripped from a player, every hero effect ships pink.
Shader "TumbangPreso/VfxFlipbook"
{
    Properties
    {
        _MainTex ("Sheet", 2D) = "white" {}
        _Color ("Tint", Color) = (1, 1, 1, 1)
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "Queue" = "Transparent+50"
            "IgnoreProjector" = "True"
            "PreviewType" = "Plane"
        }

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

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _Color;

            v2f vert(appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.pos = UnityObjectToClipPos(v.vertex);

                // ⚠️ THIS ONE LINE IS THE WHOLE REASON THE FILE EXISTS. It is what
                // `Sprites/Default` does not do.
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 c = tex2D(_MainTex, i.uv) * _Color;

                // ⚠️ THE SHEETS ARE HARD-EDGED PIXEL ART WITH BINARY ALPHA, so anything that is
                // not fully opaque came from the tint's own fade. Multiplying rather than
                // clipping keeps `VfxFlipbook.FadeFrom` working on a still.
                return c;
            }
            ENDCG
        }
    }

    Fallback "Sprites/Default"
}
