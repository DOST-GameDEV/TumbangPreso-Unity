// BLINDFOLD, DON'T LOOK'S GHOST (Nemu, owner 2026-10-15), drawn from `Visual.BlindfoldGhostModel`'s vertex-coloured voxels.
//  * Unlit: a ghost gives off its own cold light, and the mockup's face shading is already baked into each face's colour.
//  * A faint icy rim where it turns away from the lens, so it reads as a ghost against any court (`_Rim`).
//  * Voxels flagged to glow (uv.x 1, its cyan pinpoints) are lifted past white so the grade's bloom catches them (`_Glow`).
//  * Its tail (uv.y, 0 at the hem to 1 at the tip) sways, more toward the tip (`_Sway`).
//  * `_Fade` fades the whole ghost as ONE body: a depth-only pass first, so a fading ghost never shows its own insides.
// ⚠️ Colours are authored in sRGB like every colour property here (Unity hands the shader their linear values).
Shader "TumbangPreso/BlindfoldGhost"
{
    Properties
    {
        _Rim ("Rim", Color) = (0.62, 0.84, 1, 1)
        _RimStrength ("Rim strength", Range(0, 2)) = 0.6
        _Glow ("Glow", Float) = 3
        _Fade ("Fade", Range(0, 1)) = 1
        _Flash ("Flash", Range(0, 1)) = 0
        _Sway ("Sway (metres at the tip)", Float) = 0.05
        _Clock ("Clock", Float) = 0
    }
    SubShader
    {
        Tags { "Queue" = "Transparent-10" "RenderType" = "Transparent" "IgnoreProjector" = "True" }
        CGINCLUDE
        #include "UnityCG.cginc"
        float _Sway, _Clock;
        float4 Sway(float4 vertex, float2 uv)
        {
            float tip = uv.y * uv.y;
            vertex.x += sin(_Clock * 4.3 - uv.y * 3.0) * _Sway * tip;
            vertex.z += sin(_Clock * 3.1 - uv.y * 2.0 + 1.3) * _Sway * 0.6 * tip;
            return vertex;
        }
        ENDCG
        Pass
        {
            ZWrite On ColorMask 0 Cull Back
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; float4 color : COLOR; };
            struct v2f { float4 pos : SV_POSITION; };
            v2f vert (appdata v) { v2f o; o.pos = UnityObjectToClipPos(Sway(v.vertex, v.uv)); return o; }
            fixed4 frag (v2f i) : SV_Target { return 0; }
            ENDCG
        }
        Pass
        {
            ZWrite Off ZTest LEqual Cull Back
            Blend SrcAlpha OneMinusSrcAlpha
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            fixed4 _Rim;
            float _RimStrength, _Glow, _Fade, _Flash;
            struct appdata { float4 vertex : POSITION; float3 normal : NORMAL; float2 uv : TEXCOORD0; float4 color : COLOR; };
            struct v2f { float4 pos : SV_POSITION; float4 color : COLOR; float2 uv : TEXCOORD0; float3 normal : TEXCOORD1; float3 view : TEXCOORD2; };
            v2f vert (appdata v)
            {
                v2f o;
                float4 local = Sway(v.vertex, v.uv);
                o.pos = UnityObjectToClipPos(local);
                o.color = v.color; o.uv = v.uv;
                o.normal = UnityObjectToWorldNormal(v.normal);
                o.view = normalize(WorldSpaceViewDir(local));
                return o;
            }
            fixed4 frag (v2f i) : SV_Target
            {
                float3 colour = i.color.rgb;
                float rim = pow(1.0 - saturate(dot(normalize(i.normal), normalize(i.view))), 2.0);
                colour += _Rim.rgb * rim * _RimStrength * (1.0 - i.uv.x);
                colour = lerp(colour, colour * _Glow, i.uv.x);
                colour = lerp(colour, _Rim.rgb * 1.6, _Flash);
                return fixed4(colour, i.color.a * _Fade);
            }
            ENDCG
        }
    }
}
