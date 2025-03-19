using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using VirtueBanwa.Dialogue;
using VirtueBanwa;

public class TutorialManager : MonoBehaviour
{
    [Header("Tutorial Settings")]
    public bool autoStartTutorial = true;
    public float delayBeforeStart = 2f;
    
    [Header("NPC References")]
    public GameObject janicaNPC;
    public GameObject markNPC;
    public GameObject annieNPC;
    public GameObject rojanNPC;
    
    [Header("UI References")]
    public GameObject tutorialPanel;
    public Text tutorialText;
    public Button nextButton;
    
    private GameManager gameManager;
    private TutorialChecker tutorialChecker;
    private DirectionIndicator directionIndicator;
    
    private bool isTutorialComplete = false;
    
    void Start()
    {
        // Find needed components
        gameManager = FindObjectOfType<GameManager>();
        tutorialChecker = GetComponent<TutorialChecker>();
        if (tutorialChecker == null)
        {
            tutorialChecker = gameObject.AddComponent<TutorialChecker>();
        }
        
        directionIndicator = FindObjectOfType<DirectionIndicator>();
        
        // Hide tutorial panel initially
        if (tutorialPanel != null)
        {
            tutorialPanel.SetActive(false);
        }
        
        // Check if tutorial is already complete
        CheckTutorialStatus();
        
        // Auto-start tutorial if enabled
        if (autoStartTutorial && !isTutorialComplete)
        {
            StartCoroutine(StartTutorialAfterDelay());
        }
    }
    
    private async void CheckTutorialStatus()
    {
        string username = PlayerPrefs.GetString("Username", "");
        if (!string.IsNullOrEmpty(username) && tutorialChecker != null)
        {
            isTutorialComplete = await tutorialChecker.IsTutorialComplete(username);
            
            if (isTutorialComplete)
            {
                Debug.Log("Tutorial already completed.");
            }
        }
    }
    
    private IEnumerator StartTutorialAfterDelay()
    {
        yield return new WaitForSeconds(delayBeforeStart);
        
        // Set game mode to tutorial if not already
        if (gameManager != null && gameManager.currentGameMode != GameMode.Tutorial)
        {
            gameManager.currentGameMode = GameMode.Tutorial;
            gameManager.SendMessage("InitializeGameMode", SendMessageOptions.DontRequireReceiver);
        }
        
        // Show first tutorial step
        ShowTutorialPanel("Welcome to Virtue Banwa! Follow the green arrow to find Janica and begin your adventure.");
    }
    
    public void ShowTutorialPanel(string message)
    {
        if (tutorialPanel != null && tutorialText != null)
        {
            tutorialPanel.SetActive(true);
            tutorialText.text = message;
        }
    }
    
    public void HideTutorialPanel()
    {
        if (tutorialPanel != null)
        {
            tutorialPanel.SetActive(false);
        }
    }
}
