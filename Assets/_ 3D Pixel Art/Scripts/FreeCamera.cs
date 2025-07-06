using System;
using UnityEngine;
using UnityEngine.InputSystem;
using Quaternion = UnityEngine.Quaternion;

public class FreeCamera : MonoBehaviour
{
    [Header("Untargeted Settings")] [SerializeField]
    float cameraVelocity = 10;
    
    [Header("Settings")] [SerializeField]
    float angleIncrement = 45f;
    
    [SerializeField] float targetAngle = 45f;
    [SerializeField] float mouseSensitivity = 8f;
    [SerializeField] float rotationSpeed = 5f;
    private float currentAngle;

    private InputAction rotationAction;
    private InputAction moveAction;
    private InputAction upAction;
    private InputAction downAction;

    private bool isMovingUp;
    private bool isMovingDown;
    private void Awake()
    {
        rotationAction = InputSystem.actions.FindAction("Rotation");
        moveAction = InputSystem.actions.FindAction("Movement");
        upAction = InputSystem.actions.FindAction("Up");
        downAction = InputSystem.actions.FindAction("Down");
    }
    
    private void OnEnable()
    {
        upAction.performed += SetMovingUpTrue;
        upAction.canceled += SetMovingUpFalse;
        
        downAction.performed += SetMovingDownTrue;
        downAction.canceled += SetMovingDownFalse;
    }
    
    private void OnDisable()
    {
        upAction.performed -= SetMovingUpTrue;
        upAction.canceled -= SetMovingUpFalse;
        
        downAction.performed -= SetMovingDownTrue;
        downAction.canceled -= SetMovingDownFalse;
    }
    
    private void SetMovingUpTrue(InputAction.CallbackContext context) => isMovingUp = true; 
    private void SetMovingUpFalse(InputAction.CallbackContext context) => isMovingUp = false; 
    private void SetMovingDownTrue(InputAction.CallbackContext context) => isMovingDown = true; 
    private void SetMovingDownFalse(InputAction.CallbackContext context) => isMovingDown = false; 
    
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

    private void HandleMove(Vector2 inputDirection)
    {
        if (inputDirection.magnitude == 0)
            return;
        var forward = transform.forward;
        forward.y = 0;
        var directionWS = transform.right * inputDirection.x + forward * inputDirection.y;
        transform.position += (Time.deltaTime * cameraVelocity) * directionWS;
    }

    private void HandleUpDown()
    {
        if (isMovingUp)
        {
            transform.position += Vector3.up * (cameraVelocity * Time.deltaTime);
        }
        if (isMovingDown)
        {
            transform.position += Vector3.down * (cameraVelocity * Time.deltaTime);
        }
    }
    private void Update()
    {
        // TODO: вообще все передвижения нужно делать в FixedUpdate, чтобы не зависило от количества кадров.
        // Иначе передвижение будет отличаться. Но тут можно
        HandleMove(moveAction.ReadValue<Vector2>());
        HandleRotation(rotationAction.ReadValue<float>());
        HandleUpDown();
    }
}
