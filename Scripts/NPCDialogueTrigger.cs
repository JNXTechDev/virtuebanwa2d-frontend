using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using VirtueBanwa.Dialogue;  // Add this namespace reference


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

    [Header("Reward UI")]
    public GameObject rewardCanvas;
    public TMP_Text congratsText;
    public TMP_Text rewardText;
    public Button claimRewardButton;
    public Image rewardImage; // Add this field

    [Header("Instruction Arrows")]
    public GameObject profileArrow;    // Arrow pointing to profile button
    public GameObject menuArrow;       // Arrow pointing to menu button

    [Header("Character Images")]
    public Image npcImage;         // NPC portrait
    public Image playerImage;      // Player portrait
    public TMP_Text npcNameText;   // NPC name text
    public TMP_Text playerNameText; // Player name text
    public Sprite npcPortrait;     // NPC's own portrait
    public Sprite boyPortrait;     // Boy player portrait
    public Sprite girlPortrait;    // Girl player portrait

    private GameManager gameManager;
    private NPCDialogue currentDialogue;
    private int currentDialogueStep = 0;
    private string[] dialogueSequence;
    private bool hasReceivedReward = false;

    void Start()
    {
        gameManager = FindObjectOfType<GameManager>();
        
        // Setup UI initial state
        if (dialoguePanel != null)
            dialoguePanel.SetActive(false);
        if (rewardCanvas != null)
            rewardCanvas.SetActive(false);
        
        // Setup reward claim button
        if (claimRewardButton != null)
            claimRewardButton.onClick.AddListener(OnClaimRewardClicked);

        if (string.IsNullOrEmpty(npcName))
            npcName = gameObject.name;

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

        // Setup navigation button listeners
        if (continueButton != null)
        {
            continueButton.onClick.RemoveAllListeners(); // Clear any existing listeners
            continueButton.onClick.AddListener(() => OnContinuePressed());
        }
        
        if (backButton != null)
        {
            backButton.onClick.RemoveAllListeners(); // Clear any existing listeners
            backButton.onClick.AddListener(() => OnBackPressed());
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
            StartDialogue();
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (IsPlayerCollider(other))
        {
            if (dialoguePanel != null)
                dialoguePanel.SetActive(false);
            if (rewardCanvas != null)
                rewardCanvas.SetActive(false);
        }
    }

    void StartDialogue()
    {
        if (gameManager != null && gameManager.dialogueManager != null)
        {
            currentDialogue = gameManager.dialogueManager.GetDialogueForNPC(npcName);
            if (currentDialogue != null)
            {
                ShowDialogue(currentDialogue);
            }
        }
    }

    void ShowDialogue(NPCDialogue dialogue)
    {
        if (dialoguePanel != null)
        {
            dialoguePanel.SetActive(true);
            currentDialogue = dialogue;
            currentDialogueStep = 0;

            // Build dialogue sequence array
            dialogueSequence = new string[] {
                dialogue.initialNarration,
                dialogue.initialDialogue,
                dialogue.instruction1,
                dialogue.instruction2,
                dialogue.lastDialogue
            };

            // Show first dialogue
            ShowCurrentDialogueStep();
        }
    }

    void ShowCurrentDialogueStep()
    {
        if (dialogueText != null && currentDialogueStep < dialogueSequence.Length)
        {
            string currentText = dialogueSequence[currentDialogueStep];
            
            // Hide any visible choice buttons first
            foreach (var button in choiceButtons)
            {
                if (button != null)
                    button.gameObject.SetActive(false);
            }

            // Hide both arrows first
            if (profileArrow != null)
                profileArrow.SetActive(false);
            if (menuArrow != null)
                menuArrow.SetActive(false);
            
            // Only show text if it's not empty
            if (!string.IsNullOrEmpty(currentText))
            {
                dialogueText.text = currentText;

                // Show continue/back buttons
                if (continueButton != null)
                    continueButton.gameObject.SetActive(true);
                if (backButton != null)
                    backButton.gameObject.SetActive(currentDialogueStep > 0);

                // Determine who is speaking based on the dialogue step

                // Show appropriate arrow based on the dialogue step
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

                // Let GameManager handle the navigation buttons
                if (gameManager != null)
                {
                    gameManager.UpdateNavigationButtons(
                        currentDialogueStep > 0,  // Can go back
                        currentDialogueStep < dialogueSequence.Length - 1  // Can go forward
                    );
                }
            }
            else
            {
                // Skip empty dialogue steps
                OnContinuePressed();
            }
        }
    }

    void OnContinuePressed()
    {
        // Hide arrows before moving to next step
        if (profileArrow != null)
            profileArrow.SetActive(false);
        if (menuArrow != null)
            menuArrow.SetActive(false);

        currentDialogueStep++;
        
        // Check if we've reached the end of the dialogue sequence
        if (currentDialogueStep >= dialogueSequence.Length)
        {
            // Show choices at the end of dialogue
            ShowChoices(currentDialogue);
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
        // Hide navigation buttons when showing choices

        // Hide all choice buttons first
        for (int i = 0; i < choiceButtons.Length; i++)
        {
            choiceButtons[i].gameObject.SetActive(false);
        }

        // Show only the buttons we need
        for (int i = 0; i < dialogue.choices.Count; i++)
        {
            if (i < choiceButtons.Length)
            {
                choiceButtons[i].gameObject.SetActive(true);
                choiceTexts[i].text = dialogue.choices[i].text;

                var choice = dialogue.choices[i];
                choiceButtons[i].onClick.RemoveAllListeners();
                choiceButtons[i].onClick.AddListener(() => OnChoiceSelected(choice));
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

        if (choice.reward != null)
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
}
