// The k11 VARIANT of the light of Ilyas's water wall (`RafiWaterVisual.WallGlowVariant` = K11). The approved look is k10,
// drawn by `RafiWallGlow.shader`, which this file leaves alone: the owner passed k10 ("for now rafi's wall is good to go")
// and asked for the faults it still has to be fixed as an option ("still try making the fixes for the issues you pointed
// out"). Same pieces, same inputs, same blend; only how each is painted differs (see the three functions below).
// The light of Ilyas's water wall (`RafiWallCurtain`, behind `RafiWaterVisual.WallGlow`). Three pieces, one shader, picked
// by `_Mode`:
//   0 THE INNER LIGHT. Worn on the wall's own sheet mesh, over the sheet: a pale aqua core strongest low in the middle and
//     along the sheet's falling streaks, fading to the sheet's own cyan at the edges, breathing slowly, with caustic bands
//     drifting UP through it while the water runs down. It is eaten by the sheet's own erosion, so it falls with it.
//   1 THE HALO. A flat card in the wall's plane, a little bigger than the wall: a soft aqua fringe outside the silhouette and
//     a lit rim just inside it, so the edge looks like it gives off light.
//   2 THE SPILL. A card lying on the ground under the wall: an aqua pool brightest at the foot and gone about 1.4 m out on
//     both sides, with slow caustic ripples running through it.
//
// ⚠️ WHY VALUE AND SPILL, AND NOT LIGHT ADDED. The owner, of the k3 film: "the wall itself dont look like its glowing". No
// game camera blooms, so a glow on a sunlit road is sold by two things only: the thing is brighter than anything round it,
// and it colours the ground round it. Each piece is therefore mostly COVER (premultiplied, `_Add` well under 1): additive
// light on a pale road goes pale and reads as nothing (`docs/VFX_BENCH_METHOD.md` section 6).
// ⚠️ NEVER WHITE. The core's red channel is held well under the others, so a pixel of the wall cannot reach the white the
// film counts as clipped even where it lies over the white threads.
// ⚠️ TIME IS `_Age`, set by whoever poses the wall. Nothing reads `_Time`: a film steps it outside Play and every peer draws
// the same frame. Premultiplied, as `FxStudyRibbon` is.
Shader "TumbangPreso/RafiWallGlowK11"
{
    Properties
    {
        _MaskTex ("The sheet's streak picture", 2D) = "white" {}
        _ErodeTex ("The sheet's erosion picture", 2D) = "white" {}
        _CloudTex ("Cloud map (the caustics)", 2D) = "white" {}
        _Mode ("0 inner light, 1 halo, 2 spill", Float) = 0
        _Edge ("Cyan at the edge", Color) = (0.00, 0.66, 0.90, 1)
        _Body ("Aqua", Color) = (0.22, 0.90, 1.00, 1)
        _Core ("Pale aqua core", Color) = (0.70, 1.00, 0.96, 1)
        _Age ("Clock (s, set per frame)", Float) = 0
        _Opacity ("How lit (set per frame)", Range(0,1)) = 1
        _Strength ("Strength (`RafiWaterVisual.WallGlowStrength`)", Range(0,2)) = 1
        _Erode ("The sheet's erosion (set per frame)", Range(0,1)) = 0
        _Tile ("The sheet's tiling", Vector) = (1, 1, 0, 0)
        _Scroll ("The sheet's scroll", Vector) = (0, 0, 0, 0)
        _ErodeTile ("The sheet's erosion tiling", Vector) = (1, 1, 0, 0)
        _ErodeScroll ("The sheet's erosion scroll", Vector) = (0, 0, 0, 0)
        _Sweep ("The sheet's erosion sweep", Float) = 0
        _Soft ("The sheet's erosion softness", Float) = 0.2
        _Warp ("The sheet's warp", Vector) = (0, 2, 0, 1)
        _Quad ("The card in metres: wide, deep, middle x, middle y", Vector) = (5, 2.3, 0, 1.15)
        _Box ("The wall: half width, top, reach of the fringe or the spill (m), how far it has fallen", Vector) = (2, 1.76, 0.4, 0)
        _Add ("Share laid on as light (1) rather than covering (0)", Range(0,1)) = 0.4
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
            sampler2D _MaskTex, _ErodeTex, _CloudTex;
            fixed4 _Edge, _Body, _Core;
            float _Mode, _Age, _Opacity, _Strength, _Erode, _Sweep, _Soft, _Add;
            float4 _Tile, _Scroll, _ErodeTile, _ErodeScroll, _Warp, _Quad, _Box;
            struct appdata { float4 vertex:POSITION; fixed4 colour:COLOR; float2 uv:TEXCOORD0; };
            struct v2f { float4 vertex:SV_POSITION; float2 uv:TEXCOORD0; float2 p:TEXCOORD1; float facing:TEXCOORD2; fixed4 colour:COLOR; };
            v2f vert(appdata v)
            {
                v2f o;
                if(_Mode<0.5)
                {
                    // The sheet's own warp (`FxStudyRibbon`), number for number, so the light never slides off the water.
                    float along=v.uv.x*_Warp.y*6.2832+_Warp.z;
                    float drift=_Age*_Warp.w;
                    v.vertex.xyz+=float3(sin(along+drift*1.3),cos(along*0.7+drift+1.9),sin(along*1.3-drift*0.8+4.1))*_Warp.x*(0.35+0.65*v.uv.x);
                }
                o.vertex=UnityObjectToClipPos(v.vertex);
                o.uv=v.uv;
                o.colour=v.colour;
                // A card in metres: the upright halo reads x and y, the lying spill x and z.
                float2 flat=_Mode>1.5? v.vertex.xz : v.vertex.xy;
                o.p=flat*_Quad.xy+_Quad.zw;
                // The halo is a flat card: seen edge-on it would be a smear, so it thins as it turns away.
                float3 world=mul(unity_ObjectToWorld,v.vertex).xyz;
                float3 n=normalize(UnityObjectToWorldNormal(float3(0,0,1)));
                o.facing=abs(dot(n,normalize(_WorldSpaceCameraPos-world)));
                return o;
            }
            // The grey the pictures were painted in (the project is linear).
            float grey(float sampled)
            {
                #ifdef UNITY_COLORSPACE_GAMMA
                return sampled;
                #else
                return LinearToGammaSpaceExact(sampled);
                #endif
            }
            float cloud(float2 q) { return grey(tex2D(_CloudTex,q).r); }
            // Caustics: two drifting reads of the cloud map, bright where they nearly agree, which draws the thin
            // linked cells light makes on the floor of a pool.
            float caustic(float2 q,float2 a,float2 b,float width)
            {
                float c1=cloud(q+a), c2=cloud(q*1.31+b+0.37);
                return 1.0-smoothstep(0.0,width,abs(c1-c2));
            }
            float3 ramp(float k)
            {
                float3 c=lerp(_Edge.rgb,_Body.rgb,smoothstep(0.05,0.40,k));
                return lerp(c,_Core.rgb,smoothstep(0.38,0.88,k));
            }
            // ---------------------------------------------------------------- the k11 variant
            // The owner, passing k10, asked for the faults it still has to be fixed as a variant:
            //   * at its peak the middle puffs into a milky patch that reads as fog;
            //   * the pool on the ground is one even oval that does not move;
            //   * the fringe is one thickness all round.
            float4 innerK11(v2f i,float s)
            {
                float2 uv=i.uv;
                float2 eat=uv*_ErodeTile.xy+_ErodeTile.zw-_ErodeScroll.xy*_Age;
                float e=grey(tex2D(_ErodeTex,eat).r)+_Sweep*(uv.x-0.5);
                float span=1.0+abs(_Sweep)+_Soft*2.0;
                float threshold=_Erode*span-abs(_Sweep)*0.5-_Soft;
                float kept=smoothstep(threshold,threshold+_Soft,e);
                float2 at=uv*_Tile.xy+_Tile.zw-_Scroll.xy*_Age;
                float mask=grey(tex2D(_MaskTex,at).r)*i.colour.a;
                // Streak cores cut tighter than k10's, so the light is in the water and not between it.
                float streak=smoothstep(0.48,0.82,mask);
                float across=abs(uv.y-0.5)*2.0;
                float mid=1.0-smoothstep(0.30,1.0,across);
                float low=lerp(1.0,0.42,smoothstep(0.15,0.90,uv.x))*smoothstep(0.0,0.04,uv.x);
                float core=mid*low;
                float2 m=float2(uv.y*4.0,uv.x*2.1);
                float breathe=0.84+0.16*sin(_Age*1.35+uv.y*2.6+uv.x*1.2);
                // ⚠️ CAUSTIC BANDS IN THE WATER, NOT A PUFF ON IT. Light through moving water lies in wavering bands. Here
                // they run across the sheet, bent by slow noise, climbing against the fall at about a third of a metre a
                // second, and are broken into lengths by the cloud map so no band runs the full 4 m (a ruled ring reads as
                // a scan line). They are strongest low in the middle and on a streak.
                float height=m.y+0.22*(cloud(float2(uv.y*1.1+0.37,_Age*0.04))-0.5);
                float wave=0.5+0.5*sin((height-_Age*0.34)*10.0);
                float band=pow(wave,3.0)*smoothstep(0.40,0.68,cloud(m*float2(0.36,0.30)+float2(0.29,-_Age*0.12)));
                // The gaps keep an aqua light held well under the pale core: k10's gap light reached the core colour, and
                // that was the milky patch.
                // ⚠️ THE GAPS GLOW AS STRONGLY AS k10's BUT NEVER TURN PALE: how much light (alpha) and what colour are
                // split, the colour held at saturated aqua. v11a capped the light itself and the whole wall went dim.
                float gap=core*(0.50+0.30*smoothstep(0.48,0.78,cloud(m*float2(0.30,0.42)+float2(0.13,-_Age*0.16))))*breathe;
                float lit=streak*(0.48+0.62*core)*(0.90+0.10*breathe);
                // The bands are laid OVER the body's light, not compared with it: under a max they lost to the gap light
                // everywhere and never showed (v11b).
                float lift=band*core*(0.55+0.45*streak);
                float k=saturate(max(gap,lit*1.08)+0.42*lift);
                float tone=saturate(max(min(gap,0.40),lit*1.08)+0.60*lift);
                k=saturate(k*lerp(0.6,1.0,s));
                float a=smoothstep(0.04,0.75,k)*0.90*s*_Opacity*kept;
                float3 c=ramp(tone*lerp(0.6,1.0,s));
                float add=lerp(0.80,0.30,saturate(streak+lift*0.6));
                return float4(c*a,a*(1.0-add));
            }
            float4 haloK11(v2f i,float s)
            {
                float2 p=i.p;
                float top=_Box.y;
                float r=0.38;
                float2 h=float2(_Box.x,top*0.5);
                float2 q=abs(p-float2(0.0,top*0.5))-h+r;
                float d=length(max(q,0.0))+min(max(q.x,q.y),0.0)-r;
                d+=0.10*(cloud(float2(p.x*0.30+0.21,p.y*0.25+_Age*0.04))-0.5);
                // ⚠️ STRONGEST AT THE FOOT, FADING TOWARD THE CREST: the light is thickest where the water is, so the
                // fringe both reaches further and burns brighter low down.
                float up=smoothstep(0.0,top+0.25,p.y);
                float reach=_Box.z*lerp(1.35,0.65,up);
                float fringe=step(0.0,d)*pow(1.0-smoothstep(0.0,reach,d),3.0);
                float rim=step(d,0.0)*(1.0-smoothstep(0.0,lerp(0.30,0.16,up),-d))*0.50;
                float shimmer=0.70+0.30*cloud(p*float2(0.9,0.6)+float2(0.4,-_Age*0.28));
                // The sheet thins at its two ends; a fringe as strong there as at the foot stood beside it as a lit post.
                float ends=lerp(1.0,0.55,smoothstep(_Box.x-0.45,_Box.x+0.05,abs(p.x))*smoothstep(0.25,0.75,up));
                float k=(fringe+rim)*shimmer*ends*lerp(1.10,0.35,up);
                float a=saturate(k*0.66)*s*_Opacity*smoothstep(0.12,0.45,i.facing)*step(0.0,p.y);
                float3 c=lerp(_Body.rgb,_Core.rgb,rim*0.6);
                return float4(c*a,a*(1.0-_Add));
            }
            float4 spillK11(v2f i,float s)
            {
                float2 p=i.p;
                float d=length(float2(max(abs(p.x)-_Box.x,0.0),p.y));
                // ⚠️ AN UNEVEN EDGE THAT DRIFTS: the reach is pushed in and out by slow noise, more the further out, so the
                // pool is a body of light and not a stamped oval.
                float wobble=cloud(p*0.11+float2(_Age*0.03,-_Age*0.025))-0.5;
                float dn=d*(1.0+1.30*wobble*smoothstep(0.10,0.60,d/_Box.z));
                float pool=smoothstep(0.0,1.0,saturate(1.15-1.15*dn/_Box.z));
                // ⚠️ CAUSTICS THAT MOVE: smaller, brighter cells than k10's, drifting two ways at two speeds, laid on as
                // paler light inside the pool (colour and a little cover), never as dark lines.
                float ripple=caustic(p*0.52,float2(_Age*0.10,_Age*0.14),float2(-_Age*0.12,_Age*0.06),0.22);
                float ripple2=caustic(p*0.31+1.7,float2(-_Age*0.05,_Age*0.09),float2(_Age*0.07,-_Age*0.04),0.24);
                float cells=saturate(0.65*ripple+0.45*ripple2);
                float rings=0.5+0.5*sin(dn*7.0-_Age*2.6);
                float k=pool*(0.55+0.45*cells)*(0.84+0.16*rings);
                float a=saturate(k*1.05)*s*_Opacity;
                // The cells light the pool in its own aqua; pushed toward the pale core they turned the near pool milky (v11b).
                float3 c=ramp(saturate(0.25+0.50*pool+0.14*cells*pool+0.30*_Box.y*pool));
                return float4(c*a,a*(1.0-_Add));
            }
            float4 frag(v2f i):SV_Target
            {
                float s=saturate(_Strength);
                if(_Mode<0.5) return innerK11(i,s);
                if(_Mode<1.5) return haloK11(i,s);
                return spillK11(i,s);
            }
            ENDCG
        }
    }
    Fallback Off
}
