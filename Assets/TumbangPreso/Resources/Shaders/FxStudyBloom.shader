// The effects bench's bloom (`Editor/MapKit/PaeteAbilityFilm.FxBench.cs`). BENCH ONLY: no camera in the game uses it.
//
// ⚠️ WHY IT EXISTS. The owner, of the study's glow: "you're putting the wisp glow on the texture and not as an
// emmissive/actual glwo". He was right. With nothing blooming, every piece drew its own halo (its mask's blurred mips,
// and soft sprites behind it), on an 8-bit target where stacked light just clips to white: so a glow had its piece's
// shape, sat flat on it, could not spill over the dark thing in front of it, and a bright core was a flat white disc.
// In the references the glow is the camera's: surfaces are brighter than white and the picture blooms. This is that,
// for the bench: the scene is drawn into a floating-point picture where a core may be several times over white, the
// part above a soft threshold is blurred down a pyramid of halvings and added back, and the sum is rolled off to
// white by a tone curve, so an over-bright core goes white with a coloured fringe round it instead of clipping.
//
// Passes: 0 keeps what is over the threshold (soft knee); 1 halves (thirteen taps, so it does not shimmer); 2 doubles
// and adds (a nine-tap tent); 3 adds the bloom to the scene and applies the tone curve.
// ⚠️ Whether the GAME gets this is the owner's call, not this file's: the cost is in the bench file's header.
Shader "Hidden/TumbangPreso/FxStudyBloom"
{
    Properties { _MainTex ("", 2D) = "black" {} _BloomTex ("", 2D) = "black" {} }
    SubShader
    {
        Cull Off ZWrite Off ZTest Always
        CGINCLUDE
        #include "UnityCG.cginc"
        sampler2D _MainTex, _BloomTex;
        float4 _MainTex_TexelSize;
        float _Threshold, _Knee, _Intensity, _White;
        struct v2f { float4 vertex:SV_POSITION; float2 uv:TEXCOORD0; };
        v2f vert(appdata_img v) { v2f o; o.vertex=UnityObjectToClipPos(v.vertex); o.uv=v.texcoord; return o; }
        float3 tap(float2 uv) { return tex2D(_MainTex,uv).rgb; }
        ENDCG
        Pass // 0: what is brighter than the threshold, let in gently over a knee
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            float4 frag(v2f i):SV_Target
            {
                float3 c=tap(i.uv);
                float bright=max(c.r,max(c.g,c.b));
                float soft=clamp(bright-_Threshold+_Knee,0.0,2.0*_Knee);
                soft=soft*soft/(4.0*_Knee+1e-4);
                return float4(c*max(soft,bright-_Threshold)/max(bright,1e-4),1);
            }
            ENDCG
        }
        Pass // 1: halve
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            float4 frag(v2f i):SV_Target
            {
                float2 t=_MainTex_TexelSize.xy;
                float3 c=tap(i.uv)*0.125;
                c+=(tap(i.uv+t*float2(-1,-1))+tap(i.uv+t*float2(1,-1))+tap(i.uv+t*float2(-1,1))+tap(i.uv+t*float2(1,1)))*0.125;
                c+=(tap(i.uv+t*float2(-2,0))+tap(i.uv+t*float2(2,0))+tap(i.uv+t*float2(0,-2))+tap(i.uv+t*float2(0,2)))*0.0625;
                c+=(tap(i.uv+t*float2(-2,-2))+tap(i.uv+t*float2(2,-2))+tap(i.uv+t*float2(-2,2))+tap(i.uv+t*float2(2,2)))*0.03125;
                return float4(c,1);
            }
            ENDCG
        }
        Pass // 2: double, and add to the level above
        {
            Blend One One
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            float4 frag(v2f i):SV_Target
            {
                float2 t=_MainTex_TexelSize.xy;
                float3 c=tap(i.uv)*4.0;
                c+=(tap(i.uv+t*float2(-1,0))+tap(i.uv+t*float2(1,0))+tap(i.uv+t*float2(0,-1))+tap(i.uv+t*float2(0,1)))*2.0;
                c+=tap(i.uv+t*float2(-1,-1))+tap(i.uv+t*float2(1,-1))+tap(i.uv+t*float2(-1,1))+tap(i.uv+t*float2(1,1));
                return float4(c/16.0,1);
            }
            ENDCG
        }
        Pass // 3: the scene and its bloom, rolled off to white
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            float4 frag(v2f i):SV_Target
            {
                float3 c=tap(i.uv)+tex2D(_BloomTex,i.uv).rgb*_Intensity;
                // Untouched up to three quarters, so nothing ordinary changes colour; above that it bends over toward
                // white, each channel by itself, which is what leaves a coloured fringe round a white core.
                float3 over=max(c-0.75,0.0);
                c=min(c,0.75)+0.25*(1.0-exp(-over/(0.25*_White)));
                return float4(c,1);
            }
            ENDCG
        }
    }
    Fallback Off
}
