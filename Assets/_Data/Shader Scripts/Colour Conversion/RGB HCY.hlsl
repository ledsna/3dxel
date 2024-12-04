float Epsilon = 1e-10;


// The weights of RGB contributions to luminance.
// Should sum to unity.
float3 HCYwts = float3(0.299, 0.587, 0.114);

float3 HUEtoRGB(in float H)
{
    float R = abs(H * 6 - 3) - 1;
    float G = 2 - abs(H * 6 - 2);
    float B = 2 - abs(H * 6 - 4);
    return saturate(float3(R,G,B));
}

float3 HCYtoRGB(in float3 HCY)
{
    float3 RGB = HUEtoRGB(HCY.x);
    float Z = dot(RGB, HCYwts);
    if (HCY.z < Z)
    {
        HCY.y *= HCY.z / Z;
    }
    else if (Z < 1)
    {
        HCY.y *= (1 - HCY.z) / (1 - Z);
    }
    return (RGB - Z) * HCY.y + HCY.z;
}

float3 RGBtoHCV(in float3 RGB)
{
    // Based on work by Sam Hocevar and Emil Persson
    float4 P = (RGB.g < RGB.b) ? float4(RGB.bg, -1.0, 2.0/3.0) : float4(RGB.gb, 0.0, -1.0/3.0);
    float4 Q = (RGB.r < P.x) ? float4(P.xyw, RGB.r) : float4(RGB.r, P.yzx);
    float C = Q.x - min(Q.w, Q.y);
    float H = abs((Q.w - Q.y) / (6 * C + Epsilon) + Q.z);
    return float3(H, C, Q.x);
}

float3 RGBtoHCY(in float3 RGB)
{
    // Corrected by David Schaeffer
    float3 HCV = RGBtoHCV(RGB);
    float Y = dot(RGB, HCYwts);
    float Z = dot(HUEtoRGB(HCV.x), HCYwts);
    if (Y < Z)
    {
        HCV.y *= Z / (Epsilon + Y);
    }
    else
    {
        HCV.y *= (1 - Z) / (Epsilon + 1 - Y);
    }
    return float3(HCV.x, HCV.y, Y);
}
  
#ifndef RGB_TO_HCY
#define RGB_TO_HCY

void RGBtoHCY_float(float3 RGB, out float3 HCY) {
    HCY = RGBtoHCY(RGB);
}

void HCYtoRGB_float(float3 HCY, out float3 RGB) {
    RGB = HCYtoRGB(HCY);
}

#endif