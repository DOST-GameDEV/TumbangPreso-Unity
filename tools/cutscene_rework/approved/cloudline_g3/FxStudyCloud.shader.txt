// The effects study's CLOUD (`FxCloud` in `Runtime/Visual/FxStudy/FxStudyCloud.cs`): toon smoke as a mass of solid balls.
//
// ⚠️ WHY IT IS NOT `FxStudySmoke`. That shader's smoke is eaten: each ball is cut away by a noise picture as it ages. The
// owner, shown a picture of the smoke he means: "the issue with the wisp smokes is that you use noise textures to fade
// them out instead of a random decreasing size gradient modifier". In his picture every ball is a clean solid shape for
// the whole of its life and simply gets SMALLER until it is gone. So nothing here eats anything: there is no erosion
// map, no clip and no alpha.
//
// ⚠️⚠️ THE WHOLE EMITTER RUNS HERE, IN THE VERTEX PROGRAM, AND THE MESH NEVER CHANGES. The first cloud rebuilt its mesh on
// the processor every frame (every vertex of every ball, and a search of every ball against its neighbours). On the
// bench, which is not real time, that cost nothing anyone saw; stood in a match it did. The owner: "the cloud is
// really laggy", then "remember this needs to be heavily optimized". So the mesh is every ball as a unit ball at the
// origin, built once, each vertex carrying its ball's own draws (`TEXCOORD0` to `TEXCOORD2`), and `Place` below works
// out from `_Clock` where that ball is in its life, how big it is and where it has got to. One number a frame comes
// from the processor: `_Clock`.
//
// ⚠️ `SizeOverLife` IS THE APPROVED CURVE AND IT IS HERE NOW (the owner, of film c6: "ye size looks good"). Born near
// nothing, growing to its full size 60 to 75 percent of the way through its life, then shrinking to nothing. `FxCloud`
// keeps the same function in C# as the record of it; the two must stay the same function.
//
// What his picture's shading is, and so what this is:
//   * TWO FLAT TONES with a hard step between them, and nothing else: no gradient, no rim, no third tone;
//   * THE SHADOW BELONGS TO THE WHOLE MASS as much as to each ball: one side of the plume is in shadow as one big shape,
//     with small lit islands inside it. Each ball knows which side of the column it is on (`lies`) and the light is
//     taken partly from that (`_Mass`) and partly from the ball's own surface;
//   * OPAQUE, with hard edges;
//   * A THIN LINE of a darker tone of the smoke's own colour round the silhouette AND along the folds where one lobe sits
//     in front of another (`_Line`, metres; 0 is none). The owner, having seen the cloud under the game's ambient
//     occlusion: "i feel like an outline shader would work better instead of ao". It is each ball drawn again a little
//     bigger and inside out, with ordinary depth testing: where a ball's edge stands in front of its neighbours its
//     line shows, and where it is buried their bodies cover it. The line is thinner on a small ball and absent on a
//     very small one (`_LineSize`), so the many small balls under the surface do not each draw a ring, which is what
//     the second film of this did; and it is set back from the eye by a few centimetres (`_LinePush`), far less than
//     the third of a metre that left only the outer silhouette.
//
// ⚠️ THE CLOUD HAS NO CREASE TONE OF ITS OWN ANY MORE AND TAKES NO PART IN THE GAME'S AMBIENT OCCLUSION. The line does
// that job. It is tagged `Transparent` and drawn just after the solid world (queue 2501) although it is opaque and
// writes depth: the game's occlusion and ink pass (`WorldOutline`) runs between the solid world and that queue, and it
// finds its edges in a picture of the scene redrawn through a replacement shader chosen by this tag. That redraw would
// not run the vertex program below, so a cloud tagged `Opaque` would be drawn into it as a heap of metre-wide balls at
// the emitter's foot. Tagged this way it is not in that picture at all and is drawn after the pass has finished.
// ⚠️ No `_Time`, no depth texture, no loops: the rules of `FxStudyRibbon`.
//
// KEEP THE LINE AS IT IS (`_LineSize` 0.05 to 0.20, `_LinePush` 0.010, with `FxStudyCloud.Line` 0.022 m), and the two
// flat tones and the size curve. Of this cloud stood in a match the owner said on 2026-10-11: "looks good".
Shader "TumbangPreso/FxStudyCloud"
{
    Properties
    {
        _Lit ("Light tone", Color) = (0.84, 0.84, 0.84, 1)
        _Shade ("Shadow tone", Color) = (0.62, 0.62, 0.63, 1)
        _LineColor ("Line tone", Color) = (0.42, 0.42, 0.44, 1)
        _Key ("Where the light comes from (world)", Vector) = (-0.55, 0.70, -0.45, 0)
        _Mass ("How much the whole cluster decides the light, and not the ball", Range(0,1)) = 0.5
        _Step ("Where light turns to shadow (-1 to 1), and how soft the step is", Vector) = (0.08, 0.02, 0, 0)
        _Line ("Line width on a full-sized ball (m; 0 is none)", Range(0,0.2)) = 0
        _LineSize ("Ball radius at which the line begins, and at which it is full (m)", Vector) = (0.05, 0.20, 0, 0)
        _LinePush ("How far the line is set back from the eye (m)", Range(0,0.4)) = 0.010
        _Clock ("The emitter's clock (s)", Float) = 0
        _Source ("Emitter: where the balls leave from (xyz), how wide that mouth is (w)", Vector) = (0, 0, 0, 0.24)
        _Rise ("Emitter: how far a ball travels in its life (xyz), how long it lives (w)", Vector) = (0, 3.5, 0, 2)
        _Shape ("Emitter: widen, weave, smallest and largest ball", Vector) = (0.5, 0.24, 0.13, 0.62)
        _Curve ("Emitter: peak moment low and high, shrink power low and high", Vector) = (0.60, 0.75, 1.0, 2.4)
        _Flow ("Emitter: on from, on to (s), the size a ball is gone at (m), the column's bend", Vector) = (-1000, 1000, 0.07, 0)
        _Side ("Emitter: the column's sideways axis", Vector) = (1, 0, 0, 0)
        _Fore ("Emitter: the column's forward axis (xyz), one over its half width (w)", Vector) = (0, 0, 1, 1.6)
    }
    CGINCLUDE
    #include "UnityCG.cginc"
    float _Clock;
    float4 _Source, _Rise, _Shape, _Curve, _Flow, _Side, _Fore;

    // The size of a ball over its life, 0 to 1. The same function as `FxCloud.SizeOverLife`: keep the two alike.
    float SizeOverLife(float t, float peak, float sharp)
    {
        t = saturate(t);
        float up = pow(max(1e-6, sin(t / max(0.01, peak) * 1.5707963)), 0.8);
        float down = 1.0 - pow(max(1e-6, (t - peak) / max(0.01, 1.0 - peak)), sharp);
        return t <= peak ? up : down;
    }

    struct ball { float3 at; float radius; float3 lies; };

    // Where one ball is now, in the cloud's own space, from the clock and that ball's draws:
    //   a = its place in the loop (0 to 1), how large it gets (0 to 1), when it peaks (0 to 1), how sharply it shrinks (0 to 1)
    //   b = its pace up the rise, its bearing round the axis (radians), how far out it lies (0 to 1), the rate of its weave
    //   c.x = where in its weave it starts
    ball Place(float4 a, float4 b, float4 c)
    {
        ball o;
        float life = max(0.05, _Rise.w);
        float through = _Clock / life + a.x;
        float t = through - floor(through), born = _Clock - t * life;
        float peak = lerp(_Curve.x, _Curve.y, a.z), sharp = lerp(_Curve.z, _Curve.w, a.w);
        float radius = lerp(_Shape.z, _Shape.w, a.y) * SizeOverLife(t, peak, sharp);
        // A ball the emitter was off for is not there; one that has shrunk below `_Flow.z` is gone (without that the
        // top of a column is a spray of specks). A ball that is not there is every one of its points at one place,
        // so it covers no pixel at all.
        float gone = t > peak ? max(0.004, _Flow.z) : 0.012;
        float there = step(_Flow.x, born) * step(born, _Flow.y) * step(gone, radius - 1e-6);
        radius *= there;
        float climbed = b.x * pow(max(1e-6, t), 0.85);
        float out_ = _Source.w * 0.5 * sqrt(b.z) + _Shape.x * b.z * pow(max(1e-6, t), 0.7);
        // Its own weave, and the slow bend of the whole column (the same for every ball at a height).
        float weave = _Shape.y * (0.5 * sin(t * b.w + c.x) * t + sin(climbed * 3.6 + _Flow.w) * sqrt(t));
        float3 off = _Side.xyz * (cos(b.y) * out_ + weave) + _Fore.xyz * (sin(b.y) * out_ + weave * 0.6);
        o.at = _Source.xyz + _Rise.xyz * climbed + off;
        o.at.y = max(o.at.y, _Source.y + radius * 0.35);
        o.radius = radius;
        o.lies = off * _Fore.w;
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
            fixed4 _Lit, _Shade;
            float4 _Key, _Step;
            float _Mass;
            struct appdata { float4 vertex:POSITION; float4 a:TEXCOORD0; float4 b:TEXCOORD1; float4 c:TEXCOORD2; };
            struct v2f { float4 vertex:SV_POSITION; float3 normal:TEXCOORD0; float3 mass:TEXCOORD1; };
            v2f vert(appdata v)
            {
                v2f o;
                ball p=Place(v.a,v.b,v.c);
                // The mesh's own point is a point of a unit ball, so it is also the normal there.
                o.vertex=UnityObjectToClipPos(float4(p.at+v.vertex.xyz*p.radius,1.0));
                o.normal=UnityObjectToWorldNormal(v.vertex.xyz);
                o.mass=mul((float3x3)unity_ObjectToWorld,p.lies);
                return o;
            }
            fixed4 frag(v2f i):SV_Target
            {
                float3 n=normalize(i.normal);
                // How far out from the plume's axis the ball is, is how much its side counts: a ball on the axis has none.
                float has=saturate(length(i.mass)*1.6);
                float3 facing=normalize(lerp(n,normalize(i.mass+float3(0,1e-5,0)),_Mass*has));
                float light=dot(facing,normalize(_Key.xyz));
                fixed3 colour=lerp(_Shade.rgb,_Lit.rgb,smoothstep(_Step.x-_Step.y,_Step.x+_Step.y,light));
                return fixed4(colour,1);
            }
            ENDCG
        }
        // The line: every ball again, a little bigger, inside out. It shows where a ball's edge stands clear of the rest.
        Pass
        {
            Cull Front
            ZWrite On
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            fixed4 _LineColor;
            float _Line, _LinePush;
            float4 _LineSize;
            struct appdata { float4 vertex:POSITION; float4 a:TEXCOORD0; float4 b:TEXCOORD1; float4 c:TEXCOORD2; };
            struct v2f { float4 vertex:SV_POSITION; };
            v2f vert(appdata v)
            {
                v2f o;
                ball p=Place(v.a,v.b,v.c);
                // Thinner on a small ball, none on a very small one or one that is not there.
                float width=_Line*smoothstep(_LineSize.x,_LineSize.y,p.radius);
                float3 world=mul(unity_ObjectToWorld,float4(p.at+v.vertex.xyz*(p.radius+width),1.0)).xyz;
                world+=normalize(world-_WorldSpaceCameraPos)*_LinePush;
                o.vertex=mul(UNITY_MATRIX_VP,float4(world,1.0));
                // No line here: the pass draws nothing of this ball.
                o.vertex=lerp(float4(0,0,-2,1),o.vertex,step(0.0005,width));
                return o;
            }
            fixed4 frag(v2f i):SV_Target { return fixed4(_LineColor.rgb,1); }
            ENDCG
        }
    }
    Fallback Off
}
