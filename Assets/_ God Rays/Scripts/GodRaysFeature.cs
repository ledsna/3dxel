using System;
using NaughtyAttributes;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

#if UNITY_EDITOR
using UnityEditor;
#endif


public class GodRaysFeature : ScriptableRendererFeature
{
    [Serializable]
    public class GodRaysSettings
    {
        [InfoBox("This is default settings of God Rays effect. " +
                 "For changing params you need create Global Volume and add God Rays Volume Component")]
        [Min(0)]
        public float Intensity = 1;

        [Min(0)] public float Scattering = 0.5f;
        [Min(0)] public float MaxDistance = 100f;
        [Min(0)] public float JitterVolumetric = 100;
        public Color GodRayColor = Color.white;
    }

    [Serializable]
    public class BlurSettings
    {
        [InfoBox("This is default settings of Bilaterial Blur. " +
                 "For changing params you need create Global Volume and add God Rays Volume Component")]
        // MAX VALUE = 7. You can't set values higher than 8
        [Range(0, 8)]
        public int GaussSamples = 4;

        [Min(0)] public float GaussAmount = 0.5f;
    }

    // God Rays
    // --------
    [SerializeField] private GodRaysSettings defaultGodRaysSettings;
    [SerializeField] [Required] private Shader godRaysShader;
    private SampleCountEnum lastSampleCount;
    private Material godRaysMaterial;
    private GodRaysPass godRaysPass;

    [Space(10)]

    // Blur Settings
    // -------------
    [SerializeField] 
    private BlurSettings defaultBlurSettings;
    [SerializeField] [Required] private Shader blurShader;
    private Material blurMaterial;

    [Space(10)]

    // General 
    // -------
    [Header("General")]
    [SerializeField] private bool renderInScene = false;
    [SerializeField] [Required] private ShaderVariantCollection svc;
    [SerializeField] private SampleCountEnum sampleCount = SampleCountEnum._64;

    [Header("DEBUG ONLY")] [SerializeField] [InfoBox("Temporary field for debugging", EInfoBoxType.Warning)]
    private Light mainLight;
    
#if UNITY_EDITOR
    private bool isInitialized = false;
#endif
    
    public override void Create()
    {
#if UNITY_EDITOR
        if (!TryLoadAll())
        {
            isInitialized = false;
            Debug.LogError("Can't load all resources for God Rays Feature");
            return;
        }
        else
            isInitialized = true;
#endif
        
        lastSampleCount = sampleCount;
        godRaysMaterial = new Material(godRaysShader);
        blurMaterial = new Material(blurShader);
        godRaysPass = new GodRaysPass(godRaysMaterial, defaultGodRaysSettings, blurMaterial, defaultBlurSettings);
        godRaysPass.renderPassEvent = RenderPassEvent.AfterRenderingSkybox;
        godRaysMaterial.EnableKeyword("ITERATIONS_" + (int)sampleCount);
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (godRaysPass == null)
            return;
        
        if (!renderInScene && renderingData.cameraData.cameraType != CameraType.Game)
            return;

        // Check if Main Light exists and is active
        var mainLightIndex = renderingData.lightData.mainLightIndex;
        if (mainLightIndex == -1) // -1 means no main light
            return;

        var mainLight = renderingData.lightData.visibleLights[mainLightIndex];
        if (mainLight.light == null || !mainLight.light.enabled)
            return;

        renderer.EnqueuePass(godRaysPass);
    }

    protected override void Dispose(bool disposing)
    {
        if (Application.isPlaying)
            Destroy(godRaysMaterial);
        else
            DestroyImmediate(godRaysMaterial);
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (!isInitialized || lastSampleCount == sampleCount)
            return;

        var oldKeyword = "ITERATIONS_" + (int)lastSampleCount;
        var newKeyword = "ITERATIONS_" + (int)sampleCount;

        var oldVariant =
            new ShaderVariantCollection.ShaderVariant(godRaysShader, PassType.Normal, oldKeyword);
        if (svc.Contains(oldVariant))
            svc.Remove(oldVariant);

        var newVariant =
            new ShaderVariantCollection.ShaderVariant(godRaysShader, PassType.Normal, newKeyword);
        if (!svc.Contains(oldVariant))
            svc.Add(newVariant);

        godRaysMaterial.DisableKeyword(oldKeyword);
        godRaysMaterial.EnableKeyword(newKeyword);

        lastSampleCount = sampleCount;
    }

    private bool TryLoadAll()
    {
        if (svc == null)
        {
            var path = "Assets/_ God Rays/GodRaysSVC.shadervariants";
            svc = AssetDatabase.LoadAssetAtPath<ShaderVariantCollection>(path);
            if (svc == null)
            {
                Debug.LogWarning(
                    "Can't find Shader Variant Collection for God Rays Feature. You should manually set it");
                return false;
            }
        }

        if (godRaysShader == null)
        {
            godRaysShader = Shader.Find("Ledsna/GodRays");
            if (godRaysShader == null)
            {
                Debug.LogWarning(
                    "Can't find God Rays.shader for God Rays Feature. You should manually set it");
                return false;
            }
        }

        if (blurShader == null)
        {
            blurShader = Shader.Find("Ledsna/BilaterialBlur");
            if (blurShader == null)
            {
                Debug.LogWarning(
                    "Can't find Bilaterial Blur.shader for God Rays Feature. You should manually set it");
                return false;
            }
        }

        EditorUtility.SetDirty(this);
        return true;
    }
#endif
}