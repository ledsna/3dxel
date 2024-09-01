using System;
using UnityEngine;


public class PlayerInputManager : MonoBehaviour {
	public static PlayerInputManager instance;

	PlayerControls playerControls;

	[Header("MOVEMENT INPUT")] [SerializeField]
	Vector2 movementInput;

	public float verticalInput;
	public float horizontalInput;

	[Header("CAMERA MOVEMET INNPUT")] public Vector2 cameraMovementInput;
	public bool moveUp = false;
	public bool moveDown = false;

	private void Awake() {
		if (instance == null) {
			instance = this;
		}
		else {
			Destroy(gameObject);
		}
	}

	private void OnEnable() {
		if (playerControls is null) {
			playerControls = new PlayerControls();

			playerControls.PlayerMovement.Movement.performed += i => movementInput = i.ReadValue<Vector2>();
			playerControls.CameraMovement.Movement.performed += i => cameraMovementInput = i.ReadValue<Vector2>();
			playerControls.CameraMovement.Up.performed += i => { moveUp = true; };
			
		}

		playerControls.Enable();
	}


	private void Update() {
		verticalInput = movementInput.y;
		horizontalInput = movementInput.x;
		
		moveUp = playerControls.CameraMovement.Up.IsPressed();
		moveDown = playerControls.CameraMovement.Down.IsPressed();
	}
}