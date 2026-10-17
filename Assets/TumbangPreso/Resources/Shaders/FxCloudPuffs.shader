// SMALL PUFFS OF THE CLOUD, MANY AT ONCE, IN ONE DRAW (`CloudPuffField` in `Runtime/Visual/CloudPuffs.cs`).
//
// The cloud (`FxStudyCloud.shader`) is toon smoke as solid balls that grow and then shrink away, two flat tones and a
// thin line of a darker tone of its own colour. The owner passed it ("looks good") and asked for small ones of it in
// place of the flat dust sprites: "can you also use this to replace the jump-land particles that are using the same 2d
// cloud", and of all of this work, "remember this needs to be heavily optimized".
//
// ⚠️⚠️ A LANDING HAPPENS ALL THE TIME, FOR EVERY CHARACTER, SO NOTHING IS MADE WHEN ONE DOES. The mesh is `SLOTS` puffs of
// `BALLS` unit balls each, built once. Each ball knows which slot it belongs to (`TEXCOORD2.y`). A puff is five numbers
// written into that slot's row of the arrays below: where and when, how big, and its three tones. The vertex program
// works out every ball of every slot from the one clock `_PuffNow`; a slot with no puff in it, a ball not yet born
// and a ball that has shrunk away are each drawn as a single point, which covers no pixel. So twenty-four puffs are
// one mesh, one material and two draw calls (the balls, then their line), whether one is live or all of them.
//
// A PUFF, as opposed to the column: its balls leave a small ring on the ground all at nearly the same moment, are kicked
// OUTWARD along the ground (fast, then slowing), lift a little, and each lives once. The size over that one life is the
// cloud's own curve (`SizeOverLife`: born near nothing, full 60 to 75 percent of the way through, then gone), and the
// shading and the line are the cloud's. ⚠️ `SizeOverLife` and the two-tone shading below are COPIES of the ones in
// `FxStudyCloud.shader`, which is approved and locked and so is not edited to share them. Keep them the same.
//
// ⚠️ TAGGED `Transparent` AND DRAWN AT 2501 THOUGH IT IS OPAQUE AND WRITES DEPTH, for the reason the cloud's shader gives:
// the game's occlusion and ink pass redraws the scene through a replacement shader that would not run this vertex
// program, and would see a heap of unit balls at the origin.
// ⚠️ No `_Time` (the field has its own clock, which holds when the game is paused), no depth texture, no loops.
Shader "TumbangPreso/FxCloudPuffs"
{
    Properties
    {
        _Key ("Where the light comes from (world)", Vector) = (-0.55, 0.70, -0.45, 0)
        _Mass ("How much the whole puff decides the light, and not the ball", Range(0,1)) = 0.34
        _Step ("Where light turns to shadow (-1 to 1), and how soft the step is", Vector) = (0.10, 0.015, 0, 0)
        _Line ("Line width on a full-sized ball, at a puff scale of 1 (m; 0 is none)", Range(0,0.1)) = 0.012
        _LineSize ("Ball radius at which the line begins, and at which it is full (m, at scale 1)", Vector) = (0.02, 0.07, 0, 0)
        _LinePush ("How far the line is set back from the eye (m)", Range(0,0.2)) = 0.006
        _PuffNow ("The field's clock (s)", Float) = 0
    }
    CGINCLUDE
    #include "UnityCG.cginc"
    #define SLOTS 24
    float _PuffNow;
    // One row a slot:
    //   _PuffAt     xyz where on the ground (world), w when it began (the field's clock; far in the past for an empty slot)
    //   _PuffShape  x its scale, y how long a ball lives (s), z how far a ball is kicked out (m at scale 1), w how far it lifts
    //   _PuffLit    rgb the light tone, w the puff's turn about upright (radians)
    //   _PuffShade  rgb the shadow tone, w the share of its balls that are drawn (1 all, 0.5 half)
    //   _PuffLine   rgb the line tone
    float4 _PuffAt[SLOTS], _PuffShape[SLOTS], _PuffLit[SLOTS], _PuffShade[SLOTS], _PuffLine[SLOTS];

    // A COPY of `SizeOverLife` in `FxStudyCloud.shader` (and of `FxCloud.SizeOverLife`): the approved curve.
    float SizeOverLife(float t, float peak, float sharp)
    {
        t = saturate(t);
        float up = pow(max(1e-6, sin(t / max(0.01, peak) * 1.5707963)), 0.8);
        float down = 1.0 - pow(max(1e-6, (t - peak) / max(0.01, 1.0 - peak)), sharp);
        return t <= peak ? up : down;
    }

    struct ball { float3 at; float radius; float3 lies; float scale; int slot; };

    // Where one ball of one puff is now (world), from the clock, its slot's row and its own draws:
    //   a = how late it leaves (0 to 1 of a small part of the life), how large it gets (0 to 1), when it peaks, how sharply it shrinks
    //   b = its bearing round the ring (radians), how far it is kicked (a share), how much it lifts (a share), where in the ring it starts (0 to 1)
    //   c = (which ball of its puff it is, 0 to 1; which slot)
    ball Place(float4 a, float4 b, float4 c)
    {
        ball o;
        int slot = (int)(c.y + 0.5);
        float4 at = _PuffAt[slot], shape = _PuffShape[slot];
        float life = max(0.05, shape.y);
        float t = (_PuffNow - at.w) / life - a.x * 0.18;
        float peak = lerp(0.60, 0.75, a.z), sharp = lerp(1.0, 2.4, a.w);
        // Small and smaller: the largest ball of a puff at scale 1 is eleven centimetres in radius.
        float radius = lerp(0.045, 0.11, a.y) * SizeOverLife(t, peak, sharp);
        float there = step(0.0, t) * step(t, 1.0) * step(c.x, _PuffShade[slot].w) * step(0.006, radius);
        radius *= there;
        float u = saturate(t), kicked = 1.0 - (1.0 - u) * (1.0 - u);
        float turn = b.x + _PuffLit[slot].w;
        float3 way = float3(cos(turn), 0.0, sin(turn));
        float out_ = 0.07 + 0.09 * b.w + shape.z * b.y * kicked;
        float3 off = way * out_ + float3(0.0, radius * 0.55 + shape.w * b.z * u, 0.0);
        o.at = at.xyz + off * shape.x;
        o.radius = radius * shape.x;
        o.lies = way * saturate(out_ / max(0.05, 0.16 + shape.z));
        o.scale = shape.x;
        o.slot = slot;
        return o;
    }
    ENDCG
    SubShader
    {
        Tags { "Queue"="AlphaTest+51" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Pass
        {
            Cull Back
            ZWrite On
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.5
            float4 _Key, _Step;
            float _Mass;
            struct appdata { float4 vertex:POSITION; float4 a:TEXCOORD0; float4 b:TEXCOORD1; float4 c:TEXCOORD2; };
            struct v2f { float4 vertex:SV_POSITION; float3 normal:TEXCOORD0; float3 mass:TEXCOORD1; fixed3 lit:TEXCOORD2; fixed3 shade:TEXCOORD3; };
            v2f vert(appdata v)
            {
                v2f o;
                ball p=Place(v.a,v.b,v.c);
                // The puffs are placed in the world, whatever the field's own object is doing.
                o.vertex=mul(UNITY_MATRIX_VP,float4(p.at+v.vertex.xyz*p.radius,1.0));
                o.normal=v.vertex.xyz;
                o.mass=p.lies;
                o.lit=_PuffLit[p.slot].rgb; o.shade=_PuffShade[p.slot].rgb;
                return o;
            }
            fixed4 frag(v2f i):SV_Target
            {
                // A COPY of the cloud's two flat tones (`FxStudyCloud.shader`).
                float3 n=normalize(i.normal);
                float has=saturate(length(i.mass)*1.6);
                float3 facing=normalize(lerp(n,normalize(i.mass+float3(0,1e-5,0)),_Mass*has));
                float light=dot(facing,normalize(_Key.xyz));
                fixed3 colour=lerp(i.shade,i.lit,smoothstep(_Step.x-_Step.y,_Step.x+_Step.y,light));
                return fixed4(colour,1);
            }
            ENDCG
        }
        // The line: every ball again, a little bigger, inside out.
        Pass
        {
            Cull Front
            ZWrite On
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.5
            float _Line, _LinePush;
            float4 _LineSize;
            struct appdata { float4 vertex:POSITION; float4 a:TEXCOORD0; float4 b:TEXCOORD1; float4 c:TEXCOORD2; };
            struct v2f { float4 vertex:SV_POSITION; fixed3 tone:TEXCOORD0; };
            v2f vert(appdata v)
            {
                v2f o;
                ball p=Place(v.a,v.b,v.c);
                float width=_Line*p.scale*smoothstep(_LineSize.x*p.scale,_LineSize.y*p.scale,p.radius);
                float3 world=p.at+v.vertex.xyz*(p.radius+width);
                world+=normalize(world-_WorldSpaceCameraPos)*_LinePush;
                o.vertex=mul(UNITY_MATRIX_VP,float4(world,1.0));
                o.vertex=lerp(float4(0,0,-2,1),o.vertex,step(0.0002,width));
                o.tone=_PuffLine[p.slot].rgb;
                return o;
            }
            fixed4 frag(v2f i):SV_Target { return fixed4(i.tone,1); }
            ENDCG
        }
    }
    Fallback Off
}
