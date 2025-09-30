#include <Core.hlsl>
#include <Common.hlsl>
#include <CommonForBayer.hlsl>

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

Texture2DArray _input[SourceCount];
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
    
    const int2 tileSize = int2(
        TileWidth,
        TileHeight);
    
    discard_if_out_of_range(tileSize, EdgeSize, TileRows, TileColumns, texcoord);
    const int2 xy = int2(
        (int) texcoord.x,
        (int) texcoord.y);
    
    Texture2DArray tiles = _input[sourceIndex];

    int positionInPattern = (xy.y % 2) * 2 + (xy.x % 2);
    if (positionInPattern == 0)
    {
        return float4(
            load_corners(tiles, tileSize, EdgeSize, TileColumns, xy).r,
            load_siblings(tiles, tileSize, EdgeSize, TileColumns, xy).r,
            load(tiles, tileSize, EdgeSize, TileColumns, xy).r,
            1.0);
    }
    else if (positionInPattern == 1)
    {
        return float4(
            load_up_down(tiles, tileSize, EdgeSize, TileColumns, xy).r,
            load(tiles, tileSize, EdgeSize, TileColumns, xy).r,
            load_left_right(tiles, tileSize, EdgeSize, TileColumns, xy).r,
            1.0);
    }
    else if (positionInPattern == 2)
    {
        return float4(
            load_left_right(tiles, tileSize, EdgeSize, TileColumns, xy).r,
            load(tiles, tileSize, EdgeSize, TileColumns, xy).r,
            load_up_down(tiles, tileSize, EdgeSize, TileColumns, xy).r,
            1.0);
    }
    else // positionInPattern == 3
    {
        return float4(
            load(tiles, tileSize, EdgeSize, TileColumns, xy).r,
            load_siblings(tiles, tileSize, EdgeSize, TileColumns, xy).r,
            load_corners(tiles, tileSize, EdgeSize, TileColumns, xy).r,
            1.0);
    }
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