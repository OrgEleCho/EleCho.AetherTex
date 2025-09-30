#include <Common.hlsl>

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
    
    float uGlobal = texcoord.x / TileWidth / TileColumns;
    float vGlobal = texcoord.y / TileHeight / TileRows;
    clip(float4(uGlobal, vGlobal, 1 - uGlobal, 1 - vGlobal));
    
    int tileX = ((int) texcoord.x) / TileWidth;
    int tileY = ((int) texcoord.y) / TileHeight;
    int tileIndex = tileY * TileColumns + tileX;
    
    float tileXStart = TileWidth * tileX;
    float tileYStart = TileHeight * tileY;
    float xInTile = texcoord.x - tileXStart;
    float yInTile = texcoord.y - tileYStart;
    float uInTile = xInTile / TileWidth;
    float vInTile = yInTile / TileHeight;
    
    return _input[sourceIndex].Sample(_sampler, float3(uInTile, vInTile, tileIndex));
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