#include <Core.hlsl>
#include <Common.hlsl>

#ifndef SourceExpr
#define SourceExpr sources[0]
#endif

Texture2D _input;
SamplerState _sampler
{
    Filter = MIN_MAG_MIP_LINEAR;
    AddressU = CLAMP;
    AddressV = CLAMP;
};

struct vertex
{
    float2 texcoord : TEXCOORD0;
};

struct ps_in
{
    float4 position : SV_POSITION;
    float2 texcoord : TEXCOORD0;
};

ps_in vs_main(vertex input)
{
    ps_in output =
    {
        float4(input.texcoord, 0.5, 1.0),
        input.texcoord
    };
    
    return output;
}

float4 ps_main(vertex input) : SV_TARGET
{
    float4 sources[1];
    
    sources[0] = _input.Sample(_sampler, input.texcoord);

    float4 result = SourceExpr;
    
    return result;
}