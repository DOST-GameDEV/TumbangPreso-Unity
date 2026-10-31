// MAGMA FIST'S FIST OF FIRE (owner 2026-10-29: "a more fieryy design, like its not a solid opaque fist, but u can make it out to
// be a fist still"). `giant_fist_flame` (`tools/build_sean_magma_fist.py`) is only its SHAPE; this draws it three times:
//   0 THE CORE: the shape drawn a little inside itself, dense fire in painted bands, with the finger lines laid into its front
//     in deep red, so the fist reads at a glance. Its strength is the variant's (`SeanFireFistLook.Variant`).
//   1 THE INNER FLAME: the shape pushed out along its normals, turning one way.
//   2 THE OUTER FLAME: pushed out further, turning the other way, its upward faces (the knuckles' tops, the thumb's back) licking
//     up in tongues.
// BUILT FROM FIRE THE OWNER HAS PASSED, NOT INVENTED (`docs/VFX_BENCH_METHOD.md` sections 1a and 10.1):
//   * the inside is his recipe and Bastion b8b's construction (`FxStudyBastionColumns.shader`): the streak picture stood up,
//     scrolled UP at two speeds and multiplied by itself, so the inside keeps changing without anything popping, at two layers
//     with their own speeds and widths;
//   * soft ragged sides from the cloud map drifting upward, and eaten from the TOP by the cloud map, each part at its own height;
//   * Bastion's soft painted bands (deep red, red-orange, orange, warm yellow), plus a thin near-white heart only in the densest
//     spots of the core, never a broad area;
//   * Bastion's daylight answer: premultiplied, with a FLOOR of cover (`_Cover`), never pure additive, because additive alone
//     goes pale on the sunlit court with no bloom, and too much cover is the "flashing sticker" he turned down.
// BIRTH AND DEATH: `_Form` 0 is a column of fire roaring up out of the pool; it resolves into the fist as `_Form` reaches 1.
// `_Tear` 0 to 1 tears it apart from the top into tongues while it sinks back into the pool (the sink is the transform's).
// Nothing fades a solid: every part is eaten by the cloud map. GPU only, a static mesh: three numbers a frame per fist.
Shader "TumbangPreso/SeanFireFist"
{
    Properties
    {
        _NoiseA ("Streaks (erode_streak)", 2D) = "grey" {}
        _NoiseB ("Cloud (erode_cloud)", 2D) = "grey" {}
        _Age ("Clock (s, set per fist)", Float) = 0
        _Form ("0 a column of fire, 1 the fist", Range(0,1)) = 1
        _Tear ("Torn from the top (0 whole, 1 gone)", Range(0,1)) = 0
        _CoreShrink ("Core drawn this far inside the shape (m, object)", Float) = 0.015
        _CoreGain ("Core density", Range(0,2)) = 0.9
        _CoreCover ("Core: share of its colour that covers", Range(0,1)) = 0.6
        _ShellA ("Inner flame out from the shape (m, object)", Float) = 0.035
        _ShellB ("Outer flame out from the shape (m, object)", Float) = 0.075
        _ShellAlpha ("Flame layers: how much they show", Range(0,1)) = 0.85
        _Cover ("Flame layers: share of colour that covers", Range(0,1)) = 0.5
        _Height ("The shape's height (m, object)", Float) = 1.06
    }
    CGINCLUDE
    #include "UnityCG.cginc"
    sampler2D _NoiseA, _NoiseB;
    float _Age, _Form, _Tear, _CoreShrink, _CoreGain, _CoreCover, _ShellA, _ShellB, _ShellAlpha, _Cover, _Height;
    struct appdata { float4 vertex:POSITION; float3 normal:NORMAL; };
    struct v2f { float4 pos:SV_POSITION; float4 uv:TEXCOORD0; float3 wn:TEXCOORD1; float3 wp:TEXCOORD2; };
    float grey(float s)
    {
        #ifdef UNITY_COLORSPACE_GAMMA
        return s;
        #else
        return LinearToGammaSpaceExact(s);
        #endif
    }
    float A(float2 q) { return grey(tex2D(_NoiseA, q).r); }
    float B(float2 q) { return grey(tex2D(_NoiseB, q).r); }
    // Bastion's soft painted bands, and above them a thin near-white heart for the densest spots only.
    float3 Paint(float d)
    {
        const float s = 0.05;
        float3 c = float3(0.55, 0.07, 0.03);
        c = lerp(c, float3(0.90, 0.20, 0.05), smoothstep(0.30 - s, 0.30 + s, d));
        c = lerp(c, float3(1.00, 0.42, 0.08), smoothstep(0.55 - s, 0.55 + s, d));
        c = lerp(c, float3(1.00, 0.70, 0.26), smoothstep(0.80 - s, 0.80 + s, d));
        return lerp(c, float3(1.00, 0.93, 0.72), smoothstep(0.95, 0.97, d));
    }

    v2f Shape(appdata v, float layer)
    {
        v2f o;
        float3 p = v.vertex.xyz, n = normalize(v.normal);
        // The column it roars up as: every point drawn in to a round column round the shape's (forward-leaning) axis, a
        // little taller, then let go into the fist as `_Form` reaches 1.
        float2 axis = float2(0.0, 0.14 * saturate(p.y / _Height));
        float2 rad = p.xz - axis;
        float r = max(1e-3, length(rad));
        float form = smoothstep(0.0, 1.0, _Form);
        p.xz = axis + lerp(rad / r * 0.26, rad, form);
        p.y *= lerp(1.2, 1.0, form);
        n = normalize(lerp(float3(rad.x / r, 0.0, rad.y / r), n, form));
        float ang = atan2(p.x, p.z) / 6.2832 + 0.5;
        float spin = layer < 0.5 ? 0.0 : (layer < 1.5 ? 0.05 : -0.08);
        float2 fc = float2(ang + spin * _Age, p.y);
        // Tongues: the shells' upward faces lick up, unevenly and differently every moment.
        float lick = grey(tex2Dlod(_NoiseB, float4(fc.x * 3.0 + layer * 0.37, p.y * 0.8 - _Age * 1.4, 0, 0)).r);
        float reach = layer < 0.5 ? 0.0 : (layer < 1.5 ? 0.10 : 0.22);
        float up = saturate(n.y + 0.25) * reach * (0.35 + lick) * (1.0 + 0.6 * _Tear);
        float push = layer < 0.5 ? -_CoreShrink : (layer < 1.5 ? _ShellA : _ShellB);
        p += n * push + float3(0.0, up, 0.0);
        o.pos = UnityObjectToClipPos(float4(p, 1.0));
        o.uv = float4(fc, v.vertex.x, n.z);
        o.wn = UnityObjectToWorldNormal(n);
        o.wp = mul(unity_ObjectToWorld, float4(p, 1.0)).xyz;
        return o;
    }

    float4 Burn(v2f i, float layer)
    {
        float a = i.uv.x, y = i.uv.y, h = saturate(y / _Height);
        float facing = saturate(dot(normalize(i.wn), normalize(_WorldSpaceCameraPos - i.wp)));
        float seed = layer * 0.41;
        // His recipe: the streaks stood up, scrolled up at two speeds and two widths, multiplied by itself.
        float s1 = A(float2(y * 0.55 - _Age * (1.2 + 0.3 * layer), a * 2.0 + seed));
        float s2 = A(float2(y * 0.38 - _Age * (2.3 + 0.4 * layer), a * 3.0 + 0.4 + seed));
        float streak = saturate(s1 * s2 * 2.8);
        // Eaten from the top, ragged and moving; the tear brings the line down through it.
        float cut = B(float2(a * 2.0 + seed * 5.0, y * 0.7 - _Age * 1.1));
        float line_ = lerp(layer < 0.5 ? 1.5 : 1.2, -0.25, _Tear);
        float top = 1.0 - smoothstep(line_ - 0.45, line_, h + 0.45 * (cut - 0.5));
        float d, alpha, cover;
        if (layer < 0.5)
        {
            // THE CORE: dense, hottest low and in the middle, with the finger lines laid into its front in deep red.
            float flow = B(float2(a * 3.0, y * 1.1 - _Age * 0.9)) * B(float2(a * 5.0 + 0.3, y * 1.9 - _Age * 1.7)) * 2.4;
            d = (0.45 + 0.35 * saturate(flow) + 0.25 * streak + 0.15 * facing) * _CoreGain * top;
            float x = i.uv.z;
            float lines = min(min(abs(x + 0.148), abs(x)), abs(x - 0.148));
            float front = step(0.35, i.uv.w) * step(0.55, y) * step(y, 0.98) * smoothstep(0.0, 1.0, _Form);
            d = lerp(d, min(d, 0.32), front * (1.0 - smoothstep(0.012, 0.024, lines)));
            alpha = smoothstep(0.08, 0.35, d);
            cover = _CoreCover;
        }
        else
        {
            // A FLAME SHELL: soft ragged sides (thin where it is seen edge-on), the streaks for its body.
            float edge = B(float2(a * 2.4 + seed, y * 0.8 - _Age * 0.8));
            float body = smoothstep(0.08, 0.6, facing + 0.5 * (edge - 0.5));
            d = body * top * (0.18 + 0.75 * streak) * (layer < 1.5 ? 0.95 : 0.75);
            alpha = smoothstep(0.06, 0.5, d) * _ShellAlpha;
            cover = _Cover;
        }
        d = saturate(d);
        clip(alpha - 0.01);
        return float4(Paint(d) * alpha, alpha * cover);
    }
    ENDCG
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Blend One OneMinusSrcAlpha
        Pass
        {
            ZWrite On
            Cull Back
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.5
            v2f vert(appdata v) { return Shape(v, 0.0); }
            float4 frag(v2f i) : SV_Target { return Burn(i, 0.0); }
            ENDCG
        }
        Pass
        {
            ZWrite Off
            Cull Back
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.5
            v2f vert(appdata v) { return Shape(v, 1.0); }
            float4 frag(v2f i) : SV_Target { return Burn(i, 1.0); }
            ENDCG
        }
        Pass
        {
            ZWrite Off
            Cull Back
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.5
            v2f vert(appdata v) { return Shape(v, 2.0); }
            float4 frag(v2f i) : SV_Target { return Burn(i, 2.0); }
            ENDCG
        }
    }
    Fallback Off
}
