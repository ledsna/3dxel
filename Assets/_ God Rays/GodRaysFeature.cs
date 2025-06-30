using System;
using NaughtyAttributes;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.Serialization;


public class GodRaysFeature : ScriptableRendererFeature
{
    [Serializable]
    public class GodRaysSettings
    {
        public SampleCountEnum SampleCount = SampleCountEnum._64;
        [Min(0)] public float Intensity = 1;
        [Min(0)] public float Scattering = 0.5f;
        [Min(0)] public float MaxDistance = 100f;
        [Min(0)] public float JitterVolumetric = 100;
        public Color GodRayColor = Color.white;
    }

    [Serializable]
    public class BlurSettings
    {
        // MAX VALUE = 7. You can't set values higher than 8
        [Range(0, 8)] public int GaussSamples = 4;
        [Min(0)] public float GaussAmount = 0.5f;
    }
    
    // God Rays
    // --------
    [SerializeField] private GodRaysSettings defaultGodRaysSettings;
    [SerializeField] private Shader godRaysShader;
    private Material godRaysMaterial;
    private GodRaysPass godRaysPass;

    [Space(10)]

    // Blur Settings
    // -------------
    [SerializeField]
    private BlurSettings defaultBlurSettings;

    [SerializeField] private Shader blurShader;
    private Material blurMaterial;

    [Space(10)]

    // General 
    // -------
    [Header("General")]
    [SerializeField]
    private bool renderInScene = false;

    public override void Create()
    {
        if (godRaysShader == null)
            return;
        godRaysMaterial = new Material(godRaysShader);
        blurMaterial = new Material(blurShader);
        godRaysPass = new GodRaysPass(godRaysMaterial, defaultGodRaysSettings, blurMaterial, defaultBlurSettings);
        godRaysPass.renderPassEvent = RenderPassEvent.AfterRenderingSkybox;
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (godRaysPass == null)
            return;
        if (!renderInScene && renderingData.cameraData.cameraType != CameraType.Game)
            return;

        // Check if Main Light exists and is active
        var mainLightIndex = renderingData.lightData.mainLightIndex;
        if (mainLightIndex == -1) // -1 means no main light
            return;

        var mainLight = renderingData.lightData.visibleLights[mainLightIndex];
        if (mainLight.light == null || !mainLight.light.enabled)
            return;

        renderer.EnqueuePass(godRaysPass);
    }

    protected override void Dispose(bool disposing)
    {
        if (Application.isPlaying)
        {
            Destroy(godRaysMaterial);
        }
        else
        {
            DestroyImmediate(godRaysMaterial);
        }
    }
}