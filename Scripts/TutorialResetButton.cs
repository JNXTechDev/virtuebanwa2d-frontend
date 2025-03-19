using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using VirtueBanwa.Dialogue;  // Add this namespace reference

public class TutorialResetButton : MonoBehaviour
{
    private Button resetButton;
    private string baseUrl => NetworkConfig.BaseUrl;

    void Start()
    {
        resetButton = GetComponent<Button>();

        if (resetButton == null)
        {
            Debug.LogError("Reset button component not found!");
            return;
        }

        resetButton.onClick.AddListener(ResetTutorial);
    }

    private async void ResetTutorial()
    {
        string username = PlayerPrefs.GetString("Username", "");
        if (string.IsNullOrEmpty(username))
        {
            Debug.LogWarning("Cannot reset tutorial: Username not found in PlayerPrefs");
            return;
        }

        try
        {
            Debug.Log($"Resetting tutorial progress for user: {username}");

            // Disable button during reset
            resetButton.interactable = false;

            // Call API to reset tutorial progress
            bool success = await ResetTutorialProgress(username);

            if (success)
            {
                Debug.Log("Tutorial progress reset successfully!");

                // Reload the scene to apply changes
                string currentScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
                UnityEngine.SceneManagement.SceneManager.LoadScene(currentScene);
            }
            else
            {
                Debug.LogError("Failed to reset tutorial progress.");
                resetButton.interactable = true;
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error resetting tutorial progress: {ex.Message}");
            resetButton.interactable = true;
        }
    }

    private async Task<bool> ResetTutorialProgress(string username)
    {
        try
        {
            using (HttpClient client = new HttpClient())
            {
                // Reset tutorial progress
                HttpResponseMessage response = await client.DeleteAsync($"{baseUrl}/game_progress/{username}/tutorial");

                if (response.IsSuccessStatusCode)
                {
                    // Also reset all dialogue states
                    await DialogueState.ResetAllDialogueStates(username);

                    // Reset NPC states by finding all NPCDialogueTriggers and calling CheckRewardStatus
                    NPCDialogueTrigger[] allTriggers = FindObjectsOfType<NPCDialogueTrigger>();
                    foreach (var trigger in allTriggers)
                    {
                        trigger.SendMessage("CheckRewardStatus", SendMessageOptions.DontRequireReceiver);
                    }

                    Debug.Log($"Reset tutorial API response: {await response.Content.ReadAsStringAsync()}");
                    return true;
                }
                else
                {
                    Debug.LogError($"Failed to reset tutorial: {response.StatusCode}");
                    return false;
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"Exception when resetting tutorial: {ex.Message}");
            return false;
        }
    }
}