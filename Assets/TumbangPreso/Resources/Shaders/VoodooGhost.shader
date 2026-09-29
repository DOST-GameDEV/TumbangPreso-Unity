// ⚠️⚠️ THE VICTIM'S SOUL, PULLED OUT OF THEM (HERO-10 v3, 2026-09-29, film v20). The owner on v19's soul draw: *"this animation
// dont look that good yet"*, *"lock in thoroughly improve"*. v17 to v19 drew the pull as blobs of coloured smoke round the victim, which
// read as a smear. The read every soul-steal shares is a GHOST OF THE VICTIM, their own shape, see-through and lit, dragged out of
// their body toward the one taking it. This is that ghost: the victim's own skinned meshes baked each frame (`VoodooSoulDraw`) and
// drawn with this shader.
//
//  * A lit RIM (fresnel) over a faint fill: a ghost is its outline, and a rim reads on the sunny court where a flat tint does not.
//  * `_Pull` (world direction, toward her doll) and `_Stretch` (metres): the ghost SMEARS toward her, most at its far side, and the
//    smear is broken by noise so it looks torn from the body rather than slid.
//  * Bands of light crawl along it toward her (`_Flow`), so even a still frame says which way it is going.
//  * `_Alpha` fades it in and out; `_Dissolve` eats it away from the far end (the yank into the doll).
// Loaded through Resources (`Shaders/VoodooGhost`), which keeps it in the player.
Shader "TumbangPreso/VoodooGhost"
{
    Properties
    {
        _Color ("Colour", Color) = (1, 0.22, 0.3, 1)
        _Pull ("Pull (world direction)", Vector) = (0, 0, 1, 0)
        _Stretch ("Stretch (m)", Float) = 0.3
        _Origin ("Body centre (world)", Vector) = (0, 1, 0, 0)
        _Alpha ("Alpha", Float) = 1
        _Dissolve ("Dissolve", Float) = 0
        _Flow ("Flow speed", Float) = 2.2
    }
    SubShader
    {
        Tags { "Queue" = "Transparent+11" "RenderType" = "Transparent" "IgnoreProjector" = "True" }
        Pass
        {
            ZWrite Off
            ZTest LEqual
            Cull Back
            Blend SrcAlpha OneMinusSrcAlpha

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            float4 _Color, _Pull, _Origin;
            float _Stretch, _Alpha, _Dissolve, _Flow;

            struct appdata { float4 vertex : POSITION; float3 normal : NORMAL; };
            struct v2f { float4 pos : SV_POSITION; float3 world : TEXCOORD0; float3 normal : TEXCOORD1; float along : TEXCOORD2; };

            float hash(float3 p) { return frac(sin(dot(p, float3(12.9898, 78.233, 37.719))) * 43758.5453); }
            float noise(float3 p)
            {
                float3 i = floor(p), f = frac(p);
                float3 u = f * f * (3.0 - 2.0 * f);
                float a = lerp(lerp(hash(i), hash(i + float3(1, 0, 0)), u.x), lerp(hash(i + float3(0, 1, 0)), hash(i + float3(1, 1, 0)), u.x), u.y);
                float b = lerp(lerp(hash(i + float3(0, 0, 1)), hash(i + float3(1, 0, 1)), u.x), lerp(hash(i + float3(0, 1, 1)), hash(i + float3(1, 1, 1)), u.x), u.y);
                return lerp(a, b, u.z);
            }

            v2f vert(appdata v)
            {
                v2f o;
                float3 world = mul(unity_ObjectToWorld, v.vertex).xyz;
                float3 n = normalize(mul((float3x3)unity_ObjectToWorld, v.normal));
                float3 pull = normalize(_Pull.xyz + float3(0, 1e-4, 0));
                // How far toward her this point already is (-1 the back of the body, +1 the side facing her).
                float side = dot(normalize(world - _Origin.xyz + 1e-4), pull);
                float torn = noise(world * 3.1 + _Time.y * 1.3);
                float smear = _Stretch * saturate(side * 0.5 + 0.5) * (0.35 + 0.65 * torn);
                world += pull * smear;
                o.world = world;
                o.normal = n;
                o.along = dot(world - _Origin.xyz, pull);
                o.pos = mul(UNITY_MATRIX_VP, float4(world, 1.0));
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float3 view = normalize(_WorldSpaceCameraPos - i.world);
                float rim = pow(1.0 - saturate(abs(dot(normalize(i.normal), view))), 1.5);
                float bands = 0.5 + 0.5 * sin(i.along * 9.0 - _Time.y * _Flow * 6.0);
                float grain = noise(i.world * 7.0 - _Time.y * 0.9);
                // Eaten away from the far end when it is yanked in (or lets go).
                float keep = step(_Dissolve, saturate(0.55 - i.along * 0.35 + grain * 0.45));
                float a = (0.07 + rim * 0.95 + bands * 0.1) * _Alpha * keep;
                float3 rgb = _Color.rgb * (1.2 + rim * 1.8 + bands * 0.5) + rim * 0.25;
                return fixed4(rgb, saturate(a));
            }
            ENDCG
        }
    }
}
