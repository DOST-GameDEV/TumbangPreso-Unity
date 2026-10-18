// Yasmin's GLACIAL WALL ice, IN BETWEEN (`CheskaGlacialWallLook`, styles Between A/B/C and Chunky Between A/B/C).
// The owner, of the g3 style board: "Painted, Chunky and Blocky all look the same to me. try to give me an in-between
// texture design. and something slightly more cyan". Crystal (the approved crystal shader) read glassy and realistic;
// Painted (`CheskaWallIce`) read as one flat blue sticker. This sits between them:
//   * FOUR CRISP VALUE BANDS, from a painted light that mixes the key light, the face's tilt to the sky and its
//     height, so the bands show from every side of the wall and not only from the key light's side: a pale top,
//     a light face, the cyan body, a deeper crease. Faces can be cut flat (`_Facet`) from screen derivatives, so even
//     the crystal meshes paint as broad planes.
//   * A BRIGHT FROSTED RIM at the silhouette and on upturned edges, crisp, so the wall holds against a pale road.
//   * PAINTED INNER STREAKS AND CRACKS in the piece's own rest space (they stick to it as it rises): slanted light
//     streaks and thin bright crack lines, both from one cheap value noise.
//   * A LITTLE SEE-THROUGH DEPTH without refraction: the streak pattern is read a second time, pushed back along the
//     view inside the piece and drawn darker, so it slides behind the surface as the camera moves.
//   * IT LIVES: a slow frost shimmer band climbs each piece, and a crawl of small sparkles rises through the lit faces.
// The rise out of the road and the sink back in are the per-vertex growth `CheskaWallIce` uses (TEXCOORD1 the piece's
// foot, TEXCOORD2 its birth, height, death and seed), on `_Age` set by the owner.
// SOLID, writes depth, no texture reads, a few value-noise evaluations a pixel.
Shader "TumbangPreso/CheskaWallBetween"
{
    Properties
    {
        _Top ("Band 1: pale top", Color) = (0.88, 1.00, 0.99, 1)
        _Light ("Band 2: light face", Color) = (0.58, 0.93, 0.96, 1)
        _Mid ("Band 3: cyan body", Color) = (0.30, 0.79, 0.89, 1)
        _Crease ("Band 4: crease", Color) = (0.13, 0.52, 0.70, 1)
        _RimColor ("Frosted rim", Color) = (0.94, 1.00, 1.00, 1)
        _Deep ("Inner depth tint", Color) = (0.10, 0.45, 0.62, 1)
        _KeyDir ("Toward the key light (world)", Vector) = (-0.62, 0.60, -0.22, 0)
        _Contrast ("Band contrast", Range(0,1.2)) = 0.85
        _Facet ("Flat facets", Range(0,1)) = 0.8
        _Rim ("Rim strength", Range(0,1)) = 0.9
        _Streaks ("Inner streaks", Range(0,1)) = 0.5
        _Cracks ("Crack lines", Range(0,1)) = 0.5
        _Depth ("See-through depth", Range(0,1)) = 0.45
        _Sparkle ("Crawling sparkle", Range(0,1)) = 0.8
        _Shimmer ("Climbing shimmer", Range(0,1)) = 0.14
        // g6, the owner: "glacial wall designs are still lacking the depth detail the realistic one had". All off by
        // default, so the g4/g5 variants draw exactly as they were filmed.
        [NoScaleOffset] _MaskTex ("Crystal bake: R glow-through, A thickness (linear)", 2D) = "black" {}
        _HasMask ("Use the crystal's baked thickness (0 or 1)", Float) = 0
        _Core ("Dark core where the ice is thick", Range(0,1)) = 0
        _CoreColor ("Core colour", Color) = (0.03, 0.33, 0.50, 1)
        _Shell ("Frosted shell where it is thin", Range(0,1)) = 0
        _Inner ("Veins inside, at three depths", Range(0,1.5)) = 0
        _InnerDepth ("How deep the inside goes (m)", Range(0,0.6)) = 0.30
        _VeinHot ("Vein near the surface", Color) = (0.86, 1.00, 1.00, 1)
        _VeinDeep ("Vein deep inside", Color) = (0.20, 0.78, 0.92, 1)
        _Cloud ("Cloudy inner volume", Range(0,1)) = 0
        _Glint ("Glint size", Range(0.5,2.5)) = 1
        _Age ("Owner's clock (s)", Float) = 0
        _Lit ("Lit from inside (1 is normal)", Range(0,3)) = 1
        _GrowTime ("Seconds a piece takes to rise", Float) = 0.24
        _DieAt ("Owner's clock at which the sinking starts", Float) = 100000
        _DieTime ("Seconds a piece takes to sink", Float) = 0.40
    }
    SubShader
    {
        Tags { "Queue"="Geometry" "RenderType"="Opaque" }
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #include "UnityCG.cginc"
            fixed4 _Top, _Light, _Mid, _Crease, _RimColor, _Deep;
            float4 _KeyDir;
            float _Contrast, _Facet, _Rim, _Streaks, _Cracks, _Depth, _Sparkle, _Shimmer;
            float _Age, _Lit, _GrowTime, _DieAt, _DieTime;
            sampler2D _MaskTex;
            float _HasMask, _Core, _Shell, _Inner, _InnerDepth, _Cloud, _Glint;
            fixed4 _CoreColor, _VeinHot, _VeinDeep;
            struct appdata { float4 vertex:POSITION; float3 normal:NORMAL; float2 uv:TEXCOORD0; float4 foot:TEXCOORD1; float4 life:TEXCOORD2; };
            struct v2f
            {
                float4 vertex:SV_POSITION; float3 normal:TEXCOORD0; float3 wpos:TEXCOORD1; float3 rest:TEXCOORD2;
                float4 info:TEXCOORD3; float3 viewObj:TEXCOORD4; float2 uv:TEXCOORD5;
            };
            v2f vert(appdata v)
            {
                v2f o;
                float g=saturate((_Age-v.life.x)/_GrowTime);
                float rise=1.0-(1.0-g)*(1.0-g)*(1.0-g);
                float bump=sin(saturate((g-0.45)/0.55)*3.14159);
                float d=saturate((_Age-_DieAt-v.life.z)/_DieTime);
                d=d*d*(3.0-2.0*d);
                float3 rel=v.vertex.xyz-v.foot.xyz;
                float h=rel.y;
                rel.y*=(1.0+0.12*bump)*(1.0-0.20*d);
                rel.xz*=(lerp(0.62,1.0,rise)-0.06*bump)*(1.0-0.40*d);
                rel.y-=(1.0-rise)*v.life.y*1.08+d*v.life.y*1.05;
                float4 p=float4(v.foot.xyz+rel,1.0);
                o.vertex=UnityObjectToClipPos(p);
                o.normal=UnityObjectToWorldNormal(v.normal);
                o.wpos=mul(unity_ObjectToWorld,p).xyz;
                // Rest space, with each piece offset by its seed, so neighbours do not share one pattern.
                o.rest=v.vertex.xyz+v.life.w*float3(3.17,0.0,1.93);
                o.info=float4(h, max(0.05,v.life.y), v.life.w, 0);
                float3 camObj=mul(unity_WorldToObject,float4(_WorldSpaceCameraPos,1)).xyz;
                o.viewObj=camObj-p.xyz;
                o.uv=v.uv;
                return o;
            }
            float hash3(float3 p){ p=frac(p*0.3183099+0.1); p*=17.0; return frac(p.x*p.y*p.z*(p.x+p.y+p.z)); }
            float vnoise(float3 x)
            {
                float3 i=floor(x), f=frac(x); f=f*f*(3.0-2.0*f);
                return lerp(lerp(lerp(hash3(i),hash3(i+float3(1,0,0)),f.x),lerp(hash3(i+float3(0,1,0)),hash3(i+float3(1,1,0)),f.x),f.y),
                            lerp(lerp(hash3(i+float3(0,0,1)),hash3(i+float3(1,0,1)),f.x),lerp(hash3(i+float3(0,1,1)),hash3(i+float3(1,1,1)),f.x),f.y),f.z);
            }
            // Slanted painted streaks: long, thin, warped a little; bright along a spine, soft to the edges.
            float streaks(float3 p)
            {
                float w=vnoise(p*float3(1.4,0.6,1.4));
                float u=p.y*0.55+(p.x*0.8+p.z*0.5)+w*0.9;
                float s=abs(frac(u*1.6)-0.5)*2.0;
                return smoothstep(0.66,0.92,s)*smoothstep(0.25,0.6,vnoise(p*float3(0.9,0.35,0.9)+7.1));
            }
            // Fractures INSIDE the ice: two families of nearly flat planes and one family of curved sheets, each cut
            // into pieces by a slow noise. Read at a point pushed back along the view, a plane sits under the surface
            // and slides against it as the camera moves, which is what reads as looking into the ice.
            float veins(float3 q, float seed)
            {
                float warp=(vnoise(q*0.9+1.7+seed)-0.5)*0.30;
                float3 d1=normalize(float3(0.55+0.3*frac(seed*0.37),1.0,0.30));
                float3 d2=normalize(float3(-0.85,0.45,0.35+0.3*frac(seed*0.61)));
                float l1=abs(frac(dot(q,d1)*1.10+warp)-0.5);
                float l2=abs(frac(dot(q,d2)*0.90-warp+0.31)-0.5);
                float l3=abs(vnoise(q*float3(1.6,0.9,1.6)+seed*3.1)-0.5);
                float a=(1.0-smoothstep(0.006,0.022,l1))*smoothstep(0.42,0.58,vnoise(q*1.1+11.0));
                float b=(1.0-smoothstep(0.006,0.020,l2))*smoothstep(0.48,0.62,vnoise(q*1.3+23.0));
                float c=(1.0-smoothstep(0.010,0.030,l3))*smoothstep(0.50,0.66,vnoise(q*0.8+37.0));
                return saturate(max(max(a,b),c*0.8));
            }
            fixed4 frag(v2f i):SV_Target
            {
                float3 ns=normalize(i.normal);
                float3 nf=normalize(cross(ddy(i.wpos),ddx(i.wpos)));
                nf*=sign(dot(nf,ns)+1e-4);
                float3 n=normalize(lerp(ns,nf,_Facet));
                float3 l=normalize(_KeyDir.xyz), v=normalize(_WorldSpaceCameraPos-i.wpos);
                float h01=saturate(i.info.x/i.info.y);
                // The painted light: key light wrapped, tilt to the sky, and height up the piece.
                float ndl=dot(n,l)*0.5+0.5;
                float paint=ndl*0.78+saturate(n.y)*0.30+h01*0.22-0.13;
                // Creases: low on the piece and faces turned down are deeper.
                paint-=(1.0-smoothstep(0.0,0.22,i.info.x))*0.22+saturate(-n.y)*0.12;
                // Four crisp bands.
                float e=0.015;
                float b1=smoothstep(0.30-e,0.30+e,paint), b2=smoothstep(0.50-e,0.50+e,paint), b3=smoothstep(0.70-e,0.70+e,paint);
                // The full crease is kept for the lower part of a piece; higher up the darkest step is half a crease,
                // so the small chips in the crystal meshes do not paint as dark dents.
                b1=max(b1,smoothstep(0.30,0.55,h01)*0.55);
                float3 col=lerp(_Crease.rgb,_Mid.rgb,b1);
                col=lerp(col,_Light.rgb,b2);
                col=lerp(col,_Top.rgb,b3);
                col=lerp(col,_Top.rgb,smoothstep(0.80,0.86,n.y));
                col=lerp(_Mid.rgb,col,_Contrast);

                float3 p=i.rest;
                float face=saturate(dot(n,v));
                // See-through depth: the same streaks read again further inside, along the view, drawn deep.
                float3 vo=normalize(i.viewObj);
                float inner=streaks(p-vo*0.32)*0.8+ (vnoise((p-vo*0.20)*2.2)-0.5)*0.6;
                col=lerp(col,_Deep.rgb,saturate(inner)*_Depth*face*0.45*b1*(1.0-b3*0.6));
                col=lerp(col,_Deep.rgb,_Depth*0.18*face*(1.0-b2));
                // Painted streaks on the surface, lighter than the band they cross.
                float st=streaks(p);
                col=lerp(col,lerp(col,_Top.rgb,0.75),st*_Streaks*(1.0-b3));
                // Thin bright crack lines: two families of nearly straight cuts at a slant, barely warped, broken
                // into segments by a slow noise, so they read as painted fractures and not as scribbles.
                float warp=(vnoise(p*0.9+1.7)-0.5)*0.25;
                float3 d1=normalize(float3(0.55+0.3*frac(i.info.z*0.37),1.0,0.30));
                float3 d2=normalize(float3(-0.85,0.45,0.35+0.3*frac(i.info.z*0.61)));
                float l1=abs(frac(dot(p,d1)*0.85+warp)-0.5);
                float l2=abs(frac(dot(p,d2)*0.70-warp+0.31)-0.5);
                float crack=max((1.0-smoothstep(0.004,0.016,l1))*smoothstep(0.50,0.62,vnoise(p*1.1+11.0)),
                                (1.0-smoothstep(0.004,0.014,l2))*smoothstep(0.55,0.66,vnoise(p*1.3+23.0)));
                col=lerp(col,_RimColor.rgb,crack*_Cracks*0.85);
                // THE INSIDE (g6). How thick the ice is behind this point: the crystal's own bake where there is one,
                // otherwise a convex piece's guess (faced straight on and low down is thick, grazing and the tip thin).
                float thick=face*face*(1.0-0.55*h01*h01);
                if (_HasMask>0.5) thick=lerp(thick,tex2D(_MaskTex,i.uv).a,0.7);
                float thin=1.0-thick;
                // A dark saturated core where it is thick; the bands stay on top of it, only pushed down.
                col=lerp(col,_CoreColor.rgb*lerp(0.75,1.15,b2),saturate(thick*1.25-0.15)*_Core);
                // Cloudy inner volume, read once at mid depth.
                float cloud=vnoise((p-vo*_InnerDepth*0.55)*1.8+5.0);
                col=lerp(col,_CoreColor.rgb*0.8,smoothstep(0.45,0.75,cloud)*thick*_Cloud);
                // Veins at three depths: brighter and paler near the surface, deeper and more saturated further in.
                float sd=i.info.z*0.71;
                float v1=veins(p-vo*_InnerDepth*0.25,sd);
                float v2=veins(p-vo*_InnerDepth*0.60+1.3,sd+2.0);
                float v3=veins(p-vo*_InnerDepth*1.00+2.9,sd+4.0);
                float inside=saturate(0.25+0.75*thick);
                col=lerp(col,_VeinDeep.rgb*0.85,v3*_Inner*0.55*inside);
                col=lerp(col,lerp(_VeinDeep.rgb,_VeinHot.rgb,0.5),v2*_Inner*0.70*inside);
                col=lerp(col,_VeinHot.rgb,v1*_Inner*0.85*inside);
                // A lighter frosted shell where it is thin, toward the edges and tips.
                col=lerp(col,_Light.rgb*1.04,smoothstep(0.35,0.85,thin)*_Shell);
                // The frosted rim: crisp at the silhouette.
                float fres=1.0-face;
                col=lerp(col,_RimColor.rgb,smoothstep(0.58,0.66,fres)*_Rim);
                // Climbing frost shimmer: one soft band a piece at its own phase.
                float band=frac(i.info.x*0.40-_Age*0.32+i.info.z*0.137);
                col=lerp(col,_Light.rgb*1.08,smoothstep(0.0,0.05,band)*(1.0-smoothstep(0.05,0.11,band))*_Shimmer);
                // Crawling sparkle: one jittered point per cell, cells drifting up, each blinking at its own phase.
                float3 q=p*6.0+float3(0.0,-_Age*1.2,0.0);
                float3 c=floor(q), f=frac(q)-0.5;
                float hs=hash3(c);
                float3 off=float3(hash3(c+5.1),hash3(c+9.7),hash3(c+2.3))-0.5;
                float3 fo=f-off*0.6;
                float3 ta=normalize(cross(n,float3(0.3,1.0,0.2))), tb=cross(n,ta);
                float2 g2=float2(dot(fo,ta),dot(fo,tb));
                float near=1.0-smoothstep(0.10,0.16,abs(dot(fo,n)));
                float2 ag=abs(g2);
                // Arms taper from the heart to a point; a cell is 1/6 m, so an arm is about 6 cm and a heart 1 cm.
                float2 tw=0.034*_Glint*saturate(1.0-ag/(0.36*_Glint));
                float arms=max(1.0-smoothstep(tw.x*0.5,tw.x,ag.y),1.0-smoothstep(tw.y*0.5,tw.y,ag.x));
                float core=1.0-smoothstep(0.035*_Glint,0.055*_Glint,length(g2));
                float blink=pow(saturate(sin(_Age*4.0+hs*6.2832)),3.0);
                float glint=saturate(max(arms*(0.35+0.65*blink),core))*blink;
                float spark=glint*near*step(0.55,hs)*(0.35+0.65*b2);
                col=lerp(col,float3(1,1,1),spark*_Sparkle);
                col*=lerp(1.0,1.25,saturate(_Lit-1.0));
                return fixed4(col,1);
            }
            ENDCG
        }
    }
    Fallback Off
}
