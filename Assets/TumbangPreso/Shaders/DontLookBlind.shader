// BLINDED, THE VICTIM'S OWN SCREEN (Nemu's DON'T LOOK, owner 2026-10-15: *"the ghost would lunge at their eyes and cover it for
// 2 secondns and slow them for 5 seconds (the slow effect gets lower overtime)"*). Driven from the replicated BLINDED clock
// (`UI.TumpHudEffects.TickBlinded`), nothing stored.
//
// ⚠️⚠️ THE GHOST ITSELF IS NOT DRAWN HERE. The real Blindfold model jumps into the victim's lens and clings there, alive
// (`Abilities.NemuBlindfold.PoseForLens`). Owner on v4, where this shader drew its face: *"looks wrong"*, *"bat parang
// slideshoww"*, *"wont it be nicer if the ghost jumps at u"*. (Before that, three rounds of hands: *"it looks so fkn baddd"*.)
// So this is only what is round it:
//  * THE LUNGE (`_Elapsed` 0 to 0.22 s): the dark rushes in from the edges as the ghost comes at the lens.
//  * BLIND (to 1.7 s): everything but the ghost's face is dark, a cold navy with a little smoke moving in it; the court
//    cannot be seen round it.
//  * LET GO (`_Open`, 1.7 to 2.0 s): the dark draws back to the edges as it flies off.
//  * THE SLOW (`_Slow` 1 to 0 as it wears off): its wispy tail still coiled round the bottom of your view, drifting,
//    pale ghost-white, sinking away as the slow wears off. ⚠️ NOT FROST: owner on v4's frosted edge, *"thes elooks like he
//    was frosted"*, *"use diff hud indicator"* (frost is Cheska's).
// `_Calm` (Reduce visual effects) stills the smoke and the wisps; the blind itself stays (it is the rule).
// ⚠️ Colours are authored in sRGB like every colour property here (Unity hands the shader their linear values).
Shader "TumbangPreso/DontLookBlind"
{
    Properties
    {
        _MainTex ("Sprite Texture (unused, required by UGUI)", 2D) = "white" {}
        _Ink ("Dark", Color) = (0.02, 0.03, 0.06, 1)
        _Smoke ("Smoke in the dark", Color) = (0.08, 0.12, 0.2, 1)
        _Wisp ("Its tail", Color) = (0.86, 0.93, 1, 1)
        _Elapsed ("Seconds since it sprang", Float) = 0
        _Open ("Open", Range(0, 1)) = 0
        _Slow ("Slow left", Range(0, 1)) = 0
        _Covered ("Covered", Range(0, 1)) = 0
        _Aspect ("Aspect", Float) = 1.777
        _Calm ("Reduced effects", Float) = 0
    }
    SubShader
    {
        Tags { "Queue" = "Transparent" "IgnoreProjector" = "True" "RenderType" = "Transparent" "PreviewType" = "Plane" "CanUseSpriteAtlas" = "True" }
        Cull Off Lighting Off ZWrite Off ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; float4 color : COLOR; };
            struct v2f { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; float4 color : COLOR; };
            fixed4 _Ink, _Smoke, _Wisp;
            float _Elapsed, _Open, _Slow, _Covered, _Aspect, _Calm;

            v2f vert (appdata v) { v2f o; o.pos = UnityObjectToClipPos(v.vertex); o.uv = v.uv; o.color = v.color; return o; }

            float hash(float2 q) { return frac(sin(dot(q, float2(127.1, 311.7))) * 43758.5453); }
            float noise(float2 q)
            {
                float2 c = floor(q), f = frac(q);
                float2 u = f * f * (3.0 - 2.0 * f);
                return lerp(lerp(hash(c), hash(c + float2(1, 0)), u.x), lerp(hash(c + float2(0, 1)), hash(c + float2(1, 1)), u.x), u.y);
            }
            float fbm(float2 q) { return noise(q) * 0.55 + noise(q * 2.1 + 3.7) * 0.3 + noise(q * 4.3 + 7.1) * 0.15; }

            fixed4 frag (v2f i) : SV_Target
            {
                float t = _Calm > 0.5 ? 0.0 : _Time.y;
                float2 p = (i.uv * 2.0 - 1.0) * float2(_Aspect, 1.0);
                float n = fbm(p * 2.0 + float2(t * 0.2, -t * 0.3));

                // THE DARK ROUND IT: an oval hole the size of its face in the middle, the rest dark. It rushes in over the
                // lunge (from beyond the edges to round its face) and draws back out as it lets go.
                float r = length(p / float2(1.05, 0.95)) + (n - 0.5) * 0.25;
                float lunge = saturate(_Elapsed / 0.22);
                float hole = lerp(2.6, 0.8, lunge * lunge) + _Open * _Open * 2.0;
                float dark = smoothstep(hole - 0.25, hole + 0.1, r) * _Covered;
                float3 colour = lerp(_Ink.rgb, _Smoke.rgb, saturate((n - 0.4) * 1.5));
                float alpha = dark;

                // ITS TAIL, COILED ROUND THE BOTTOM OF YOUR VIEW WHILE THE SLOW LASTS: pale smoky wisps rising from the
                // bottom edge and drifting sideways, sinking away as it wears off. Brighter at the edge of each wisp.
                float slow = saturate(_Slow);
                // Long and thin (strands of a tail, not clouds: film r12's round puffs read as fog).
                float2 q = float2(p.x * 0.55 + sin(p.y * 2.0 + t * 0.8) * 0.25 + t * 0.35, p.y * 4.0 - t * 0.5);
                float w = fbm(q * 1.6);
                float strands = smoothstep(0.45, 0.75, w) * (1.0 - smoothstep(0.75, 0.95, w) * 0.5);
                float reach = -1.0 + slow * 0.75;
                float rise = 1.0 - smoothstep(reach - 0.35, reach + 0.25 + (w - 0.5) * 0.4, p.y);
                // Round the lower corners as well as along the bottom.
                float corners = smoothstep(_Aspect - 0.9, _Aspect + 0.1, abs(p.x)) * smoothstep(0.4, -0.6, p.y) * slow;
                float tail = saturate((rise + corners) * (0.1 + strands * 0.9)) * saturate(slow * 1.8);
                colour = lerp(colour, _Wisp.rgb * (0.7 + strands * 0.4), saturate(tail * (1.0 - dark)));
                alpha = max(alpha, tail * 0.75);
                return fixed4(colour, saturate(alpha) * i.color.a);
            }
            ENDCG
        }
    }
}
