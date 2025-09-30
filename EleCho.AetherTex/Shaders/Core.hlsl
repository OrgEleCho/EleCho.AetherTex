void discard_if_out_of_range(int2 tileSize, int edgeSize, int rowCount, int columnCount, float2 texcoord)
{
    int2 tileClientSize = int2(
        tileSize.x - edgeSize - edgeSize,
        tileSize.y - edgeSize - edgeSize);
    
    float2 wholeClientSize = int2(
        tileClientSize.x * columnCount,
        tileClientSize.y * rowCount);
    
    float2 uv = float2(
        texcoord.x / wholeClientSize.x,
        texcoord.y / wholeClientSize.y);

    clip(float4(uv, 1 - uv.x, 1 - uv.y));
}

int calculate_tile_index(int2 tileSize, int edgeSize, int columnCount, float2 texcoord)
{
    int2 tileClientSize = int2(
        tileSize.x - edgeSize - edgeSize,
        tileSize.y - edgeSize - edgeSize);
    
    int2 tileXY = int2(
        (int) (texcoord.x / tileClientSize.x),
        (int) (texcoord.y / tileClientSize.y));
    
    return tileXY.y * columnCount + tileXY.x;
}

float2 calculate_coordinates_in_tile(int2 tileSize, int edgeSize, float2 texcoord)
{
    int2 tileClientSize = int2(
        tileSize.x - edgeSize - edgeSize,
        tileSize.y - edgeSize - edgeSize);
    
    return float2(
        texcoord.x % tileClientSize.x + edgeSize,
        texcoord.y % tileClientSize.y + edgeSize);
}

float2 calculate_uv_in_tile(float2 tileTextureSize, float2 coordsInTileTexture)
{
    float uInTileTexture = (coordsInTileTexture.x + 0.5) / (tileTextureSize.x);
    float vInTileTexture = (coordsInTileTexture.y + 0.5) / (tileTextureSize.y);
    
    return float2(uInTileTexture, vInTileTexture);
}