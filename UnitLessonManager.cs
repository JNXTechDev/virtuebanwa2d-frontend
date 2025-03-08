using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.UI;
using UnityEngine.Video;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using System;

public class UnitLessonManager : MonoBehaviour
{
    [System.Serializable]
    public class LessonData
    {
        public string lessonName;
        public string defaultScene;
        public bool isUnlocked;
        public string lastCheckpoint;
        // Removed status field as it's dynamic and fetched from database
    }

    // Keep enum for internal status tracking
    private enum LessonStatus
    {
        Locked,
        Available,
        InProgress,
        Completed
    }

    [System.Serializable]
    public class UnitData
    {
        public string unitName;
        public List<LessonData> lessons = new List<LessonData>();
        public bool isUnlocked;
    }

    [Header("UI References")]
    [SerializeField] private Transform unitButtonContainer;
    [SerializeField] private GameObject unitButtonPrefab;
    [SerializeField] private GameObject lessonSelectionPanel;
    [SerializeField] private Transform lessonButtonContainer;
    [SerializeField] private GameObject lessonButtonPrefab;
    [SerializeField] private TextMeshProUGUI usernameText;
    [SerializeField] private GameObject loadingPanel;
    [SerializeField] private TextMeshProUGUI loadingText;
    [SerializeField] private Button backButton;

    [Header("Retake UI")]
    public GameObject retakePanel;
    public Button retakeYesButton;
    public Button retakeNoButton;
    public TextMeshProUGUI retakeMessageText;

    private LessonData pendingLesson;
    private string pendingUnitName;

    [Header("Unit Data")]
    [SerializeField] private List<UnitData> units = new List<UnitData>();

    private string currentUsername;


 // Base URL for the API
    private string baseUrl => NetworkConfig.BaseUrl; 


    // Add a GameProgress class property to store loaded data
    private GameProgress progressData = null;

    private void OnEnable()
    {
        StartCoroutine(InitializeAfterDelay());

        if (backButton != null)
        {
            backButton.onClick.AddListener(OnBackButtonClicked);
        }
        else
        {
            Debug.LogError("Back Button reference is missing!");
        }
    }

    private IEnumerator InitializeAfterDelay()
    {
        while (PlayManager.Instance == null || string.IsNullOrEmpty(PlayManager.Instance.GetUsername()))
        {
            yield return new WaitForSeconds(0.1f);
        }

        Initialize();
    }

    private void Initialize()
    {
        try
        {
            Debug.Log("Initializing UnitLessonManager");

            currentUsername = PlayerPrefs.GetString("Username", "Unknown");
            Debug.Log($"[UnitLessonManager] Using stored username: {currentUsername}");

            if (usernameText != null)
            {
                usernameText.text = $"Player: {currentUsername}";
            }
            else
            {
                Debug.LogError("Username Text reference is missing!");
            }

            LoadUserProgress();
            SetupUnitButtons();
            lessonSelectionPanel.SetActive(false);
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error in Initialize: {ex.Message}\n{ex.StackTrace}");
        }
    }

    private void SetupUnitButtons()
    {
        foreach (UnitData unit in units)
        {
            GameObject buttonObj = Instantiate(unitButtonPrefab, unitButtonContainer);
            Button button = buttonObj.GetComponent<Button>();
            TextMeshProUGUI buttonText = buttonObj.GetComponentInChildren<TextMeshProUGUI>();

            buttonText.text = unit.unitName;
            button.onClick.AddListener(() => ShowLessonsForUnit(unit));

            button.interactable = unit.isUnlocked;
        }
    }

    private async Task LoadLesson(string unitName, LessonData lesson)
    {
        // Store pending lesson info
        pendingLesson = lesson;
        pendingUnitName = unitName;

        // Special case for Explore mode - skip completion check and just load
        bool isExploreMode = (lesson.lessonName == "Virtue Banwa" || lesson.lessonName.Contains("Explore"));
        if (isExploreMode)
        {
            StartLesson(); // Load the scene directly without checks
            return;
        }

        // Special check for tutorial
        if (unitName == "UNIT 1" && lesson.lessonName == "Tutorial")
        {
            // Check if tutorial is already completed
            bool isTutorialCompleted = await CheckTutorialCompletion();
            
            if (isTutorialCompleted)
            {
                ShowRetakePrompt("Tutorial", "");
                return;
            }
        }
        else 
        {
            // Check if regular lesson was already completed
            bool isCompleted = await CheckLessonCompletion(unitName, lesson.lessonName);
            
            if (isCompleted)
            {
                ShowRetakePrompt(unitName, lesson.lessonName);
                return;
            }
        }
        
        // Proceed with normal lesson load
        StartLesson();
    }

    // Add this new method to specifically check tutorial completion
    private async Task<bool> CheckTutorialCompletion()
    {
        try
        {
            using (HttpClient client = new HttpClient())
            {
                HttpResponseMessage response = await client.GetAsync($"{baseUrl}/game_progress/{currentUsername}");
                if (response.IsSuccessStatusCode)
                {
                    string content = await response.Content.ReadAsStringAsync();
                    Debug.Log($"CheckTutorialCompletion received: {content}");
                    
                    // Parse just the tutorial portion using Newtonsoft.Json
                    var jsonObject = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, object>>(content);
                    
                    if (jsonObject != null && jsonObject.TryGetValue("tutorial", out object tutorialObj))
                    {
                        var tutorialData = tutorialObj as Newtonsoft.Json.Linq.JObject;
                        
                        if (tutorialData != null && tutorialData.TryGetValue("status", out var status))
                        {
                            Debug.Log($"Tutorial status: {status}");
                            return status.ToString() == "Completed";
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error checking tutorial completion: {ex.Message}");
        }
        return false;
    }

    private void ShowLessonsForUnit(UnitData unit)
    {
        foreach (Transform child in lessonButtonContainer)
        {
            Destroy(child.gameObject);
        }

        foreach (LessonData lesson in unit.lessons)
        {
            GameObject buttonObj = Instantiate(lessonButtonPrefab, lessonButtonContainer);
            Button button = buttonObj.GetComponent<Button>();
            
            // Set lesson name text
            TextMeshProUGUI buttonText = buttonObj.transform.Find("LessonText")?.GetComponent<TextMeshProUGUI>();
            if (buttonText != null)
            {
                buttonText.text = lesson.lessonName;
            }
            else
            {
                Debug.LogError("LessonText component not found on lesson button prefab.");
            }
            
            // Find the status indicator and status text
            Transform statusIndicator = buttonObj.transform.Find("StatusIndicator");
            TextMeshProUGUI statusText = null;
            
            if (statusIndicator != null)
            {
                statusText = statusIndicator.Find("StatusText")?.GetComponent<TextMeshProUGUI>();
            }
            
            RectTransform rectTransform = buttonObj.GetComponent<RectTransform>();
            
            // Special case for "Explore" - "Virtue Banwa" lesson - hide status and always enable
            bool isExploreMode = (lesson.lessonName == "Virtue Banwa" || lesson.lessonName.Contains("Explore"));
            
            if (isExploreMode)
            {
                // Hide status indicator for explore mode
                if (statusIndicator != null)
                {
                    statusIndicator.gameObject.SetActive(false);
                }
                
                // Always enable the button for explore mode
                button.interactable = true;
                
                // Skip other status processing
                button.onClick.AddListener(async () => await LoadLesson(unit.unitName, lesson));
                
                // Make button visually distinct (optional)
                ColorBlock colors = button.colors;
                colors.normalColor = new Color(0.9f, 0.9f, 1.0f); // Light blue tint for explore
                button.colors = colors;
            }
            else
            {
                // Normal lesson processing
                LessonStatus status = GetLessonStatusFromDatabase(unit.unitName, lesson);
                button.interactable = status != LessonStatus.Locked;
                
                // Update status visuals
                UpdateButtonVisuals(button, status, statusText);
                
                // Add click handler
                button.onClick.AddListener(async () => await LoadLesson(unit.unitName, lesson));
            }
            
            // Force UI update
            LayoutRebuilder.ForceRebuildLayoutImmediate(rectTransform);
        }

        lessonSelectionPanel.SetActive(true);
    }
    
    private void UpdateButtonVisuals(Button button, LessonStatus status, TextMeshProUGUI statusText)
    {
        // Update button colors based on status
        ColorBlock colors = button.colors;
        switch (status)
        {
            case LessonStatus.Locked:
             //   colors.normalColor = new Color(0.5f, 0.5f, 0.5f); // Gray for locked
                break;
            case LessonStatus.Completed:
             //   colors.normalColor = new Color(0.8f, 1.0f, 0.8f); // Light green for completed
                break;
            case LessonStatus.InProgress:
             //   colors.normalColor = new Color(1.0f, 1.0f, 0.8f); // Light yellow for in progress
                break;
            default:
                //colors.normalColor = Color.black; // White for available
                break;
        }
        button.colors = colors;
        
        // Update status text if available
        if (statusText != null)
        {
            // Set the text based on status
            switch (status)
            {
                case LessonStatus.Locked:
                    statusText.text = "LOCKED";
                    //statusText.color = Color.gray;
                    break;
                case LessonStatus.Available:
                    statusText.text = "AVAILABLE";
                   // statusText.color = Color.white;
                    break;
                case LessonStatus.InProgress:
                    statusText.text = "IN PROGRESS";
                   // statusText.color = Color.yellow;
                    break;
                case LessonStatus.Completed:
                    statusText.text = "COMPLETED";
                 //   statusText.color = Color.green;
                    break;
            }
            
            // Force the text to be visible and update
            statusText.enabled = true;
            statusText.gameObject.SetActive(true);
            
            Debug.Log($"Updated StatusText to: {statusText.text}, Color: {statusText.color}");
            
            // Force the layout to update
            LayoutRebuilder.ForceRebuildLayoutImmediate(statusText.rectTransform);
        }
        else
        {
            Debug.LogWarning("StatusText is null, cannot update status display");
        }
    }

    private LessonStatus GetLessonStatusFromDatabase(string unitName, LessonData lesson)
    {
        // Format unit and lesson names to match API format
        string unitKey = unitName.Replace("UNIT ", "Unit").Replace(" ", "");
        string lessonKey = lesson.lessonName.Replace("Lesson ", "Lesson").Replace(" ", "");
        
        // Special handling for Tutorial
        if (lesson.lessonName == "Tutorial")
        {
            // Check if tutorial is completed in the database
            if (progressData?.tutorial?.status == "Completed")
            {
                lesson.isUnlocked = true;
                return LessonStatus.Completed;
            }
            
            // Check tutorial status in PlayerPrefs as a fallback
            bool tutorialCompleted = 
                PlayerPrefs.GetInt("TutorialJanica", 0) == 1 && 
                PlayerPrefs.GetInt("TutorialMark", 0) == 1 &&
                PlayerPrefs.GetInt("TutorialRojan", 0) == 1;
            
            if (tutorialCompleted)
            {
                lesson.isUnlocked = true;
                return LessonStatus.Completed;
            }
            else
            {
                // Tutorial is always available as the first content
                lesson.isUnlocked = true;
                return LessonStatus.Available;
            }
        }
        
        // For regular lessons, get status from our loaded progress data
        if (progressData != null && 
            progressData.units != null && 
            progressData.units.TryGetValue(unitKey, out var unitData) &&
            unitData.lessons != null &&
            unitData.lessons.TryGetValue(lessonKey, out var lessonData))
        {
            // Set status based on API data
            lesson.isUnlocked = lessonData.status != "Locked";
            
            switch (lessonData.status)
            {
                case "Completed":
                    return LessonStatus.Completed;
                case "InProgress":
                    return LessonStatus.InProgress;
                case "Available":
                    return LessonStatus.Available;
                case "Locked":
                default:
                    return LessonStatus.Locked;
            }
        }
        
        // If first lesson of Unit 1, make it available by default
        if (unitName == "UNIT 1" && lesson.lessonName == "Lesson 1")
        {
            lesson.isUnlocked = true;
            return LessonStatus.Available;
        }
        
        // Default to locked
        lesson.isUnlocked = false;
        return LessonStatus.Locked;
    }

    private void ShowRetakePrompt(string unitName, string lessonName)
    {
        if (retakePanel != null)
        {
            retakePanel.SetActive(true);
            if (retakeMessageText != null)
            {
                // Special message for tutorial
                if (unitName == "Tutorial" || (unitName == "UNIT 1" && lessonName == "Tutorial"))
                {
                    retakeMessageText.text = $"You have already completed the tutorial. Would you like to retake it?";
                }
                else
                {
                    retakeMessageText.text = $"You have already completed {unitName} - {lessonName}. Would you like to retake it?";
                }
            }
            
            // Setup button listeners
            if (retakeYesButton != null && retakeNoButton != null)
            {
                // Make sure we remove previous listeners first
                retakeYesButton.onClick.RemoveAllListeners();
                retakeNoButton.onClick.RemoveAllListeners();
                
                // Add fresh listeners
                retakeYesButton.onClick.AddListener(OnRetakeYesClicked);
                retakeNoButton.onClick.AddListener(OnRetakeNoClicked);
            }
        }
    }

    private void OnRetakeYesClicked()
    {
        retakePanel.SetActive(false);
        StartLesson(); // Use the stored pending lesson info
    }

    private void OnRetakeNoClicked()
    {
        retakePanel.SetActive(false);
        pendingLesson = null;
        pendingUnitName = null;
    }

    private async void StartLesson()
    {
        if (pendingLesson == null) return;

        loadingPanel.SetActive(true);
        loadingText.text = $"Loading {pendingUnitName} - {pendingLesson.lessonName}...";

        // Save current unit and lesson to PlayerPrefs
        PlayerPrefs.SetString("CurrentUnit", pendingUnitName);
        PlayerPrefs.SetString("CurrentLesson", pendingLesson.lessonName);
        PlayerPrefs.Save();

        // Add await here
        await SaveSceneTransitionProgress(currentUsername, pendingUnitName, pendingLesson.lessonName);
        
        SceneManager.LoadScene(pendingLesson.defaultScene);
    }

    private async Task<bool> CheckLessonCompletion(string unitName, string lessonName)
    {
        try
        {
            using (HttpClient client = new HttpClient())
            {
                HttpResponseMessage response = await client.GetAsync($"{baseUrl}/game_progress/{currentUsername}");
                if (response.IsSuccessStatusCode)
                {
                    string content = await response.Content.ReadAsStringAsync();
                    var progressData = JsonUtility.FromJson<GameProgress>(content);

                    // Extract unit number and lesson number
                    string unitKey = unitName.Replace("UNIT ", "Unit").Replace(" ", "");
                    string lessonKey = lessonName.Replace("Lesson ", "Lesson");

                    // Check if lesson exists and is completed
                    if (progressData?.units != null && 
                        progressData.units.ContainsKey(unitKey) && 
                        progressData.units[unitKey].lessons.ContainsKey(lessonKey))
                    {
                        return progressData.units[unitKey].lessons[lessonKey].status == "Completed";
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error checking lesson completion: {ex.Message}");
        }
        return false;
    }

    private async void LoadUserProgress()
    {
        loadingPanel.SetActive(true);
        loadingText.text = "Loading progress...";

        using (HttpClient client = new HttpClient())
        {
            try
            {
                HttpResponseMessage response = await client.GetAsync($"{baseUrl}/game_progress/{currentUsername}");
                if (response.IsSuccessStatusCode)
                {
                    string responseContent = await response.Content.ReadAsStringAsync();
                    Debug.Log($"Progress data loaded: {responseContent}");

                    // Store the loaded data in our class property
                    progressData = JsonUtility.FromJson<GameProgress>(responseContent);

                    if (progressData != null)
                    {
                        Debug.Log($"Tutorial Status: {progressData.tutorial?.status}, Reward: {progressData.tutorial?.reward}, Date: {progressData.tutorial?.date}");

                        // Check if units exists and has data
                        if (progressData.units != null)
                        {
                            foreach (var unit in progressData.units)
                            {
                                if (unit.Value?.lessons != null)
                                {
                                    foreach (var lesson in unit.Value.lessons)
                                    {
                                        Debug.Log($"Unit: {unit.Key}, Lesson: {lesson.Key}, Status: {lesson.Value.status}, Reward: {lesson.Value.reward}");
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        Debug.Log("No progress data loaded.");
                    }
                }
                else
                {
                    Debug.LogError($"Failed to load progress: {response.ReasonPhrase}");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error loading progress: {ex.Message}");
            }
            finally
            {
                loadingPanel.SetActive(false);
            }
        }
    }

    private async Task SaveSceneTransitionProgress(string username, string unitName, string lessonName)
    {
        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(unitName) || string.IsNullOrEmpty(lessonName))
        {
            Debug.LogError($"[SaveProgress] Invalid data - Username: {username}, Unit: {unitName}, Lesson: {lessonName}");
            return;
        }

        Debug.Log($"[SaveProgress] Saving progress - Username: {username}, Unit: {unitName}, Lesson: {lessonName}");

        var progressData = new GameProgressData
        {
            Username = username, // Changed from username to Username
            unit = unitName,
            lesson = lessonName
        };

        string json = JsonUtility.ToJson(progressData);
        Debug.Log($"[SaveProgress] Sending JSON: {json}");

        var content = new StringContent(json, Encoding.UTF8, "application/json");

        using (HttpClient client = new HttpClient())
        {
            HttpResponseMessage response = await client.PostAsync($"{baseUrl}/game_progress", content);
            
            string responseContent = await response.Content.ReadAsStringAsync();
            if (response.IsSuccessStatusCode)
            {
                Debug.Log($"[SaveProgress] Success. Response: {responseContent}");
            }
            else
            {
                Debug.LogError($"[SaveProgress] Failed - {response.ReasonPhrase}. Response: {responseContent}");
            }
        }
    }

    private void OnBackButtonClicked()
    {
        lessonSelectionPanel.SetActive(false);
        foreach (Transform child in lessonButtonContainer)
        {
            Destroy(child.gameObject);
        }
    }

    private void OnDestroy()
    {
        if (backButton != null)
        {
            backButton.onClick.RemoveListener(OnBackButtonClicked);
        }

        // Remove button listeners
        if (retakeYesButton != null)
            retakeYesButton.onClick.RemoveListener(OnRetakeYesClicked);
        if (retakeNoButton != null)
            retakeNoButton.onClick.RemoveListener(OnRetakeNoClicked);
    }

    private string SanitizeUsername(string username)
    {
        return username.Replace(" ", "_").Replace(".", "-");
    }
}