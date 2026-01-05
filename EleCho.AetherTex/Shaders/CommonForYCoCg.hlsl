SamplerState __sampler_point_clamp
{
    Filter = MIN_MAG_MIP_POINT;
    AddressU = CLAMP;
    AddressV = CLAMP;
};

// YCoCg-R (reversible) conversion
// RGB to YCoCg-R:
//   Co = R - B
//   tmp = B + Co/2
//   Cg = G - tmp
//   Y = tmp + Cg/2
//
// YCoCg-R to RGB:
//   tmp = Y - Cg/2
//   G = Cg + tmp
//   B = tmp - Co/2
//   R = B + Co

float3 ycocg_to_rgb(float3 ycocg)
{
    float y = ycocg.x;
    float co = ycocg.y - 0.5;  // Map from [0,1] to [-0.5,0.5]
    float cg = ycocg.z - 0.5;  // Map from [0,1] to [-0.5,0.5]
    
    // Scale back to [-1,1] range for YCoCg-R
    co = co * 2.0;
    cg = cg * 2.0;

    float tmp = y - cg * 0.5;
    float g = cg + tmp;
    float b = tmp - co * 0.5;
    float r = b + co;

    return float3(r, g, b);
}

float3 rgb_to_ycocg(float3 rgb)
{
    float r = rgb.x;
    float g = rgb.y;
    float b = rgb.z;

    float co = r - b;
    float tmp = b + co * 0.5;
    float cg = g - tmp;
    float y = tmp + cg * 0.5;

    // Map Co and Cg from [-1,1] to [0,1]
    co = co * 0.5 + 0.5;
    cg = cg * 0.5 + 0.5;

    return float3(y, co, cg);
}
