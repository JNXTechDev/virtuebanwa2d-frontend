using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading.Tasks;
using System.Net.Http;
using System;
using Newtonsoft.Json.Linq;
using UnityEngine.UI;
using TMPro;

public class TutorialChecker : MonoBehaviour
{
    [Header("Feedback UI")]
    public GameObject feedbackPanel;
    public TMP_Text feedbackText;
    public Button closeFeedbackButton;
    
    private string baseUrl => NetworkConfig.BaseUrl;
    
    void Start()
    {
        // Initialize UI elements
        if (feedbackPanel != null)
        {
            feedbackPanel.SetActive(false);
        }
        
        if (closeFeedbackButton != null)
        {
            closeFeedbackButton.onClick.AddListener(() => {
                if (feedbackPanel != null)
                    feedbackPanel.SetActive(false);
            });
        }
    }
    
    public async Task<bool> IsTutorialComplete(string username)
    {
        try
        {
            using (HttpClient client = new HttpClient())
            {
                HttpResponseMessage response = await client.GetAsync($"{baseUrl}/game_progress/{username}");
                
                if (response.IsSuccessStatusCode)
                {
                    string responseContent = await response.Content.ReadAsStringAsync();
                    JObject progressData = JObject.Parse(responseContent);
                    
                    // Check if tutorial exists and is completed
                    if (progressData["tutorial"] != null && 
                        progressData["tutorial"]["status"] != null &&
                        progressData["tutorial"]["status"].ToString() == "Completed")
                    {
                        return true;
                    }
                }
            }
            
            return false;
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error checking tutorial status: {ex.Message}");
            return false;
        }
    }
    
    // Show feedback about tutorial status
    public void ShowTutorialFeedback(bool isCompleted)
    {
        if (feedbackPanel == null || feedbackText == null)
        {
            Debug.LogWarning("Feedback UI elements not assigned in TutorialChecker");
            return;
        }
        
        feedbackPanel.SetActive(true);
        
        if (isCompleted)
        {
            feedbackText.text = "You have already completed the tutorial. Well done!";
        }
        else
        {
            feedbackText.text = "You need to complete the tutorial before accessing this area.";
        }
    }
}
