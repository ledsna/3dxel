using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Serialization;

[ExecuteInEditMode]
public class PlanarReflector : MonoBehaviour {
    [SerializeField] [Range(1, 4)] int textureID = 1;
    [Space(10)]
    
    [Range(0.01f, 1.0f)] float reflectionsQuality = 1f;
    [Space(10)] 
    
    [SerializeField] bool renderInEditor;

    [Space(10)] 
    private float unitsPerPixel;
    private Camera reflector;
    private Skybox reflectorSkybox;
    private RenderTexture renderTexture;

    private void OnEnable ()
    {
        reflector = GetComponent<Camera>();
        reflector.cameraType = CameraType.Reflection;
        reflector.targetTexture = renderTexture;
        RenderPipelineManager.beginCameraRendering += PreRender;
    }

    private void OnDisable() {
        RenderPipelineManager.beginCameraRendering -= PreRender;
        if (renderTexture) renderTexture.Release();
    }

    private void PreRender(ScriptableRenderContext context, Camera viewer)
    {
        // if (viewer.CompareTag("NoReflections")) return;
        if (viewer.CompareTag("Untagged") && viewer.cameraType != CameraType.SceneView) return;
        if (viewer.cameraType is CameraType.Reflection or CameraType.Preview) return;
        if (!renderInEditor && viewer.cameraType == CameraType.SceneView) return;

        Transform parent = transform.parent;
        UpdateSettings(viewer, out float offset);
        CalculateCurrentPosition(viewer.transform, parent, offset);
        CalculateObliqueProjection(parent, offset);

        // var renderRequest = new UniversalRenderPipeline.SingleCameraRequest();
        // renderRequest.destination = renderTexture;
        // RenderPipeline.SubmitRenderRequest(reflector, renderRequest);

        #pragma warning disable CS0618 
        UniversalRenderPipeline.RenderSingleCamera(context, reflector);
        reflector.targetTexture.SetGlobalShaderProperty("_Reflection" + textureID);
    }
    
    private void UpdateSettings(Camera viewer, out float offset) {
        // Calculate offset: > 7e-5
        // For half a pixel to be < 7e-5 in Units,
        //  the entire screen should be at least 1 000 000 / 7 / 2 ~= 71 429 pixels tall.
        // Make sure it's < 0.5 pixels big, so it doesn't create a visible 1 pixel offset.
        reflector.orthographicSize = viewer.orthographicSize;
        offset = 0.49f * reflector.orthographicSize * 2 / viewer.scaledPixelHeight;
        offset = 0;
        int width = (int) (viewer.scaledPixelWidth * reflectionsQuality);
        int height = (int) (viewer.scaledPixelHeight * reflectionsQuality);

        if (viewer.cameraType == CameraType.SceneView) {
            width /= 10;
            height /= 10;
        }
        
        if (renderTexture && renderTexture.width == width && renderTexture.height == height) return;
        if (renderTexture) renderTexture.Release();
        
        renderTexture = new RenderTexture(width, height, 24, RenderTextureFormat.ARGBFloat)
        {
            filterMode = FilterMode.Point // Set the filter mode to Point
        };
        
        reflector.targetTexture = renderTexture;
        // reflector.orthographicSize = viewer.orthographicSize;
        reflector.clearFlags = viewer.clearFlags;
        reflector.backgroundColor = viewer.backgroundColor;
    }
    
    private void CalculateCurrentPosition(Transform viewer, Transform plane, float offset)
    {
        Vector3 normal = plane.up;
        Vector3 viewerPos = viewer.position;
        // Flip viewer's position across the \offset\ reflective plane
        Vector3 proj = normal * Vector3.Dot(normal, viewerPos - (plane.position + offset * normal));
        transform.position = viewerPos - 2 * proj;
        
        // Reflect the viewer's rotation across the normal to the reflective plane
        Vector3 probeForward = Vector3.Reflect(viewer.forward, normal);
        Vector3 probeUp = Vector3.Reflect(viewer.up, normal);
        transform.LookAt(transform.position + probeForward, probeUp);
    }
    
    private void CalculateObliqueProjection (Transform plane, float offset)
    {
        Vector3 normal = plane.up;
        // Replace the Near Clip plane with the \offset\ reflective plane (parent) coordinates.
        Matrix4x4 viewMatrix = reflector.worldToCameraMatrix;
        Vector3 viewPosition = viewMatrix.MultiplyPoint(plane.position + offset * normal);
        Vector3 viewNormal = viewMatrix.MultiplyVector(normal).normalized;
        Vector4 clipPlane = new Vector4(viewNormal.x, viewNormal.y, viewNormal.z, 
            -Vector3.Dot(viewPosition, viewNormal));
        reflector.projectionMatrix = reflector.CalculateObliqueMatrix(clipPlane);
    }
}