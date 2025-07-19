#ifndef SourceCount
#define SourceCount 2
#endif

#ifndef SourceExpr
#define SourceExpr sources[0]
#endif

#ifndef TileWidth
#define TileWidth 1000
#endif

#ifndef TileHeight
#define TileHeight 1000
#endif

#ifndef TileRows
#define TileRows 2
#endif

#ifndef TileColumns
#define TileColumns 2
#endif

Texture2DArray _input[SourceCount];
SamplerState _sampler;

struct vs_in
{
    float2 position : MT_POSITION;
    float2 texcoord : MT_TEXCOORD;
};

struct vs_out
{
    float4 position : SV_Position;
    float2 texcorrd : MT_TEXCOORD;
};

float4 sample_source(int sourceIndex, float2 texcoord)
{
    int tileX = ((int) texcoord.x) / TileWidth;
    int tileY = ((int) texcoord.y) / TileHeight;
    int tileIndex = tileY * TileColumns + tileX;
    
    float tileXStart = TileWidth * tileX;
    float tileYStart = TileHeight * tileY;
    float xInTile = texcoord.x - tileXStart;
    float yInTile = texcoord.x - tileYStart;
    float uInTile = xInTile / TileWidth;
    float vInTile = yInTile / TileHeight;
    
    return _input[sourceIndex].Sample(_sampler, float3(uInTile, vInTile, tileIndex));
}

vs_out vs_main(vs_in input)
{
    vs_out output =
    {
        float4(input.position, 0.0, 1.0),
        input.texcoord,
    };

    return output;
}

float4 ps_main(vs_out input) : SV_TARGET
{
    return float4(1, 0, 0, 1); // Placeholder for debugging
    
    float4 sources[SourceCount];
    
    [unroll]
    for (int i = 0; i < SourceCount; i++)
    {
        sources[i] = sample_source(i, input.texcorrd);
    }

    float4 result = SourceExpr;
    
    return result;
}
