#include <Core.hlsl>
#include <Common.hlsl>
#include <CommonForYuv.hlsl>

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
    
    const int2 tileClientSize = int2(
        TileWidth - EdgeSize - EdgeSize,
        TileWidth - EdgeSize - EdgeSize);
    
    const int2 tileTextureSize = int2(
        TileWidth,
        TileHeight * 3);
    
    discard_if_out_of_range(tileSize, EdgeSize, TileRows, TileColumns, texcoord);

    const int2 xy = int2(
        (int) texcoord.x,
        (int) texcoord.y);
    
    Texture2DArray tiles = _input[sourceIndex];
    int2 tileXY = int2(
        xy.x / tileClientSize.x,
        xy.y / tileClientSize.y);
    
    int2 xyInTile = int2(
        xy.x % tileClientSize.x + EdgeSize,
        xy.y % tileClientSize.y + EdgeSize);
    
    int tileIndex = tileXY.y * TileColumns + tileXY.x;
    
    int yuvXInTileTexture = xyInTile.x;
    int2 yXyInTileTexture = xyInTile;
    
    int uYInTileTexture = TileHeight + xyInTile.y;
    int vYInTileTexture = TileHeight + TileHeight + xyInTile.y;
    
    float y = load_in_tile(tiles, tileTextureSize, 0, yXyInTileTexture).r;
    float u = load_in_tile(tiles, tileTextureSize, 0, int2(yuvXInTileTexture, uYInTileTexture)).r - 0.5;
    float v = load_in_tile(tiles, tileTextureSize, 0, int2(yuvXInTileTexture, vYInTileTexture)).r - 0.5;
    
    float3 rgb = yuv_to_rgb(float3(y, u, v));
    
    return float4(rgb, 1);
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