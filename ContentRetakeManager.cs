using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine.SceneManagement;
using TMPro;

public class ContentRetakeManager : MonoBehaviour
{
    [Header("UI References")]
    public GameObject retakePanel;
    public Button yesButton;
    public Button noButton;
    public GameObject lessonSelectionBook;
    public TextMeshProUGUI retakeMessageText;  // For customizing the message

    [Header("Configuration")]
    [Tooltip("Default scene for tutorial retake")]
    public string tutorialScene = "Tutorial/Tutorial Outside";
    [Tooltip("Default scene prefix for unit lessons")]
    public string unitScenePrefix = "Unit";
    [Tooltip("Delay before checking content status")]
    public float checkDelay = 0.5f; 
    [Tooltip("Custom message for tutorial retake")]
    public string tutorialMessage = "You have already completed the tutorial. Would you like to retake it?";
    [Tooltip("Custom message for lesson retake")]
    public string lessonMessage = "You have already completed this lesson. Would you like to retake it?";
    
        private const string baseUrl = "http://192.168.43.149:5000/api"; // Updated URL
    
    private bool contentCompleted = false;
    private string contentType = "tutorial";  // "tutorial", "lesson1", etc.
    private string contentScene = "";         // Scene path to load if retaking

    private void OnEnable()
    {
        // Each time the content selection panel is activated, check status
        if (lessonSelectionBook != null && lessonSelectionBook.activeInHierarchy)
        {
            StartCoroutine(CheckContentStatus());
        }
    }

    void Start()
    {
        // Initialize UI
        if (retakePanel != null)
        {
            retakePanel.SetActive(false);
        }
        
        // Set up button listeners
        if (yesButton != null)
        {
            yesButton.onClick.AddListener(OnYesClicked);
        }
        
        if (noButton != null)
        {
            noButton.onClick.AddListener(OnNoClicked);
        }
    }
    
    // Called when the lesson book panel opens
    public void OnLessonBookOpened()
    {
        StartCoroutine(CheckContentStatus());
    }

    // Public method to check specific content (tutorial or lesson)
    public void CheckContent(string type, string scenePath = "")
    {
        contentType = type.ToLower();  // normalize to lowercase
        contentScene = scenePath;
        StartCoroutine(CheckContentStatus());
    }

    private IEnumerator CheckContentStatus()
    {
        // Wait a short delay to ensure everything is loaded
        yield return new WaitForSeconds(checkDelay);
        
        // Check the content status from the database
        CheckCompletionFromDB();
    }

    private async void CheckCompletionFromDB()
    {
        string username = PlayerPrefs.GetString("Username", "Unknown");
        
        try
        {
            var progress = await FetchPlayerProgress(username);
            
            // Check if the content is completed based on content type
            contentCompleted = IsContentCompleted(progress, contentType);
            
            if (contentCompleted)
            {
                ShowRetakePanel(contentType);
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error checking content status: {e.Message}");
        }
    }

    private async Task<Dictionary<string, object>> FetchPlayerProgress(string username)
    {
        using (var client = new HttpClient())
        {
            var response = await client.GetAsync($"{baseUrl}/game_progress/{username}");
            
            if (response.IsSuccessStatusCode)
            {
                string responseContent = await response.Content.ReadAsStringAsync();
                Debug.Log($"Progress data received: {responseContent}");
                
                var progress = JsonConvert.DeserializeObject<Dictionary<string, object>>(responseContent);
                return progress;
            }
            else
            {
                Debug.LogWarning($"Failed to get player progress: {response.StatusCode}");
                return new Dictionary<string, object>();
            }
        }
    }

    private bool IsContentCompleted(Dictionary<string, object> progress, string type)
    {
        // Handle tutorial case
        if (type == "tutorial")
        {
            if (progress.TryGetValue("tutorial", out var tutorialObj) && 
                tutorialObj is Newtonsoft.Json.Linq.JObject tutorialData)
            {
                // Check if the tutorial status is "Completed"
                if (tutorialData.TryGetValue("status", out var statusToken) && 
                    statusToken.ToString() == "Completed")
                {
                    Debug.Log("Tutorial is marked as completed in the database");
                    return true;
                }
                
                // Alternative check: look for completion of specific NPCs
                if (tutorialData.TryGetValue("checkpoints", out var checkpointsToken) && 
                    checkpointsToken is Newtonsoft.Json.Linq.JObject checkpoints)
                {
                    bool hasMarkCompleted = checkpoints.ContainsKey("Mark");
                    bool hasAnnieCompleted = checkpoints.ContainsKey("Annie");
                    
                    if (hasMarkCompleted && hasAnnieCompleted)
                    {
                        Debug.Log("Tutorial is completed (Mark and Annie checkpoints found)");
                        return true;
                    }
                }
            }
        }
        // Handle unit/lesson cases (format: "unit1lesson1", "unit2lesson3", etc.)
        else if (type.StartsWith("unit"))
        {
            // Parse unit and lesson from the type string
            string unitKey = $"Unit{type[4]}"; // e.g., "Unit1"
            string lessonKey = $"Lesson{type[type.IndexOf("lesson") + 6]}"; // e.g., "Lesson1"
            
            if (progress.TryGetValue("units", out var unitsObj) && 
                unitsObj is Newtonsoft.Json.Linq.JObject unitsData)
            {
                // Check if the specific unit exists
                if (unitsData.TryGetValue(unitKey, out var unitData) && 
                    unitData is Newtonsoft.Json.Linq.JObject unitObj)
                {
                    // Check if lessons data exists
                    if (unitObj.TryGetValue("lessons", out var lessonsData) && 
                        lessonsData is Newtonsoft.Json.Linq.JObject lessonsObj)
                    {
                        // Check if the specific lesson exists and is completed
                        if (lessonsObj.TryGetValue(lessonKey, out var lessonData) && 
                            lessonData is Newtonsoft.Json.Linq.JObject lessonObj)
                        {
                            if (lessonObj.TryGetValue("status", out var statusToken) && 
                                statusToken.ToString() == "Completed")
                            {
                                Debug.Log($"{unitKey} {lessonKey} is marked as completed");
                                return true;
                            }
                        }
                    }
                }
            }
        }
        
        Debug.Log($"Content '{type}' is not completed yet");
        return false;
    }

    private void ShowRetakePanel(string type)
    {
        if (retakePanel != null)
        {
            // Set the appropriate message based on content type
            if (retakeMessageText != null)
            {
                if (type == "tutorial")
                {
                    retakeMessageText.text = tutorialMessage;
                }
                else
                {
                    retakeMessageText.text = lessonMessage;
                }
            }
            
            retakePanel.SetActive(true);
        }
    }

    private void OnYesClicked()
    {
        Debug.Log($"User chose to retake {contentType}");
        
        // Hide the retake panel
        retakePanel.SetActive(false);
        
        // Determine which scene to load
        string sceneToLoad = DetermineSceneToLoad();
        
        // Load the appropriate scene
        if (!string.IsNullOrEmpty(sceneToLoad))
        {
            // Use SceneTransition if available
            SceneTransition sceneTransition = FindObjectOfType<SceneTransition>();
            if (sceneTransition != null)
            {
                sceneTransition.StartTransition(sceneToLoad);
            }
            else
            {
                // Direct scene loading if no transition component found
                SceneManager.LoadScene(sceneToLoad);
            }
        }
    }

    private string DetermineSceneToLoad()
    {
        // Use explicitly provided scene path if available
        if (!string.IsNullOrEmpty(contentScene))
        {
            return contentScene;
        }
        
        // Otherwise determine based on content type
        if (contentType == "tutorial")
        {
            return tutorialScene;
        }
        else if (contentType.StartsWith("unit"))
        {
            // Parse unit and lesson numbers
            char unitNumber = contentType[4];
            char lessonNumber = contentType[contentType.IndexOf("lesson") + 6];
            
            // Construct scene path based on convention (adjust as needed)
            return $"{unitScenePrefix}{unitNumber}/Lesson{lessonNumber}";
        }
        
        Debug.LogError($"Could not determine scene to load for content type: {contentType}");
        return "";
    }

    private void OnNoClicked()
    {
        Debug.Log("User chose not to retake the content");
        
        // Simply hide the retake panel
        retakePanel.SetActive(false);
    }
}
