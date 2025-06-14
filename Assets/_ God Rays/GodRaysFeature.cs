using System;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class GodRaysFeature : ScriptableRendererFeature
{
    [Serializable]
    public class Settings
    {
        [Range(16, 256)] public int sampleCount = 64;
        public float density = 0.8f;
        public float weight = 0.5f;
        public float decay = 1.0f;
        public float exposure = 1.0f;
        public Vector3 lightDirection = new Vector3(0, 1, 0);
    }

    [SerializeField] private Settings settings;
    [SerializeField] private Shader shader;
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

        if (renderingData.cameraData.cameraType == CameraType.Game)
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