using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;

public class ViewMatrixToShader : MonoBehaviour
{
    private Camera thisCamera;
    // Start is called before the first frame update

    void Awake()
    {
    }
    void Start()
    {
    }
    
    void OnEnable()
    {
        thisCamera = GetComponent<Camera>();
        RenderPipelineManager.beginCameraRendering += PreRender;
    }

    void OnDisable()
    {
        RenderPipelineManager.beginCameraRendering -= PreRender;
    }

    // Update is called once per frame
    void Update()
    {
    }
    
    void PreRender(ScriptableRenderContext context, Camera viewer)
    {
        Matrix4x4 matrix = thisCamera.cameraToWorldMatrix;
        Shader.SetGlobalMatrix("_CameraViewToWorld", matrix);
    }
}
