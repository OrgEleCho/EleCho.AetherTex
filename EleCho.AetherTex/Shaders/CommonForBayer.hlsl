SamplerState __sampler_point_clamp
{
    Filter = MIN_MAG_MIP_POINT;
    AddressU = CLAMP;
    AddressV = CLAMP;
};

float4 load(Texture2DArray tiles, int2 tileSize, int edgeSize, int tileColumns, int2 xy)
{
    int2 tileClientSize = int2(
        tileSize.x - edgeSize - edgeSize,
        tileSize.y - edgeSize - edgeSize);
    
    int2 tileXY = int2(
        xy.x / tileClientSize.x,
        xy.y / tileClientSize.y);
    
    int tileIndex = tileXY.y * tileColumns + tileXY.x;
    
    int xInTile = xy.x % tileClientSize.x + edgeSize;
    int yInTile = xy.y % tileClientSize.y + edgeSize;
    float uInTile = (xInTile + 0.5) / (tileSize.x);
    float vInTile = (yInTile + 0.5) / (tileSize.y);
    
    return tiles.Sample(__sampler_point_clamp, float3(uInTile, vInTile, tileIndex));
}

float4 load_corners(Texture2DArray tiles, int2 tileSize, int edgeSize, int tileColumns, int2 xy)
{
    float4 leftUp = load(tiles, tileSize, edgeSize, tileColumns, xy + int2(-1, -1));
    float4 rightUp = load(tiles, tileSize, edgeSize, tileColumns, xy + int2(1, -1));
    float4 leftDown = load(tiles, tileSize, edgeSize, tileColumns, xy + int2(-1, 1));
    float4 rightDown = load(tiles, tileSize, edgeSize, tileColumns, xy + int2(1, 1));
    
    return (leftUp + rightUp + leftDown + rightDown) / 4;
}

float4 load_siblings(Texture2DArray tiles, int2 tileSize, int edgeSize, int tileColumns, int2 xy)
{
    float4 up = load(tiles, tileSize, edgeSize, tileColumns, xy + int2(0, -1));
    float4 down = load(tiles, tileSize, edgeSize, tileColumns, xy + int2(0, 1));
    float4 left = load(tiles, tileSize, edgeSize, tileColumns, xy + int2(-1, 0));
    float4 right = load(tiles, tileSize, edgeSize, tileColumns, xy + int2(1, 0));
    
    return (up + down + left + right) / 4;
}

float4 load_left_right(Texture2DArray tiles, int2 tileSize, int edgeSize, int tileColumns, int2 xy)
{
    float4 left = load(tiles, tileSize, edgeSize, tileColumns, xy + int2(-1, 0));
    float4 right = load(tiles, tileSize, edgeSize, tileColumns, xy + int2(1, 0));
    
    return (left + right) / 2;
}

float4 load_up_down(Texture2DArray tiles, int2 tileSize, int edgeSize, int tileColumns, int2 xy)
{
    float4 up = load(tiles, tileSize, edgeSize, tileColumns, xy + int2(0, -1));
    float4 down = load(tiles, tileSize, edgeSize, tileColumns, xy + int2(0, 1));
    
    return (up + down) / 2;
}