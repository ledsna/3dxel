using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;


public class GodRaysPass : ScriptableRenderPass
{
    private GodRaysFeature.GodRaysSettings defaultGodRaysSettings;
    private Material godRaysMaterial;

    private GodRaysFeature.BlurSettings defaultBlurSettings;
    private Material blurMaterial;

    private TextureDesc godRaysTextureDescriptor;

    // God Rays Shader Properties
    // --------------------------
    private static readonly int intensityId = Shader.PropertyToID("_Intensity");
    private static readonly int scatteringId = Shader.PropertyToID("_Scattering");
    private static readonly int godRayColorId = Shader.PropertyToID("_GodRayColor");
    private static readonly int maxDistanceId = Shader.PropertyToID("_MaxDistance");
    private static readonly int jitterVolumetricId = Shader.PropertyToID("_JitterVolumetric");

    private static string k_GodRaysTextureName = "_GodRaysTexture";
    private static string k_GodRaysPassName = "God Rays";
    private static string k_CompositePassName = "Compositing";

    private Dictionary<SampleCountEnum, LocalKeyword> iterationKeywords;

    // Blur Shader Properties
    // ----------------------
    private static readonly int gaussSamplesId = Shader.PropertyToID("_GaussSamples");
    private static readonly int gaussAmountId = Shader.PropertyToID("_GaussAmount");
    private static string k_HorizontalBlurTextureName = "_HorizontalBlurTexture";
    private static string k_VerticalBlurTextureName = "_VerticalBlurTexture";
    private static string k_HorizontalBlurPassName = "Horizontal Blur";
    private static string k_VerticalBlurPassName = "Vertical Blur";

    // private GodRaysVolumeComponent volumeComponent;

    public GodRaysPass(Material godRaysMaterial, GodRaysFeature.GodRaysSettings defaultGodRaysSettings,
        Material blurMaterial, GodRaysFeature.BlurSettings defaultBlurSettings)
    {
        this.godRaysMaterial = godRaysMaterial;
        this.defaultGodRaysSettings = defaultGodRaysSettings;

        this.defaultBlurSettings = defaultBlurSettings;
        this.blurMaterial = blurMaterial;

        iterationKeywords = new Dictionary<SampleCountEnum, LocalKeyword>
        {
            [SampleCountEnum._8] = new(godRaysMaterial.shader, "ITERATIONS_8"),
            [SampleCountEnum._16] = new(godRaysMaterial.shader, "ITERATIONS_16"),
            [SampleCountEnum._32] = new(godRaysMaterial.shader, "ITERATIONS_32"),
            [SampleCountEnum._64] = new(godRaysMaterial.shader, "ITERATIONS_64"),
            [SampleCountEnum._86] = new(godRaysMaterial.shader, "ITERATIONS_86"),
            [SampleCountEnum._128] = new(godRaysMaterial.shader, "ITERATIONS_128"),
        };
    }

    class GodRaysPassData
    {
        internal TextureHandle depthCameraTexture;
        internal TextureHandle mainLightShadowMapTexture;
        internal Material material;
    }

    class BlurPassData
    {
        internal TextureHandle sourceTexture;
        internal Material material;
        internal int pass;
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

        UpdateSettings();

        ComputeGodRaysPass(renderGraph, resourceData, out var godRaysTexture);

        if (IsBlurEnabled())
        {
            var desc = srcCamColor.GetDescriptor(renderGraph);
            godRaysTextureDescriptor.width = desc.width;
            godRaysTextureDescriptor.height = desc.height;

            godRaysTextureDescriptor.name = k_HorizontalBlurTextureName;
            var horizontalBlurredTexture = renderGraph.CreateTexture(godRaysTextureDescriptor);
            BlurPass(renderGraph, resourceData.cameraDepthTexture, godRaysTexture, horizontalBlurredTexture, 0);
            BlurPass(renderGraph, resourceData.cameraDepthTexture, horizontalBlurredTexture, godRaysTexture, 1);
        }

        CompositingPass(renderGraph, resourceData, godRaysTexture);
    }

    private void ComputeGodRaysPass(RenderGraph renderGraph, UniversalResourceData resourceData,
        out TextureHandle godRaysTexture)
    {
        using (var builder = renderGraph.AddRasterRenderPass<GodRaysPassData>(k_GodRaysPassName,
                   out var passData))
        {
            var srcCameraColorDesc = resourceData.activeColorTexture.GetDescriptor(renderGraph);
            godRaysTextureDescriptor = new TextureDesc(
                srcCameraColorDesc.width,
                srcCameraColorDesc.height,
                false, false
            );
            godRaysTextureDescriptor.format = GraphicsFormat.R16_UNorm;
            godRaysTextureDescriptor.depthBufferBits = 0;
            godRaysTextureDescriptor.clearBuffer = false;
            godRaysTextureDescriptor.msaaSamples = MSAASamples.None;
            godRaysTextureDescriptor.name = k_GodRaysTextureName;

            godRaysTexture = renderGraph.CreateTexture(godRaysTextureDescriptor);

            builder.SetRenderAttachment(godRaysTexture, 0);

            passData.depthCameraTexture = resourceData.cameraDepthTexture;
            passData.mainLightShadowMapTexture = resourceData.mainShadowsTexture;
            passData.material = godRaysMaterial;

            builder.UseTexture(passData.depthCameraTexture);
            builder.UseTexture(passData.mainLightShadowMapTexture);

            builder.SetRenderFunc<GodRaysPassData>(ExecuteGodRaysPass);
        }
    }

    private void CompositingPass(RenderGraph renderGraph, UniversalResourceData resourceData,
        TextureHandle godRaysTexture)
    {
        using (var builder = renderGraph.AddRasterRenderPass<CompositePassData>(k_CompositePassName,
                   out var passData))
        {
            passData.sourceTexture = resourceData.activeColorTexture;
            passData.godRaysTexture = godRaysTexture;
            passData.material = godRaysMaterial;

            var desc = passData.sourceTexture.GetDescriptor(renderGraph);
            desc.name = "_CompositeTexture";

            var compositeTexture = renderGraph.CreateTexture(desc);

            builder.SetInputAttachment(passData.sourceTexture, 0);
            builder.SetInputAttachment(passData.godRaysTexture, 1);

            builder.SetRenderAttachment(compositeTexture, 0);

            builder.SetRenderFunc<CompositePassData>(ExecuteCompositePass);

            resourceData.cameraColor = compositeTexture;
        }
    }

    private void BlurPass(RenderGraph renderGraph, TextureHandle depthTexture, TextureHandle source,
        TextureHandle destination, int pass)
    {
        var passName = pass == 0 ? k_HorizontalBlurPassName : k_VerticalBlurPassName;

        using (var builder = renderGraph.AddRasterRenderPass<BlurPassData>(passName,
                   out var passData))
        {
            passData.material = blurMaterial;
            passData.sourceTexture = source;
            passData.pass = pass;

            builder.SetInputAttachment(passData.sourceTexture, 0);
            // TODO: Ask question about using SetInputAttachment with depthTexture — why I can't store in framebuffer DepthTexture?
            // builder.SetInputAttachment(depthTexture, 1);
            builder.UseTexture(depthTexture);
            builder.SetRenderAttachment(destination, 0);

            builder.SetRenderFunc<BlurPassData>(ExecuteBlurPass);
        }
    }

    private static void ExecuteGodRaysPass(GodRaysPassData data, RasterGraphContext context)
    {
        Blitter.BlitTexture(context.cmd, new Vector4(1, 1, 0, 0), data.material, 0);
    }


    private static void ExecuteCompositePass(CompositePassData data, RasterGraphContext context)
    {
        Blitter.BlitTexture(context.cmd, new Vector4(1, 1, 0, 0), data.material, 1);
    }


    private static void ExecuteBlurPass(BlurPassData data, RasterGraphContext context)
    {
        Blitter.BlitTexture(context.cmd, new Vector4(1, 1, 0, 0), data.material, data.pass);
    }

    private void UpdateSettings()
    {
        if (godRaysMaterial == null) return;
        UpdateGodRaysSettings();

        if (blurMaterial == null) return;
        UpdateBlurSettings();
    }

    private void UpdateGodRaysSettings()
    {
        var volumeComponent = VolumeManager.instance.stack.GetComponent<GodRaysVolumeComponent>();
        
        godRaysMaterial.SetFloat(intensityId,
            GetFloatFromVolumeOrDefault(defaultGodRaysSettings.Intensity, volumeComponent.Intensity));

        godRaysMaterial.SetFloat(scatteringId,
            GetFloatFromVolumeOrDefault(defaultGodRaysSettings.Scattering, volumeComponent.Scattering));

        godRaysMaterial.SetColor(godRayColorId,
            GetColorFromVolumeOrDefault(defaultGodRaysSettings.GodRayColor, volumeComponent.GodRayColor));

        godRaysMaterial.SetFloat(maxDistanceId,
            GetFloatFromVolumeOrDefault(defaultGodRaysSettings.MaxDistance, volumeComponent.MaxDistance));

        godRaysMaterial.SetFloat(jitterVolumetricId,
            GetFloatFromVolumeOrDefault(defaultGodRaysSettings.JitterVolumetric, volumeComponent.JitterVolumetric));
    }

    private void UpdateBlurSettings()
    {
        var volumeComponent = VolumeManager.instance.stack.GetComponent<GodRaysVolumeComponent>();

        blurMaterial.SetInt(gaussSamplesId,
            Mathf.Max(GetIntFromVolumeOrDefault(defaultBlurSettings.GaussSamples, volumeComponent.GaussSamples) - 1, 0));

        blurMaterial.SetFloat(gaussAmountId,
            GetFloatFromVolumeOrDefault(defaultBlurSettings.GaussAmount, volumeComponent.GaussAmount));
    }

    private int GetIntFromVolumeOrDefault(int defaultValue, IntParameter parameter) =>
        parameter.overrideState ? parameter.value : defaultValue;

    private float GetFloatFromVolumeOrDefault(float defaultValue, FloatParameter parameter) =>
        parameter.overrideState ? parameter.value : defaultValue;

    private Color GetColorFromVolumeOrDefault(Color defaultValue, ColorParameter parameter) =>
        parameter.overrideState ? parameter.value : defaultValue;

    private bool IsBlurEnabled()
    {
        var volumeComponent = VolumeManager.instance.stack.GetComponent<GodRaysVolumeComponent>();
        if (volumeComponent.GaussSamples.overrideState)
            return volumeComponent.GaussSamples.value != 0;
        return defaultBlurSettings.GaussSamples != 0;
    }
}