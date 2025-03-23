using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using VirtueBanwa.Dialogue;

public class NPCDialogueTrigger : MonoBehaviour
{
    [Header("NPC Info")]
    public string npcName;

    [Header("UI Elements")]
    public GameObject dialoguePanel;
    public TMP_Text dialogueText;
    public Transform choiceContainer;
    public Button[] choiceButtons;
    public TMP_Text[] choiceTexts;
    public Button continueButton;
    public Button backButton;

    [Header("Reward UI")]
    public GameObject rewardCanvas;
    public TMP_Text congratsText;
    public TMP_Text rewardText;
    public Button claimRewardButton;
    public Image rewardImage;

    [Header("Instruction Arrows")]
    public GameObject profileArrow;
    public GameObject menuArrow;

    [Header("Character Images")]
    public Image npcImage;
    public Image playerImage;
    public TMP_Text npcNameText;
    public TMP_Text playerNameText;
    public Sprite npcPortrait;
    public Sprite boyPortrait;
    public Sprite girlPortrait;

    [Header("Checkpoint Settings")]
    public bool useCheckpoint = false;
    public NPCDialogueTrigger nextNPC;
    private static NPCDialogueTrigger activeNPC;

    private GameManager gameManager;
    private NPCDialogue currentDialogue;
    private int currentDialogueStep = 0;
    private string[] dialogueSequence;
    private bool hasReceivedReward = false;
    private bool isShowingChoices = false;
    private bool isActive = false;

    // Add this static field at the top of the class
    private static NPCDialogueTrigger currentActiveNPC;

    void Start()
    {
        gameManager = FindObjectOfType<GameManager>();

        // Setup UI initial state
        if (dialoguePanel != null)
            dialoguePanel.SetActive(false);
        if (rewardCanvas != null)
            rewardCanvas.SetActive(false);

        // Setup name displays
        if (npcNameText != null)
            npcNameText.text = npcName;

        SetupPlayerPortrait(); // This already sets up player name

        // Setup reward claim button
        if (claimRewardButton != null)
            claimRewardButton.onClick.AddListener(OnClaimRewardClicked);

        // Set NPC name if empty (this should be before any dialogue initialization)
        if (string.IsNullOrEmpty(npcName))
        {
            npcName = gameObject.name;
            Debug.Log($"Using GameObject name as NPC name: {npcName}");
        }

        // Initially hide arrows
        if (profileArrow != null)
            profileArrow.SetActive(false);
        if (menuArrow != null)
            menuArrow.SetActive(false);

        CheckRewardStatus();

        // Set up player portrait based on character selection
        SetupPlayerPortrait();

        // Set NPC's portrait
        if (npcImage != null)
            npcImage.sprite = npcPortrait;

        // Setup navigation buttons
        SetupNavigationButtons();

        // Validate UI setup
        ValidateUISetup();

        // Check if this is the first NPC for this game mode and make it active
        if ((gameManager.currentGameMode == GameMode.Unit1PreTest && npcName == "Janica1") ||
            (gameManager.currentGameMode == GameMode.Tutorial && npcName == "Janica"))
        {
            activeNPC = this;
            Debug.Log($"[{npcName}] Set as active NPC for {gameManager.currentGameMode}");
        }
    }

    private void SetupNavigationButtons()
    {
        // No need to add listeners here anymore
        // The buttons will be handled by static methods
    }

    private void ValidateUISetup()
    {
        if (choiceButtons == null || choiceButtons.Length == 0)
        {
            Debug.LogError($"[{npcName}] No choice buttons assigned!");
        }

        if (choiceTexts == null || choiceTexts.Length == 0)
        {
            Debug.LogError($"[{npcName}] No choice texts assigned!");
        }

        if (choiceButtons != null && choiceTexts != null &&
            choiceButtons.Length != choiceTexts.Length)
        {
            Debug.LogError($"[{npcName}] Mismatch between number of choice buttons ({choiceButtons.Length}) and texts ({choiceTexts.Length})");
        }
    }

    async void CheckRewardStatus()
    {
        string username = PlayerPrefs.GetString("Username", "");
        if (!string.IsNullOrEmpty(username))
        {
            // Check if this NPC's dialogue is completed in the database
            bool isCompleted = await DialogueState.IsDialogueCompleted(username, npcName);
            hasReceivedReward = isCompleted;

            if (hasReceivedReward)
            {
                Debug.Log($"[{npcName}] Player has already received reward");
            }
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (IsPlayerCollider(other) && !hasReceivedReward)
        {
            Debug.Log($"[{npcName}] Player entered trigger area for {npcName}");
            
            // Fix for Pre-Test sequence - identify all NPCs in the sequence 
            bool inPreTestSequence = (gameManager != null && gameManager.currentGameMode == GameMode.Unit1PreTest);
            // Add Principal to the list of Pre-Test NPCs
            bool isPreTestNPC = (npcName == "Janica1" || npcName == "Mom" || npcName == "Principal");
            
            // Special debugging for Principal NPC
            if (npcName == "Principal") {
                Debug.Log($"[{npcName}] Principal NPC triggered. inPreTestSequence={inPreTestSequence}, gameMode={gameManager?.currentGameMode}");
            }
            
            // Always make Pre-Test NPCs active regardless of checkpoint settings
            if (inPreTestSequence && isPreTestNPC) 
            {
                Debug.Log($"[{npcName}] Pre-Test NPC activated");
                
                // Force-set this NPC as active for the sequence
                currentActiveNPC = this;
                isActive = true;
                
                // Reset dialogue and start it
                ResetDialogueState();
                StartDialogue();
                
                // Set up button listeners
                SetupSharedButtons();
            }
            // Original checkpoint logic for other NPCs
            else if (!useCheckpoint || this == activeNPC)
            {
                // Set this NPC as the current active one
                currentActiveNPC = this;
                isActive = true;
                
                // Reset any existing dialogue state
                ResetDialogueState();
                StartDialogue();
                
                // Set up the shared button listeners
                SetupSharedButtons();
            }
            else
            {
                Debug.Log($"[{npcName}] Cannot interact - NPC not active yet");
            }
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (IsPlayerCollider(other))
        {
            if (currentActiveNPC == this)
            {
                currentActiveNPC = null;
                // Remove the shared button listeners
                if (continueButton != null)
                    continueButton.onClick.RemoveAllListeners();
                if (backButton != null)
                    backButton.onClick.RemoveAllListeners();
            }
            isActive = false;
            ResetDialogueState();
        }
    }

    private void SetupSharedButtons()
    {
        if (continueButton != null)
        {
            continueButton.onClick.RemoveAllListeners();
            continueButton.onClick.AddListener(() => {
                // Only the current active NPC should respond
                if (currentActiveNPC == this)
                {
                    Debug.Log($"[{npcName}] Continue button clicked");
                    OnContinuePressed();
                }
            });
        }
        
        if (backButton != null)
        {
            backButton.onClick.RemoveAllListeners();
            backButton.onClick.AddListener(() => {
                // Only the current active NPC should respond
                if (currentActiveNPC == this)
                {
                    Debug.Log($"[{npcName}] Back button clicked");
                    OnBackPressed();
                }
            });
        }
    }

    private void ResetDialogueState()
    {
        // Reset ALL dialogue state
        currentDialogueStep = 0;
        currentDialogue = null;
        dialogueSequence = null;
        isShowingChoices = false;

        // Reset UI elements
        if (dialoguePanel != null)
            dialoguePanel.SetActive(false);
        if (rewardCanvas != null)
            rewardCanvas.SetActive(false);
        if (continueButton != null)
            continueButton.gameObject.SetActive(false);
        if (backButton != null)
            backButton.gameObject.SetActive(false);
        if (npcNameText != null)
            npcNameText.text = "";
        if (dialogueText != null)
            dialogueText.text = "";
    }

    void StartDialogue()
    {
        Debug.Log($"[{npcName}] Starting dialogue for NPC: {npcName}");

        if (gameManager != null && gameManager.dialogueManager != null)
        {
            currentDialogue = gameManager.dialogueManager.GetDialogueForNPC(npcName);
            if (currentDialogue != null)
            {
                // Set NPC name and portrait
                if (npcNameText != null)
                    npcNameText.text = npcName;
                if (npcImage != null && npcPortrait != null)
                    npcImage.sprite = npcPortrait;

                // Set the NPC name in the dialogue to match this NPC
                currentDialogue.npcName = npcName;

                Debug.Log($"[{npcName}] Loaded dialogue with initial narration: {currentDialogue.initialNarration}");
                ShowDialogue(currentDialogue);
            }
            else
            {
                Debug.LogError($"[{npcName}] No dialogue data found for NPC: {npcName}");
            }
        }
        else
        {
            Debug.LogError($"[{npcName}] GameManager or DialogueManager is null");
        }
    }

    void ShowDialogue(NPCDialogue dialogue)
    {
        if (dialoguePanel != null)
        {
            dialoguePanel.SetActive(true);
            currentDialogue = dialogue;
            currentDialogueStep = 0;

            // Build dialogue sequence array, filtering out empty strings
            var dialogueList = new List<string>();
            if (!string.IsNullOrEmpty(dialogue.initialNarration)) dialogueList.Add(dialogue.initialNarration);
            if (!string.IsNullOrEmpty(dialogue.initialDialogue)) dialogueList.Add(dialogue.initialDialogue);
            if (!string.IsNullOrEmpty(dialogue.instruction1)) dialogueList.Add(dialogue.instruction1);
            if (!string.IsNullOrEmpty(dialogue.instruction2)) dialogueList.Add(dialogue.instruction2);
            if (!string.IsNullOrEmpty(dialogue.lastDialogue)) dialogueList.Add(dialogue.lastDialogue);

            dialogueSequence = dialogueList.ToArray();
            Debug.Log($"[{npcName}] Built dialogue sequence with {dialogueSequence.Length} steps");

            // Show first dialogue
            ShowCurrentDialogueStep();
        }
    }

    void ShowCurrentDialogueStep()
    {
        // Add generic NPC debugging
        Debug.Log($"[{npcName}] Showing step {currentDialogueStep}");
        if (dialogueSequence != null)
        {
            Debug.Log($"[{npcName}] Total steps: {dialogueSequence.Length}");
            Debug.Log($"[{npcName}] Current text: {(currentDialogueStep < dialogueSequence.Length ? dialogueSequence[currentDialogueStep] : "end of sequence")}");
        }
        else
        {
            Debug.LogError($"[{npcName}] dialogueSequence is null!");
            return;
        }

        if (dialogueText != null && currentDialogueStep < dialogueSequence.Length)
        {
            string currentText = dialogueSequence[currentDialogueStep];
            Debug.Log($"[{npcName}] Showing dialogue step {currentDialogueStep}/{dialogueSequence.Length}: {currentText}");

            // Reset choices state
            isShowingChoices = false;

            // Hide UI elements but keep dialogue panel active
            bool wasDialoguePanelActive = dialoguePanel ? dialoguePanel.activeSelf : false;
            HideAllUIElements();
            if (dialoguePanel && wasDialoguePanelActive)
            {
                dialoguePanel.SetActive(true);

                // Make sure names are visible when dialogue is active
                if (npcNameText != null)
                    npcNameText.gameObject.SetActive(true);
                if (playerNameText != null)
                    playerNameText.gameObject.SetActive(true);
            }

            // Only show text if it's not empty
            if (!string.IsNullOrEmpty(currentText))
            {
                dialogueText.text = currentText;

                // Show continue button on every step, including the last one
                if (continueButton != null)
                {
                    continueButton.gameObject.SetActive(true);
                }

                // Show back button if we're not on the first step
                if (backButton != null)
                {
                    backButton.gameObject.SetActive(currentDialogueStep > 0);
                }

                // Show appropriate arrows
                if (currentDialogueStep == 2) // instruction1
                {
                    if (profileArrow != null)
                        profileArrow.SetActive(true);
                }
                else if (currentDialogueStep == 3) // instruction2
                {
                    if (menuArrow != null)
                        menuArrow.SetActive(true);
                }
            }
            else
            {
                OnContinuePressed();
            }
        }
        else if (!isShowingChoices)
        {
            // We've reached the end of dialogue, show choices
            ShowChoices(currentDialogue);
        }
    }

    void OnContinuePressed()
    {
        // Debug NPC state
        Debug.Log($"[{npcName}] Checking continue: Active={isActive}, Is current active NPC={currentActiveNPC == this}");

        // Fix to ensure Pre-Test NPCs always respond to continue button
        bool inPreTestSequence = (gameManager != null && gameManager.currentGameMode == GameMode.Unit1PreTest);
        bool isPreTestNPC = (npcName == "Janica1" || npcName == "Mom" || npcName == "Principal");
        
        // Skip the checkpoint check for Pre-Test NPCs
        if (inPreTestSequence && isPreTestNPC)
        {
            // Continue directly for Pre-Test NPCs without checkpoint check
            Debug.Log($"[{npcName}] Pre-Test NPC continuing dialogue");
        }
        // For other NPCs, only continue if active or not using checkpoints
        else if (!useCheckpoint || this == activeNPC)
        {
            if (!isActive)
            {
                Debug.LogWarning($"[{npcName}] Ignoring continue press - NPC not active");
                return;
            }
        }
        else
        {
            Debug.LogWarning($"[{npcName}] Cannot continue - NPC not active");
            return;
        }

        // Make sure we're using the correct NPC's dialogue state
        if (currentDialogue == null)
        {
            Debug.LogError($"[{npcName}] Cannot continue - dialogue is null");
            return;
        }

        if (dialogueSequence == null)
        {
            Debug.LogError($"[{npcName}] Cannot continue - dialogue sequence is null");
            return;
        }

        Debug.Log($"[{npcName}] Continue pressed. Step: {currentDialogueStep}, Max steps: {dialogueSequence.Length}");

        // Hide arrows
        if (profileArrow != null)
            profileArrow.SetActive(false);
        if (menuArrow != null)
            menuArrow.SetActive(false);

        currentDialogueStep++;

        // Check if this was the last step
        if (currentDialogueStep >= dialogueSequence.Length)
        {
            // Hide continue and back buttons
            if (continueButton != null)
                continueButton.gameObject.SetActive(false);
            if (backButton != null)
                backButton.gameObject.SetActive(false);

            // Show choices specific to this NPC
            if (currentDialogue != null)
            {
                ShowChoices(currentDialogue);
            }
            else
            {
                Debug.LogError($"[{npcName}] Current dialogue is null when trying to show choices");
            }
        }
        else
        {
            ShowCurrentDialogueStep();
        }
    }

    void OnBackPressed()
    {
        // Hide arrows before moving to previous step
        if (profileArrow != null)
            profileArrow.SetActive(false);
        if (menuArrow != null)
            menuArrow.SetActive(false);

        if (currentDialogueStep > 0)
        {
            currentDialogueStep--;
            ShowCurrentDialogueStep();
        }
    }

    void ShowChoices(NPCDialogue dialogue)
    {
        if (dialogue.npcName != npcName)
        {
            Debug.LogError($"[{npcName}] Trying to show choices for wrong NPC: {dialogue.npcName}");
            return;
        }

        Debug.Log($"[{npcName}] Attempting to show choices");

        if (dialogue != null)
        {
            Debug.Log($"[{npcName}] Has choices: {(dialogue.choices != null ? dialogue.choices.Count.ToString() : "no choices")}");
            if (dialogue.choices != null)
            {
                foreach (var choice in dialogue.choices)
                {
                    Debug.Log($"[{npcName}] Choice text: {choice.text}");
                }
            }
        }

        if (dialogue == null || dialogue.choices == null || dialogue.choices.Count == 0)
        {
            Debug.LogWarning($"[{npcName}] No choices available to show");
            return;
        }

        // Keep dialogue panel active
        if (dialoguePanel != null)
        {
            dialoguePanel.SetActive(true);
        }

        isShowingChoices = true;

        // Don't hide the dialogue panel when hiding UI elements
        bool wasDialoguePanelActive = dialoguePanel ? dialoguePanel.activeSelf : false;
        HideAllUIElements();
        if (dialoguePanel && wasDialoguePanelActive)
        {
            dialoguePanel.SetActive(true);
        }

        Debug.Log($"[{npcName}] Number of choices to show: {dialogue.choices.Count}");

        // Show choices
        for (int i = 0; i < dialogue.choices.Count && i < choiceButtons.Length; i++)
        {
            if (choiceButtons[i] != null)
            {
                choiceButtons[i].gameObject.SetActive(true);
                Debug.Log($"[{npcName}] Activated choice button {i}");

                if (choiceTexts[i] != null)
                {
                    choiceTexts[i].gameObject.SetActive(true);
                    choiceTexts[i].text = dialogue.choices[i].text;
                    Debug.Log($"[{npcName}] Set choice {i} text to: {dialogue.choices[i].text}");
                }
                else
                {
                    Debug.LogError($"[{npcName}] Choice text {i} is null!");
                }

                var choice = dialogue.choices[i];
                choiceButtons[i].onClick.RemoveAllListeners();
                choiceButtons[i].onClick.AddListener(() => OnChoiceSelected(choice));
            }
            else
            {
                Debug.LogError($"[{npcName}] Choice button {i} is null!");
            }
        }
    }

    void OnChoiceSelected(DialogueChoice choice)
    {
        dialogueText.text = choice.response;

        // Hide all choice buttons
        foreach (var button in choiceButtons)
        {
            button.gameObject.SetActive(false);
        }

        // Check if the choice has a "Test" reward type
        if (choice.reward != null && choice.reward.type == "Test")
        {
            // Find the NPCTestTrigger on this GameObject and start the test
            NPCTestTrigger testTrigger = GetComponent<NPCTestTrigger>();
            if (testTrigger != null)
            {
                Debug.Log($"[{npcName}] Starting test via NPCTestTrigger");
                testTrigger.StartTest();
            }
            else
            {
                Debug.LogError($"[{npcName}] No NPCTestTrigger found on this GameObject!");
            }
        }
        else if (choice.reward != null)
        {
            ShowReward(choice.reward);
        }
    }

    void ShowReward(DialogueReward reward)
    {
        if (rewardCanvas != null)
        {
            rewardCanvas.SetActive(true);
            if (congratsText != null)
                congratsText.text = "Congratulations!";
            if (rewardText != null)
                rewardText.text = reward.message;

            // Load and show reward image
            if (rewardImage != null)
            {
                string spritePath = $"Rewards/{reward.type}";
                Sprite rewardSprite = Resources.Load<Sprite>(spritePath);
                if (rewardSprite != null)
                {
                    rewardImage.sprite = rewardSprite;
                }
                else
                {
                    Debug.LogError($"Could not load reward sprite: {spritePath}");
                }
            }
        }
    }

    async void OnClaimRewardClicked()
    {
        if (rewardCanvas != null)
            rewardCanvas.SetActive(false);
        if (dialoguePanel != null)
            dialoguePanel.SetActive(false);

        // Tell GameManager to save progress
        if (gameManager != null && currentDialogue != null)
        {
            await gameManager.SaveProgress(npcName, currentDialogue);
            
            // Mark this dialogue as completed
            string username = PlayerPrefs.GetString("Username", "");
            if (!string.IsNullOrEmpty(username))
            {
                await DialogueState.SaveDialogueState(username, npcName, true);
                hasReceivedReward = true;
            }

            // If using checkpoint system, activate next NPC
            if (useCheckpoint && nextNPC != null)
            {
                activeNPC = nextNPC;
            }
        }
    }

    private bool IsPlayerCollider(Collider2D collider)
    {
        string character = PlayerPrefs.GetString("Character", "Boy");
        return (character == "Boy" && collider.gameObject.name.Contains("BoySprite")) ||
               (character == "Girl" && collider.gameObject.name.Contains("GirlSprite"));
    }

    private void SetupPlayerPortrait()
    {
        string character = PlayerPrefs.GetString("Character", "Boy");
        string firstName = PlayerPrefs.GetString("FirstName", "Player");
        
        if (playerImage != null)
        {
            playerImage.sprite = character == "Boy" ? boyPortrait : girlPortrait;
        }
        
        if (playerNameText != null)
        {
            playerNameText.text = firstName;
        }
    }

    private void HideAllUIElements()
    {
        if (dialoguePanel != null)
            dialoguePanel.SetActive(false);
        if (rewardCanvas != null)
            rewardCanvas.SetActive(false);
        foreach (var button in choiceButtons)
        {
            if (button != null)
                button.gameObject.SetActive(false);
        }
        foreach (var text in choiceTexts)
        {
            if (text != null)
                text.gameObject.SetActive(false);
        }
    }
}
