using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class Unit1Lesson1Controller : MonoBehaviour
{
    [Header("NPC References")]
    public GameObject npc1;
    public GameObject npc2;
    public GameObject npc3;

    [Header("UI References")]
    public Button talkToNPC1Button;
    public Button talkToNPC2Button;
    public Button talkToNPC3Button;
    public Button nextLessonButton;
    public TextMeshProUGUI lessonProgressText;

    // Use DialogueManager instead of LessonDialogueManager
    private DialogueManager dialogueManager;
    private int talkedToNPCs = 0;
    private const int TOTAL_NPCS = 3;

    private void Start()
    {
        // Find the DialogueManager in the scene
        dialogueManager = FindObjectOfType<DialogueManager>();
        if (dialogueManager == null)
        {
            Debug.LogError("DialogueManager not found in scene!");
            return;
        }

        // Set up dialogue manager for this lesson
        dialogueManager.lessonFile = "Unit1Lesson1";
        
        // Set up buttons
        if (talkToNPC1Button != null)
            talkToNPC1Button.onClick.AddListener(() => TalkToNPC(npc1, 0));

        if (talkToNPC2Button != null)
            talkToNPC2Button.onClick.AddListener(() => TalkToNPC(npc2, 1));

        if (talkToNPC3Button != null)
            talkToNPC3Button.onClick.AddListener(() => TalkToNPC(npc3, 2));
            
        if (nextLessonButton != null)
        {
            nextLessonButton.gameObject.SetActive(false);
            nextLessonButton.onClick.AddListener(OnNextLessonClicked);
        }
        
        UpdateProgress();
    }

    private void TalkToNPC(GameObject npc, int npcIndex)
    {
        // Ensure the NPC is enabled and visible
        npc.SetActive(true);
        
        // Start dialogue with this NPC
        FindObjectOfType<DialogueManager>()?.OnChoiceSelected(npcIndex);
        
        // Mark NPC as talked to
        talkedToNPCs++;
        
        // Update UI
        UpdateProgress();
    }
    
    private void UpdateProgress()
    {
        // Update progress text
        if (lessonProgressText != null)
        {
            lessonProgressText.text = $"Progress: {talkedToNPCs}/{TOTAL_NPCS} NPCs";
        }
        
        // Check if all NPCs have been talked to
        if (talkedToNPCs >= TOTAL_NPCS && nextLessonButton != null)
        {
            // Show next lesson button
            nextLessonButton.gameObject.SetActive(true);
        }
    }
    
    private void OnNextLessonClicked()
    {
        // Save progress and move to next lesson
        PlayerPrefs.SetString("CurrentLesson", "Lesson 2");
        PlayerPrefs.Save();
        
        // Load the lesson selection scene
        UnityEngine.SceneManagement.SceneManager.LoadScene("LessonSelection");
    }

    private void OnDestroy()
    {
        // Clean up button listeners
        if (talkToNPC1Button != null)
            talkToNPC1Button.onClick.RemoveAllListeners();

        if (talkToNPC2Button != null)
            talkToNPC2Button.onClick.RemoveAllListeners();

        if (talkToNPC3Button != null)
            talkToNPC3Button.onClick.RemoveAllListeners();
            
        if (nextLessonButton != null)
            nextLessonButton.onClick.RemoveAllListeners();
    }
}
