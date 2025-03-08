using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using TMPro;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Linq;
using VirtueBanwa;

/// <summary>
/// Comprehensive tutorial manager that handles Mark and Annie NPC interactions,
/// dialogue management, and database synchronization in one place.
/// 
/// This script consolidates functionality from:
/// - TutorialSceneSetup
/// - TutorialManager
/// - NPCDialogueCoordinator
/// - AnnieDialogueFixer
/// - AnnieRewardFixer
/// </summary>
public class TutorialManager : MonoBehaviour
{
    [Header("NPC References")]
    [SerializeField] private GameObject janicaNPC;
    [SerializeField] private GameObject markNPC;
    [SerializeField] private GameObject annieNPC;
    [SerializeField] private GameObject rojanNPC;  // This might be null in outdoor scene
    
    [Header("UI Components")]
    [SerializeField] private GameObject rewardPanel;
    [SerializeField] private Image rewardImage;
    [SerializeField] private TextMeshProUGUI rewardTitleText;
    [SerializeField] private TextMeshProUGUI congratsText;
    [SerializeField] private TextMeshProUGUI debugText;
    
    [Header("Navigation")]
    [SerializeField] private GameObject navigationArrow;
    [SerializeField] private Transform janicaPosition;
    [SerializeField] private Transform markPosition;
    [SerializeField] private Transform anniePosition;
    [SerializeField] private Transform buildingEntrancePosition;
    [SerializeField] private Transform rojanPosition;  // This might be null in outdoor scene
    
    [Header("Configuration")]
    [SerializeField] private bool enableDebugMode = true;
    [SerializeField] private bool resetStateOnStart = false;
    
    // Annie's correct reward data
    private string annieRewardSprite = "TwoStar";
    private string annieRewardMessage = "Tutorial: Meeting Annie";
    private string annieResponseText = "Great! Just walk up to the entrance and you'll be transported inside. Good luck!";
    
    // NPC dialogue tracking
    private NPCscript activeNPC = null;
    private List<string> completedNPCs = new List<string>();
    
    // API settings
    //private const string baseUrl = "http://192.168.1.11:5000/api";
                private const string baseUrl = "http://192.168.43.149:5000/api"; // Updated URL

    
    // Delegate for dialogue events
    public delegate void DialogueStartedDelegate(NPCscript activeNPC);
    public event DialogueStartedDelegate OnDialogueStarted;
    
    // Singleton pattern
    public static TutorialManager Instance { get; private set; }
    
    private void Awake()
    {
        // Singleton setup
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        // Find NPCs if not assigned
        if (janicaNPC == null)
            janicaNPC = GameObject.Find("NPC Janica Tutorial");
            
        if (markNPC == null)
            markNPC = GameObject.Find("NPC Mark Tutorial");
            
        if (annieNPC == null)
            annieNPC = GameObject.Find("NPC Annie Tutorial");
            
        if (rojanNPC == null)
            rojanNPC = GameObject.Find("NPC Rojan Tutorial");
            
        if (markNPC == null || annieNPC == null)
            Debug.LogError("Tutorial NPCs not found in scene! Please assign them in the inspector.");
            
        // Find UI components if not assigned
        FindUIComponents();
        
        // Reset tutorial state if needed (for testing purposes)
        if (resetStateOnStart)
        {
            ResetTutorialState();
        }
    }
    
    private void Start()
    {
        // Setup initial tutorial state
        SetupTutorialState();
        
        // Start monitoring for tutorials
        StartCoroutine(MonitorTutorialProgress());
        
        if (enableDebugMode && debugText != null)
        {
            StartCoroutine(RefreshDebugText());
        }
        else if (debugText != null)
        {
            debugText.gameObject.SetActive(false);
        }
    }
    
    private void SetupTutorialState()
    {
        // Start by loading saved tutorial progress
        StartCoroutine(LoadTutorialProgress());
        
        // Make sure Mark is always active at start
        if (markNPC != null)
        {
            markNPC.SetActive(true);
            
            // Ensure Mark has correct dialogue
            NPCscript markScript = markNPC.GetComponent<NPCscript>();
            if (markScript != null)
            {
                TextAsset markDialogue = Resources.Load<TextAsset>("DialogueData/TutorialMark");
                if (markDialogue != null && markScript.dialogueFile != markDialogue)
                {
                    markScript.dialogueFile = markDialogue;
                    markScript.ReloadDialogueFile();
                }
            }
        }
        
        // Configure Annie based on Mark's completion status
        bool markCompleted = PlayerPrefs.GetInt("TutorialMark", 0) == 1;
        
        if (annieNPC != null)
        {
            // Initially hide Annie until Mark is completed
            if (!markCompleted)
            {
                annieNPC.SetActive(false);
                Debug.Log("Annie is initially hidden as Mark hasn't been completed yet");
            }
            else
            {
                annieNPC.SetActive(true);
                Debug.Log("Annie is active because Mark was already completed");
                
                // Ensure Annie has correct dialogue
                NPCscript annieScript = annieNPC.GetComponent<NPCscript>();
                if (annieScript != null)
                {
                    TextAsset annieDialogue = Resources.Load<TextAsset>("DialogueData/TutorialAnnie");
                    if (annieDialogue != null)
                    {
                        annieScript.dialogueFile = annieDialogue;
                        annieScript.ReloadDialogueFile();
                        annieScript.enabled = true;
                    }
                }
            }
        }
        
        // Configure Janica as the first NPC
        if (janicaNPC != null)
        {
            janicaNPC.SetActive(true);
            
            // Ensure Janica has correct dialogue
            NPCscript janicaScript = janicaNPC.GetComponent<NPCscript>();
            if (janicaScript != null)
            {
                TextAsset janicaDialogue = Resources.Load<TextAsset>("DialogueData/TutorialJanica");
                if (janicaDialogue != null && janicaScript.dialogueFile != janicaDialogue)
                {
                    janicaScript.dialogueFile = janicaDialogue;
                    janicaScript.ReloadDialogueFile();
                }
            }
        }
        
        // Configure Mark based on Janica's completion status
        bool janicaCompleted = PlayerPrefs.GetInt("TutorialJanica", 0) == 1;
        
        if (markNPC != null)
        {
            // Initially hide Mark until Janica is completed
            if (!janicaCompleted)
            {
                markNPC.SetActive(false);  // Comment this line if you want Mark to always be visible
                Debug.Log($"Mark is initially hidden as Janica hasn't been completed yet (Janica completed: {janicaCompleted})");
            }
            else
            {
                markNPC.SetActive(true);
                Debug.Log($"Mark is active because Janica was already completed (Janica completed: {janicaCompleted})");
                
                // Ensure Mark has correct dialogue
                NPCscript markScript = markNPC.GetComponent<NPCscript>();
                if (markScript != null)
                {
                    TextAsset markDialogue = Resources.Load<TextAsset>("DialogueData/TutorialMark");
                    if (markDialogue != null)
                    {
                        markScript.dialogueFile = markDialogue;
                        markScript.ReloadDialogueFile();
                        markScript.enabled = true;
                    }
                }
            }
        }
        
        // Configure Rojan only when in school scene
        if (rojanNPC != null)
        {
            bool annieCompleted = PlayerPrefs.GetString("TutorialCheckpoint", "") == "Annie";
            rojanNPC.SetActive(annieCompleted);
            
            if (annieCompleted)
            {
                NPCscript rojanScript = rojanNPC.GetComponent<NPCscript>();
                if (rojanScript != null)
                {
                    TextAsset rojanDialogue = Resources.Load<TextAsset>("DialogueData/TutorialRojan");
                    if (rojanDialogue != null)
                    {
                        rojanScript.dialogueFile = rojanDialogue;
                        rojanScript.ReloadDialogueFile();
                        rojanScript.enabled = true;
                    }
                }
            }
        }
        
        // Set up navigation arrow
        UpdateNavigationArrow();
    }
    
    private IEnumerator LoadTutorialProgress()
    {
        string username = PlayerPrefs.GetString("Username", "");
        if (string.IsNullOrEmpty(username))
        {
            Debug.LogWarning("Username not found in PlayerPrefs, cannot load tutorial progress");
            yield break;
        }
        
        Task<List<string>> task = null;
        
        try
        {
            task = GetCompletedNPCsAsync(username);
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Error starting progress load task: {ex.Message}");
            yield break;
        }
        
        yield return new WaitUntil(() => task != null && task.IsCompleted);
        
        try
        {
            if (task.IsFaulted)
            {
                Debug.LogError($"Error loading NPC progress: {task.Exception?.Message}");
            }
            else
            {
                completedNPCs = task.Result;
                Debug.Log($"Loaded {completedNPCs.Count} completed NPCs: {string.Join(", ", completedNPCs)}");
                
                // Update PlayerPrefs based on loaded data
                if (completedNPCs.Contains("Mark"))
                {
                    PlayerPrefs.SetInt("TutorialMark", 1);
                    
                    if (annieNPC != null && !annieNPC.activeSelf)
                    {
                        annieNPC.SetActive(true);
                        
                        NPCscript annieScript = annieNPC.GetComponent<NPCscript>();
                        if (annieScript != null)
                        {
                            annieScript.enabled = true;
                            annieScript.ReloadDialogueFile();
                        }
                        
                        Debug.Log("Activated Annie based on loaded data");
                    }
                }
                
                if (completedNPCs.Contains("Annie"))
                {
                    PlayerPrefs.SetString("TutorialCheckpoint", "Annie");
                }
                
                // Set a clear PlayerPrefs flag if both NPCs are completed
                if (completedNPCs.Contains("Mark") && completedNPCs.Contains("Annie"))
                {
                    PlayerPrefs.SetString("TutorialStatus", "Completed");
                    Debug.Log("Set TutorialStatus to Completed in PlayerPrefs");
                }
                
                PlayerPrefs.Save();
                
                // Update UI to reflect loaded data
                UpdateNavigationArrow();
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Error in LoadTutorialProgress: {ex.Message}");
        }
    }
    
    private IEnumerator MonitorTutorialProgress()
    {
        while (true)
        {
            // Check for Mark's completion every few seconds
            yield return new WaitForSeconds(2f);
            
            bool markCompleted = PlayerPrefs.GetInt("TutorialMark", 0) == 1;
            
            // Enable Annie if Mark is completed but she's not active
            if (markCompleted && annieNPC != null && !annieNPC.activeInHierarchy)
            {
                Debug.Log("Mark was completed, activating Annie");
                annieNPC.SetActive(true);
                
                // Setup Annie's script
                NPCscript annieScript = annieNPC.GetComponent<NPCscript>();
                if (annieScript != null)
                {
                    TextAsset annieDialogue = Resources.Load<TextAsset>("DialogueData/TutorialAnnie");
                    if (annieDialogue != null)
                    {
                        annieScript.dialogueFile = annieDialogue;
                        annieScript.enabled = true;
                        annieScript.ReloadDialogueFile();
                    }
                }
                
                // Update navigation arrow to point to Annie
                UpdateNavigationArrow();
            }
            
            // Also monitor for reward panel to fix Annie's reward if needed
            if (annieNPC != null && annieNPC.activeSelf && rewardPanel != null && rewardPanel.activeInHierarchy)
            {
                // Check if this is Annie's reward by seeing if Annie is the active NPC
                if (activeNPC != null && activeNPC.gameObject == annieNPC)
                {
                    EnsureCorrectAnnieReward();
                }
            }
        }
    }
    
    private void FindUIComponents()
    {
        if (rewardPanel == null)
            rewardPanel = GameObject.Find("RewardPanel");
            
        if (rewardPanel != null)
        {
            if (rewardImage == null)
                rewardImage = rewardPanel.GetComponentInChildren<Image>(true);
                
            if (rewardTitleText == null || congratsText == null)
            {
                TextMeshProUGUI[] texts = rewardPanel.GetComponentsInChildren<TextMeshProUGUI>(true);
                foreach (var text in texts)
                {
                    if (text.name.Contains("RewardText") || text.name.Contains("TitleText"))
                        rewardTitleText = text;
                    else if (text.name.Contains("CongratsText"))
                        congratsText = text;
                }
            }
        }
        
        if (navigationArrow == null)
            navigationArrow = GameObject.Find("NavigationArrow");
    }
    
    private void EnsureCorrectAnnieReward()
    {
        if (rewardTitleText != null && !rewardTitleText.text.Contains("Annie"))
        {
            Debug.Log("Fixing Annie's reward title");
            rewardTitleText.text = annieRewardMessage;
        }
        
        if (congratsText != null)
        {
            congratsText.text = annieResponseText;
        }
        
        if (rewardImage != null)
        {
            Sprite correctSprite = Resources.Load<Sprite>($"Rewards/{annieRewardSprite}");
            if (correctSprite != null && rewardImage.sprite != correctSprite)
            {
                rewardImage.sprite = correctSprite;
            }
        }
    }
    
    private void UpdateNavigationArrow()
    {
        if (navigationArrow == null) return;
        
        bool janicaCompleted = completedNPCs.Contains("Janica");
        bool markCompleted = completedNPCs.Contains("Mark");
        bool annieCompleted = completedNPCs.Contains("Annie");
        bool rojanCompleted = completedNPCs.Contains("Rojan");
        
        // Update the tutorial flow to include Janica and Rojan
        Transform janicaPosition = GameObject.Find("NPC Janica Tutorial")?.transform;
        
        // Point to appropriate target based on tutorial progression
        if (!janicaCompleted && janicaPosition != null)
        {
            // First point to Janica
            navigationArrow.transform.position = janicaPosition.position + Vector3.up * 1f;
            navigationArrow.SetActive(true);
        }
        else if (!markCompleted && markPosition != null)
        {
            // Then point to Mark
            navigationArrow.transform.position = markPosition.position + Vector3.up * 1f;
            navigationArrow.SetActive(true);
        }
        else if (markCompleted && !annieCompleted && anniePosition != null)
        {
            // Then point to Annie
            navigationArrow.transform.position = anniePosition.position + Vector3.up * 1f;
            navigationArrow.SetActive(true);
        }
        else if (markCompleted && annieCompleted && buildingEntrancePosition != null)
        {
            // Then point to building entrance
            navigationArrow.transform.position = buildingEntrancePosition.position + Vector3.up * 1f;
            navigationArrow.SetActive(true);
        }
        else
        {
            // Hide arrow if positions aren't set
            navigationArrow.SetActive(false);
        }
        
        // Log the current state of tutorial progression
        Debug.Log($"Tutorial state: Janica={janicaCompleted}, Mark={markCompleted}, " +
                  $"Annie={annieCompleted}, Rojan={rojanCompleted}");
    }
    
    private IEnumerator RefreshDebugText()
    {
        while (true)
        {
            UpdateDebugText();
            yield return new WaitForSeconds(2f);
        }
    }
    
    private void UpdateDebugText()
    {
        if (debugText == null) return;
        
        string markStatus = PlayerPrefs.GetInt("TutorialMark", 0) == 1 ? "COMPLETED" : "Not completed";
        string annieStatus = PlayerPrefs.GetString("TutorialCheckpoint", "") == "Annie" ? "COMPLETED" : "Not completed";
        
        debugText.text = $"<b>TUTORIAL STATUS:</b>\n" +
                         $"Mark: {markStatus} (Active: {(markNPC != null && markNPC.activeSelf)})\n" +
                         $"Annie: {annieStatus} (Active: {(annieNPC != null && annieNPC.activeSelf)})\n" +
                         $"Completed NPCs: {string.Join(", ", completedNPCs)}";
    }
    
    // --- NPC Dialogue Coordination Methods ---
    
    /// <summary>
    /// Called by an NPCscript to request starting a dialogue.
    /// Returns true if the request is approved.
    /// </summary>
    public bool RequestDialogueStart(NPCscript npc)
    {
        if (activeNPC != null && activeNPC != npc)
        {
            Debug.LogWarning($"Denied dialogue start for {npc.name}, {activeNPC.name} is already active");
            return false;
        }
        
        activeNPC = npc;
        OnDialogueStarted?.Invoke(npc);
        
        Debug.Log($"{npc.name} started dialogue");
        return true;
    }
    
    /// <summary>
    /// Called by an NPCscript when dialogue ends
    /// </summary>
    public void EndDialogue(NPCscript npc)
    {
        if (activeNPC == npc)
        {
            activeNPC = null;
        }
    }
    
    /// <summary>
    /// Checks if the given NPC is the active one
    /// </summary>
    public bool IsActiveNPC(NPCscript npc)
    {
        return activeNPC == npc;
    }
    
    // --- Tutorial Progress Methods ---
    
    /// <summary>
    /// Called when an NPC dialogue is completed
    /// </summary>
    public void OnNPCDialogueCompleted(string npcName)
    {
        Debug.Log($"NPC {npcName} dialogue completed");
        
        if (!completedNPCs.Contains(npcName))
        {
            completedNPCs.Add(npcName);
        }
        
        // Handle specific NPC completion
        if (npcName == "Janica")
        {
            PlayerPrefs.SetInt("TutorialJanica", 1);
            PlayerPrefs.Save();
            
            // Enable Mark NPC
            if (markNPC != null)
            {
                markNPC.SetActive(true);
                
                // Setup Mark's script
                NPCscript markScript = markNPC.GetComponent<NPCscript>();
                if (markScript != null)
                {
                    markScript.enabled = true;
                    markScript.ReloadDialogueFile();
                }
                
                Debug.Log("Enabled Mark NPC after Janica completion");
            }
        }
        else if (npcName == "Mark")
        {
            PlayerPrefs.SetInt("TutorialMark", 1);
            PlayerPrefs.Save();
            
            // Enable Annie NPC
            if (annieNPC != null)
            {
                annieNPC.SetActive(true);
                
                // Setup Annie's script
                NPCscript annieScript = annieNPC.GetComponent<NPCscript>();
                if (annieScript != null)
                {
                    annieScript.enabled = true;
                    annieScript.ReloadDialogueFile();
                }
                
                Debug.Log("Enabled Annie NPC after Mark completion");
            }
        }
        else if (npcName == "Annie")
        {
            PlayerPrefs.SetString("TutorialCheckpoint", "Annie");
            PlayerPrefs.Save();
        }
        else if (npcName == "Rojan")
        {
            PlayerPrefs.SetInt("TutorialRojan", 1);
            PlayerPrefs.Save();
            Debug.Log("Rojan's dialogue complete, tutorial complete!");
        }
        
        // Update navigation arrow
        UpdateNavigationArrow();
        
        // Update the database with the latest progress
        StartCoroutine(SaveTutorialProgressToDatabase());
    }
    
    /// <summary>
    /// Save tutorial progress to the database
    /// </summary>
    private IEnumerator SaveTutorialProgressToDatabase()
    {
        string username = PlayerPrefs.GetString("Username", "");
        if (string.IsNullOrEmpty(username))
        {
            Debug.LogWarning("Cannot save tutorial progress, username not found in PlayerPrefs");
            yield break;
        }
        
        Task saveTask = null;
        
        try
        {
            saveTask = SaveProgressAsync(username, completedNPCs);
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Error starting save progress task: {ex.Message}");
            yield break;
        }
        
        // Wait outside the try block
        yield return new WaitUntil(() => saveTask != null && saveTask.IsCompleted);
        
        try
        {
            if (saveTask.IsFaulted)
            {
                Debug.LogError($"Error saving tutorial progress: {saveTask.Exception?.Message}");
            }
            else
            {
                Debug.Log("Tutorial progress saved successfully");
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Error in SaveTutorialProgressToDatabase: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Save progress to the database
    /// </summary>
    private async Task SaveProgressAsync(string username, List<string> npcsCompleted)
    {
        try
        {
            using (var handler = CustomHttpHandler.GetInsecureHandler())
            using (var client = new HttpClient(handler))
            {
                client.DefaultRequestHeaders.Accept.Add(
                    new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
                
                // Determine tutorial status
                string tutorialStatus = "In Progress";
                if (npcsCompleted.Contains("Mark") && npcsCompleted.Contains("Annie"))
                {
                    tutorialStatus = "Completed";
                }
                else if (npcsCompleted.Contains("Mark"))
                {
                    tutorialStatus = "Mark Completed";
                }
                else if (npcsCompleted.Contains("Annie"))
                {
                    tutorialStatus = "Annie Completed";
                }
                
                var progressData = new
                {
                    Username = username,
                    tutorial = new
                    {
                        status = tutorialStatus,
                        reward = "TwoStar",
                        date = System.DateTime.UtcNow.ToString("o")
                    },
                    units = new Dictionary<string, object>
                    {
                        ["Unit1"] = new
                        {
                            status = "Not Started",
                            completedLessons = 0,
                            unitScore = 0,
                            lessons = new Dictionary<string, object>
                            {
                                ["Lesson1"] = new
                                {
                                    npcsTalkedTo = npcsCompleted.ToArray()
                                }
                            }
                        },
                        ["Unit2"] = new { status = "Not Started", completedLessons = 0, unitScore = 0, lessons = new Dictionary<string, object>() },
                        ["Unit3"] = new { status = "Not Started", completedLessons = 0, unitScore = 0, lessons = new Dictionary<string, object>() },
                        ["Unit4"] = new { status = "Not Started", completedLessons = 0, unitScore = 0, lessons = new Dictionary<string, object>() }
                    }
                };
                
                string jsonData = Newtonsoft.Json.JsonConvert.SerializeObject(progressData);
                Debug.Log($"Saving progress data: {jsonData}");
                
                var content = new StringContent(jsonData, Encoding.UTF8, "application/json");
                var response = await client.PostAsync($"{baseUrl}/game_progress", content);
                string responseText = await response.Content.ReadAsStringAsync();
                
                Debug.Log($"Save progress response: {(int)response.StatusCode} - {responseText}");
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Error saving progress: {ex.Message}");
            throw;
        }
    }
    
    /// <summary>
    /// Get the list of NPCs the player has already completed
    /// </summary>
    private async Task<List<string>> GetCompletedNPCsAsync(string username)
    {
        try
        {
            using (var handler = CustomHttpHandler.GetInsecureHandler())
            using (var client = new HttpClient(handler))
            {
                var response = await client.GetAsync($"{baseUrl}/game_progress/{username}");
                
                if (response.IsSuccessStatusCode)
                {
                    string responseContent = await response.Content.ReadAsStringAsync();
                    
                    // Parse the response using Newtonsoft.Json
                    var progress = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, object>>(responseContent);
                    
                    // Extract the npcsTalkedTo array
                    List<string> result = new List<string>();
                    
                    if (progress != null && 
                        progress.TryGetValue("units", out var units) && 
                        units is Dictionary<string, object> unitsDict &&
                        unitsDict.TryGetValue("Unit1", out var unit1) &&
                        unit1 is Dictionary<string, object> unit1Dict &&
                        unit1Dict.TryGetValue("lessons", out var lessons) &&
                        lessons is Dictionary<string, object> lessonsDict &&
                        lessonsDict.TryGetValue("Lesson1", out var lesson1) &&
                        lesson1 is Dictionary<string, object> lesson1Dict &&
                        lesson1Dict.TryGetValue("npcsTalkedTo", out var npcsList) &&
                        npcsList is Newtonsoft.Json.Linq.JArray npcsArray)
                    {
                        foreach (var npc in npcsArray)
                        {
                            result.Add(npc.ToString());
                        }
                    }
                    
                    // Also check tutorial status
                    if (progress != null && 
                        progress.TryGetValue("tutorial", out var tutorial) &&
                        tutorial is Dictionary<string, object> tutorialDict)
                    {
                        string status = tutorialDict["status"]?.ToString() ?? "";
                        
                        // Add Mark if tutorial status indicates he's completed
                        if (status == "Mark Completed" || status == "Completed")
                        {
                            if (!result.Contains("Mark"))
                                result.Add("Mark");
                        }
                        
                        // Add Annie if tutorial status indicates she's completed
                        if (status == "Annie Completed" || status == "Completed")
                        {
                            if (!result.Contains("Annie"))
                                result.Add("Annie");
                        }
                    }
                    
                    return result;
                }
                else
                {
                    Debug.LogWarning($"Failed to get progress: {response.StatusCode}");
                    return new List<string>();
                }
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Error getting completed NPCs: {ex.Message}");
            return new List<string>();
        }
    }
    
    // --- Public Methods for Unity Inspector Button Use ---
    
    /// <summary>
    /// Reset the tutorial state completely
    /// </summary>
    public void ResetTutorialState()
    {
        // Clear PlayerPrefs flags
        PlayerPrefs.DeleteKey("TutorialJanica");
        PlayerPrefs.DeleteKey("TutorialMark");
        PlayerPrefs.DeleteKey("TutorialCheckpoint");
        PlayerPrefs.DeleteKey("TutorialRojan");
        PlayerPrefs.DeleteKey("FirstMeetingMark");
        PlayerPrefs.DeleteKey("ForceEnableAnnie");
        PlayerPrefs.Save();
        
        // Clear completion states
        DialogueState.ResetDialogueState("Janica");
        DialogueState.ResetDialogueState("Mark");
        DialogueState.ResetDialogueState("Annie");
        DialogueState.ResetDialogueState("Rojan");
        
        // Reset NPC states
        if (markNPC != null)
        {
            markNPC.SetActive(true);
            
            NPCscript markScript = markNPC.GetComponent<NPCscript>();
            if (markScript != null)
            {
                // Fix for error: use existing method in NPCscript
                if (PlayerPrefs.GetString("GameMode", "") != "Tutorial")
                {
                    PlayerPrefs.SetString("GameMode", "Tutorial");
                }
                DialogueState.ResetDialogueState("Mark");
                markScript.enabled = true;
            }
        }
        
        if (annieNPC != null)
        {
            annieNPC.SetActive(false);
            
            NPCscript annieScript = annieNPC.GetComponent<NPCscript>();
            if (annieScript != null)
            {
                // Fix for error: use existing method in NPCscript
                DialogueState.ResetDialogueState("Annie");
                annieScript.enabled = true;
            }
        }
        
        if (janicaNPC != null)
        {
            janicaNPC.SetActive(true);
            
            NPCscript janicaScript = janicaNPC.GetComponent<NPCscript>();
            if (janicaScript != null)
            {
                DialogueState.ResetDialogueState("Janica");
                janicaScript.enabled = true;
            }
        }
        
        if (rojanNPC != null)
        {
            rojanNPC.SetActive(false);
            
            NPCscript rojanScript = rojanNPC.GetComponent<NPCscript>();
            if (rojanScript != null)
            {
                DialogueState.ResetDialogueState("Rojan");
                rojanScript.enabled = true;
            }
        }
        
        // Clear completed NPCs list
        completedNPCs.Clear();
        
        // Update navigation arrow
        UpdateNavigationArrow();
        
        // Update debug text
        if (debugText != null)
        {
            UpdateDebugText();
        }
        
        Debug.Log("Tutorial state has been completely reset");
    }
    
    // Add this alias method for TutorialResetButton
    public void ResetTutorialProgress()
    {
        ResetTutorialState();
    }
    
    /// <summary>
    /// Skip directly to completed state (for testing)
    /// </summary>
    public void SkipToCompleted()
    {
        // Set PlayerPrefs flags
        PlayerPrefs.SetInt("TutorialJanica", 1);
        PlayerPrefs.SetInt("TutorialMark", 1);
        PlayerPrefs.SetString("TutorialCheckpoint", "Annie");
        
        // Optionally, also complete Rojan if in school scene
        if (rojanNPC != null && rojanNPC.scene.IsValid())
        {
            PlayerPrefs.SetInt("TutorialRojan", 1);
        }
        
        PlayerPrefs.Save();
        
        // Set dialogue completed states
        DialogueState.SetDialogueCompleted("Janica");
        DialogueState.SetDialogueCompleted("Mark");
        DialogueState.SetDialogueCompleted("Annie");
        
        if (rojanNPC != null && rojanNPC.scene.IsValid())
        {
            DialogueState.SetDialogueCompleted("Rojan");
        }
        
        // Update NPC states
        if (markNPC != null)
        {
            markNPC.SetActive(true);
            
            NPCscript markScript = markNPC.GetComponent<NPCscript>();
            if (markScript != null)
            {
                markScript.enabled = true;
            }
        }
        
        if (annieNPC != null)
        {
            annieNPC.SetActive(true);
            
            NPCscript annieScript = annieNPC.GetComponent<NPCscript>();
            if (annieScript != null)
            {
                annieScript.enabled = true;
            }
        }
        
        if (janicaNPC != null)
        {
            janicaNPC.SetActive(true);
            
            NPCscript janicaScript = janicaNPC.GetComponent<NPCscript>();
            if (janicaScript != null)
            {
                janicaScript.enabled = true;
            }
        }
        
        if (rojanNPC != null)
        {
            rojanNPC.SetActive(true);
            
            NPCscript rojanScript = rojanNPC.GetComponent<NPCscript>();
            if (rojanScript != null)
            {
                rojanScript.enabled = true;
            }
        }
        
        // Update completed NPCs list
        if (!completedNPCs.Contains("Janica"))
            completedNPCs.Add("Janica");
            
        if (!completedNPCs.Contains("Mark"))
            completedNPCs.Add("Mark");
            
        if (!completedNPCs.Contains("Annie"))
            completedNPCs.Add("Annie");
            
        if (rojanNPC != null && rojanNPC.scene.IsValid() && !completedNPCs.Contains("Rojan"))
            completedNPCs.Add("Rojan");
        
        // Update navigation arrow
        UpdateNavigationArrow();
        
        // Update debug text
        if (debugText != null)
        {
            UpdateDebugText();
        }
        
        Debug.Log("Tutorial skipped to completed state");
        
        // Save to database
        StartCoroutine(SaveTutorialProgressToDatabase());
    }
    
    // Update how we check for tutorial completion in TutorialManager
    private bool IsTutorialCompleted()
    {
        // Now we need to check all required NPCs
        bool markCompleted = completedNPCs.Contains("Mark");
        bool annieCompleted = completedNPCs.Contains("Annie");
        bool janicaCompleted = completedNPCs.Contains("Janica");
        bool rojanCompleted = completedNPCs.Contains("Rojan");
        
        // The tutorial is completed if all required NPCs have been completed
        // For the initial flow, just Janica and Mark+Annie are required
        // Rojan completes the full tutorial in the school scene
        bool initialTutorialDone = janicaCompleted && markCompleted && annieCompleted;
        bool fullTutorialDone = initialTutorialDone && rojanCompleted;
        
        // For now, return true if the initial tutorial is done, since Rojan is in another scene
        return initialTutorialDone;
    }
}

