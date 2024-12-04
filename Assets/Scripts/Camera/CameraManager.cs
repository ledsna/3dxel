using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Quaternion = UnityEngine.Quaternion;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;

namespace SG 
{
	public class CameraManager : MonoBehaviour {
		public static CameraManager instance;

		public PlayerManager player;

		[SerializeField]
		private List<Transform> snapObjects = new List<Transform>();
		private List<Vector3> offsets = new List<Vector3>();
		private List<Vector3> origins = new List<Vector3>();

		[SerializeField] Camera mainCamera;
		[SerializeField] RawImage screenTexture;

		[SerializeField] private DearImGUIWrapper dearImGUIWrapper;

		// private float cameraSmoothSpeed = 1;
		// [SerializeField] float leftAndRightRotationSpeed = 220;
		// [SerializeField] float upAndDownRotationSpeed = 220;
		// [SerializeField] float minimumPivot = -30;
		// [SerializeField] float maximumPivor = 60;

		private Vector3 currentVelocity;

		[SerializeField] float cameraSmoothTime = 7;
		[SerializeField] float leftAndRightLookAngle;
		[SerializeField] float upAndDownLookAngle;

		[Header("Untargeted Settings")] 
		[SerializeField] float cameraSpeed = 10;

		[Header("Rotation Settings")] 
		[SerializeField] float angleIncrement = 45f;

		[SerializeField] float targetAngle = 45f;
		[SerializeField] float mouseSensitivity = 8f;
		[SerializeField] float rotationSpeed = 5f;
		private float currentAngle;
		UnityEngine.Quaternion lastRotation;

		[Header("Zoom settings")] 
		[SerializeField] float zoomSpeed = 5000f; // Speed of zoom

		[SerializeField] float minZoom = 1f; // Minimum zoom level
		[SerializeField] float maxZoom = 20f; // Maximum zoom level
		[SerializeField] float zoomSmoothness = 10f; // Smoothness of the zoom transition
		private float targetZoom;
		private float zoomLerpRate;
		private float zoom = 1;

		[Header("Blit to Viewport")] 
		private Vector3 offsetWS = Vector3.zero;
		private float pixelW, pixelH;

		// private Vector3 localForwardVector = new Vector3();

		private Vector3 ToWorldSpace(Vector3 vector) {
			return transform.TransformVector(vector);
		}

		private Vector3 ToScreenSpace(Vector3 vector) {
			return transform.InverseTransformVector(vector);
		}

		void Setup() {
			// Fraction of pixel size to screen size
			pixelW = 1f / mainCamera.scaledPixelWidth;
			pixelH = 1f / mainCamera.scaledPixelHeight;
			// Offsetting vertical and horizontal positions by 1 pixel
			//  and shrinking the screen size by 2 pixels from each side
			// mainCamera.pixelRect = new Rect(1, 1, mainCamera.pixelWidth - 1, mainCamera.pixelHeight - 1);
			if (screenTexture != null)
				screenTexture.uvRect = new Rect(0.5f + pixelW, 0.5f + pixelH, 1f - pixelW, 1f - pixelH);

			lastRotation = transform.rotation;

			offsets.Add(Vector3.zero);
		}

		private void Snap()
		{
			float ppu = mainCamera.scaledPixelHeight / mainCamera.orthographicSize / 2;

			Vector3 snappedPositionWS = GetSnappedPositionWS(transform.position, offsetWS);
			offsetWS += transform.position - snappedPositionWS;
			transform.position = snappedPositionWS;

			// snappedPositionWS = GetSnappedPositionWS(player.transform.position, offsets[0]);
			// offsets[0] += player.transform.position - snappedPositionWS;
			// player.transform.position = snappedPositionWS;

			Rect uvRect = screenTexture.uvRect;
			// Offset the Viewport by 1 - offset pixels in both dimensions
			uvRect.x = (0.5f + ToScreenSpace(offsetWS).x * ppu) * pixelW;
			uvRect.y = (0.5f + ToScreenSpace(offsetWS).y * ppu) * pixelH;

			// Blit to Viewport
			screenTexture.uvRect = uvRect;
		}

		public Vector3 GetSnappedPositionWS(Vector3 position_ws, Vector3 offset_ws) {
			float ppu = mainCamera.scaledPixelHeight / mainCamera.orthographicSize / 2;

			Vector3 posSS = ToScreenSpace(position_ws) + ToScreenSpace(offset_ws);
			Vector3 snappedPosSS = new Vector3(Mathf.Round(posSS.x * ppu),
											Mathf.Round(posSS.y * ppu),
											ToScreenSpace(position_ws).z * ppu)
											/ ppu;

			// 											

			return ToWorldSpace(snappedPosSS);
		}


		void HandleRotation() {
			// Application.targetFrameRate = -1; // Uncapped
			float mouseX = Input.GetAxis("Mouse X");
			// float mouseY = Input.GetAxis("Mouse Y");

			if (Input.GetMouseButton(0))
				// While holding LMB, the Camera will follow the cursor
				targetAngle += mouseX * mouseSensitivity;
			else {
				// Snap to the closest whole increment angle
				targetAngle = Mathf.Round(targetAngle / angleIncrement);
				targetAngle *= angleIncrement;
			}

			targetAngle = (targetAngle + 360) % 360;
			currentAngle = Mathf.LerpAngle(transform.eulerAngles.y, targetAngle,
										rotationSpeed * Time.deltaTime);
			// vertAngle = Mathf.LerpAngle(transform.eulerAngles.x, 30, rotationSpeed / 10 * Time.deltaTime);
			transform.rotation = Quaternion.Euler(transform.eulerAngles.x, currentAngle, 0);
			
			lastRotation = transform.rotation;
			// offsetWS = Vector3.zero;

			// offsets[0] = Vector3.zero;
		}

		private void Zoom(float target_zoom) {
			screenTexture.GetComponent<RectTransform>().localScale = new Vector3(target_zoom, target_zoom, target_zoom);
		}

		void HandleZoom() {
			float scroll = Input.GetAxis("Mouse ScrollWheel"); // Get mouse wheel input
			if (scroll != 0) {
				targetZoom += scroll * zoomSpeed; // Calculate target zoom level based on input
				targetZoom = Mathf.Clamp(targetZoom, minZoom, maxZoom); // Clamp target zoom to min/max bounds
				zoomLerpRate = 1f - Mathf.Pow(1f - zoomSmoothness * Time.deltaTime, 3);
				// mainCamera.orthographicSize = Mathf.Lerp(mainCamera.orthographicSize, targetZoom, zoomLerpRate);
				Zoom(Mathf.Lerp(zoom, targetZoom, zoomLerpRate));
				zoom = targetZoom;
			}
		}

		private void HandleFollowTarget()
		{
			// Unlocked camera
			if (PlayerInputManager.instance.moveUp) {
				transform.position += Vector3.up * (Time.deltaTime * cameraSpeed);
			}
			else if (PlayerInputManager.instance.moveDown) {
				transform.position -= Vector3.up * (Time.deltaTime * cameraSpeed);
			}
		
			if (PlayerInputManager.instance.cameraMovementInput != Vector2.zero) {
				// Normalize movement to ensure consistent speed
				Vector2 directionSS = PlayerInputManager.instance.cameraMovementInput;
				// localForwardVector.x = transform.up.x;
				// localForwardVector.z = transform.up.z;
				Vector3 directionWS = transform.right * directionSS.x + transform.up * directionSS.y;
				transform.position += (Time.deltaTime * cameraSpeed) * directionWS;

			}
			else if (player is not null)
			{
				Vector3 targetCameraPosition = Vector3.SmoothDamp
					(transform.position + offsetWS, 
						player.transform.position,
						ref currentVelocity, 
						cameraSmoothTime * Time.deltaTime);
				
				transform.position = targetCameraPosition - offsetWS;
			}
		}

		private void Start() {
			// DontDestroyOnLoad(gameObject);
			Setup();
		}

		private void Awake() {
			if (instance != null)
				Destroy(gameObject);
			instance = this;
		}

		void Update() {
			ReflectionProbe[] reflectionProbes = FindObjectsOfType<ReflectionProbe>();

			// Loop through each reflection probe
			foreach (ReflectionProbe probe in reflectionProbes)
			{
				if (probe.texture != null) // && probe.mode == UnityEngine.Rendering.ReflectionProbeMode.Realtime)
				{
					// Set the filter mode of each reflection probe's cubemap to point filtering
					probe.texture.filterMode = FilterMode.Point;
				}
			}
		}

		public void HandleAllCameraActions() {
			if (dearImGUIWrapper != null && !dearImGUIWrapper.MouseInsideImguiWindow) {
				HandleRotation();
				HandleZoom();
				HandleFollowTarget();
			}
			Snap();
		}

	}
}
