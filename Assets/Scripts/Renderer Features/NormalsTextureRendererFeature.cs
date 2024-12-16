using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.RenderGraphModule;


namespace Renderer_Features
{
    public class NormalsTextureRendererFeature : ScriptableRendererFeature
    {
        public class NormalsTexturePass : ScriptableRenderPass
        {
            private const string normalsTextureName = "_NormalsTexture";
            
            private readonly List<ShaderTagId> m_ShaderTagIds;
            private readonly Material m_Material;
            private readonly LayerMask m_LayerMask;

            public NormalsTexturePass(LayerMask layerMask)
            {
                m_Material = new Material(Shader.Find("Hidden/ViewSpaceNormals"));
                m_ShaderTagIds = new List<ShaderTagId>
                {
                    new("UniversalForward"),
                    new("UniversalForwardOnly"),
                    new("CustomSRPDefault")
                };
                
                m_LayerMask = layerMask;
            }

            private class PassData
            {
                internal RendererListHandle rendererListHandle;
                internal TextureHandle activeColorTexture;
                internal Material material;
                internal TextureHandle normalsTexture, activeDepthTexture;
            }

            public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameContext)
            {
                var resourceData = frameContext.Get<UniversalResourceData>();
                var renderingData = frameContext.Get<UniversalRenderingData>();
                var cameraData = frameContext.Get<UniversalCameraData>();
                var lightData = frameContext.Get<UniversalLightData>();

                // if (cameraData.camera.cameraType != CameraType.Game) return;
                using (IRasterRenderGraphBuilder builder =
                       renderGraph.AddRasterRenderPass<PassData>("NormalsTexturePass", out var passData))
                {
                    var drawSettings = RenderingUtils.CreateDrawingSettings(m_ShaderTagIds, renderingData, cameraData,
                        lightData, cameraData.defaultOpaqueSortFlags);
                    drawSettings.overrideMaterial = m_Material;
                    
                    var rendererListParams =
                        new RendererListParams(renderingData.cullResults, drawSettings,
                            new FilteringSettings(RenderQueueRange.opaque, m_LayerMask));
                    var cameraTextureDescriptor = cameraData.cameraTargetDescriptor;
                    var renderTextureDescriptor = new RenderTextureDescriptor(cameraTextureDescriptor.width,
                        cameraTextureDescriptor.height, cameraTextureDescriptor.colorFormat, 0,
                        cameraTextureDescriptor.mipCount, RenderTextureReadWrite.Default);
                    
                    var normalsTexture = UniversalRenderer.CreateRenderGraphTexture(renderGraph,
                        renderTextureDescriptor, normalsTextureName, false);
                    
                    passData.material = m_Material;
                    passData.rendererListHandle = renderGraph.CreateRendererList(rendererListParams);
                    passData.normalsTexture = normalsTexture;
                    // passData.activeColorTexture = resourceData.activeColorTexture;
                    // passData.activeDepthTexture = resourceData.activeDepthTexture;
                    builder.UseRendererList(passData.rendererListHandle);
                    
                    builder.SetRenderAttachment(normalsTexture, 0);
                    // builder.SetRenderAttachmentDepth(resourceData.activeDepthTexture, AccessFlags.Read);

                    builder.SetRenderFunc((
                            PassData data, RasterGraphContext context) =>
                            {
                                context.cmd.ClearRenderTarget(false, true, Color.clear);
                                context.cmd.DrawRendererList(data.rendererListHandle);
                            });
                    
                    builder.SetGlobalTextureAfterPass(normalsTexture, Shader.PropertyToID(normalsTextureName));
                }
            }

        }
        
        [SerializeField] private RenderPassEvent renderPassEvent = RenderPassEvent.BeforeRenderingOpaques;
        [SerializeField] private LayerMask textureLayerMask;
        NormalsTexturePass m_ScriptablePass;
        
        /// <inheritdoc/>
        public override void Create()
        {
            m_ScriptablePass = new NormalsTexturePass(textureLayerMask);
            m_ScriptablePass.renderPassEvent = renderPassEvent;
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            renderer.EnqueuePass(m_ScriptablePass);
        }
    }
}
