using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using UnityEngine.InputSystem; // Add this for the new Input System

public class NPCscript : MonoBehaviour
{
    public GameObject dialoguePanel;
    public Text dialogueText;
    public string[] dialogue;
    public string[] narration;
    private int index;
    private int narrationIndex;

    public GameObject contButton;
    public GameObject choiceButton1;
    public GameObject choiceButton2;
    public GameObject choiceButton3;
    public Text[] choiceTexts;

    [System.Serializable]
    public class ReplySet
    {
        public string[] replies;
    }

    [SerializeField]
    public ReplySet[] npcReplies;

    public float wordSpeed;
    public bool playerisClose;
    private bool isReplying;
    private bool isNarrating;
    private Coroutine typingCoroutine;

    public GameObject currentSpeakerImage;
    public Sprite playerSprite;
    public Sprite npcSprite;
    public Sprite npcPovSprite;

    public bool firstLineIsPlayerSprite;

    public string currentUnit = "UNIT 1";
    public string currentLesson = "Lesson 1";

    private string playerUsername;

    public string nextSceneName;
    public GameObject playerUsernameObject;
    public SceneTransition sceneTransition;
    private bool isDialogueComplete = false;

    // New variables for player movement control
    private GameObject player;
    private PlayerMovement playerMovement; // Reference to the player's movement script

    public GameObject rewardPopup;
    public TextMeshProUGUI rewardText;
    public TextMeshProUGUI congratsText; // New field for congratulatory text
    public Image rewardImage;
    public Image rewardBackground;
    public bool useRewardSystem = true;

    [System.Serializable]
    public class RewardChoice
    {
        public string choiceName;
        public string congratsMessage; // Changed from xpValue to congratsMessage
        public Sprite rewardSprite;
        public string rewardText; // New field for reward text
    }

    public List<RewardChoice> rewardChoices = new List<RewardChoice>();

    public float rewardDisplayDuration = 2f; // New variable to set the duration in the Inspector

    public GameObject backButton;
    private List<string> npcDialogueHistory = new List<string>();
    private List<string> npcReplyHistory = new List<string>();
    private List<string> narrationHistory = new List<string>();
    private List<Sprite> spriteHistory = new List<Sprite>();

    // Add a flag to track which type of dialogue is active
    private enum DialogueType { NPC, Reply, Narration, Choice }
    private DialogueType currentDialogueType;

    // Base URL for your API
    private const string baseUrl = "http://192.168.1.98:5000/api";

    void Start()
    {
        InitializeChoiceButtons();
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
    }

    private string GetCurrentUsername()
    {
        if (playerUsernameObject != null)
        {
            TMP_Text usernameText = playerUsernameObject.GetComponent<TMP_Text>();
            if (usernameText != null)
            {
                return usernameText.text;
            }
        }
        return PlayerPrefs.GetString("PlayerUsername", "Player");
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

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            playerisClose = true;
            StartDialogue();
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

    // New method to freeze player movement
    private void FreezePlayer()
    {
        if (playerMovement != null)
        {
            playerMovement.FreezeMovement();
        }
    }

    // New method to unfreeze player movement
    private void UnfreezePlayer()
    {
        if (playerMovement != null)
        {
            playerMovement.UnfreezeMovement();
        }
    }

    private void StartDialogue()
    {
        if (!dialoguePanel.activeInHierarchy)
        {
            dialoguePanel.SetActive(true);
            FreezePlayer(); // Freeze player when dialogue starts
            StartTyping();
        }
    }

    public void zeroText()
    {
        dialogueText.text = "";
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
    }

    private void StartTyping()
    {
        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
        }

        currentDialogueType = DialogueType.NPC;
        playerUsername = GetCurrentUsername();
        Sprite currentSprite = index == 0 && firstLineIsPlayerSprite ? playerSprite : npcSprite;
        SetSprite(currentSprite);

        string processedDialogue = ProcessDialogue(dialogue[index]);

        // Store in NPC dialogue history
        npcDialogueHistory.Add(processedDialogue);
        spriteHistory.Add(currentSprite);

        // Show back button only after first line of NPC dialogue
        UpdateBackButtonVisibility();

        typingCoroutine = StartCoroutine(Typing(processedDialogue));
    }

    private string ProcessDialogue(string rawDialogue)
    {
        return rawDialogue.Replace("[Your Name]", playerUsername);
    }

    IEnumerator Typing(string textToType)
    {
        dialogueText.text = "";
        foreach (char letter in textToType.ToCharArray())
        {
            dialogueText.text += letter;
            yield return new WaitForSeconds(wordSpeed);
        }

        if (dialogueText.text == textToType)
        {
            contButton.SetActive(true);
        }
    }

    public void NextLine()
    {
        contButton.SetActive(false);

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
    }

    private void ShowChoices()
    {
        currentDialogueType = DialogueType.Choice;

        for (int i = 0; i < 3; i++)
        {
            GameObject choiceButton = GetChoiceButton(i);
            choiceButton.GetComponentInChildren<Text>().text = choiceTexts[i].text;
            choiceButton.SetActive(true);
        }

        // Hide both continue and back buttons during choices
        contButton.SetActive(false);
        if (backButton != null)
        {
            backButton.SetActive(false);
        }

        SetSprite(playerSprite);
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

    private void HideChoiceButtons()
    {
        choiceButton1.SetActive(false);
        choiceButton2.SetActive(false);
        choiceButton3.SetActive(false);
    }

    private void OnChoiceSelected(int choiceIndex)
    {
        Debug.Log($"Player chose option {choiceIndex + 1}");
        HideChoiceButtons();

        if (useRewardSystem)
        {
            ShowRewardPopup(choiceIndex);
        }
        else
        {
            SaveChoiceToAPI(choiceIndex).ContinueWith(task => {
                if (task.IsFaulted)
                {
                    Debug.LogError("Error saving choice: " + task.Exception);
                }
            }, TaskScheduler.FromCurrentSynchronizationContext());
            ShowNPCReply(choiceIndex);
        }
    }

    private async Task SaveChoiceToAPI(int choiceIndex)
    {
        string currentSceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;

        using (HttpClient client = new HttpClient())
        {
            try
            {
                var choiceData = new
                {
                    username = playerUsername,
                    unit = currentUnit,
                    lesson = currentLesson,
                    sceneName = currentSceneName,
                    selectedChoice = choiceIndex
                };

                string json = JsonUtility.ToJson(choiceData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                HttpResponseMessage response = await client.PostAsync($"{baseUrl}/saveChoice", content);

                if (response.IsSuccessStatusCode)
                {
                    Debug.Log("Choice saved successfully via API.");
                }
                else
                {
                    string responseContent = await response.Content.ReadAsStringAsync();
                    Debug.LogError($"Failed to save choice via API: {responseContent}");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error saving choice via API: {ex.Message}");
            }
        }
    }

    private async Task SaveRewardToAPI(int choiceIndex, string rewardName, string congratsMessage)
    {
        string currentSceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;

        using (HttpClient client = new HttpClient())
        {
            try
            {
                var rewardData = new
                {
                    username = playerUsername,
                    unit = currentUnit,
                    lesson = currentLesson,
                    sceneName = currentSceneName,
                    selectedChoice = choiceIndex,
                    reward = new
                    {
                        rewardName = rewardName,
                        congratsMessage = congratsMessage,
                        timestamp = System.DateTime.UtcNow
                    }
                };

                string json = JsonUtility.ToJson(rewardData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                HttpResponseMessage response = await client.PostAsync($"{baseUrl}/saveReward", content);

                if (response.IsSuccessStatusCode)
                {
                    Debug.Log("Reward saved successfully via API.");
                }
                else
                {
                    string responseContent = await response.Content.ReadAsStringAsync();
                    Debug.LogError($"Failed to save reward via API: {responseContent}");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error saving reward via API: {ex.Message}");
            }
        }
    }

    private void ShowNPCReply(int choiceIndex)
    {
        isReplying = true;
        SetSprite(npcPovSprite);

        if (choiceIndex >= 0 && choiceIndex < npcReplies.Length)
        {
            StartCoroutine(TypeNPCReplyLines(npcReplies[choiceIndex].replies));
        }
        else
        {
            Debug.LogError("Invalid choice index for NPC replies.");
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
        yield return new WaitUntil(() => Mouse.current.leftButton.wasPressedThisFrame || (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame));
        contButton.SetActive(false);

        callback?.Invoke();
    }

    private void CompleteDialogue()
    {
        isDialogueComplete = true;
        StartCoroutine(PrepareForSceneTransition());
    }

    private IEnumerator PrepareForSceneTransition()
    {
        yield return new WaitForSeconds(1f);
        if (sceneTransition != null)
        {
            sceneTransition.StartTransition();
        }
        else
        {
            Debug.LogError("SceneTransition reference is missing!");
        }
    }

    private void SetSprite(Sprite newSprite)
    {
        if (currentSpeakerImage != null && newSprite != null)
        {
            currentSpeakerImage.GetComponent<Image>().sprite = newSprite;
        }
    }

    private void ShowRewardPopup(int choiceIndex)
    {
        if (rewardPopup != null && choiceIndex >= 0 && choiceIndex < rewardChoices.Count)
        {
            RewardChoice selectedReward = rewardChoices[choiceIndex];
            rewardPopup.SetActive(true);

            // Check if the reward system is enabled
            if (useRewardSystem)
            {
                // Set congratulatory text
                if (congratsText != null)
                {
                    congratsText.text = selectedReward.congratsMessage;
                    congratsText.gameObject.SetActive(true); // Activate congrats text
                    Debug.Log($"Setting congrats text: {congratsText.text}");
                }
                else
                {
                    Debug.LogWarning("congratsText is null!");
                }

                // Set reward text
                if (rewardText != null) // Ensure rewardText is defined in your class
                {
                    rewardText.text = selectedReward.rewardText; // Set the reward text
                    rewardText.gameObject.SetActive(true); // Activate reward text
                    Debug.Log($"Setting reward text: {rewardText.text}");
                }
                else
                {
                    Debug.LogWarning("rewardText is null!");
                }

                // Set reward image (star/object)
                if (rewardImage != null)
                {
                    if (selectedReward.rewardSprite != null)
                    {
                        rewardImage.sprite = selectedReward.rewardSprite;
                        rewardImage.gameObject.SetActive(true);
                        Debug.Log("Reward image set and activated");
                    }
                    else
                    {
                        rewardImage.gameObject.SetActive(false);
                        Debug.Log("Reward sprite is null, deactivating reward image");
                    }
                }
                else
                {
                    Debug.LogWarning("rewardImage is null!");
                }

                if (rewardBackground != null)
                {
                    rewardBackground.gameObject.SetActive(true);
                }
            }
            else
            {
                // Deactivate texts if reward system is not used
                if (congratsText != null)
                {
                    congratsText.gameObject.SetActive(false);
                }
                if (rewardText != null)
                {
                    rewardText.gameObject.SetActive(false);
                }
            }

            StartCoroutine(HideRewardPopupAndContinue(selectedReward, choiceIndex));
        }
        else
        {
            Debug.LogWarning("Reward popup is not set up correctly or invalid choice index!");
            SaveChoiceToAPI(choiceIndex).ContinueWith(task => {
                if (task.IsFaulted)
                {
                    Debug.LogError("Error saving choice: " + task.Exception);
                }
            }, TaskScheduler.FromCurrentSynchronizationContext());
            ShowNPCReply(choiceIndex);
        }
    }

    private IEnumerator HideRewardPopupAndContinue(RewardChoice reward, int choiceIndex)
    {
        yield return new WaitForSeconds(rewardDisplayDuration); // Use the public variable for duration

        if (rewardPopup != null)
        {
            rewardPopup.SetActive(false);
        }
        if (rewardBackground != null)
        {
            rewardBackground.gameObject.SetActive(false);
        }
        if (rewardImage != null)
        {
            rewardImage.gameObject.SetActive(false);
            Debug.Log("Deactivating reward image");
        }
        if (congratsText != null)
        {
            congratsText.gameObject.SetActive(false);
            Debug.Log("Deactivating congrats text");
        }
        if (rewardText != null) // Ensure rewardText is defined in your class
        {
            rewardText.gameObject.SetActive(false); // Deactivate reward text
            Debug.Log("Deactivating reward text");
        }

        SaveRewardToAPI(choiceIndex, reward.choiceName, reward.congratsMessage).ContinueWith(task => {
            if (task.IsFaulted)
            {
                Debug.LogError("Error saving reward: " + task.Exception);
            }
        }, TaskScheduler.FromCurrentSynchronizationContext());
        ShowNPCReply(choiceIndex);
    }

    public void SetUseRewardSystem(bool use)
    {
        useRewardSystem = use;
    }

    public void SetRewardChoices(List<RewardChoice> newChoices)
    {
        rewardChoices = newChoices;
    }

    public void GoBack()
    {
        List<string> currentHistory;

        switch (currentDialogueType)
        {
            case DialogueType.NPC:
                currentHistory = npcDialogueHistory;
                if (index > 0) index--;
                break;
            case DialogueType.Reply:
                currentHistory = npcReplyHistory;
                break;
            case DialogueType.Narration:
                currentHistory = narrationHistory;
                if (narrationIndex > 0) narrationIndex--;
                break;
            default:
                return;
        }

        if (currentHistory.Count > 1)
        {
            // Remove current dialogue
            currentHistory.RemoveAt(currentHistory.Count - 1);
            spriteHistory.RemoveAt(spriteHistory.Count - 1);

            // Get previous dialogue
            string previousDialogue = currentHistory[currentHistory.Count - 1];

            // Update UI
            if (typingCoroutine != null)
            {
                StopCoroutine(typingCoroutine);
            }
            dialogueText.text = previousDialogue;
            contButton.SetActive(true);

            // Update back button visibility
            UpdateBackButtonVisibility();
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
}