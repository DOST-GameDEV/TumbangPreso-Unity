// Yasmin's ice FLOOR (`CheskaFrostKit`), c3 2026-10-29: COLD FEET's field and the sheet ABSOLUTE ZERO leaves behind.
//
// The owner, of c1's pale painted frost: "yasmins's ground effects genuinely look horrible. they're flat and dont read as
// an icy floor at all". THE reference for all of her effects is Greg Tapper's ice sketch #27 (realtimevfx 10536), owner:
// "this has the closest reference to how i want cheska's vfx in general. the ice we have looks good and matches this
// reference, the other vfx not so much". Read off it (looked at, nothing taken): a glossy floor whose seams SWIRL out
// from a centre, bright crack and seam lines spiralling through it, under a crown of jagged crystals (those are the
// approved DepthB crystals, drawn by their own shader, not here). Secondary, for the daylight read only: Lissandra's
// ultimate in League of Legends, a deep blue body with crisp bright cracks, so the floor reads as ice by VALUE.
// The references are dark rooms with heavy bloom; the court is sunlit with none, so this is four crisp painted bands
// and the body is darker than the road.
//
//   * THICKNESS: the mesh is a slab: a top, a bevelled lip (a thin bright ring) and a dark side face on the road.
//   * DEPTH (DepthB's idea on a floor; owner of the wall: "depth b looks good"): the cells and their seams are read again
//     at a point pushed down along the view, so the inside slides under the top as the camera moves.
//   * THE SWIRL: cell seams turned about the impact point (`_Swirl`) so they run as curved arcs, a cyan line with a white
//     heart over a darker line read a little deeper (cut INTO the ice), and five white spiral seams out of the impact.
//   * GLOSS: a pale sky sheen at grazing views and two long crisp highlight bands from the reflected view.
//   * IT FORMS by spiralling out (`_Front`, run units: 0 the impact, 1 the far edge, with a spiral lag) and THAWS by
//     cracking up (`_Thaw`): whole cells melt, the edge's first, the seams widening. No holes while it stands.
// Solid, writes depth, no texture reads. TEXCOORD0 = (run 0..1, metres to the nearest edge), TEXCOORD1 = metres from
// the impact point, TEXCOORD2.x = part (0 top, 1 lip, 2 side).
Shader "TumbangPreso/CheskaIceSlab"
{
    Properties
    {
        _Top ("Band 1: white", Color) = (0.92, 0.99, 1.00, 1)
        _Light ("Band 2: cyan", Color) = (0.36, 0.84, 0.98, 1)
        _Body ("Band 3: mid blue", Color) = (0.11, 0.38, 0.70, 1)
        _Deep ("Band 4: deep blue", Color) = (0.04, 0.16, 0.40, 1)
        _Side ("Side face", Color) = (0.05, 0.22, 0.48, 1)
        _Sky ("Sheen (sky)", Color) = (0.70, 0.90, 1.00, 1)
        _Seam ("Crack seam", Color) = (0.45, 0.92, 1.00, 1)
        _KeyDir ("Toward the key light (world)", Vector) = (-0.62, 0.60, -0.22, 0)
        _DepthAmt ("How deep the inside reads (m)", Range(0,0.5)) = 0.20
        _BodyBias ("Darker (-) or lighter (+) body", Range(-0.3,0.3)) = 0
        _CellSize ("Crack cell size (m)", Float) = 0.70
        _Swirl ("Swirl (radians a metre out)", Float) = 0.75
        _SeamWidth ("Seam width (m)", Float) = 0.022
        _Gloss ("Highlight bands", Range(0,1)) = 1
        _EdgeScale ("Metres from the edge to the middle", Float) = 2.5
        _Front ("Run (0 impact .. 1 far edge)", Float) = 2
        _SpiralLag ("How far the forming front lags round the spiral (run)", Float) = 0.10
        _Thaw ("Thaw (0 to 1)", Range(0,1.1)) = 0
        _Clip ("World box drawn (minX, maxX, minZ, maxZ)", Vector) = (-10000, 10000, -10000, 10000)
    }
    SubShader
    {
        Tags { "Queue"="Geometry+1" "RenderType"="Opaque" }
        Cull Off
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #include "UnityCG.cginc"
            fixed4 _Top, _Light, _Body, _Deep, _Side, _Sky, _Seam;
            float4 _KeyDir, _Clip;
            float _DepthAmt, _BodyBias, _CellSize, _Swirl, _SeamWidth, _Gloss, _EdgeScale, _Front, _Thaw, _SpiralLag;
            struct appdata { float4 vertex:POSITION; float3 normal:NORMAL; float2 run:TEXCOORD0; float2 metres:TEXCOORD1; float2 part:TEXCOORD2; };
            struct v2f { float4 vertex:SV_POSITION; float3 wn:TEXCOORD0; float3 wpos:TEXCOORD1; float2 run:TEXCOORD2; float2 m:TEXCOORD3; float2 part:TEXCOORD4; };
            v2f vert(appdata v)
            {
                v2f o;
                float4 p=v.vertex;
                o.vertex=UnityObjectToClipPos(p);
                o.wn=UnityObjectToWorldNormal(v.normal);
                o.wpos=mul(unity_ObjectToWorld,p).xyz;
                o.run=v.run; o.m=v.metres; o.part=v.part;
                return o;
            }
            float2 hash22(float2 p)
            {
                p=float2(dot(p,float2(127.1,311.7)),dot(p,float2(269.5,183.3)));
                return frac(sin(p)*43758.5453);
            }
            float hash2(float2 p){ return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453); }
            float vnoise(float2 x)
            {
                float2 i=floor(x), f=frac(x); f=f*f*(3.0-2.0*f);
                return lerp(lerp(hash2(i),hash2(i+float2(1,0)),f.x),lerp(hash2(i+float2(0,1)),hash2(i+float2(1,1)),f.x),f.y);
            }
            // Swirl a point about the impact: the further out, the more it turns, so the cell seams spiral.
            float2 swirl(float2 m)
            {
                float a=length(m)*_Swirl;
                float s=sin(a), c=cos(a);
                return float2(c*m.x-s*m.y, s*m.x+c*m.y);
            }
            // Voronoi in cells of `_CellSize`: x = distance to the nearest seam (metres), y = the cell's own random.
            float2 cells(float2 m)
            {
                float2 q=swirl(m)/_CellSize;
                float2 g=floor(q), f=frac(q);
                float d1=8.0, d2=8.0; float id=0;
                [unroll] for (int y=-1;y<=1;y++)
                [unroll] for (int x=-1;x<=1;x++)
                {
                    float2 o=float2(x,y);
                    float2 r=o+0.12+0.76*hash22(g+o)-f;
                    float d=dot(r,r);
                    if (d<d1) { d2=d1; d1=d; id=hash2(g+o+3.3); }
                    else if (d<d2) d2=d;
                }
                return float2((sqrt(d2)-sqrt(d1))*0.5*_CellSize, id);
            }
            // Five white spiral seams out of the impact point.
            float spiralDist(float2 m)
            {
                float r=length(m)+1e-4;
                float a=atan2(m.y,m.x);
                float k=a/6.2832*5.0-log(r+0.35)*2.2;
                return abs(frac(k)-0.5)/5.0*6.2832*r;   // back to metres along the arc, roughly
            }
            fixed4 frag(v2f i):SV_Target
            {
                clip(i.wpos.x-_Clip.x); clip(_Clip.y-i.wpos.x); clip(i.wpos.z-_Clip.z); clip(_Clip.w-i.wpos.z);
                float3 n=normalize(i.wn);
                float3 l=normalize(_KeyDir.xyz), v=normalize(_WorldSpaceCameraPos-i.wpos);
                float facing=saturate(abs(dot(n,v)));
                float e=0.015;
                // It forms by spiralling out: the front lags round the impact point by up to `_SpiralLag` of the run.
                float ang=atan2(i.m.y,i.m.x)/6.2832+0.5;
                float lag=frac(ang-i.run.x*1.6)*_SpiralLag;
                float shown=_Front*(1.02+_SpiralLag)-i.run.x-lag;
                clip(shown);
                float2 cl=cells(i.m);
                float sd=spiralDist(i.m);
                // The thaw cracks it up: whole cells go, the edge's first, and the seams widen as it goes.
                float edge01=saturate(i.run.y/max(_EdgeScale,0.01));
                float field=edge01*0.60+cl.y*0.40;
                float cut=_Thaw*1.06-0.03;
                clip(field-cut);
                float lead=(_Front<1.0)?(1.0-smoothstep(0.0,0.03,shown)):0.0;
                float aaC=max(fwidth(cl.x),0.002), aaS=max(fwidth(sd),0.002);

                float3 col;
                if (i.part.x>1.5)
                {
                    // THE SIDE: the slab's dark cut face.
                    float s=dot(n,l)*0.5+0.5;
                    col=lerp(_Deep.rgb,_Side.rgb,smoothstep(0.45,0.47,s));
                }
                else if (i.part.x>0.5)
                {
                    // THE LIP: the thin bright ring that marks the edge, white where it faces the light.
                    float s=dot(n,l)*0.5+0.5+saturate(n.y)*0.2;
                    col=lerp(_Light.rgb,_Top.rgb,smoothstep(0.70,0.72,s));
                }
                else
                {
                    // THE TOP. The inside, read under the surface along the view.
                    float vy=max(v.y,0.22);
                    float2 dv=-v.xz/vy;
                    float2 under=cells(i.m+dv*_DepthAmt);
                    float cloud=vnoise((i.m+dv*_DepthAmt)*0.9);
                    float fres=pow(1.0-facing,3.0);
                    // Each cell its own value, darker deep down; lighter toward the thin edge and at grazing views.
                    float val=0.34+_BodyBias+(cl.y-0.5)*0.22+(under.y-0.5)*0.10+(cloud-0.5)*0.12+(1.0-edge01)*0.06+fres*0.32;
                    col=lerp(_Deep.rgb,_Body.rgb,smoothstep(0.32-e,0.32+e,val));
                    col=lerp(col,_Light.rgb,smoothstep(0.58-e,0.58+e,val));
                    col=lerp(col,_Top.rgb,smoothstep(0.80-e,0.80+e,val));
                    // The deep seams: the network seen again under the top, a mid-blue ghost that slides with the view.
                    float ghost=1.0-smoothstep(_SeamWidth*0.8,_SeamWidth*0.8+aaC,under.x);
                    col=lerp(col,_Body.rgb*1.25,ghost*0.6);
                    // The sheen toward a pale sky, crisp-edged so it paints as a band.
                    // Kept to a band at grazing views only: the body must stay darker than the road from a player's eye height.
                    col=lerp(col,_Sky.rgb,smoothstep(0.62,0.67,fres+cloud*0.12)*0.55);
                    // Two long, sharp highlight bands from the reflected view.
                    float3 r=reflect(-v,n);
                    float2 across=float2(0.80,0.60);
                    float sweep=dot(r.xz,across)*2.2+dot(i.wpos.xz,across)*0.035;
                    float hl=max(smoothstep(0.455,0.462,abs(frac(sweep)-0.5)),smoothstep(0.478,0.483,abs(frac(sweep*1.7+0.37)-0.5))*0.85);
                    hl*=smoothstep(0.35,0.55,vnoise(i.m*0.22+1.7));
                    col=lerp(col,_Top.rgb,hl*_Gloss);
                    // The seams cut in: a dark line a little below, offset by the view, then cyan with a white heart.
                    float w=_SeamWidth*(1.0+_Thaw*2.0);
                    float inCut=cells(i.m+dv*0.03).x;
                    float dark=1.0-smoothstep(w*1.4,w*1.4+aaC,inCut);
                    col=lerp(col,_Deep.rgb*0.55,dark*0.9);
                    float seam=1.0-smoothstep(w,w+aaC,cl.x);
                    float heart=1.0-smoothstep(w*0.35,w*0.35+aaC,cl.x);
                    col=lerp(col,_Seam.rgb,seam);
                    col=lerp(col,_Top.rgb,heart);
                    // The spiral seams, white, thinning toward the edge.
                    float sw=_SeamWidth*lerp(0.6,1.1,edge01);
                    float spiral=(1.0-smoothstep(sw,sw+aaS,sd))*smoothstep(0.04,0.25,length(i.m));
                    col=lerp(col,_Top.rgb,spiral);
                }
                col=lerp(col,_Top.rgb,lead);
                return fixed4(col,1);
            }
            ENDCG
        }
    }
    Fallback Off
}
