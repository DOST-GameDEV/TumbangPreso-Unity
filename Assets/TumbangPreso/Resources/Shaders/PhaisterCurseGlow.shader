// THE CURSE LIGHTING A BODY FROM WITHIN (`PhaisterCurseGlow`). Owner: "add a purple glow inside/to the character".
//
// The hero's shaders and textures are not touched: this draws the body's OWN meshes a second time, at exactly the same
// place, skinned by the same bones (no mesh is rebuilt). ⚠️ NOTHING IS INFLATED: "light from within is a hole, never a ridge"
// (`docs/HERO_KIT_METHOD.md`), so a shell pushed out along the normals, which would read as a raised ridge, is what this
// must not be. Instead light is strongest where the surface faces the eye, at the chest, and drops to nothing at the
// silhouette, so the body looks lit through from inside rather than outlined. It drifts upward through the bench's cloud
// map, so it is never a flat tint. ⚠️ THE MAIN PASS MULTIPLIES THE BODY'S OWN COLOUR: flat light added over black hair stained it
// purple and washed the torso to pink, so black stays black and a colour keeps its depth. Only a small heart at the chest adds.
Shader "TumbangPreso/PhaisterCurseGlow"
{
    Properties
    {
        _CloudTex ("Drift (erode_cloud)", 2D) = "gray" {}
        _Glow ("How strong, 0 to 1.5", Float) = 0
        _Age ("The effect's clock (s)", Float) = 0
        _Chest ("Where the heart of the light is (world), w its reach (m)", Vector) = (0,0.8,0,0.6)
        _Magenta ("Light", Color) = (0.92,0.16,0.95,1)
        _Violet ("Deep light", Color) = (0.42,0.06,0.86,1)
        _Hot ("Heart", Color) = (1.0,0.82,1.0,1)
    }
    SubShader
    {
        Tags { "Queue"="Transparent+5" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Pass
        {
            Cull Back
            ZWrite Off
            ZTest LEqual
            Offset -1, -1
            Blend DstColor OneMinusSrcAlpha
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #include "UnityCG.cginc"
            sampler2D _CloudTex;
            float _Glow, _Age;
            float4 _Chest;
            fixed4 _Magenta, _Violet, _Hot;
            struct appdata { float4 vertex:POSITION; float3 normal:NORMAL; };
            struct v2f { float4 pos:SV_POSITION; float3 world:TEXCOORD0; float3 normal:TEXCOORD1; };
            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.world = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.normal = UnityObjectToWorldNormal(v.normal);
                return o;
            }
            float G(float x) { return pow(saturate(x), 0.4545); }
            fixed4 frag(v2f i) : SV_Target
            {
                float3 view = normalize(_WorldSpaceCameraPos - i.world);
                float facing = saturate(dot(normalize(i.normal), view));
                // Lit through: full where it faces the eye, nothing at the silhouette (no outline, no ridge).
                float through = facing * facing * (3.0 - 2.0 * facing);
                float2 w = float2(i.world.x + i.world.z, i.world.y);
                float a = G(tex2D(_CloudTex, w * 0.9 + float2(0.0, -_Age * 0.45)).r);
                float b = G(tex2D(_CloudTex, w * 1.9 + float2(0.37, -_Age * 0.8)).r);
                float drift = saturate(a * 0.9 + b * 0.5 - 0.15);
                float d = length(i.world - _Chest.xyz) / max(_Chest.w, 0.05);
                float heart = exp(-d * d);
                float e = saturate(_Glow) * through * smoothstep(0.15, 0.85, drift) * (0.16 + 0.84 * heart);
                fixed3 c = lerp(_Violet.rgb, _Magenta.rgb, saturate(e * 1.8));
                float cover = saturate(e * 1.1);
                // dst * (c * 1.5 * cover + 1 - cover): pulls the body's colour toward her violet and lifts it a little.
                return fixed4(c * 1.5 * cover, cover);
            }
            ENDCG
        }
        Pass
        {
            // The heart: real added light, tight to the chest, so it can never reach the hair.
            Cull Back
            ZWrite Off
            ZTest LEqual
            Offset -1, -1
            Blend One One
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #include "UnityCG.cginc"
            float _Glow;
            float4 _Chest;
            fixed4 _Magenta;
            struct appdata { float4 vertex:POSITION; float3 normal:NORMAL; };
            struct v2f { float4 pos:SV_POSITION; float3 world:TEXCOORD0; float3 normal:TEXCOORD1; };
            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.world = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.normal = UnityObjectToWorldNormal(v.normal);
                return o;
            }
            fixed4 frag(v2f i) : SV_Target
            {
                float facing = saturate(dot(normalize(i.normal), normalize(_WorldSpaceCameraPos - i.world)));
                float d = length(i.world - _Chest.xyz) / max(_Chest.w * 0.6, 0.05);
                return fixed4(_Magenta.rgb * saturate(_Glow) * 0.28 * exp(-d * d) * facing * facing, 0);
            }
            ENDCG
        }
    }
}
