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
using UnityEngine.Networking; // Add this for UnityWebRequest
using VirtueBanwa.Progress;
using VirtueBanwa.Dialogue;

public class UnitLessonManager : MonoBehaviour
{
    // Enum for predefined unit names
    public enum UnitName
    {
        Tutorial,      // For the tutorial unit
        Unit1,
        Unit2,
        VirtueBanwa    // For the free roam unit
    }

    // Enum for predefined lesson names
    public enum LessonName
    {
        Tutorial,      // For the tutorial lesson
        PreTest,
        Lesson1,
        Lesson2,
        Lesson3,
        Lesson4,
        Lesson5,
        Lesson6,
        PostTest,
        Explore        // For the free roam lesson
    }

    [System.Serializable]
    public class LessonData
    {
        public LessonName lessonName; // Use enum for lesson names
        public string defaultScene;
        public bool isUnlocked;
        public string lastCheckpoint;
    }

    [System.Serializable]
    public class UnitData
    {
        public UnitName unitName; // Use enum for unit names
        public List<LessonData> lessons = new List<LessonData>();
        public bool isUnlocked;
    }

    [Header("UI References")]
    [SerializeField] private ScrollRect lessonScrollView;           
    [SerializeField] private Transform lessonButtonContainer;       
    [SerializeField] private GameObject lessonButtonPrefab;
    [SerializeField] private GameObject lessonSelectionPanel;      // Add this field
    [SerializeField] private TextMeshProUGUI usernameText;
    [SerializeField] private GameObject loadingPanel;
    [SerializeField] private TextMeshProUGUI loadingText;
    [SerializeField] private Button backButton;
    [SerializeField] private Button refreshButton; // Add this field for the refresh button

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
    private string baseUrl => NetworkConfig.BaseUrl;
    private bool isExploreMode = false;

    // Add progressData field
    private GameProgress progressData; // Keep this if needed for UI updates, but no longer fetch it independently

    private Dictionary<string, string> displayNameMap = new Dictionary<string, string>
    {
        { "Unit1", "Unit 1" },
        { "PreTest", "Pre Test" },
        { "Lesson1", "Lesson 1" },
        { "Lesson2", "Lesson 2" }
    };

    public string GetDisplayName(string backendName)
    {
        if (displayNameMap.TryGetValue(backendName, out string displayName))
        {
            return displayName;
        }
        return backendName; // Fallback to the original name if no mapping exists
    }

    private void Awake()
    {
        // Validate and fix container references
        if (lessonScrollView != null && lessonButtonContainer == null)
        {
            lessonButtonContainer = lessonScrollView.content;
            Debug.Log("Lesson button container auto-assigned to ScrollView content");
        }

        // Validate references
        if (lessonButtonContainer == null)
        {
            Debug.LogError("Lesson button container (Content) reference is missing!");
        }

        // Add listener for the refresh button
        if (refreshButton != null)
        {
            refreshButton.onClick.AddListener(RefreshUnitAndLessonManager);
        }
        else
        {
            Debug.LogError("Refresh Button reference is missing!");
        }
    }

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

    private async void Initialize()
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

            // Remove the call to LoadUserProgress
            // _ = LoadUserProgress();

            SetupLessonButtons();
            lessonSelectionPanel.SetActive(true);
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error in Initialize: {ex.Message}\n{ex.StackTrace}");
        }
    }

    //here
private void SetupLessonButtons()
{
    if (lessonButtonContainer == null)
    {
        Debug.LogError("Lesson button container is null!");
        return;
    }

    // Clear existing buttons
    foreach (Transform child in lessonButtonContainer)
    {
        Destroy(child.gameObject);
    }

    // Add the Tutorial button separately
    GameObject tutorialButtonObj = Instantiate(lessonButtonPrefab, lessonButtonContainer);
    Button tutorialButton = tutorialButtonObj.GetComponent<Button>();

    // Set the tutorial button text
    TextMeshProUGUI tutorialButtonText = tutorialButtonObj.transform.Find("LessonText")?.GetComponent<TextMeshProUGUI>();
    if (tutorialButtonText != null)
    {
        tutorialButtonText.text = "Tutorial - Tutorial"; // Unique identifier for the tutorial
    }
    else
    {
        Debug.LogError("LessonText component not found on tutorial button prefab.");
    }

    // Add click handler for the tutorial button
    tutorialButton.onClick.AddListener(async () => await LoadLesson("Tutorial", new LessonData
    {
        lessonName = LessonName.Tutorial,
        defaultScene = "Tutorial Outside" // Replace with your tutorial scene name
    }));

    // Add buttons for other units and lessons
    foreach (UnitData unit in units)
    {
        foreach (LessonData lesson in unit.lessons)
        {
            GameObject buttonObj = Instantiate(lessonButtonPrefab, lessonButtonContainer);
            Button button = buttonObj.GetComponent<Button>();

            // Set lesson name text with unit name as a prefix
            TextMeshProUGUI buttonText = buttonObj.transform.Find("LessonText")?.GetComponent<TextMeshProUGUI>();
            if (buttonText != null)
            {
                buttonText.text = $"{unit.unitName} - {lesson.lessonName}"; // Combine unit and lesson name
            }
            else
            {
                Debug.LogError("LessonText component not found on lesson button prefab.");
            }

            // Add click handler for other statuses to load the lesson
            button.onClick.AddListener(async () => await LoadLesson(unit.unitName.ToString(), lesson));
        }
    }
}
//end
    private async Task LoadLesson(string unitName, LessonData lesson)
    {
        Debug.Log($"[LoadLesson] Starting for {unitName} - {lesson.lessonName}");
        
        // Store pending lesson info for use in StartLesson
        pendingLesson = lesson;
        pendingUnitName = unitName;

        // Special case for Explore mode - skip completion check and just load
        bool isExploreMode = (lesson.lessonName == LessonName.Lesson1 || lesson.lessonName.ToString().Contains("Explore"));
        if (isExploreMode)
        {
            StartLesson();
            return;
        }

        // Let StartLesson handle all the completion checks
        StartLesson();
    }

    private async Task<bool> CheckTutorialCompletion()
    {
        try
        {
            // Use await properly with the async check
            string username = PlayerPrefs.GetString("Username", "DefaultPlayer");
            bool janicaCompleted = await DialogueState.IsDialogueCompleted(username, "Janica");
            bool markCompleted = await DialogueState.IsDialogueCompleted(username, "Mark");
            bool annieCompleted = await DialogueState.IsDialogueCompleted(username, "Annie");
            bool rojanCompleted = await DialogueState.IsDialogueCompleted(username, "Rojan");
            
            return janicaCompleted && markCompleted && annieCompleted && rojanCompleted;
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error checking tutorial completion: {ex.Message}");
            return false;
        }
    }

    private async Task<bool> CheckAllNPCsCompleted()
    {
        string username = PlayerPrefs.GetString("Username", "DefaultPlayer");
        bool janicaCompleted = await DialogueState.IsDialogueCompleted(username, "Janica");
        bool markCompleted = await DialogueState.IsDialogueCompleted(username, "Mark");
        bool annieCompleted = await DialogueState.IsDialogueCompleted(username, "Annie");
        bool rojanCompleted = await DialogueState.IsDialogueCompleted(username, "Rojan");
        
        return janicaCompleted && markCompleted && annieCompleted && rojanCompleted;
    }

    private void ShowRetakePrompt(string unitName, string lessonName)
    {
        Debug.Log($"[ShowRetakePrompt] SHOWING RETAKE PANEL for {unitName} - {lessonName}");
        
        // Force store the pending values
        if (unitName == "Tutorial" || lessonName == "Tutorial")
        {
            if (pendingLesson == null)
            {
                pendingLesson = new LessonData
                {
                    lessonName = LessonName.Lesson1,
                    defaultScene = "Tutorial Outside"
                };
            }
            if (string.IsNullOrEmpty(pendingUnitName))
            {
                pendingUnitName = "UNIT 1";
            }
            
            Debug.Log($"[ShowRetakePrompt] Set pendingLesson.lessonName={pendingLesson.lessonName}, pendingUnitName={pendingUnitName}");
        }
        
        if (retakePanel == null)
        {
            Debug.LogError("[ShowRetakePrompt] retakePanel is NULL! Cannot show retake panel!");
            return;
        }
        
        // Force deactivate and then reactivate to ensure proper initialization
        retakePanel.SetActive(false);
        
        // Force any CanvasGroup to be fully visible if present
        CanvasGroup canvasGroup = retakePanel.GetComponent<CanvasGroup>();
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
        }
        
        // Ensure the panel is in the hierarchy properly
        if (retakePanel.transform.parent != null)
        {
            // Make sure the parent is active
            retakePanel.transform.parent.gameObject.SetActive(true);
        }
        
        // Set the message before activating
        if (retakeMessageText != null)
        {
            if (unitName == "Tutorial" || (unitName == "UNIT 1" && lessonName == "Tutorial"))
            {
                retakeMessageText.text = $"You have already completed the tutorial. Would you like to retake it?";
            }
            else
            {
                retakeMessageText.text = $"You have already completed {unitName} - {lessonName}. Would you like to retake it?";
            }
            
            // Make sure the text is visible
            retakeMessageText.enabled = true;
        }
        else
        {
            Debug.LogError("[ShowRetakePrompt] retakeMessageText is null!");
        }
        
        // Setup buttons directly here to ensure they work
        if (retakeYesButton != null && retakeNoButton != null)
        {
            // Remove previous listeners
            retakeYesButton.onClick.RemoveAllListeners();
            retakeNoButton.onClick.RemoveAllListeners();
            
            // Add new listeners that directly reference member functions
            retakeYesButton.onClick.AddListener(OnRetakeYesClicked);
            retakeNoButton.onClick.AddListener(OnRetakeNoClicked);
            
            // Ensure the buttons are active and properly enabled
            retakeYesButton.gameObject.SetActive(true);
            retakeNoButton.gameObject.SetActive(true);
            retakeYesButton.interactable = true;
            retakeNoButton.interactable = true;
        }
        else
        {
            Debug.LogError("[ShowRetakePrompt] One or both retake buttons are NULL!");
        }
        
        // Force set panel active last
        retakePanel.SetActive(true);
        
        // Verify the panel was activated
        Debug.Log($"[ShowRetakePrompt] Retake panel active: {retakePanel.activeSelf}");
    }

    private void OnRetakeYesClicked()
    {
        Debug.Log("[OnRetakeYesClicked] Yes button clicked, processing retake action");
        retakePanel.SetActive(false);

        // Special handling for tutorial retake - FIX THE CONDITION HERE
        if (pendingUnitName == "UNIT 1" && (pendingLesson?.lessonName == LessonName.Lesson1 || pendingLesson?.lessonName == LessonName.Lesson1))
        {
            Debug.Log("[OnRetakeYesClicked] Resetting tutorial progress before retaking...");
            StartCoroutine(ResetTutorialProgress());
        }
        else if (pendingUnitName == "Tutorial" || (pendingLesson != null && pendingLesson.lessonName == LessonName.Lesson1))
        {
            // Additional case for when unitName is just "Tutorial"
            Debug.Log("[OnRetakeYesClicked] Resetting tutorial progress (alternative path)...");
            StartCoroutine(ResetTutorialProgress());
        }
        else
        {
            // For regular lessons, just load the scene directly
            // ...existing code...
            if (pendingLesson != null)
            {
                // Bypass all checks and load directly
                loadingPanel.SetActive(true);
                loadingText.text = $"Loading {pendingUnitName} - {pendingLesson.lessonName}...";
                
                // Save to PlayerPrefs
                PlayerPrefs.SetString("CurrentUnit", pendingUnitName);
                PlayerPrefs.SetString("CurrentLesson", pendingLesson.lessonName.ToString());
                PlayerPrefs.Save();
                
                // Directly load the scene
                SceneManager.LoadScene(pendingLesson.defaultScene);
            }
        }
    }

    private void OnRetakeNoClicked()
    {
        retakePanel.SetActive(false);
        pendingLesson = null;
        pendingUnitName = null;
    }

    
    private IEnumerator ResetTutorialProgress()
    {
        Debug.Log("[ResetTutorialProgress] *** STARTING TUTORIAL RESET PROCESS ***");
        loadingPanel.SetActive(true);
        loadingText.text = "Resetting tutorial progress...";

        // Get current player username for clearing player-specific states
        string playerUsername = PlayerPrefs.GetString("Username", "DefaultPlayer");
        Debug.Log($"[ResetTutorialProgress] Resetting tutorial for user: {playerUsername}");

        // 1. Reset local tutorial states first - be more thorough
        string[] tutorialKeys = {
            "TutorialJanica", "TutorialMark", "TutorialCheckpoint", "TutorialRojan",
            "TutorialAnnie", "FirstMeetingMark", "GameMode", "Tutorial_Completed"
        };
        
        foreach (string key in tutorialKeys)
        {
            if (PlayerPrefs.HasKey(key))
            {
                Debug.Log($"[ResetTutorialProgress] Deleting PlayerPrefs key: {key}");
                PlayerPrefs.DeleteKey(key);
            }
        }
        
        // 2. Reset tutorial dialogue states for this player specifically
        string[] npcNames = { "Janica", "Mark", "Annie", "Rojan" };
        foreach (string npcName in npcNames)
        {
            // Check both formats of keys
            string[] keyFormats = {
                $"{playerUsername}_Completed_{npcName}",
                $"{playerUsername}_{npcName}_Completed",
                $"{playerUsername}_{npcName}_Tutorial"
            };
            
            foreach (string key in keyFormats)
            {
                if (PlayerPrefs.HasKey(key))
                {
                    Debug.Log($"[ResetTutorialProgress] Deleting NPC completion key: {key}");
                    PlayerPrefs.DeleteKey(key);
                }
            }
        }
        
        // Apply PlayerPrefs changes
        PlayerPrefs.Save();
        Debug.Log("[ResetTutorialProgress] All PlayerPrefs keys cleared");

        // 3. Reset dialogue state in DialogueState helper - UPDATED to handle async in coroutine
        yield return StartCoroutine(ResetDialogueStatesCoroutine(playerUsername, npcNames));

        // 4. Reset progress in the database
        bool resetSuccess = false;
        for (int attempts = 0; attempts < 3 && !resetSuccess; attempts++) {
            // Use UnityWebRequest instead of HttpClient to avoid await
            using (UnityWebRequest request = UnityWebRequest.Delete($"{baseUrl}/game_progress/{currentUsername}/tutorial"))
            {
                // Set content type for DELETE request
                request.SetRequestHeader("Content-Type", "application/json");
                // Ensure we have a valid download handler
                request.downloadHandler = new DownloadHandlerBuffer();
                
                Debug.Log($"Sending tutorial reset request to: {baseUrl}/game_progress/{currentUsername}/tutorial");
                yield return request.SendWebRequest();
                
                if (request.result == UnityWebRequest.Result.Success)
                {
                    string responseContent = request.downloadHandler.text;
                    Debug.Log($"Tutorial reset response: {request.responseCode} - {responseContent}");
                    
                    // Reset locally stored progress data too
                    if (progressData != null)
                    {
                        if (progressData.tutorial != null)
                        {
                            progressData.tutorial.status = "Not Started";
                            progressData.tutorial.reward = "";
                            // Clear checkpoints dictionary
                            if (progressData.tutorial.checkpoints != null)
                            {
                                progressData.tutorial.checkpoints.Clear();
                            }
                        }
                        
                        if (progressData.units != null && 
                            progressData.units.ContainsKey("Unit1") && 
                            progressData.units["Unit1"].lessons != null &&
                            progressData.units["Unit1"].lessons.ContainsKey("Lesson1"))
                        {
                            // Update NPCs talked to list
                            var lesson = progressData.units["Unit1"].lessons["Lesson1"];
                            if (lesson.npcsTalkedTo != null)
                            {
                                lesson.npcsTalkedTo.Clear();
                            }
                        }
                    }
                    
                    resetSuccess = true;
                }
                else
                {
                    Debug.LogError($"Failed to reset tutorial progress on server: {request.responseCode}");
                    Debug.LogError($"Error: {request.error}");
                    yield return new WaitForSeconds(0.5f);
                }
            }
        }
        
        // 5. Reset NPC states by finding all NPCDialogueTriggers and calling CheckRewardStatus
        NPCDialogueTrigger[] allTriggers = FindObjectsOfType<NPCDialogueTrigger>();
        foreach (var trigger in allTriggers)
        {
            trigger.SendMessage("CheckRewardStatus", SendMessageOptions.DontRequireReceiver);
        }
        
        // Force reload progress data regardless of reset success
        yield return StartCoroutine(LoadProgressAfterResetCoroutine());
        
        // 6. Now load the tutorial scene
        loadingText.text = "Loading tutorial...";
        yield return new WaitForSeconds(0.5f);

        Debug.Log($"[ResetTutorialProgress] Loading tutorial scene: {pendingLesson?.defaultScene}");
        
        if (pendingLesson != null && !string.IsNullOrEmpty(pendingLesson.defaultScene))
        {
            SceneManager.LoadScene(pendingLesson.defaultScene);
        }
        else
        {
            Debug.LogError("[ResetTutorialProgress] Cannot load scene: pendingLesson is null or defaultScene is empty");
            
            // Try to load a fallback scene
            string fallbackScene = "Tutorial Outside";
            Debug.Log($"[ResetTutorialProgress] Attempting to load fallback scene: {fallbackScene}");
            SceneManager.LoadScene(fallbackScene);
        }
    }

    // New helper coroutine to handle async DialogueState tasks
    private IEnumerator ResetDialogueStatesCoroutine(string username, string[] npcNames)
    {
        Debug.Log($"[ResetDialogueStatesCoroutine] Resetting all dialogue states for user: {username}");
        
        // Create a simple operation to reset all dialogue states
        var resetAllOperation = DialogueState.ResetAllDialogueStates(username);
        while (!resetAllOperation.IsCompleted)
        {
            yield return null;
        }
        
        Debug.Log("[ResetDialogueStatesCoroutine] All dialogue states reset via DialogueState.ResetAllDialogueStates");
        
        // Reset specific NPC dialogues one by one
        foreach (string npcName in npcNames)
        {
            Debug.Log($"[ResetDialogueStatesCoroutine] Resetting dialogue state for NPC: {npcName}");
            
            // Reset the base NPC name
            var resetOperation = DialogueState.ResetDialogueState(username, npcName);
            while (!resetOperation.IsCompleted)
            {
                yield return null;
            }
            
            // Reset the NPC_Tutorial name format
            var resetTutorialOperation = DialogueState.ResetDialogueState(username, $"{npcName}_Tutorial");
            while (!resetTutorialOperation.IsCompleted)
            {
                yield return null;
            }
        }
        
        Debug.Log("[ResetDialogueStatesCoroutine] All NPC dialogue states reset successfully");
    }

    private async void StartLesson()
    {
        if (pendingLesson == null) return;
        
        // NEW: Check if the lesson is completed before loading
        if (pendingLesson.lessonName == LessonName.Lesson1)
        {
            // Check if tutorial is completed
            if (progressData?.tutorial?.status == "Completed")
            {
                // Don't proceed with scene loading, show retake panel instead
                Debug.Log("[StartLesson] Tutorial is completed, showing retake prompt instead of loading scene");
                ShowRetakePrompt("Tutorial", "");
                return;
            }
        }
        else
        {
            // For regular lessons, check completion status
            string unitKey = pendingUnitName.Replace("UNIT ", "Unit").Replace(" ", "");
            string lessonKey = pendingLesson.lessonName.ToString();
            
            if (progressData?.units != null && 
                progressData.units.TryGetValue(unitKey, out var unitData) &&
                unitData.lessons != null &&
                unitData.lessons.TryGetValue(lessonKey, out var lessonData) &&
                lessonData.status == "Completed")
            {
                // Don't proceed with scene loading, show retake panel instead
                Debug.Log($"[StartLesson] {pendingUnitName} - {pendingLesson.lessonName} is completed, showing retake prompt");
                ShowRetakePrompt(pendingUnitName, pendingLesson.lessonName.ToString());
                return;
            }
        }

        // If we got here, the lesson is not completed (or we're retaking it), so continue with loading
        loadingPanel.SetActive(true);
        loadingText.text = $"Loading {pendingUnitName} - {pendingLesson.lessonName}...";

        // Save current unit and lesson to PlayerPrefs
        PlayerPrefs.SetString("CurrentUnit", pendingUnitName);
        PlayerPrefs.SetString("CurrentLesson", pendingLesson.lessonName.ToString());
        PlayerPrefs.Save();

        // Add await here
        await SaveSceneTransitionProgress(currentUsername, pendingUnitName, pendingLesson.lessonName.ToString());
        
        // Now load the scene
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

    private async Task SaveSceneTransitionProgress(string username, string unitName, string lessonName)
    {
        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(unitName) || string.IsNullOrEmpty(lessonName))
        {
            Debug.LogError($"[SaveProgress] Invalid data - Username: {username}, Unit: {unitName}, Lesson: {lessonName}");
            return;
        }

        Debug.Log($"[SaveProgress] Saving progress - Username: {username}, Unit: {unitName}, Lesson: {lessonName}");

        var progressData = new ProgressUpdateData
        {
            Username = username,
            UnitName = unitName,
            LessonName = lessonName  // Now this field exists in the class
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

        // Remove listener for the refresh button
        if (refreshButton != null)
        {
            refreshButton.onClick.RemoveListener(RefreshUnitAndLessonManager);
        }
    }

    private string SanitizeUsername(string username)
    {
        return username.Replace(" ", "_").Replace(".", "-");
    }

    private async Task CheckProgress()
    {
        string username = PlayerPrefs.GetString("Username", "");
        if (string.IsNullOrEmpty(username))
        {
            Debug.LogError("No username found in PlayerPrefs");
            return;
        }

        bool isTutorialComplete = await DialogueState.IsDialogueCompleted(username, "Rojan");
        if (!isTutorialComplete)
        {
            Debug.Log("Tutorial not completed yet - locking lessons");
            LockAllLessons();
            return;
        }
        
        // Tutorial is complete, unlock first lesson
        UnlockFirstLesson();
    }

    private void LockAllLessons()
    {
        foreach (var unit in units)
        {
            foreach (var lesson in unit.lessons)
            {
                lesson.isUnlocked = false;
            }
        }
        UpdateLessonUI();
    }

    private void UnlockFirstLesson()
    {
        if (units.Count > 0 && units[0].lessons.Count > 0)
        {
            units[0].lessons[0].isUnlocked = true;
            UpdateLessonUI();
        }
    }

    private void UpdateLessonUI()
    {
        // Clear existing buttons
        foreach (Transform child in lessonButtonContainer)
        {
            Destroy(child.gameObject);
        }

        // Recreate lesson buttons with updated unlock status
        foreach (var unit in units)
        {
            foreach (var lesson in unit.lessons)
            {
                // Create button and set status
                GameObject buttonObj = Instantiate(lessonButtonPrefab, lessonButtonContainer);
                Button button = buttonObj.GetComponent<Button>();
                button.interactable = lesson.isUnlocked;
                
                // Update button visuals based on status
                TextMeshProUGUI buttonText = buttonObj.GetComponentInChildren<TextMeshProUGUI>();
                if (buttonText != null)
                {
                    buttonText.text = lesson.lessonName.ToString(); // Use enum for lesson names
                }
            }
        }
    }

    private void ResetNPCDialogues()
    {
        string username = PlayerPrefs.GetString("Username", "DefaultPlayer");
        DialogueState.ResetDialogueState(username, "Janica_Tutorial");
        DialogueState.ResetDialogueState(username, "Mark_Tutorial");
        DialogueState.ResetDialogueState(username, "Annie_Tutorial");
        DialogueState.ResetDialogueState(username, "Rojan_Tutorial");
    }

    public void RefreshUnitAndLessonManager()
    {
        // Remove the call to RefreshUnitAndLessonManagerAsync
        // _ = RefreshUnitAndLessonManagerAsync();
        Debug.Log("RefreshUnitAndLessonManager is no longer needed.");
    }

    // Remove the RefreshUnitAndLessonManagerAsync method
    // Remove this method entirely
    /*
    private async Task RefreshUnitAndLessonManagerAsync()
    {
        // This method is no longer needed since MainView.cs handles refreshing progress data
    }
    */

    // Add a debug method to log the current status of the tutorial
    private void DebugTutorialStatus()
    {
        if (progressData?.tutorial != null)
        {
            Debug.Log($"Tutorial status: {progressData.tutorial.status}");
            
            if (progressData.tutorial.checkpoints != null)
            {
                Debug.Log("Tutorial checkpoints:");
                foreach (var checkpoint in progressData.tutorial.checkpoints)
                {
                    Debug.Log($"- {checkpoint.Key}: {checkpoint.Value.status}");
                }
            }
        }
        else
        {
            Debug.Log("No tutorial progress data available");
        }
    }
    private IEnumerator LoadProgressAfterResetCoroutine()
    {
        using (UnityWebRequest request = UnityWebRequest.Get($"{baseUrl}/game_progress/{currentUsername}"))
        {
            yield return request.SendWebRequest();
            
            if (request.result == UnityWebRequest.Result.Success)
            {
                string responseContent = request.downloadHandler.text;
                Debug.Log($"Reloaded progress data after reset: {responseContent}");

                // Store the loaded data in our class property
                progressData = JsonUtility.FromJson<GameProgress>(responseContent);
            }
            else
            {
                Debug.LogError($"Error reloading tutorial progress: {request.error}");
            }
        }
    }
    // Add this method to show the retake prompt forcefully for debugging
    public void ShowRetakePanel(string lessonType = "Tutorial")
    {
        if (retakePanel != null)
        {
            Debug.Log($"Forcefully showing retake panel for {lessonType}");
            retakePanel.SetActive(true);
            
            if (retakeMessageText != null)
            {
                retakeMessageText.text = $"You have already completed the {lessonType}. Would you like to retake it?";
            }

            // Setup button listeners
            if (retakeYesButton != null && retakeNoButton != null)
            {
                // Make sure we remove previous listeners first
                retakeYesButton.onClick.RemoveAllListeners();
                retakeNoButton.onClick.RemoveAllListeners();
                
                retakeYesButton.onClick.AddListener(() => {
                    retakePanel.SetActive(false);
                    pendingLesson = new LessonData { 
                        lessonName = LessonName.Lesson1, 
                        defaultScene = "Tutorial Outside" 
                    };
                    pendingUnitName = "UNIT 1";
                    StartCoroutine(ResetTutorialProgress());
                });
                
                retakeNoButton.onClick.AddListener(() => {
                    retakePanel.SetActive(false);
                });
            }
        }
        else
        {
            Debug.LogError("Retake panel is null - cannot show retake prompt");
        }
    }


//here
public void UpdateLessonStatusIndicator(string unitName, string lessonName, string status)
{
    Debug.Log($"[UpdateLessonStatusIndicator] Unit: {unitName}, Lesson: {lessonName}, Status: {status}");

    // Handle Tutorial - Tutorial separately
    if (unitName == "Tutorial" && lessonName == "Tutorial")
    {
        // Find the tutorial button by its unique identifier
        string tutorialIdentifier = "Tutorial - Tutorial";
        foreach (Transform child in lessonButtonContainer)
        {
            TextMeshProUGUI buttonText = child.Find("LessonText")?.GetComponent<TextMeshProUGUI>();
            if (buttonText != null && buttonText.text == tutorialIdentifier)
            {
                // Find the status indicator and update its text
                Transform statusIndicator = child.Find("StatusIndicator");
                TextMeshProUGUI statusText = statusIndicator?.Find("StatusText")?.GetComponent<TextMeshProUGUI>();
                if (statusText != null)
                {
                    statusText.text = status.ToUpper();
                    Debug.Log($"Updated status for Tutorial - Tutorial to {status.ToUpper()}");
                }

                // Disable the button if the status is Locked
                Button button = child.GetComponent<Button>();
                if (button != null)
                {
                    button.interactable = status != "Locked";
                    Debug.Log($"Button for Tutorial - Tutorial is now {(button.interactable ? "enabled" : "disabled")}");

                    // Add click listener to handle "Completed" status
                    button.onClick.RemoveAllListeners();
                    button.onClick.AddListener(() =>
                    {
                        if (status == "Completed")
                        {
                            ShowRetakePrompt("Tutorial", "Tutorial");
                        }
                        else
                        {
                            StartLesson();
                        }
                    });
                }

                return;
            }
        }
    }

    // Handle Virtue Banwa - Explore (no status indicator)
    if (unitName == "VirtueBanwa" && lessonName == "Explore")
    {
        // No status indicator needed, player can enter anytime
        return;
    }

    // Handle regular lessons and post-tests
    string combinedIdentifier = $"{unitName} - {lessonName}";
    foreach (Transform child in lessonButtonContainer)
    {
        TextMeshProUGUI buttonText = child.Find("LessonText")?.GetComponent<TextMeshProUGUI>();
        if (buttonText != null && buttonText.text == combinedIdentifier)
        {
            // Find the status indicator and update its text
            Transform statusIndicator = child.Find("StatusIndicator");
            TextMeshProUGUI statusText = statusIndicator?.Find("StatusText")?.GetComponent<TextMeshProUGUI>();
            if (statusText != null)
            {
                statusText.text = status.ToUpper();
                Debug.Log($"Updated status for {unitName} - {lessonName} to {status.ToUpper()}");
            }

            // Disable the button if the status is Locked
            Button button = child.GetComponent<Button>();
            if (button != null)
            {
                button.interactable = status != "Locked";
                Debug.Log($"Button for {unitName} - {lessonName} is now {(button.interactable ? "enabled" : "disabled")}");
            }

            return;
        }
    }

    Debug.LogWarning($"Lesson button not found for Unit: {unitName}, Lesson: {lessonName}");
}
    //end here

    // Only keep these classes if they're not defined elsewhere
    [System.Serializable]
    public class ProgressUpdateData
    {
        public string Username;
        public string UnitName;
        public string LessonName; // Add this field that was missing
    }
}
