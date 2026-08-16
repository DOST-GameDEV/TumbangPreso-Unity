// § THE DANGER VIGNETTE — the taya's "your can is down" and the attacker's "you are
// catchable", transcribed from `assets/ui/downed_vignette.gdshader`.
//
// ⚠️⚠️ IT IS A VIGNETTE, AND THE PORT SHIPPED IT AS A FLAT FULL-SCREEN RECT. `HUD.tscn` puts
// a ShaderMaterial on `DownedFlash` and this is that shader; `Hud.BuildDangerFlash` created a
// bare `Image` with a solid colour and no material, so every pixel of the frame got the same
// red instead of only the corners. Reported as *"mmy screen is just red in the game as taya"*,
// which is exactly right: the taya's hold is live for most of a round — measured at about 70
// of 180 live seconds in the original — so a uniform tint is what that player looks at for the
// whole game.
//
// The port's own notes had already walked up to this and stopped one step short. `Hud` records
// that a full-screen red held at flash strength *"reads as the renderer being broken"* and
// answers it by lowering the ALPHA to 0.16; `FrostVignette` records the better answer, that the
// fix is SHAPE — *"putting the opacity where the player is NOT looking"* — and applies it to the
// ice. The red needed the same treatment and never got it.
//
// ⚠️ THE FALLOFF IS THE .gdshader's, NOT A NEW ONE. `smoothstep(0.3, 1.2, dist)` on the radius
// of a UV mapped to -1..1: dead clear inside 0.3, and still only part-way up at the corners,
// where the radius is sqrt(2) = 1.414. Combined with the material's own 0.6 alpha and the HUD's
// 0.16 hold, the strongest pixel on screen lands at about 0.096 and the centre at zero.
//
// ⚠️ NOT ASPECT-CORRECTED, DELIBERATELY, unlike the frost beside it. The frost draws a BAND of
// fixed reach from each edge, which needs the aspect or it is thicker on the long axis; this is
// a radial ramp across the whole frame, and the original stretches it with the viewport. Adding
// a correction here would be a change to the look rather than a port of it.
Shader "TumbangPreso/DownedVignette"
{
    Properties
    {
        // ⚠️ REQUIRED BY UGUI EVEN THOUGH NOTHING SAMPLES IT. Same rule the frost shader's own
        // note records: a Graphic hands its texture to the CanvasRenderer through this exact
        // name, and a UI material without it logs an error on every canvas rebuild.
        _MainTex ("Sprite Texture (unused, required by UGUI)", 2D) = "white" {}

        // The material's own alpha in the .tscn is 0.6 and multiplies the ramp; the Image's
        // vertex colour then carries the HUD's hold or pulse level on top of it.
        _VignetteColor ("Vignette Colour", Color) = (0.97, 0.0, 0.0, 0.6)
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
            "PreviewType" = "Plane"
            "CanUseSpriteAtlas" = "True"
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            fixed4 _VignetteColor;

            v2f vert (appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.color = v.color;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // Radial: 0 at the centre, 1 at the corners.
                float2 uv = i.uv * 2.0 - 1.0;
                float dist = length(uv);

                float alpha = smoothstep(0.3, 1.2, dist) * _VignetteColor.a;

                // ⚠️ THE GRAPHIC'S OWN COLOUR IS THE LEVEL. `Hud` writes the hold and the pulse
                // onto `Image.color`, which arrives here as the vertex colour, so the ramp is a
                // shape and the strength stays where the HUD can drive it.
                return fixed4(i.color.rgb, alpha * i.color.a);
            }
            ENDCG
        }
    }
}
