using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;


public class GodRaysPass : ScriptableRenderPass
{
    private GodRaysFeature.GodRaysSettings godRaysSettings;
    private Material godRaysMaterial;

    private GodRaysFeature.BlurSettings blurSettings;
    private Material blurMaterial;

    private TextureDesc godRaysTextureDescriptor;

    // God Rays Shader Properties
    // --------------------------
    private static readonly int sampleCountId = Shader.PropertyToID("_SampleCount");
    private static readonly int densityId = Shader.PropertyToID("_A");
    private static readonly int weightId = Shader.PropertyToID("_B");
    private static readonly int decayId = Shader.PropertyToID("_C");
    private static readonly int exposureId = Shader.PropertyToID("_D");
    private static readonly int godRayColorId = Shader.PropertyToID("_GodRayColor");
    private static readonly int maxDistanceId = Shader.PropertyToID("_MaxDistance");
    private static readonly int jitterVolumetricId = Shader.PropertyToID("_JitterVolumetric");

    private static string k_GodRaysTextureName = "_GodRaysTexture";
    private static string k_GodRaysPassName = "God Rays";
    private static string k_CompositePassName = "Compositing";

    private static string k_fboOptimizationKeyword = "FBO_OPTIMIZATION_APPLIED";
    private static string k_fboOptimizationForFirstPassKeyword = "FBO_OPTIMIZATION_APPLIED_FOR_FIRST_PASS";
    private GlobalKeyword fboOptimizationGlobalKeyword;
    private LocalKeyword fboOptimizationForSecondPassGlobalKeyword;

    // Blur Shader Properties
    // ----------------------
    private static readonly int gaussSamplesId = Shader.PropertyToID("_GaussSamples");
    private static readonly int gaussAmountId = Shader.PropertyToID("_GaussAmount");
    private static string k_HorizontalBlurTextureName = "_HorizontalBlurTexture";
    private static string k_VerticalBlurTextureName = "_VerticalBlurTexture";
    private static string k_HorizontalBlurPassName = "Horizontal Blur";
    private static string k_VerticalBlurPassName = "Vertical Blur";


    public GodRaysPass(Material godRaysMaterial, GodRaysFeature.GodRaysSettings godRaysSettings,
        Material blurMaterial, GodRaysFeature.BlurSettings blurSettings)
    {
        this.godRaysMaterial = godRaysMaterial;
        this.godRaysSettings = godRaysSettings;

        this.blurSettings = blurSettings;
        this.blurMaterial = blurMaterial;

        fboOptimizationGlobalKeyword = GlobalKeyword.Create(k_fboOptimizationKeyword);
        fboOptimizationForSecondPassGlobalKeyword =
            new LocalKeyword(blurMaterial.shader, k_fboOptimizationForFirstPassKeyword);
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
        internal bool useInputAttachment;
    }

    class CompositePassData
    {
        internal TextureHandle sourceTexture;
        internal TextureHandle godRaysTexture;
        internal Material material;
        internal bool isFrameBufferOptimizationApplied;
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

        ComputeGodRaysPass(renderGraph, resourceData, out var godRaysTexture);

        if (blurSettings.gaussSamples != 0)
        {
            var desc = srcCamColor.GetDescriptor(renderGraph);
            godRaysTextureDescriptor.width = desc.width;
            godRaysTextureDescriptor.height = desc.height;

            godRaysTextureDescriptor.name = k_HorizontalBlurTextureName;
            var horizontalBlurredTexture = renderGraph.CreateTexture(godRaysTextureDescriptor);
            BlurPass(renderGraph, resourceData.cameraDepthTexture, godRaysTexture, horizontalBlurredTexture, 0,
                godRaysSettings.DownSampling == GodRaysFeature.GodRaysSettings.DownSample.off);

            if (godRaysSettings.DownSampling != GodRaysFeature.GodRaysSettings.DownSample.off)
            {
                godRaysTextureDescriptor.name = k_VerticalBlurTextureName;
                var verticalBlurredTexture = renderGraph.CreateTexture(godRaysTextureDescriptor);
                BlurPass(renderGraph, resourceData.cameraDepthTexture, horizontalBlurredTexture, verticalBlurredTexture,
                    1, true);
                godRaysTexture = verticalBlurredTexture;
            }
            else
            {
                // Optimization, that remove additional texture when downsampling used 
                BlurPass(renderGraph, resourceData.cameraDepthTexture, horizontalBlurredTexture, godRaysTexture, 1,
                    true);
            }
        }

        var useInputAttachment = blurSettings.gaussSamples != 0 ||
                                 godRaysSettings.DownSampling == GodRaysFeature.GodRaysSettings.DownSample.off;
        CompositingPass(renderGraph, resourceData, godRaysTexture, useInputAttachment);
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

            // Down Sampling 
            // WARNING:
            // When using downsampling unity can't merge passes
            var divider = (int)godRaysSettings.DownSampling;
            godRaysTextureDescriptor.width /= divider;
            godRaysTextureDescriptor.height /= divider;

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
        TextureHandle godRaysTexture,
        bool useInputAttachment)
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

            if (useInputAttachment)
            {
                builder.SetInputAttachment(passData.sourceTexture, 0);
                builder.SetInputAttachment(passData.godRaysTexture, 1);
                passData.isFrameBufferOptimizationApplied = true;
            }
            else
            {
                // When downsampling applied we can't use FBO for performance improvement as origin texture have 
                // different resolution
                builder.UseTexture(passData.godRaysTexture);
                // TODO: Maybe I can remove this UseTexture?
                builder.UseTexture(passData.sourceTexture);
                passData.isFrameBufferOptimizationApplied = false;
            }

            builder.SetRenderAttachment(compositeTexture, 0);

            builder.SetRenderFunc<CompositePassData>(ExecuteCompositePass);

            resourceData.cameraColor = compositeTexture;
        }
    }

    private void BlurPass(RenderGraph renderGraph, TextureHandle depthTexture, TextureHandle source,
        TextureHandle destination, int pass, bool useInputAttachment)
    {
        var passName = pass == 0 ? k_HorizontalBlurPassName : k_VerticalBlurPassName;

        using (var builder = renderGraph.AddRasterRenderPass<BlurPassData>(passName,
                   out var passData))
        {
            passData.material = blurMaterial;
            passData.sourceTexture = source;
            passData.pass = pass;
            passData.useInputAttachment = useInputAttachment;

            if (useInputAttachment)
                builder.SetInputAttachment(passData.sourceTexture, 0);
            else
                builder.UseTexture(passData.sourceTexture);
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
        if (!data.isFrameBufferOptimizationApplied)
        {
            data.material.SetTexture(k_GodRaysTextureName, data.godRaysTexture);
            Blitter.BlitTexture(context.cmd, data.sourceTexture, new Vector4(1, 1, 0, 0), data.material, 1);
        }
        else
        {
            Blitter.BlitTexture(context.cmd, new Vector4(1, 1, 0, 0), data.material, 1);
        }
    }


    private static void ExecuteBlurPass(BlurPassData data, RasterGraphContext context)
    {
        if (data.useInputAttachment)
            Blitter.BlitTexture(context.cmd, new Vector4(1, 1, 0, 0), data.material, data.pass);
        else
            Blitter.BlitTexture(context.cmd, data.sourceTexture, new Vector4(1, 1, 0, 0), data.material, data.pass);
    }

    private void UpdateGodRaysSettings()
    {
        if (godRaysMaterial == null) return;

        if (godRaysSettings.DownSampling == GodRaysFeature.GodRaysSettings.DownSample.off)
        {
            Shader.EnableKeyword(fboOptimizationGlobalKeyword);
            blurMaterial.EnableKeyword(fboOptimizationForSecondPassGlobalKeyword);
        }
        else if (blurSettings.gaussSamples != 0)
        {
            Shader.EnableKeyword(fboOptimizationGlobalKeyword);
            blurMaterial.DisableKeyword(fboOptimizationForSecondPassGlobalKeyword);
        }
        else
        {
            Shader.DisableKeyword(fboOptimizationGlobalKeyword);
            blurMaterial.DisableKeyword(fboOptimizationForSecondPassGlobalKeyword);
        }

        // Update values god rays material
        // -------------------------------
        godRaysMaterial.SetInt(sampleCountId, godRaysSettings.sampleCount);
        godRaysMaterial.SetFloat(densityId, godRaysSettings.A);
        godRaysMaterial.SetFloat(weightId, godRaysSettings.B);
        godRaysMaterial.SetFloat(decayId, godRaysSettings.C);
        godRaysMaterial.SetFloat(exposureId, godRaysSettings.D);
        godRaysMaterial.SetColor(godRayColorId, godRaysSettings.godRayColor);
        godRaysMaterial.SetFloat(maxDistanceId, godRaysSettings.MaxDistance);
        godRaysMaterial.SetFloat(jitterVolumetricId, godRaysSettings.JitterVolumetric);

        if (blurMaterial == null) return;

        // Update values blur material
        // ---------------------------
        blurMaterial.SetInt(gaussSamplesId, Mathf.Max(blurSettings.gaussSamples - 1, 0));
        blurMaterial.SetFloat(gaussAmountId, blurSettings.gaussAmount);
    }
}