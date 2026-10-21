// The one shader of the ultimate cutscenes' effects kit (`Runtime/Visual/UltimateFx.cs`): wisps, trails, shard bursts,
// impact discs and halftone patches, all drawn the same way.
//
// ⚠️ WHY IT EXISTS. The owner, of the cutscene effects so far: they lack "proper shaders and effects", and "you should be
// more creative with particle effects and do more with emmissives". He pointed at four references (a swept-ribbon "wispy
// explosion", ribbon trails each driven by one small greyscale mask, comic pop explosions, a set of magic effects) and
// said "try doing these". He had already rejected pixel-art sprite explosions and bland dot particles. What the four
// have in common is ONE trick, and this is that trick:
//   * the SHAPE is a mesh with a silhouette of its own (a ribbon swept along a curve, a fan of pointed shards, a disc);
//   * a greyscale MASK runs along it (uv.x is along the ribbon, uv.y across it);
//   * the piece does not fade, it ERODES: it is drawn only where the mask is above a threshold, and the threshold
//     rises with `_Erode` (0 fresh, 1 gone), so it tears into streaks and islands and the last of it is the brightest
//     grain of the mask;
//   * the band just above the threshold is the EDGE colour and the thin line at the threshold is the burning RIM, which
//     is light added on top, so a thing that is tearing glows where it tears.
// The mask is made here, not read from a texture: four looks picked by `_Look` (soft cells, wavy smoke, ink squiggle,
// clean streak), so a new effect needs no new file.
//
// ⚠️ TIME IS `_Age`, set by the stage from its own clock. Nothing here reads `_Time`: a cutscene plays with the match
// paused and is filmed outside Play, and every peer must draw the same frame at the same moment. No depth texture
// either: the stage is drawn by its own camera with no depth pass.
//
// ⚠️ WRITTEN FOR PHONES AS WELL. Every loop has constant bounds, there are no screen-space derivatives, and the dots of
// the halftone are laid out in the mesh's own uv and softened by a fixed width.
//
// Premultiplied: the body is laid over what is behind it and the rim is added to it.
Shader "TumbangPreso/UltimateErode"
{
    Properties
    {
        _Core ("Body colour", Color) = (0.05, 0.46, 0.52, 1)
        _Edge ("Edge colour (the band beside a torn edge)", Color) = (0.82, 0.94, 0.91, 1)
        _Hot ("Burning rim (light, added)", Color) = (0.35, 0.95, 0.88, 1)
        _DarkColor ("Dark mode colour", Color) = (0.01, 0.04, 0.06, 1)
        _Age ("Stage clock (s, set per frame)", Float) = 0
        _Erode ("Erosion (0 fresh, 1 gone, set per frame)", Range(0,1)) = 0
        _Opacity ("Opacity (set per frame)", Range(0,1)) = 1
        _Seed ("Which piece of the mask", Float) = 0
        _Look ("Mask: 0 cells, 1 smoke, 2 ink, 3 streak", Float) = 1
        _Scale ("Mask grain (along, across)", Vector) = (2.2, 1.2, 0, 0)
        _Scroll ("Mask scroll a second (along, across)", Vector) = (1.6, 0, 0, 0)
        _Warp ("Mask warp", Range(0,2)) = 0.6
        _Shape ("How much the ribbon's own edges thin the mask", Range(0,1)) = 0.5
        _Fill ("How much of the mask is filled in (1 is a plain shape that only tears at its taper)", Range(0,1)) = 0
        _Sweep ("Erodes root first (+) or tip first (-)", Range(-2,2)) = 0
        _Band ("Edge band width", Range(0,1)) = 0.2
        _Lead ("Pale leading edge from this v (2 is off)", Range(0,2)) = 2
        _Rim ("Rim width", Range(0,0.3)) = 0.05
        _Glow ("Rim light", Range(0,3)) = 0.6
        _Emit ("Body's own light", Range(0,2)) = 0
        _Soft ("Edge softness", Range(0.005,0.3)) = 0.03
        _Dark ("Dark mode (comic black shards)", Range(0,1)) = 0
        _Radial ("Radial: a disc eroded from its middle out", Range(0,1)) = 0
        _Spread ("Radial: how wide the eroding front is", Range(0.05,2)) = 0.6
        _Dots ("Halftone: draw as dots that follow the mask", Range(0,1)) = 0
        _DotScale ("Halftone dots across the uv", Float) = 14
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
            fixed4 _Core, _Edge, _Hot, _DarkColor;
            float _Age, _Erode, _Opacity, _Seed, _Look, _Warp, _Shape, _Sweep, _Band, _Lead, _Rim, _Glow, _Emit, _Soft;
            float _Dark, _Radial, _Spread, _Dots, _DotScale, _Fill;
            float4 _Scale, _Scroll;
            struct appdata { float4 vertex:POSITION; fixed4 colour:COLOR; float2 uv:TEXCOORD0; };
            struct v2f { float4 vertex:SV_POSITION; float2 uv:TEXCOORD0; fixed4 colour:COLOR; };
            v2f vert(appdata v)
            {
                v2f o;
                o.vertex=UnityObjectToClipPos(v.vertex);
                o.uv=v.uv;
                o.colour=v.colour;
                return o;
            }
            float2 hash2(float2 p)
            {
                p=float2(dot(p,float2(127.1,311.7)),dot(p,float2(269.5,183.3)));
                return frac(sin(p)*43758.5453);
            }
            float noise(float2 p)
            {
                float2 i=floor(p), f=frac(p); f=f*f*(3.0-2.0*f);
                float a=hash2(i).x, b=hash2(i+float2(1,0)).x, c=hash2(i+float2(0,1)).x, d=hash2(i+float2(1,1)).x;
                return lerp(lerp(a,b,f.x),lerp(c,d,f.x),f.y);
            }
            // How far a place is from the nearest of a scattered net of points: dark at each point, bright between them.
            float cells(float2 p)
            {
                float2 cell=floor(p), f=frac(p);
                float nearest=8.0;
                for(int y=-1;y<=1;y++) for(int x=-1;x<=1;x++)
                {
                    float2 g=float2(x,y);
                    float2 o=hash2(cell+g);
                    nearest=min(nearest,length(g+o-f));
                }
                return saturate(nearest*1.25);
            }
            // The four masks. `p.x` is along the piece and has already been scrolled.
            float mask(float2 p)
            {
                float2 bent=p+(float2(noise(p*.7+3.1),noise(p*.7+9.4))-.5)*_Warp*2.0;
                if(_Look<.5)
                    // Soft cells: lace. Holes open in it and it comes apart as a net.
                    return cells(bent);
                if(_Look<1.5)
                {
                    // Wavy smoke: two sizes of soft noise, bent.
                    float n=noise(bent)*.62+noise(bent*2.3+5.7)*.38;
                    return saturate((n-.18)/.64);
                }
                if(_Look<2.5)
                {
                    // Ink squiggle: only the ridges of the noise, so what is left last is thin wandering lines.
                    float n=noise(bent*1.3);
                    float ridge=1.0-abs(2.0*n-1.0);
                    return ridge*ridge;
                }
                // Clean streak: grain drawn out long along the piece.
                float s=noise(float2(p.x*.22,p.y*2.6))*.68+noise(float2(p.x*.5+7.3,p.y*6.1))*.32;
                return saturate((s-.15)/.7);
            }
            fixed4 frag(v2f i):SV_Target
            {
                float2 uv=i.uv;
                float2 at;
                float across=1.0, local, out_=0.0;
                if(_Radial>.5)
                {
                    // A disc. The mask is read round its middle, so its grain is streaks running outward, and it is
                    // eaten from the middle out: the hole's edge is as ragged as the streaks are.
                    float2 p=uv*2.0-1.0;
                    float r=length(p);
                    out_=r;
                    float2 dir=p/max(r,.0001);
                    at=dir*_Scale.y+float2(r*_Scale.x-_Age*_Scroll.x,_Seed*7.31);
                    local=saturate((_Erode*(1.0+_Spread)-r)/_Spread);
                }
                else
                {
                    at=float2(uv.x*_Scale.x-_Age*_Scroll.x+_Seed*7.31,uv.y*_Scale.y-_Age*_Scroll.y+_Seed*3.17);
                    across=1.0-abs(uv.y*2.0-1.0);
                    float from=_Sweep>=0.0?uv.x:1.0-uv.x;
                    float sweep=abs(_Sweep);
                    local=saturate(_Erode*(1.0+sweep)-from*sweep);
                }
                float m=lerp(mask(at),1.0,_Fill);
                float value=m*lerp(1.0,smoothstep(0.0,.7,across),_Shape);
                // A vertex's alpha is its own share of erosion: a ribbon's ends and a disc's rim are always torn, never cut.
                float gone=saturate(local+(1.0-i.colour.a));
                float threshold=lerp(-_Soft-.02,1.02,gone);
                float over=value-threshold;
                float alpha=smoothstep(0.0,_Soft,over);
                float edge=1.0-smoothstep(_Band*.6,_Band+.0001,over);
                edge=max(edge,smoothstep(_Lead,_Lead+.04,uv.y+(m-.5)*.25));
                float rim=(1.0-smoothstep(0.0,_Rim+.0001,over-_Soft))*alpha;
                if(_Dots>.5)
                {
                    // Halftone: the surface is a grid of dots, and a dot is as big as the mask is strong there
                    // (and, on a disc, smaller the further out it is).
                    float2 cell=frac(uv*_DotScale)-.5;
                    float radius=saturate(over-out_*.9)*.6;
                    alpha=(1.0-smoothstep(radius-.07,radius,length(cell)))*step(.02,radius);
                    edge=0.0; rim=0.0;
                }
                float3 col=lerp(_Core.rgb,_Edge.rgb,edge)*(1.0+_Emit);
                col=lerp(col,_DarkColor.rgb,_Dark);
                rim*=1.0-_Dark;
                alpha*=_Opacity;
                return fixed4(col*alpha+_Hot.rgb*rim*_Glow*_Opacity,alpha);
            }
            ENDCG
        }
    }
    Fallback Off
}
