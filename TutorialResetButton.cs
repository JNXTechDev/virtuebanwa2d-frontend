using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using UnityEngine.SceneManagement;

public class TutorialResetButton : MonoBehaviour
{
    [Header("Visual Feedback")]
    public float resetFadeTime = 1.0f;
    public GameObject resetFeedbackPanel;
    
    [SerializeField] private Button resetButton;
    [SerializeField] private Button skipButton;
    
    private TutorialManager tutorialManager;
    
    private void Awake()
    {
        // Find tutorial manager
        tutorialManager = FindObjectOfType<TutorialManager>();
        
        if (resetButton != null)
        {
            resetButton.onClick.AddListener(OnResetButtonClicked);
        }
        
        if (skipButton != null)
        {
            skipButton.onClick.AddListener(OnSkipButtonClicked);
        }
    }
    
    private void OnResetButtonClicked()
    {
        if (tutorialManager != null)
        {
            tutorialManager.ResetTutorialProgress();
            Debug.Log("Tutorial progress reset");
        }
        else
        {
            Debug.LogError("TutorialManager not found!");
        }
    }
    
    private void OnSkipButtonClicked()
    {
        if (tutorialManager != null)
        {
            tutorialManager.SkipToCompleted();
            Debug.Log("Tutorial skipped to completed state");
        }
        else
        {
            Debug.LogError("TutorialManager not found!");
        }
    }
    
    private void OnDestroy()
    {
        if (resetButton != null)
        {
            resetButton.onClick.RemoveListener(OnResetButtonClicked);
        }
        
        if (skipButton != null)
        {
            skipButton.onClick.RemoveListener(OnSkipButtonClicked);
        }
    }
    
    void Start()
    {
        if (resetButton != null)
        {
            resetButton.onClick.RemoveAllListeners();
            resetButton.onClick.AddListener(ResetTutorialWithVisualFeedback);
        }
        
        if (resetFeedbackPanel != null)
        {
            resetFeedbackPanel.SetActive(false);
        }
    }
    
    private void ResetTutorialWithVisualFeedback()
    {
        StartCoroutine(ResetTutorialSequence());
    }
    
    private IEnumerator ResetTutorialSequence()
    {
        // First disable the button to prevent multiple clicks
        resetButton.interactable = false;
        
        // Show visual feedback if available
        if (resetFeedbackPanel != null)
        {
            resetFeedbackPanel.SetActive(true);
        }
        
        // Find Annie NPC but don't change visibility
        GameObject annieNPC = GameObject.Find("NPC Annie Tutorial");
        
        // Reset tutorial progress
        if (tutorialManager != null)
        {
            // Call without parameter
            tutorialManager.ResetTutorialProgress();
            Debug.Log("Tutorial reset initiated - Annie's visibility preserved");
            
            // Wait a moment to show the visual feedback
            yield return new WaitForSeconds(resetFadeTime);
            
            // Reload the current scene
            Scene currentScene = SceneManager.GetActiveScene();
            SceneManager.LoadScene(currentScene.name);
        }
        else
        {
            Debug.LogError("TutorialManager not found!");
            
            // If we couldn't find the manager, still reset the tutorial state properly
            if (annieNPC != null)
            {
                // Instead of calling ResetTutorialState directly, reset PlayerPrefs values
                // that affect Annie's behavior
                PlayerPrefs.DeleteKey("TutorialMark");
                PlayerPrefs.DeleteKey("TutorialCheckpoint");
                PlayerPrefs.Save();
                
                // Try to disable Annie if she's active
                annieNPC.SetActive(false);
            }
            
            // Clear tutorial PlayerPrefs directly as a backup
            PlayerPrefs.DeleteKey("TutorialMark");
            PlayerPrefs.DeleteKey("TutorialCheckpoint");
            PlayerPrefs.DeleteKey("FirstMeetingMark");
            PlayerPrefs.Save();
            
            // Re-enable the button if we're not reloading the scene
            resetButton.interactable = true;
            if (resetFeedbackPanel != null)
            {
                resetFeedbackPanel.SetActive(false);
            }
        }
    }
}
