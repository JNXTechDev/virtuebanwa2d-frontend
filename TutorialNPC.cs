using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

public class TutorialNPC : MonoBehaviour
{
    [System.Serializable]
    public class TutorialDialogue
    {
        public string initialText;
        public string choiceText;
        public string response;
        public string rewardSprite;
    }

    [Header("NPC Settings")]
    public string npcName;
    public bool isFirstNPC = false; // Mark = true, Annie = false
    public TutorialDialogue dialogue;

    [Header("UI References")]
    public GameObject dialoguePanel;
    public TMP_Text dialogueText;
    public TMP_Text npcNameText;
    public Button choiceButton;
    public GameObject rewardPanel;

    private bool hasCompletedDialogue = false;
 //   private const string baseUrl = "http://192.168.1.11:5000/api";
            private const string baseUrl = "http://192.168.43.149:5000/api"; // Updated URL

    void Start()
    {
        if (!isFirstNPC) // This is Annie
        {
            CheckMarkProgress();
        }
        else // This is Mark
        {
            gameObject.SetActive(true);
        }

        InitializeUI();
    }

    private void InitializeUI()
    {
        if (choiceButton != null)
        {
            choiceButton.onClick.RemoveAllListeners();
            choiceButton.onClick.AddListener(OnChoiceSelected);
        }

        dialoguePanel?.SetActive(false);
        rewardPanel?.SetActive(false);
    }

    async void CheckMarkProgress()
    {
        string username = PlayerPrefs.GetString("Username");
        bool markCompleted = await HasCompletedTutorialNPC(username, "Mark");
        gameObject.SetActive(markCompleted);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            if (hasCompletedDialogue)
            {
                ShowResponse();
            }
            else
            {
                StartDialogue();
            }
        }
    }

    private void StartDialogue()
    {
        dialoguePanel.SetActive(true);
        dialogueText.text = dialogue.initialText;
        npcNameText.text = npcName;
        choiceButton.gameObject.SetActive(true);
        choiceButton.GetComponentInChildren<TMP_Text>().text = dialogue.choiceText;
    }

    private void ShowResponse()
    {
        dialoguePanel.SetActive(true);
        dialogueText.text = dialogue.response;
        choiceButton.gameObject.SetActive(false);
        StartCoroutine(AutoCloseDialogue());
    }

    private async void OnChoiceSelected()
    {
        string username = PlayerPrefs.GetString("Username");
        await SaveProgress(username);
        ShowReward();
        hasCompletedDialogue = true;
    }

    private void ShowReward()
    {
        rewardPanel.SetActive(true);
        StartCoroutine(AutoCloseReward());
    }

    private async Task<bool> HasCompletedTutorialNPC(string username, string npcName)
    {
        try
        {
            using (var client = new HttpClient())
            {
                var response = await client.GetAsync($"{baseUrl}/game_progress/{username}");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var progress = JsonConvert.DeserializeObject<GameProgress>(content);
                    return progress != null && progress.tutorial != null && progress.tutorial.status.Contains($"{npcName} Completed");
                }
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Error checking progress: {ex.Message}");
        }
        return false;
    }

    private async Task SaveProgress(string username)
    {
        try
        {
            using (var client = new HttpClient())
            {
                var data = new
                {
                    Username = username,
                    tutorial = new
                    {
                        status = $"{npcName} Completed",
                        reward = dialogue.rewardSprite,
                        date = System.DateTime.UtcNow
                    }
                };

                var content = new StringContent(
                    JsonConvert.SerializeObject(data),
                    Encoding.UTF8,
                    "application/json"
                );

                await client.PostAsync($"{baseUrl}/game_progress", content);
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Error saving progress: {ex.Message}");
        }
    }

    IEnumerator AutoCloseDialogue()
    {
        yield return new WaitForSeconds(3f);
        dialoguePanel.SetActive(false);
    }

    IEnumerator AutoCloseReward()
    {
        yield return new WaitForSeconds(3f);
        rewardPanel.SetActive(false);
        dialoguePanel.SetActive(false);
    }
}
