using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform playerTransform;
    public Vector3 cameraOffset;
    public float smoothSpeed = 0.125f;

    void Start()
    {
        // Ensure the playerTransform is set based on the selected character
        if (UserData.CurrentUser != null)
        {
            string character = UserData.CurrentUser.Character; // Access Character through CurrentUser
            if (character == "Boy")
            {
                playerTransform = GameObject.Find("BoySprite").transform;
            }
            else if (character == "Girl")
            {
                playerTransform = GameObject.Find("GirlSprite").transform;
            }
            else
            {
                Debug.LogWarning("Character data not found or is invalid.");
            }
        }
        else
        {
            Debug.LogError("UserData.CurrentUser is not set.");
        }
    }

    void Update()
    {
        if (playerTransform != null)
        {
            Vector3 desiredPosition = playerTransform.position + cameraOffset;
            Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed);
            transform.position = new Vector3(smoothedPosition.x, smoothedPosition.y, transform.position.z);
        }
    }
}