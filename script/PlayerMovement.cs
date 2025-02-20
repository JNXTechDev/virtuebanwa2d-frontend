using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    public float moveSpeed = 10f;  // Speed at which the player moves
    public Rigidbody2D rb;       // Rigidbody2D component for physics-based movement
    public Animator animator;    // Animator component for animations
    public Joystick joystick;    // Joystick for input

    private Vector2 movement;    // Stores movement direction
    private bool isMovementFrozen = false;  // Flag to control movement freezing

    void Update()
    {
        if (!isMovementFrozen)
        {
            // Get joystick input for movement
            movement.x = joystick.Horizontal;
            movement.y = joystick.Vertical;

            // Normalize movement to prevent diagonal movement from being faster
            movement = movement.normalized;

            // Update animator parameters to control animations
            animator.SetFloat("Horizontal", movement.x);
            animator.SetFloat("Vertical", movement.y);
            animator.SetFloat("Speed", movement.sqrMagnitude);  // Use squared magnitude for performance

            // Check if there is any movement
            if (movement != Vector2.zero)
            {
                animator.SetBool("isWalking", true);  // Trigger walking animation
            }
            else
            {
                animator.SetBool("isWalking", false);  // Trigger idle animation
            }
        }
        else
        {
            // Reset movement and animations when frozen
            movement = Vector2.zero;
            animator.SetFloat("Speed", 0f);
            animator.SetBool("isWalking", false);
        }
    }

    void FixedUpdate()
    {
        if (!isMovementFrozen)
        {
            // Move the player character directly without smoothing
            rb.MovePosition(rb.position + movement * moveSpeed * Time.fixedDeltaTime);
        }
    }

    // Method called by NPCscript to freeze movement during dialogue
    public void FreezeMovement()
    {
        isMovementFrozen = true;
        rb.velocity = Vector2.zero; // Stop any existing movement
    }

    // Method called by NPCscript to unfreeze movement after dialogue
    public void UnfreezeMovement()
    {
        isMovementFrozen = false;
    }
}