#include <Core.hlsl>
#include <Common.hlsl>
#include <CommonForYCoCg.hlsl>

#ifndef SourceCount
#define SourceCount 2
#endif

#ifndef SourceExpr
#define SourceExpr sources[0]
#endif

#ifndef TileWidth
#define TileWidth 100
#endif

#ifndef TileHeight
#define TileHeight 100
#endif

#ifndef TileRows
#define TileRows 2
#endif

#ifndef TileColumns
#define TileColumns 2
#endif

#ifndef EdgeSize
#define EdgeSize 0
#endif

Texture2DArray _input[SourceCount] : register(t0);
Texture2DArray _inputAlt[SourceCount] : register(t1);
SamplerState _sampler;

cbuffer ConstantBuffer : register(b0)
{
    float3x3 TransformMatrix;
};

struct vs_in
{
    float2 position : MT_POSITION;
    float2 texcoord : MT_TEXCOORD;
};

struct vs_out
{
    float4 position : SV_Position;
    float2 texcoord : MT_TEXCOORD;
};

float4 sample_source(int sourceIndex, float2 texcoord)
{
    float3 transformResult = mul(float1x3(texcoord, 1), TransformMatrix);
    texcoord = float2(
        transformResult.x / transformResult.z,
        transformResult.y / transformResult.z);
    
    int2 tileSize = int2(TileWidth, TileHeight);
    discard_if_out_of_range(tileSize, EdgeSize, TileRows, TileColumns, texcoord);
    int tileIndex = calculate_tile_index(tileSize, EdgeSize, TileColumns, texcoord);
    float2 xyInTile = calculate_coordinates_in_tile(tileSize, EdgeSize, texcoord);
    float2 uvInTile = calculate_uv_in_tile(tileSize, xyInTile);
    
    float4 colorY = _input[sourceIndex].Sample(_sampler, float3(uvInTile, tileIndex));
    float4 colorCoCg = _inputAlt[sourceIndex].Sample(_sampler, float3(uvInTile, tileIndex));
    
    return float4(ycocg_to_rgb(float3(colorY.x, colorCoCg.x, colorCoCg.y)), 1);
}

vs_out vs_main(vs_in input)
{
    vs_out output =
    {
        float4(input.position, 0.5, 1.0),
        input.texcoord,
    };

    return output;
}

float4 ps_main(vs_out input) : SV_TARGET
{
    float4 sources[SourceCount];
    
    [unroll]
    for (int i = 0; i < SourceCount; i++)
    {
        sources[i] = sample_source(i, input.texcoord);
    }

    float4 result = SourceExpr;
    
    return result;
}
