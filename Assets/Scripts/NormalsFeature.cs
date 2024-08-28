using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class NormalTextureRenderFeature : ScriptableRendererFeature
{
    class NormalsRenderPass : ScriptableRenderPass
    {
        private RenderTargetIdentifier normalsTextureIdentifier;
        private RenderTargetHandle normalsTextureHandle;
        private Material normalsMaterial;
        private string profilerTag = "Render Normals Texture";
        private static readonly int NormalsTextureID = Shader.PropertyToID("_NormalsTexture");

        public NormalsRenderPass(Material material)
        {
            normalsMaterial = material;
            normalsTextureHandle.Init("_NormalsTexture");
        }

        public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
        {
            // Configure the texture descriptor and clear settings
            cameraTextureDescriptor.colorFormat = RenderTextureFormat.ARGB32;
            cameraTextureDescriptor.depthBufferBits = 0;
            cmd.GetTemporaryRT(normalsTextureHandle.id, cameraTextureDescriptor);
            normalsTextureIdentifier = normalsTextureHandle.Identifier();

            // Set the render target for the normals
            ConfigureTarget(normalsTextureIdentifier);
            ConfigureClear(ClearFlag.Color, Color.black);
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            CommandBuffer cmd = CommandBufferPool.Get(profilerTag);
            var drawingSettings = CreateDrawingSettings(new ShaderTagId("UniversalForward"), ref renderingData, SortingCriteria.CommonOpaque);
            drawingSettings.overrideMaterial = normalsMaterial;

            // Draw the scene with the normals material
            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();

            var filteringSettings = new FilteringSettings(RenderQueueRange.all);
            context.DrawRenderers(renderingData.cullResults, ref drawingSettings, ref filteringSettings);

            // Set the global texture so that it is accessible to other shaders
            cmd.SetGlobalTexture(NormalsTextureID, normalsTextureIdentifier);

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        public override void FrameCleanup(CommandBuffer cmd)
        {
            // Cleanup temporary render target
            if (normalsTextureHandle != RenderTargetHandle.CameraTarget)
            {
                cmd.ReleaseTemporaryRT(normalsTextureHandle.id);
            }
        }
    }

    [System.Serializable]
    public class NormalTextureSettings
    {
        public Material normalsMaterial;
    }

    public NormalTextureSettings settings = new NormalTextureSettings();
    private NormalsRenderPass renderPass;

    public override void Create()
    {
        if (settings.normalsMaterial == null)
        {
            Debug.LogError("NormalTextureRenderFeature: Missing material for normals rendering.");
            return;
        }

        // Create the render pass and set its event before opaque rendering
        renderPass = new NormalsRenderPass(settings.normalsMaterial)
        {
            renderPassEvent = RenderPassEvent.BeforeRenderingOpaques
        };
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (settings.normalsMaterial != null)
        {
            renderer.EnqueuePass(renderPass);
        }
    }
}
