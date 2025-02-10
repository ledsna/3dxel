
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using Quaternion = UnityEngine.Quaternion;
using Vector3 = UnityEngine.Vector3;

namespace Ledsna
{
    public class PixelPerfectCamera : MonoBehaviour
    {
        [SerializeField] public Camera mainCamera;

        [SerializeField] private RawImage orthographicTexture;

        private Vector3 currentVelocity;

        [SerializeField] float cameraSmoothTime = 7;


        
        [Header("Zoom settings")] [SerializeField]
        float zoomSpeed = 5000f; // Speed of zoom

        [SerializeField] float minZoom = 1f; // Minimum zoom level
        [SerializeField] float maxZoom = 20f; // Maximum zoom level
        [SerializeField] float zoomSmoothness = 10f; // Smoothness of the zoom transition
        private float targetZoom;
        private float zoomLerpRate;
        private float zoom = 1;
        private InputAction zoomAction;
        private InputAction unzoomAction;
        
        private RectTransform orthographicRectTransform;
        

        
        [Header("Blit to Viewport")] private Vector3 offsetWS = Vector3.zero;
        private float pixelW, pixelH;

        private Vector3 ToWorldSpace(Vector3 vector)
        {
            return transform.TransformVector(vector);
        }

        private Vector3 ToScreenSpace(Vector3 vector)
        {
            return transform.InverseTransformVector(vector);
        }
        
        private void Awake()
        {
            orthographicRectTransform = orthographicTexture.GetComponent<RectTransform>();

            pixelW = 1f / mainCamera.scaledPixelWidth;
            pixelH = 1f / mainCamera.scaledPixelHeight;

            orthographicTexture.uvRect = new Rect(0.5f * pixelW + pixelW, 0.5f * pixelH + pixelH,
                1f - pixelW, 1f - pixelH);
            
            zoomAction = InputSystem.actions.FindAction("Zoom");
            unzoomAction = InputSystem.actions.FindAction("Unzoom");
        }

        private void SnapToPixelGrid()
        {
            var pixelsPerUnit = mainCamera.scaledPixelHeight / mainCamera.orthographicSize / 2 * 10;

            var snappedPositionWs = GetSnappedPositionWs(transform.position, offsetWS, pixelsPerUnit);
            offsetWS += transform.position - snappedPositionWs;
            transform.position = snappedPositionWs;

            var uvRect = orthographicTexture.uvRect;
            uvRect.x = (0.5f + ToScreenSpace(offsetWS).x * pixelsPerUnit) * pixelW;
            uvRect.y = (0.5f + ToScreenSpace(offsetWS).y * pixelsPerUnit) * pixelH;
            orthographicTexture.uvRect = uvRect;
        }

        private Vector3 GetSnappedPositionWs(Vector3 positionWs, Vector3 offsetWs, float ppu)
        {
            var posSs = ToScreenSpace(positionWs) + ToScreenSpace(offsetWs);
            var snappedPosSs = new Vector3(Mathf.Round(posSs.x * ppu),
                                   Mathf.Round(posSs.y * ppu),
                                   ToScreenSpace(positionWs).z * ppu)
                               / ppu;

            return ToWorldSpace(snappedPosSs);
        }
        
        private void OnEnable()
        {
            zoomAction.performed += HandleZoom;
            unzoomAction.performed += HandleZoom;
        }

        private void OnDisable()
        {
            zoomAction.performed -= HandleZoom;
            unzoomAction.performed -= HandleZoom;
        }
        
        private void Zoom(float target_zoom)
        {
            orthographicRectTransform.localScale = new Vector3(target_zoom, target_zoom, target_zoom);
        }

        private void HandleZoom(InputAction.CallbackContext context)
        {
            var deltaScroll = context.ReadValue<float>();   
            if (deltaScroll == 0) return;
            targetZoom += deltaScroll * zoomSpeed; // Calculate target zoom level based on input
            targetZoom = Mathf.Clamp(targetZoom, minZoom, maxZoom); // Clamp target zoom to min/max bounds
            zoomLerpRate = 1f - Mathf.Pow(1f - zoomSmoothness * Time.deltaTime, 3);
            Zoom(Mathf.Lerp(zoom, targetZoom, zoomLerpRate));
            zoom = targetZoom;
        }
        
        
        
        void LateUpdate()
        {
            SnapToPixelGrid();
        }
    }
}