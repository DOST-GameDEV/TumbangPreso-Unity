// § THE STUN FROST — the screen half. 🧑 2026-08-06, on the Godot build: *"can we have like
// a frost effect to indicate that an attacker is stunned after getting tagged?"*, with a
// reference image of an icy frame around a clear centre.
//
// ⚠️⚠️ EDGE-WEIGHTED, CENTRE-CLEAR, AND THAT SHAPE IS THE WHOLE REASON THIS CAN EXIST AT
// ALL. `Hud` records what happened the last time a full-screen tint was HELD rather than
// pulsed: a full-screen red rect left on for forty seconds "does not read as feedback, it
// reads as the renderer being broken". The fix there was alpha, `DangerHoldAlpha` 0.16, low
// enough to see through for a whole round.
//
// A tag stun is 5 seconds: far past the 0.45 s a flash gets, far short of a round. Dropping
// it to 0.16 would make the biggest moment in the defender's game invisible; holding it at
// flash strength would black out the arena. The reference resolves it by putting the opacity
// where the player is NOT looking. The frame can be strong because the centre stays clear, so
// the stunned player still watches the round they cannot act in.
//
// ⚠️ PROCEDURAL, NOT A TEXTURE. The reference is photographic rime; this game is a flat
// two-band toon look with ink outlines, and a photoreal overlay would sit on top of it like a
// sticker. These cracks are drawn from value noise, so they carry the same hand-drawn
// irregularity as the rest of the art and cost no asset and no import.
Shader "TumbangPreso/FrostVignette"
{
    Properties
    {
        // ⚠️⚠️ `_MainTex` IS NOT OPTIONAL ON A UI MATERIAL EVEN WHEN NOTHING SAMPLES IT, AND
        // LEAVING IT OUT LOGGED AN ERROR ON EVERY CANVAS REBUILD: *"Material
        // 'TumbangPreso/FrostVignette' with Shader 'TumbangPreso/FrostVignette' doesn't have a
        // texture property '_MainTex'"*. UGUI hands a Graphic's texture to the CanvasRenderer
        // through that exact name and does not care that this effect is procedural, so the
        // property has to exist for the material to be a legal UI material at all. Found by
        // `StunFrostTests`, which fails on any unexpected error line — the frost itself drew
        // correctly the whole time, which is why nothing else noticed.
        //
        // Declared and deliberately never sampled: the ice is generated, and an Image with no
        // sprite hands over a 1x1 white anyway, so reading it would multiply by 1 and cost a
        // fetch per pixel of a full-screen quad.
        _MainTex ("Sprite Texture (unused, required by UGUI)", 2D) = "white" {}

        // 0 = clear, 1 = fully iced. Driven from the remaining stun, so the frost RECEDES as
        // the effect wears off, which is the "tell the player when it is ending" half of the
        // accessible-status guidance this was designed against.
        _Coverage ("Coverage", Range(0, 1)) = 0
        _FrostTint ("Frost Tint", Color) = (0.66, 0.90, 0.95, 1)
        _CrackColor ("Crack Colour", Color) = (0.80, 0.98, 1.0, 1)

        // How far in from the frame the ice reaches at full coverage, in units of SCREEN
        // HEIGHT.
        //
        // ⚠️⚠️ CUT 0.36 → 0.24 ON 2026-08-23, AND THE ARITHMETIC IS THE ARGUMENT.
        // Reach is measured in SCREEN HEIGHTS from each edge, and the two opposite edges both
        // spend it: at 0.36 the clear strip left in the middle was 1 - 0.72 = **0.28 of the
        // screen height**. Roughly three quarters of the frame was ice, held for the whole five
        // second stun. That is the same failure `Hud.SetDownedFlash` records for the held red
        // tint, arriving through coverage instead of through alpha. At 0.24 the clear centre is
        // **0.52**, so the stunned player keeps half the frame to watch the round in and the
        // frost is still unmistakably a frozen frame.
        //
        // ⚠️ THE SHAPE AND THE FILAMENTS ARE UNTOUCHED. What was wrong was how much of the
        // screen it covered, not what it looked like.
        //
        // History: the Godot shader's own default said 0.15 and `HUD.tscn` overrode it to 0.36
        // on the material, so 0.36 is what that build rendered. The 0.15 was a tuning pass that
        // never took effect. This lands between the two, deliberately.
        _MaxReach ("Max Reach", Range(0.05, 0.5)) = 0.24

        // Viewport width / height, written from `Hud` every frame.
        //
        // ⚠️ WITHOUT IT THE BAND IS UNEVEN: UV is 0..1 on BOTH axes regardless of shape, so a
        // fixed UV distance is ~1.8x more pixels horizontally than vertically on 16:9. Measured
        // on the Godot build as a thick blue curtain down the left and right and a thin line
        // top and bottom.
        _Aspect ("Aspect", Float) = 1.7777
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
            "PreviewType" = "Plane"
            "CanUseSpriteAtlas" = "True"
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
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
                float4 color : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            half _Coverage;
            fixed4 _FrostTint;
            fixed4 _CrackColor;
            half _MaxReach;
            float _Aspect;

            v2f vert (appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.color = v.color;
                return o;
            }

            float hash (float2 p)
            {
                return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453);
            }

            float value_noise (float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                f = f * f * (3.0 - 2.0 * f);
                return lerp(lerp(hash(i), hash(i + float2(1.0, 0.0)), f.x),
                            lerp(hash(i + float2(0.0, 1.0)), hash(i + float2(1.0, 1.0)), f.x),
                            f.y);
            }

            float fbm (float2 p)
            {
                float total = 0.0;
                float amp = 0.5;

                for (int i = 0; i < 4; i++)
                {
                    total += value_noise(p) * amp;
                    p *= 2.0;
                    amp *= 0.5;
                }

                return total;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float2 uv = i.uv;

                // ⚠️ DISTANCE TO THE NEAREST EDGE, NOT RADIAL DISTANCE. The downed vignette
                // beside this one uses `length(uv)`, which is right for a soft radial wash but
                // wrong here: on a 16:9 frame a radial falloff reaches the left and right edges
                // long before the top and bottom, so the ice would pool in the corners and
                // leave the long edges bare. The reference frosts all four edges evenly.
                float2 d = min(uv, 1.0 - uv);

                // ⚠️ `d.x * aspect` PUTS BOTH AXES IN THE SAME UNIT, screen heights, so the
                // band is the same thickness in PIXELS on all four edges. A bare
                // `min(d.x, d.y)` does not give that on a wide frame.
                float edge = min(d.x * _Aspect, d.y);

                float reach = _MaxReach * _Coverage;

                // Break the boundary up so it is a ragged frost line rather than a clean border.
                float ragged = (fbm(uv * float2(7.0 * _Aspect, 7.0)) - 0.5) * 0.07 * _Coverage;

                // ⚠️ A TIGHT TRANSITION, NOT A GRADIENT. A wide falloff is what made the first
                // Godot pass read as haze: the ice needs a definable front with clear glass
                // behind it, so the player can still watch the round they are locked out of.
                float body = 1.0 - smoothstep(reach * 0.62, reach + ragged, edge);

                // Crystal filaments: thin bright ridges from folded noise, reaching FURTHER in
                // than the body does, so the frost has spindly fingers instead of a soft cloud.
                // These carry the effect's character; the body behind them is only their bed.
                float veins = 1.0 - abs(fbm(uv * float2(5.0 * _Aspect, 5.0) + 11.0) * 2.0 - 1.0);
                veins = pow(saturate(veins), 12.0);
                float vein_mask = 1.0 - smoothstep(reach * 0.3, (reach + ragged) * 1.6, edge);
                float cracks = veins * vein_mask;

                // Frozen breath speckle, densest where the ice is thickest.
                float speck = pow(value_noise(uv * float2(90.0 * _Aspect, 90.0)), 6.0) * body;

                float3 rgb = lerp(_FrostTint.rgb, _CrackColor.rgb,
                                  saturate(cracks * 1.4 + speck));

                // ⚠️ TUNED DOWN FROM 0.58/0.90/0.45 ON A LOOK-AT — 🧑: *"lower down the opacity
                // a bit for the frozen screen. it's a bit too strong"*. The SHAPE was right at
                // those values and is untouched; only the weight came off. The filaments keep
                // the largest share of what is left, because they are what makes it read as ice
                // rather than as a blue haze.
                // ⚠️ THE BODY LOST WEIGHT AGAIN AND THE FILAMENTS DID NOT, for the reason
                // the first tune-down gives: the cracks are what make this read as ice rather
                // than as a blue haze, so the flat wash behind them is the part that can afford
                // to go. 0.36 → 0.30 on the body, alongside the reach cut above.
                float alpha = saturate(body * 0.30 + cracks * 0.64 + speck * 0.24) * _Coverage;

                return fixed4(rgb, alpha * i.color.a);
            }
            ENDCG
        }
    }
}
