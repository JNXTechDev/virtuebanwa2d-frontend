using UnityEngine;
using TMPro;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using System; // Add this line

public class TutorialChecker : MonoBehaviour
{
    private bool hasCompletedMarkDialogue = false;
    private bool hasCompletedAnnieDialogue = false;
    public GameObject feedbackPanel;
    public TMP_Text feedbackText;
    public float feedbackDuration = 3f;

    //private const string baseUrl = "http://192.168.1.11:5000/api"; // Changed from https to http
            private const string baseUrl = "http://192.168.43.149:5000/api"; // Updated URL

    void Start()
    {
        // Load checkpoint state from MongoDB
        LoadCheckpointState();
    }

    private async void LoadCheckpointState()
    {
        try
        {
            using (var handler = CustomHttpHandler.GetInsecureHandler())
            using (var client = new HttpClient(handler))
            {
                client.Timeout = TimeSpan.FromSeconds(10);
                string username = PlayerPrefs.GetString("Username");
                Debug.Log($"Loading checkpoint state for user: {username}");
                
                var response = await client.GetAsync($"{baseUrl}/game_progress/{username}");
                string content = await response.Content.ReadAsStringAsync();
                
                Debug.Log($"Tutorial Progress Response: {content}");

                if (response.IsSuccessStatusCode)
                {
                    var progressData = JsonUtility.FromJson<GameProgress>(content);
                    string tutorialStatus = progressData?.tutorial?.status ?? "Not Started";
                    Debug.Log($"Tutorial Status: {tutorialStatus}");

                    hasCompletedMarkDialogue = tutorialStatus.Contains("Mark");
                    hasCompletedAnnieDialogue = tutorialStatus.Contains("Annie");

                    // Enable Annie if Mark's dialogue is completed
                    if (hasCompletedMarkDialogue)
                    {
                        EnableAnnieNPC();
                        PlayerPrefs.SetInt("TutorialMark", 1);
                        PlayerPrefs.Save();
                    }

                    Debug.Log($"Mark completed: {hasCompletedMarkDialogue}, Annie completed: {hasCompletedAnnieDialogue}");
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error loading checkpoint state: {ex.Message}");
        }
    }

    public void OnNPCDialogueCompleted(string npcName)
    {
        Debug.Log($"NPC {npcName} dialogue completed");
        if (npcName == "Mark")
        {
            hasCompletedMarkDialogue = true;
            SaveCheckpointState("Mark");
            EnableAnnieNPC();
            PlayerPrefs.SetInt("TutorialMark", 1);
            PlayerPrefs.Save();
            Debug.Log("Saved Mark's tutorial checkpoint and enabled Annie");
        }
        else if (npcName == "Annie")
        {
            hasCompletedAnnieDialogue = true;
            SaveCheckpointState("Annie");
            Debug.Log("Saved Annie's tutorial checkpoint");
        }
    }

    private void EnableAnnieNPC()
    {
        GameObject annieNPC = GameObject.Find("NPC Annie Tutorial");
        if (annieNPC != null)
        {
            NPCscript annieScript = annieNPC.GetComponent<NPCscript>();
            if (annieScript != null)
            {
                annieScript.enabled = true;
                Debug.Log("Annie NPC enabled");
            }
            else
            {
                Debug.LogError("NPCscript component not found on Annie");
            }
        }
        else
        {
            Debug.LogError("Annie NPC object not found in scene");
        }
    }

    private async void SaveCheckpointState(string npcName)
    {
        string username = PlayerPrefs.GetString("Username");
        using (var handler = CustomHttpHandler.GetInsecureHandler())
        using (var client = new HttpClient(handler))
        {
            var data = new Dictionary<string, object>
            {
                ["Username"] = username,
                ["tutorial"] = new Dictionary<string, object>
                {
                    ["status"] = $"{npcName} Completed",
                    ["date"] = DateTime.Now.ToString("o")
                },
                ["units"] = new Dictionary<string, object>
                {
                    ["Unit1"] = new Dictionary<string, object>
                    {
                        ["lessons"] = new Dictionary<string, object>
                        {
                            ["Lesson1"] = new Dictionary<string, object>
                            {
                                ["npcsTalkedTo"] = new List<string> { npcName } // Add NPC name to the list
                            }
                        }
                    }
                }
                };

            var content = new StringContent(JsonUtility.ToJson(data), Encoding.UTF8, "application/json");
            await client.PostAsync($"{baseUrl}/game_progress", content);
        }
    }

    public bool CanEnterSchool()
    {
        bool canEnter = hasCompletedMarkDialogue && hasCompletedAnnieDialogue;
        Debug.Log($"Can enter school? {canEnter} (Mark: {hasCompletedMarkDialogue}, Annie: {hasCompletedAnnieDialogue})");
        return canEnter;
    }

    public void ShowTutorialFeedback()
    {
        if (feedbackPanel != null && feedbackText != null)
        {
            feedbackText.text = "Please complete the tutorial first! Talk to Mark and Annie.";
            feedbackPanel.SetActive(true);
            Invoke("HideFeedback", feedbackDuration);
        }
    }

    private void HideFeedback()
    {
        if (feedbackPanel != null)
        {
            feedbackPanel.SetActive(false);
        }
    }
}
