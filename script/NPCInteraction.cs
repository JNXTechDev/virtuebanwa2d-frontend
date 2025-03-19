using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NPCInteraction : MonoBehaviour
{
    public string npcName;
    public float interactionRadius = 2.0f;
    public GameObject interactionIndicator;
    
    private bool playerInRange = false;
    private GameManager gameManager;
    private Transform playerTransform;
    
    void Start()
    {
        // Find the game manager
        gameManager = FindObjectOfType<GameManager>();
        
        if (gameManager == null)
        {
            Debug.LogError("GameManager not found in the scene!");
        }
        
        // If npcName is not set, use the game object name
        if (string.IsNullOrEmpty(npcName))
        {
            npcName = gameObject.name;
        }
        
        // Hide the interaction indicator initially
        if (interactionIndicator != null)
        {
            interactionIndicator.SetActive(false);
        }
    }
    
    void Update()
    {
        // Find player if not already set
        if (playerTransform == null)
        {
            FindPlayer();
        }
        
        // Check if player is in range
        if (playerTransform != null)
        {
            float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);
            bool wasInRange = playerInRange;
            
            playerInRange = distanceToPlayer <= interactionRadius;
            
            // Only update indicator if the in-range status changed
            if (wasInRange != playerInRange && interactionIndicator != null)
            {
                interactionIndicator.SetActive(playerInRange);
            }
        }
        
        // Check for interaction when player is in range
        if (playerInRange && (Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Space)))
        {
            Interact();
        }
    }
    
    private void FindPlayer()
    {
        string character = PlayerPrefs.GetString("Character", "Boy");
        
        if (character == "Boy")
        {
            GameObject player = GameObject.Find("BoyPlayer");
            if (player != null)
            {
                playerTransform = player.transform;
            }
        }
        else if (character == "Girl")
        {
            GameObject player = GameObject.Find("GirlPlayer");
            if (player != null)
            {
                playerTransform = player.transform;
            }
        }
        
        if (playerTransform == null)
        {
            Debug.LogWarning("Could not find player character in scene.");
        }
    }
    
    private void Interact()
    {
        if (gameManager != null)
        {
            gameManager.InteractWithNPC(npcName);
        }
        else
        {
            Debug.LogError("Cannot interact with NPC: GameManager not found!");
        }
    }
    
    // Draw the interaction radius in the Scene view for debugging
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, interactionRadius);
        
        // Add NPC name label
        UnityEditor.Handles.color = Color.white;
        UnityEditor.Handles.Label(transform.position + Vector3.up * 2, npcName);
    }
}
