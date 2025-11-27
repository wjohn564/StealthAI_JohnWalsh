using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [SerializeField] private CharacterController characterController;

    [Header("Speed Settings")]
    // base movement speed
    [SerializeField] private float moveSpeed = 3.2f;
    // sprint speed modifier
    [SerializeField] private float sprintModifier = 1.5f;

    void Start()
    {   
        characterController = GetComponent<CharacterController>();
    }

    
    void Update()
    {
        // Control input
        float horizontalInput = Input.GetAxisRaw("Horizontal");
        float verticalInput = Input.GetAxisRaw("Vertical");
        bool isSprinting = Input.GetButton("Fire3");

        // Calculate movement direction
        // Builds a 2D direction on the XZ plane
        Vector3 inputDirection = new Vector3(horizontalInput, 0.0f, verticalInput).normalized;

        // Transform the input direction from local space to world space
        Vector3 moveDirection = transform.TransformDirection(inputDirection);

        // Project the movement direction onto the ground plane
        Vector3 groundNormal = Vector3.up;
        if (characterController.isGrounded)
        {
            // Raycast down to find the ground normal
            RaycastHit hit;
            // Slightly extend the raycast distance to ensure it hits the ground
            if (Physics.Raycast(transform.position, Vector3.down, out hit, 1.1f))
            {
                groundNormal = hit.normal;
            }
        }

        // Adjust move direction to be parallel to the ground
        moveDirection = Vector3.ProjectOnPlane(moveDirection, groundNormal);

        // Apply speed modifiers
        float calculatedMoveSpeed = isSprinting ? moveSpeed * sprintModifier : moveSpeed;
        // Scale the move direction by the calculated speed
        moveDirection *= calculatedMoveSpeed;
        print(moveDirection);
        // Move the character
        characterController.Move(moveDirection * Time.deltaTime);
    }
}