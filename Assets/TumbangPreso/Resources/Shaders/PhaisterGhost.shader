// THE SHAPE SHE LEAVES BEHIND WHEN SHE TELEPORTS (`PhaisterGhostBody`, directions A and B of the kit rework).
//
// Her pose is baked ONCE at the release into one world-space mesh (never per frame), and everything after that is this
// program, from one clock `_Age`:
//   _Mode 0 (A, moonlit moths): for a moment the whole silhouette is DRAWN toward where she is going (the moths ride out
//          of that stretch), then it is left behind as a dark violet ghost that drips: each column of it falls at its own
//          speed, pools where it meets the ground, and is eaten from the top down.
//   _Mode 1 (B, sleepy cosmos): it twists and sinks into the pool under her feet, squeezed thin as it goes, and is cut
//          off at the pool's surface, so the court reads it as swallowed.
//
// Drawn twice: depth first, then colour against that depth, so the ghost is one see-through layer and never shows its own
// insides. Dark painted body under a lilac rim, which is what reads on a sunlit court without bloom.
Shader "TumbangPreso/PhaisterGhost"
{
    Properties
    {
        _Age ("The effect's clock (s)", Float) = 0
        _Mode ("0 drawn and dripping (A), 1 swallowed (B)", Float) = 0
        _Base ("Ground where she stood (world), w her height (m)", Vector) = (0,0,0,1.7)
        _Target ("Where she is going (ground, world)", Vector) = (0,0,5,0)
        _Body ("Body", Color) = (0.17,0.07,0.27,0.86)
        _Mid ("Middle band", Color) = (0.36,0.16,0.55,1)
        _Rim ("Rim", Color) = (0.79,0.64,0.94,1)
        _Star ("Star fleck", Color) = (1.0,0.93,0.97,1)
    }
    CGINCLUDE
    #include "UnityCG.cginc"
    float _Age, _Mode;
    float4 _Base, _Target;

    float Hash(float2 p) { return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453); }
    float Noise(float2 p)
    {
        float2 i = floor(p), f = frac(p);
        f = f * f * (3.0 - 2.0 * f);
        return lerp(lerp(Hash(i), Hash(i + float2(1,0)), f.x), lerp(Hash(i + float2(0,1)), Hash(i + float2(1,1)), f.x), f.y);
    }

    struct appdata { float4 vertex:POSITION; float3 normal:NORMAL; };
    struct v2f { float4 pos:SV_POSITION; float3 world:TEXCOORD0; float3 normal:TEXCOORD1; float3 shape:TEXCOORD2; };

    v2f Shape(appdata v)
    {
        v2f o;
        float3 p = v.vertex.xyz;
        float3 base = _Base.xyz;
        float height = max(0.3, _Base.w);
        float h = saturate((p.y - base.y) / height);
        float column = Noise(p.xz * 3.2 + 3.7);
        float3 toward = _Target.xyz - base; toward.y = 0;
        float dist = length(toward);
        toward = dist > 0.01 ? toward / dist : float3(0,0,1);
        if (_Mode < 0.5)
        {
            // Drawn toward the destination, then let go: a fast pull out and a slower settle back.
            float pull = smoothstep(0.0, 0.07, _Age) * (1.0 - smoothstep(0.09, 0.30, _Age));
            float front = saturate(dot(p - base, toward) * 2.5 + 0.5);
            p += toward * pull * min(2.2, dist * 0.4) * (0.25 + 0.75 * front) * (0.55 + 0.45 * column) * (0.4 + 0.6 * h);
            // Drips: every column of the body falls at its own pace, faster higher up, and spreads where it meets the ground.
            float k = saturate((_Age - 0.10) / 0.95);
            p.y -= k * k * (0.35 + 1.6 * column) * (0.25 + 0.75 * h) * height;
            float floorY = base.y + 0.015 + 0.01 * column;
            float under = max(0.0, floorY - p.y);
            p.y = max(p.y, floorY);
            float2 out_ = p.xz - base.xz;
            p.xz += (dot(out_, out_) > 1e-5 ? normalize(out_) : float2(0,0)) * under * 0.45;
        }
        else
        {
            // Swallowed: sinking slowly then fast, twisting the way the pool turns, squeezed toward the middle.
            float s = saturate((_Age - 0.03) / 0.36);
            s = s * s;
            float turn = -s * (2.2 + 1.6 * h);
            float2 r = p.xz - base.xz;
            float cs = cos(turn), sn = sin(turn);
            r = float2(cs * r.x - sn * r.y, sn * r.x + cs * r.y) * lerp(1.0, 0.30, s);
            p.xz = base.xz + r;
            p.y -= s * (height + 0.15);
        }
        o.pos = mul(UNITY_MATRIX_VP, float4(p, 1.0));
        o.world = p;
        o.normal = v.normal;
        o.shape = float3(h, column, 0);
        return o;
    }

    // Keeps a pixel or eats it: A from the top down as it drips, B below the pool's surface.
    void Eat(v2f i)
    {
        if (_Mode < 0.5)
        {
            float e = saturate((_Age - 0.22) / 0.85);
            float n = Noise(i.world.xz * 3.0 + i.world.y * 2.4);
            clip((1.0 - i.shape.x) * 0.75 + n * 0.45 + 0.15 - e * 1.45);
        }
        else
        {
            clip(i.world.y - _Base.y - 0.005);
            clip(0.97 - saturate((_Age - 0.03) / 0.36));
        }
    }
    ENDCG
    SubShader
    {
        Tags { "Queue"="Transparent+5" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Pass
        {
            ZWrite On
            ColorMask 0
            Cull Back
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.5
            v2f vert(appdata v) { return Shape(v); }
            fixed4 frag(v2f i) : SV_Target { Eat(i); return 0; }
            ENDCG
        }
        Pass
        {
            ZWrite Off
            ZTest LEqual
            Cull Back
            Blend SrcAlpha OneMinusSrcAlpha
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.5
            fixed4 _Body, _Mid, _Rim, _Star;
            v2f vert(appdata v) { return Shape(v); }
            fixed4 frag(v2f i) : SV_Target
            {
                Eat(i);
                float3 view = normalize(_WorldSpaceCameraPos - i.world);
                float3 n = normalize(i.normal);
                float edge = 1.0 - saturate(abs(dot(n, view)));
                // Three bands: the dark body, a violet middle toward the edge, a lilac rim.
                fixed3 c = _Body.rgb;
                c = lerp(c, _Mid.rgb, step(0.45, edge));
                c = lerp(c, _Rim.rgb * 1.15, smoothstep(0.70, 0.78, edge));
                // Its top side takes a little more light than its underside.
                c *= lerp(0.85, 1.05, step(0.2, n.y));
                float a = _Body.a;
                if (_Mode < 0.5)
                {
                    // Where the drip has pooled it is the darkest thing on the ground.
                    float low = 1.0 - saturate((i.world.y - _Base.y) / 0.05);
                    c = lerp(c, _Body.rgb * 0.8, low);
                }
                else
                {
                    // Flecks of star caught in her as she goes under.
                    float2 cell = floor(i.world.xz * 6.0 + i.world.y * 4.0);
                    float star = step(0.93, Hash(cell)) * step(0.5, sin(_Age * 23.0 + Hash(cell + 1.0) * 6.28) + 0.6);
                    c = lerp(c, _Star.rgb, star * step(edge, 0.7));
                    // The part nearest the surface goes dark, as if under water.
                    c *= lerp(0.55, 1.0, saturate((i.world.y - _Base.y) / 0.35));
                }
                return fixed4(c, a);
            }
            ENDCG
        }
    }
}
