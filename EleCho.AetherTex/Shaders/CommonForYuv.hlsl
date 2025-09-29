SamplerState __sampler_point_clamp
{
    Filter = MIN_MAG_MIP_POINT;
    AddressU = CLAMP;
    AddressV = CLAMP;
};

float4 load_in_tile(Texture2DArray tiles, int2 tileTextureSize, int tileIndex, int2 xyInTileTexture)
{   
    float uInTileTexture = (xyInTileTexture.x + 0.5) / (tileTextureSize.x);
    float vInTileTexture = (xyInTileTexture.y + 0.5) / (tileTextureSize.y);
    
    return tiles.Sample(__sampler_point_clamp, float3(uInTileTexture, vInTileTexture, tileIndex));
}

float3 yuv_to_rgb(float3 yuv)
{
    float y = yuv.x;
    float u = yuv.y;
    float v = yuv.z;

    float r = y                  + (1.370705 * v);
    float g = y - (0.337633 * u) - (0.698001 * v);
    float b = y + (1.732446 * u);

    return float3(r, g, b);
}