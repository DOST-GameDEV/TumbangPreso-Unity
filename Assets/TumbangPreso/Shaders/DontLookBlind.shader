// BLINDED, THE VICTIM'S OWN SCREEN (Nemu's DON'T LOOK, owner 2026-10-15: *"the ghost would lunge at their eyes and cover it for
// 2 secondns and slow them for 5 seconds (the slow effect gets lower overtime)"*). Driven from the replicated BLINDED clock
// (`UI.TumpHudEffects.TickBlinded`), nothing stored.
//
// ⚠️⚠️ IT IS THE GHOST THEY JUST SAW, FROM INSIDE. Owner: *"dapat blindfold matches the vfx ren sa screen"*. The ghost on the court
// is Blindfold (`Visual.BlindfoldGhostModel`): icy blue, a white cloth over its eyes that drops as it springs, hollow black eyes
// with cyan pinpoints, two fangs. So the victim's screen is that cloth over THEIR eyes and that face pressed against it:
//  * THE LUNGE (`_Elapsed` 0 to 0.22 s): the white cloth whips down from the top of the view, its lit hem leading.
//  * BLIND (to 1.7 s): the cloth from inside, cold navy, a fabric weave, a little icy light through it. Pressed against it,
//    the ghost's pale blocky face: its hollow square eyes with a cyan pinpoint each (huge as it hits, settling), and its
//    mouth with two fangs. The pinpoints flicker.
//  * THE SLIDE (`_Open`, 1.7 to 2.0 s): the cloth slides down off your eyes, the world coming back from the top.
//  * THE SLOW (`_Slow` 1 to 0 as it wears off): a frost edge round the screen, melting inward-out as the slow wears off.
// No hands and no smoke (rejected: *"it looks so fkn baddd"*); the darkness the owner approved stays the body of it.
// `_Calm` (Reduce visual effects) stills the cloth and the flicker; the blind itself stays (it is the rule).
// ⚠️ Colours are authored in sRGB like every colour property here (Unity hands the shader their linear values).
Shader "TumbangPreso/DontLookBlind"
{
    Properties
    {
        _MainTex ("Sprite Texture (unused, required by UGUI)", 2D) = "white" {}
        _Ink ("Inside the cloth", Color) = (0.035, 0.05, 0.1, 1)
        _Cloth ("Cloth", Color) = (0.94, 0.93, 0.95, 1)
        _Face ("Its face through the cloth", Color) = (0.42, 0.55, 0.72, 1)
        _Eye ("Pinpoint", Color) = (0.5, 0.95, 1, 1)
        _Frost ("Frost", Color) = (0.62, 0.84, 1, 1)
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
            fixed4 _Ink, _Cloth, _Face, _Eye, _Frost;
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

            // How far inside a voxel rectangle [lo, hi) (grid units) a point is, softened by `blur` (the cloth between).
            float cells(float2 g, float2 lo, float2 hi, float blur)
            {
                float2 d = max(lo - g, g - hi);
                return 1.0 - smoothstep(-blur, blur, max(d.x, d.y));
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float t = _Calm > 0.5 ? 0.0 : _Time.y;
                float2 p = (i.uv * 2.0 - 1.0) * float2(_Aspect, 1.0);

                // THE CLOTH'S EDGES, BILLOWING: its hem falls from above the top of the view to below the bottom in 0.22 s;
                // when the eyes are uncovered its top slides down off the bottom the same way.
                float billow = sin(p.x * 3.1 + t * 5.0) * 0.035 + (fbm(float2(p.x * 2.5, t * 1.3)) - 0.5) * 0.09;
                float lunge = saturate(_Elapsed / 0.22);
                float hem = lerp(1.2, -1.25, 1.0 - (1.0 - lunge) * (1.0 - lunge));
                float slide = _Open * _Open;
                float top = lerp(1.25, -1.3, slide);
                float y = p.y + billow;
                float below = smoothstep(hem - 0.015, hem + 0.015, y);
                float above = 1.0 - smoothstep(top - 0.015, top + 0.015, y);
                float cover = below * above * _Covered;

                // INSIDE THE CLOTH: cold navy, broad folds, a fine weave, a little icy light coming through from above.
                float folds = fbm(float2(p.x * 1.6 + billow * 3.0, p.y * 0.5 + t * 0.08));
                float3 colour = lerp(_Ink.rgb, _Ink.rgb * 2.4, folds * 0.6);
                // A fine weave of threads (lines, not dots: film r10's product of sines read as a dotted screen).
                float2 w = abs(frac(p * 90.0) - 0.5) * 2.0;
                float weave = max(smoothstep(0.6, 1.0, w.x), smoothstep(0.6, 1.0, w.y));
                colour *= 0.94 + weave * 0.1;
                float light = exp(-length((p - float2(0.0, 1.1)) / float2(1.6, 0.9)) * 1.4) * lerp(0.5, 0.25, saturate(_Elapsed / 1.5));
                colour += _Frost.rgb * light * 0.35;

                // ITS FACE, PRESSED AGAINST THE CLOTH: the ghost's own head, row for row (`BlindfoldGhostModel`, sprung). The
                // grid is its voxels: columns 0 to 7, its eyes (1-2 and 5-6) rows 11 to 13, its mouth row 10. Huge as it hits,
                // then settling.
                float settle = smoothstep(0.08, 0.7, _Elapsed);
                float vox = lerp(0.42, 0.17, settle);
                float2 centre = float2(sin(t * 0.9) * 0.015, lerp(0.15, 0.02, settle) + sin(t * 1.3) * 0.01);
                float2 g = (p - centre) / vox + float2(4.0, 12.0);
                float blur = 0.35 + 0.15 * sin(t * 2.0);
                float face = cells(g, float2(0.0, 9.0), float2(8.0, 14.0), 0.6);
                float hair = cells(g, float2(-0.5, 14.0), float2(8.5, 17.0), 0.6) + cells(g, float2(-0.5, 8.5), float2(0.0, 14.0), 0.6) + cells(g, float2(8.0, 8.5), float2(8.5, 14.0), 0.6);
                float eyes = cells(g, float2(1.0, 11.0), float2(3.0, 14.0), blur) + cells(g, float2(5.0, 11.0), float2(7.0, 14.0), blur);
                float mouth = cells(g, float2(2.0, 10.0), float2(6.0, 11.0), blur);
                float fangs = (cells(g, float2(2.0, 10.0), float2(3.0, 10.6), blur) + cells(g, float2(5.0, 10.0), float2(6.0, 10.6), blur));
                float faceOn = smoothstep(0.04, 0.2, _Elapsed) * (1.0 - _Open);
                colour = lerp(colour, _Face.rgb * (0.92 + weave * 0.08) * (0.9 + folds * 0.3), saturate(face) * 0.6 * faceOn);
                colour = lerp(colour, _Ink.rgb * 0.5, saturate(hair) * 0.5 * faceOn);
                colour = lerp(colour, float3(0.0, 0.0, 0.0), saturate(eyes + mouth) * 0.92 * faceOn);
                colour = lerp(colour, _Cloth.rgb * 0.7, saturate(fangs) * 0.8 * faceOn);
                // A cyan pinpoint in each eye, glowing through, flickering like a candle.
                float flicker = _Calm > 0.5 ? 1.0 : 0.8 + 0.2 * sin(t * 23.0) * sin(t * 7.0 + 1.0);
                float pin = 0.0;
                for (int e = 0; e < 2; e++)
                {
                    // A point of light inside a hollow eye, not a lit window: small core, tight glow.
                    float2 at = float2(e == 0 ? 2.5 : 5.5, 12.5);
                    float d = length(g - at);
                    pin += cells(g, at - 0.3, at + 0.3, 0.1) * 1.3 + exp(-d * 3.5) * 0.45;
                }
                colour += _Eye.rgb * pin * flicker * faceOn;

                // THE CLOTH'S LIT HEM, leading as it whips down, and its top edge as it slides away.
                float hemLine = (1.0 - smoothstep(0.0, 0.06, abs(y - hem))) * step(lunge, 0.999)
                              + (1.0 - smoothstep(0.0, 0.06, abs(y - top))) * step(0.001, _Open);
                float fray = smoothstep(0.35, 0.75, noise(float2(p.x * 40.0, 3.0)));
                colour = lerp(colour, _Cloth.rgb * (0.7 + fray * 0.3), saturate(hemLine) * cover);

                // A blind: opaque. ⚠️ THE CANVAS BLENDS IN LINEAR: 5 % of the court left under a 0.95 cover comes back as
                // about a quarter of its brightness on screen.
                float alpha = saturate(cover + saturate(hemLine) * _Covered * 0.9);

                // THE SLOW WEARING OFF: frost round the edge of the view, crystals reaching in, melting back as it fades.
                float r = length(p / float2(_Aspect * 0.8, 1.0));
                float crystals = fbm(p * 7.0) * 0.6 + fbm(p * 19.0 + 4.0) * 0.4;
                float edgeR = r + (crystals - 0.5) * 0.35;
                float slowIn = 1.2 - 0.45 * _Slow;
                float frost = smoothstep(slowIn, slowIn + 0.35, edgeR) * saturate(_Slow * 1.6);
                float3 frostColour = lerp(_Frost.rgb * 0.55, _Frost.rgb, smoothstep(0.55, 0.8, crystals));
                colour = lerp(colour, frostColour, saturate(frost * (1.0 - cover)));
                alpha = max(alpha, frost * 0.8);
                return fixed4(colour, saturate(alpha) * i.color.a);
            }
            ENDCG
        }
    }
}
