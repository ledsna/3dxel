using UnityEngine;
using UnityEngine.Rendering.Universal;
using Unity.Mathematics;
using UnityEngine.Serialization;

public class ScrollingPerlinNoiseCookie : MonoBehaviour
{
    private Light light;
    private UniversalAdditionalLightData lightData;

    [FormerlySerializedAs("noisetex")] [SerializeField] private Texture2D cookieTexture;
    [SerializeField] private Vector2 cookieSize;
    [SerializeField] private Vector2 cookieSpeed;
    [SerializeField] private float scale;
    
    // [SerializeField] private Texture2D nTex;

    void Start()
    {
        light = GetComponent<Light>();
        lightData = GetComponent<UniversalAdditionalLightData>();
        // light.cookie = cookieTexture;
        if (cookieTexture == null) light.cookie = GenerateTilingPerlinNoiseTexture(2048, 2048);
        else light.cookie = cookieTexture;
        // lightData.lightCookieOffset = cookieOffset;
        lightData.lightCookieSize = cookieSize;
    }

    void Update()
    {
        lightData.lightCookieOffset += Time.deltaTime * cookieSpeed;
    }

    Texture2D GenerateTilingPerlinNoiseTexture(int width, int height)
    {
        var noisetex = new Texture2D(width, height);

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                // Calculate tiling Perlin noise coordinates
                float u = (float)x / width;
                float v = (float)y / height;

                float tileableNoise = TileablePerlinNoise(u, v, scale);
                Color color = new Color(tileableNoise, tileableNoise, tileableNoise);

                noisetex.SetPixel(x, y, color);
            }
        }
        // Apply changes to the texture
        noisetex.Apply();
        return noisetex;
    }
    
    float TileablePerlinNoise(float u, float v, float scale)
    {
        // Scale the input coordinates
        u *= scale;
        v *= scale;

        // Calculate periodic Perlin noise using sine and cosine for wrapping
        float noise = 0;
        float frequency = 1.0f;

        // Use a couple of octaves for a more complex pattern
        for (int i = 0; i < 4; i++)
        {
            float uu = Mathf.Cos(2 * Mathf.PI * u) * frequency;
            float vv = Mathf.Cos(2 * Mathf.PI * v) * frequency;
            float ww = Mathf.Sin(2 * Mathf.PI * u) * frequency + Mathf.Sin(2 * Mathf.PI * v) * frequency;

            noise += Mathf.PerlinNoise(uu, vv + ww) / frequency;
            frequency *= 2.0f;
        }

        return Mathf.Clamp01(noise);
    }

    // Optional: Save the texture to a file in the editor
// #if UNITY_EDITOR
//     [ContextMenu("Save Texture to File")]
//     void SaveTextureToFile()
//     {
//         if (cookieTexture == null)
//         {
//             Debug.LogWarning("No texture generated to save.");
//             return;
//         }
//
//         byte[] bytes = cookieTexture.EncodeToPNG();
//         string path = Application.dataPath + "/PerlinNoiseTexture.png";
//         System.IO.File.WriteAllBytes(path, bytes);
//         Debug.Log("Texture saved to " + path);
//         AssetDatabase.Refresh();
//     }
// #endif
}
