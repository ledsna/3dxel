using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerLocomotionManager : MonoBehaviour
{
    CharacterController characterController;

    [Header("MOVEMENT SETTINGS")]
    private Vector3 movementDirection;
    [SerializeField] float walkingSpeed = 2;

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
    }

    // Update is called once per frame
    void Update()
    {
        movementDirection = CameraManager.instance.transform.forward * PlayerInputManager.instance.verticalInput +
             CameraManager.instance.transform.right * PlayerInputManager.instance.horizontalInput;
        movementDirection.Normalize();
        movementDirection.y = 0;

        characterController.Move(movementDirection * walkingSpeed * Time.deltaTime);
    }
}
