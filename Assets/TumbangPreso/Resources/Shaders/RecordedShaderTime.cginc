#ifndef TUMP_RECORDED_SHADER_TIME_INCLUDED
#define TUMP_RECORDED_SHADER_TIME_INCLUDED
#include "UnityCG.cginc"
float4 _TumpRecordedClock;
float TumpShaderTime()
{
    return _TumpRecordedClock.y > 0.5 ? _TumpRecordedClock.x : _Time.y;
}
#endif
