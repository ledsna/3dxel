using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using Quaternion = UnityEngine.Quaternion;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;

namespace Ledsna
{
    public class PixelPerfectCamera : MonoBehaviour
    {
        public static PixelPerfectCamera instance;

        public PlayerManager player;

        [SerializeField] public Camera mainCamera;

        [SerializeField] private RawImage orthographicTexture;

        private Vector3 currentVelocity;

        [SerializeField] float cameraSmoothTime = 7;

        [Header("Untargeted Settings")] [SerializeField]
        float cameraSpeed = 10;

        [Header("Rotation Settings")] [SerializeField]
        float angleIncrement = 45f;

        [SerializeField] float targetAngle = 45f;
        [SerializeField] float mouseSensitivity = 8f;
        [SerializeField] float rotationSpeed = 5f;
        private float currentAngle;

        [Header("Zoom settings")] [SerializeField]
        float zoomSpeed = 5000f; // Speed of zoom

        [SerializeField] float minZoom = 1f; // Minimum zoom level
        [SerializeField] float maxZoom = 20f; // Maximum zoom level
        [SerializeField] float zoomSmoothness = 10f; // Smoothness of the zoom transition
        private float targetZoom;
        private float zoomLerpRate;
        private float zoom = 1;
        private RectTransform orthographicRectTransform;
        private InputAction rotationAction;
        private InputAction zoomAction;
        private InputAction unzoomAction;


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

        private void Setup()
        {
            orthographicRectTransform = orthographicTexture.GetComponent<RectTransform>();

            pixelW = 1f / mainCamera.scaledPixelWidth;
            pixelH = 1f / mainCamera.scaledPixelHeight;

            orthographicTexture.uvRect = new Rect(0.5f * pixelW + pixelW, 0.5f * pixelH + pixelH,
                1f - pixelW, 1f - pixelH);
            
            rotationAction = InputSystem.actions.FindAction("Rotation");
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


        private void HandleRotation(float deltaX)
        {
            if (Input.GetMouseButton(0))
                // While holding LMB, the Camera will follow the cursor
                targetAngle += deltaX * mouseSensitivity;
            else
            {
                // Snap to the closest whole increment angle
                targetAngle = Mathf.Round(targetAngle / angleIncrement);
                targetAngle *= angleIncrement;
            }

            targetAngle = (targetAngle + 360) % 360;
            currentAngle = Mathf.LerpAngle(transform.eulerAngles.y, targetAngle,
                rotationSpeed * Time.deltaTime);
            transform.rotation = Quaternion.Euler(transform.eulerAngles.x, currentAngle, 0);
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

        private void HandleFollowTarget()
        {
            // Unlocked camera
            if (PlayerInputManager.instance.moveUp)
            {
                transform.position += Vector3.up * (Time.deltaTime * cameraSpeed);
            }
            else if (PlayerInputManager.instance.moveDown)
            {
                transform.position -= Vector3.up * (Time.deltaTime * cameraSpeed);
            }

            if (PlayerInputManager.instance.cameraMovementInput != Vector2.zero)
            {
                Vector2 directionSS = PlayerInputManager.instance.cameraMovementInput;
                Vector3 directionWS = transform.right * directionSS.x + transform.up * directionSS.y;
                transform.position += (Time.deltaTime * cameraSpeed) * directionWS;
            }
            else if (player is not null)
            {
                Vector3 targetCameraPosition = Vector3.SmoothDamp
                (transform.position + offsetWS,
                    player.transform.position + new Vector3(0, 1.75f, 0),
                    ref currentVelocity,
                    cameraSmoothTime * Time.deltaTime);

                transform.position = targetCameraPosition - offsetWS;
            }
        }
        
        private void Awake()
        {
            if (instance != null)
                Destroy(gameObject);
            instance = this;
            
            Setup();
        }

        void LateUpdate()
        {
            HandleRotation(rotationAction.ReadValue<float>());

            SnapToPixelGrid();
        }
    }
}