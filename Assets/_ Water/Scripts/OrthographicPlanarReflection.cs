using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Serialization;

namespace Reflections
{
    [ExecuteInEditMode]
    [RequireComponent(typeof(Camera))]
    public class PlanarReflector : MonoBehaviour
    {
        [SerializeField] 
        [Range(1, 4)] int textureID = 1;

        [Space(10)] [SerializeField] [Range(0.01f, 1.0f)]
        private float reflectionsQuality = 1f;

        [Space(10)] 
        [SerializeField] bool renderInEditor;

        [Space(10)]
        private Camera reflector;
        private RenderTexture renderTexture;

        private void Start()
        {
            reflector = GetComponent<Camera>();
            reflector.cameraType = CameraType.Reflection;
            reflector.targetTexture = renderTexture;
            reflector.enabled = false;
            RenderPipelineManager.beginCameraRendering += PreRender;
        }

        private void OnDestroy()
        {
            RenderPipelineManager.beginCameraRendering -= PreRender;
            if (renderTexture) renderTexture.Release();
        }

        private void PreRender(ScriptableRenderContext context, Camera viewer)
        {
            // if (viewer.CompareTag("NoReflections")) return;
            if (viewer.CompareTag("Untagged") && viewer.cameraType != CameraType.SceneView) return;
            if (viewer.cameraType is CameraType.Reflection or CameraType.Preview) return;
            if (!renderInEditor && viewer.cameraType == CameraType.SceneView) return;

            var planeTransform = transform.parent;
            UpdateSettings(viewer);
            UpdatePosition(viewer.transform, planeTransform);
            UpdateObliqueProjection(planeTransform);

            var renderRequest = new UniversalRenderPipeline.SingleCameraRequest();
            renderRequest.destination = renderTexture;
            RenderPipeline.SubmitRenderRequest(reflector, renderRequest);
        }

        private void UpdateSettings(Camera viewer)
        {
            reflector.orthographicSize = viewer.orthographicSize;
            var width = (int)(viewer.scaledPixelWidth * reflectionsQuality);
            var height = (int)(viewer.scaledPixelHeight * reflectionsQuality);

            // if (viewer.cameraType == CameraType.SceneView)
            // {
            //     width /= 10;
            //     height /= 10;
            // }

            if (renderTexture && renderTexture.width == width && renderTexture.height == height) return;
            if (renderTexture) renderTexture.Release();
            
            Debug.Log("Planar Reflections renderTexture was updated.");

            renderTexture = new RenderTexture(width, height, 24, RenderTextureFormat.ARGBFloat)
            {
                filterMode = FilterMode.Point
            };

            reflector.targetTexture = renderTexture;
            // reflector.orthographicSize = viewer.orthographicSize;
            reflector.clearFlags = viewer.clearFlags;
            reflector.backgroundColor = viewer.backgroundColor;
            reflector.targetTexture.SetGlobalShaderProperty("_Reflection" + textureID);
        }

        private void UpdatePosition(Transform viewer, Transform plane)
        {
            var normal = plane.up;
            var viewerPos = viewer.position;
            // Flip viewer's position across the \offset\ reflective plane
            var proj = normal * Vector3.Dot(normal, viewerPos - (plane.position));
            transform.position = viewerPos - 2 * proj;

            // Reflect the viewer's rotation across the normal to the reflective plane
            var probeForward = Vector3.Reflect(viewer.forward, normal);
            var probeUp = Vector3.Reflect(viewer.up, normal);
            transform.LookAt(transform.position + probeForward, probeUp);
        }

        private void UpdateObliqueProjection(Transform plane)
        {
            var normal = plane.up;
            // Replace the Near Clip plane with the parent plane coordinates
            var viewMatrix = reflector.worldToCameraMatrix;
            var viewPosition = viewMatrix.MultiplyPoint(plane.position);
            var viewNormal = viewMatrix.MultiplyVector(normal).normalized;
            var clipPlane = new Vector4(viewNormal.x, viewNormal.y, viewNormal.z,
                -Vector3.Dot(viewPosition, viewNormal));
            reflector.projectionMatrix = reflector.CalculateObliqueMatrix(clipPlane);
        }
    }
}

// 640
//
// 2**7 * 5 
// 5 * 3**2 * 2**3