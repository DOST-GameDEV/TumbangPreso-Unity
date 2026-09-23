#ifndef TUMP_ENVIRONMENT_SURFACE_INCLUDED
#define TUMP_ENVIRONMENT_SURFACE_INCLUDED

// Opt-in architectural finishes. Coordinates are metres, independent of the
// retained color-atlas UVs. Role zero leaves the source surface unchanged.
float _SurfaceKind, _SurfaceVertexRoles, _SurfaceCoordinates, _SurfaceScale, _SurfaceStrength, _SurfaceBaseY;
float _SurfaceDebug;
float _SurfaceHasTexture;
float _DeckSurface;
float _WorldArchitecture;
float4 _WorldGlassSky,_WorldGlassHorizon;

float TumpSurfaceHash(float2 p)
{
    float3 q=frac(float3(p.xyx)*.1031);q+=dot(q,q.yzx+33.33);
    return frac((q.x+q.y)*q.z);
}
float TumpSurfaceNoise(float2 p)
{
    float2 i=floor(p),f=frac(p);f=f*f*(3-2*f);
    return lerp(lerp(TumpSurfaceHash(i),TumpSurfaceHash(i+float2(1,0)),f.x),
                lerp(TumpSurfaceHash(i+float2(0,1)),TumpSurfaceHash(i+1),f.x),f.y);
}
float TumpSurfaceJoint(float x,float width)
{
    float edge=min(frac(x),1-frac(x));
    return 1-smoothstep(width,width+max(fwidth(x),.0005),edge);
}

void TumpEnvironmentSurface(float3 world,float3 normal,float4 roles,float2 coordinates,inout SurfaceOutputStandard o)
{
    if(_SurfaceDebug>1.5)
    {
        // Opaque vertex-color study. Transparent sprite shaders cannot be used
        // here: they draw internal floors over the visible facade without depth.
        o.Albedo=roles.rgb*.6;o.Emission=roles.rgb*.4;o.Metallic=0;o.Smoothness=0;return;
    }
    float kind=_SurfaceVertexRoles>.5?floor(roles.r*32+.5):_SurfaceKind;
    if(kind<.5||kind>19.5)return;
    if(_SurfaceDebug>.5)
    {
        o.Albedo=frac(float3(.37,.61,.83)*kind);o.Emission=o.Albedo*.25;
        o.Metallic=0;o.Smoothness=0;return;
    }
    float scale=max(_SurfaceScale,.01);
    // Saved UV3 carries original part axes in metres. Static batching rewrites
    // object matrices, but preserves this attribute, so native and Editor match.
    float3 p=world*scale,n=abs(normal);
    float2 globalPlane=n.y>.65?p.xz:n.x>n.z?p.zy:p.xy;
    float2 uv=_SurfaceCoordinates>.5?coordinates*scale:globalPlane;
    float fine=1-smoothstep(.25,.65,length(fwidth(uv))*24);
    float height=max(0,world.y-_SurfaceBaseY);
    float shade=0,smoothness=.16,metallic=0;
    // Flat disjoint branches avoid the Built-in surface parser stack limit
    // reached by a sixteen-level else-if material chain.
    if(kind<1.5) // Painted mineral plaster: broad repaired paint and finer grain.
    {
        float patches=TumpSurfaceNoise(globalPlane*.65+3.1);
        float grain=(TumpSurfaceNoise(uv*22)-.5)*fine;
        float lower=exp(-height*1.2)*smoothstep(.35,.7,TumpSurfaceNoise(uv*float2(1.8,.25)));
        shade=(patches-.5)*.16+grain*.11-lower*.10;
    }
    if(kind>=1.5&&kind<2.5) // Cast concrete: formwork joints and directional runoff.
    {
        float2 panel=uv/float2(2.4,1.15);
        float seams=max(TumpSurfaceJoint(panel.x,.003),TumpSurfaceJoint(panel.y,.004));
        float cast=TumpSurfaceHash(floor(panel))-.5;
        float streak=TumpSurfaceNoise(uv*float2(3.1,.18));
        shade=cast*.08+(streak-.5)*.09-seams*.14+(TumpSurfaceNoise(uv*13)-.5)*.065*fine;
        smoothness=.13;
    }
    if(kind>=2.5&&kind<3.5) // Civic cut stone: staggered large courses, not a brick wallpaper.
    {
        float2 block=uv/float2(.85,.42);block.x+=frac(floor(block.y)*.5);
        float mortar=max(TumpSurfaceJoint(block.x,.008),TumpSurfaceJoint(block.y,.014));
        shade=(TumpSurfaceHash(floor(block))-.5)*.14-mortar*.18+(TumpSurfaceNoise(uv*17)-.5)*.07*fine;
        smoothness=.11;
    }
    if(kind>=3.5&&kind<4.5) // Timber: assembled boards and lengthwise grain.
    {
        float board=TumpSurfaceJoint(uv.x/.19,.012);
        float grain=sin(uv.x*92+TumpSurfaceNoise(uv*float2(2.8,.28))*7);
        shade=(TumpSurfaceHash(float2(floor(uv.x/.19),0))-.5)*.15-board*.15+grain*.045*fine;
        // Real deck geometry already contains the plank divisions. Do not draw
        // a second.19m grid across each.38m plank. Derivative filtering removes
        // grain before it becomes a subpixel stripe at a player's grazing angle.
        float grainFootprint=fwidth(uv.y)*76;
        float resolved=1-smoothstep(.3,.85,grainFootprint);
        float deckGrain=sin(uv.y*76+TumpSurfaceNoise(uv*float2(.33,4))*4)*.025*resolved;
        float deckWear=(TumpSurfaceNoise(uv*float2(.20,1.6))-.5)*.075;
        shade=lerp(shade,deckWear+deckGrain,saturate(_DeckSurface));
        smoothness=.22;
    }
    if(kind>=4.5&&kind<5.5) // Galvanized/corrugated sheet: ribs and actual sheet seams.
    {
        float rib=cos(uv.x*6.283185/.17);
        float seam=TumpSurfaceJoint(uv.y/1.8,.004);
        float sheet=TumpSurfaceHash(floor(uv/float2(.85,1.8)))-.5;
        float ribs=1-smoothstep(.25,.65,fwidth(uv.x)/.17);
        shade=rib*.085*ribs+sheet*.10-seam*.17;
        smoothness=.25+rib*.035*ribs;metallic=.08;
    }
    if(kind>=5.5&&kind<6.5) // Fired roof tile: staggered courses and rounded tile shoulders.
    {
        float2 tile=uv/float2(.28,.40);tile.x+=frac(floor(tile.y)*.5);
        float joints=max(TumpSurfaceJoint(tile.x,.014),TumpSurfaceJoint(tile.y,.018));
        float curve=sin(frac(tile.x)*3.141593);
        shade=(TumpSurfaceHash(floor(tile))-.5)*.15+curve*.055-joints*.20;
        smoothness=.24;
    }
    if(kind>=6.5&&kind<7.5) // Quiet stylized sky reflection; retain the pane's source tint.
    {
        shade=(TumpSurfaceNoise(uv*float2(2,.12))-.5)*.035;
        smoothness=.64;
        float3 view=normalize(_WorldSpaceCameraPos-world);
        float3 reflected=reflect(-view,normalize(normal));
        float skyHeight=smoothstep(-.12,.65,reflected.y);
        float3 skyColour=lerp(_WorldGlassHorizon.rgb,_WorldGlassSky.rgb,skyHeight);
        float facing=1-saturate(abs(dot(normalize(normal),view)));
        float reflection=(.34+.12*facing)*saturate(_WorldArchitecture*_SurfaceStrength);
        o.Albedo=lerp(o.Albedo,skyColour,reflection);
        smoothness=lerp(smoothness,.40,saturate(_WorldArchitecture));
    }
    if(kind>=7.5&&kind<8.5) // Painted hardware: calmer and less reflective than bare sheet metal.
    {
        shade=(TumpSurfaceNoise(uv*1.7)-.5)*.075;
        smoothness=.30;metallic=.08;
    }
    if(kind>=8.5&&kind<9.5) // Cloth: broad folds, with weave fading before it can shimmer.
    {
        float weave=(sin(uv.x*140)+sin(uv.y*152))*.012*fine;
        shade=(TumpSurfaceNoise(uv*float2(1,3))-.5)*.08+weave;
        smoothness=.08;
    }
    if(kind>=9.5&&kind<10.5) // Clay pots: mineral variation and restrained throwing bands.
    {
        shade=(TumpSurfaceNoise(uv*4)-.5)*.12+sin(uv.y*55)*.018*fine;
        smoothness=.12;
    }
    if(kind>=10.5&&kind<11.5) // Rubber: quiet matte microstructure, no metal wear.
    {
        shade=(TumpSurfaceNoise(uv*24)-.5)*.08*fine;smoothness=.08;
    }
    if(kind>=11.5&&kind<12.5) // Asphalt: broad wear; preserve the original aggregate texture.
    {
        shade=(TumpSurfaceNoise(uv*.31)-.5)*.12;smoothness=.10;
    }
    if(kind>=12.5&&kind<13.5) // Concrete paving: larger slabs, finer joints than the court paint.
    {
        float2 slab=uv/.95;
        float seam=max(TumpSurfaceJoint(slab.x,.003),TumpSurfaceJoint(slab.y,.003));
        if(_SurfaceHasTexture>.5)seam=0; // Existing paving already owns its joints.
        shade=(TumpSurfaceHash(floor(slab))-.5)*.09-seam*.12+(TumpSurfaceNoise(uv*18)-.5)*.045*fine;
        smoothness=.15;
    }
    if(kind>=13.5&&kind<14.5) // Bark: long irregular grain, unlike the regular timber boards.
    {
        float furrow=sin(uv.x*27+TumpSurfaceNoise(uv*float2(2.1,.4))*8);
        shade=furrow*.085*fine+(TumpSurfaceNoise(uv*.8)-.5)*.12;smoothness=.08;
    }
    if(kind>=14.5&&kind<15.5) // Leaf masses: broad varied planes, not repeated leaf photographs.
    {
        shade=(TumpSurfaceNoise((p.xz+p.y*.37)*1.6)-.5)*.10;smoothness=.16;
    }
    if(kind>=15.5&&kind<16.5) // Molded plastic: smooth broad variation; no plaster cracks or wood grain.
    {
        shade=(TumpSurfaceNoise(uv*1.1)-.5)*.04;smoothness=.38;
    }
    if(kind>=16.5&&kind<17.5) // Loose potting earth: broad crumbs, quiet dry micro-grain.
    {
        shade=(TumpSurfaceNoise(uv*4.3)-.5)*.16+(TumpSurfaceNoise(uv*35)-.5)*.055*fine;
        smoothness=.04;
    }
    if(kind>=17.5&&kind<18.5) // Paper/cardboard: dry fibre, without invented wood boards.
    {
        shade=(TumpSurfaceNoise(uv*2.1)-.5)*.055+(TumpSurfaceNoise(uv*48)-.5)*.035*fine;
        smoothness=.06;
    }
    if(kind>=18.5) // Glazed ceramic slabs on the small resident shade roof.
    {
        float2 tile=uv/.30;
        float grout=max(TumpSurfaceJoint(tile.x,.006),TumpSurfaceJoint(tile.y,.006));
        shade=(TumpSurfaceHash(floor(tile))-.5)*.045-grout*.11;
        smoothness=.38;
    }
    // Broad base shade on constructed walls, and a restrained light shoulder on
    // roof sheets/tiles. Grain remains construction-specific above, not a new
    // universal dirt/noise overlay. Signs, leaves, cloth and art remain unchanged.
    if(kind<4.5)shade-=(1-saturate(n.y))*exp(-height*.8)*.10*_WorldArchitecture;
    if(kind>=4.5&&kind<6.5)shade+=saturate(n.y-.2)*.065*_WorldArchitecture;
    o.Albedo*=max(.55,1+shade*_SurfaceStrength);
    o.Smoothness=lerp(o.Smoothness,smoothness,saturate(_SurfaceStrength));
    o.Metallic=lerp(o.Metallic,metallic,saturate(_SurfaceStrength));
}
#endif
