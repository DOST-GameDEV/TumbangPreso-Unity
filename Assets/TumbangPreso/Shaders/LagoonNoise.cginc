// Shared helpers for the Lagoon Cove sample map's shaders (LagoonRock, LagoonGround,
// LagoonFoliage). Everything here exists to reproduce, in Unity's built-in pipeline, what the
// Blender materials in tools/author_lagoon_cove.py and tools/render_lagoon_texture_preview.py
// compute with their node trees.
//
// ⚠️ BLENDER AXES, ON PURPOSE. The Blender materials read noise and texture coordinates in
// Blender's world space. A Unity world position (x, y, z) came FROM Blender (-x, -z, y) (the
// export maps Blender (x, y, z) to Unity (-x, z, -y)), so `ToBlender` turns it back and every
// noise pattern and every top-down projection lands where the Blender renders put it. Without
// it the ground's anti-tiling patches and the rocks' edge breakups would be mirrored copies.
#ifndef LAGOON_NOISE_INCLUDED
#define LAGOON_NOISE_INCLUDED

inline float3 ToBlender(float3 unityPos) { return float3(-unityPos.x, -unityPos.z, unityPos.y); }

// A float-only hash (no integer ops, so it runs at shader target 3.0).
inline float3 LagoonHash33(float3 p)
{
    p = frac(p * float3(0.1031, 0.1030, 0.0973));
    p += dot(p, p.yxz + 33.33);
    return frac((p.xxy + p.yxx) * p.zyx) * 2.0 - 1.0;
}

// Gradient (Perlin) noise, signed, about -1..1.
// ⚠️ THE GRADIENTS ARE RESCALED TO LENGTH SQRT(2) AND THE RESULT BY 0.982, because that is what
// Blender's perlin does (its twelve edge gradients are (+-1, +-1, 0) permutations, and
// noise_scale3 is 0.9820). The Blender materials cut their masks at hand-tuned thresholds
// (0.42..0.58, 0.5..0.58); a noise with a narrower spread would never cross them and every
// breakup would vanish.
inline float LagoonGrad(float3 cell, float3 f)
{
    float3 g = LagoonHash33(cell);
    g = normalize(g + 1e-5) * 1.41421356;
    return dot(g, f);
}

float LagoonPerlin(float3 p)
{
    float3 i = floor(p);
    float3 f = p - i;
    float3 u = f * f * f * (f * (f * 6.0 - 15.0) + 10.0);
    float n000 = LagoonGrad(i, f);
    float n100 = LagoonGrad(i + float3(1, 0, 0), f - float3(1, 0, 0));
    float n010 = LagoonGrad(i + float3(0, 1, 0), f - float3(0, 1, 0));
    float n110 = LagoonGrad(i + float3(1, 1, 0), f - float3(1, 1, 0));
    float n001 = LagoonGrad(i + float3(0, 0, 1), f - float3(0, 0, 1));
    float n101 = LagoonGrad(i + float3(1, 0, 1), f - float3(1, 0, 1));
    float n011 = LagoonGrad(i + float3(0, 1, 1), f - float3(0, 1, 1));
    float n111 = LagoonGrad(i + float3(1, 1, 1), f - float3(1, 1, 1));
    float x00 = lerp(n000, n100, u.x), x10 = lerp(n010, n110, u.x);
    float x01 = lerp(n001, n101, u.x), x11 = lerp(n011, n111, u.x);
    return 0.982 * lerp(lerp(x00, x10, u.y), lerp(x01, x11, u.y), u.z);
}

// Blender's Noise Texture "Fac" output (normalize on, lacunarity 2, distortion 0): an fBm of
// floor(detail) + 1 octaves, the fractional octave blended in, mapped to 0..1 about 0.5.
// ⚠️ OCTAVES ARE CAPPED AT `maxOctaves`. The rock's "fine" and "grain" noises ask for detail 8
// and 10 (nine and eleven octaves) at 7 and 28 cycles a metre; past about the fifth octave the
// features are far below a pixel at any distance a player sees, so they only alias and cost.
float BlenderNoise(float3 p, float scale, float detail, float roughness, int maxOctaves)
{
    p *= scale;
    float fscale = 1.0, amp = 1.0, maxamp = 0.0, sum = 0.0;
    int n = min((int)floor(detail), maxOctaves - 1);
    [loop] for (int i = 0; i <= n; i++)
    {
        sum += LagoonPerlin(p * fscale) * amp;
        maxamp += amp;
        amp *= saturate(roughness);
        fscale *= 2.0;
    }
    float result = 0.5 * sum / maxamp + 0.5;
    float rmd = detail - floor(detail);
    if (rmd > 0.0 && (int)floor(detail) < maxOctaves)
    {
        float sum2 = sum + LagoonPerlin(p * fscale) * amp;
        float result2 = 0.5 * sum2 / (maxamp + amp) + 0.5;
        result = lerp(result, result2, rmd);
    }
    return result;
}

// Blender Map Range, linear (clamped) and smoothstep.
inline float MapRangeLinear(float v, float a, float b, float c, float d)
{
    return c + (d - c) * saturate((v - a) / (b - a));
}
inline float MapRangeSmooth(float v, float a, float b, float c, float d)
{
    return c + (d - c) * smoothstep(a, b, v);
}

// Euler XYZ rotation as Blender's Mapping node applies it (X first, then Y, then Z).
float3 RotateEulerXYZ(float3 v, float3 radians)
{
    float s, c;
    sincos(radians.x, s, c); v = float3(v.x, c * v.y - s * v.z, s * v.y + c * v.z);
    sincos(radians.y, s, c); v = float3(c * v.x + s * v.z, v.y, -s * v.x + c * v.z);
    sincos(radians.z, s, c); v = float3(c * v.x - s * v.y, s * v.x + c * v.y, v.z);
    return v;
}

// Blender Box projection (image texture, projection BOX): the X face reads (y, z), the Y face
// (x, z), the Z face (x, y), blended by the normal. `blend` is Blender's projection_blend.
float3 BoxWeights(float3 n, float blend)
{
    float3 w = abs(n);
    // ⚠️ A soft threshold rather than Blender's exact piecewise blend: at 0.45 the two agree
    // to within a few per cent of weight, and this form never produces a zero total.
    w = saturate(w - (0.577 - 0.5 * blend));
    w = w * w;
    return w / max(dot(w, 1.0), 1e-4);
}

#endif
