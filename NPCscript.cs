using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using UnityEngine.InputSystem;
using System.Linq; // Add this for Select()
using VirtueBanwa;  // Add this at the top with other using statements
using System.Globalization;
using UnityEngine.SceneManagement; // Add to top using statements
using UnityEngine.Audio; // Add this for audio functionality
using Newtonsoft.Json; // Add this to the top with other using statements

// Add this at the top with other enums
public enum NPCType
{
    Lesson,  // For lesson-related NPCs
    Idle     // For NPCs that just give random dialogue
}

public class NPCscript : MonoBehaviour
{
    [Header("NPC Configuration")]
    public NPCType npcType;
    public TextAsset dialogueFile;  // This will determine if it's tutorial or lesson
    public string[] idleDialogues;  // Add this field for idle NPC dialogues

    [Header("Dialogue Configuration")]
    // Remove duplicate dialogueFile declaration
    // public TextAsset dialogueFile;  // <-- Remove this line

    [Header("Core UI Elements")]
    public Text dialogueText;
    public GameObject dialoguePanel;
    public GameObject DialogueBoxPanel; // Add this reference in the header section
    public GameObject contButton;
    public TextMeshProUGUI npcNameText;    // Reference to NPC name TMP text
    public TextMeshProUGUI playerNameText;    // Player name TMP text
    public GameObject playerPortraitImage;     // Player portrait container
    
    [Header("Choice UI")]
    public GameObject choiceButton1;
    public GameObject choiceButton2;
    public GameObject choiceButton3;
    
    [Header("Reward UI")]
    public GameObject rewardPanel;
    public Image rewardImage;
    public TextMeshProUGUI congratsText;
    public GameObject rewardBackground;
    public Button closeRewardButton;
    public bool useRewardSystem = true;
    public float rewardDisplayDuration = 2f;
    
    [Header("Characters")]
    public GameObject currentSpeakerImage;
    public Sprite npcSprite;
    
    [Header("Quest UI")]
    public TextMeshProUGUI questLabelText;
    public string currentUnit = "UNIT 1";
    public string currentLesson = "Lesson 1";
    public float wordSpeed = 0.05f;
    
    [Header("Scene Management")]
    public string nextSceneName;
    public GameObject playerUsernameObject;
    public SceneTransition sceneTransition;
    public GameObject backButton;

    [Header("Player Detection")]
    public bool playerisClose;
    public bool firstLineIsPlayerSprite;

    // Add this field to the class
    [Header("Quest Book Integration")]
    [Tooltip("If true, clicking start lesson will open quest book instead of loading scene directly")]
    public bool useQuestBookForTutorial = false;
    public QuestBookManager questBookManager;

    [Header("Audio Configuration")]
    [Tooltip("Audio source component to play dialogue sounds")]
    public AudioSource dialogueAudioSource;
    [Tooltip("Audio clips that match dialogue progression")]
    public AudioClip[] dialogueAudioClips;
    [Tooltip("Button to toggle audio playback")]
    public Button speakerButton;
    [Tooltip("Play audio automatically when dialogue changes")]
    public bool autoPlayAudio = true;
    [Tooltip("Volume for dialogue audio")]
    [Range(0f, 1f)]
    public float audioVolume = 1.0f;

    // Private fields
    private DialogueData currentDialogue;
    private string playerUsername;
    private int index;
    private bool isReplying;
    private bool isNarrating;
    private Coroutine typingCoroutine;
    private PlayerMovement playerMovement;
    private bool isDialogueComplete = false;

 // Base URL for the API
    private string baseUrl => NetworkConfig.BaseUrl; 



    private System.Action<int> OnRewardPopupRequested;  // Renamed from ShowRewardPopup

    // Add these fields at the top of the class, after the existing headers
    private enum DialogueType { NPC, Reply, Narration, Choice }
    private DialogueType currentDialogueType;
    
    private string lessonTitle;
    private string questAction;
    private float typingSpeed = 0.05f;
    private Coroutine questCoroutine;
    private GameObject player;
    
    private string[] dialogue;
    private string[] narration;
    private int narrationIndex;
    
    private List<string> npcDialogueHistory = new List<string>();
    private List<string> npcReplyHistory = new List<string>();
    private List<string> narrationHistory = new List<string>(); // Add this line
    private List<Sprite> spriteHistory = new List<Sprite>();
    
    [Header("Additional UI Elements")]
    public GameObject rewardPopup;
    public TextMeshProUGUI rewardText;
    public Text[] choiceTexts;

    [Header("Character Sprites")]
    public Sprite playerSprite;
    public Sprite npcPovSprite;

    private TutorialProgressManager tutorialManager;
    private TutorialChecker tutorialChecker;

    private bool hasCompletedDialogue = false;

    private string _cachedNpcIdentifier = null;
    private int currentAudioIndex = 0;
    private bool isAudioMuted = false;

    private bool choiceIsStartTutorial = false;

    // Modify these variables to track nested dialogue state
    private List<DialogueData.Choice[]> choiceStack = new List<DialogueData.Choice[]>();
    private List<int> choiceIndexStack = new List<int>();
    private DialogueData.Choice[] currentChoices;
    private bool isInNestedDialogue = false;

void Awake()
{
    // Force the dialogue panel to be hidden at the very start of the game
    // This runs before Start() so it ensures the panel is off from the beginning
    if (dialoguePanel != null)
    {
        dialoguePanel.SetActive(false);
    }
    
    // Subscribe to the dialogue coordinator's event
    if (TutorialManager.Instance != null)
    {
        TutorialManager.Instance.OnDialogueStarted += OnOtherNPCStartedDialogue;
    }
    else if (NPCDialogueCoordinator.Instance != null)
    {
        NPCDialogueCoordinator.Instance.OnDialogueStarted += OnOtherNPCStartedDialogue;
    }
    
    // Critical fix: Force Annie to load her own dialogue file at the very start
    if (gameObject.name.Contains("Annie"))
    {
        TextAsset annieDialogue = Resources.Load<TextAsset>("DialogueData/TutorialAnnie");
        if (annieDialogue != null)
        {
            Debug.Log($"AWAKE: Forcing Annie to use her own dialogue file: {annieDialogue.name}");
            dialogueFile = annieDialogue;
        }
        else
        {
            Debug.LogError("CRITICAL ERROR: Cannot find TutorialAnnie.json in Resources/DialogueData!");
        }
    }
}

    void OnDestroy()
    {
        // Unsubscribe to prevent memory leaks
        if (TutorialManager.Instance != null)
        {
            TutorialManager.Instance.OnDialogueStarted -= OnOtherNPCStartedDialogue;
        }
        
        if (NPCDialogueCoordinator.Instance != null)
        {
            NPCDialogueCoordinator.Instance.OnDialogueStarted -= OnOtherNPCStartedDialogue;
        }

        if (speakerButton != null)
        {
            speakerButton.onClick.RemoveAllListeners();
        }
    }

    // This gets called when any NPC starts dialogue
    private void OnOtherNPCStartedDialogue(NPCscript activeNPC)
    {
        // If another NPC started dialogue, hide our choices
        if (activeNPC != this)
        {
            HideDialogue();
        }
    }

    private void HideDialogue()
    {
        // Hide all dialogue UI elements
        HideChoiceButtons();
        if (dialoguePanel != null && dialoguePanel.activeInHierarchy)
        {
            zeroText();
        }
    }

    void Start()
    {
        // Get current lesson from scene name or dialogue file instead
        bool isTutorial = dialogueFile.name.Contains("Tutorial");
        
        if (isTutorial)
        {
            currentUnit = "Tutorial";
            currentLesson = "Tutorial";
            lessonTitle = "Tutorial";
            questAction = "Complete the tutorial";
        }
        else
        {
            // Parse unit and lesson from dialogue filename (e.g., "Unit1Lesson1.json")
            string fileName = dialogueFile.name;
            currentUnit = $"UNIT {fileName[4]}";  // Gets the number after "Unit"
            currentLesson = $"Lesson {fileName[11]}";  // Gets the number after "Lesson"
            
            lessonTitle = $"{currentUnit}: {currentLesson}";
            questAction = "Talk to the NPC";
        }

        Debug.Log($"Setting up lesson: {lessonTitle} with action: {questAction}");

        InitializeChoiceButtons();
        InitializeContinueButton(); // Add this line
        InitializeAudioComponents(); // Add this line
        playerUsername = GetCurrentUsername();

        // Find the player object and get its movement component
        player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerMovement = player.GetComponent<PlayerMovement>();
        }
        else
        {
            Debug.LogError("Player not found! Make sure the player has the 'Player' tag.");
        }

        LoadDialogueFromJson();

        if (questLabelText != null)
        {
            StartQuestLabelSequence();
        }

        tutorialManager = FindObjectOfType<TutorialProgressManager>();
        tutorialChecker = FindObjectOfType<TutorialChecker>();

        // Check if this NPC's dialogue has been completed
        if (currentDialogue?.npcName == "Annie")
        {
            bool markCompleted = PlayerPrefs.GetInt("TutorialMark", 0) == 1; // <-- THIS LINE CHECKS IF MARK IS COMPLETED
            this.enabled = markCompleted;
            Debug.Log($"Annie NPC enabled check - Mark completed: {markCompleted}");
            
            // If Annie is enabled, make sure we're loading Annie's dialogue
            if (markCompleted)
            {
                // Ensure we load the right dialogue for Annie
                // This prevents showing Mark's choices for Annie
                LoadDialogueFromJson();
            }
        }
        else if (currentDialogue?.npcName == "Mark" && dialogueFile.name.Contains("Tutorial"))
        {
            // Make sure Mark always starts with his initial dialogue in Tutorial mode
            if (PlayerPrefs.GetString("GameMode", "") != "Tutorial")
            {
                // First time playing, set tutorial mode
                PlayerPrefs.SetString("GameMode", "Tutorial");
                
                // Reset any previous completion state for Mark
                DialogueState.ResetDialogueState("Mark");
                hasCompletedDialogue = false;
                Debug.Log("Mark's dialogue state reset for tutorial start");
            }
        }

        // Check if this NPC's dialogue was already completed
        if (currentDialogue != null)
        {
            hasCompletedDialogue = DialogueState.HasCompletedDialogue(currentDialogue.npcName);
            Debug.Log($"NPC {currentDialogue.npcName} dialogue completed: {hasCompletedDialogue}");
        }

        // Ensure dialogue panel is NOT active at start
        if (dialoguePanel != null)
        {
            dialoguePanel.SetActive(false);
        }
    }

private void LoadDialogueFromJson()
{
    if (dialogueFile == null)
    {
        Debug.LogError("No dialogue file assigned in Inspector!");
        
        // Emergency fallback for Annie
        if (gameObject.name.Contains("Annie"))
        {
            TextAsset annieDialogue = Resources.Load<TextAsset>("DialogueData/TutorialAnnie");
            if (annieDialogue != null)
            {
                Debug.Log("Emergency: Loading Annie's dialogue file as fallback");
                dialogueFile = annieDialogue;
            }
        }
        else if (gameObject.name.Contains("Mark") && !gameObject.name.Contains("Tutorial"))
        {
            // Try to load FreeRoam dialogue for Mark in Outside scene
            TextAsset freeRoamDialogue = Resources.Load<TextAsset>("DialogueData/FreeRoam");
            if (freeRoamDialogue != null)
            {
                Debug.Log("Emergency: Loading FreeRoam dialogue file for Mark");
                dialogueFile = freeRoamDialogue;
            }
        }
        
        if (dialogueFile == null) return; // Still null? Give up.
    }

    string actualNpcName = GetActualNPCName();
    Debug.Log($"[NPC:{gameObject.name}] Loading dialogue from file: {dialogueFile.name} for {actualNpcName}");
    
    try
    {
        // Clear any existing dialogue data
        if (currentDialogue != null)
        {
            currentDialogue.ClearData();
            currentDialogue = null;
        }

        // Check if content is array format (Lesson 2) or single object (Lesson 1)
        bool isArrayFormat = dialogueFile.text.TrimStart().StartsWith("[");
        
        if (isArrayFormat)
        {
            string wrappedJson = $"{{\"stories\":{dialogueFile.text}}}";
            var wrapper = JsonUtility.FromJson<StoriesWrapper>(wrappedJson);
            currentDialogue = wrapper.stories[0]; // Get first story
            Debug.Log($"[NPC:{gameObject.name}] Loaded dialogue for {currentDialogue.npcName} from array format");
        }
        else
        {
            currentDialogue = JsonUtility.FromJson<DialogueData>(dialogueFile.text);
            Debug.Log($"[NPC:{gameObject.name}] Loaded dialogue for {currentDialogue.npcName} from single object format");
        }

        // Explicitly load FreeRoam dialogue if we're in the Outside scene with the main Mark NPC
        if (gameObject.name.Contains("Mark") && !gameObject.name.Contains("Tutorial") && 
            SceneManager.GetActiveScene().name == "Outside")
        {
            TextAsset freeRoamDialogue = Resources.Load<TextAsset>("DialogueData/FreeRoam");
            if (freeRoamDialogue != null && freeRoamDialogue != dialogueFile)
            {
                Debug.Log("Specifically loading FreeRoam dialogue for Mark in Outside scene");
                dialogueFile = freeRoamDialogue;
                currentDialogue = JsonUtility.FromJson<DialogueData>(dialogueFile.text);
                Debug.Log($"Loaded FreeRoam dialogue: {currentDialogue.initialDialogue}");
            }
        }

        // Verify the dialogue data matches the NPC name or filename
        string expectedNpcName = gameObject.name.Replace("NPC ", "").Replace(" Tutorial", "");
        Debug.Log($"[NPC:{gameObject.name}] Expected NPC name: {expectedNpcName}, Actual NPC name from JSON: {currentDialogue.npcName}");

        // Log dialogue content for debugging
        Debug.Log($"[NPC:{gameObject.name}] Initial dialogue: \"{currentDialogue.initialDialogue}\"");
        if (currentDialogue.choices != null && currentDialogue.choices.Length > 0)
        {
            Debug.Log($"[NPC:{gameObject.name}] Number of choices: {currentDialogue.choices.Length}");
            for (int i = 0; i < currentDialogue.choices.Length; i++)
            {
                Debug.Log($"[NPC:{gameObject.name}] Choice {i+1}: \"{currentDialogue.choices[i].text}\"");
                Debug.Log($"[NPC:{gameObject.name}] Response {i+1}: \"{currentDialogue.choices[i].response}\"");
            }
        }

        // Check if this is Janica's tutorial dialogue
        if (gameObject.name.Contains("Janica") && dialogueFile.name.Contains("TutorialJanica"))
        {
            // Use TutorialDialogueData for Janica's special format
            TutorialDialogueData tutorialData = JsonUtility.FromJson<TutorialDialogueData>(dialogueFile.text);
            currentDialogue = tutorialData;
            
            Debug.Log($"[NPC:Janica] Loaded tutorial dialogue: {tutorialData.initialDialogue}");
            Debug.Log($"[NPC:Janica] Instruction1: {tutorialData.instruction1}");
            Debug.Log($"[NPC:Janica] Instruction2: {tutorialData.instruction2}");
            
            // Create a custom dialogue array for Janica including all the steps
            dialogue = new string[] { 
                tutorialData.initialDialogue,
                tutorialData.instruction1,
                tutorialData.instruction2,
                tutorialData.lastDialogue
            };
            narration = new string[] { tutorialData.initialNarration };
        }
        else
        {
            // Regular dialogue format
            dialogue = new string[] { currentDialogue.initialDialogue };
            narration = new string[] { currentDialogue.initialNarration };
        }

        // Check if this is a simple dialogue (no choices)
        if (currentDialogue.choices == null || currentDialogue.choices.Length == 0)
        {
            if (contButton != null)
            {
                contButton.SetActive(false); // Hide continue button for simple dialogues
            }
        }

        InitializeChoiceTexts();
        SetupRewardPopup();
    }
    catch (Exception ex)
    {
        Debug.LogError($"[NPC:{gameObject.name}] Error parsing dialogue file: {ex.Message}");
        Debug.LogError($"JSON content: {dialogueFile?.text ?? "null"}");
    }
}

    [System.Serializable]
    private class StoriesWrapper
    {
        public List<DialogueData> stories;
    }

    private string GetCurrentUsername()
    {
        if (playerUsernameObject != null)
        {
            TMP_Text usernameText = playerUsernameObject.GetComponent<TMP_Text>();
            if (usernameText != null)
            {
                string username = usernameText.text.Replace("#", ""); // Remove the "#" character
                Debug.Log($"Current Username from Text: {username}"); //change to Username? 
                return username;
            }
        }
        string usernameFromPrefs = PlayerPrefs.GetString("Username", "Player");
        Debug.Log($"Current Username from PlayerPrefs: {usernameFromPrefs}");
        return usernameFromPrefs;
    }

    private void InitializeChoiceButtons()
    {
        choiceButton1.SetActive(false);
        choiceButton2.SetActive(false);
        choiceButton3.SetActive(false);

        choiceButton1.GetComponent<Button>().onClick.RemoveAllListeners();
        choiceButton2.GetComponent<Button>().onClick.RemoveAllListeners();
        choiceButton3.GetComponent<Button>().onClick.RemoveAllListeners();

        choiceButton1.GetComponent<Button>().onClick.AddListener(() => OnChoiceSelected(0));
        choiceButton2.GetComponent<Button>().onClick.AddListener(() => OnChoiceSelected(1));
        choiceButton3.GetComponent<Button>().onClick.AddListener(() => OnChoiceSelected(2));
    }

    private void InitializeContinueButton()
    {
        if (contButton != null)
        {
            Button continueBtn = contButton.GetComponent<Button>();
            if (continueBtn != null)
            {
                continueBtn.onClick.RemoveAllListeners();
                continueBtn.onClick.AddListener(NextLine);
            }
            contButton.SetActive(false); // Start with continue button hidden
        }
        else
        {
            Debug.LogError("Continue button reference is missing!");
        }
    }

    private void InitializeAudioComponents()
    {
        // Create audio source if needed
        if (dialogueAudioSource == null)
        {
            dialogueAudioSource = gameObject.AddComponent<AudioSource>();
            dialogueAudioSource.playOnAwake = false;
            dialogueAudioSource.volume = audioVolume;
        }

        // Set up speaker button if assigned
        if (speakerButton != null)
        {
            speakerButton.onClick.RemoveAllListeners();
            speakerButton.onClick.AddListener(ToggleAudioMute);
            UpdateSpeakerButtonUI();
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        Debug.Log($"Trigger entered by: {other.gameObject.name}");
        if (other.CompareTag("Player"))
        {
            Debug.Log("Player detected, starting dialogue");
            playerisClose = true;
            
            // For Mark in the Outside scene, ensure FreeRoam dialogue is loaded
            if (gameObject.name.Contains("Mark") && !gameObject.name.Contains("Tutorial") && 
                SceneManager.GetActiveScene().name == "Outside")
            {
                TextAsset freeRoamDialogue = Resources.Load<TextAsset>("DialogueData/FreeRoam");
                if (freeRoamDialogue != null)
                {
                    Debug.Log("Loading FreeRoam dialogue for Mark in Outside scene");
                    dialogueFile = freeRoamDialogue;
                }
            }
            
            // Check if this is a tutorial NPC and if dialogue was already completed
            bool isTutorial = dialogueFile != null && dialogueFile.name.Contains("Tutorial");
            if (isTutorial && hasCompletedDialogue)
            {
                // Show response instead of initial dialogue
                ShowCompletedResponse();
            }
            else
            {
                // Normal dialogue flow
                StartDialogue();
            }
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            playerisClose = false;
            zeroText();
        }
    }

    private void FreezePlayer()
    {
        if (playerMovement != null)
        {
            playerMovement.FreezeMovement();
        }
    }

    private void UnfreezePlayer()
    {
        if (playerMovement != null)
        {
            playerMovement.UnfreezeMovement();
        }
    }

    private void StartDialogue()
    {
        Debug.Log($"[NPC:{gameObject.name}] StartDialogue called. Panel active: {dialoguePanel.activeInHierarchy}");
        
        // First try to use TutorialManager, then fallback to NPCDialogueCoordinator
        bool canProceed = false;
        
        if (TutorialManager.Instance != null)
        {
            canProceed = TutorialManager.Instance.RequestDialogueStart(this);
            if (!canProceed)
            {
                Debug.Log($"[NPC:{gameObject.name}] TutorialManager denied dialogue start request");
                return;
            }
        }
        else if (NPCDialogueCoordinator.Instance != null && !NPCDialogueCoordinator.Instance.RequestDialogueStart(this))
        {
            Debug.Log($"[NPC:{gameObject.name}] NPCDialogueCoordinator denied dialogue start request");
            return;
        }
        
        if (!dialoguePanel.activeInHierarchy)
        {
            Debug.Log($"[NPC:{gameObject.name}] Activating dialogue panel");
            dialoguePanel.SetActive(true);
            
            // Reset nested dialogue tracking
            choiceStack.Clear();
            choiceIndexStack.Clear();
            currentChoices = null;
            isInNestedDialogue = false;
            
            // Force reload dialogue data to ensure we have the latest content
            Debug.Log($"[NPC:{gameObject.name}] Reloading dialogue file: {dialogueFile?.name ?? "null"}");
            LoadDialogueFromJson();
            
            // Handle different NPC types
            if (npcType == NPCType.Idle)
            {
                ShowRandomIdleDialogue();
            }
            else // NPCType.Lesson
            {
                StartLessonDialogue();
            }
            
         //   FreezePlayer(); // Freeze player when dialogue starts
        }
    }

    private void ShowRandomIdleDialogue()
    {
        if (idleDialogues != null && idleDialogues.Length > 0)
        {
            int randomIndex = UnityEngine.Random.Range(0, idleDialogues.Length); // Explicitly use UnityEngine.Random
            dialogueText.text = idleDialogues[randomIndex];
            
            // Optionally hide choice buttons for idle NPCs
            choiceButton1?.SetActive(false);
            choiceButton2?.SetActive(false);
            choiceButton3?.SetActive(false);
            
            // Show continue button
            contButton?.SetActive(false);
        }
    }

    private void StartLessonDialogue()
    {
        if (npcNameText != null && currentDialogue != null)
        {
            Debug.Log($"[NPC:{gameObject.name}] Starting lesson dialogue for {currentDialogue.npcName}");
            
            // Clear existing dialogue text
            if (dialogueText != null)
            {
                dialogueText.text = "";
            }

            // Reset dialogue state
            index = 0;
            isReplying = false;
            isNarrating = false;

            npcNameText.text = currentDialogue.npcName;
            npcNameText.gameObject.SetActive(true);
            
            if (hasCompletedDialogue && currentDialogue.choices.Length > 0)
            {
                dialogueText.text = currentDialogue.choices[0].response;
                StartCoroutine(AutoCloseDialogue());
            }
            else
            {
                // Log initial dialogue content for debugging
                Debug.Log($"[NPC:{gameObject.name}] Initial dialogue: \"{currentDialogue.initialDialogue}\"");
                
                // Start typing the dialogue
                StartTyping();
            }
        }
        else
        {
            Debug.LogError($"[NPC:{gameObject.name}] Cannot start lesson dialogue: npcNameText={npcNameText!=null}, currentDialogue={currentDialogue!=null}");
        }
    }

    public void zeroText()
    {
        dialogueText.text = "";
        
        // Hide NPC name when dialogue ends
        if (npcNameText != null)
        {
            npcNameText.text = "";
            npcNameText.gameObject.SetActive(false);
        }
        
        index = 0;
        narrationIndex = 0;
        dialoguePanel.SetActive(false);
        HideChoiceButtons();
        isReplying = false;
        isNarrating = false;
        // Clear all histories
        npcDialogueHistory.Clear();
        npcReplyHistory.Clear();
        narrationHistory.Clear();
        spriteHistory.Clear();
        if (backButton != null)
        {
            backButton.SetActive(false);
        }
        UnfreezePlayer();
        
        // Notify the appropriate manager that this dialogue has ended
        if (TutorialManager.Instance != null)
        {
            TutorialManager.Instance.EndDialogue(this);
        }
        else if (NPCDialogueCoordinator.Instance != null)
        {
            NPCDialogueCoordinator.Instance.EndDialogue(this);
        }

        // Ensure arrow is hidden when dialogue ends
        GameObject upArrow = GameObject.Find("UpArrow");
        if (upArrow != null)
        {
            upArrow.SetActive(false);
        }
    }

    private void StartTyping()
    {
        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
        }
        currentDialogueType = DialogueType.NPC;
        playerUsername = GetCurrentUsername();
        
        // Update NPC name display
        if (npcNameText != null)
        {
            npcNameText.text = currentDialogue.npcName;
            npcNameText.gameObject.SetActive(true); // Make sure it's visible
        }

        Sprite currentSprite = index == 0 && firstLineIsPlayerSprite ? playerSprite : npcSprite;
        SetSprite(currentSprite);
        
        // For Janica, use the current index from the special dialogue array
        if (dialogue != null && index >= 0 && index < dialogue.Length)
        {
            string textToType = dialogue[index];
            string processedDialogue = ProcessDialogue(textToType);
            
            // Log what text is being shown
            Debug.Log($"[NPC:{gameObject.name}] Typing dialogue: \"{processedDialogue}\"");
            
            // Store in NPC dialogue history
            npcDialogueHistory.Add(processedDialogue);
            spriteHistory.Add(currentSprite);
            
            // Show back button only after first line of NPC dialogue
            UpdateBackButtonVisibility();
            typingCoroutine = StartCoroutine(Typing(processedDialogue));

            // Play corresponding audio if available
            PlayDialogueAudio(index);
        }
        else
        {
            Debug.LogError($"[NPC:{gameObject.name}] Invalid dialogue index: {index}, dialogue array length: {dialogue?.Length ?? 0}");
        }
    }

    private string ProcessDialogue(string rawDialogue)
    {
        return rawDialogue.Replace("[Your Name]", playerUsername);
    }

    IEnumerator Typing(string textToType)
    {
        dialogueText.text = "";
        contButton?.SetActive(false); // Use null conditional operator
        
        foreach (char letter in textToType.ToCharArray())
        {
            dialogueText.text += letter;
            yield return new WaitForSeconds(wordSpeed);
        }

        // For simple dialogues without choices, wait and close automatically
        if (dialogue.Length == 1 && currentDialogue.choices.Length == 0)
        {
            yield return new WaitForSeconds(3f); // Wait 3 seconds
            zeroText(); // Close dialogue
        }
        else if (dialogueText.text == textToType)
        {
            contButton?.SetActive(true);
        }
    }

    public void NextLine()
    {
        contButton.SetActive(false);
        
        // For Janica's special tutorial dialogue
        if (gameObject.name.Contains("Janica") && currentDialogue is TutorialDialogueData)
        {
            HandleJanicaDialogueFlow();
            return;
        }
        
        // Regular dialogue flow
        if (isReplying && !isNarrating)
        {
            StartNarration();
        }
        else if (isNarrating)
        {
            ContinueNarration();
        }
        else if (index < dialogue.Length - 1)
        {
            index++;
            if (index == 1)
            {
                SetSprite(npcSprite);
            }
            StartTyping();
        }
        else if (index == dialogue.Length - 1 && !isReplying)
        {
            ShowChoices();
        }
        else if (!isReplying && !isNarrating)
        {
            zeroText();
        }

        // Play audio for the next line if auto-play is enabled
        if (autoPlayAudio && dialogueAudioSource != null)
        {
            currentAudioIndex++;
            PlayDialogueAudio(currentAudioIndex);
        }
    }

    private void ShowChoices()
    {
        // Check if we're allowed to show choices via appropriate manager
        bool isActiveNPC = false;
        
        if (TutorialManager.Instance != null)
        {
            isActiveNPC = TutorialManager.Instance.IsActiveNPC(this);
        }
        else if (NPCDialogueCoordinator.Instance != null)
        {
            isActiveNPC = NPCDialogueCoordinator.Instance.IsActiveNPC(this);
        }
        
        if (!isActiveNPC)
        {
            Debug.LogWarning($"[NPC:{gameObject.name}] Attempted to show choices but is not the active NPC");
            return;
        }
        
        // Additional validity checks
        if (!this.isActiveAndEnabled || !gameObject.activeInHierarchy)
        {
            Debug.LogWarning($"[NPC:{gameObject.name}] Attempting to show choices while inactive!");
            return;
        }

        currentDialogueType = DialogueType.Choice;
        Debug.Log($"[NPC:{gameObject.name}] Showing choices for {currentDialogue.npcName}");

        // Show player info and hide NPC info
        if (playerNameText != null)
        {
            string firstName = PlayerPrefs.GetString("FirstName", "Player");
            string lastName = PlayerPrefs.GetString("LastName", "");
            playerNameText.text = $"{firstName} {lastName}".Trim();
            playerNameText.gameObject.SetActive(true);
        }

        // Show player portrait and hide NPC image
        if (playerPortraitImage != null && currentSpeakerImage != null)
        {
            GameObject boySprite = GameObject.Find("BoySprite");
            GameObject girlSprite = GameObject.Find("GirlSprite");
            
            if ((boySprite != null && boySprite.activeSelf) || (girlSprite != null && girlSprite.activeSelf))
            {
                SpriteRenderer activeSprite = boySprite != null && boySprite.activeSelf ? 
                    boySprite.GetComponent<SpriteRenderer>() : 
                    girlSprite.GetComponent<SpriteRenderer>();

                if (activeSprite != null)
                {
                    playerPortraitImage.GetComponent<Image>().sprite = activeSprite.sprite;
                    currentSpeakerImage.SetActive(false); // Hide NPC image
                    playerPortraitImage.SetActive(true);  // Show player image
                }
            }
        }

        // Hide NPC name during player choice
        if (npcNameText != null)
        {
            npcNameText.gameObject.SetActive(false);
        }

        // Show choice buttons
        for (int i = 0; i < currentDialogue.choices.Length; i++)
        {
            GameObject choiceButton = GetChoiceButton(i);
            if (choiceButton != null)
            {
                string choiceText = currentDialogue.choices[i].text;
                choiceButton.GetComponentInChildren<Text>().text = choiceText;
                choiceButton.SetActive(true);
                Debug.Log($"[NPC:{gameObject.name}] Setting choice button {i+1} text to: \"{choiceText}\"");
            }
        }
    }

    private void HideChoiceButtons()
    {
        // Hide player info and show NPC info
        if (playerNameText != null)
        {
            playerNameText.gameObject.SetActive(false);
        }
        if (playerPortraitImage != null && currentSpeakerImage != null)
        {
            playerPortraitImage.SetActive(false);
            currentSpeakerImage.SetActive(true); // Show NPC image again
        }

        // Show NPC name again
        if (npcNameText != null)
        {
            npcNameText.gameObject.SetActive(true);
        }

        // Hide choice buttons
        choiceButton1.SetActive(false);
        choiceButton2.SetActive(false);
        choiceButton3.SetActive(false);
    }

    private GameObject GetChoiceButton(int index)
    {
        switch (index)
        {
            case 0: return choiceButton1;
            case 1: return choiceButton2;
            case 2: return choiceButton3;
            default: return null;
        }
    }

    // Add new methods to handle nested dialogues
private IEnumerator ShowNestedChoicesAfterDelay()
{
    Debug.Log("ShowNestedChoicesAfterDelay: Waiting to show nested choices");
    yield return new WaitForSeconds(2.0f);
    
    if (currentChoices != null && currentChoices.Length > 0)
    {
        Debug.Log($"Showing {currentChoices.Length} nested choices");
        ShowNestedChoices();
    }
    else
    {
        Debug.LogWarning("No nested choices to show, closing dialogue");
        zeroText();
    }
}

private void ShowNestedChoices()
{
    Debug.Log($"ShowNestedChoices: Displaying {currentChoices.Length} nested choice buttons");
    
    currentDialogueType = DialogueType.Choice;
    
    // Show player info and hide NPC info (same as ShowChoices)
    if (playerNameText != null)
    {
        string firstName = PlayerPrefs.GetString("FirstName", "Player");
        string lastName = PlayerPrefs.GetString("LastName", "");
        playerNameText.text = $"{firstName} {lastName}".Trim();
        playerNameText.gameObject.SetActive(true);
    }

    // Show player portrait and hide NPC image
    if (playerPortraitImage != null && currentSpeakerImage != null)
    {
        GameObject boySprite = GameObject.Find("BoySprite");
        GameObject girlSprite = GameObject.Find("GirlSprite");
        
        if ((boySprite != null && boySprite.activeSelf) || (girlSprite != null && girlSprite.activeSelf))
        {
            SpriteRenderer activeSprite = boySprite != null && boySprite.activeSelf ? 
                boySprite.GetComponent<SpriteRenderer>() : 
                girlSprite.GetComponent<SpriteRenderer>();

            if (activeSprite != null)
            {
                playerPortraitImage.GetComponent<Image>().sprite = activeSprite.sprite;
                currentSpeakerImage.SetActive(false);
                playerPortraitImage.SetActive(true);
            }
        }
    }

    // Hide NPC name during player choice
    if (npcNameText != null)
    {
        npcNameText.gameObject.SetActive(false);
    }

    // Show choice buttons
    int buttonCount = Mathf.Min(currentChoices.Length, 3); // Maximum of 3 choices
    for (int i = 0; i < buttonCount; i++)
    {
        GameObject choiceButton = GetChoiceButton(i);
        if (choiceButton != null)
        {
            string choiceText = currentChoices[i].text;
            choiceButton.GetComponentInChildren<Text>().text = choiceText;
            choiceButton.SetActive(true);
            Debug.Log($"Setting nested choice button {i+1} text to: \"{choiceText}\"");
            
            // Ensure the button uses the nested choice handler
            Button btn = choiceButton.GetComponent<Button>();
            if (btn != null)
            {
                int index = i; // Capture the index for the lambda
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(() => OnNestedChoiceSelected(index));
            }
        }
    }
}

// New method to handle nested choice selection
private void OnNestedChoiceSelected(int choiceIndex)
{
    Debug.Log($"Player selected nested choice {choiceIndex + 1}");
    
    if (currentChoices == null || choiceIndex >= currentChoices.Length)
    {
        Debug.LogError($"Invalid nested choice index: {choiceIndex}");
        return;
    }
    
    HideChoiceButtons();
    
    // Get the selected choice
    var choice = currentChoices[choiceIndex];
    
    // Show response in dialogue
    dialogueText.text = choice.response;
    Debug.Log($"Showing nested choice response: {choice.response}");
    
    // Check if this choice has further nested choices
    if (choice.nextChoices != null && choice.nextChoices.Length > 0)
    {
        // Store current level
        choiceStack.Add(currentChoices);
        choiceIndexStack.Add(choiceIndex);
        
        // Set up for the next level of choices
        currentChoices = choice.nextChoices;
        
        // Show these choices after a delay
        StartCoroutine(ShowNestedChoicesAfterDelay());
    }
    // Check if we should open quest book (e.g., "Yes, show me the Quest Book")
    else if (choice.openQuestBook)
    {
        Debug.Log("Opening quest book after nested choice");
        StartCoroutine(OpenQuestBookAfterDelay());
    }
    // Check if we should load a scene (e.g., "Yes, please show me the tutorial")
    else if (!string.IsNullOrEmpty(choice.sceneToLoad))
    {
        Debug.Log($"Loading scene from nested choice: {choice.sceneToLoad}");
        StartCoroutine(LoadSceneAfterDelay(choice.sceneToLoad));
    }
    // Otherwise just close dialogue after showing response
    else
    {
        StartCoroutine(CloseDialogueAfterDelay());
    }
}

private IEnumerator OpenQuestBookAfterDelay()
{
    Debug.Log("OpenQuestBookAfterDelay called");
    
    // Wait for player to read the response
    yield return new WaitForSeconds(1.5f);
    
    // Hide dialogue first
    dialoguePanel.SetActive(false);
    
    // Show quest book if manager is available
    if (questBookManager != null)
    {
        questBookManager.ShowQuestBook();
    }
    else
    {
        Debug.LogWarning("QuestBookManager not assigned! Cannot open quest book.");
        // Just close the dialogue if we can't open quest book
        zeroText();
    }
}

// Method that works with NPCs in tutorial - Keep only this version
private void AttemptToFixNPCDialogue(string npcName)
{
    // Try to load the specific NPC's dialogue directly from resources
    TextAsset npcDialogue = Resources.Load<TextAsset>($"DialogueData/Tutorial{npcName}");
    if (npcDialogue != null) 
    {
        Debug.Log($"Loading {npcName}'s dialogue file directly from Resources");
        dialogueFile = npcDialogue;
        
        // Parse the dialogue again
        currentDialogue = JsonUtility.FromJson<DialogueData>(dialogueFile.text);
        dialogue = new string[] { currentDialogue.initialDialogue };
        narration = new string[] { currentDialogue.initialNarration };
        
        Debug.Log($"{npcName} dialogue fixed. NpcName: {currentDialogue.npcName}");
        
        if (currentDialogue.choices != null)
        {
            for (int i = 0; i < currentDialogue.choices.Length; i++)
            {
                Debug.Log($"{npcName} Choice {i+1}: {currentDialogue.choices[i].text}");
                Debug.Log($"{npcName} Reward {i+1}: {currentDialogue.choices[i].reward.sprite}");
            }
        }
        else
        {
            Debug.LogError($"{npcName}'s dialogue has no choices!");
        }
    }
    else
    {
        Debug.LogError($"Could not find {npcName}'s dialogue file in Resources/DialogueData/Tutorial{npcName}");
    }
}

private async void OnChoiceSelected(int choiceIndex)
{
    Debug.Log($"Player chose option {choiceIndex + 1}");
    HideChoiceButtons();   
    
    // Reset the flag
    choiceIsStartTutorial = false;
    
    // Add debug logs to track FreeRoam dialog flow
    if (dialogueFile != null && dialogueFile.name == "FreeRoam")
    {
        Debug.Log($"Processing FreeRoam dialogue choice {choiceIndex + 1}");
        
        // Get the choice data
        var choice = currentDialogue.choices[choiceIndex];
        
        // Show response in dialogue
        dialogueText.text = choice.response;
        Debug.Log($"Showing response: {choice.response}");
        
        // Check if this choice has nested choices
        if (choice.nextChoices != null && choice.nextChoices.Length > 0)
        {
            Debug.Log($"This choice has {choice.nextChoices.Length} nested choices");
            
            // Store current level
            choiceStack.Add(currentDialogue.choices);
            choiceIndexStack.Add(choiceIndex);
            
            // Set up for the next level of choices
            currentChoices = choice.nextChoices;
            isInNestedDialogue = true;
            
            // Show these choices after a delay
            StartCoroutine(ShowNestedChoicesAfterDelay());
            return;
        }
        
        // If this isn't a choice with nested choices, check if we should open the quest book
        // This should only happen for specific nested choices, not the first level
        if (choice.openQuestBook)
        {
            Debug.Log("Choice has openQuestBook=true flag");
            StartCoroutine(OpenQuestBookAfterDelay());
            return;
        }
        
        // Check if there's a scene to load
        if (!string.IsNullOrEmpty(choice.sceneToLoad))
        {
            Debug.Log($"Will load scene after delay: {choice.sceneToLoad}");
            
            // Check if this is the tutorial choice (first option)
            if (choiceIndex == 0 && choice.text.Contains("start") && choice.text.Contains("lessons"))
            {
                choiceIsStartTutorial = true;
            }
            
            StartCoroutine(LoadSceneAfterDelay(choice.sceneToLoad));
            return;
        }
        
        // If we get here, just close the dialogue after a delay
        StartCoroutine(CloseDialogueAfterDelay());
        return;
    }
    
    // Check if this is a tutorial NPC
    bool isTutorial = dialogueFile.name.Contains("Tutorial");
    if (isTutorial)
    {
        // Get actual NPC name using our consistent method
        string actualNpcName = GetActualNPCName();
        Debug.Log($"Tutorial choice selected for NPC: {actualNpcName}");
        
        // For any NPC, verify we have the right dialogue data
        if (currentDialogue.npcName != actualNpcName)
        {
            Debug.LogError($"{actualNpcName} has wrong dialogue data! Attempting to fix before showing reward...");
            AttemptToFixNPCDialogue(actualNpcName);
            
            // Make sure fix worked before continuing
            if (currentDialogue.npcName != actualNpcName || choiceIndex >= currentDialogue.choices.Length)
            {
                Debug.LogError($"Failed to fix {actualNpcName}'s dialogue. Cannot continue.");
                return;
            }
        }
        
        // For tutorial NPCs, don't transition to a new scene
        var choice = currentDialogue.choices[choiceIndex];
        
        // Save progress to API - use the SaveRewardToAPI method
        await SaveRewardToAPI(actualNpcName, choiceIndex, choice.reward.sprite, choice.response);
        
        // Show reward popup
        ShowRewardPopup(choiceIndex);
        
        // Mark this dialogue as completed
        DialogueState.SetDialogueCompleted(actualNpcName);
        hasCompletedDialogue = true;
        
        // Handle specific NPCs
        if (actualNpcName == "Janica")
        {
            PlayerPrefs.SetInt("TutorialJanica", 1);
            PlayerPrefs.Save();
            
            // Find and enable Mark NPC - Add this code
            GameObject markNPC = GameObject.Find("NPC Mark Tutorial");
            if (markNPC != null)
            {
                markNPC.SetActive(true);
                Debug.Log("Mark NPC has been enabled after Janica dialogue completion");
            }
            else
            {
                Debug.LogError("Mark NPC not found in scene after Janica dialogue!");
            }
            
            // Notify the TutorialManager instead
            if (TutorialManager.Instance != null)
            {
                TutorialManager.Instance.OnNPCDialogueCompleted("Janica");
            }
            else
            {
                Debug.LogWarning("TutorialManager not found for Janica completion");
            }
            
            Debug.Log("Janica tutorial completed");
        }
        else if (actualNpcName == "Mark")
        {
            PlayerPrefs.SetInt("TutorialMark", 1);
            PlayerPrefs.Save();
            
            // Find and enable Annie NPC
            GameObject annieNPC = GameObject.Find("NPC Annie Tutorial");
            if (annieNPC != null)
            {
                annieNPC.SetActive(true);
                Debug.Log("Annie NPC has been enabled");
                
                // Notify the TutorialManager instead
                if (TutorialManager.Instance != null)
                {
                    TutorialManager.Instance.OnNPCDialogueCompleted("Mark");
                }
                else
                {
                    Debug.LogWarning("TutorialManager not found for Mark completion");
                }
            }
            else
            {
                Debug.LogError("Annie NPC not found in scene! Creating a delayed search for Annie...");
                StartCoroutine(FindAnnieWithDelay());
            }
            
            // Update tutorial checkpoint
            PlayerPrefs.SetString("TutorialCheckpoint", "Mark");
            Debug.Log("Tutorial checkpoint set to Mark");
        }
        else if (actualNpcName == "Annie")
        {
            // Set specific checkpoint for Annie
            PlayerPrefs.SetString("TutorialCheckpoint", "Annie");
            
            // Notify the TutorialManager instead
            if (TutorialManager.Instance != null)
            {
                TutorialManager.Instance.OnNPCDialogueCompleted("Annie");
            }
            else
            {
                Debug.LogWarning("TutorialManager not found for Annie completion");
            }
            
            Debug.Log("Tutorial checkpoint set to Annie");
        }
        else if (actualNpcName == "Rojan")
        {
            // Set specific checkpoint for Rojan - marks the full tutorial completion
            PlayerPrefs.SetInt("TutorialRojan", 1);
            PlayerPrefs.Save();
            
            // Notify the TutorialManager instead
            if (TutorialManager.Instance != null)
            {
                TutorialManager.Instance.OnNPCDialogueCompleted("Rojan");
            }
            else
            {
                Debug.LogWarning("TutorialManager not found for Rojan completion");
            }
            
            Debug.Log("Tutorial fully completed with Rojan");
        }
    }
    else
    {
        // Original behavior for non-tutorial NPCs
        if (dialogueFile.name == "FreeRoam")
        {
            // Get the choice data
            var choice = currentDialogue.choices[choiceIndex];
            
            // Show response in dialogue
            dialogueText.text = choice.response;
            
            // If there's a scene to load, load it after a delay
            if (!string.IsNullOrEmpty(choice.sceneToLoad))
            {
                StartCoroutine(LoadSceneAfterDelay(choice.sceneToLoad));
            }
            else
            {
                // Just close the dialogue if staying in current scene
                StartCoroutine(CloseDialogueAfterDelay());
            }
        }
        else
        {
            // Non-tutorial NPC handling for regular scene transitions
            var choice = currentDialogue.choices[choiceIndex];
            
            // Show the NPC's response to the player's choice
            dialogueText.text = choice.response;
            
            // Wait 2 seconds before transitioning
            await Task.Delay(2000); // Use Task.Delay instead of yield return
            
            // If there's a scene to load, transition to it
            if (!string.IsNullOrEmpty(choice.sceneToLoad))
            {
                Debug.Log($"Loading scene: {choice.sceneToLoad}");
                
                // Check if we have a scene transition component
                if (sceneTransition != null)
                {
                    sceneTransition.StartTransition(choice.sceneToLoad);
                }
                else
                {
                    // Fallback to direct scene loading
                    SceneManager.LoadScene(choice.sceneToLoad);
                }
            }
            else
            {
                // No scene to load, just close the dialogue
                zeroText();
            }
        }
    }

    // Play response audio if available (using index based on dialogue length + choice index)
    int responseAudioIndex = dialogue.Length + choiceIndex;
    PlayDialogueAudio(responseAudioIndex);
}

    // Add these new coroutines for FreeRoam dialogue handling
    private IEnumerator LoadSceneAfterDelay(string sceneName)
    {
        Debug.Log($"LoadSceneAfterDelay called for scene: {sceneName}");
        
        // Wait for player to read the response
        yield return new WaitForSeconds(2.5f);
        
        // Check if using quest book for tutorial
        if (useQuestBookForTutorial && choiceIsStartTutorial)
        {
            Debug.Log("Opening Quest Book instead of loading scene directly");
            
            // Hide dialogue first
            dialoguePanel.SetActive(false);
            
            // Show quest book if manager is available
            if (questBookManager != null)
            {
                questBookManager.ShowQuestBook();
                yield break; // Exit the coroutine without loading scene
            }
            else
            {
                Debug.LogWarning("QuestBookManager not assigned but useQuestBookForTutorial is enabled!");
                // Fall through to regular scene loading if no quest book manager
            }
        }
        
        // Regular scene loading code
        // Get the actual scene name (without path) if needed
        string actualSceneName = sceneName;
        
        // If the scene name contains a path, extract just the scene name
        if (sceneName.Contains("/"))
        {
            actualSceneName = sceneName.Substring(sceneName.LastIndexOf('/') + 1);
            Debug.Log($"Extracted scene name from path: {actualSceneName}");
        }
        
        // Try to find the scene in the build settings first by its full path
        bool sceneExists = false;
        for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
        {
            string scenePath = SceneUtility.GetScenePathByBuildIndex(i);
            if (scenePath.EndsWith(sceneName) || scenePath.EndsWith(actualSceneName + ".unity"))
            {
                sceneExists = true;
                break;
            }
        }
        
        // Log detailed information for debugging
        Debug.Log($"FreeRoam NPC: Attempting to load scene: {sceneName} (Actual: {actualSceneName})");
        Debug.Log($"Scene exists in build settings: {sceneExists}");
        
        // Use scene transition if available
        if (sceneTransition != null)
        {
            Debug.Log($"FreeRoam NPC: Loading scene with transition: {sceneName}");
            sceneTransition.StartTransition(sceneName);
        }
        else
        {
            // Try to load by the original scene path first
            try
            {
                Debug.Log($"FreeRoam NPC: Loading scene directly: {sceneName}");
                SceneManager.LoadScene(sceneName);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to load scene by path: {e.Message}");
                // Fallback to just the scene name without path
                try
                {
                    Debug.Log($"FreeRoam NPC: Trying fallback to just scene name: {actualSceneName}");
                    SceneManager.LoadScene(actualSceneName);
                }
                catch (System.Exception e2)
                {
                    Debug.LogError($"Failed to load scene by name too: {e2.Message}");
                }
            }
        }
    }

    private IEnumerator CloseDialogueAfterDelay()
    {
        // Wait for player to read the response
        yield return new WaitForSeconds(2.0f);
        
        // Close dialogue
        zeroText();
        Debug.Log("FreeRoam NPC: Closing dialogue after delay");
    }

    private async Task SaveChoiceToAPI(int choiceIndex)
    {
        try
        {
            playerUsername = GetCurrentUsername();
            Debug.Log($"Saving progress for user: {playerUsername}");
            if (string.IsNullOrEmpty(playerUsername))
            {
                throw new Exception("Username is empty or null");
            }

            using (var handler = CustomHttpHandler.GetInsecureHandler())
            using (var client = new HttpClient(handler))
            {
                client.DefaultRequestHeaders.Accept.Add(
                    new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));

                var choice = currentDialogue.choices[choiceIndex];
                var reward = choice.reward;
                string currentNpcName = gameObject.name.Replace("NPC ", "").Replace(" Tutorial", "");
                Debug.Log($"Saving progress for NPC: {currentNpcName} with reward: {reward.sprite}");
                
                var progressData = new
                {
                    Username = playerUsername,
                    tutorial = new
                    {
                        status = $"{currentNpcName} Completed", // Use the correct NPC name
                        reward = reward.sprite,
                        date = DateTime.UtcNow.ToString("o")
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
                                    npcsTalkedTo = new[] { currentNpcName } // Use the correct NPC name
                                }
                            }
                        },
                        // Add empty structures for the other required units
                        ["Unit2"] = new
                        {
                            status = "Not Started",
                            completedLessons = 0,
                            unitScore = 0,
                            lessons = new Dictionary<string, object>()
                        },
                        ["Unit3"] = new
                        {
                            status = "Not Started",
                            completedLessons = 0,
                            unitScore = 0,
                            lessons = new Dictionary<string, object>()
                        },
                        ["Unit4"] = new
                        {
                            status = "Not Started",
                            completedLessons = 0,
                            unitScore = 0,
                            lessons = new Dictionary<string, object>()
                        }
                    }
                };

                string jsonData = Newtonsoft.Json.JsonConvert.SerializeObject(progressData);
                Debug.Log($"Sending data: {jsonData}");

                var content = new StringContent(jsonData, Encoding.UTF8, "application/json");
                
                Debug.Log($"Sending POST request to: {baseUrl}/game_progress");
                Debug.Log($"Request headers: {content.Headers}");

                var response = await client.PostAsync($"{baseUrl}/game_progress", content);
                string responseContent = await response.Content.ReadAsStringAsync();
                Debug.Log($"Response ({response.StatusCode}): {responseContent}");

                if (!response.IsSuccessStatusCode)
                {
                    throw new Exception($"Server error: {response.StatusCode} - {responseContent}");
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"SaveChoiceToAPI failed: {ex.Message}");
            throw;
        }
    }

    private void ShowNPCReply(int choiceIndex)
    {
        isReplying = true;
        SetSprite(npcPovSprite);
        if (choiceIndex >= 0 && choiceIndex < currentDialogue.choices.Length)
        {
            string response = currentDialogue.choices[choiceIndex].response;
            dialogueText.text = response;
            StartCoroutine(TypeDialogueAndTransition(response));
        }
    }

    private IEnumerator TypeDialogueAndTransition(string text)
    {
        // First type out the response
        yield return StartCoroutine(TypeDialogue(text));
        
        // Show continue button and wait for click/tap
        contButton.SetActive(true);
        while (!Mouse.current.leftButton.wasPressedThisFrame && 
               !(Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame))
        {
            yield return null;
        }
        contButton.SetActive(false);
        
        // Show the lesson learned text
        dialogueText.text = currentDialogue.lessonLearned;
        yield return StartCoroutine(TypeDialogue(currentDialogue.lessonLearned));
   
        // Wait a moment before transition
        yield return new WaitForSeconds(2f);
        
        // Start the scene transition with loading animation
        if (sceneTransition != null)
        {
            Debug.Log("Starting transition with loading animation");
            // Get LoadingIdleCanvas from scene instead of specific lesson
            GameObject loadingCanvas = GameObject.Find("LoadingIdleCanvas");
            if (loadingCanvas != null)
            {
                loadingCanvas.SetActive(true);
                yield return new WaitForSeconds(1f);
            }

            // Use sceneToLoad from currentDialogue or SceneTransition loading animation
            string nextScene = string.IsNullOrEmpty(currentDialogue.sceneToLoad) ? 
                sceneTransition.sceneToLoad : currentDialogue.sceneToLoad;
            sceneTransition.StartTransition(nextScene);
        }
        else
        {
            Debug.LogError("SceneTransition reference is missing!");
        }
    }

    private void SetSprite(Sprite newSprite)
    {
        if (currentSpeakerImage != null)
        {
            Image speakerImage = currentSpeakerImage.GetComponent<Image>();
            if (speakerImage != null)
            {
                if (firstLineIsPlayerSprite && index == 0)
                {
                    // Handle player sprite
                    GameObject boySprite = GameObject.Find("BoySprite");
                    GameObject girlSprite = GameObject.Find("GirlSprite");
                    
                    if ((boySprite != null && boySprite.activeSelf) || (girlSprite != null && girlSprite.activeSelf))
                    {
                        SpriteRenderer activeSprite = boySprite != null && boySprite.activeSelf ? 
                            boySprite.GetComponent<SpriteRenderer>() : 
                            girlSprite.GetComponent<SpriteRenderer>();

                        if (activeSprite != null)
                        {
                            speakerImage.sprite = activeSprite.sprite;
                        }
                    }
                }
                else
                {
                    // For NPC sprite, use the sprite from the NPC's SpriteRenderer if available
                    SpriteRenderer npcSpriteRenderer = GetComponent<SpriteRenderer>();
                    if (npcSpriteRenderer != null && npcSpriteRenderer.sprite != null)
                    {
                        speakerImage.sprite = npcSpriteRenderer.sprite;
                    }
                    else if (npcSprite != null) // Fallback to assigned npcSprite
                    {
                        speakerImage.sprite = npcSprite;
                    }
                    else // Final fallback to newSprite parameter
                    {
                        speakerImage.sprite = newSprite;
                    }
                }
                
                // Ensure the image is visible
                speakerImage.enabled = true;
                currentSpeakerImage.SetActive(true);
            }
        }
    }

    private IEnumerator TypeNPCReplyLines(string[] replyLines)
    {
        currentDialogueType = DialogueType.Reply;
        npcReplyHistory.Clear(); // Clear previous reply history
        for (int i = 0; i < replyLines.Length; i++)
        {
            // Store in reply history
            npcReplyHistory.Add(replyLines[i]);
            spriteHistory.Add(npcPovSprite);
            // Update back button visibility
            UpdateBackButtonVisibility();
            yield return StartCoroutine(TypingWithCallback(replyLines[i], null));
            if (i < replyLines.Length - 1)
            {
                contButton.SetActive(true);
                yield return new WaitUntil(() => Mouse.current.leftButton.wasPressedThisFrame || (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame));
                contButton.SetActive(false);
            }
        }
        StartNarration();
    }

    private void StartNarration()
    {
        isReplying = false;
        isNarrating = true;
        narrationIndex = 0;
        SetSprite(npcSprite);
        ContinueNarration();
    }

    private void ContinueNarration()
    {
        if (narrationIndex < narration.Length)
        {
            StartTypingNarration();
        }
        else
        {
            CompleteDialogue();
        }
    }

    private void StartTypingNarration()
    {
        currentDialogueType = DialogueType.Narration;
        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
        }
        // Store in narration history
        narrationHistory.Add(narration[narrationIndex]);
        spriteHistory.Add(npcSprite);
        // Update back button visibility
        UpdateBackButtonVisibility();
        typingCoroutine = StartCoroutine(TypingWithCallback(narration[narrationIndex], () => {
            narrationIndex++;
            ContinueNarration();
        }));
    }

    private IEnumerator TypingWithCallback(string textToType, System.Action callback)
    {
        dialogueText.text = "";
        foreach (char letter in textToType.ToCharArray())
        {
            dialogueText.text += letter;
            yield return new WaitForSeconds(wordSpeed);
        }
        contButton.SetActive(true);

        // Wait for either mouse click or touch
        while (true)
        {
            // Check for mouse click
            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            {
                break;
            }

            // Check for touch
            if (Touchscreen.current != null)
            {
                var touch = Touchscreen.current.primaryTouch;
                if (touch.press.isPressed)
                {
                    break;
                }
            }

            // Check if continue button was clicked directly
            if (!contButton.activeInHierarchy)
            {
                break;
            }

            yield return null;
        }
        contButton.SetActive(false);
        callback?.Invoke();
    }

    private void CompleteDialogue()
    {
        isDialogueComplete = true;
        StartCoroutine(TransitionToLessonSelection());
    }

    private IEnumerator TransitionToLessonSelection()
    {
        yield return new WaitForSeconds(1f);
        if (sceneTransition != null)
        {
            // Use the scene name from SceneTransition instead of hardcoding
            sceneTransition.StartTransition(sceneTransition.sceneToLoad);
        }
        else
        {
            Debug.LogError("SceneTransition reference is missing!");
        }
    }

    private void UpdateBackButtonVisibility()
    {
        if (backButton != null)
        {
            bool shouldShow = false;
            switch (currentDialogueType)
            {
                case DialogueType.NPC:
                    shouldShow = npcDialogueHistory.Count > 1;
                    break;
                case DialogueType.Reply:
                    shouldShow = npcReplyHistory.Count > 1;
                    break;
                case DialogueType.Narration:
                    shouldShow = narrationHistory.Count > 1;
                    break;
                case DialogueType.Choice:
                    shouldShow = false;
                    break;
            }
            backButton.SetActive(shouldShow);
        }
    }

    private IEnumerator TypeDialogue(string text)
    {
        dialogueText.text = "";
        foreach (char c in text.ToCharArray())
        {
            dialogueText.text += c;
            yield return new WaitForSeconds(wordSpeed);
        }
    }

    private void EnsureAnnieHasCorrectDialogue()
    {
        if (gameObject.name.Contains("Annie"))
        {
            // If Annie's dialogue references Mark or has incorrect NPC name, reload it
            if (currentDialogue == null || currentDialogue.npcName != "Annie")
            {
                Debug.LogWarning($"Annie has incorrect dialogue data: {currentDialogue?.npcName ?? "null"}");
                // Try to find and load Annie's dialogue explicitly
                TextAsset annieDialogue = Resources.Load<TextAsset>("DialogueData/TutorialAnnie");
                if (annieDialogue != null)
                {
                    Debug.Log("Loading Annie's dialogue file explicitly");
                    dialogueFile = annieDialogue;
                    LoadDialogueFromJson();
                }
                else
                {
                    Debug.LogError("Could not find Annie's dialogue file!");
                }
            }
            else
            {
                Debug.Log($"Successfully loaded Annie's dialogue: Initial message: {currentDialogue.initialDialogue}");
                if (currentDialogue.choices != null && currentDialogue.choices.Length > 0)
                {
                    Debug.Log($"Annie's reward sprite: {currentDialogue.choices[0].reward.sprite}");
                }
            }
        }
    }

    private void ShowCompletedResponse()
    {
        dialoguePanel.SetActive(true);
        
        if (currentDialogue != null && currentDialogue.choices.Length > 0)
        {
            dialogueText.text = currentDialogue.choices[0].response;
            
            // No choice buttons for completed dialogues
            choiceButton1?.SetActive(false);
            choiceButton2?.SetActive(false);
            choiceButton3?.SetActive(false);
            
            // Show continue button
            contButton?.SetActive(true);
        }
        else
        {
            dialogueText.text = "...";
        }
        
       // FreezePlayer();
    }

    // Add this function to ensure each NPC loads their own file
    public void ReloadDialogueFile()
    {
        Debug.Log($"[NPC:{gameObject.name}] Forcing reload of dialogue for {gameObject.name} with file {dialogueFile?.name ?? "null"}");
        
        // Clear existing data to ensure a clean reload
        if (currentDialogue != null)
        {
            currentDialogue = null;
        }
        
        // Reload the dialogue file
        LoadDialogueFromJson();
        
        // Additional validation for Annie's dialogue
        if (gameObject.name.Contains("Annie"))
        {
            string expectedNpcName = "Annie";
            if (currentDialogue != null)
            {
                if (currentDialogue.npcName != expectedNpcName)
                {
                    Debug.LogError($"Dialogue mismatch: Expected {expectedNpcName} but got {currentDialogue.npcName}");
                    // Emergency fix: try to load Annie's dialogue directly
                    TextAsset annieDialogue = Resources.Load<TextAsset>("DialogueData/TutorialAnnie");
                    if (annieDialogue != null)
                    {
                        Debug.Log("Emergency: Loading Annie's dialogue file directly");
                        dialogueFile = annieDialogue;
                        LoadDialogueFromJson();
                    }
                    else
                    {
                        Debug.LogError("Could not find Annie's dialogue file!");
                    }
                }
                else
                {
                    Debug.Log($"Successfully loaded {expectedNpcName}'s dialogue: Initial message: {currentDialogue.initialDialogue}");
                    if (currentDialogue.choices != null && currentDialogue.choices.Length > 0)
                    {
                        Debug.Log($"Annie's reward sprite: {currentDialogue.choices[0].reward.sprite}");
                    }
                }
            }
        }
    }

    private void OnDisable()
    {
        // Add this method to ensure choices are hidden when NPC is disabled
        HideChoiceButtons();
        
        // Clean up any active coroutines
        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
            typingCoroutine = null;
        }
        
        // Reset dialogue state
        if (dialoguePanel != null)
        {
            dialoguePanel.SetActive(false);
        }
        
        // Notify the DialogueCoordinator that this dialogue has ended
        NPCDialogueCoordinator.Instance?.EndDialogue(this);
    }

    public void SetUseRewardSystem(bool use)
    {
        useRewardSystem = use;
    }

    private string GetActualNPCName()
    {
        // Use cached value if we have it
        if (!string.IsNullOrEmpty(_cachedNpcIdentifier))
        {
            return _cachedNpcIdentifier;
        }
        
        // Fall back to the GameObject name parsing
        _cachedNpcIdentifier = gameObject.name.Replace("NPC ", "").Replace(" Tutorial", "");
        return _cachedNpcIdentifier;
    }

    private void AttemptToFixAnnieDialogue()
    {
        // Try to load Annie's dialogue directly from resources
        TextAsset annieDialogue = Resources.Load<TextAsset>("DialogueData/TutorialAnnie");
        if (annieDialogue != null)
        {
            Debug.Log("Loading Annie's dialogue file directly from Resources");
            dialogueFile = annieDialogue;
            
            // Parse the dialogue again
            currentDialogue = JsonUtility.FromJson<DialogueData>(dialogueFile.text);
            dialogue = new string[] { currentDialogue.initialDialogue };
            narration = new string[] { currentDialogue.initialNarration };
            
            Debug.Log($"Annie dialogue fixed. NpcName: {currentDialogue.npcName}");
            
            if (currentDialogue.choices != null)
            {
                for (int i = 0; i < currentDialogue.choices.Length; i++)
                {
                    Debug.Log($"Annie Choice {i+1}: {currentDialogue.choices[i].text}");
                    Debug.Log($"Annie Reward {i+1}: {currentDialogue.choices[i].reward.sprite}");
                }
            }
            else
            {
                Debug.LogError("Annie's dialogue has no choices!");
            }
        }
        else
        {
            Debug.LogError("Could not find Annie's dialogue file in Resources/DialogueData/TutorialAnnie");
        }
    }

    public void ResetTutorialState()
    {
        // Reset dialogue completion
        if (currentDialogue != null)
        {
            DialogueState.ResetDialogueState(currentDialogue.npcName);
            hasCompletedDialogue = false;
        }
        
        // Reset NPC specific states
        if (gameObject.name.Contains("Janica"))
        {
            PlayerPrefs.DeleteKey("TutorialJanica");
        }
        else if (gameObject.name.Contains("Mark"))
        {
            PlayerPrefs.DeleteKey("TutorialMark");
            PlayerPrefs.DeleteKey("FirstMeetingMark");
        }
        else if (gameObject.name.Contains("Annie"))
        {
            // Annie-specific reset
        }
        else if (gameObject.name.Contains("Rojan"))
        {
            PlayerPrefs.DeleteKey("TutorialRojan");
        }
        
        // Reset global tutorial state
        if (PlayerPrefs.GetString("TutorialCheckpoint", "") != "")
        {
            PlayerPrefs.DeleteKey("TutorialCheckpoint");
        }
        
        Debug.Log($"Tutorial state reset for {gameObject.name}");
    }

    private void HandleJanicaDialogueFlow()
    {
        // Move to next dialogue line
        if (index < dialogue.Length - 1)
        {
            index++;
            
            // Get the JanicaTutorialHandler component
            JanicaTutorialHandler tutorialHandler = GetComponent<JanicaTutorialHandler>();
            
            // Handle arrow positioning based on current step
            if (tutorialHandler != null)
            {
                tutorialHandler.HandleInstructionStep(index);
            }
            else
            {
                // Fallback for old functionality
                GameObject upArrow = GameObject.Find("UpArrow");
                
                // Hide arrow as default action
                if (upArrow != null)
                {
                    upArrow.SetActive(index == 1 || index == 2);
                }
            }
            StartTyping();
        }
        else
        {
            // We've reached the end, show choices
            ShowChoices();
            // Ensure arrow is hidden
            GameObject upArrow = GameObject.Find("UpArrow");
            if (upArrow != null)
            {
                upArrow.SetActive(false);
            }
        }
    }

    // Play dialogue audio for current index
    public void PlayDialogueAudio(int dialogueIndex)
    {
        if (isAudioMuted || dialogueAudioSource == null)
            return;

        // Make sure we don't exceed the array length
        if (dialogueAudioClips != null && dialogueIndex < dialogueAudioClips.Length && dialogueAudioClips[dialogueIndex] != null)
        {
            currentAudioIndex = dialogueIndex;
            dialogueAudioSource.clip = dialogueAudioClips[dialogueIndex];
            dialogueAudioSource.Play();
            Debug.Log($"Playing audio clip {dialogueIndex} for NPC {gameObject.name}");
        }
        else
        {
            Debug.LogWarning($"No audio clip available for dialogue index {dialogueIndex}");
        }
    }

    // Toggle audio mute state
    public void ToggleAudioMute()
    {
        isAudioMuted = !isAudioMuted;
        
        if (dialogueAudioSource != null)
        {
            dialogueAudioSource.mute = isAudioMuted;
            
            // If unmuting, replay current audio
            if (!isAudioMuted && dialogueAudioSource.clip != null && !dialogueAudioSource.isPlaying)
            {
                dialogueAudioSource.Play();
            }
        }
        
        UpdateSpeakerButtonUI();
    }

    // Update speaker button appearance based on mute status
    private void UpdateSpeakerButtonUI()
    {
        if (speakerButton != null)
        {
            // Change the button image/color based on mute status
            Image buttonImage = speakerButton.GetComponent<Image>();
            if (buttonImage != null)
            {
                // You could change the color or use different sprites for muted/unmuted
                buttonImage.color = isAudioMuted ? Color.gray : Color.white;
            }
        }
    }

    // Initialize the choice text components
    private void InitializeChoiceTexts()
    {
        if (choiceTexts == null || choiceTexts.Length == 0)
        {
            // Initialize the array if needed
            if (currentDialogue?.choices != null)
            {
                choiceTexts = new Text[currentDialogue.choices.Length];
                for (int i = 0; i < currentDialogue.choices.Length; i++)
                {
                    // Try to get text components from choice buttons
                    GameObject choiceButton = GetChoiceButton(i);
                    if (choiceButton != null)
                    {
                        Text textComp = choiceButton.GetComponentInChildren<Text>();
                        if (textComp != null)
                        {
                            choiceTexts[i] = textComp;
                        }
                    }
                }
            }
        }
    }

    // Set up the reward popup UI
    private void SetupRewardPopup()
    {
        if (rewardPopup == null || currentDialogue?.choices == null)
            return;
            
        // Ensure the reward popup is initially hidden
        rewardPopup.SetActive(false);
        
        // Set up the close button if needed
        if (closeRewardButton != null)
        {
            closeRewardButton.onClick.RemoveAllListeners();
        }
    }

    // Start quest label sequence for UI
    private void StartQuestLabelSequence()
    {
        if (questLabelText == null)
            return;
            
        // Clear existing text
        questLabelText.text = "";
        
        // Start coroutine to type out quest text
        if (questCoroutine != null)
        {
            StopCoroutine(questCoroutine);
        }
        questCoroutine = StartCoroutine(TypeQuestLabel());
    }

    // Coroutine to type out quest label text
    private IEnumerator TypeQuestLabel()
    {
        // First type out the lesson title
        questLabelText.text = "";
        foreach (char c in lessonTitle)
        {
            questLabelText.text += c;
            yield return new WaitForSeconds(typingSpeed);
        }

        // Wait a moment before showing the action
        yield return new WaitForSeconds(1f);

        // Add new line and type out the action
        questLabelText.text += "\n";
        string actionText = "► " + questAction; // Add an arrow or bullet point
        foreach (char c in actionText)
        {
            questLabelText.text += c;
            yield return new WaitForSeconds(typingSpeed);
        }
    }

    // Auto close dialogue after delay
    private IEnumerator AutoCloseDialogue()
    {
        yield return new WaitForSeconds(3f);
        zeroText();
    }

    // Find Annie NPC with delay
    private IEnumerator FindAnnieWithDelay()
    {
        // Try multiple times with delays between attempts
        for (int i = 0; i < 5; i++)
        {
            yield return new WaitForSeconds(0.5f);
            
            GameObject annieNPC = GameObject.Find("NPC Annie Tutorial");
            if (annieNPC != null)
            {
                Debug.Log($"Found Annie on attempt {i+1}");
                annieNPC.SetActive(true);
                
                // Set Annie's script as enabled
                NPCscript annieScript = annieNPC.GetComponent<NPCscript>();
                if (annieScript != null)
                {
                    annieScript.enabled = true;
                    annieScript.ReloadDialogueFile();
                    Debug.Log("Annie's NPCscript has been enabled after delay");
                }
                
                // Notify Annie's fixer component
                AnnieRewardFixer annieFixer = annieNPC.GetComponent<AnnieRewardFixer>();
                if (annieFixer != null)
                {
                    annieFixer.OnMarkDialogueCompleted();
                }
                
                yield break; // Exit the coroutine once Annie is found
            }
            
            Debug.LogWarning($"Annie not found on attempt {i+1}, trying again...");
        }
        
        Debug.LogError("Failed to find Annie after multiple attempts!");
    }

    // Show reward popup after selecting choice
    private void ShowRewardPopup(int choiceIndex)
    {
        if (rewardPopup != null && choiceIndex >= 0 && choiceIndex < currentDialogue.choices.Length)
        {
            string actualNpcName = GetActualNPCName();
            
            var choice = currentDialogue.choices[choiceIndex];
            
            // Hide all dialogue-related panels first
            if (dialoguePanel != null)
                dialoguePanel.SetActive(false);
                
  
            
            // Show reward popup and update its content
            rewardPopup.SetActive(true);
            
            // Update reward UI
            if (rewardText != null)
                rewardText.text = choice.reward.message;
                
            if (rewardImage != null) {
                string rewardPath = $"Rewards/{choice.reward.sprite}";
                Debug.Log($"Loading reward image from: {rewardPath} for NPC: {actualNpcName}");
                Sprite rewardSprite = Resources.Load<Sprite>(rewardPath);
                if (rewardSprite != null) {
                    rewardImage.sprite = rewardSprite;
                    Debug.Log($"Loaded reward sprite: {rewardPath}");
                } else {
                    Debug.LogError($"Failed to load reward sprite: {rewardPath}");
                }
            }
            
            if (congratsText != null)
                congratsText.text = choice.response;
            
            // Setup close button
            if (closeRewardButton != null)
            {
                closeRewardButton.gameObject.SetActive(true);
                closeRewardButton.interactable = true;
                closeRewardButton.onClick.RemoveAllListeners();
                closeRewardButton.onClick.AddListener(() => OnRewardButtonClicked(choiceIndex));
            }
        }
        else
        {
            Debug.LogError($"Cannot show reward popup: choiceIndex={choiceIndex}, choices count={currentDialogue?.choices?.Length ?? 0}");
        }
    }

    // Callback for reward button click
    private async void OnRewardButtonClicked(int choiceIndex)
    {
        try {
            rewardPopup.SetActive(false);
            var choice = currentDialogue.choices[choiceIndex];
            var reward = choice.reward;
            bool isTutorial = dialogueFile.name.Contains("Tutorial");
            
            string actualNpcName = gameObject.name.Replace("NPC ", "").Replace(" Tutorial", "");
            
            // Optionally update progress again if needed
        }
        catch (Exception ex) {
            Debug.LogError($"Error in OnRewardButtonClicked: {ex.Message}");
        }
    }

    // Save reward progress to API
    private async Task SaveRewardToAPI(string npcName, int choiceIndex, string rewardSprite, string responseText)
    {
        try
        {
            // Get the actual reward sprite from the current choice
            string actualRewardSprite = "OneStar"; // Default fallback
            if (currentDialogue != null && 
                currentDialogue.choices != null && 
                choiceIndex < currentDialogue.choices.Length &&
                currentDialogue.choices[choiceIndex].reward != null)
            {
                // Get the specific reward sprite for this NPC from their dialogue file
                actualRewardSprite = currentDialogue.choices[choiceIndex].reward.sprite;
            }
            else
            {
                Debug.LogWarning($"Could not get reward from dialogue. Using default: {actualRewardSprite}");
            }
            
            playerUsername = GetCurrentUsername();
            
            if (string.IsNullOrEmpty(playerUsername))
            {
                throw new Exception("Username is empty or null");
            }
            
            using (var handler = CustomHttpHandler.GetInsecureHandler())
            using (var client = new HttpClient(handler))
            {
                client.DefaultRequestHeaders.Accept.Add(
                    new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
                
                // Create API request data for tutorial progress
                var progressData = new
                {
                    Username = playerUsername,
                    tutorial = new
                    {
                        status = $"{npcName} Completed",
                        reward = actualRewardSprite,
                        date = DateTime.UtcNow.ToString("o"),
                        checkpoints = new Dictionary<string, object>
                        {
                            [npcName] = new
                            {
                                reward = actualRewardSprite,
                                status = "Completed",
                                date = DateTime.UtcNow.ToString("o"),
                                message = responseText
                            }
                        }
                    }
                };
                
                // Serialize and send the data
                string jsonData = JsonConvert.SerializeObject(progressData);
                
                var content = new StringContent(jsonData, Encoding.UTF8, "application/json");
                
                var httpResponse = await client.PostAsync($"{baseUrl}/game_progress", content);
                
                if (!httpResponse.IsSuccessStatusCode)
                {
                    throw new Exception($"Server error: {httpResponse.StatusCode}");
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"SaveRewardToAPI failed: {ex.Message}");
            throw;
        }
    }

    // Fix for string.contains error - use Contains() method instead (case sensitive)
    // Fix a line around 288 replacing: dialogueFile.name.contains("Tutorial")
    // Replace with: dialogueFile.name.Contains("Tutorial")
}

// Add this class definition
public class NPCDialogueCoordinator : MonoBehaviour
{
    private static NPCDialogueCoordinator _instance;
    public static NPCDialogueCoordinator Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<NPCDialogueCoordinator>();
                if (_instance == null)
                {
                    GameObject go = new GameObject("NPCDialogueCoordinator");
                    _instance = go.AddComponent<NPCDialogueCoordinator>();
                    DontDestroyOnLoad(go);
                }
            }
            return _instance;
        }
    }

    private NPCscript activeNPC = null;
    public delegate void DialogueStartedDelegate(NPCscript activeNPC);
    public event DialogueStartedDelegate OnDialogueStarted;

    public bool RequestDialogueStart(NPCscript npc)
    {
        if (activeNPC != null && activeNPC != npc) return false;
        activeNPC = npc;
        OnDialogueStarted?.Invoke(npc);
        return true;
    }

    public void EndDialogue(NPCscript npc)
    {
        if (activeNPC == npc) activeNPC = null;
    }

    public bool IsActiveNPC(NPCscript npc)
    {
        return activeNPC == npc;
    }
}


// Add this class to support Annie's reward fix
public class AnnieRewardFixer : MonoBehaviour
{
    private NPCscript annieScript;

    void Awake()
    {
        annieScript = GetComponent<NPCscript>();
    }

    public void OnMarkDialogueCompleted()
    {
        if (annieScript != null)
        {
            annieScript.ReloadDialogueFile();
            annieScript.enabled = true;
            Debug.Log("AnnieRewardFixer: Reloaded Annie's dialogue");
        }
    }
}

// Add a new class for the tutorial instruction type json structure
[System.Serializable]
public class TutorialDialogueData : DialogueData
{
    public string instruction1;
    public string instruction2;
    public string lastDialogue;

    // Override the ClearData method to clear these additional fields
    public override void ClearData()
    {
        instruction1 = null;
        instruction2 = null;
        lastDialogue = null;
        base.ClearData();
    }
}

// Update DialogueData class to include nested choices
[System.Serializable]
public class DialogueData
{
    public string npcName;
    public string title;
    public string subtitle;
    public string setting;
    public string initialDialogue;
    public string initialNarration;
    public Choice[] choices;
    public string lessonLearned;
    public string sceneToLoad;
    
    // Add support for nested choices
    [System.Serializable]
    public class Choice
    {
        public string text;
        public string response;
        public Reward reward;
        public string sceneToLoad;
        // New field for nested choices
        public Choice[] nextChoices;
        // Special field for opening quest book
        public bool openQuestBook;
    }
    
    [System.Serializable]
    public class Reward
    {
        public string type;
        public string sprite;
        public string message;
    }
    
    public virtual void ClearData()
    {
        npcName = null;
        title = null;
        subtitle = null;
        setting = null;
        initialDialogue = null;
        initialNarration = null;
        choices = null;
        lessonLearned = null;
        sceneToLoad = null;
    }
}





