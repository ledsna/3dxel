using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SG 
{
	public class PlayerInputManager : MonoBehaviour {
		public static PlayerInputManager instance;

		public PlayerManager player;
		PlayerControls playerControls;
		[SerializeField] Vector2 movementInput;

		public float verticalInput;
		public float horizontalInput;
		public float moveAmount;

		[Header("PLAYER ACTION INPUT")] [SerializeField]
		private bool dodgeInput = false;
		
		public Vector2 cameraMovementInput;
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

		private void OnSceneChange(Scene oldScene, Scene newScene)
		{
			// IF WE ARE LOADING INTO THE WORLD SCENE, ENABLE CONTROLS
			if (newScene.buildIndex == WorldSaveGameManager.instance.GetWorldSceneIndex())
			{
				instance.enabled = true;
			}
			// DISABLE IN MENU
			else
			{
				instance.enabled = false;
			}
		}

		private void Start() 
		{
			DontDestroyOnLoad(gameObject);

			SceneManager.activeSceneChanged += OnSceneChange;

			instance.enabled = false;
		}

		private void OnEnable() 
		{
			if (playerControls == null) {
				playerControls = new PlayerControls();

				playerControls.PlayerMovement.Movement.performed += i => movementInput = i.ReadValue<Vector2>();
				playerControls.CameraMovement.Movement.performed += i => cameraMovementInput = i.ReadValue<Vector2>();
				playerControls.CameraMovement.Up.performed += i => { moveUp = true; };
				playerControls.PlayerAction.Dodge.performed += i => dodgeInput = true;
			}

			playerControls.Enable();
		}

		private void OnDestroy() 
		{
			SceneManager.activeSceneChanged -= OnSceneChange;
		}

		private void OnApplicationFocus(bool focus)
		{
			if (enabled)
			{
				if (focus)
				{
					playerControls.Enable();
				}
				else
				{
					playerControls.Disable();
				}
			}
		}

		private void HandleMovementInput() 
		{
			verticalInput = movementInput.y;
			horizontalInput = movementInput.x;

			moveAmount = Mathf.Clamp01(Mathf.Abs(verticalInput) + Mathf.Abs(horizontalInput));

			if (moveAmount <= 0.5 && moveAmount > 0)
			{
				moveAmount = 0.5f;
			}
			else if (moveAmount > 0.5 && moveAmount <= 1)
			{
				moveAmount = 1;
			}

			if (player == null)
				return;

			player.playerAnimatorManager.UpdateAnimatorMovementParameters(0, moveAmount);
		}

		private void HandleDodgeInput()
		{
			if (dodgeInput)
			{
				dodgeInput = false;
				
				// FUTURE NOTE: RETURN (DO NOTHING) IF MENU OR UI WINDOW IS OPEN, DO NOTHING
				// PERFORM A DODGE
				
				player.playerLocomotionManager.AttemptToPerformDodge();

			}
		}
		
		private void HandleALlInputs()
		{
			HandleMovementInput();
			HandleDodgeInput();
		}

		private void Update() {
			HandleALlInputs();
			verticalInput = movementInput.y;
			horizontalInput = movementInput.x;
			
			moveUp = playerControls.CameraMovement.Up.IsPressed();
			moveDown = playerControls.CameraMovement.Down.IsPressed();
		}
	}
}