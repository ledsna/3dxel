// using UnityEngine;
// using UnityEngine.Experimental.Rendering;
// using UnityEngine.Rendering;
// using UnityEngine.Rendering.RenderGraphModule;
// using UnityEngine.Rendering.Universal;
//
// public class BilateralBlurPass : ScriptableRenderPass
// {
//     private GodRaysFeature.BlurSettings settings;
//     private Material material;
//
//     private TextureDesc blurDescriptor;
//
//     // Shader Properties
//     // -----------------
//     private static readonly int gaussSamplesId = Shader.PropertyToID("_GaussSamples");
//     private static readonly int gaussAmountId = Shader.PropertyToID("_GaussAmount");
//
//     private static string k_BlurTextureName = "_BlurTexture";
//     private static string k_HorizontalBlurPassName = "Vertical Blur";
//     private static string k_VerticalBlurPassName = "Horizontal Blur";
//     
//     public BilateralBlurPass(Material material, GodRaysFeature.BlurSettings settings)
//     {
//         this.material = material;
//         this.settings = settings;
//     }
//
//     class PassData
//     {
//         internal TextureHandle sourceTexture;
//         internal Material material;
//     }
//
//     public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
//     {
//         var resourceData = frameData.Get<UniversalResourceData>();
//         
//         // The following line ensures that the render pass doesn't blit
//         // from the back buffer.
//         if (resourceData.isActiveTargetBackBuffer)
//             return;
//         
//         var srcCamColor = resourceData.activeColorTexture;
//         
//         // This check is to avoid an error from the material preview in the scene
//         if (!srcCamColor.IsValid() || !srcCamColor.IsValid())
//             return;
//         
//         UpdateSettings();
//
//         blurDescriptor = resourceData.activeColorTexture.GetDescriptor(renderGraph);
//         blurDescriptor.name = k_BlurTextureName;
//         blurDescriptor.depthBufferBits = 0;
//         var dst = renderGraph.CreateTexture(blurDescriptor);
//         
//         
//     }
//
//     private void UpdateSettings()
//     {
//         if (material == null) return;
//
//         material.SetInt(gaussSamplesId, settings.gaussSamples);
//         material.SetFloat(gaussAmountId, settings.gaussAmount);
//     }
// }