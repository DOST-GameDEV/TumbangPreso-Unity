// AIRBURST'S DOME: the shockwave of air that blows out from her feet over the 10 m she takes (`FxStudyAirburst`). The
// owner: "should probably add a sphere (dome) that expands outwards. the sphere should look organic like wind shockwave.
// can you also add a distort effect that it bends the light as it blasts outward?"
//
// What makes it organic and not a glass bubble:
//   * it is seen by its RIM: a fresnel that is brightest at the silhouette, with almost nothing in the middle, so the court
//     shows through it;
//   * WIND STREAKS, the bench's own streak map (`erode_streak`), are swept across its surface along the spiral's turn and
//     scroll outward and round; the rim is made of them, so its thickness varies all the way round;
//   * it TEARS: the bench's cloud map (`erode_cloud`) eats it from the crown down, so the top opens first and the last of it
//     is a broken band near the court; it is never a hard line and it fades out before it meets the court, so there is no
//     flat ring at its foot;
//   * it BENDS THE LIGHT behind it, through the project's one shared refraction (`FxRefract.cginc`: one named grab,
//     `_FxRefractGrab`, for every effect that bends light), offset along the dome's screen-space normal at its rim and
//     along its streaks, so the court and anything behind ripple outward as it passes.
//
// ⚠️ THE GRAB IS A FULL-SCREEN COPY FOR EVERY CAMERA THAT DRAWS IT, so it lives in the LOD 300 SubShader only. The LOD 100
// SubShader below is the same dome without it (no bending), and `FxStudyAirburst` drops this shader's `maximumLOD` to 100
// on a lean graphics tier (`Settings.GraphicsProfiles`, Low) and on phones. The grab is taken only while a dome is drawn
// (about the first 0.6 s of the burst). Colours are the caller's (the approved Whirlwind's silk). `_Age` is the caller's
// clock; no `_Time`, no depth texture, no loops.
Shader "TumbangPreso/FxStudyAirDome"
{
    Properties
    {
        _MaskTex ("Streaks", 2D) = "white" {}
        _ErodeTex ("Tearing", 2D) = "white" {}
        _Deep ("Deep", Color) = (0.02, 0.26, 0.17, 1)
        _Body ("Body", Color) = (0.07, 0.58, 0.45, 1)
        _Hot ("Hot", Color) = (0.86, 1, 0.95, 1)
        _Age ("Clock (s)", Float) = 0
        _Opacity ("Opacity", Range(0,1)) = 1
        _Erode ("Torn away (0 whole, 1 gone)", Range(0,1)) = 0
        _Light ("Light share (1 added, 0 covers)", Range(0,1)) = 0.75
        _Fill ("Fill in the middle", Range(0,1)) = 0.06
        _Swirl ("Streaks: turns round, tiling up, scroll round, scroll out", Vector) = (4, 2.2, 0.45, -1.4)
        _Bend ("How far it bends the light (share of the screen)", Float) = 0.15
        _Paint ("How much of its own colour it lays on (the silk gusts carry the look)", Range(0,1)) = 0.0
    }

    CGINCLUDE
    #include "UnityCG.cginc"
    sampler2D _MaskTex, _ErodeTex;
    fixed4 _Deep, _Body, _Hot;
    float _Age, _Opacity, _Erode, _Light, _Fill, _Bend, _Paint;
    float4 _Swirl;
    #include "FxRefract.cginc"
    struct appdata { float4 vertex:POSITION; float3 normal:NORMAL; float2 uv:TEXCOORD0; };
    struct v2f { float4 pos:SV_POSITION; float2 uv:TEXCOORD0; float3 n:TEXCOORD1; float3 eye:TEXCOORD2; float4 grab:TEXCOORD3; float2 screenN:TEXCOORD4; };

    float grey(float c)
    {
        #ifdef UNITY_COLORSPACE_GAMMA
        return c;
        #else
        return LinearToGammaSpaceExact(c);
        #endif
    }

    v2f vert(appdata v)
    {
        v2f o;
        o.pos = UnityObjectToClipPos(v.vertex);
        o.uv = v.uv;
        float3 world = mul(unity_ObjectToWorld, v.vertex).xyz;
        o.n = UnityObjectToWorldNormal(v.normal);
        o.eye = _WorldSpaceCameraPos - world;
        o.grab = ComputeGrabScreenPos(o.pos);
        // The normal as the screen sees it: the way the light behind is pushed.
        o.screenN = FxRefractScreenDir(o.n);
        return o;
    }

    // The dome's own picture: colour (premultiplied by its share) and how much of the light behind it bends.
    float4 Dome(v2f i, out float bendBy)
    {
        float3 n = normalize(i.n), e = normalize(i.eye);
        float rim = 1.0 - abs(dot(n, e));
        rim = rim * rim;
        // u runs round the dome, v from the court (0) to the crown (1). The streaks are swept along the spiral's turn.
        float2 at = float2(i.uv.x * _Swirl.x + i.uv.y * 0.8 - _Age * _Swirl.z, i.uv.y * _Swirl.y - _Age * _Swirl.w);
        float streak = grey(tex2D(_MaskTex, at).r);
        float streak2 = grey(tex2D(_MaskTex, at * float2(2.1, 1.7) + 0.37).r);
        float wind = saturate(streak * 0.7 + streak2 * 0.45);
        // Torn from the crown down; faded out before the court so there is no ring at its foot.
        float eat = grey(tex2D(_ErodeTex, float2(i.uv.x * 2.0, i.uv.y * 1.2) + _Age * 0.05).r);
        float kept = smoothstep(_Erode * 1.3 - 0.05, _Erode * 1.3 + 0.08, eat * 0.7 + (1.0 - i.uv.y) * 0.45);
        float foot = smoothstep(0.0, 0.22, i.uv.y);
        // ⚠️ RIM AND STREAKS ONLY. r13's first film filled the whole dome with a dark green and read as a glass ball; the
        // middle is now the court, and the rim itself is broken by the streaks so its thickness wanders.
        float edge = smoothstep(0.30, 0.85, rim) * smoothstep(0.40, 0.75, wind);
        float body = (edge * (0.5 + 0.8 * wind) + _Fill * wind * wind) * kept * foot * _Opacity;
        float3 colour = lerp(_Body.rgb, _Hot.rgb, smoothstep(0.45, 0.9, wind * rim * 1.4));
        // ⚠️ RAGGED, NOT A CLEAN SHELL (r15 read as a glass ball): it bends only in gusts, where the streaks are strong, and
        // most near the rim, so the warp comes and goes round it.
        // The bend runs right down to the court (only the paint is faded at the foot): seen from above at a game angle the
        // dome's silhouette IS its foot, and r17 faded the bend away exactly where it would have shown.
        bendBy = smoothstep(0.0, 0.55, rim) * lerp(0.35, 1.0, smoothstep(0.30, 0.70, wind)) * kept * smoothstep(0.0, 0.04, i.uv.y) * _Opacity;
        body *= _Paint;
        return float4(colour * saturate(body), saturate(body));
    }
    ENDCG

    // With the light bent (LOD 300): the scene behind, read back pushed outward along the dome, under the dome's own light.
    SubShader
    {
        LOD 300
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        GrabPass { "_FxRefractGrab" }
        Pass
        {
            ZWrite Off
            Cull Off
            Blend One OneMinusSrcAlpha
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            float4 frag(v2f i) : SV_Target
            {
                float bendBy;
                float4 d = Dome(i, bendBy);
                float3 behind = FxRefract(i.grab, i.screenN, _Bend * bendBy);
                float3 straight = FxRefractStraight(i.grab);
                // Where the dome bends, the bent picture replaces the straight one; then the dome's own colour is laid on.
                float m = saturate(bendBy * 2.0);
                float3 under = lerp(straight, behind, m);
                // Cover: what the dome's own colour hides (premultiplied, `_Light` of it laid on as light).
                float cover = d.a * (1.0 - _Light);
                return float4(under * m * (1.0 - cover) + d.rgb, max(m, d.a * (1.0 - _Light)));
            }
            ENDCG
        }
    }

    // Without (LOD 100): the same dome, nothing bent and no grab.
    SubShader
    {
        LOD 100
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Pass
        {
            ZWrite Off
            Cull Off
            Blend One OneMinusSrcAlpha
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            float4 frag(v2f i) : SV_Target
            {
                float bendBy;
                float4 d = Dome(i, bendBy);
                return float4(d.rgb, d.a * (1.0 - _Light));
            }
            ENDCG
        }
    }
    Fallback Off
}
