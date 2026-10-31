// THE CANDLE OF CURSE: DRAIN (`PhaisterDrainLook`): their stamina as a flame burning on their head.
//
// ONE STATIC MESH, ANIMATED HERE. Three teardrop skins turned on a lathe and built once (`PhaisterDrainLook.BuildFlame`), told
// apart by the vertex colour's alpha: outer violet skin, magenta body, hot heart. Every skin's outline is pushed about in the
// vertex program by waves that climb it at their own speeds, so its edge never slides as one rigid shape (the owner of an early
// fire: "all of the flame moves as one. it doesnt look like flame at all"), and each is coloured in the fragment program from two
// noise pictures panned at two speeds and multiplied, through a few value bands. Nothing here is a flat fill.
//
// IT COVERS AS WELL AS GLOWS. Purely added light vanishes on a sunlit wall, so the outer skin lays down real dark violet: on
// the pale court the flame is a dark shape with a bright heart, on the dark bench a bright shape with a dark rim.
//
// TIME IS `_Age`, nothing reads `_Time`: the film steps it outside Play and a paused game holds it.
Shader "TumbangPreso/PhaisterDrainFlame"
{
    Properties
    {
        _Age ("Clock (s)", Float) = 0
        _NoiseTex ("Cells", 2D) = "white" {}
        _CloudTex ("Erosion", 2D) = "white" {}
        _Lean ("Which way the tip is drawn (world, m)", Vector) = (0,0,0,0)
        _Eaten ("Eaten: 0 whole, 1 gone", Range(0,1)) = 0
        _Spent ("0 a live flame, 1 a dying ember's colours", Range(0,1)) = 0
        _Deep ("Outer skin", Color) = (0.17,0.035,0.30,1)
        _Body ("Body", Color) = (0.62,0.06,0.62,1)
        _Hot ("Hot", Color) = (1.0,0.28,0.78,1)
        _Core ("Heart", Color) = (1.0,0.86,0.96,1)
    }
    SubShader
    {
        Tags { "Queue"="Transparent+8" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Pass
        {
            Cull Off
            ZWrite Off
            Blend One OneMinusSrcAlpha
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #include "UnityCG.cginc"
            sampler2D _NoiseTex, _CloudTex;
            float _Age, _Eaten, _Spent;
            float4 _Lean;
            fixed4 _Deep, _Body, _Hot, _Core;
            struct appdata { float4 vertex:POSITION; float3 normal:NORMAL; float2 uv:TEXCOORD0; fixed4 colour:COLOR; };
            struct v2f { float4 pos:SV_POSITION; float2 uv:TEXCOORD0; float3 n:TEXCOORD1; float3 view:TEXCOORD2; float layer:TEXCOORD3; };
            float grey(float s)
            {
                #ifdef UNITY_COLORSPACE_GAMMA
                return s;
                #else
                return LinearToGammaSpaceExact(s);
                #endif
            }
            v2f vert(appdata v)
            {
                v2f o;
                float layer = v.colour.a;        // 0 outer, .5 body, 1 heart
                float up = v.uv.y;               // 0 foot, 1 tip
                float turn = v.uv.x * 6.2832;
                float speed = lerp(1.0, 1.55, layer);
                // Tongues: lobes round the flame that climb and change. Two counts of lobe at two speeds, more toward the tip.
                float a = sin(turn * 3.0 + layer * 2.1 - _Age * 7.3 * speed + up * 5.0);
                float b = sin(turn * 5.0 - layer * 3.7 + _Age * 5.1 * speed + up * 7.5);
                float grow = up * (1.0 - up * 0.35);
                float3 p = v.vertex.xyz;
                float c3 = sin(turn * 2.0 + layer * 5.3 - _Age * 4.1 * speed + up * 9.0);
                p.xz *= 1.0 + (0.46 * a + 0.30 * b + 0.28 * c3) * grow * 2.1;
                // Height flickers, and the outer skin's tip licks higher than the heart's.
                p.y *= 1.0 + 0.10 * sin(_Age * 9.1 + layer * 4.0) + 0.06 * sin(_Age * 14.3 + layer * 1.3);
                // The whole sways as a slow S that climbs it, the tip furthest.
                p.x += (0.11 * sin(_Age * 3.7 - up * 3.1 + layer) + 0.05 * sin(_Age * 8.3 - up * 6.0)) * up;
                p.z += (0.11 * cos(_Age * 3.1 - up * 2.7 + layer * 1.7)) * up;
                float4 world = mul(unity_ObjectToWorld, float4(p, 1.0));
                world.xyz += _Lean.xyz * up * up;
                o.pos = mul(UNITY_MATRIX_VP, world);
                o.uv = v.uv;
                o.layer = layer;
                o.n = UnityObjectToWorldNormal(v.normal);
                o.view = _WorldSpaceCameraPos - world.xyz;
                return o;
            }
            fixed4 frag(v2f i) : SV_Target
            {
                float up = i.uv.y;
                float layer = i.layer;
                float2 at = float2(i.uv.x * 5.0 + layer * 0.37, up * 0.55 - _Age * lerp(0.75, 1.2, layer));
                float2 at2 = float2(i.uv.x * 3.0 - layer * 0.53, up * 0.45 - _Age * lerp(0.4, 0.65, layer));
                // A trail texture panned and multiplied by itself at two speeds (the owner's fire recipe).
                float n = grey(tex2D(_NoiseTex, at).r);
                float c = grey(tex2D(_CloudTex, at2).r);
                float flow = saturate(n * 1.1 + 0.05) * saturate(c * 1.5);
                // How deep the eye looks through the skin: the middle of a shape is thick, its edge is thin.
                float thick = abs(dot(normalize(i.n), normalize(i.view)));
                thick = thick * thick * (3.0 - 2.0 * thick);
                // The tip tears into cells: toward it more of the surface is empty and what is left is thinner.
                float open = lerp(-0.12, 0.62, up * up) + _Eaten * 1.25;
                float keep = smoothstep(open, open + 0.16, flow + 0.28 * thick);
                float footFade = smoothstep(0.0, 0.10, up);
                // THE SILHOUETTE IS PAINTED, NOT THE MESH'S: thin skin is eaten by the flow, so the edge is licks that climb. A skin
                // that ran to its mesh edge read as a dark teardrop round a sticker on the pale floor.
                float lick = smoothstep(0.52, 0.62, thick * 0.95 + (flow - 0.30) * 2.3 - up * 0.55);
                float alpha, glow;
                fixed3 col;
                if (layer < 0.25)
                {
                    // The outer violet skin: dark, covering, a little see-through, magenta where it is thickest.
                    col = lerp(_Deep.rgb * 2.4, _Body.rgb, smoothstep(0.12, 0.42, flow * thick + 0.25 * (1.0 - up)));
                    alpha = keep * footFade * lick * lerp(0.55, 0.9, thick);
                    glow = 0.0;
                }
                else if (layer < 0.75)
                {
                    // The body: bands from magenta to hot pink, following the flow.
                    float v = flow * (0.35 + 0.65 * thick);
                    // Three bands, wine to magenta to hot, and the hot one only in the running streaks: a flat pink was a sticker.
                    col = lerp(_Deep.rgb * 1.5, _Body.rgb, smoothstep(0.10, 0.20, v));
                    col = lerp(col, _Hot.rgb, smoothstep(0.34, 0.44, v));
                    alpha = keep * footFade * lick * smoothstep(0.12, 0.5, thick) * smoothstep(0.03, 0.14, v) * 0.95;
                    glow = 0.20;
                }
                else
                {
                    // The heart: small and nearly white. White belongs in small hearts, never in broad areas.
                    float v = flow * thick;
                    col = lerp(_Hot.rgb, _Core.rgb, smoothstep(0.34, 0.50, v));
                    alpha = keep * footFade * smoothstep(0.35, 0.7, thick) * smoothstep(0.06, 0.16, v);
                    glow = 0.35;
                }
                // Spent: an ember is a dull wine, no white and no glow.
                col = lerp(col, lerp(_Deep.rgb, _Body.rgb, 0.45 + 0.4 * layer), _Spent);
                glow *= 1.0 - _Spent;
                return fixed4(col * alpha * (1.0 + glow), alpha);
            }
            ENDCG
        }
    }
    Fallback Off
}
