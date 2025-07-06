using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Ledsna
{
	public class PlayerInputManager : MonoBehaviour {
		public static PlayerInputManager instance;

		public PlayerManager player;
		PlayerControls playerControls;
		
		[Header("PLAYER MOVEMENT INPUT")]
		[SerializeField] Vector2 movementInput;
		public float verticalInput;
		public float horizontalInput;
		public float moveAmount;

		[Header("PLAYER ACTION INPUT")] 
		[SerializeField] bool dodgeInput = false;
		[SerializeField] bool sprintInput = false;
		[SerializeField] bool jumpInput = false;
		
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

				// layerControls.PlayerMovement.Movement.performed += i => movementInput = i.ReadValue<Vector2>();
				playerControls.PlayerAction.Dodge.performed += i => dodgeInput = true;
				playerControls.PlayerAction.Jump.performed += i => jumpInput = true;
				
				playerControls.PlayerAction.Sprint.performed += i => sprintInput = true;
				playerControls.PlayerAction.Sprint.canceled += i => sprintInput = false;
				
				playerControls.CameraMovement.Movement.performed += i => cameraMovementInput = i.ReadValue<Vector2>();
				playerControls.CameraMovement.Up.performed += i => { moveUp = true; };

			}

			playerControls.Enable();
		}

		private void OnDestroy() 
		{
			SceneManager.activeSceneChanged -= OnSceneChange;
		}

		private void OnApplicationFocus(bool focus)
		{
			if (!enabled)
				return;
			
			if (playerControls == null)
				return;
			if (!focus)
			{
				playerControls.Disable();
				return;
			}
			playerControls.Enable();
		}
		
		private void Update() {
			HandleALlInputs();
			horizontalInput = movementInput.x;
			verticalInput = movementInput.y;
			
			moveUp = playerControls.CameraMovement.Up.IsPressed();
			moveDown = playerControls.CameraMovement.Down.IsPressed();
		}
		
		private void HandleALlInputs()
		{
			HandleMovementInput();
			HandleDodgeInput();
			HandleSprintingInput();
			HandleJumpInput();
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
			
			player?.playerAnimatorManager.UpdateAnimatorMovementParameters(0, moveAmount, player.playerNetworkManager.isSprinting.Value);
		}

		private void HandleDodgeInput()
		{
			if (!dodgeInput)
				return;
			
			dodgeInput = false;
			
			// FUTURE NOTE: RETURN (DO NOTHING) IF MENU OR UI WINDOW IS OPEN, DO NOTHING
			
			// PERFORM A DODGE
			// player.playerLocomotionManager.AttemptToPerformDodge();
		}

		private void HandleSprintingInput()
		{
			if (sprintInput)
			{
				player.playerLocomotionManager.HandleSprinting();
			}
			else
			{
				player.playerNetworkManager.isSprinting.Value = false;
			}
		}

		private void HandleJumpInput()
		{
			if (!jumpInput)
				return;
			jumpInput = false;

			player.playerLocomotionManager.AttemptToPerformJump();
		}
	}
}