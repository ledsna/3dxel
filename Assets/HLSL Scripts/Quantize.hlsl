#ifndef QUANTIZER_INCLUDED
#define QUANTIZER_INCLUDED

void Quantizer_float(float steps, float shade, out float result)
{
    steps = max(steps, 2);
    // result = min(round(shade * (steps - 1)), steps - 2) / (steps - 1);
    result = floor(shade * (steps - 1) + 0.5) / (steps - 1);
}
#endif
