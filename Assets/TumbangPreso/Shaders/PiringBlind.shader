// BLINDED, THE VICTIM'S OWN SCREEN (Nemu's DON'T LOOK, owner 2026-10-15: *"the ghost would lunge at their eyes and cover it for
// 2 secondns and slow them for 5 seconds (the slow effect gets lower overtime)"*). Driven from the replicated BLINDED clock
// (`UI.TumpHudEffects.TickBlinded`), nothing stored.
//
// ⚠️ NO HANDS, NO DRAWING OVER THE SCREEN. Three rounds of hands (outlined mittens, gaunt fingers, close palms) all read as a
// sticker on the lens; owner: *"it looks so fkn baddd"*, *"figure out howw to make the blind look better"*. The blinds that
// work (Valorant's Omen Paranoia and Reyna Leer, Among Us lights-out) take the WORLD away and give it back. So, from the
// mockup the owner approved:
//  * THE LUNGE (`_Elapsed` 0 to 0.22 s): black smoke slams in from every edge to the middle, and its two violet eyes come
//    up huge and close as the last light goes.
//  * THE DARK (to 1.7 s): total darkness, smoke barely moving in it, and its eyes, smaller now, watching you. They blink.
//  * THE TEAR (`_Open`, 1.7 to 2.0 s): your view rips back open from the middle through ragged smoke; the eyes go.
//  * THE SLOW (`_Slow` 1 to 0 as it wears off): a smoky black edge round the screen that thins with it.
// `_Calm` (Reduce visual effects) stills the smoke and the blinks; the blind itself stays (it is the rule).
// ⚠️ Colours are authored in sRGB like every colour property here (Unity hands the shader their linear values).
Shader "TumbangPreso/PiringBlind"
{
    Properties
    {
        _MainTex ("Sprite Texture (unused, required by UGUI)", 2D) = "white" {}
        _Ink ("Ink", Color) = (0.03, 0.018, 0.045, 1)
        _Smoke ("Smoke", Color) = (0.13, 0.08, 0.19, 1)
        _Eye ("Eye", Color) = (0.86, 0.66, 1, 1)
        _Elapsed ("Seconds since it sprang", Float) = 0
        _Open ("Open", Range(0, 1)) = 0
        _Slow ("Slow left", Range(0, 1)) = 0
        _Covered ("Covered", Range(0, 1)) = 0
        _Aspect ("Aspect", Float) = 1.777
        _Calm ("Reduced effects", Float) = 0
    }
    SubShader
    {
        Tags { "Queue" = "Transparent" "IgnoreProjector" = "True" "RenderType" = "Transparent" "PreviewType" = "Plane" "CanUseSpriteAtlas" = "True" }
        Cull Off Lighting Off ZWrite Off ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; float4 color : COLOR; };
            struct v2f { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; float4 color : COLOR; };
            fixed4 _Ink, _Smoke, _Eye;
            float _Elapsed, _Open, _Slow, _Covered, _Aspect, _Calm;

            v2f vert (appdata v) { v2f o; o.pos = UnityObjectToClipPos(v.vertex); o.uv = v.uv; o.color = v.color; return o; }

            float hash(float2 q) { return frac(sin(dot(q, float2(127.1, 311.7))) * 43758.5453); }
            float noise(float2 q)
            {
                float2 c = floor(q), f = frac(q);
                float2 u = f * f * (3.0 - 2.0 * f);
                return lerp(lerp(hash(c), hash(c + float2(1, 0)), u.x), lerp(hash(c + float2(0, 1)), hash(c + float2(1, 1)), u.x), u.y);
            }
            float fbm(float2 q) { return noise(q) * 0.55 + noise(q * 2.1 + 3.7) * 0.3 + noise(q * 4.3 + 7.1) * 0.15; }

            // Its eyes: two slanted violet almonds, a hot core and a soft glow. `size` 1 is the watching size.
            float eyes(float2 p, float2 centre, float sep, float size)
            {
                float glow = 0.0;
                for (int e = -1; e <= 1; e += 2)
                {
                    float2 d = (p - centre - float2(e * sep, 0.0)) / (float2(0.11, 0.05) * size);
                    float a = e * 0.35;
                    float2 r = float2(d.x * cos(a) - d.y * sin(a) * 0.45, d.x * sin(a) * 2.2 + d.y * cos(a));
                    float l = length(r);
                    glow += pow(saturate(1.0 - l), 0.6) + exp(-l * 1.6) * 0.5;
                }
                return glow;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float t = _Calm > 0.5 ? 0.0 : _Time.y;
                float2 p = (i.uv * 2.0 - 1.0) * float2(_Aspect, 1.0);
                float r = length(p / float2(_Aspect * 0.8, 1.0));
                float n = fbm(p * 2.2 + float2(t * 0.15, -t * 0.25));
                float n2 = fbm(p * 5.5 + float2(-t * 0.3, 1.7));
                float ragged = r + (n - 0.5) * 0.6 + (n2 - 0.5) * 0.18;

                // THE LUNGE: the dark closes from beyond the edges to past the middle in 0.22 s, fast at the end.
                float lunge = saturate(_Elapsed / 0.22);
                float closeR = lerp(2.0, -0.4, lunge * lunge);
                float dark = smoothstep(closeR - 0.12, closeR + 0.06, ragged);
                // THE TEAR: a hole rips open from the middle, ragged, its smoke edge left hanging.
                float holeR = _Open * 2.3;
                float hole = _Open > 0.0 ? 1.0 - smoothstep(holeR - 0.14, holeR, ragged) : 0.0;
                float cover = dark * (1.0 - hole) * _Covered;

                // In the dark, smoke barely moving: just enough that it is not a flat black card.
                float3 colour = lerp(_Ink.rgb, _Smoke.rgb, saturate((n - 0.45) * 1.6) * 0.5);
                // The ragged edge of the dark catches a little light where it meets the world (lunge and tear only).
                float edge = (smoothstep(0.25, 0.0, abs(ragged - closeR)) * step(lunge, 0.999)
                            + smoothstep(0.2, 0.0, abs(ragged - holeR)) * step(0.001, _Open)) * (n2 * 0.8);
                colour = lerp(colour, _Smoke.rgb * 1.3, saturate(edge));

                // ITS EYES: huge as it comes in, settling to watching size, blinking, gone as you get your sight back.
                float settle = smoothstep(0.1, 0.6, _Elapsed);
                float size = lerp(2.4, 1.0, settle);
                float sep = lerp(0.55, 0.28, settle);
                float2 centre = float2(0.0, lerp(0.12, 0.1, settle)) + float2(sin(t * 0.7) * 0.02, sin(t * 1.1) * 0.012);
                float blinkPhase = frac(_Elapsed * 0.9 + 0.35);
                float blink = _Calm > 0.5 ? 1.0 : 1.0 - smoothstep(0.0, 0.04, 0.06 - abs(blinkPhase - 0.5)) * step(0.3, _Elapsed);
                float eyeOn = smoothstep(0.05, 0.2, _Elapsed) * lerp(1.0, 0.6, settle) * (1.0 - _Open) * blink;
                colour += _Eye.rgb * eyes(p, centre, sep, size) * eyeOn * cover;

                // A blind: opaque. ⚠️ THE CANVAS BLENDS IN LINEAR: 5 % of the court left under a 0.95 cover comes back as
                // about a quarter of its brightness on screen.
                float alpha = cover;

                // THE SLOW WEARING OFF: a smoky black edge, thinner as it fades.
                float slowIn = 1.15 - 0.5 * _Slow;
                float slowEdge = smoothstep(slowIn, slowIn + 0.45, ragged) * saturate(_Slow * 1.6);
                colour = lerp(colour, _Ink.rgb, saturate(slowEdge * (1.0 - cover)));
                alpha = max(alpha, slowEdge * 0.95);
                return fixed4(colour, saturate(alpha) * i.color.a);
            }
            ENDCG
        }
    }
}
