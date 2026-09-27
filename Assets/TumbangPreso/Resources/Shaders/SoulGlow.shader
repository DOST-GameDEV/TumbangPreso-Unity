// ⚠️⚠️ THE LIGHT INSIDE PHAISTER'S VOODOO DOLL (HERO-10 v3). Owner, 2026-09-27: *"i want it to actually look like its coming out of
// the holes"*, *"make the glow really look like it comes from within not js drawn on"*, then on v16, whose openings had torn lips
// standing up round them: *"why does it pop out its the opposite it should look like its from withhin"*, *"same with the mouth"*,
// *"why do u pop out his features"*.
//
// ⚠️⚠️ SO EVERY OPENING IS A HOLE CUT INTO THE BODY, AND THIS SHADER IS WHAT CUTS IT. Nothing stands off the cloth. Each opening
// (the grin, the eye sockets, the torn seams, the crown) is two things in `glow-mesh` (`tools/build_phaister_doll_voxel.py`):
//  * its MOUTH, a flat polygon lying on the cloth exactly where the hole is (TEXCOORD0.x = 1), and
//  * its INSIDE, a cup of walls and a floor that sit BELOW the cloth, inside the doll (TEXCOORD0.x = 0): the walls dim where they
//    meet the cloth and grow hot toward the floor, the floor white-hot along its middle (vertex colours, typed by the builder).
// The body is solid, so the inside is behind its own cloth and would never draw. Four passes, per doll, after every opaque thing:
//  1 MASK: the mouth, depth-tested against the scene, marks its pixels in one stencil bit (only where the mouth is really seen:
//    a stitch lying across it, or an arm in front of it, keeps its pixels).
//  2 OPEN: in those pixels only, the depth is pushed back `_Open` rig units behind the cloth, along the view ray (the cloth is
//    "cut away" for what follows). ⚠️ NOT to the far plane: any sliver of mouth the cup does not cover would then be painted by
//    the skybox, which draws after this queue wherever the depth is far.
//  3 INSIDE: the cup is drawn, depth-tested among its own walls and floor, in those pixels only: the light is seen THROUGH the
//    hole, down inside the doll, with real parallax as the camera moves.
//  4 CLOSE: the stencil bit is cleared again.
// The light that falls OUT of each hole onto the cloth round it is `SoulSpill` (additive), in the spill mesh.
//
//  * `_Intensity` above 1 lifts it past the lit cloth. `_Pulse` how far it breathes, `_Rate` how fast; the phase climbs the body.
//  * The stencil bit is 64 (the UI's masks use the low bits and are cleared with their canvases).
// Loaded through Resources (`Shaders/SoulGlow`), which is what keeps it in the player (`ToonSkin`'s rule).
Shader "TumbangPreso/SoulGlow"
{
    Properties
    {
        _Tint ("Tint", Color) = (1, 1, 1, 1)
        _Intensity ("Intensity", Float) = 1.35
        _Pulse ("Pulse", Float) = 0.22
        _Rate ("Pulse rate", Float) = 2.4
        _Open ("How far behind the cloth a hole opens (rig units)", Float) = 0.1
    }
    SubShader
    {
        Tags { "Queue" = "Geometry+400" "RenderType" = "Opaque" "IgnoreProjector" = "True" }

        CGINCLUDE
        #include "UnityCG.cginc"

        fixed4 _Tint;
        float _Intensity, _Pulse, _Rate, _Open;

        struct appdata { float4 vertex : POSITION; float4 colour : COLOR; float2 uv : TEXCOORD0; };
        struct v2f { float4 pos : SV_POSITION; float height : TEXCOORD0; float4 colour : TEXCOORD1; };

        // A vertex of the other kind is thrown outside the clip volume, so each pass draws only its own polygons.
        v2f Keep(appdata v, float role)
        {
            v2f o;
            o.pos = abs(v.uv.x - role) < 0.5 ? UnityObjectToClipPos(v.vertex) : float4(2.0, 2.0, 2.0, 1.0);
            o.height = mul(unity_ObjectToWorld, v.vertex).y;
            o.colour = v.colour;
            return o;
        }

        v2f vertMouth(appdata v) { return Keep(v, 1.0); }

        // The mouth pushed straight back along the view ray (so it covers the same pixels), `_Open` rig units scaled as the doll is.
        v2f vertOpen(appdata v)
        {
            v2f o = Keep(v, 1.0);
            if (abs(v.uv.x - 1.0) < 0.5)
            {
                float3 view = UnityObjectToViewPos(v.vertex);
                float push = _Open * length(unity_ObjectToWorld[0].xyz);
                if (unity_OrthoParams.w > 0.5) view.z -= push;
                else view *= (length(view) + push) / max(length(view), 1e-4);
                o.pos = mul(UNITY_MATRIX_P, float4(view, 1.0));
            }
            return o;
        }
        v2f vertInside(appdata v) { return Keep(v, 0.0); }

        fixed4 fragNothing(v2f i) : SV_Target { return 0; }


        fixed4 fragInside(v2f i) : SV_Target
        {
            // Two waves out of step, so the throb is uneven, and climbing: the phase falls with height.
            float t = _Time.y * _Rate - i.height * 3.1;
            float breath = 0.6 * sin(t) + 0.4 * sin(t * 2.37 + 1.3);
            float3 light = i.colour.rgb * _Tint.rgb * _Intensity * (1.0 + _Pulse * breath * (0.35 + i.colour.a));
            return fixed4(light, 1.0);
        }
        ENDCG

        Pass
        {
            Name "MASK"
            ZWrite Off
            ZTest LEqual
            Cull Off
            ColorMask 0
            Stencil { Ref 64 WriteMask 64 Comp Always Pass Replace }
            CGPROGRAM
            #pragma vertex vertMouth
            #pragma fragment fragNothing
            ENDCG
        }
        Pass
        {
            Name "OPEN"
            ZWrite On
            ZTest Always
            Cull Off
            ColorMask 0
            Stencil { Ref 64 ReadMask 64 Comp Equal Pass Keep }
            CGPROGRAM
            #pragma vertex vertOpen
            #pragma fragment fragNothing
            ENDCG
        }
        Pass
        {
            Name "INSIDE"
            ZWrite On
            ZTest LEqual
            Cull Off
            Stencil { Ref 64 ReadMask 64 Comp Equal Pass Keep }
            CGPROGRAM
            #pragma vertex vertInside
            #pragma fragment fragInside
            ENDCG
        }
        Pass
        {
            Name "CLOSE"
            ZWrite Off
            ZTest Always
            Cull Off
            ColorMask 0
            Stencil { Ref 0 WriteMask 64 Comp Always Pass Replace }
            CGPROGRAM
            #pragma vertex vertMouth
            #pragma fragment fragNothing
            ENDCG
        }
    }
    FallBack Off
}
