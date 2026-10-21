// ⚠️⚠️ THE CIRCLE AS LIGHT (HERO-10 v3, 2026-09-29). The owner on the first circle (stitched lines against the sky): *"improve magic
// circle it looks underwhelming af it doesnt feel like an ult"*, and *"pls use genshin reference and other ult cutscenes"*. What the
// references do that the lines did not (Castorice's summoning, Nahida's shrine, Raiden's emblem; research.md section 5): the circle is
// a SOURCE OF LIGHT with many layers turning against each other, not an outline. This is the whole circle on one disc, procedural, so
// every layer is crisp at any size:
//
//   r 0.97-1.07  TEETH       a ring of jagged fangs biting outward, turning with the outer rim
//   r 0.95       OUTER RIM   her crimson, thick and hot, sewn round by `_Reveal`, with a bloom halo
//   r 0.80-0.88  RUNE BAND   a band of stitched glyphs (procedural strokes per cell) crawling the other way, embers
//   r 0.74       STAR        an eight-point star {8/3} in crimson, inside a violet ring, turning slowly
//   r 0.62       STITCHES    a ring of X stitches
//   r < 0.50     THE VOID    a dark swirling hole with violet currents, the circle's inside
//   centre       THE EYE     an almond eye opening (`_Eye`), violet lids, a burning ember slit that twitches (`_Look`)
//
// Premultiplied alpha (`Blend One OneMinusSrcAlpha`): the void and the teeth are DARK and cover the sky, the lines are HDR light that
// blooms. So it reads as a hole torn in the sky with a burning rim, in the night of the cutscene and in daylight in play alike.
// Everything moves from script uniforms (`_Spin`, `_Phase`), never `_Time`, because the cutscene poses from its own clock.
// Loaded through Resources (`Shaders/VoodooCircle`), which keeps it in the player.
Shader "TumbangPreso/VoodooCircle"
{
    Properties
    {
        _Crimson ("Crimson", Color) = (1, 0.18, 0.28, 1)
        _Violet ("Violet", Color) = (0.72, 0.36, 1, 1)
        _Ember ("Ember", Color) = (1, 0.52, 0.2, 1)
        _Reveal ("Reveal (rims sewn)", Float) = 1
        _Inner ("Inner reveal (runes, star, stitches)", Float) = 1
        _Eye ("Eye open", Float) = 1
        _Look ("Pupil offset", Float) = 0
        _LookY ("Pupil offset, across", Float) = 0
        _Seam ("The shut seam showing", Float) = 0
        _Tear ("Seam stitches snapped", Float) = 0
        _Spin ("Spin (radians)", Float) = 0
        _Phase ("Phase", Float) = 0
        _Glow ("Glow", Float) = 2.6
        _Alpha ("Alpha", Float) = 1
        _Parts ("Layers drawn: rim, runes, star and stitches, the void and eye", Vector) = (1, 1, 1, 1)
        _Fangs ("Fangs on the rim", Float) = 1
    }
    SubShader
    {
        Tags { "Queue" = "Transparent+9" "RenderType" = "Transparent" "IgnoreProjector" = "True" }
        Pass
        {
            ZWrite Off
            ZTest LEqual
            Cull Off
            Blend One OneMinusSrcAlpha

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            float4 _Crimson, _Violet, _Ember;
            float _Reveal, _Inner, _Eye, _Look, _LookY, _Seam, _Tear, _Spin, _Phase, _Glow, _Alpha, _Fangs;
            float4 _Parts;

            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct v2f { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; };

            v2f vert(appdata v) { v2f o; o.pos = UnityObjectToClipPos(v.vertex); o.uv = v.uv; return o; }

            #define TAU 6.2831853
            float hash(float n) { return frac(sin(n * 12.9898) * 43758.5453); }
            float ring(float r, float at, float w) { return saturate(1.0 - abs(r - at) / w); }
            float line2(float2 p, float2 a, float2 b, float w)
            {
                float2 pa = p - a, ba = b - a;
                float h = saturate(dot(pa, ba) / dot(ba, ba));
                return saturate(1.0 - length(pa - ba * h) / w);
            }
            // 0..1 along the circle from its seam, for the sewing sweep.
            float around(float a) { return frac(a / TAU + 1.0); }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 p = (i.uv - 0.5) * 2.0;
                float r = length(p);
                if (r > 1.12) discard;
                float a = atan2(p.y, p.x);
                float3 col = 0;
                float dark = 0;

                // --- the outer rim, sewn round by _Reveal (clockwise), with its halo and its teeth.
                float aOut = a + _Spin;
                float sewn = step(1.0 - around(-aOut), _Reveal);
                float halo = exp(-pow((r - 0.95) / 0.13, 2.0)) * 0.45 * _Reveal;
                col += _Crimson.rgb * halo;
                col += _Crimson.rgb * 3.2 * ring(r, 0.95, 0.022) * sewn;
                col += _Violet.rgb * 1.8 * ring(r, 0.905, 0.007) * sewn;
                float teeth = 46.0;
                float t = frac(aOut * teeth / TAU);
                float fang = 0.975 + 0.095 * (1.0 - abs(t * 2.0 - 1.0));
                float inFang = step(0.975, r) * step(r, fang) * sewn * _Fangs;
                dark += inFang * 0.9;
                col += _Crimson.rgb * 1.6 * inFang * saturate(1.0 - (fang - r) / 0.02);
                // v12 (the owner on v11: *"the circle itself looks flatly drawn and basic"*): each part can be drawn on a disc of its own,
                // stacked at depths that turn against each other (`SkyCircle`), so every part is kept apart and weighed by `_Parts`.
                float3 cRim = col; float dRim = dark; col = 0; dark = 0;

                // --- the rune band, crawling the other way: glyph cells between two thin rings.
                float aRune = a - _Spin * 2.2;
                float inner = saturate(_Inner);
                float band = step(0.80, r) * step(r, 0.875);
                col += _Violet.rgb * 1.6 * (ring(r, 0.80, 0.005) + ring(r, 0.875, 0.005)) * inner;
                float cells = 40.0;
                float cf = aRune * cells / TAU;
                float ci = floor(cf);
                float2 cu = float2(frac(cf), (r - 0.80) / 0.075);
                float h1 = hash(ci), h2 = hash(ci + 7.1), h3 = hash(ci + 13.7);
                float g = 0;
                g += step(0.25, h1) * saturate(1.0 - abs(cu.x - 0.35) / 0.09) * step(0.15, cu.y) * step(cu.y, 0.85);
                g += step(0.4, h2) * saturate(1.0 - abs(cu.y - 0.5) / 0.1) * step(0.2, cu.x) * step(cu.x, 0.8);
                g += step(0.5, h3) * saturate(1.0 - abs((cu.x - 0.2) - (cu.y - 0.15) * 0.8) / 0.1) * step(0.15, cu.y) * step(cu.y, 0.85);
                float cellOn = step(ci / cells - floor(ci / cells), inner);
                col += _Ember.rgb * 2.4 * saturate(g) * band * inner * cellOn;
                dark += band * 0.35 * inner;
                float3 cRune = col; float dRune = dark; col = 0; dark = 0;

                // --- the star {8/3} in its violet ring, turning slowly.
                float aStar = _Spin * 0.6;
                float starOn = saturate(inner * 1.4 - 0.2);
                col += _Violet.rgb * 2.2 * ring(r, 0.74, 0.008) * starOn;
                float s = 0;
                [unroll] for (int k = 0; k < 8; k++)
                {
                    float a0 = aStar + k * TAU / 8.0, a1 = aStar + (k + 3) * TAU / 8.0;
                    s = max(s, line2(p, float2(cos(a0), sin(a0)) * 0.74, float2(cos(a1), sin(a1)) * 0.74, 0.008));
                }
                col += _Crimson.rgb * 2.4 * s * starOn;

                // --- a ring of X stitches.
                float xc = 16.0;
                float xf = (a + _Spin * 0.3) * xc / TAU;
                float2 xu = float2(frac(xf) - 0.5, (r - 0.62) / 0.05);
                float xs = max(saturate(1.0 - abs(xu.x * 2.6 - xu.y * 0.9) / 0.18), saturate(1.0 - abs(xu.x * 2.6 + xu.y * 0.9) / 0.18))
                           * step(abs(xu.y), 0.9);
                col += _Crimson.rgb * 1.8 * xs * inner;
                float3 cStar = col; float dStar = dark; col = 0; dark = 0;

                // --- the void: an ABYSS (v12; v11's lavender disc with black blotches read as flat paint). Near black, deepening to its
                // middle; thin filaments of light spiral in along a logarithmic spiral and are swallowed; specks of light fall inward;
                // a hot lip where the sky is torn.
                float voidMask = saturate((0.5 - r) / 0.04);
                float lr = log(max(r, 0.004));
                float spiral = frac((a / TAU) * 3.0 + lr * 1.6 + _Phase * 0.35);
                float filament = saturate(1.0 - abs(spiral - 0.5) / 0.035) * saturate((r - 0.06) / 0.2) * saturate((0.5 - r) / 0.1);
                float spiral2 = frac(-(a / TAU) * 2.0 + lr * 2.3 + _Phase * 0.22);
                float filament2 = saturate(1.0 - abs(spiral2 - 0.5) / 0.02) * saturate((r - 0.1) / 0.2) * saturate((0.5 - r) / 0.12);
                col += _Violet.rgb * 0.34 * filament * voidMask + _Crimson.rgb * 0.22 * filament2 * voidMask;
                float speckCell = floor(a / TAU * 60.0 + 30.0);
                float fall = frac(hash(speckCell) + _Phase * (0.25 + 0.3 * hash(speckCell + 2.0)));
                float speck = saturate(1.0 - abs(r - 0.5 * (1.0 - fall)) / 0.008) * saturate(1.0 - abs(frac(a / TAU * 60.0) - 0.5) / 0.12);
                col += float3(1.0, 0.75, 0.9) * 0.6 * speck * step(0.6, hash(speckCell + 5.0)) * voidMask * fall;
                col += _Crimson.rgb * 1.6 * ring(r, 0.5, 0.012) * inner + _Crimson.rgb * 0.5 * exp(-pow((r - 0.47) / 0.03, 2.0)) * inner;
                col += _Violet.rgb * 0.9 * ring(r, 0.515, 0.006) * inner;
                dark += voidMask * 0.97;

                // --- the eye: almond lids opening, a burning slit that twitches. v5 (film v4: *"the eye in the void does not show"*, it
                // was a thin ring in the dark): nearly the void's width, thick hot lids with a halo and X stitches across them, the
                // inside burning from the iris out, so it reads from under the circle and from across the court.
                float eyeW = 0.46;
                float bulge = saturate(1.0 - pow(p.x / eyeW, 2.0));
                float lid = 0.27 * saturate(_Eye) * bulge;
                float onEye = step(abs(p.x), eyeW) * step(0.01, _Eye);
                float inEye = onEye * step(abs(p.y), lid);
                float fromLid = abs(abs(p.y) - lid);
                col += _Violet.rgb * 2.6 * saturate(1.0 - fromLid / 0.026) * onEye;
                col += _Crimson.rgb * 0.9 * exp(-pow(fromLid / 0.06, 2.0)) * onEye * (1.0 - inEye);
                float stitchAt = abs(frac(p.x / eyeW * 3.5 + 0.5) - 0.5);
                float stitch = saturate(1.0 - stitchAt / 0.08) * step(fromLid, 0.05) * onEye * step(0.3, bulge);
                col += _Crimson.rgb * 3.0 * stitch;
                // v8 (film v7: the open eye read as a flat pink symbol): a DARK bloodshot white with red veins running in to the iris,
                // a burning ember iris, and a BLACK slit pupil with a hot rim. It is dark where a symbol would be bright.
                float2 q = p - float2(_Look * 0.14, _LookY * 0.06 * saturate(_Eye));
                float qr = length(q);
                float qa = atan2(q.y, q.x);
                float vein = saturate(1.0 - abs(frac(qa * 3.0 + sin(qr * 40.0) * 0.08) - 0.5) / 0.05) * saturate((qr - 0.17) / 0.1);
                // (`col` is multiplied by `_Glow`, about 2.6, below: the white is kept near black before it.)
                col = lerp(col, _Crimson.rgb * 0.05, inEye);
                col += _Crimson.rgb * 0.55 * vein * inEye;
                col += _Crimson.rgb * 0.5 * inEye * saturate(1.0 - fromLid / 0.04);
                float irisIn = saturate((0.17 - qr) / 0.01) * inEye;
                // v9: the iris is kept under the bloom's threshold, or its glow floods the dark white red again (film v9).
                col = lerp(col, lerp(_Ember.rgb * 0.9, _Crimson.rgb * 0.45, saturate(qr / 0.17)), irisIn);
                col += _Ember.rgb * 0.7 * ring(qr, 0.17, 0.014) * inEye;
                // A cat's slit: wide at its middle, pinched to points at the lids.
                float slitW = 0.085 * saturate(1.0 - pow(q.y / max(0.001, lid * 0.86), 2.0));
                float slit = saturate(1.0 - abs(q.x) / slitW) * step(abs(q.y), lid * 0.86) * irisIn;
                float slitRim = saturate(1.0 - abs(abs(q.x) - slitW) / 0.012) * step(abs(q.y), lid * 0.86) * irisIn;
                col = lerp(col, float3(0.0, 0.0, 0.0), step(0.2, slit));
                col += float3(1.0, 0.7, 0.35) * 0.8 * slitRim;
                dark += inEye * 0.9;

                // --- THE SEAM (cutscene v7): before it opens, the eye is a stitched split in the sky, red light leaking out between
                // black thread stitches that snap one by one (`_Tear`); the lids then peel apart from it.
                // v12: a JAGGED TEAR, not a line: its edge zigzags, it gapes wider as the stitches go (`_Tear`), a white-hot core with
                // crimson round it, light spiking out across it, and thick black thread stitches holding it shut.
                float seamOn = saturate(_Seam) * step(abs(p.x), eyeW) * (1.0 - saturate(_Eye * 3.0));
                float along = p.x / eyeW;
                float taper = saturate(1.0 - along * along);
                float zig = (frac(along * 7.0 + hash(floor(along * 7.0)) * 0.3) - 0.5) * 0.025 + sin(along * 23.0) * 0.006;
                float gape = (0.006 + 0.028 * saturate(_Tear)) * taper;
                float seamY = p.y - zig * taper;
                float seamFrom = abs(seamY);
                float inside = saturate(1.0 - seamFrom / max(0.002, gape));
                col += float3(1.0, 0.85, 0.75) * 2.2 * inside * seamOn;
                col += _Crimson.rgb * seamOn * (2.2 * saturate(1.0 - seamFrom / (gape + 0.02)) + 1.3 * exp(-seamFrom / 0.06)) * taper;
                float spikeCell = floor(along * 14.0 + 7.0);
                float spike = saturate(1.0 - abs(frac(along * 14.0) - 0.5) / 0.05) * step(0.55, hash(spikeCell + 11.0));
                col += _Crimson.rgb * 1.4 * spike * exp(-seamFrom / (0.05 + 0.08 * hash(spikeCell))) * seamOn * taper;
                float sk = floor((along * 0.5 + 0.5) * 9.0);
                float sl = frac((along * 0.5 + 0.5) * 9.0) - 0.5;
                float held = step(_Tear, hash(sk + 3.7));
                float reach = 0.05 + gape;
                float xst = max(saturate(1.0 - abs(sl * 2.2 - seamY / reach * 0.9) / 0.3), saturate(1.0 - abs(sl * 2.2 + seamY / reach * 0.9) / 0.3))
                            * step(abs(seamY), reach) * step(0.2, taper);
                col *= 1.0 - xst * held * seamOn;
                dark += xst * held * seamOn;

                float3 cCore = col; float dCore = dark;
                col = cRim * _Parts.x + cRune * _Parts.y + cStar * _Parts.z + cCore * _Parts.w;
                dark = dRim * _Parts.x + dRune * _Parts.y + dStar * _Parts.z + dCore * _Parts.w;

                col *= _Glow;
                float light = saturate(max(col.r, max(col.g, col.b)) * 0.35);
                float alpha = saturate(max(dark, light)) * _Alpha;
                return fixed4(col * _Alpha, alpha);
            }
            ENDCG
        }
    }
}
