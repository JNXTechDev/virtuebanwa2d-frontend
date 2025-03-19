using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TutorialProgressManager : MonoBehaviour
{
    public string[] NpcNames; // Array to hold NPC names
    public GameObject directionArrow;
    public Transform markNPCLocation;
    public Transform annieNPCLocation;
    public Transform schoolEntranceLocation;

    private bool talkedToMark = false;
    private bool talkedToAnnie = false;

    [Header("Tutorial State")]
    public bool talkedToJanica = false;
    
    [Header("UI Elements")]
    public GameObject progressPanel;
    public TMP_Text progressText;
    public Image progressBar;
    
    [Header("References")]
    public GameManager gameManager; // This will be assigned via Inspector
    public QuestManager questManager; // This will be assigned via Inspector
    
    // Initialize on Start
    void Start()
    {
        if (directionArrow != null)
        {
            // Start by pointing to Mark
            UpdateArrowTarget(markNPCLocation);
        }

        // Find components if not assigned
        if (gameManager == null)
        {
            gameManager = FindObjectOfType<GameManager>();
        }
        
        if (questManager == null && gameManager != null)
        {
            questManager = gameManager.GetComponent<QuestManager>();
        }
        
        UpdateProgressDisplay();
    }

    public void OnTutorialProgress(string npcName)
    {
        if (npcName == "Mark")
        {
            talkedToMark = true;
            UpdateArrowTarget(annieNPCLocation);
        }
        else if (npcName == "Annie")
        {
            talkedToAnnie = true;
            UpdateArrowTarget(schoolEntranceLocation);
        }
    }

    private void UpdateArrowTarget(Transform target)
    {
        if (directionArrow != null && target != null)
        {
            directionArrow.SetActive(true);
            directionArrow.transform.position = target.position + Vector3.up * 2f; // Float above target
        }
    }

    // Track when player talks to an NPC
    public void TrackNPCInteraction(string npcName)
    {
        switch (npcName)
        {
            case "Janica":
                talkedToJanica = true;
                break;
            case "Mark":
                // Track that we talked to Mark
                PlayerPrefs.SetInt("TalkedToMark", 1);
                break;
            case "Annie":
                // Track that we talked to Annie
                PlayerPrefs.SetInt("TalkedToAnnie", 1);
                break;
            case "Rojan":
                // Track that we talked to Rojan
                PlayerPrefs.SetInt("TalkedToRojan", 1);
                break;
        }
        
        UpdateProgressDisplay();
    }
    
    // Check if a specific NPC has been talked to
    public bool HasTalkedToNPC(string npcName)
    {
        switch (npcName)
        {
            case "Janica":
                return talkedToJanica;
            case "Mark":
                return PlayerPrefs.GetInt("TalkedToMark", 0) == 1;
            case "Annie":
                return PlayerPrefs.GetInt("TalkedToAnnie", 0) == 1;
            case "Rojan":
                return PlayerPrefs.GetInt("TalkedToRojan", 0) == 1;
            default:
                return false;
        }
    }
    
    // Update progress UI
    private void UpdateProgressDisplay()
    {
        // Skip if UI elements aren't assigned
        if (progressPanel == null || progressText == null)
            return;
        
        // Count how many NPCs we've talked to
        int talkedToCount = 0;
        if (talkedToJanica) talkedToCount++;
        if (PlayerPrefs.GetInt("TalkedToMark", 0) == 1) talkedToCount++;
        if (PlayerPrefs.GetInt("TalkedToAnnie", 0) == 1) talkedToCount++;
        if (PlayerPrefs.GetInt("TalkedToRojan", 0) == 1) talkedToCount++;
        
        // Log progress for debugging
        Debug.Log($"Tutorial Progress: {talkedToCount}/4 NPCs");
        
        // Update UI if available
        if (progressText != null)
        {
            progressText.text = $"Tutorial Progress: {talkedToCount}/4 NPCs";
        }
        
        if (progressBar != null)
        {
            progressBar.fillAmount = talkedToCount / 4f;
        }
    }
    
    // Reset tutorial progress
    public void ResetProgress()
    {
        talkedToJanica = false;
        PlayerPrefs.DeleteKey("TalkedToMark");
        PlayerPrefs.DeleteKey("TalkedToAnnie");
        PlayerPrefs.DeleteKey("TalkedToRojan");
        PlayerPrefs.Save();
        
        UpdateProgressDisplay();
    }
}
