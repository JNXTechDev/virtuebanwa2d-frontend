using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Threading.Tasks;
using System.Net.Http;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using VirtueBanwa.Dialogue;  // Add namespace reference
using VirtueBanwa; // For GameMode enum
using UnityEngine.InputSystem; // Add this for new Input System
using System.Linq; // Add this for LINQ methods (All, Except)
using UnityEngine.SceneManagement; // Add this for SceneManager

public class GameManager : MonoBehaviour
{
    [Header("Game Configuration")]
    public GameMode currentGameMode = GameMode.Tutorial;
    
    [Header("UI References")]
    public GameObject dialoguePanel;
    public TMP_Text dialogueText;
    public TMP_Text playerNameText;
    public TMP_Text npcNameText;
    public Transform choiceContainer;
    public GameObject choiceButtonPrefab;
    public GameObject[] choiceButtons; // Array of choice button GameObjects
    public TMP_Text[] choiceTexts;    // Array of choice text components
    
    [Header("Reward UI")]
    public GameObject rewardCanvas;
    public TMP_Text congratsText;
    public TMP_Text rewardText;
    public Button claimRewardButton;
    public Image rewardImage; // Add this field
    
    [Header("Direction Indicator")]
    public GameObject directionIndicator;
    public Image arrowImage;
    
    [Header("Intro Dialogue")]
    public GameObject introDialoguePanel;
    public TMP_Text introDialogueText;
    public float typingSpeed = 0.05f;
    private bool isIntroCompleted = false;
    private bool isTyping = false;
    private string introMessage = "Hello there Student! Welcome! Ara ka subong sa Virtue Banwa! Ang ini nga hampang maga tudlo sa imo sang Good Morals and Right Conduct! Come on and lets play!";
    
    [Header("Navigation UI")]
    public Button continueButton;
    public Button backButton;

    // References
    public DialogueManager dialogueManager;
    private QuestManager questManager;
    
    // Player Data
    private string playerUsername;
    private string playerFullName;
    private string playerCharacter;
    
    // Base URL for API
    private string baseUrl => NetworkConfig.BaseUrl;

    void Awake()
    {
        // Initialize managers
        dialogueManager = GetComponent<DialogueManager>();
        if (dialogueManager == null)
        {
            dialogueManager = gameObject.AddComponent<DialogueManager>();
        }
        
        questManager = GetComponent<QuestManager>();
        if (questManager == null)
        {
            questManager = gameObject.AddComponent<QuestManager>();
        }
        
        // Load player data
        LoadPlayerData();
        
        // Setup UI
        SetupUI();
        
        // Initialize the appropriate game mode
        InitializeGameMode();
    }
    
    void Start()
    {
        // Freeze player movement
       // Time.timeScale = 0;
        
        // Show intro dialogue
        if (introDialoguePanel != null)
        {
            introDialoguePanel.SetActive(true);
            StartCoroutine(TypeIntroDialogue());
        }
        
        // Setup navigation buttons
        if (continueButton != null)
            continueButton.gameObject.SetActive(false);
        if (backButton != null)
            backButton.gameObject.SetActive(false);
    }

    private IEnumerator TypeIntroDialogue()
    {
        isTyping = true;
        introDialogueText.text = "";

        foreach (char letter in introMessage.ToCharArray())
        {
            introDialogueText.text += letter;
            yield return new WaitForSecondsRealtime(typingSpeed);
        }

        isTyping = false;
        // Wait for player click
    }

    void Update()
    {
        // Check for player input to dismiss intro dialogue
        if (!isIntroCompleted && !isTyping && 
            (Mouse.current.leftButton.wasPressedThisFrame || 
             Keyboard.current.spaceKey.wasPressedThisFrame))
        {
            CompleteIntroDialogue();
        }
    }

    private void CompleteIntroDialogue()
    {
        isIntroCompleted = true;
        introDialoguePanel.SetActive(false);
        // Unfreeze player movement
        Time.timeScale = 1;
    }
    
    private void LoadPlayerData()
    {
        playerUsername = PlayerPrefs.GetString("Username", "Player");
        string firstName = PlayerPrefs.GetString("FirstName", "Unknown");
        string lastName = PlayerPrefs.GetString("LastName", "Player");
        playerFullName = $"{firstName} {lastName}";
        playerCharacter = PlayerPrefs.GetString("Character", "Boy");
        
        Debug.Log($"Player data loaded - Username: {playerUsername}, Name: {playerFullName}, Character: {playerCharacter}");
    }
    
    private void SetupUI()
    {
        // Hide dialogue panel and reward canvas initially
        if (dialoguePanel != null)
            dialoguePanel.SetActive(false);
            
        if (rewardCanvas != null)
            rewardCanvas.SetActive(false);
            
        if (directionIndicator != null)
            directionIndicator.SetActive(false);
            
        // Setup claim reward button
        if (claimRewardButton != null)
            claimRewardButton.onClick.AddListener(OnClaimRewardClicked);
    }
    
private void InitializeGameMode()
{
    Debug.Log($"Initializing game mode: {currentGameMode}");
    
    // Clear any existing quests and dialogues
    dialogueManager.Reset();
    questManager.Reset();
    
    // Setup the appropriate content based on the current game mode
    switch (currentGameMode)
    {
        case GameMode.Tutorial:
            SetupTutorialMode();
            break;
        case GameMode.Unit1Lesson1:
            SetupUnit1Lesson1();
            break;
        case GameMode.Unit1PreTest:
            SetupPreTest("Unit1");
            break;
        // Add other modes as you implement them
        default:
            Debug.LogWarning($"Game mode {currentGameMode} not implemented yet.");
            break;
    }
}
    
    private void SetupTutorialMode()
    {
        Debug.Log("Setting up Tutorial mode");
        
        // Add tutorial NPCs and their dialogues
        dialogueManager.AddDialogueForNPC("Janica", CreateJanicaDialogue());
        dialogueManager.AddDialogueForNPC("Mark", CreateMarkDialogue());
        dialogueManager.AddDialogueForNPC("Annie", CreateAnnieDialogue());
        dialogueManager.AddDialogueForNPC("Rojan", CreateRojanDialogue());
        
        // Create tutorial quest sequence
        questManager.CreateQuestSequence(
            "Tutorial", 
            new string[] { "Janica", "Mark", "Annie", "Rojan" },
            OnTutorialComplete
        );
        
        // Start with the first quest target
        SetNextQuestTarget();
    }
    
    private void SetupUnit1Lesson1()
    {
        Debug.Log("Setting up Unit 1 Lesson 1");
        
        // Add your Unit 1 Lesson 1 NPCs and dialogues here
        // Example: dialogueManager.AddDialogueForNPC("Teacher", CreateTeacherDialogue());
        
        // Create Unit 1 Lesson 1 quest sequence
        // Example: questManager.CreateQuestSequence("Unit1Lesson1", new string[] { "Teacher", "Student1" }, OnUnit1Lesson1Complete);
        
        // Start with the first quest target
        SetNextQuestTarget();
    }

    private void SetupPreTest(string unitId)
    {
        Debug.Log($"Setting up Pre-Test for {unitId}");
        
        if (unitId == "Unit1")
        {
            // Set game mode
            currentGameMode = GameMode.Unit1PreTest;
            
            // Clear any existing dialogues
            dialogueManager.Reset();
            questManager.Reset();
            
            // Add NPC dialogues in sequence
            dialogueManager.AddDialogueForNPC("Janica1", PreTestOutsideDialogue());
            dialogueManager.AddDialogueForNPC("Mom", PreTestHouseDialogue());
            dialogueManager.AddDialogueForNPC("Principal", PreTestSchoolDialogue());
            
            // Create quest sequence
            questManager.CreateQuestSequence(
                "Unit1PreTest",
                new string[] { "Janica1", "Mom", "Principal" },
                OnUnit1PreTestComplete
            );
            
            // Start with first target
            SetNextQuestTarget();
        }
    }

    private NPCDialogue PreTestOutsideDialogue()
    {
        return new NPCDialogue
        {
            npcName = "Janica1",
            title = "Unit 1 Pre Test",
            subtitle = "Practice",
            initialDialogue = "Welcome! Are you ready to start your journey in learning good morals?",
            instruction1 = "To proceed with the Unit 1 Practice Test (Pre-Test), you need to go home first.",
            instruction2 = "Your mom is waiting for you at home.",
            lastDialogue = "Follow the green arrow to find your way home.",
            lessonLearned = "Head Home",
            choices = new List<DialogueChoice>
            {
                new DialogueChoice
                {
                    text = "Yes, I'll go home now.",
                    response = "Great! Your mom will help prepare you for the test.",
                    reward = new DialogueReward
                    {
                        type = "OneStar",
                        message = "Continue to the next step!",
                        sprite = "OneStar",
                        score = 5
                    }
                }
            }
        };
    }

    private NPCDialogue PreTestHouseDialogue()
    {
        return new NPCDialogue
        {
            npcName = "Mom",
            title = "Unit 1 Pre Test",
            subtitle = "Practice",
            initialDialogue = "Welcome home! Before you head to school, let me ask you something. When someone needs help, what should you do?",
            instruction1 = "You just arrived home. Mother Teresa is cooking your favorite meal.",
            instruction2 = "Talk to your mom for some guidance before the test.",
            lastDialogue = "After our talk, it's time to go to school!",
            lessonLearned = "Go To School",
            choices = new List<DialogueChoice>
            {
                new DialogueChoice
                {
                    text = "I will help them in any way I can.",
                    response = "That's the right attitude! Now go to school and do your best on the test.",
                    reward = new DialogueReward
                    {
                        type = "TwoStar",
                        message = "Helping others shows you care!",
                        sprite = "TwoStar",
                        score = 10
                    }
                },
                new DialogueChoice
                {
                    text = "I will ignore them",
                    response = "Think about how you would feel if you needed help. Now go to school and learn more about helping others.",
                    reward = new DialogueReward
                    {
                        type = "OneStar",
                        message = "There's always room to grow!",
                        sprite = "OneStar",
                        score = 5
                    }
                }
            }
        };
    }

    private NPCDialogue PreTestSchoolDialogue()
    {
        return new NPCDialogue
        {
            npcName = "Principal",
            title = "Unit 1 Pre Test",
            subtitle = "Practice",
            //narration = "You've arrived at Virtue Banwa Academy. The principal is waiting in the office.",
            initialDialogue = "Hi there! I am Principal V of Virtue Banwa Academy. Would you like to take the practice test?",
            instruction1 = "The test will help us understand what you already know about good morals.",
            instruction2 = "Are you ready to begin?",
            lastDialogue = "",
            lessonLearned = "First Step is taking a Pre Test!",
            choices = new List<DialogueChoice>
            {
                new DialogueChoice
                {
                    text = "Yes, I will take the test!",
                    response = "Excellent! Let's begin. Don't worry, this is just to see what you already know.",
                    reward = new DialogueReward
                    {
                        type = "Test",
                        message = "Begin Pre-Test",
                        sprite = "Test",
                        score = 0
                    }
                },
                new DialogueChoice
                {
                    text = "I need more time to prepare.",
                    response = "That's okay. Come back when you're ready.",
                    reward = null
                }
            }
        };
    }

    private void OnUnit1PreTestComplete()
    {
        Debug.Log("Unit 1 Pre-Test sequence completed!");
        // Any additional logic after all dialogues are done
    }
    
    public void SetNextQuestTarget()
    {
        string nextNpcName = questManager.GetNextQuestTarget();
        
        if (nextNpcName != null)
        {
            // Get the NPC transform to point the arrow at
            Transform targetNpc = GameObject.Find(nextNpcName)?.transform;
            
            if (targetNpc != null)
            {
                // Show the direction indicator pointing to the next NPC
                ShowDirectionIndicator(targetNpc);
            }
            else
            {
                Debug.LogWarning($"Could not find NPC named {nextNpcName} in the scene.");
                directionIndicator.SetActive(false);
            }
        }
        else
        {
            // No more quest targets, hide the indicator
            directionIndicator.SetActive(false);
        }
    }
    
    private void ShowDirectionIndicator(Transform target)
    {
        if (directionIndicator != null)
        {
            directionIndicator.SetActive(true);
            StartCoroutine(UpdateDirectionIndicator(target));
        }
    }
    
    private IEnumerator UpdateDirectionIndicator(Transform target)
    {
        // Get player transform
        Transform player = FindActivePlayerTransform();
        
        while (directionIndicator.activeSelf && player != null && target != null)
        {
            // Calculate direction from player to target
            Vector3 direction = target.position - player.position;
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            
            // Update arrow rotation
            directionIndicator.transform.rotation = Quaternion.Euler(0, 0, angle);
            
            // Position the indicator above the player
            directionIndicator.transform.position = player.position + Vector3.up * 1.5f;
            
            // Check if player is close enough to target
            if (Vector2.Distance(player.position, target.position) < 2f)
            {
                // Hide indicator when close enough
                directionIndicator.SetActive(false);
                yield break;
            }
            
            yield return null;
        }
    }
    
    private Transform FindActivePlayerTransform()
    {
        // Find the active player character based on the player's character selection
        GameObject playerObject = null;
        
        if (playerCharacter == "Boy")
        {
            playerObject = GameObject.Find("BoyPlayer");
        }
        else if (playerCharacter == "Girl")
        {
            playerObject = GameObject.Find("GirlPlayer");
        }
        
        return playerObject?.transform;
    }
    
    // Called when an NPC is interacted with
    public void InteractWithNPC(string npcName)
    {
        Debug.Log($"GameManager: InteractWithNPC called with {npcName}");
        
        if (dialoguePanel == null)
        {
            Debug.LogError("DialoguePanel reference is missing in GameManager!");
            return;
        }
        
        if (dialogueManager == null)
        {
            Debug.LogError("DialogueManager reference is missing in GameManager!");
            return;
        }
        
        NPCDialogue dialogue = dialogueManager.GetDialogueForNPC(npcName);
        if (dialogue != null)
        {
            Debug.Log($"Found dialogue for {npcName}, showing dialogue panel");
            ShowDialogue(dialogue);
        }
        else
        {
            Debug.LogError($"No dialogue found for NPC: {npcName}");
        }
    }
    
    private void ShowGenericDialogue(string npcName)
    {
        if (dialoguePanel != null)
        {
            dialoguePanel.SetActive(true);
            
            if (npcNameText != null)
                npcNameText.text = npcName;
                
            if (dialogueText != null)
                dialogueText.text = "I don't have anything important to tell you right now.";
                
            if (playerNameText != null)
                playerNameText.text = playerFullName;
                
            // Clear any existing choice buttons
            foreach (Transform child in choiceContainer)
            {
                Destroy(child.gameObject);
            }
            
            // Add a simple "Goodbye" button
            GameObject choiceObj = Instantiate(choiceButtonPrefab, choiceContainer);
            Button choiceButton = choiceObj.GetComponent<Button>();
            TMP_Text choiceText = choiceObj.GetComponentInChildren<TMP_Text>();
            
            if (choiceText != null)
                choiceText.text = "Goodbye";
                
            if (choiceButton != null)
                choiceButton.onClick.AddListener(() => CloseDialogue());
        }
    }
    
    private void ShowDialogue(NPCDialogue dialogue)
    {
        if (dialoguePanel != null)
        {
            dialoguePanel.SetActive(true);
            
            StartCoroutine(ShowDialogueSequence(dialogue));
        }
    }

    private IEnumerator ShowDialogueSequence(NPCDialogue dialogue)
    {
        // Show initial narration
        if (!string.IsNullOrEmpty(dialogue.initialNarration))
        {
            dialogueText.text = dialogue.initialNarration;
            yield return new WaitForSeconds(3f);
        }
        
        // Show initial dialogue
        dialogueText.text = dialogue.initialDialogue;
        yield return new WaitForSeconds(3f);
        
        // Show instruction 1 with arrow pointing to profile button
        if (!string.IsNullOrEmpty(dialogue.instruction1))
        {
            dialogueText.text = dialogue.instruction1;
            // TODO: Show arrow pointing to profile button
            yield return new WaitForSeconds(3f);
        }
        
        // Show instruction 2 with arrow pointing to menu
        if (!string.IsNullOrEmpty(dialogue.instruction2))
        {
            dialogueText.text = dialogue.instruction2;
            // TODO: Show arrow pointing to menu button
            yield return new WaitForSeconds(3f);
        }
        
        // Show last dialogue
        if (!string.IsNullOrEmpty(dialogue.lastDialogue))
        {
            dialogueText.text = dialogue.lastDialogue;
            yield return new WaitForSeconds(3f);
        }
        
        // Finally show choices
        ShowChoices(dialogue);
    }

    private void ShowChoices(NPCDialogue dialogue)
    {
        // Hide all choice buttons first
        for (int i = 0; i < choiceButtons.Length; i++)
        {
            choiceButtons[i].SetActive(false);
        }

        // Show only the buttons we need
        for (int i = 0; i < dialogue.choices.Count; i++)
        {
            if (i < choiceButtons.Length)
            {
                choiceButtons[i].SetActive(true);
                choiceTexts[i].text = dialogue.choices[i].text;
                
                // Get the choice for use in the listener
                DialogueChoice choice = dialogue.choices[i];
                
                // Remove existing listeners and add new one
                Button button = choiceButtons[i].GetComponent<Button>();
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() => OnChoiceSelected(choice, dialogue));
            }
        }
    }
    
    private void OnChoiceSelected(DialogueChoice choice, NPCDialogue dialogue)
    {
        // Update dialogue text to show NPC response
        if (dialogueText != null)
            dialogueText.text = choice.response;
            
        // Clear choices
        foreach (Transform child in choiceContainer)
        {
            Destroy(child.gameObject);
        }
        
        // Add continue button
        GameObject continueObj = Instantiate(choiceButtonPrefab, choiceContainer);
        Button continueButton = continueObj.GetComponent<Button>();
        TMP_Text continueText = continueObj.GetComponentInChildren<TMP_Text>();
        
        if (continueText != null)
            continueText.text = "Continue";
            
        if (continueButton != null)
            continueButton.onClick.AddListener(() => {
                // Close dialogue panel and show reward if applicable
                CloseDialogue();
                
                if (choice.reward != null)
                {
                    ShowReward(dialogue.npcName, choice.reward);
                }
                else
                {
                    // Move to next quest target if no reward
                    SetNextQuestTarget();
                }
            });
    }
    
    private void CloseDialogue()
    {
        if (dialoguePanel != null)
            dialoguePanel.SetActive(false);
    }
    
    private void ShowReward(string npcName, DialogueReward reward)
    {
        if (rewardCanvas != null)
        {
            rewardCanvas.SetActive(true);
            
            if (congratsText != null)
                congratsText.text = "Congratulations!";
                
            if (rewardText != null)
                rewardText.text = reward.message;

            // Load and display reward sprite
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
                
            // Store reward info to be saved when claimed
            questManager.SetPendingReward(npcName, reward);
        }
    }
    
    private async void OnClaimRewardClicked()
    {
        // Hide reward panel
        if (rewardCanvas != null)
            rewardCanvas.SetActive(false);
            
        // Get the pending reward from quest manager
        var pendingReward = questManager.GetPendingReward();
        if (pendingReward == null)
        {
            Debug.LogError("No pending reward found when claim button was clicked");
            SetNextQuestTarget();
            return;
        }
        
        // Save progress to database
        await SaveProgressToDatabase(pendingReward.Item1, pendingReward.Item2);
        
        // Clear pending reward
        questManager.ClearPendingReward();
        
        // Move to next target
        SetNextQuestTarget();
    }
    
    private async Task SaveProgressToDatabase(string npcName, DialogueReward reward)
    {
        try
        {
            // Prepare data for API
            string gameMode = currentGameMode.ToString();
            string unit = "";
            string lesson = "";
            
            if (gameMode.Contains("Unit"))
            {
                string[] parts = gameMode.Split(new char[] { 'U', 'n', 'i', 't', 'L', 'e', 's', 'o' }, 
                    StringSplitOptions.RemoveEmptyEntries);
                
                if (parts.Length >= 2)
                {
                    unit = $"Unit{parts[0].Trim()}";
                    lesson = $"Lesson{parts[1].Trim()}";
                }
                else if (gameMode.Contains("PreTest"))
                {
                    unit = $"Unit{parts[0].Trim()}";
                    lesson = "PreTest";
                }
                else if (gameMode.Contains("PostTest"))
                {
                    unit = $"Unit{parts[0].Trim()}";
                    lesson = "PostTest";
                }
            }
            
            JObject data = new JObject();
            
            if (gameMode == "Tutorial")
            {
                // Tutorial progress
                data["Username"] = playerUsername;
                data["tutorial"] = new JObject
                {
                    ["status"] = "In Progress",
                    ["date"] = DateTime.Now,
                    ["checkpoints"] = new JObject
                    {
                        [npcName] = new JObject
                        {
                            ["status"] = "Completed",
                            ["reward"] = reward.type,
                            ["date"] = DateTime.Now,
                            ["message"] = reward.message
                        }
                    }
                };
            }
            else
            {
                // Unit lesson progress - renamed to match GameProgressData
                data["Username"] = playerUsername;
                data["units"] = new JObject
                {
                    [unit] = new JObject
                    {
                        ["lessons"] = new JObject
                        {
                            [lesson] = new JObject
                            {
                                ["npcsTalkedTo"] = new JArray(npcName),
                                ["rewards"] = new JObject
                                {
                                    [npcName] = new JObject
                                    {
                                        ["type"] = reward.type,
                                        ["message"] = reward.message,
                                        ["score"] = reward.score
                                    }
                                }
                            }
                        }
                    }
                };
            }
            
            string json = data.ToString();
            Debug.Log($"Saving progress data: {json}");
            
            using (HttpClient client = new HttpClient())
            {
                HttpContent content = new StringContent(json, Encoding.UTF8, "application/json");
                HttpResponseMessage response = await client.PostAsync($"{baseUrl}/game_progress", content);
                
                if (response.IsSuccessStatusCode)
                {
                    string responseContent = await response.Content.ReadAsStringAsync();
                    Debug.Log($"Progress saved successfully: {responseContent}");
                    return;
                }
                else
                {
                    Debug.LogError($"Error saving progress: {response.StatusCode}");
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"Exception when saving progress: {ex.Message}");
        }
    }
    
    // Callback for when tutorial is complete
    private void OnTutorialComplete()
    {
        Debug.Log("Tutorial completed!");
        
        // Update tutorial status to completed
        SaveTutorialCompletionStatus();
    }
    
    private async void SaveTutorialCompletionStatus()
    {
        try
        {
            // Check if all NPCs have been completed first
            bool allCompleted = await CheckAllNPCsCompleted();
            if (!allCompleted)
            {
                Debug.LogWarning("Not all NPCs have been completed, setting tutorial status to In Progress");
                return;
            }

            JObject data = new JObject
            {
                ["Username"] = playerUsername,
                ["tutorial"] = new JObject
                {
                    ["status"] = "Completed", // Changed from "In Progress" to "Completed"
                    ["date"] = DateTime.Now
                }
            };
            
            string json = data.ToString();
            Debug.Log($"Saving tutorial completion status: {json}");
            
            using (HttpClient client = new HttpClient())
            {
                HttpContent content = new StringContent(json, Encoding.UTF8, "application/json");
                HttpResponseMessage response = await client.PostAsync($"{baseUrl}/game_progress", content);
                
                if (response.IsSuccessStatusCode)
                {
                    string responseContent = await response.Content.ReadAsStringAsync();
                    Debug.Log($"Tutorial completion status saved successfully: {responseContent}");
                    return;
                }
                else
                {
                    Debug.LogError($"Error saving tutorial completion status: {response.StatusCode}");
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"Exception when saving tutorial completion status: {ex.Message}");
        }
    }
    
    private async Task<bool> CheckAllNPCsCompleted()
    {
        try
        {
            string[] requiredNPCs = { "Janica", "Mark", "Annie", "Rojan" };
            
            // Check each NPC's completion status
            foreach (string npc in requiredNPCs)
            {
                bool isCompleted = await DialogueState.IsDialogueCompleted(playerUsername, npc);
                if (!isCompleted)
                {
                    Debug.Log($"NPC {npc} has not been completed yet");
                    return false; // If any NPC is not completed, return false
                }
            }
            
            // If we get here, all NPCs are completed
            Debug.Log("All NPCs have been completed, setting tutorial status to Completed");
            return true;
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error checking NPC completion: {ex.Message}");
            return false; // Return false on error
        }
    }
    
    // Create tutorial dialogues
    private NPCDialogue CreateJanicaDialogue()
    {
        NPCDialogue dialogue = new NPCDialogue
        {
            npcName = "Janica",
            title = "Game Introduction",
            subtitle = "Tutorial",
            initialNarration = "You just moved to a new City called Virtue Banwa. You see a girl standing near the bridge, follow her to know more about the city.",
            initialDialogue = "Hi there! I'm Janica! The Creator of Virtue Banwa. I'm here to help you get started.",
            instruction1 = "The Profile button is located at the top left corner of the screen. Click it to view your profile.",
            instruction2 = "The Menu is located at the top right corner of the screen. Click it to view your quests, mail, achievements and settings,",
            lastDialogue = "Awesome! Kabalo ka na sang basics. Pwede ka na mag umpisa hampang, Good luck!",
            lessonLearned = "Basics Completed!",
            choices = new List<DialogueChoice>
            {
                new DialogueChoice
                {
                    text = "Thank you for the introduction!",
                    response = "You're welcome! I'm glad I could help. Now go explore the city and meet the other residents!",
                    reward = new DialogueReward
                    {
                        type = "OneStar",
                        message = "Tutorial Step Completed!",
                        sprite = "OneStar",
                        score = 10
                    }
                },
                new DialogueChoice
                {
                    text = "I'll explore the city now.",
                    response = "Excellent! Have fun exploring Virtue Banwa. Talk to other NPCs to learn more about the city.",
                    reward = new DialogueReward
                    {
                        type = "OneStar",
                        message = "Tutorial Step Completed!",
                        sprite = "OneStar",
                        score = 10
                    }
                }
            }
        };
        
        return dialogue;
    }
    
    private NPCDialogue CreateMarkDialogue()
    {
        NPCDialogue dialogue = new NPCDialogue
        {
            npcName = "Mark",
            title = "Outside Introduction",
            subtitle = "Tutorial",
            initialNarration = "You see Mark waiting near the entrance.",
            initialDialogue = "Hi there! Ako gale si Mark, Student sang Virtue Banwa Academy, May Classmate ako nga si Annie,She is very friendly.",
            instruction1 = "", // Empty but needed
            instruction2 = "", // Empty but needed
            lastDialogue = "Para ma meet mo siya, Follow the Green Arrow!",
            lessonLearned = "Let's find Annie near the flowers!",
            choices = new List<DialogueChoice>
            {
                new DialogueChoice
                {
                    text = "Thanks Mark! I'll go find Annie.",
                    response = "Great! She's just over there by the flowers. You can't miss her!",
                    reward = new DialogueReward
                    {
                        type = "OneStar",
                        message = "Tutorial: Virtue Banwa Introduction",
                        sprite = "OneStar",
                        score = 10
                    }
                }
            }
        };
        
        return dialogue;
    }

    private NPCDialogue CreateAnnieDialogue()
    {
        NPCDialogue dialogue = new NPCDialogue
        {
            npcName = "Annie",
            title = "School Introduction",
            subtitle = "Tutorial",
            initialNarration = "You see Annie standing near the flowers, just as Mark mentioned.",
            initialDialogue = "Hi, ako si Annie! Gusto ko lang ma hambal nga Good Job!",
            instruction1 = "", // Empty but needed
            instruction2 = "", // Empty but needed
            lastDialogue = "You have followed the Green Arrow to find me! Now, Virtue Banwa Academy entrance is right over there! Let's go!",
            lessonLearned = "Now head to the school entrance to begin your journey!",
            choices = new List<DialogueChoice>
            {
                new DialogueChoice
                {
                    text = "Thanks Annie! I'll head to the entrance now.",
                    response = "Great! Just walk up to the entrance and you'll be transported inside. Good luck!",
                    reward = new DialogueReward
                    {
                        type = "TwoStar",
                        message = "Tutorial: Meeting Annie",
                        sprite = "TwoStar",
                        score = 20
                    }
                }
            }
        };
        
        return dialogue;
    }
    
    private NPCDialogue CreateRojanDialogue()
    {
        NPCDialogue dialogue = new NPCDialogue
        {
            npcName = "Rojan",
            title = "School Introduction",
            subtitle = "Tutorial",
            initialNarration = "You've made it to the school entrance and met with Rojan.",
            initialDialogue = "Welcome ara ka subong sa Virtue Banwa Academy! Ako si Rojan and this is a place where you can learn and grow. Then finally! na tapos mo na ang tutorial!",
            lessonLearned = "Now open your quest book to begin your journey!",
            choices = new List<DialogueChoice>
            {
                new DialogueChoice
                {
                    text = "Wow, Virtue Banwa Academy looks well.",
                    response = "You have completed the tutorial. You are now ready to start your journey. Good luck!",
                    reward = new DialogueReward
                    {
                        type = "ThreeStar",
                        message = "Tutorial: Completed",
                        sprite = "ThreeStar",
                        score = 30
                    }
                }
            }
        };
        
        return dialogue;
    }

    public async Task SaveProgress(string npcName, NPCDialogue dialogue)
    {
        if (dialogue?.choices == null || dialogue.choices.Count == 0)
            return;

        // Get the first choice's reward (since that's what we're using)
        DialogueReward reward = dialogue.choices[0].reward;
        await SaveProgressToDatabase(npcName, reward);
    }

    public void UpdateNavigationButtons(bool canGoBack, bool canContinue)
    {
        if (continueButton != null)
        {
            continueButton.gameObject.SetActive(canContinue);
            continueButton.interactable = canContinue;
        }
        
        if (backButton != null)
        {
            backButton.gameObject.SetActive(canGoBack);
            backButton.interactable = canGoBack;
        }
    }

    public async Task<bool> CanEnterSchool()
    {
        string[] requiredNPCs = { "Janica", "Mark", "Annie" };
        
        // Get the list of completed NPCs
        List<string> completedNPCs = new List<string>();
        foreach (string npcName in requiredNPCs)
        {
            bool isCompleted = await DialogueState.IsDialogueCompleted(playerUsername, npcName);
            if (isCompleted)
                completedNPCs.Add(npcName);
        }

        // Check if all required NPCs are completed
        bool allCompleted = requiredNPCs.All(npc => completedNPCs.Contains(npc));
        
        if (!allCompleted)
        {
            var missingNPCs = requiredNPCs.Except(completedNPCs);
            Debug.Log($"Cannot enter school yet. Need to talk to: {string.Join(", ", missingNPCs)}");
            
            // Update quest target to point to next incomplete NPC
            string nextNPC = missingNPCs.FirstOrDefault();
            if (!string.IsNullOrEmpty(nextNPC))
            {
                questManager.SetCurrentTarget(nextNPC); // Changed from SetNextTarget to SetCurrentTarget
                SetNextQuestTarget();
            }
        }
        
        return allCompleted;
    }

    // Called by the school entrance trigger
    public async void TryEnterSchool(GameObject entranceTrigger)
    {
        if (await CanEnterSchool())
        {
            Debug.Log("All NPCs completed, entering school...");
            SceneManager.LoadScene("Tutorial School");
        }
        else
        {
            Debug.Log("Cannot enter school yet - not all NPCs have been talked to");
        }
    }

    public async void SavePreTestProgress(string unitId, int score, bool passed)
    {
        try
        {
            JObject data = new JObject
            {
                ["Username"] = playerUsername,
                ["units"] = new JObject
                {
                    [unitId] = new JObject
                    {
                        ["preTest"] = new JObject
                        {
                            ["status"] = "Completed",
                            ["score"] = score,
                            ["passed"] = passed,
                            ["date"] = DateTime.Now
                        }
                    }
                }
            };

            string json = data.ToString();
            Debug.Log($"Saving pre-test progress: {json}");

            using (HttpClient client = new HttpClient())
            {
                HttpContent content = new StringContent(json, Encoding.UTF8, "application/json");
                HttpResponseMessage response = await client.PostAsync($"{baseUrl}/game_progress", content);

                if (response.IsSuccessStatusCode)
                {
                    string responseContent = await response.Content.ReadAsStringAsync();
                    Debug.Log($"Pre-test progress saved successfully: {responseContent}");
                }
                else
                {
                    Debug.LogError($"Error saving pre-test progress: {response.StatusCode}");
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"Exception when saving pre-test progress: {ex.Message}");
        }
    }
}
