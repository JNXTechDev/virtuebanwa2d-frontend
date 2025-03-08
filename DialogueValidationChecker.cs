
using UnityEngine;
using System.Collections;
using System.IO;

/// <summary>
/// This script verifies that all NPCs have the correct dialogue files assigned.
/// Attach it to a game object in your tutorial scene.
/// </summary>
public class DialogueValidationChecker : MonoBehaviour
{
    [SerializeField] private bool autoFixIssues = true;
    
    void Start()
    {
        // Wait a frame to allow other scripts to initialize first
        StartCoroutine(DelayedCheck());
    }
    
    IEnumerator DelayedCheck()
    {
        yield return new WaitForEndOfFrame();
        
        // Find all NPCs in the scene
        NPCscript[] allNPCScripts = FindObjectsOfType<NPCscript>();
        Debug.Log($"Found {allNPCScripts.Length} NPCs in scene");
        
        foreach (NPCscript npc in allNPCScripts)
        {
            string npcName = npc.gameObject.name.Replace("NPC ", "").Replace(" Tutorial", "");
            Debug.Log($"Validating dialogue for {npcName} (GameObject: {npc.gameObject.name})");
            
            // Check if dialogue file exists and matches NPC name
            if (npc.dialogueFile == null)
            {
                Debug.LogError($"[ValidationError] {npcName} has no dialogue file assigned!");
                
                if (autoFixIssues)
                {
                    string expectedDialoguePath = $"DialogueData/Tutorial{npcName}";
                    TextAsset dialogueFile = Resources.Load<TextAsset>(expectedDialoguePath);
                    if (dialogueFile != null)
                    {
                        npc.dialogueFile = dialogueFile;
                        Debug.Log($"Fixed: Assigned {dialogueFile.name} to {npcName}");
                        
                        // Force reload of dialogue
                        npc.ReloadDialogueFile();
                    }
                    else
                    {
                        Debug.LogError($"Could not find dialogue file at {expectedDialoguePath}");
                    }
                }
            }
            else
            {
                // Check if the assigned file name matches the NPC name
                string dialogueFileName = npc.dialogueFile.name;
                if (!dialogueFileName.Contains(npcName))
                {
                    Debug.LogWarning($"[ValidationWarning] {npcName} has dialogue file named {dialogueFileName}, which doesn't match NPC name!");
                    
                    if (autoFixIssues)
                    {
                        string expectedDialoguePath = $"DialogueData/Tutorial{npcName}";
                        TextAsset dialogueFile = Resources.Load<TextAsset>(expectedDialoguePath);
                        if (dialogueFile != null)
                        {
                            npc.dialogueFile = dialogueFile;
                            Debug.Log($"Fixed: Replaced {dialogueFileName} with {dialogueFile.name} for {npcName}");
                            
                            // Force reload of dialogue
                            npc.ReloadDialogueFile();
                        }
                    }
                }
                
                // Check if the dialogue has the right NPC name inside it
                VirtueBanwa.DialogueData dialogue = JsonUtility.FromJson<VirtueBanwa.DialogueData>(npc.dialogueFile.text);
                if (dialogue != null && dialogue.npcName != npcName)
                {
                    Debug.LogWarning($"[ValidationWarning] {npcName}'s dialogue file contains npcName: '{dialogue.npcName}' instead of '{npcName}'");
                }
            }
        }
    }
}
