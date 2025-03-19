using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VirtueBanwa.Dialogue;

public class NPCController : MonoBehaviour
{
    public string npcName;
    public bool interactOnCollision = false;
    
    private NPCDialogueTrigger dialogueTrigger;
    
    void Start()
    {
        // Get dialogue trigger component or add one if missing
        dialogueTrigger = GetComponent<NPCDialogueTrigger>();
        if (dialogueTrigger == null)
        {
            dialogueTrigger = gameObject.AddComponent<NPCDialogueTrigger>();
        }
        
        // Set NPC name on the trigger if not already set
        if (string.IsNullOrEmpty(dialogueTrigger.npcName))
        {
            dialogueTrigger.npcName = npcName;
        }
    }
    
    public void TriggerDialogue()
    {
        GameManager gameManager = FindObjectOfType<GameManager>();
        if (gameManager != null)
        {
            gameManager.InteractWithNPC(npcName);
        }
    }
    
    void OnCollisionEnter2D(Collision2D collision)
    {
        if (interactOnCollision)
        {
            // Check if it's the player
            bool isPlayer = collision.gameObject.name.Contains("Player");
            
            if (isPlayer)
            {
                TriggerDialogue();
            }
        }
    }
}
