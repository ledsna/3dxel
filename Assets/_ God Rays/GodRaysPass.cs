using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RenderGraphModule.Util;
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
    private static readonly string drawOnlyGodRaysKeyWord = "_DRAW_GOD_RAYS";

    private static string k_GodRaysTextureName = "_GodRaysTexture";
    private static string k_GodRaysPassName = "GodRaysRenderPass";
    private static LocalKeyword drawGodRaysOnlyLocalKeyword;


    public GodRaysPass(Material material, GodRaysFeature.Settings defaultSettings)
    {
        this.material = material;
        this.defaultSettings = defaultSettings;
        requiresIntermediateTexture = true;
        drawGodRaysOnlyLocalKeyword = new LocalKeyword(material.shader, drawOnlyGodRaysKeyWord);
    }

    public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
    {
        var resourceData = frameData.Get<UniversalResourceData>();
        
        var cameraData = frameData.Get<UniversalCameraData>();
        
        // The following line ensures that the render pass doesn't blit
        // from the back buffer.
        if (resourceData.isActiveTargetBackBuffer)
            return;
        
        // resourceData.cameraDepthTexture
        
        var srcCamColor = resourceData.activeColorTexture;
        godRaysTextureDescriptor = srcCamColor.GetDescriptor(renderGraph);
        godRaysTextureDescriptor.name = k_GodRaysTextureName;
        godRaysTextureDescriptor.depthBufferBits = 0;
        var dst = renderGraph.CreateTexture(godRaysTextureDescriptor);
        
        UpdateGodRaysSettings(cameraData);
        
        // This check is to avoid an error from the material preview in the scene
        if (!srcCamColor.IsValid() || !dst.IsValid())
            return;

        RenderGraphUtils.BlitMaterialParameters paraGodRays = new(srcCamColor, dst, material, 0);
        renderGraph.AddBlitPass(paraGodRays, k_GodRaysPassName);

        resourceData.cameraColor = dst;
    }

    private void UpdateGodRaysSettings(UniversalCameraData cameraData)
    {
        if (material == null) return;

        material.SetInt(sampleCountId, defaultSettings.sampleCount);
        material.SetFloat(densityId, defaultSettings.A);
        material.SetFloat(weightId, defaultSettings.B);
        material.SetFloat(decayId, defaultSettings.C);
        material.SetFloat(exposureId, defaultSettings.D);
        material.SetColor(godRayColorId, defaultSettings.godRayColor);
        // if (material.shaderKeywords.Contains(drawOnlyGodRaysKeyWord))
        material.SetKeyword(drawGodRaysOnlyLocalKeyword, defaultSettings.DrawGodRaysOnly);
    }
}