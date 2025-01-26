using UnityEngine;
using UnityEngine.Rendering;

public class eblo : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private Camera cam;
    RenderTexture rt;
    void Start()
    {
        cam = GetComponent<Camera>();
        rt = cam.targetTexture;

    }

    // Update is called once per frame
    void Update()
    {
        cam.clearFlags = CameraClearFlags.Nothing;
    }
}
