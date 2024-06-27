// using UnityEngine;
// using UnityEngine.Rendering;
// using UnityEngine.Rendering.Universal;
//
// public class StencilWriterFeature : ScriptableRendererFeature
// {
//     class StencilWriterPass : ScriptableRenderPass
//     {
//         private string profilerTag = "Write Stencil";
//         private RenderTargetIdentifier cameraColorTargetIdent;
//         private FilteringSettings filteringSettings;
//
//         public StencilWriterPass()
//         {
//             // Set the render pass event to BeforeRenderingOpaques to make sure it writes to the stencil buffer early in the rendering process
//             renderPassEvent = RenderPassEvent.BeforeRenderingOpaques;
//
//             // Layer mask setup, replace "YourLayerName" with the layer you want to target
//             filteringSettings = new FilteringSettings(RenderQueueRange.all, LayerMask.GetMask("YourLayerName"));
//         }
//
//         public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
//         {
//             cameraColorTargetIdent = BuiltinRenderTextureType.CameraTarget;
//
//             // Configure stencil state
//             StencilState stencilState = new StencilState(true, 0xFF, 0xFF, CompareFunction.Always, StencilOp.Replace, StencilOp.Keep, StencilOp.Keep);
//             cmd.SetGlobalInt("_StencilRef", 1);
//             cmd.SetGlobalInt("_StencilComp", (int)CompareFunction.Always);
//             cmd.SetGlobalInt("_StencilPass", (int)StencilOp.Replace);
//             cmd.SetGlobalInt("_StencilFail", (int)StencilOp.Keep);
//             cmd.SetGlobalInt("_StencilZFail", (int)StencilOp.Keep);
//             cmd.SetGlobalInteger("_StencilWriteMask", 0xFF);
//             cmd.SetGlobalInteger("_StencilReadMask", 0xFF);
//         }
//
//         public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
//         {
//             CommandBuffer cmd = CommandBufferPool.Get(profilerTag);
//             using (new ProfilingScope(cmd, new ProfilingSampler(profilerTag)))
//             {
//                 context.ExecuteCommandBuffer(cmd);
//                 cmd.Clear();
//
//                 var drawingSettings = CreateDrawingSettings(new ShaderTagId("UniversalForward"), ref renderingData, renderingData.cameraData.defaultOpaqueSortFlags);
//                 drawingSettings.overrideMaterial = null;
//                 drawingSettings.overrideMaterialPassIndex = 0;
//                 drawingSettings.perObjectData = PerObjectData.None;
//                 drawingSettings.stencilSettings = new StencilState(true, 0xFF, 0xFF, CompareFunction.Always, StencilOp.Replace, StencilOp.Keep, StencilOp.Keep);
//
//                 context.DrawRenderers(renderingData.cullResults, ref drawingSettings, ref filteringSettings);
//             }
//
//             context.ExecuteCommandBuffer(cmd);
//             CommandBufferPool.Release(cmd);
//         }
//     }
//
//     private StencilWriterPass m_ScriptablePass;
//
//     public override void Create()
//     {
//         m_ScriptablePass = new StencilWriterPass();
//
//         // Configuring the pass doesn't add it to the renderer. It's added on demand at the renderer's discretion.
//     }
//
//     public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
//     {
//         renderer.EnqueuePass(m_ScriptablePass);
//     }
// }
