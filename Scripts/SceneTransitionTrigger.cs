using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneTransitionTrigger : MonoBehaviour
{
    [Header("Transition Configuration")]
    [Tooltip("The name of the scene to load")]
    public string targetSceneName;
    
    [Tooltip("Optional loading screen to show during transition")]
    public GameObject loadingScreen;
    
    [Tooltip("Text to display while loading (if loadingScreen has TextMeshPro component)")]
    public string loadingText = "Loading...";
    
    [Tooltip("Add a short delay before transition (seconds)")]
    public float transitionDelay = 0.5f;
    
    [Header("Trigger Settings")]
    [Tooltip("What layers can trigger this transition")]
    public LayerMask triggerLayers = ~0; // Default to "Everything"
    
    [Tooltip("Should this trigger work on enter or by pressing a key?")]
    public TriggerType triggerType = TriggerType.OnKeyPress;
    
    [Tooltip("Key to press for keyboard-activated transitions")]
    public KeyCode activationKey = KeyCode.E;
    
    [Header("Visual Indicator")]
    [Tooltip("Optional visual indicator to show when player can trigger transition")]
    public GameObject interactionIndicator;
    
    // Track if player is in trigger zone
    private bool playerInTriggerZone = false;
    // Track if transition is in progress
    private bool isTransitioning = false;
    
    // Different ways to trigger the transition
    public enum TriggerType
    {
        OnEnter,      // Trigger as soon as player enters the collider
        OnKeyPress    // Trigger when player presses specified key while in collider
    }
    
    private void Start()
    {
        // Validate the scene name
        if (string.IsNullOrEmpty(targetSceneName))
        {
            Debug.LogWarning("Target scene name is empty on " + gameObject.name);
        }
        
        // Hide the interaction indicator initially
        if (interactionIndicator != null)
        {
            interactionIndicator.SetActive(false);
        }
        
        // Hide loading screen initially
        if (loadingScreen != null)
        {
            loadingScreen.SetActive(false);
        }
        
        // Make sure there's a collider
        Collider2D col = GetComponent<Collider2D>();
        if (col == null)
        {
            Debug.LogError("SceneTransitionTrigger requires a Collider2D component on " + gameObject.name);
        }
        else if (!col.isTrigger)
        {
            Debug.LogWarning("Collider2D on " + gameObject.name + " should have 'Is Trigger' enabled");
            col.isTrigger = true;
        }
    }
    
    private void Update()
    {
        // Check for key press if in trigger zone and using key press mode
        if (playerInTriggerZone && triggerType == TriggerType.OnKeyPress && Input.GetKeyDown(activationKey))
        {
            TriggerTransition();
        }
    }
    
    private void OnTriggerEnter2D(Collider2D collision)
    {
        // Check if the colliding object is on the target layer
        if (IsInLayerMask(collision.gameObject.layer, triggerLayers))
        {
            playerInTriggerZone = true;
            
            // Show interaction indicator if using key press mode
            if (triggerType == TriggerType.OnKeyPress && interactionIndicator != null)
            {
                interactionIndicator.SetActive(true);
            }
            
            // If trigger type is OnEnter, start transition immediately
            if (triggerType == TriggerType.OnEnter)
            {
                TriggerTransition();
            }
        }
    }
    
    private void OnTriggerExit2D(Collider2D collision)
    {
        if (IsInLayerMask(collision.gameObject.layer, triggerLayers))
        {
            playerInTriggerZone = false;
            
            // Hide interaction indicator
            if (interactionIndicator != null)
            {
                interactionIndicator.SetActive(false);
            }
        }
    }
    
    private void TriggerTransition()
    {
        if (isTransitioning) return; // Prevent multiple transitions
        
        if (string.IsNullOrEmpty(targetSceneName))
        {
            Debug.LogError("Cannot transition: Target scene name is empty");
            return;
        }
        
        isTransitioning = true;
        
        // Start transition coroutine
        StartCoroutine(TransitionToScene());
    }
    
    private IEnumerator TransitionToScene()
    {
        // Save current scene info to PlayerPrefs if needed
        SaveTransitionData();
        
        // Show loading screen if assigned
        if (loadingScreen != null)
        {
            loadingScreen.SetActive(true);
            
            // Set loading text if there's a TextMeshPro component
            TMPro.TextMeshProUGUI loadingTextComponent = 
                loadingScreen.GetComponentInChildren<TMPro.TextMeshProUGUI>();
                
            if (loadingTextComponent != null)
            {
                loadingTextComponent.text = loadingText;
            }
        }
        
        // Short delay
        yield return new WaitForSeconds(transitionDelay);
        
        // Load the scene
        SceneManager.LoadScene(targetSceneName);
    }
    
    private bool IsInLayerMask(int layer, LayerMask layerMask)
    {
        return layerMask == (layerMask | (1 << layer));
    }
    
    private void SaveTransitionData()
    {
        // Save the current scene and position for potential return journeys
        PlayerPrefs.SetString("LastScene", SceneManager.GetActiveScene().name);
        
        // If transitioning to a different scene in same unit, we might want to save current objectives
        string currentUnit = PlayerPrefs.GetString("CurrentUnit", "");
        string currentLesson = PlayerPrefs.GetString("CurrentLesson", "");
        
        // If we're in a unit/lesson, make sure that data persists to the next scene
        if (!string.IsNullOrEmpty(currentUnit) && !string.IsNullOrEmpty(currentLesson))
        {
            PlayerPrefs.Save();
        }
    }
    
    // Editor method to make the trigger area visible
    private void OnDrawGizmos()
    {
        Collider2D collider = GetComponent<Collider2D>();
        if (collider != null)
        {
            // Draw a semitransparent blue box for the trigger area
            Gizmos.color = new Color(0.2f, 0.5f, 1f, 0.4f);
            
            if (collider is BoxCollider2D)
            {
                BoxCollider2D boxCollider = collider as BoxCollider2D;
                Gizmos.matrix = transform.localToWorldMatrix;
                Gizmos.DrawCube(boxCollider.offset, boxCollider.size);
            }
            else
            {
                // Just use bounds for non-box colliders
                Gizmos.DrawCube(collider.bounds.center, collider.bounds.size);
            }
            
            // Draw arrow in direction of target scene
            Gizmos.color = Color.green;
            Vector3 center = collider.bounds.center;
            Vector3 arrowTip = center + transform.up * 0.5f;
            Gizmos.DrawLine(center, arrowTip);
            
            // Draw a label with the target scene name
            UnityEditor.Handles.Label(arrowTip, targetSceneName);
        }
    }
}
