using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using Quaternion = UnityEngine.Quaternion;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;

namespace Ledsna
{
	public class PixelPerfectCamera : MonoBehaviour {
		public static PixelPerfectCamera instance;

		public PlayerManager player;

		[SerializeField] public Camera mainCamera;
		
		[SerializeField] private RawImage orthographicTexture;

		private Vector3 originWS;

		// private float cameraSmoothSpeed = 1;
		// [SerializeField] float leftAndRightRotationSpeed = 220;
		// [SerializeField] float upAndDownRotationSpeed = 220;
		// [SerializeField] float minimumPivot = -30;
		// [SerializeField] float maximumPivor = 60;

		private Vector3 currentVelocity;

		[SerializeField] float cameraSmoothTime = 7;
		// [SerializeField] float leftAndRightLookAngle;
		// [SerializeField] float upAndDownLookAngle;

		[Header("Untargeted Settings")] 
		[SerializeField] float cameraSpeed = 25;

		[Header("Rotation Settings")] 
		// [SerializeField] float angleIncrement = 45f;

		[SerializeField] float targetAngleY = 45f;
		[SerializeField] float targetAngleX = 30f;
		
		[SerializeField] float mouseSensitivity = 2f;
		[SerializeField] float rotationSpeed = 5f;

		[Header("Zoom settings")] 
		[SerializeField] float zoomSpeed = 5000f; // Speed of zoom

		[SerializeField] float minZoom = 1f; // Minimum zoom level
		[SerializeField] float maxZoom = 5f; // Maximum zoom level
		[SerializeField] float zoomSmoothness = 10f; // Smoothness of the zoom transition
		private float targetZoom;
		private float zoomLerpRate;
		private float zoom = 1;
		private RectTransform orthographicRectTransform;

		[Header("Blit to Viewport")] 
		private Vector3 offsetWS = Vector3.zero;
		private float pixelW, pixelH;

		private Vector3 ToWorldSpace(Vector3 vector) {
			return transform.TransformVector(vector);
		}

		private Vector3 ToScreenSpace(Vector3 vector) {
			return transform.InverseTransformVector(vector);
		}

		private void Setup()
		{
			orthographicRectTransform = orthographicTexture.GetComponent<RectTransform>();

			pixelW = 1f / mainCamera.scaledPixelWidth;
			pixelH = 1f / mainCamera.scaledPixelHeight;

			orthographicTexture.uvRect = new Rect(0.5f * pixelW + pixelW, 0.5f * pixelH + pixelH,
				1f - pixelW, 1f - pixelH);
		}
		
		private void SnapToPixelGrid()
		{
			var pixelsPerUnit = mainCamera.scaledPixelHeight / mainCamera.orthographicSize * 0.5f;// * (mainCamera.farClipPlane / 100);
			
			var snappedPositionWs = GetSnappedPositionWs(transform.position, offsetWS, pixelsPerUnit);
			offsetWS += transform.position - snappedPositionWs;
			transform.position = snappedPositionWs;
			
			var uvRect = orthographicTexture.uvRect;

			uvRect.x = (0.5f + ToScreenSpace(offsetWS).x * pixelsPerUnit) * pixelW;
			uvRect.y = (0.5f + ToScreenSpace(offsetWS).y * pixelsPerUnit) * pixelH;

			orthographicTexture.uvRect = uvRect;
		}

		private Vector3 GetSnappedPositionWs(Vector3 positionWs, Vector3 offsetWs, float ppu) {
			var posSs = ToScreenSpace(positionWs) + ToScreenSpace(offsetWs);
			var snappedPosSs = new Vector3(Mathf.Round(posSs.x * ppu),
												  Mathf.Round(posSs.y * ppu),
												  ToScreenSpace(positionWs).z * ppu)
											      / ppu;

			return ToWorldSpace(snappedPosSs);
		}


		private void HandleRotation()
		{
			var mouseX = Input.GetAxis("Mouse X");
			var mouseY = Input.GetAxis("Mouse Y");
			
			if (Input.GetMouseButton(0))
			{
				targetAngleX += mouseY * mouseSensitivity;
				targetAngleY += mouseX * mouseSensitivity;
			}

			// else {
			// 	targetAngleY = Mathf.Round(targetAngleY / angleIncrement);
			// 	targetAngleY *= angleIncrement;
			// }

			targetAngleY = (targetAngleY % 360 + 360) % 360;
			targetAngleX = (targetAngleX % 360 + 360) % 360;
			
			if (player is not null)
				targetAngleX = Mathf.Max(Mathf.Min(targetAngleX, 40), 15);
			else
			{
				if (targetAngleX > 180 && targetAngleX < 270)
					targetAngleX = 270;
				else if (targetAngleX < 180 && targetAngleX > 90)
					targetAngleX = 90;
			}
			
			var currentAngleY = Mathf.LerpAngle(transform.eulerAngles.y, targetAngleY,
					 					rotationSpeed * Time.deltaTime);
			var currentAngleX = Mathf.LerpAngle(transform.eulerAngles.x, targetAngleX, 
										rotationSpeed * Time.deltaTime);
			
			// transform.rotation = Quaternion.Euler(transform.eulerAngles.x, currentAngleY, 0);
			transform.rotation = Quaternion.Euler(currentAngleX, currentAngleY, 0);
		}

		private void Zoom(float target_zoom) {
			orthographicRectTransform.localScale = new Vector3(target_zoom, target_zoom, target_zoom);
		}

		private void HandleZoom() {
			var scroll = Input.GetAxis("Mouse ScrollWheel"); // Get mouse wheel input
			if (scroll == 0) return;
			targetZoom += scroll * zoomSpeed; // Calculate target zoom level based on input
			targetZoom = Mathf.Clamp(targetZoom, minZoom, maxZoom); // Clamp target zoom to min/max bounds
			zoomLerpRate = 1f - Mathf.Pow(1f - zoomSmoothness * Time.deltaTime, 3);
			// mainCamera.orthographicSize = Mathf.Lerp(mainCamera.orthographicSize, targetZoom, zoomLerpRate);
			Zoom(Mathf.Lerp(zoom, targetZoom, zoomLerpRate));
			zoom = targetZoom;
		}

		private void HandleFollowTarget()
		{
			// Locked camera
			if (player is not null)
			{
				Vector3 targetCameraPosition = Vector3.SmoothDamp
				(transform.position + offsetWS, 
					player.transform.position + new Vector3(0, 1.75f, 0),
					ref currentVelocity, 
					cameraSmoothTime * Time.deltaTime);
				
				transform.position = targetCameraPosition - offsetWS;
				return;
			}
			
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

		// void Update() {
		// 	ReflectionProbe[] reflectionProbes = FindObjectsByType<ReflectionProbe>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
		//
		// 	// Loop through each reflection probe
		// 	foreach (ReflectionProbe probe in reflectionProbes)
		// 	{
		// 		if (probe.texture is not null) // && probe.mode == UnityEngine.Rendering.ReflectionProbeMode.Realtime)
		// 		{
		// 			// Set the filter mode of each reflection probe's cubemap to point filtering
		// 			probe.texture.filterMode = FilterMode.Point;
		// 		}
		// 	}
		// }

		public void LateUpdate()
		{
			if (player is null)
			{
				HandleAllCameraActions();
				return;
			}			
		}

		public void HandleAllCameraActions() {
			HandleRotation();
			HandleZoom();
			HandleFollowTarget();
			if (mainCamera.orthographic)
			{Debug.Log("o");SnapToPixelGrid();}
		}
	}
}
