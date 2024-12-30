using UnityEngine;
using UnityEngine.Rendering.Universal;

public class CloudsSettings : MonoBehaviour
{
    private Light light;
    private UniversalAdditionalLightData lightData;
    
    [SerializeField] private CustomRenderTexture cloudShadows;

    [SerializeField] private float cookieSteps = -1;
    [SerializeField] private Texture2D noise;
    [SerializeField] private Texture2D details;
    [SerializeField] private Vector2 cookieSize = new(30, 30);
    [SerializeField] private Vector2 noiseSpeed = new(0.2f, 0.2f);
    [SerializeField] private Vector2 detailsSpeed = new(0.33f, 0.5f);

    void OnEnable()
    {
        EnableClouds();
    }

    void OnDisable()
    {
        DisableClouds();
    }

    void EnableClouds()
    {
        light = GetComponent<Light>();
        lightData = GetComponent<UniversalAdditionalLightData>();
        // if (noise == null) light.cookie = GenerateTilingPerlinNoiseTexture(2048, 2048);
        // else 
        light.cookie = cloudShadows;
        // lightData.lightCookieOffset = cookieOffset;        
        UpdateCookie();
    }

    void DisableClouds()
    {
        light.cookie = null;
    }

    void UpdateCookie()
    {
        lightData.lightCookieSize = cookieSize;
        cloudShadows.material.SetFloat("_CookieSteps", cookieSteps);
        cloudShadows.material.SetVector("_NoiseSpeed", noiseSpeed);
        cloudShadows.material.SetVector("_DetailsSpeed", detailsSpeed);
        cloudShadows.material.SetTexture("_Noise", noise);
        cloudShadows.material.SetTexture("_Details", details);
    }
}
