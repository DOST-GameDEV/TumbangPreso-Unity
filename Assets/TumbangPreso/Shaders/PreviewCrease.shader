// The cast's crease occlusion and the portraits' dark-tone lift, for the live character displays (`PreviewCrease`).
//
// A match gets its crease shade from `WorldOutline`'s CastAO pass, which needs the match's camera and look. A display
// has neither, so this is the same kernel (same probes, same two scales, same ceiling) as a small pass of its own:
//   0  the probes, from the camera's depth-normals texture, into one channel
//   1  the 4x4 depth-aware blur that cancels the probes' rotation tile and stops at the figure's edge
//   2  the composite: colour multiplied toward the cavity hue by the occlusion, alpha left alone
//   3  the lift: opens the darkest tones of the finished (graded) frame, in display space, alpha left alone
//
// Reached only by `Shader.Find`, so it is listed in GameBuilder.EnsureRuntimeShaders.
Shader "Hidden/TumbangPreso/PreviewCrease"
{
    Properties
    {
        _MainTex ("Source", 2D) = "white" {}
    }

    SubShader
    {
        Cull Off ZWrite Off ZTest Always

        CGINCLUDE
        #include "UnityCG.cginc"
        sampler2D _MainTex, _CreaseTex, _CameraDepthNormalsTexture;
        float4 _MainTex_TexelSize;
        // x the cast's share, y the kernel's radius in scene units, z its bias.
        float4 _CreaseParams;
        // x tan(fov / 2) times aspect, y tan(fov / 2), z the far plane.
        float4 _CreaseView;
        // The cavity hue in linear light, already scaled by the shade's depth.
        float4 _CreaseHue;
        // x the lift's knee, y its power.
        float4 _LiftParams;

        float EyeAt(float2 uv)
        {
            float depth; float3 normal;
            DecodeDepthNormal(tex2Dlod(_CameraDepthNormalsTexture, float4(uv, 0, 0)), depth, normal);
            return depth * _CreaseView.z;
        }

        bool IsFigure(float2 uv)
        {
            float depth; float3 normal;
            DecodeDepthNormal(tex2Dlod(_CameraDepthNormalsTexture, float4(uv, 0, 0)), depth, normal);
            return depth < .9999;
        }
        ENDCG

        // PASS 0. The probes.
        Pass
        {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag
            #pragma target 3.0

            half4 frag(v2f_img i) : SV_Target
            {
                float depth; float3 n;
                DecodeDepthNormal(tex2Dlod(_CameraDepthNormalsTexture, float4(i.uv, 0, 0)), depth, n);
                if (depth >= .9999) return 1;
                float eye = depth * _CreaseView.z;
                float3 p = float3((i.uv * 2 - 1) * _CreaseView.xy * eye, -eye);
                float radius = _CreaseParams.y;
                float bias = _CreaseParams.z + eye * .0015;
                float2 cell = fmod(floor(i.pos.xy), 4);
                float noise = (fmod((cell.x * 4 + cell.y) * 5, 16) + .5) / 16;
                float angle = noise * 6.2831853;
                float3 r = float3(cos(angle), sin(angle), 0);
                float3 t = normalize(r - n * dot(r, n)), b = cross(n, t);
                float fineHit = 0, fineAll = 0, broadHit = 0, broadAll = 0;
                [unroll] for (int k = 0; k < 12; k++)
                {
                    float phi = k * 2.3999632 + angle;
                    float elevation = lerp(.22, .75, frac(k * .618034 + noise));
                    float ring = (fmod(k, 4) + .5) / 4;
                    float3 dir = (t * cos(phi) + b * sin(phi)) * cos(elevation) + n * sin(elevation);
                    float weight = 1 - ring * .5;
                    float3 probe = p + dir * (radius * lerp(.14, 1.0, ring));
                    float2 uv = (probe.xy / -probe.z) / _CreaseView.xy * .5 + .5;
                    float sceneZ = -EyeAt(uv);
                    float range = smoothstep(0, 1, radius / max(abs(p.z - sceneZ), 1e-4));
                    fineHit += step(probe.z + bias, sceneZ) * range * weight; fineAll += weight;
                }
                [unroll] for (int m = 0; m < 8; m++)
                {
                    float phi = m * 2.3999632 + angle + 1.7;
                    float elevation = lerp(.45, 1.05, frac(m * .618034 + noise));
                    float ring = (fmod(m, 4) + .5) / 4;
                    float3 dir = (t * cos(phi) + b * sin(phi)) * cos(elevation) + n * sin(elevation);
                    float weight = 1 - ring * .4;
                    float wide = radius * 2.6;
                    float3 probe = p + dir * (wide * lerp(.25, 1.0, ring));
                    float2 uv = (probe.xy / -probe.z) / _CreaseView.xy * .5 + .5;
                    float sceneZ = -EyeAt(uv);
                    float range = smoothstep(0, 1, wide / max(abs(p.z - sceneZ), 1e-4));
                    broadHit += step(probe.z + bias * 2, sceneZ) * range * weight; broadAll += weight;
                }
                float fine = saturate(fineHit / max(fineAll, 1e-4) * 3.6);
                float broad = saturate(broadHit / max(broadAll, 1e-4) * 2.4);
                float shade = min(.62, 1 - (1 - fine * .75) * (1 - broad * .6));
                return half4(1 - shade, 0, 0, 1);
            }
            ENDCG
        }

        // PASS 1. The blur.
        Pass
        {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag
            #pragma target 3.0

            half4 frag(v2f_img i) : SV_Target
            {
                if (!IsFigure(i.uv)) return 1;
                float centre = EyeAt(i.uv), sum = 0, weight = 0;
                [unroll] for (int y = -2; y <= 1; y++)
                [unroll] for (int x = -2; x <= 1; x++)
                {
                    // Whole texels, so any 4x4 window of the 4-periodic tile holds each rotation once.
                    float2 uv = i.uv + float2(x, y) * _MainTex_TexelSize.xy;
                    float rel = abs(EyeAt(uv) - centre) / max(centre, 1e-3);
                    float w = exp(-(rel / .02) * (rel / .02));
                    sum += tex2Dlod(_MainTex, float4(uv, 0, 0)).r * w; weight += w;
                }
                return half4(sum / max(weight, 1e-4), 0, 0, 1);
            }
            ENDCG
        }

        // PASS 2. The composite.
        Pass
        {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag
            #pragma target 3.0

            half4 frag(v2f_img i) : SV_Target
            {
                half4 c = tex2D(_MainTex, i.uv);
                float amount = saturate((1 - tex2D(_CreaseTex, i.uv).r) * _CreaseParams.x);
                c.rgb *= lerp(float3(1, 1, 1), _CreaseHue.rgb, amount);
                return c;
            }
            ENDCG
        }

        // PASS 3. The lift: a curve on brightness below the knee only, hue kept, true black kept.
        Pass
        {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag
            #pragma target 3.0

            half4 frag(v2f_img i) : SV_Target
            {
                half4 c = tex2D(_MainTex, i.uv);
                float3 g = LinearToGammaSpace(c.rgb);
                float level = max(g.r, max(g.g, g.b));
                if (c.a <= 0 || level <= .001 || level >= _LiftParams.x) return c;
                float gain = pow(level / _LiftParams.x, _LiftParams.y) * _LiftParams.x / level;
                c.rgb = GammaToLinearSpace(g * gain);
                return c;
            }
            ENDCG
        }
    }
}
