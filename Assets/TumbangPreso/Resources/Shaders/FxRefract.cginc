// THE PROJECT'S ONE WAY TO BEND THE LIGHT BEHIND AN EFFECT (first used by Amihan's AIRBURST dome, `FxStudyAirDome.shader`;
// meant for any heat haze or shockwave after it, Dante's fire wave included).
//
// ⚠️ ONE NAMED GRAB FOR EVERY USER. A shader that bends light declares exactly
//     GrabPass { "_FxRefractGrab" }
// in its bending SubShader and includes this file. Because the name is shared, Unity grabs the screen ONCE per camera per
// frame however many of these effects are drawn (a named GrabPass is taken by the first object that needs it), so a second
// effect costs no second full-screen copy.
//
// ⚠️ AND A SECOND SUBSHADER WITHOUT IT. The grab is a full-screen copy, which is a real cost on a phone. Every user puts the
// bending version in a `LOD 300` SubShader and the same effect without bending in a `LOD 100` one, and its C# drops the
// shader's `maximumLOD` to 100 on a lean graphics tier (`Settings.GraphicsProfiles` Low) and on phones (see
// `FxStudyAirburst.Bend`). Built-in render pipeline only: there is no camera opaque texture here.
//
// Use, in a vertex program:   o.grab = ComputeGrabScreenPos(o.pos);
//                             o.screenDir = FxRefractScreenDir(worldNormal);
//     in a fragment program:  float3 bent = FxRefract(i.grab, i.screenDir, amount);   // amount: share of the screen
//                             float3 straight = FxRefractStraight(i.grab);
#ifndef FX_REFRACT_INCLUDED
#define FX_REFRACT_INCLUDED

sampler2D _FxRefractGrab;

/// The way a surface with this world normal pushes the light behind it, as the screen sees it.
float2 FxRefractScreenDir(float3 worldNormal)
{
    float3 vn = mul((float3x3)UNITY_MATRIX_V, worldNormal);
    return vn.xy;
}

/// The scene behind, read back pushed along `screenDir` by `amount` (a share of the screen; 0.01 to 0.04 reads).
float3 FxRefract(float4 grab, float2 screenDir, float amount)
{
    float2 uv = grab.xy / grab.w;
    float2 dir = screenDir / max(1e-4, length(screenDir));
    return tex2D(_FxRefractGrab, uv + dir * amount).rgb;
}

/// The scene behind, not pushed.
float3 FxRefractStraight(float4 grab)
{
    return tex2D(_FxRefractGrab, grab.xy / grab.w).rgb;
}

#endif
