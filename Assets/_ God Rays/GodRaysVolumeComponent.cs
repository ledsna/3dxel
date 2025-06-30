using System;
using UnityEditor.PackageManager.UI;
using UnityEngine;
using UnityEngine.Rendering;

public enum SampleCountEnum
{
    _8 = 8, 
    _16 = 16,
    _32 = 32,
    _64 = 64,
    _86 = 86,
    _128 = 128,
}

[Serializable]
public class GodRaysVolumeComponent : VolumeComponent
{
    [Header("God Rays settings")] 
    public ClampedFloatParameter Intensity = new (0.5f, 0.0f, 1.0f);
    public ClampedFloatParameter Scattering = new (0.5f, 0.0f, 1.0f);
    public MinFloatParameter MaxDistance = new (100.0f, 0.0f);
    public MinFloatParameter JitterVolumetric = new (100.0f, 0.0f);
    public ColorParameter GodRayColor = new (Color.white);

    [Space(10)] [Header("Blur settings")] 
    public ClampedIntParameter GaussSamples = new (4, 0, 8);
    public MinFloatParameter GaussAmount = new(0.5f, 0.0f);
}
