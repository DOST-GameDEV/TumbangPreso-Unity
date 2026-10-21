// NEMU'S PET GHOSTS (owner 2026-10-19: the mint mouse, the cocoa bear, the butter owl and its lantern), drawn from the painted
// models `tools/build_nemu_ghosts_simple.py --final` writes. Behind `Visual.PetGhostModels.PetGhosts`, which is OFF: until the
// owner approves them in game, ASTRA's voxel ghosts (`Shaders/BlindfoldGhost`) are what plays.
// It keeps everything `BlindfoldGhost` does, and takes its colour from the model's atlas instead of vertex colours:
//  * `_MainTex`: the atlas. Its upper half is the paint, its lower half the flat cells (eyes, nose, cloth).
//  * The atlas's ALPHA is the glow mask: 0 on the glow cell (the lantern's glass), lifted for the bloom (`_Glow`).
//  * LIT LIKE THE CAST (court film v1 read them brighter and flatter than the people beside them): two toon bands from the
//    scene's own sun (`_WorldSpaceLightPos0`, so this colour pass is `ForwardBase`), the shade band a cool step darker.
//  * The rim where it turns from the lens (`_Rim`), and the code's `_Flash`.
//  * The tail sways (`_Sway`), weighted by how far BELOW `_TailTop` (metres, model space) a vertex is, and TRAILS behind
//    the way it flies (`_Trail`, model space, set from its flight each frame): the beads lag, the body does not.
//  * An ink line round the silhouette like the cast's (`_Outline`, metres in model space).
//  * `_Fade` fades it as ONE body: a depth-only pass first, so a fading ghost never shows its own insides.
Shader "TumbangPreso/PetGhost"
{
    Properties
    {
        _MainTex ("Atlas", 2D) = "white" {}
        _Rim ("Rim", Color) = (1, 1, 1, 1)
        _RimStrength ("Rim strength", Range(0, 2)) = 0.14
        _Glow ("Glow", Float) = 1.7
        _Fade ("Fade", Range(0, 1)) = 1
        _Flash ("Flash", Range(0, 1)) = 0
        _Sway ("Sway (metres at the tip)", Float) = 0.05
        _Clock ("Clock", Float) = 0
        _TailTop ("Tail top (metres)", Float) = 0
        _Trail ("Trail (model space, metres at the tip)", Vector) = (0, 0, 0, 0)
        _Outline ("Outline (metres)", Float) = 0.007
        _Ink ("Ink", Color) = (0.07, 0.055, 0.11, 1)
        _ShadeTint ("Shade", Color) = (0.70, 0.66, 0.78, 1)
    }
    SubShader
    {
        Tags { "Queue" = "Transparent-10" "RenderType" = "Transparent" "IgnoreProjector" = "True" }
        CGINCLUDE
        #include "UnityCG.cginc"
        float _Sway, _Clock, _TailTop;
        float4 _Trail;
        float4 Sway(float4 vertex)
        {
            float along = _TailTop > 0.001 ? saturate((_TailTop - vertex.y) / _TailTop) : 0.0;
            float tip = along * along;
            vertex.x += sin(_Clock * 4.3 - along * 3.0) * _Sway * tip;
            vertex.z += sin(_Clock * 3.1 - along * 2.0 + 1.3) * _Sway * 0.6 * tip;
            vertex.xyz += _Trail.xyz * tip;
            return vertex;
        }
        ENDCG
        Pass
        {
            ZWrite On ColorMask 0 Cull Back
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            struct appdata { float4 vertex : POSITION; };
            struct v2f { float4 pos : SV_POSITION; };
            v2f vert (appdata v) { v2f o; o.pos = UnityObjectToClipPos(Sway(v.vertex)); return o; }
            fixed4 frag (v2f i) : SV_Target { return 0; }
            ENDCG
        }
        Pass
        {
            // The ink line: the back faces pushed out along their normals, seen only round the edge.
            ZWrite Off ZTest LEqual Cull Front
            Blend SrcAlpha OneMinusSrcAlpha
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            float _Outline, _Fade, _Flash;
            fixed4 _Ink, _Rim;
            struct appdata { float4 vertex : POSITION; float3 normal : NORMAL; };
            struct v2f { float4 pos : SV_POSITION; };
            v2f vert (appdata v)
            {
                v2f o;
                float4 local = Sway(v.vertex);
                local.xyz += normalize(v.normal) * _Outline;
                o.pos = UnityObjectToClipPos(local);
                return o;
            }
            fixed4 frag (v2f i) : SV_Target { return fixed4(lerp(_Ink.rgb, _Rim.rgb, _Flash), _Fade); }
            ENDCG
        }
        Pass
        {
            Tags { "LightMode" = "ForwardBase" }
            ZWrite Off ZTest LEqual Cull Back
            Blend SrcAlpha OneMinusSrcAlpha
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            sampler2D _MainTex;
            fixed4 _Rim, _ShadeTint;
            float _RimStrength, _Glow, _Fade, _Flash;
            struct appdata { float4 vertex : POSITION; float3 normal : NORMAL; float2 uv : TEXCOORD0; };
            struct v2f { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; float3 normal : TEXCOORD1; float3 view : TEXCOORD2; };
            v2f vert (appdata v)
            {
                v2f o;
                float4 local = Sway(v.vertex);
                o.pos = UnityObjectToClipPos(local);
                o.uv = v.uv;
                o.normal = UnityObjectToWorldNormal(v.normal);
                o.view = normalize(WorldSpaceViewDir(local));
                return o;
            }
            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 paint = tex2D(_MainTex, i.uv);
                float glow = 1.0 - paint.a;
                float3 n = normalize(i.normal);
                float3 sun = _WorldSpaceLightPos0.w < 0.5 && dot(_WorldSpaceLightPos0.xyz, _WorldSpaceLightPos0.xyz) > 0.01
                    ? normalize(_WorldSpaceLightPos0.xyz) : normalize(float3(0.3, 1.0, 0.2));
                // Two bands, a soft edge between them, as the cast's toon draws a lit side and a shade side.
                float lit = smoothstep(0.02, 0.14, dot(n, sun));
                float3 light = lerp(_ShadeTint.rgb, float3(1.0, 1.0, 1.0), lit) * 0.94;
                float3 colour = paint.rgb * lerp(light, float3(1.0, 1.0, 1.0), glow);
                float rim = pow(1.0 - saturate(dot(n, normalize(i.view))), 3.0);
                colour += _Rim.rgb * rim * _RimStrength * (1.0 - glow);
                colour = lerp(colour, colour * _Glow, glow);
                colour = lerp(colour, _Rim.rgb * 1.6, _Flash);
                return fixed4(colour, _Fade);
            }
            ENDCG
        }
    }
}
