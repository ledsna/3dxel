using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;


public class GodRaysPass : ScriptableRenderPass
{
    private GodRaysFeature.Settings defaultSettings;
    private Material material;

    private TextureDesc godRaysTextureDescriptor;

    // Shader Properties
    // -----------------
    private static readonly int sampleCountId = Shader.PropertyToID("_SampleCount");
    private static readonly int densityId = Shader.PropertyToID("_A");
    private static readonly int weightId = Shader.PropertyToID("_B");
    private static readonly int decayId = Shader.PropertyToID("_C");
    private static readonly int exposureId = Shader.PropertyToID("_D");
    private static readonly int godRayColorId = Shader.PropertyToID("_GodRayColor");
    private static readonly int maxDistanceId = Shader.PropertyToID("_MaxDistance");
    private static readonly int jitterVolumetricId = Shader.PropertyToID("_JitterVolumetric");
    private static readonly string drawOnlyGodRaysKeyWord = "_DRAW_GOD_RAYS";

    private static string k_GodRaysTextureName = "_GodRaysTexture";
    private static string k_GodRaysPassName = "God Rays";
    private static string k_CompositePassName = "Compositing";
    private static LocalKeyword drawGodRaysOnlyLocalKeyword;


    public GodRaysPass(Material material, GodRaysFeature.Settings defaultSettings)
    {
        this.material = material;
        this.defaultSettings = defaultSettings;
        drawGodRaysOnlyLocalKeyword = new LocalKeyword(material.shader, drawOnlyGodRaysKeyWord);
    }

    class GodRaysPassData
    {
        internal TextureHandle sourceTexture;
        internal TextureHandle depthCameraTexture;
        internal TextureHandle mainLightShadowMapTexture;
        internal Material material;
    }

    class CompositePassData
    {
        internal TextureHandle sourceTexture;
        internal TextureHandle godRaysTexture;
        internal Material material;
    }
    

    public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
    {
        var resourceData = frameData.Get<UniversalResourceData>();
        
        // The following line ensures that the render pass doesn't blit
        // from the back buffer.
        if (resourceData.isActiveTargetBackBuffer)
            return;
        
        var srcCamColor = resourceData.activeColorTexture;
        
        // This check is to avoid an error from the material preview in the scene
        if (!srcCamColor.IsValid() || !srcCamColor.IsValid())
            return;
        
        UpdateGodRaysSettings();
        
        TextureHandle godRaysTH;
        TextureHandle compositeTH;
        
        using (var builder = renderGraph.AddRasterRenderPass<GodRaysPassData>(k_GodRaysPassName,
                   out var passData))
        {
            godRaysTextureDescriptor = srcCamColor.GetDescriptor(renderGraph);
            godRaysTextureDescriptor.name = k_GodRaysTextureName;
            godRaysTextureDescriptor.depthBufferBits = 0;
            godRaysTextureDescriptor.clearBuffer = false;
            godRaysTextureDescriptor.msaaSamples = MSAASamples.None;
            // godRaysTextureDescriptor.colorFormat = Gra;
            // TODO: Change texture format to lightweight version like R8?

            // var srcCameraColorDesc = srcCamColor.GetDescriptor(renderGraph);
            // godRaysTextureDescriptor = new TextureDesc(
            //     srcCameraColorDesc.width,
            //     srcCameraColorDesc.height,
            //     false, false
            // );
            // godRaysTextureDescriptor.format = GraphicsFormat.R8_UNorm;
            // godRaysTextureDescriptor.depthBufferBits = 0;
            // godRaysTextureDescriptor.clearBuffer = false;
            // godRaysTextureDescriptor.msaaSamples = MSAASamples.None;
            // godRaysTextureDescriptor.name = k_GodRaysTextureName;
            
            // Down Sampling 
            var divider = (int)defaultSettings.DownSampling;
            godRaysTextureDescriptor.width /= divider;
            godRaysTextureDescriptor.height /= divider;

            godRaysTH = renderGraph.CreateTexture(godRaysTextureDescriptor);
            
            builder.SetRenderAttachment(godRaysTH, 0);

            passData.depthCameraTexture = resourceData.cameraDepthTexture;
            passData.mainLightShadowMapTexture = resourceData.mainShadowsTexture;
            passData.material = material;
            passData.sourceTexture = srcCamColor;
            
            builder.UseTexture(passData.depthCameraTexture);
            builder.UseTexture(passData.mainLightShadowMapTexture);
            
            builder.SetRenderFunc<GodRaysPassData>(ExecuteGodRaysPass);
        }
        
        using (var builder = renderGraph.AddRasterRenderPass<CompositePassData>(k_CompositePassName,
                   out var passData))
        {
            passData.sourceTexture = srcCamColor;
            passData.godRaysTexture = godRaysTH;
            passData.material = material;
            
            compositeTH = renderGraph.CreateTexture(srcCamColor);
            
            builder.UseTexture(passData.godRaysTexture);
            // builder.SetInputAttachment(passData.godRaysTexture, 0);
            builder.SetRenderAttachment(compositeTH, 0);
            
            builder.SetRenderFunc<CompositePassData>(ExecuteCompositePass);

            resourceData.cameraColor = compositeTH;
        }
    }
    
    static void ExecuteGodRaysPass(GodRaysPassData data, RasterGraphContext context)
    {
        Blitter.BlitTexture(context.cmd, data.sourceTexture, new Vector4(1, 1, 0, 0), data.material, 0);
    }
    
    static void ExecuteCompositePass(CompositePassData data, RasterGraphContext context)
    {
        data.material.SetTexture(k_GodRaysTextureName, data.godRaysTexture);
        Blitter.BlitTexture(context.cmd, data.sourceTexture, new Vector4(1, 1, 0, 0), data.material, 1);
    }

    private void UpdateGodRaysSettings()
    {
        if (material == null) return;

        material.SetInt(sampleCountId, defaultSettings.sampleCount);
        material.SetFloat(densityId, defaultSettings.A);
        material.SetFloat(weightId, defaultSettings.B);
        material.SetFloat(decayId, defaultSettings.C);
        material.SetFloat(exposureId, defaultSettings.D);
        material.SetColor(godRayColorId, defaultSettings.godRayColor);
        material.SetFloat(maxDistanceId, defaultSettings.MaxDistance);
        material.SetFloat(jitterVolumetricId ,defaultSettings.JitterVolumetric);
        
        material.SetKeyword(drawGodRaysOnlyLocalKeyword, defaultSettings.DrawGodRaysOnly);
    }
}