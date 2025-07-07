using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.RenderGraphModule;


namespace __3D_Pixel_Art.Scripts
{
    public class WSPositionsTextureRendererFeature : ScriptableRendererFeature
    {
        public class PositionsTexturePass : ScriptableRenderPass
        {
            private const string PositionsTextureName = "_PositionsTexture";
            
            private readonly List<ShaderTagId> m_ShaderTagIds;
            private readonly Material m_Material;
            private readonly LayerMask m_LayerMask;

            public PositionsTexturePass(LayerMask layerMask, Material material)
            {
                m_Material = material;
                // m_Material = new Material(Shader.Find("Ledsna/BillboardPositions"));
                
                m_ShaderTagIds = new List<ShaderTagId>
                {
                    new("UniversalForward"),
                    new("UniversalForwardOnly"),
                    new("CustomSRPDefault"),
                };
                
                m_LayerMask = layerMask;
            }

            private class PassData
            {
                internal RendererListHandle rendererListHandle;
                internal TextureHandle activeColorTexture;
                internal Material material;
                internal TextureHandle PositionsTexture, activeDepthTexture;
            }

            public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameContext)
            {
                var resourceData = frameContext.Get<UniversalResourceData>();
                var renderingData = frameContext.Get<UniversalRenderingData>();
                var cameraData = frameContext.Get<UniversalCameraData>();
                var lightData = frameContext.Get<UniversalLightData>();

                // if (cameraData.camera.cameraType != CameraType.Game) return;
                using (IRasterRenderGraphBuilder builder =
                       renderGraph.AddRasterRenderPass<PassData>("PositionsTexturePass", out var passData))
                {
                    var drawSettings = RenderingUtils.CreateDrawingSettings(m_ShaderTagIds, renderingData, cameraData,
                        lightData, cameraData.defaultOpaqueSortFlags);
                    drawSettings.overrideMaterial = m_Material;
                    
                    var rendererListParams =
                        new RendererListParams(renderingData.cullResults, drawSettings,
                            new FilteringSettings(RenderQueueRange.opaque, m_LayerMask));
                    var cameraTextureDescriptor = cameraData.cameraTargetDescriptor;
                    // cameraTextureDescriptor.colorFormat
                    var renderTextureDescriptor = new RenderTextureDescriptor(cameraTextureDescriptor.width,
                        cameraTextureDescriptor.height, RenderTextureFormat.ARGBFloat, 0,
                        cameraTextureDescriptor.mipCount, RenderTextureReadWrite.Default);
                    
                    var PositionsTexture = UniversalRenderer.CreateRenderGraphTexture(renderGraph,
                        renderTextureDescriptor, PositionsTextureName, true);
                    
                    passData.material = m_Material;
                    passData.rendererListHandle = renderGraph.CreateRendererList(rendererListParams);
                    passData.PositionsTexture = PositionsTexture;
                    passData.activeColorTexture = resourceData.activeColorTexture;
                    passData.activeDepthTexture = resourceData.activeDepthTexture;
                    builder.UseRendererList(passData.rendererListHandle);
                    
                    builder.SetRenderAttachment(PositionsTexture, 0);
                    builder.SetRenderAttachmentDepth(resourceData.activeDepthTexture, AccessFlags.ReadWrite);

                    builder.SetRenderFunc((
                            PassData data, RasterGraphContext context) =>
                            {
                                context.cmd.ClearRenderTarget(false, true, Color.clear);
                                context.cmd.DrawRendererList(data.rendererListHandle);
                            });
                    
                    builder.SetGlobalTextureAfterPass(PositionsTexture, Shader.PropertyToID(PositionsTextureName));
                }
            }
        }
        
        [SerializeField] private RenderPassEvent renderPassEvent = RenderPassEvent.BeforeRenderingOpaques;
        [SerializeField] private LayerMask textureLayerMask;
        [SerializeField] private Material material;
        PositionsTexturePass m_ScriptablePass;
        
        /// <inheritdoc/>
        public override void Create()
        {
            m_ScriptablePass = new PositionsTexturePass(textureLayerMask, material);
            m_ScriptablePass.renderPassEvent = renderPassEvent;
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            renderer.EnqueuePass(m_ScriptablePass);
        }
    }
}
