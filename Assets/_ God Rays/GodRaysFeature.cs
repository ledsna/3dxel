using System;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class GodRaysFeature : ScriptableRendererFeature
{
    [Serializable]
    public class GodRaysSettings
    {
        [Range(1, 128)] public int sampleCount = 1;
        public float A;
        [Range(0, 1)] public float B;
        public float C;
        public float D;
        public float MaxDistance;
        public float JitterVolumetric;

        public Color godRayColor = Color.white;
        public bool DrawGodRaysOnly = false; // TODO: Remove from that place. It's not settings param!

        public enum DownSample
        {
            off = 1,
            half = 2,
            third = 3,
            quarter = 4
        }

        public DownSample DownSampling = DownSample.off;
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
    
    // Blur Settings
    // -------------
    [SerializeField] private BlurSettings blurSettings;
    [SerializeField] private Shader blurShader;
    private Material blurMaterial;

    // General 
    // -------
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