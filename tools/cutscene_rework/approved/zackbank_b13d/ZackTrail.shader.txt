// Isagani's Bank Shot trail (`FxStudyZackBank`): a ribbon whose INSIDE churns every frame.
//
// WHY IT IS ITS OWN SHADER. The owner, of trails that slide past as one picture: "it just loooks like stickers and flat",
// and the recipe he sent for a living trail: a trail texture panned along the ribbon and MULTIPLIED BY ITSELF running at a
// different speed, so the bright parts are only where the two copies cross and those crossings move every frame; and a
// panning noise added to the picture coordinates for some wavy motion. The approved ribbon shader reads its mask once,
// so it cannot do this, and it is locked; so this is a separate shader for Zack's trail alone, and nothing else uses it.
//
//   * `_MaskTex` is read twice, at `_Tile1`/`_Pan1` and at `_Tile2`/`_Pan2`, and the second (lifted by `_Mix`) multiplies
//     into the first: a plain product of two greys left only flecks where the two crossed;
//   * `_NoiseTex` panned at its own speed bends the picture coordinates before both reads (`_Distort`);
//   * the vertex is pushed up and down by a wave that runs along the ribbon, stronger toward its tail (`_Wave`);
//   * the colour is a ramp through the result (dark `_Deep`, mid `_Body`, white `_Hot`), as the ribbon shader's is;
//   * it fades over its tail and its head and its two sides (`_Ends`), is drawn only over the open window (`_Window`),
//     and is eaten by raising its threshold (`_Erode`), so it thins and tears rather than fading flat.
// u runs along the ribbon (0 tail, 1 head), v across. Nothing here reads `_Time`: `_Age` is set by whoever plays it.
//
// ⚠️ TURNED TO THE EYE (`_ViewAlign` 1). The owner, of a trail seen coming at him: "the trail looks flat when looking at it
// straight on". A ribbon with a fixed face is a sliver seen edge-on. So a ribbon built for it (every point ON its own
// line, which runs down the object's +z, with its signed distance across the ribbon in uv2.x) is opened out here, per
// point, across the line and square to the eye: the usual trail that turns about its own length to face the camera.
// Seen straight down its length that has no answer, so there it opens out at its own fixed roll instead (`_Roll`,
// degrees), and a trail of strands each at its own roll is seen end-on as a soft fan, a cone coming at the eye.
// `_Thick` scales how wide it opens. At `_ViewAlign` 0 nothing of this happens and it draws as before.
//
// ⚠️ NEAR THE LENS IT DISSOLVES, AND WHAT IS EATEN TEARS INTO WISPS (`_Near`, `_Ragged`; both off at 0, so earlier
// films draw as they did). Seen with the shoe passing close, the nearest strands filled the frame as broad flat panes,
// and pieces eaten along their length were left as scraps with straight cut ends. So a ribbon nearer the eye than
// `_Near.y` metres narrows and fades, and is gone by `_Near.x`; and the threshold it is cut at, and the two ends of the
// window it is drawn in, are pushed about by the noise (`_Ragged` is how far), so every cut edge is torn.
//
// ⚠️ b13: A TRAIL POINT HAS AN AGE, AND ITS WIDTH AND ITS DEATH ARE ITS AGE'S (`_Life`, `_Grow`, `_Die`, `_Wobble`; all
// off at 0, so earlier films draw as they did). A ribbon laid from where a leg began to the shoe knows how long ago the
// shoe passed each of its points: (metres behind the head) / speed, plus the time since its leg ended. So every point
// is born thin at the shoe, swells, thins again and is eaten where it lies, and the trail's outline is the sum of those
// lives, never one shape slid along. The width also wobbles with a noise fixed to the ground and drifting slowly
// (`_Wobble`), so no two frames have the same edge.
//
// ⚠️ b13: PAINTED IN FOUR FLAT VALUES (`_Bands` on). The owner wants painted, neither stickers nor soft photographic
// light: so the result is cut into a veil (`_Deep`, see-through), a body (`_Body`), a fold (`_Fold`) and a white core
// (`_Hot`), each step crisp (`_Bands.y` wide), as Quick Circuit's streak reads: amber air, lemon charge, thin white.
//
// ⚠️ THE MASKS ARE PAINTED IN PLAIN GREY AND THE PROJECT IS LINEAR; `grey()` undoes the darkening.
Shader "TumbangPreso/ZackTrail"
{
    Properties
    {
        _MaskTex ("Trail picture (u along, v across)", 2D) = "white" {}
        _NoiseTex ("Noise that bends it", 2D) = "gray" {}
        _Deep ("Colour at the dark grey", Color) = (1, 0.5, 0.04, 1)
        _Body ("Colour at the mid grey", Color) = (1, 0.64, 0.08, 1)
        _Hot ("Colour at the white", Color) = (1, 0.95, 0.7, 1)
        _Tile1 ("First read: tiling (along, across) and offset", Vector) = (1, 1, 0, 0)
        _Pan1 ("First read: pan a second (along, across)", Vector) = (-1.6, 0.05, 0, 0)
        _Tile2 ("Second read: tiling and offset", Vector) = (1.3, 0.8, 0.37, 0.2)
        _Pan2 ("Second read: pan a second", Vector) = (-2.7, -0.08, 0, 0)
        _Mix ("How much the product is lifted (2 is about the brightness of one read)", Range(1, 4)) = 2.4
        _Distort ("Noise bend: amount, noise tiling along, pan along, pan across", Vector) = (0.06, 1.5, -0.9, 0.3)
        _Wave ("Vertex wave: height (m), waves along it, speed, seed", Vector) = (0.04, 1.5, 6, 0)
        _Cut ("Grey where it begins, and where it is solid", Vector) = (0.10, 0.45, 0, 0)
        _Stops ("Grey of the body colour, and of the white (from, to)", Vector) = (0.35, 0.60, 0.92, 0)
        _Ends ("Fade over the tail, over the head, at each side", Vector) = (0.35, 0.04, 0.12, 0)
        _Window ("Drawn from this u to this u, with this softness", Vector) = (-1, 2, 0.04, 0)
        _Add ("Additive (1 light only, 0 covers)", Range(0, 1)) = 0.75
        _Gain ("Brightness", Range(0, 4)) = 1.2
        _Level ("Picture level: the mask grey is raised to this power (1 reads the picture as painted; under 1 lifts its greys)", Range(0.2, 1)) = 1
        _Age ("Clock (s, set per frame)", Float) = 0
        _Opacity ("Opacity (set per frame)", Range(0, 1)) = 1
        _Erode ("Eaten (0 whole, 1 gone, set per frame)", Range(0, 1)) = 0
        _ViewAlign ("Opens out square to the eye (1) or keeps its built face (0)", Range(0, 1)) = 0
        _Thick ("How wide it opens, when turned to the eye", Float) = 1
        _Roll ("Its roll (degrees) about its own line, used where it is seen end-on", Float) = 0
        _Near ("Gone nearer the eye than x metres, whole beyond y (0 0 is off)", Vector) = (0, 0, 0, 0)
        _Ragged ("How far the noise tears its cut edges and window ends (0 off)", Range(0, 1)) = 0
        _Size ("Length now (m), set per frame", Vector) = (1, 1, 0, 0)
        _Life ("Age of a point: speed of the shoe (m/s, 0 off), seconds added (its leg's end)", Vector) = (0, 0, 0, 0)
        _Grow ("Width by age: share at birth, full at x s... (birth share, full at, thinned share, thinned by)", Vector) = (0.3, 0.06, 0.5, 0.35)
        _Die ("Eaten by age: from x s, gone by y s", Vector) = (0.2, 0.35, 0, 0)
        _Wobble ("Width wobble: amount, noise per metre, drift a second", Vector) = (0, 0.6, 0.4, 0)
        _Bands ("Four flat values: on (x), step softness (y), veil alpha (z), body alpha (w)", Vector) = (0, 0.03, 0.5, 0.85)
        _Fold ("Colour of the fold band", Color) = (1, 0.95, 0.28, 1)
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Blend One OneMinusSrcAlpha
        ZWrite Off
        Cull Off
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #include "UnityCG.cginc"
            sampler2D _MaskTex, _NoiseTex;
            fixed4 _Deep, _Body, _Hot;
            float4 _Tile1, _Pan1, _Tile2, _Pan2, _Distort, _Wave, _Cut, _Stops, _Ends, _Window;
            float _Level;
            float _Mix, _Add, _Gain, _Age, _Opacity, _Erode, _ViewAlign, _Thick, _Roll, _Ragged;
            float4 _Near, _Size, _Life, _Grow, _Die, _Wobble, _Bands;
            fixed4 _Fold;
            struct appdata { float4 vertex:POSITION; float2 uv:TEXCOORD0; float2 across:TEXCOORD1; fixed4 colour:COLOR; };
            struct v2f { float4 vertex:SV_POSITION; float2 uv:TEXCOORD0; fixed4 colour:COLOR; float near:TEXCOORD1; float eaten:TEXCOORD2; };
            float grey(float sampled)
            {
                #ifdef UNITY_COLORSPACE_GAMMA
                return sampled;
                #else
                return LinearToGammaSpaceExact(sampled);
                #endif
            }
            v2f vert(appdata v)
            {
                v2f o;
                // Its age, and what its age does to its width and its life.
                float lifeWidth = 1.0; o.eaten = 0.0;
                if (_Life.x > 0.0)
                {
                    float behind = (1.0 - v.uv.x) * _Size.x, age = behind / _Life.x + _Life.y;
                    lifeWidth = lerp(_Grow.x, 1.0, smoothstep(0.0, _Grow.y, age)) * lerp(1.0, _Grow.z, smoothstep(_Grow.y, _Grow.w, age));
                    o.eaten = smoothstep(_Die.x, _Die.y, age);
                }
                if (_Wobble.x > 0.0)
                {
                    float metres = v.uv.x * _Size.x;
                    float n = tex2Dlod(_NoiseTex, float4(metres * _Wobble.y + _Wobble.z * _Age, 0.37 + _Wobble.w, 0, 0)).r - 0.5;
                    lifeWidth *= max(0.2, 1.0 + 2.0 * n * _Wobble.x);
                }
                // A wave runs along it toward the tail, more at the tail than at the head.
                float free = 1.0 - v.uv.x;
                float phase = v.uv.x * _Wave.y * 6.2832 + _Age * _Wave.z + _Wave.w;
                float lift = sin(phase) * _Wave.x * (0.25 + 0.75 * free), sway = cos(phase * 0.7 + 1.3) * _Wave.x * 0.6 * free;
                if (_ViewAlign > 0.5)
                {
                    float3 centre = mul(unity_ObjectToWorld, float4(v.vertex.xyz, 1)).xyz;
                    float3 along = normalize(mul((float3x3)unity_ObjectToWorld, float3(0, 0, 1)));
                    float3 toEye = normalize(_WorldSpaceCameraPos - centre);
                    float3 square = cross(along, toEye);
                    // Its own roll, for where it is seen down its length.
                    float3 ref = abs(along.y) < 0.95 ? float3(0, 1, 0) : float3(1, 0, 0);
                    float3 b1 = normalize(cross(along, ref)), b2 = cross(along, b1);
                    float r = radians(_Roll);
                    float3 rolled = b1 * cos(r) + b2 * sin(r);
                    float endOn = 1.0 - smoothstep(0.15, 0.45, length(square));
                    float3 side = normalize(lerp(normalize(square + 1e-5), rolled, endOn));
                    float3 face = cross(side, along);
                    // Near the eye it narrows as well as fades, so it never fills the frame as a pane.
                    float near = _Near.y > 0.0 ? smoothstep(_Near.x, _Near.y, distance(_WorldSpaceCameraPos, centre)) : 1.0;
                    float3 world = centre + side * (v.across.x * _Thick * lifeWidth * lerp(0.35, 1.0, near) + sway) + face * lift;
                    o.vertex = mul(UNITY_MATRIX_VP, float4(world, 1));
                    o.near = near;
                }
                else
                {
                    v.vertex.x *= lifeWidth;
                    v.vertex.y += lift;
                    v.vertex.x += sway;
                    o.vertex = UnityObjectToClipPos(v.vertex);
                    o.near = 1.0;
                }
                o.uv = v.uv; o.colour = v.colour;
                return o;
            }
            float4 frag(v2f i):SV_Target
            {
                float2 uv = i.uv;
                float bend = grey(tex2D(_NoiseTex, float2(uv.x * _Distort.y, uv.y) + _Distort.zw * _Age).r) - 0.5;
                float2 bent = uv + float2(bend * 0.5, bend) * _Distort.x;
                float a = grey(tex2D(_MaskTex, bent * _Tile1.xy + _Tile1.zw - _Pan1.xy * _Age).r);
                float b = grey(tex2D(_MaskTex, bent * _Tile2.xy + _Tile2.zw - _Pan2.xy * _Age).r);
                // The first read carries the body; the second, running at its own speed, multiplies into it, so the body
                // churns without being cut down to the few places the two cross (that left only flecks).
                // `_Level` lets a picture with less grey in it be judged at the same trail mass as a fuller one: the cut and the
                // lift run out of room before a thin picture catches up (measured on Bank Shot's candidates, 2026-10-18).
                a = pow(a, _Level); b = pow(b, _Level);
                float m = saturate(a * (0.4 + 0.6 * saturate(b * _Mix))) * i.colour.a;

                float tail = _Ends.x > 0.0 ? smoothstep(0.0, _Ends.x, uv.x) : 1.0;
                float head = _Ends.y > 0.0 ? smoothstep(0.0, _Ends.y, 1.0 - uv.x) : 1.0;
                float side = _Ends.z > 0.0 ? smoothstep(0.0, _Ends.z, uv.y) * smoothstep(0.0, _Ends.z, 1.0 - uv.y) : 1.0;
                // Torn: one more read of the noise, finer, moves the window's ends and the cut, point by point.
                float tear = (grey(tex2D(_NoiseTex, uv * float2(3.1, 2.3) + float2(_Age * 0.37, 0.61)).r) - 0.5) * _Ragged;
                float at = uv.x + tear * 0.35;
                float open = smoothstep(_Window.x - _Window.z, _Window.x + _Window.z, at) * (1.0 - smoothstep(_Window.y - _Window.z, _Window.y + _Window.z, at));
                float v = m * tail * head * side * open;
                // Eaten: the threshold climbs, so what thins first is what was faint, and it tears.
                // The edge it is cut at stays soft however far it is eaten (eaten, the cut narrowed to a hard line, and a
                // strand seen from close by read as a card cut out with scissors).
                float erode = max(_Erode, i.eaten);
                float cutFrom = lerp(_Cut.x, 1.0, erode) + tear * (0.25 + 0.5 * erode), cutTo = cutFrom + max(0.18, lerp(_Cut.y, 1.05, erode) - cutFrom);
                float alpha; float3 colour;
                if (_Bands.x > 0.5)
                {
                    // Four flat values, each step a narrow ramp; eaten, every step climbs with the cut, so the faint veil
                    // goes first and the white core last, and the edges tear with the same noise.
                    float s = _Bands.y, lift = cutFrom - _Cut.x;
                    float veil = smoothstep(cutFrom - s, cutFrom + s, v);
                    float body = smoothstep(_Stops.x + lift - s, _Stops.x + lift + s, v);
                    float fold = smoothstep(_Stops.y + lift - s, _Stops.y + lift + s, v);
                    float core = smoothstep(_Stops.z + lift - s, _Stops.z + lift + s, v);
                    colour = lerp(lerp(lerp(_Deep.rgb, _Body.rgb, body), _Fold.rgb, fold), _Hot.rgb, core);
                    alpha = veil * lerp(_Bands.z, lerp(_Bands.w, 1.0, fold), body) * _Opacity * i.near;
                }
                else
                {
                    alpha = smoothstep(cutFrom, cutTo, v) * _Opacity * i.near;
                    colour = lerp(_Deep.rgb, _Body.rgb, smoothstep(_Cut.x, _Stops.x, v));
                    colour = lerp(colour, _Hot.rgb, smoothstep(_Stops.y, _Stops.z, v));
                }
                colour *= i.colour.rgb;
                return float4(colour * alpha * _Gain, alpha * (1.0 - _Add));
            }
            ENDCG
        }
    }
    Fallback Off
}
