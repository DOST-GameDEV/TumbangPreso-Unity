// Amihan's wind, and the ability-direction baseline's "bright thin edges around a darker middle"
// (docs/reports/amihan-kit-2026-09-25/direction.md § 2 and § 3).
//
// A ribbon is drawn from its UVs: u runs ALONG it (0 tail end of the mesh, 1 head end) and v runs
// ACROSS it (0 to 1). Three values make the ladder the direction asks for: a near-white CORE line
// down the middle, the hue BODY either side of it broken into streaks that travel with _Phase, and
// a darker INK rim at the very edge, which is what keeps wind readable on the bright court and ties
// it to the ink outlines on every character.
//
// Everything that moves is a PARAMETER, never _Time: _Phase scrolls the streaks, _Head and _Tail
// say which stretch of the ribbon exists, and _Thin narrows it to a thread as it dies ("fade by
// thinning the line, not by dimming a disc", research.md § 3 rule 10). So a paused match, a replay
// and a probe capture all show the frame the timeline asked for (Visual/VfxTimeline.cs).
Shader "TumbangPreso/WindRibbon"
{
    Properties
    {
        _CoreColor ("Core line", Color) = (0.957, 1.0, 0.914, 1)
        _BodyColor ("Body", Color) = (0.651, 0.925, 0.518, 1)
        _InkColor ("Ink edge", Color) = (0.184, 0.420, 0.165, 1)
        _Alpha ("Alpha", Range(0, 1)) = 1
        _Phase ("Streak phase", Float) = 0
        _Head ("Head (0-1 along)", Range(0, 1.2)) = 1
        _Tail ("Tail (0-1 along)", Range(-0.2, 1)) = 0
        _Thin ("Thinning", Range(0, 1)) = 0
        _Streaks ("Streaks along", Float) = 6
        _Core ("Core width", Range(0, 1)) = 0.2
        _Seed ("Seed", Float) = 0
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #include "UnityCG.cginc"

            fixed4 _CoreColor, _BodyColor, _InkColor;
            float _Alpha, _Phase, _Head, _Tail, _Thin, _Streaks, _Core, _Seed;

            struct appdata { float4 vertex:POSITION; float2 uv:TEXCOORD0; };
            struct v2f { float4 vertex:SV_POSITION; float2 uv:TEXCOORD0; UNITY_FOG_COORDS(1) };

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                UNITY_TRANSFER_FOG(o, o.vertex);
                return o;
            }

            float hash(float n) { return frac(sin(n * 12.9898 + _Seed * 78.233) * 43758.5453); }

            fixed4 frag(v2f i) : SV_Target
            {
                float u = i.uv.x;
                float span = max(_Head - _Tail, 0.0001);
                float along = saturate((u - _Tail) / span);
                if (u < _Tail || u > _Head) discard;

                // The ribbon tapers toward its tail, so the head is the thick bright leading edge
                // and the tail trails off: a gust, not a pipe.
                float taper = lerp(0.35, 1.0, smoothstep(0.0, 0.55, along));
                float width = taper * lerp(1.0, 0.12, _Thin);
                float across = abs(i.uv.y * 2.0 - 1.0);
                if (across > width) discard;
                float x = across / width;

                // Broken streaks: the body is cut into dashes that travel with _Phase, in three
                // lanes across the ribbon so neighbouring dashes do not line up.
                float lane = floor(i.uv.y * 3.0);
                float cell = floor(u * _Streaks * 3.0 - _Phase * 3.0 + lane * 0.37);
                float dash = step(0.32, hash(cell + lane * 17.0));

                float core = 1.0 - smoothstep(0.0, _Core, x);
                float ink = smoothstep(0.74, 0.88, x);
                float body = (1.0 - x) * 0.55 * lerp(0.35, 1.0, dash);

                // The ends: a soft fade in from the tail, a crisp head.
                float ends = smoothstep(0.0, 0.3, along) * (1.0 - smoothstep(0.93, 1.0, along));

                float3 colour = lerp(_BodyColor.rgb, _CoreColor.rgb, core * lerp(0.55, 1.0, dash));
                colour = lerp(colour, _InkColor.rgb, ink);
                float alpha = max(max(core * 0.9 * lerp(0.6, 1.0, dash), body), ink * 0.75) * ends * _Alpha;

                fixed4 result = fixed4(colour, alpha);
                UNITY_APPLY_FOG(i.fogCoord, result);
                return result;
            }
            ENDCG
        }
    }
    Fallback Off
}
