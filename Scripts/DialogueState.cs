using System;
using System.Collections.Generic;
using UnityEngine;
using System.Threading.Tasks;
using System.Net.Http;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace VirtueBanwa.Dialogue
{
    /// <summary>
    /// Handles saving and loading dialogue state to/from the server
    /// </summary>
    public static class DialogueState
    {
        private static string BaseUrl => NetworkConfig.BaseUrl;
        
        /// <summary>
        /// Saves the state of a dialogue interaction
        /// </summary>
        public static async Task<bool> SaveDialogueState(string username, string dialogueId, bool completed = true)
        {
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    // Create request body
                    var requestData = new
                    {
                        Username = username,
                        DialogueId = dialogueId,
                        Completed = completed
                    };
                    
                    string json = JsonConvert.SerializeObject(requestData);
                    HttpContent content = new StringContent(json, Encoding.UTF8, "application/json");
                    
                    // Send POST request
                    HttpResponseMessage response = await client.PostAsync($"{BaseUrl}/dialogue_state", content);
                    
                    if (response.IsSuccessStatusCode)
                    {
                        Debug.Log($"Dialogue state saved: {dialogueId} = {completed}");
                        return true;
                    }
                    else
                    {
                        Debug.LogError($"Failed to save dialogue state: {response.StatusCode}");
                        return false;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error saving dialogue state: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Checks if a dialogue has been completed
        /// </summary>
        public static async Task<bool> IsDialogueCompleted(string username, string dialogueId)
        {
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    // Send GET request
                    HttpResponseMessage response = await client.GetAsync($"{BaseUrl}/dialogue_state/{username}/{dialogueId}");
                    
                    if (response.IsSuccessStatusCode)
                    {
                        string responseContent = await response.Content.ReadAsStringAsync();
                        JObject stateData = JObject.Parse(responseContent);
                        
                        // Check if dialogue is completed
                        if (stateData["completed"] != null && stateData["completed"].Value<bool>())
                        {
                            return true;
                        }
                    }
                    
                    // Default to not completed
                    return false;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error checking dialogue state: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Gets all completed dialogues for a user
        /// </summary>
        public static async Task<List<string>> GetCompletedDialogues(string username)
        {
            List<string> completedDialogues = new List<string>();
            
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    // Send GET request
                    HttpResponseMessage response = await client.GetAsync($"{BaseUrl}/dialogue_state/{username}");
                    
                    if (response.IsSuccessStatusCode)
                    {
                        string responseContent = await response.Content.ReadAsStringAsync();
                        JArray statesData = JArray.Parse(responseContent);
                        
                        // Extract completed dialogues
                        foreach (JObject state in statesData)
                        {
                            if (state["completed"] != null && state["completed"].Value<bool>() &&
                                state["dialogueId"] != null)
                            {
                                completedDialogues.Add(state["dialogueId"].ToString());
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error getting completed dialogues: {ex.Message}");
            }
            
            return completedDialogues;
        }
        
        /// <summary>
        /// Resets/deletes a dialogue state
        /// </summary>
        public static async Task<bool> ResetDialogueState(string username, string dialogueId)
        {
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    // Send DELETE request
                    HttpResponseMessage response = await client.DeleteAsync($"{BaseUrl}/dialogue_state/{username}/{dialogueId}");
                    
                    return response.IsSuccessStatusCode;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error resetting dialogue state: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Resets all dialogue states for a user
        /// </summary>
        public static async Task<bool> ResetAllDialogueStates(string username)
        {
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    // Send DELETE request
                    HttpResponseMessage response = await client.DeleteAsync($"{BaseUrl}/dialogue_state/{username}");
                    
                    return response.IsSuccessStatusCode;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error resetting all dialogue states: {ex.Message}");
                return false;
            }
        }

        public static async Task<bool> IsTutorialCompleted(string username)
        {
            bool janicaCompleted = await IsDialogueCompleted(username, "Janica");
            bool markCompleted = await IsDialogueCompleted(username, "Mark");
            bool annieCompleted = await IsDialogueCompleted(username, "Annie");
            bool rojanCompleted = await IsDialogueCompleted(username, "Rojan");
            
            return janicaCompleted && markCompleted && annieCompleted && rojanCompleted;
        }
    }
}
