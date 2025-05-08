void Dither_float(float In, float2 ScreenPosition, out float Out)
{
    float2 pixelPos = ScreenPosition * _ScreenParams.xy;
    uint    x       = (pixelPos.x % 4 + 4) % 4;
    uint    y       = (pixelPos.y % 4 + 4) % 4;
    uint    index     = x * 4 + y;

    float DITHER_THRESHOLDS[16] =
    {
        1.0 / 17.0,  9.0 / 17.0,  3.0 / 17.0, 11.0 / 17.0,
        13.0 / 17.0,  5.0 / 17.0, 15.0 / 17.0,  7.0 / 17.0,
        4.0 / 17.0, 12.0 / 17.0,  2.0 / 17.0, 10.0 / 17.0,
        16.0 / 17.0,  8.0 / 17.0, 14.0 / 17.0,  6.0 / 17.0
    };
    Out = In - DITHER_THRESHOLDS[index];
}
