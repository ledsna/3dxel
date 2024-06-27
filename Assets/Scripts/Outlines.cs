using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering;

public class Outlines : ScriptableRendererFeature {

    [System.Serializable]
    private class OutlineSettings {

        public float depthOutlineScale = 1.0f;
        public float normalsOutlineScale = 1.0f;

        [Header("Depth Settings")]
        [Range(0.0f, 200.0f)]
        public float depthThreshold = 1f;

        [Header("Normal Settings")]
        [Range(0.0f, 1.0f)]
        public float normalThreshold = 0.4f;

        [Header("Normal Highlight Intensity")]
        [Range(1.0f, 10f)]
        public float highlightPower = 1.5f;

        [Header("Depth Shadow Intensity")]
        [Range(-1.0f, 5.0f)]
        public float shadowPower = 0.5f;

        [Header("General Scene View Space Normal Texture Settings")]
        public RenderTextureFormat colorFormat;
        public int depthBufferBits;
        public FilterMode filterMode;
        public Color backgroundColor = Color.clear;

        [Header("View Space Normal Texture Object Draw Settings")]
        public PerObjectData perObjectData;
        public bool enableDynamicBatching;
        public bool enableInstancing;
    }

    private class OutlinePass : ScriptableRenderPass {
        
        private readonly Material OutlineMaterial;
        private OutlineSettings settings;

        private FilteringSettings filteringSettings;

        private readonly List<ShaderTagId> shaderTagIdList;
        private readonly Material normalsMaterial;

        private RTHandle normals;
        private RendererList normalsRenderersList;

        RTHandle temporaryBuffer;

        public OutlinePass(RenderPassEvent renderPassEvent, LayerMask layerMask, OutlineSettings settings) {
            this.settings = settings;
            this.renderPassEvent = renderPassEvent;

            OutlineMaterial = new Material(Shader.Find("Hidden/Outlines"));
            OutlineMaterial.SetFloat("_DepthOutlineScale", settings.depthOutlineScale);
            OutlineMaterial.SetFloat("_HighlightPower", settings.highlightPower);
            OutlineMaterial.SetFloat("_ShadowPower", settings.shadowPower);
            OutlineMaterial.SetFloat("_NormalOutlineScale", settings.normalsOutlineScale);
            OutlineMaterial.SetFloat("_DepthThreshold", settings.depthThreshold);
            OutlineMaterial.SetFloat("_NormalThreshold", settings.normalThreshold);

            filteringSettings = new FilteringSettings(RenderQueueRange.opaque, layerMask);

            shaderTagIdList = new List<ShaderTagId> {
                new ("UniversalForward"),
                new ("UniversalForwardOnly"),
                new ("LightweightForward"),
                new ("SRPDefaultUnlit"),
            };

            // normalsMaterial = new Material(Shader.Find("Hidden/ViewSpaceNormals"));
            
            normalsMaterial = new Material(Shader.Find("Custom/VSN"));

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
            if (!OutlineMaterial || !normalsMaterial ||
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
            
            cmd.SetGlobalTexture(Shader.PropertyToID("_FilteredNormalsTexture"), normals.rt);
            
            using (new ProfilingScope(cmd, new ProfilingSampler("Outlines"))) {

                Blitter.BlitCameraTexture(cmd, renderingData.cameraData.renderer.cameraColorTargetHandle, temporaryBuffer, OutlineMaterial, 0);
                Blitter.BlitCameraTexture(cmd, temporaryBuffer, renderingData.cameraData.renderer.cameraColorTargetHandle);
            }

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        public void Release(){
            CoreUtils.Destroy(OutlineMaterial);
            CoreUtils.Destroy(normalsMaterial);
            normals?.Release();
            temporaryBuffer?.Release();
        }

    }

    [SerializeField] private RenderPassEvent renderPassEvent = RenderPassEvent.BeforeRenderingSkybox;
    [SerializeField] private LayerMask outlinesLayerMask;
    
    [SerializeField] private OutlineSettings outlineSettings = new OutlineSettings();

    private OutlinePass _outlinePass;
    
    public override void Create() {
        if (renderPassEvent < RenderPassEvent.BeforeRenderingPrePasses)
            renderPassEvent = RenderPassEvent.BeforeRenderingPrePasses;

        _outlinePass = new OutlinePass(renderPassEvent, outlinesLayerMask, outlineSettings);
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