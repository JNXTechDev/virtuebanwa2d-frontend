using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Threading.Tasks;
using System;
using VirtueBanwa.Dialogue;  // Add this namespace reference

public class Logout : MonoBehaviour
{
    // Base URL for the API
    private string baseUrl => NetworkConfig.BaseUrl;

    // Method to handle logout
    public async void LogoutUser()
    {
        try
        {
            // Get username
            string username = PlayerPrefs.GetString("Username", "");
            if (!string.IsNullOrEmpty(username))
            {
                await LogoutServerCall(username);
            }

            // Clear all PlayerPrefs data
            ClearPlayerPrefs();
            
            // Try to reset dialogue states if available
            await ClearDialogueStates(username);
            
            // Load the login scene
            SceneManager.LoadScene("CreateorLogIn");
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error during logout: {ex.Message}");
            // Still load login scene even if there's an error
            SceneManager.LoadScene("CreateorLogIn");
        }
    }
    
    private async Task ClearDialogueStates(string username)
    {
        if (!string.IsNullOrEmpty(username))
        {
            try
            {
                // Reset all dialogue states on the server
                await DialogueState.ResetAllDialogueStates(username);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Error clearing dialogue states: {ex.Message}");
            }
        }
    }

    private void ClearPlayerPrefs()
    {
        PlayerPrefs.DeleteKey("Username");
        PlayerPrefs.DeleteKey("FirstName");
        PlayerPrefs.DeleteKey("LastName");
        PlayerPrefs.DeleteKey("Role");
        PlayerPrefs.DeleteKey("Section");
        PlayerPrefs.DeleteKey("Character");
        PlayerPrefs.DeleteKey("LoggedIn");
        PlayerPrefs.Save();
        
        // Reset all NPCs if needed
      //  ResetAllNPCs();
    }
    
    private void ResetAllNPCs()
    {
        // Find all NPCs in the scene
        NPCDialogueTrigger[] npcs = FindObjectsOfType<NPCDialogueTrigger>();
        foreach (NPCDialogueTrigger npc in npcs)
        {
            // Reset NPC state if needed
            if (npc != null && npc.gameObject.activeSelf)
            {
                // Reset any state needed for NPCs
                Debug.Log($"Resetting NPC: {npc.npcName}");
            }
        }
    }

    private async Task LogoutServerCall(string username)
    {
        // If you have a logout API endpoint, you can call it here
        // This is a placeholder for server-side logout functionality
        await Task.Delay(100); // Simulate network call
        Debug.Log($"User {username} logged out");
    }
}