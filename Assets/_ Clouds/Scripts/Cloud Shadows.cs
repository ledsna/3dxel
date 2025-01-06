using System;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

public class CloudsSettings : MonoBehaviour
{
    private Light light;
    private UniversalAdditionalLightData lightData;
    
    [SerializeField] private CustomRenderTexture renderTexture;

    [SerializeField] private float cookieSteps = -1;
    [SerializeField] private Texture2D noise;
    [SerializeField] private Texture2D details;
    [SerializeField] private Vector2 cookieSize = new(30, 30);
    [SerializeField] private Vector2 noiseSpeed = new(0.2f, 0.2f);
    [SerializeField] private Vector2 detailsSpeed = new(0.33f, 0.5f);

    private void OnEnable()
    {
        EnableClouds();
        UpdateCookie();
    }

    private void OnDisable()
    {
        light.cookie = null;
        renderTexture.Release();
    }

    void EnableClouds()
    {
        light = GetComponent<Light>();
        lightData = GetComponent<UniversalAdditionalLightData>();
        light.cookie = renderTexture;
        lightData.lightCookieSize = cookieSize;
    }

    void UpdateCookie()
    {
        // renderTexture.Update();
        renderTexture.material.SetTexture("_Noise", noise);
        renderTexture.material.SetTexture("_Details", details);
        renderTexture.material.SetFloat("_CookieSteps", cookieSteps);
        renderTexture.material.SetVector("_NoiseSpeed", noiseSpeed);
        renderTexture.material.SetVector("_DetailsSpeed", detailsSpeed);
    }
}
