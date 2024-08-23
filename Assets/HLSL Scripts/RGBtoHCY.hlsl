// The weights of RGB contributions to luminance.
// Should sum to unity.
float3 HCYwts = float3(0.299, 0.587, 0.114);

float3 HUEtoRGB(float H)
{
  float R = abs(H * 6 - 3) - 1;
  float G = 2 - abs(H * 6 - 2);
  float B = 2 - abs(H * 6 - 4);
  return saturate(float3(R,G,B));
}

float RGBCVtoHUE(float3 RGB, float C, float V)
{
    float3 Delta = (V - RGB) / C;
    Delta.rgb -= Delta.brg;
    Delta.rgb += float3(2,4,6);
    // NOTE 1
    Delta.brg = step(V, RGB) * Delta.brg;
    float H;
    H = max(Delta.r, max(Delta.g, Delta.b));
    return frac(H / 6);
}

float3 RGBtoHCY(float3 RGB)
{
  float3 HCY = 0;
  float U, V;
  U = -min(RGB.r, min(RGB.g, RGB.b));
  V = max(RGB.r, max(RGB.g, RGB.b));
  HCY.y = V + U;
  HCY.z = dot(RGB, HCYwts);
  if (HCY.y != 0)
  {
    HCY.x = RGBCVtoHUE(RGB, HCY.y, V);
    float Z = dot(HUEtoRGB(HCY.x), HCYwts);
    if (HCY.z > Z)
    {
      HCY.z = 1 - HCY.z;
      Z = 1 - Z;
    }
    HCY.y *= Z / HCY.z;
  }
  return HCY;
}

float3 HCYtoRGB(float3 HCY)
{
  float RGB = HUEtoRGB(HCY.x);
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

#ifndef RGB_TO_HCY
#define RGB_TO_HCY

void RGBtoHCY_float(float3 RGB, out float3 HCY) {
    HCY = RGBtoHCY(RGB);
}

void HCYtoRGB_float(float3 HCY, out float3 RGB) {
    RGB = HCYtoRGB(HCY);
}

#endif