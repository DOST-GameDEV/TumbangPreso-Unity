// ⚠️⚠️ PAETE CHANNELLING THE MOUNTAIN (HERO-9, owner, 2026-09-26 night: *"HE GOES TO THE GHHROUND AND HIS ROOTS CONNECT TO IT
// AND HE IS CHANNELLING HIS POWER AND HE GLOWS AND SHIT"*). direction.md section 5.14, beat 3.
//
// A second drawing of his OWN body, added on top of it (one shell renderer that skins the same mesh on the same bones,
// `PaeteChannelGlow`), unlit and ADDITIVE, so it can only brighten him. It is not a sticker and not a halo round him:
//  * his VINES light up. The light rides the parts of him that are already living wood: palette slot 4 (the green vines
//    wound round his arms and trunk) strongest, slot 7 (the lit moss) after it, the eye slots fully. `_SlotGlow0..3` hold the
//    sixteen weights, so the bark (13 to 15) stays nearly dark and the silhouette keeps his bark read;
//  * an EDGE LIGHT in her jade on everything, so from any angle he reads as a thing full of light, not a lamp in a box;
//  * a SWEEP: only what is ABOVE `_SweepY` (world metres) is lit, with a brighter front at the line, and the driver moves the
//    line DOWN him from his eyes to his hands: the light she gave him going down through him into the ground;
//  * a PULSE: a narrow bright band at `_PulseY` that the driver runs from his shoulders to his palms three times, quickening
//    (the heartbeat of the channel); each one ends in a ring of light in the court (drawn by the scene, not here).
// `_Strength` 0 draws nothing at all. The palette cell rule is `TumbangPreso/Toon`'s (glTF rows arrive flipped: Unity rows
// 0 to 7). Loaded through Resources (`Shaders/SpiritVeins`), which is what keeps it in the player (`ToonSkin`'s rule).
Shader "TumbangPreso/SpiritVeins"
{
    Properties
    {
        _GlowColor ("Glow", Color) = (0.85, 1.0, 0.42, 1)
        _RimColor ("Edge", Color) = (0.62, 1.0, 0.70, 1)
        _Strength ("Strength", Float) = 0
        _Rim ("Edge strength", Float) = 0.55
        _SweepY ("Lit above (world y)", Float) = -1000
        _SweepSoft ("Sweep softness (m)", Float) = 0.22
        _PulseY ("Pulse band (world y)", Float) = -1000
        _PulseStrength ("Pulse strength", Float) = 0
        _PulseWidth ("Pulse width (m)", Float) = 0.16
        _SlotGlow0 ("Slot weights 0 to 3", Vector) = (0.18, 0.10, 0.22, 0.14)
        _SlotGlow1 ("Slot weights 4 to 7", Vector) = (1.00, 0.30, 0.06, 0.55)
        _SlotGlow2 ("Slot weights 8 to 11", Vector) = (0.00, 0.00, 1.00, 1.00)
        _SlotGlow3 ("Slot weights 12 to 15", Vector) = (0.50, 0.05, 0.03, 0.08)
    }
    SubShader
    {
        Tags { "Queue" = "Transparent+10" "RenderType" = "Transparent" "IgnoreProjector" = "True" }
        Pass
        {
            ZWrite Off
            ZTest LEqual
            Cull Back
            // The shell is the body's own surface: pulled a hair toward the lens so it wins the depth test against it.
            Offset -1, -1
            Blend One One

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            fixed4 _GlowColor, _RimColor;
            float _Strength, _Rim, _SweepY, _SweepSoft, _PulseY, _PulseStrength, _PulseWidth;
            float4 _SlotGlow0, _SlotGlow1, _SlotGlow2, _SlotGlow3;

            struct appdata { float4 vertex : POSITION; float3 normal : NORMAL; float2 uv : TEXCOORD0; };
            struct v2f { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; float3 normal : TEXCOORD1; float3 world : TEXCOORD2; };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.normal = UnityObjectToWorldNormal(v.normal);
                o.world = mul(unity_ObjectToWorld, v.vertex).xyz;
                return o;
            }

            float SlotWeight(int slot)
            {
                float4 row = slot < 4 ? _SlotGlow0 : slot < 8 ? _SlotGlow1 : slot < 12 ? _SlotGlow2 : _SlotGlow3;
                int k = slot - (slot / 4) * 4;
                return k == 0 ? row.x : k == 1 ? row.y : k == 2 ? row.z : row.w;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                if (_Strength <= 0.001) discard;
                float2 cell = floor(clamp(i.uv, 0.0, 0.9999) * 16.0);
                int col = (int)cell.x, row = (int)cell.y;
                int slot = (col / 2) + (row <= 3 ? 8 : 0);
                float w = SlotWeight(slot);

                float3 n = normalize(i.normal);
                float3 v = normalize(_WorldSpaceCameraPos - i.world);
                float rim = pow(1.0 - saturate(dot(n, v)), 2.2);

                // Lit above the sweep line, with a bright front AT the line as it travels down him.
                float lit = smoothstep(_SweepY - _SweepSoft, _SweepY + _SweepSoft, i.world.y);
                float d = (i.world.y - _SweepY) / max(0.02, _SweepSoft);
                float front = exp(-d * d) * 0.9;
                float p = (i.world.y - _PulseY) / max(0.02, _PulseWidth);
                float pulse = _PulseStrength * exp(-p * p);

                float3 c = _GlowColor.rgb * (w * (0.85 * lit + front) + pulse * (0.35 + w))
                         + _RimColor.rgb * rim * _Rim * (0.35 + 0.65 * lit);
                return fixed4(c * _Strength, 0.0);
            }
            ENDCG
        }
    }
    FallBack Off
}
