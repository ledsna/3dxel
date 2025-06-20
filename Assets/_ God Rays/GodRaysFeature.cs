using System;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.Serialization;

public class GodRaysFeature : ScriptableRendererFeature
{
    [Serializable]
    public class GodRaysSettings
    {
        [Range(1, 128)] public int SampleCount = 32;
        [Min(0)] public float Intensity = 1;
        [Min(0)] public float Scattering = 0.5f;
        public float MaxDistance = 100f;
        public float JitterVolumetric = 100;

        public Color godRayColor = Color.white;
        
        public enum DownSample
        {
            Off = 1,
            Half = 2,
            Third = 3,
            Quarter = 4
        }

        public DownSample DownSampling = DownSample.Off;
    }

    [Serializable]
    public class BlurSettings
    {
        // MAX VALUE = 7. You can't set values higher than 8
        [Range(0, 8)]
        public int gaussSamples = 4;
        public float gaussAmount = 0.5f;
    }

    // God Rays
    // --------
    [SerializeField] private GodRaysSettings godRaysGodRaysSettings;
    [SerializeField] private Shader godRaysShader;
    private Material godRaysMaterial;
    private GodRaysPass godRaysPass;
    
    [Space(10)]
    
    // Blur Settings
    // -------------
    [SerializeField] private BlurSettings blurSettings;
    [SerializeField] private Shader blurShader;
    private Material blurMaterial;

    [Space(10)]
    
    
    // General 
    // -------
    [Header("General")]
    [SerializeField] private bool renderInScene = false;
    
    public override void Create()
    {
        if (godRaysShader == null)
            return;
        godRaysMaterial = new Material(godRaysShader);
        blurMaterial = new Material(blurShader);
        godRaysPass = new GodRaysPass(godRaysMaterial, godRaysGodRaysSettings, blurMaterial, blurSettings);
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