using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SG
{
    public class PlayerManager : CharacterManager
    {
        [HideInInspector] public PlayerAnimatorManager playerAnimatorManager;
        [HideInInspector] public PlayerLocomotionManager playerLocomotionManager;
        protected override void Awake() 
        {
            base.Awake();
            
            playerAnimatorManager = GetComponent<PlayerAnimatorManager>();
            playerLocomotionManager = GetComponent<PlayerLocomotionManager>();
        }

        protected override void Update()
        {
            base.Update();

            if (!IsOwner)
                return;

            playerLocomotionManager.HandleAllMovement();
        }

        protected override void LateUpdate()
        {
            if (!IsOwner)
                return;
            
            base.LateUpdate();

            CameraManager.instance.HandleAllCameraActions();
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            if (IsOwner)
            {
                CameraManager.instance.player = this;
                PlayerInputManager.instance.player = this;
            }
        }
    }
}