using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Vector3 = UnityEngine.Vector3;


namespace SG 
{
    public class PlayerLocomotionManager : CharacterLocomotionManager
    {
        PlayerManager player;
        [HideInInspector] public float verticalMovement;
        [HideInInspector] public float horizontalMovement;
        [HideInInspector] public float moveAmount;

        [Header("MOVEMENT SETTINGS")]
        private Vector3 moveDirection;
        private Vector3 lookDirection;
        [SerializeField] float walkingSpeed = 2;
        [SerializeField] float runningSpeed = 5;
        [SerializeField] float rotationSpeed = 15;

        [Header("DODGE")] private Vector3 rollDirection;

        protected override void Awake()
        {
            base.Awake();

            player = GetComponent<PlayerManager>();
        }

        protected override void Update()
        {
            base.Update();

            if (player.IsOwner)
            {
                player.characterNetworkManager.verticalMovement.Value = verticalMovement;
                player.characterNetworkManager.horizontalMovement.Value = horizontalMovement;
                player.characterNetworkManager.moveAmount.Value = moveAmount;
            }
            else
            {
                verticalMovement = player.characterNetworkManager.verticalMovement.Value;
                horizontalMovement = player.characterNetworkManager.horizontalMovement.Value;
                moveAmount = player.characterNetworkManager.moveAmount.Value;

                player.playerAnimatorManager.UpdateAnimatorMovementParameters(0, moveAmount);
            }
        }

        private void GetMovementValues() 
        {
            verticalMovement = PlayerInputManager.instance.verticalInput;
            horizontalMovement = PlayerInputManager.instance.horizontalInput;
            moveAmount = PlayerInputManager.instance.moveAmount;
            // CLAMP THE MOVEMENTS
        }

        private void HandleGroundedMovement()
        {
            GetMovementValues();
            moveDirection = CameraManager.instance.transform.forward * verticalMovement +
                            CameraManager.instance.transform.right * horizontalMovement;
            moveDirection.Normalize();
            moveDirection.y = 0;

            if (PlayerInputManager.instance.moveAmount > 0.5f)
            {
                player.characterController.Move(Time.deltaTime * runningSpeed * moveDirection);
                // Debug.Log(moveDirection * Time.deltaTime * runningSpeed);
            }
            else if (PlayerInputManager.instance.moveAmount <= 0.5f)
            {
                player.characterController.Move(Time.deltaTime * walkingSpeed * moveDirection);
                // Debug.Log(moveDirection * Time.deltaTime * runningSpeed);
            }
        }

        private void HandleRotation()
        {
            lookDirection = Vector3.zero;
            lookDirection = CameraManager.instance.transform.forward * verticalMovement +
                            CameraManager.instance.transform.right * horizontalMovement;
            lookDirection.Normalize();
            lookDirection.y = 0;

            if (lookDirection == Vector3.zero)
            {
                lookDirection = transform.forward;
            }

            Quaternion rotation = Quaternion.LookRotation(lookDirection);
            Quaternion targetRotation = Quaternion.Slerp(transform.rotation, rotation, rotationSpeed * Time.deltaTime);
            transform.rotation = targetRotation;
        }

        public void HandleAllMovement() 
        {
            HandleGroundedMovement();
            HandleRotation();
            // GROUNDED MOVEMENT
            // AERIAL MOVEMENT
        }

        public void AttemptToPerformDodge()
        {
            // if (player.isPerformingAction)
            // {
            //     return;
            // }
            GetMovementValues();
            if (moveAmount <= 0)
            {
                rollDirection = CameraManager.instance.transform.forward * verticalMovement +
                                CameraManager.instance.transform.right * horizontalMovement;
                rollDirection.Normalize();

                rollDirection.y = 0;
                
                if (rollDirection == Vector3.zero)
                {
                    rollDirection = transform.forward;
                }
                Quaternion rotation = Quaternion.LookRotation(rollDirection);
                player.transform.rotation = rotation;
                
                player.playerAnimatorManager.PlayTargetActionAnimation("Roll_Forward_01", true, true);
                // PERFORM A ROLL ANIMATION
            }
            else
            {
                // PERFORM BACKSTEP ANIMATION
            }
        }
    }
}
