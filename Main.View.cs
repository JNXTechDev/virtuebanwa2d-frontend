using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using System.Threading.Tasks;
using System.Net.Http;
using System.Text;
using System;
using System.Collections.Generic;
using UnityEngine.UI;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq; // Add this for JObject instead of dynamic

public class MainView : MonoBehaviour
{
    // UI Elements
    public TMP_Text playerNameText;
    public TMP_Text classroomNameText;
    public GameObject boySprite;
    public GameObject girlSprite;
    public GameObject talkButton;
    public GameObject interactButton;
    public GameObject txtBox1;
    public GameObject npcGirl1;
    public GameObject profilePictureButton;
    public GameObject profileExitButton;
    public GameObject viewProfilePanel;
    public TMP_Text usernameText;
    public TMP_Text detailText;
    public TMP_Text sectionText;
    public TMP_Text roleText;
    public TMP_Text progressText;
    public TMP_Text rewardText;
    public Transform playerTransform;
    public Vector3 cameraOffset;
    public float smoothSpeed = 0.125f;
    public GameObject DialogueBoxPanel;
    public TMP_Text DialogueText;
    public GameObject congratsPanel;
    public TMP_Text congratsText;
    public Image spriteImage;
    public Image spriteImage2;
    public Sprite boyHead;
    public Sprite girlHead;

    // MongoDB API URL
 // Base URL for the API
    private string baseUrl => NetworkConfig.BaseUrl; 



    // Camera Follow reference
    public CameraFollow cameraFollow;

    private bool isNearNpc = false;

    // Start method
    void Start()
    {
        InitializeUI();
        interactButton.SetActive(true);
        DisplayPlayerInfo();
        
        // Make sure dialogue panels are hidden at start
        if (DialogueBoxPanel != null)
        {
            DialogueBoxPanel.SetActive(false);
        }
    }

    // Display player information using PlayerPrefs
    private void DisplayPlayerInfo()
    {
        // Retrieve user data from PlayerPrefs
        string username = PlayerPrefs.GetString("Username");
        string role = PlayerPrefs.GetString("Role");
        string section = PlayerPrefs.GetString("Section");
        string firstName = PlayerPrefs.GetString("FirstName");
        string lastName = PlayerPrefs.GetString("LastName");
        string character = PlayerPrefs.GetString("Character");

        // Debug logs to verify retrieved data
        Debug.Log($"Retrieved from PlayerPrefs - Username: {username}, Role: {role}, Section: {section}, FirstName: {firstName}, LastName: {lastName}, Character: {character}");

        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(role))
        {
            Debug.LogError("Username or role is not set in PlayerPrefs.");
            playerNameText.text = "Player: Unknown";
            return;
        }

        // Combine FirstName and LastName
        string fullName = $"{firstName} {lastName}";

        // Display player information for the profile panel not yet open 
        playerNameText.text = fullName; // Ensure this line is correct

        // Display player information for the profile panel when open
        usernameText.text = $"#{username}";
        detailText.text = fullName;
        sectionText.text = $"{section}";
        roleText.text = $"{role}";

        Debug.Log($"Character selected: {character}");
        Debug.Log($"Full Name: {fullName}");
        ShowCharacterSprite(character);

        // Set the playerTransform for CameraFollow
        if (character == "Boy")
        {
            playerTransform = boySprite.transform;
        }
        else if (character == "Girl")
        {
            playerTransform = girlSprite.transform;
        }
        else
        {
            Debug.LogWarning("Character data not found or is invalid.");
        }

        // Initialize CameraFollow
        if (cameraFollow != null)
        {
            cameraFollow.SetTarget(); // Ensure the target is set correctly
        }
        else
        {
            Debug.LogError("CameraFollow reference is not set in the Inspector.");
        }

        DisplayClassroomInfo();
    }

    // Display classroom information using PlayerPrefs
    private void DisplayClassroomInfo()
    {
        string section = PlayerPrefs.GetString("Section");

        if (string.IsNullOrEmpty(section))
        {
            Debug.LogError("Section is not set in PlayerPrefs.");
            classroomNameText.text = "Section: Not enrolled.";
            return;
        }

        classroomNameText.text = $"{section}";
        Debug.Log($"Updated classroomNameText: {classroomNameText.text}");
    }

    // Show character sprite based on selection
    public void ShowCharacterSprite(string character)
    {
        if (boySprite != null && girlSprite != null)
        {
            boySprite.SetActive(false);
            girlSprite.SetActive(false);

            if (character == "Boy")
            {
                boySprite.SetActive(true);
                Debug.Log("Boy sprite activated.");
                if (spriteImage != null)
                {
                    spriteImage.sprite = boyHead;
                }
                if (spriteImage2 != null)
                {
                    spriteImage2.sprite = boyHead;
                }
            }
            else if (character == "Girl")
            {
                girlSprite.SetActive(true);
                Debug.Log("Girl sprite activated.");
                if (spriteImage != null)
                {
                    spriteImage.sprite = girlHead;
                }
                if (spriteImage2 != null)
                {
                    spriteImage2.sprite = girlHead;
                }
            }
            else
            {
                Debug.LogWarning("Character data not found or is invalid.");
            }
        }
        else
        {
            Debug.LogError("Character sprites are not set in the Inspector.");
        }
    }

    // Initialize UI elements
    private void InitializeUI()
    {
        if (profilePictureButton != null)
        {
            profilePictureButton.GetComponent<Button>().onClick.AddListener(OnProfilePictureButtonClicked);
        }
        else
        {
            Debug.LogError("ProfilePictureButton reference is not set in the Inspector.");
        }

        if (profileExitButton != null)
        {
            profileExitButton.GetComponent<Button>().onClick.AddListener(OnProfileExitButtonClicked);
        }
        else
        {
            Debug.LogError("ProfileExitButton reference is not set in the Inspector.");
        }

        if (viewProfilePanel != null)
        {
            viewProfilePanel.SetActive(false);
        }
        else
        {
            Debug.LogError("ViewProfilePanel reference is not set in the Inspector.");
        }

        if (talkButton != null)
        {
            talkButton.SetActive(true);
        }
        else
        {
            Debug.LogError("TalkButton reference is not set in the Inspector.");
        }

        if (txtBox1 != null)
        {
            txtBox1.SetActive(false);
        }
        else
        {
            Debug.LogError("TxtBox1 reference is not set in the Inspector.");
        }

        if (congratsPanel != null)
        {
            congratsPanel.SetActive(false);
        }
        else
        {
            Debug.LogError("CongratsPanel reference is not set in the Inspector.");
        }

        if (DialogueBoxPanel != null)
        {
            DialogueBoxPanel.SetActive(true);
        }
        else
        {
            Debug.LogError("DialogueBoxPanel reference is not set in the Inspector.");
        }

        GameObject logoutButton = GameObject.Find("LogoutButton");
        if (logoutButton != null)
        {
            logoutButton.GetComponent<Button>().onClick.AddListener(Logout);
        }
        else
        {
            Debug.LogError("LogoutButton reference is not set in the Inspector.");
        }

        // Add refresh button listener
        GameObject refreshButton = GameObject.Find("RefreshButton");
        if (refreshButton != null)
        {
            refreshButton.GetComponent<Button>().onClick.AddListener(FetchPlayerProgress);
        }
        else
        {
            Debug.LogError("RefreshButton not found in the scene.");
        }
        
        // Fetch progress initially
        FetchPlayerProgress();
    }

    // Update method for frame updates
    void Update()
    {
        if (playerTransform != null)
        {
            Vector3 desiredPosition = playerTransform.position + cameraOffset;
            Vector3 smoothedPosition = Vector3.Lerp(Camera.main.transform.position, desiredPosition, smoothSpeed);
            Camera.main.transform.position = new Vector3(smoothedPosition.x, smoothedPosition.y, Camera.main.transform.position.z);
        }

        if (isNearNpc)
        {
            if (talkButton != null)
            {
                talkButton.SetActive(true);
            }

            if (Input.GetKeyDown(KeyCode.E))
            {
                TalkToNpc();
            }
        }
        else
        {
            if (talkButton != null)
            {
                talkButton.SetActive(false);
            }
        }
    }

    private void DisplayPlayerProfileInfo()
    {
        // Retrieve user data from PlayerPrefs
        string username = PlayerPrefs.GetString("Username");  
        string firstName = PlayerPrefs.GetString("FirstName");
        string lastName = PlayerPrefs.GetString("LastName");
        string section = PlayerPrefs.GetString("Section");
        string role = PlayerPrefs.GetString("Role");
        
        // Update UI elements with user data
        usernameText.text = $"#{username}"; 
        detailText.text = $"{firstName} {lastName}";
        sectionText.text = $"{section}";
        roleText.text = $"{role}";
        
        // Fetch the latest progress data from API instead of using PlayerPrefs
        FetchPlayerProgress();
    }

    // Handle profile picture button click
    private void OnProfilePictureButtonClicked()
    {
        if (viewProfilePanel != null)
        {
            viewProfilePanel.SetActive(true);
            DisplayPlayerProfileInfo();
        }
    }

    // Handle profile exit button click
    private void OnProfileExitButtonClicked()
    {
        if (viewProfilePanel != null)
        {
            viewProfilePanel.SetActive(false);
        }
    }

    // Handle NPC trigger enter
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("NPC"))
        {
            isNearNpc = true;
        }
    }

    // Handle NPC trigger exit
    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("NPC"))
        {
            isNearNpc = false;
        }
    }

    // Talk to NPC
    private void TalkToNpc()
    {
        ShowIntroDialogue();
    }

    // Show introductory dialogue
    private void ShowIntroDialogue()
    {
        if (DialogueBoxPanel != null)
        {
            DialogueBoxPanel.SetActive(true);
            DialogueText.text = "Hello! Welcome to the game!";
        }
    }

    // Show congratulations message
    private void ShowCongratulations()
    {
        if (congratsPanel != null)
        {
            congratsPanel.SetActive(true);
            congratsText.text = "Congratulations! You've completed Lesson 1 of Unit 1!";
        }
    }

    // Method to handle logout
    public void Logout()
    {
        // Clear user data from PlayerPrefs
        PlayerPrefs.DeleteKey("Username");
        PlayerPrefs.DeleteKey("Role");
        PlayerPrefs.DeleteKey("Section");
        PlayerPrefs.DeleteKey("FirstName");
        PlayerPrefs.DeleteKey("LastName");
        PlayerPrefs.DeleteKey("Character");
        PlayerPrefs.DeleteKey("Progress");
        PlayerPrefs.DeleteKey("Rewards");
        PlayerPrefs.Save();

        Debug.Log("UserData cleared from PlayerPrefs on logout.");

        // Optionally, update the UI to reflect the logged-out state
        UpdateUIAfterLogout();

        // Load the CreateorLogIn scene
        SceneManager.LoadScene("CreateorLogIn");
    }

    private void UpdateUIAfterLogout()
    {
        playerNameText.text = "Player: Unknown";
        classroomNameText.text = "Section: Not enrolled.";
    }

    // Method to fetch player progress from the API
    public async void FetchPlayerProgress()
    {
        try
        {
            string username = PlayerPrefs.GetString("Username");
            if (string.IsNullOrEmpty(username))
            {
                Debug.LogError("Username is not set in PlayerPrefs.");
                if (progressText != null)
                    progressText.text = "Progress: Not available";
                if (rewardText != null)
                    rewardText.text = "Rewards: Not available";
                return;
            }

            // Show loading indicator
            if (progressText != null)
                progressText.text = "Progress: Loading...";
            if (rewardText != null)
                rewardText.text = "Rewards: Loading...";

            using (HttpClient client = new HttpClient())
            {
                HttpResponseMessage response = await client.GetAsync($"{baseUrl}/game_progress/{username}");
                if (response.IsSuccessStatusCode)
                {
                    string responseContent = await response.Content.ReadAsStringAsync();
                    Debug.Log($"Progress data: {responseContent}");

                    // Parse the JSON using JObject instead of dynamic
                    JObject progressData = JsonConvert.DeserializeObject<JObject>(responseContent);
                    
                    if (progressData != null)
                    {
                        // Format the progress data for display
                        string formattedProgress = FormatProgressData(progressData);
                        string formattedRewards = FormatRewardsData(progressData);
                        
                        // Update UI
                        if (progressText != null)
                            progressText.text = formattedProgress;
                        if (rewardText != null)
                            rewardText.text = formattedRewards;
                        
                        Debug.Log($"Progress updated: {formattedProgress}");
                    }
                    else
                    {
                        Debug.LogError("Failed to parse progress data");
                        if (progressText != null)
                            progressText.text = "Progress: Data format error";
                    }
                }
                else
                {
                    Debug.LogError($"Error fetching progress: {response.StatusCode}");
                    if (progressText != null)
                        progressText.text = "Progress: Failed to load";
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error fetching player progress: {ex.Message}");
            if (progressText != null)
                progressText.text = "Progress: Error loading data";
        }
    }

    // Helper method to format progress data for display
    private string FormatProgressData(JObject data)
    {
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        sb.AppendLine("Progress Summary:");
        
        // Tutorial progress
        string tutorialStatus = "Not Started";
        
        // Safely access tutorial status with null checks
        JToken tutorialToken = data["tutorial"];
        if (tutorialToken != null && tutorialToken["status"] != null)
        {
            tutorialStatus = tutorialToken["status"].ToString();
        }
        
        sb.AppendLine($"Tutorial: {tutorialStatus}");
        
        // Units progress
        int completedUnits = 0;
        int completedLessons = 0;
        
        try
        {
            // Count completed units and lessons
            JToken unitsToken = data["units"];
            if (unitsToken != null)
            {
                foreach (JProperty unitProp in unitsToken)
                {
                    JToken unitValue = unitProp.Value;
                    
                    // Check if unit is completed
                    if (unitValue["status"] != null && unitValue["status"].ToString() == "Completed")
                    {
                        completedUnits++;
                    }
                    
                    // Check lessons
                    JToken lessonsToken = unitValue["lessons"];
                    if (lessonsToken != null)
                    {
                        foreach (JProperty lessonProp in lessonsToken)
                        {
                            JToken lessonValue = lessonProp.Value;
                            
                            // Check if lesson is completed
                            if (lessonValue["status"] != null && lessonValue["status"].ToString() == "Completed")
                            {
                                completedLessons++;
                            }
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"Error counting progress: {ex.Message}");
        }
        
        sb.AppendLine($"Units: {completedUnits}/4 completed");
        sb.AppendLine($"Lessons: {completedLessons}/24 completed");
        
        return sb.ToString();
    }

    // Helper method to format rewards data for display
    private string FormatRewardsData(JObject data)
    {
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        sb.AppendLine("Rewards Collected:");
        int starCount = 0;
        
        try
        {
            // Count tutorial stars
            JToken tutorialToken = data["tutorial"];
            JToken checkpointsToken = tutorialToken?["checkpoints"];
            
            if (checkpointsToken != null)
            {
                foreach (JProperty checkpoint in checkpointsToken)
                {
                    JToken rewardToken = checkpoint.Value["reward"];
                    string reward = rewardToken?.ToString() ?? "";
                    
                    if (!string.IsNullOrEmpty(reward))
                    {
                        starCount++;
                        sb.AppendLine($"• {checkpoint.Name}: {reward}");
                    }
                }
            }
            
            // Count unit/lesson stars
            JToken unitsToken = data["units"];
            if (unitsToken != null)
            {
                foreach (JProperty unit in unitsToken)
                {
                    JToken lessonsToken = unit.Value["lessons"];
                    if (lessonsToken != null)
                    {
                        foreach (JProperty lesson in lessonsToken)
                        {
                            JToken rewardToken = lesson.Value["reward"];
                            string reward = rewardToken?.ToString() ?? "";
                            
                            if (!string.IsNullOrEmpty(reward))
                            {
                                starCount++;
                                sb.AppendLine($"• {unit.Name} {lesson.Name}: {reward}");
                            }
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"Error counting rewards: {ex.Message}");
        }
        
        sb.Insert(0, $"Total Stars: {starCount}\n");
        
        return sb.ToString();
    }
}