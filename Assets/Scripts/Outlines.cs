using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering;

public class ViewSpaceNormalsTexture : ScriptableRendererFeature {

    [System.Serializable]
    public class TextureSettings {

        [Header("Texture Settings")]
        public RenderTextureFormat colorFormat;
        public int depthBufferBits;
        public FilterMode filterMode;
        public Color backgroundColor = Color.clear;

        [Header("Texture Object Draw Settings")]
        public PerObjectData perObjectData;
        public bool enableDynamicBatching;
        public bool enableInstancing;
    }

    private class NormalsTexturePass : ScriptableRenderPass {
        private TextureSettings settings;

        private FilteringSettings filteringSettings;

        private readonly List<ShaderTagId> shaderTagIdList;
        private readonly Material normalsMaterial;

        private RTHandle normals;
        private RendererList normalsRenderersList;

        RTHandle temporaryBuffer;

        public NormalsTexturePass(RenderPassEvent renderPassEvent, LayerMask layerMask, TextureSettings settings) {
            this.settings = settings;
            this.renderPassEvent = renderPassEvent;

            filteringSettings = new FilteringSettings(RenderQueueRange.opaque, layerMask);

            shaderTagIdList = new List<ShaderTagId> {
                new ("UniversalForward"),
                new ("UniversalForwardOnly"),
                new ("LightweightForward"),
                new ("SRPDefaultUnlit"),
            };

            normalsMaterial = new Material(Shader.Find("Hidden/ViewSpaceNormals"));
        }

        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData) {
            // Normals
            RenderTextureDescriptor textureDescriptor = renderingData.cameraData.cameraTargetDescriptor;
            textureDescriptor.colorFormat = settings.colorFormat;
            textureDescriptor.depthBufferBits = settings.depthBufferBits;
            RenderingUtils.ReAllocateIfNeeded(ref normals, textureDescriptor, settings.filterMode);

            // Color Buffer
            textureDescriptor.depthBufferBits = 0;
            RenderingUtils.ReAllocateIfNeeded(ref temporaryBuffer, textureDescriptor, FilterMode.Point);

            ConfigureTarget(normals, renderingData.cameraData.renderer.cameraDepthTargetHandle);
            ConfigureClear(ClearFlag.Color, settings.backgroundColor);
        }
        
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData) {
            if (!normalsMaterial ||
                renderingData.cameraData.renderer.cameraColorTargetHandle.rt == null || temporaryBuffer.rt == null)
                return;

            CommandBuffer cmd = CommandBufferPool.Get();
            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();
                
            DrawingSettings drawSettings = CreateDrawingSettings(shaderTagIdList, ref renderingData, renderingData.cameraData.defaultOpaqueSortFlags);
            drawSettings.perObjectData = settings.perObjectData;
            drawSettings.enableDynamicBatching = settings.enableDynamicBatching;
            drawSettings.enableInstancing = settings.enableInstancing;
            drawSettings.overrideMaterial = normalsMaterial;
            
            RendererListParams normalsRenderersParams = new RendererListParams(renderingData.cullResults, drawSettings, filteringSettings);
            normalsRenderersList = context.CreateRendererList(ref normalsRenderersParams);

            cmd.DrawRendererList(normalsRenderersList);
            
            cmd.SetGlobalTexture(Shader.PropertyToID("_NormalsTexture"), normals.rt);
            
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        public void Release(){
            CoreUtils.Destroy(normalsMaterial);
            normals?.Release();
            temporaryBuffer?.Release();
        }

    }

    [SerializeField] private RenderPassEvent renderPassEvent = RenderPassEvent.BeforeRenderingSkybox;
    [SerializeField] private LayerMask outlinesLayerMask;
    
    [SerializeField] public TextureSettings outlineSettings = new TextureSettings();

    private NormalsTexturePass _outlinePass;
    
    public override void Create() {
        if (renderPassEvent < RenderPassEvent.BeforeRenderingPrePasses)
            renderPassEvent = RenderPassEvent.BeforeRenderingPrePasses;

        _outlinePass = new NormalsTexturePass(renderPassEvent, outlinesLayerMask, outlineSettings);
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData) {
        renderer.EnqueuePass(_outlinePass);
    }

    protected override void Dispose(bool disposing){
        if (disposing)
        {
            _outlinePass?.Release();
        }
    }

}