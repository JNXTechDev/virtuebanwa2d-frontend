using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;

public class QuestBookManager : MonoBehaviour
{
    [Header("UI References")]
    public GameObject questBookPanel;
    public Button closeButton;
    
    [Header("Tutorial References")]
    public Button startTutorialButton;
    public string tutorialSceneName = "Tutorial Outside";
    
    [Header("Option Configuration")]
    // Whether to use the quest book as entry point for tutorials/lessons
    public bool useQuestBookForLessons = true;
    
    private SceneTransition sceneTransition;
    
    private void Awake()
    {
        sceneTransition = FindObjectOfType<SceneTransition>();
        
        if (questBookPanel != null)
        {
            questBookPanel.SetActive(false);
        }
        
        if (closeButton != null)
        {
            closeButton.onClick.AddListener(HideQuestBook);
        }
        
        if (startTutorialButton != null)
        {
            startTutorialButton.onClick.AddListener(StartTutorial);
        }
    }
    
    public void ShowQuestBook()
    {
        if (questBookPanel != null)
        {
            questBookPanel.SetActive(true);
            
            // Update any quest status indicators here
            UpdateQuestStatus();
        }
    }
    
    public void HideQuestBook()
    {
        if (questBookPanel != null)
        {
            questBookPanel.SetActive(false);
        }
    }
    
    private void UpdateQuestStatus()
    {
        // You can update quest completion status, available quests, etc. here
        bool tutorialCompleted = PlayerPrefs.GetInt("TutorialCompleted", 0) == 1;
        
        // Update UI based on completion status
        // For example, you could change button text or colors
        
        // Example: Update tutorial button text based on completion
        if (startTutorialButton != null)
        {
            TextMeshProUGUI buttonText = startTutorialButton.GetComponentInChildren<TextMeshProUGUI>();
            if (buttonText != null)
            {
                buttonText.text = tutorialCompleted ? "Replay Tutorial" : "Start Tutorial";
            }
        }
    }
    
    private void StartTutorial()
    {
        HideQuestBook();
        
        if (sceneTransition != null)
        {
            sceneTransition.StartTransition(tutorialSceneName);
        }
        else
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene(tutorialSceneName);
        }
    }
}
