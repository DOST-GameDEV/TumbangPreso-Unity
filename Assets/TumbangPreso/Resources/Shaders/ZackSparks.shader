// A burst of Zack's forked sparks (`FxStudyZackBank`), drawn as ONE prebuilt mesh with ONE shared material.
//
// WHY. The first Bank Shot sample flew six sparks as six pieces, each its own object and material, posed every frame on
// the CPU. A bank happens a few times a throw and a load fizzles out once, so this is the same few sparks over and over,
// and the owner's standing rule for game effects is that they are animated in the shader on a static mesh and measured.
// So a burst is built ONCE: each spark is a quad whose four corners all carry that spark's whole flight (which way it is
// thrown, how hard, how long it lives, which picture of the sheet it wears). Nothing about a spark is set from outside
// but the moment the burst began (`_Born`) and the clock (`_Now`), both through a property block, and the renderer's own
// transform: it stands where the burst is, its +z the way the sparks are thrown back (the wall's face, or up).
//
// Per spark, in the mesh: normal = its throw in the burst's own space (m/s); uv = where its corner is in its picture;
// uv2 = (which corner across, which corner along: -1 or 1); colour.a = its life (s), colour.r = its delay (s).
// A spark is thrown fast and braked hard (most of its way in the first third of its life), then hangs and falls, and is
// laid along the way it is going and turned to the eye. It thins as it dies: the mask is cut harder, so it frays.
//
// ⚠️ THE MASKS ARE PAINTED IN PLAIN GREY AND THE PROJECT IS LINEAR; `grey()` undoes the darkening.
Shader "TumbangPreso/ZackSparks"
{
    Properties
    {
        _MaskTex ("Spark sheet (2 x 2)", 2D) = "black" {}
        _Deep ("Colour at the mask's dark grey", Color) = (1, 0.8, 0.1, 1)
        _Body ("Colour at the mask's mid grey", Color) = (1, 0.95, 0.28, 1)
        _Hot ("Colour at the mask's white", Color) = (1, 1, 0.9, 1)
        _Size ("Spark length, width (m)", Vector) = (0.30, 0.20, 0, 0)
        _Fall ("Fall (m/s/s)", Float) = 7
        _Add ("Additive (1 light only, 0 covers)", Range(0, 1)) = 0.85
        _Gain ("Brightness", Range(0, 4)) = 1.4
        _Now ("Clock (s, per renderer)", Float) = 0
        _Born ("When the burst began (s, per renderer)", Float) = -100
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Blend One OneMinusSrcAlpha
        ZWrite Off
        Cull Off
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            sampler2D _MaskTex;
            fixed4 _Deep, _Body, _Hot;
            float4 _Size;
            float _Fall, _Add, _Gain, _Now, _Born;
            struct appdata { float4 vertex:POSITION; float3 normal:NORMAL; float2 uv:TEXCOORD0; float2 corner:TEXCOORD1; fixed4 colour:COLOR; };
            struct v2f { float4 vertex:SV_POSITION; float2 uv:TEXCOORD0; float fade:TEXCOORD1; };
            float grey(float sampled)
            {
                #ifdef UNITY_COLORSPACE_GAMMA
                return sampled;
                #else
                return LinearToGammaSpaceExact(sampled);
                #endif
            }
            v2f vert(appdata v)
            {
                v2f o;
                float life = max(0.05, v.colour.a), since = _Now - _Born - v.colour.r, u = since / life;
                float alive = step(0.0, u) * step(u, 1.0);
                float far = 1.0 - (1.0 - saturate(u)) * (1.0 - saturate(u));
                float3 origin = mul(unity_ObjectToWorld, float4(0, 0, 0, 1)).xyz;
                float3 throwW = mul((float3x3)unity_ObjectToWorld, v.normal);
                float s = max(0.0, since);
                float3 head = origin + throwW * (life * 0.55 * far) + float3(0, -0.5 * _Fall * s * s, 0);
                float3 going = throwW * (1.0 - 0.8 * saturate(u)) + float3(0, -_Fall * s, 0);
                float3 toEye = normalize(_WorldSpaceCameraPos - head);
                float3 along = normalize(going + 1e-4);
                float3 side = normalize(cross(along, toEye) + 1e-4);
                // Laid along the way it goes: its picture's x is along, from tail (-1) to head (+1).
                float3 p = head + along * (v.corner.y * 0.5 - 0.5) * _Size.x + side * v.corner.x * 0.5 * _Size.y;
                // A spark that is not alive is folded to nothing where it would be.
                p = lerp(origin, p, alive);
                o.vertex = mul(UNITY_MATRIX_VP, float4(p, 1));
                o.uv = v.uv;
                o.fade = alive * (1.0 - saturate(u) * saturate(u));
                return o;
            }
            float4 frag(v2f i):SV_Target
            {
                float m = grey(tex2D(_MaskTex, i.uv).r);
                // A dying spark loses its thin ragged parts first: it frays, it does not shrink.
                float cut = lerp(0.55, 0.10, i.fade);
                float alpha = smoothstep(cut, cut + 0.30, m) * step(0.001, i.fade);
                float3 colour = lerp(_Deep.rgb, _Body.rgb, smoothstep(0.10, 0.45, m));
                colour = lerp(colour, _Hot.rgb, smoothstep(0.62, 0.92, m));
                return float4(colour * alpha * _Gain, alpha * (1.0 - _Add));
            }
            ENDCG
        }
    }
    Fallback Off
}
