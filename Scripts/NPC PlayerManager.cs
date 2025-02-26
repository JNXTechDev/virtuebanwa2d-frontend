using UnityEngine;

public class PlayerManager : MonoBehaviour
{
    public GameObject boySprite;
    public GameObject girlSprite;
    public Camera mainCamera;
    public float smoothSpeed = 0.125f;
    public Vector3 offset = new Vector3(0, 0, -10); // Camera offset from player
    
    private string currentCharacter;
    private Transform activePlayerTransform;

    void Start()
    {
        // Get character from PlayerPrefs
        currentCharacter = PlayerPrefs.GetString("Character", "");
        Debug.Log($"Character loaded: {currentCharacter}");

        // Deactivate both initially
        boySprite.SetActive(false);
        girlSprite.SetActive(false);

        // Show correct sprite and set active player transform
        if (currentCharacter == "Boy")
        {
            boySprite.SetActive(true);
            activePlayerTransform = boySprite.transform;
            Debug.Log("Activated Boy sprite");
        }
        else if (currentCharacter == "Girl")
        {
            girlSprite.SetActive(true);
            activePlayerTransform = girlSprite.transform;
            Debug.Log("Activated Girl sprite");
        }
        else
        {
            Debug.LogError($"Invalid character type: {currentCharacter}");
        }

        // Set initial camera position
        if (mainCamera != null && activePlayerTransform != null)
        {
            mainCamera.transform.position = activePlayerTransform.position + offset;
        }
    }

    void LateUpdate()
    {
        if (mainCamera != null && activePlayerTransform != null)
        {
            Vector3 desiredPosition = activePlayerTransform.position + offset;
            Vector3 smoothedPosition = Vector3.Lerp(mainCamera.transform.position, desiredPosition, smoothSpeed);
            mainCamera.transform.position = smoothedPosition;
        }
    }
}
