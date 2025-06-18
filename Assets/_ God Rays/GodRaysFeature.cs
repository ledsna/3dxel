using System;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class GodRaysFeature : ScriptableRendererFeature
{
    [Serializable]
    public class Settings
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

    [SerializeField] private Settings settings;
    [SerializeField] private Shader shader;
    [SerializeField] private bool renderInScene = false;
    private Material material;
    private GodRaysPass godRaysPass;

    public override void Create()
    {
        if (shader == null)
            return;
        material = new Material(shader);
        godRaysPass = new GodRaysPass(material, settings);
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
            Destroy(material);
        }
        else
        {
            DestroyImmediate(material);
        }
    }
}