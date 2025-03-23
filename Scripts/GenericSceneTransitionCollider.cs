using UnityEngine;
using UnityEngine.SceneManagement; // Add this missing reference

public class GenericSceneTransitionCollider : MonoBehaviour 
{
    [Header("Transition Settings")]
    [Tooltip("Name of the scene to load when player enters the trigger")]
    public string targetSceneName;
    
    [Tooltip("Optional: Save player's position before transitioning (for returning later)")]
    public bool savePlayerPosition = false;
    
    [Header("Optional Effects")]
    [Tooltip("Whether to fade screen when transitioning")]
    public bool useFade = true;
    
    [Tooltip("Message to display during transition (leave empty for none)")]
    public string transitionMessage = "";
    
    [Header("Player Detection")]
    [Tooltip("Which player tags can trigger this transition")]
    public string[] allowedPlayerTags = { "Player" };
    
    [Tooltip("If true, checks if player is using boy/girl from PlayerPrefs")]
    public bool checkCharacterPrefs = true;
    
    // Reference to scene transition manager
    private SceneTransition sceneTransition;
    
    private void Start()
    {
        // Find scene transition component in the scene
        sceneTransition = FindObjectOfType<SceneTransition>();
        
        if (sceneTransition == null)
            Debug.LogWarning("SceneTransition component not found in scene. Scene transition won't work!");
            
        // Validate that this object has a collider
        if (GetComponent<Collider2D>() == null)
            Debug.LogError("This object needs a Collider2D component set as trigger!");
    }
    
    private void OnTriggerEnter2D(Collider2D other)
    {
        // Check if this is a player object
        bool isPlayer = IsPlayerObject(other.gameObject);
        
        if (isPlayer && sceneTransition != null && !string.IsNullOrEmpty(targetSceneName))
        {
            Debug.Log($"Player entered transition collider. Loading scene: {targetSceneName}");
            
            // Save player position if needed
            if (savePlayerPosition)
            {
                Vector3 position = other.transform.position;
                PlayerPrefs.SetFloat("LastPositionX", position.x);
                PlayerPrefs.SetFloat("LastPositionY", position.y);
                PlayerPrefs.SetFloat("LastPositionZ", position.z);
                PlayerPrefs.SetString("LastScene", SceneManager.GetActiveScene().name);
                PlayerPrefs.Save();
            }
            
            // Start transition
            if (!string.IsNullOrEmpty(transitionMessage))
            {
                // If we have a message, use it
                sceneTransition.sceneToLoad = targetSceneName;
                sceneTransition.StartTransition(targetSceneName);
            }
            else
            {
                // Otherwise just load the scene
                sceneTransition.sceneToLoad = targetSceneName;
                sceneTransition.StartTransition(targetSceneName);
            }
        }
    }
    
    private bool IsPlayerObject(GameObject obj)
    {
        // Check if object has any of our allowed tags
        foreach (string tag in allowedPlayerTags)
        {
            if (obj.CompareTag(tag))
                return true;
        }
        
        // If we're checking character from PlayerPrefs
        if (checkCharacterPrefs)
        {
            string character = PlayerPrefs.GetString("Character", "Boy");
            return (character == "Boy" && obj.name.Contains("BoySprite")) ||
                  (character == "Girl" && obj.name.Contains("GirlSprite"));
        }
        
        return false;
    }
}
