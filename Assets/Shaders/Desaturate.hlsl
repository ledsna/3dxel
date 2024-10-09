// Desaturate.hlsl

// Desaturate function
void Desaturate_float(float3 colour, float fraction, out float3 result) {
    float luminance = dot(colour, float3(0.3, 0.59, 0.11));
    result = lerp(colour, float3(luminance, luminance, luminance), saturate(fraction));
}