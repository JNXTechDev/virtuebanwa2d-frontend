using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public float followSpeed = 2f;  // Speed at which the camera follows the player
    public float yOffset = 1f;      // Vertical offset for the camera
    public Transform boyTransform;   // Reference to the Boy sprite's transform
    public Transform girlTransform;   // Reference to the Girl sprite's transform

    private Transform target;        // The target (player) to follow
    private Vector3 velocity = Vector3.zero; // For SmoothDamp

    void Start()
    {
        // Initialize camera position
        SetTarget();
    }

    void LateUpdate()
    {
        if (target != null)
        {
            // Smoothly move the camera towards the target position
            Vector3 targetPos = new Vector3(target.position.x, target.position.y + yOffset, transform.position.z);
            transform.position = Vector3.SmoothDamp(transform.position, targetPos, ref velocity, followSpeed);
        }
    }

    // Set the target based on the active character
    public void SetTarget()
    {
        string character = UserProfile.CurrentUser.Character;
        
        if (character == "Boy" && boyTransform != null)
        {
            target = boyTransform;
            Debug.Log("Camera target set to Boy.");
        }
        else if (character == "Girl" && girlTransform != null)
        {
            target = girlTransform;
            Debug.Log("Camera target set to Girl.");
        }
        else
        {
            Debug.LogWarning($"Character not recognized or transform missing. Character: {character}");
        }
    }
}