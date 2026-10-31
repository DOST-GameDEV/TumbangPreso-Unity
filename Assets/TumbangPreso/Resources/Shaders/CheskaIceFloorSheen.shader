// ABSOLUTE ZERO's floor, its depth and shine (`FxStudyLotusFrost`). The owner, of the floes beside a crystal: "should also be a
// bit more shiny/reflective and its lacking that deep blue/cyan to it". The floes themselves stay in the Glacial Wall's locked
// material; this is a second draw of the same mesh over them, and the wall knows nothing of it.
//   * DEPTH: lays the wall's darkest face colour over a floe, deepest at its middle and nothing at its edge (TEXCOORD3.x, metres
//     from the floe's outline), so the frost and bevels stay pale and the body goes deep.
//   * SHINE: what a flat wet-looking sheet of ice does and a painted floor does not: it changes with where you stand. A crisp
//     mirror of the key light; the sky by fresnel, strong only at low angles; a few upright streaks standing in for the
//     reflections of crystals and buildings, read off the reflected direction so they glide as the camera moves. A slow wave in
//     the normal bends them a little. No texture reads; one value noise a pixel.
// The rise and sink are the wall's growth (`CheskaWallBetween`), written out the same, every vertex its own foot.
Shader "TumbangPreso/CheskaIceFloorSheen"
{
    Properties
    {
        _DeepIce ("The wall's darkest face", Color) = (0.20, 0.59, 0.70, 1)
        _Depth ("How far the middle of a floe goes to it", Range(0,1)) = 0.92
        _DepthOver ("Metres from the edge to full depth", Float) = 0.9
        _Sky ("Reflected sky", Color) = (0.70, 0.93, 1.0, 1)
        _KeyDir ("Toward the key light (world)", Vector) = (-0.62, 0.60, -0.22, 0)
        _SkyStrength ("Sky by fresnel", Range(0,1)) = 0.28
        _Glint ("Key light mirror", Range(0,2)) = 0.9
        _Streaks ("Upright reflected streaks", Range(0,1)) = 0.32
        _Age ("Owner's clock (s)", Float) = 0
        _GrowTime ("Seconds a piece takes to rise", Float) = 0.14
        _DieAt ("Owner's clock at which the sinking starts", Float) = 100000
        _DieTime ("Seconds a piece takes to sink", Float) = 0.34
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Blend One OneMinusSrcAlpha
        ZWrite Off
        Offset -1, -1
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #include "UnityCG.cginc"
            fixed4 _DeepIce, _Sky;
            float4 _KeyDir;
            float _Depth, _DepthOver, _SkyStrength, _Glint, _Streaks, _Age, _GrowTime, _DieAt, _DieTime;
            struct appdata { float4 vertex:POSITION; float3 normal:NORMAL; float4 foot:TEXCOORD1; float4 life:TEXCOORD2; float2 extra:TEXCOORD3; };
            struct v2f { float4 vertex:SV_POSITION; float3 normal:TEXCOORD0; float3 wpos:TEXCOORD1; float2 extra:TEXCOORD2; };
            v2f vert(appdata v)
            {
                v2f o;
                float g=saturate((_Age-v.life.x)/_GrowTime);
                float rise=1.0-(1.0-g)*(1.0-g)*(1.0-g);
                float bump=sin(saturate((g-0.45)/0.55)*3.14159);
                float d=saturate((_Age-_DieAt-v.life.z)/_DieTime);
                d=d*d*(3.0-2.0*d);
                float3 rel=v.vertex.xyz-v.foot.xyz;
                rel.y*=(1.0+0.12*bump)*(1.0-0.20*d);
                rel.xz*=(lerp(0.62,1.0,rise)-0.06*bump)*(1.0-0.40*d);
                rel.y-=(1.0-rise)*v.life.y*1.08+d*v.life.y*1.05;
                float4 p=float4(v.foot.xyz+rel,1.0);
                o.vertex=UnityObjectToClipPos(p);
                o.normal=UnityObjectToWorldNormal(v.normal);
                o.wpos=mul(unity_ObjectToWorld,p).xyz;
                o.extra=v.extra;
                return o;
            }
            float hash2(float2 p){ p=frac(p*0.3183099+0.1); p*=17.0; return frac(p.x*p.y*(p.x+p.y)); }
            float vnoise(float2 x)
            {
                float2 i=floor(x), f=frac(x); f=f*f*(3.0-2.0*f);
                return lerp(lerp(hash2(i),hash2(i+float2(1,0)),f.x),lerp(hash2(i+float2(0,1)),hash2(i+float2(1,1)),f.x),f.y);
            }
            fixed4 frag(v2f i):SV_Target
            {
                // A slow wave in the face, so reflections bend and are not ruled lines.
                float2 w=i.wpos.xz*0.45;
                float3 n=normalize(normalize(i.normal)+float3(vnoise(w)-0.5,0.0,vnoise(w+17.3)-0.5)*0.05);
                float3 v=normalize(_WorldSpaceCameraPos-i.wpos);
                float3 r=reflect(-v,n);
                float face=saturate(dot(n,v));
                float fres=pow(1.0-face,5.0);
                // Upright streaks: a few bands round the horizon, hard-edged, seen only as fresnel lets them.
                float az=atan2(r.x,r.z)*1.9;
                float band=frac(az+(vnoise(float2(az,1.3))-0.5)*0.5);
                float streak=smoothstep(0.03,0.05,band)*(1.0-smoothstep(0.13,0.15,band));
                // The key light, as light breaks on rough ice: drawn out along the way to the light and narrow across it, and cut
                // into bits by the noise. A round mirror of it read as a white disc stuck on the floor (c9g).
                float3 key=normalize(_KeyDir.xyz);
                float2 towards=normalize(key.xz+float2(1e-4,0.0)), rel2=r.xz-key.xz;
                float along2=dot(rel2,towards), across=dot(rel2,float2(-towards.y,towards.x));
                float spot2=(across*across)/(0.014*0.014)+(along2*along2)/(0.24*0.24);
                // Uneven bits: at one size the breaks read as road markings (c10).
                float broken=smoothstep(0.40,0.62,vnoise(i.wpos.xz*1.1+vnoise(i.wpos.zx*0.5)*3.0))*(0.55+0.45*vnoise(i.wpos.xz*5.0));
                float glint=(1.0-smoothstep(0.5,1.0,spot2))*broken*step(0.0,r.y);
                float3 add=_Sky.rgb*(fres*_SkyStrength+streak*saturate(fres*3.0)*_Streaks)+float3(1,1,1)*glint*_Glint;
                // Depth over the floe's body, nothing at its edge. The far floes keep it: only the sky thins it at a low angle.
                // (A broad key light was a white ball on a plaza, c9b: only the small core is kept.)
                float deep=_Depth*smoothstep(0.0,_DepthOver,i.extra.x)*(0.8+0.2*i.extra.y);
                deep=max(deep,0.0);
                return fixed4(_DeepIce.rgb*deep*(1.0-fres*_SkyStrength)+add,deep*(1.0-fres*_SkyStrength));
            }
            ENDCG
        }
    }
    Fallback Off
}
