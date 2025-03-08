using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.UI;
using UnityEngine.Video;
using System.Collections;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System;
using VirtueBanwa;  // For DialogueData
using System.Globalization;
using System.Collections.Generic;  // Add this line to fix Dictionary errors
using Newtonsoft.Json;  // Add this using statement

public class SceneTransition : MonoBehaviour
{
    [Header("Scene Configuration")]
    public GameObject usernameObject;      // GameObject containing the dynamic username
    public string sceneName;               // The name of the current scene
    public string sceneToLoad;             // The next scene to load after transition
    
    [Header("UI Elements")]
    public TMP_Text progressText;          // TextMeshPro UI text for showing progress messages
    public Image blackScreen;              // Fade transition imagea
    public GameObject loadingIdleObject;   // Loading animation object
    public VideoPlayer loadingIdleVideo;   // Loading animation video player

    [Range(0f, 1f)]
    public float initialBlackScreenAlpha = 0f;

    private bool isSaving = false;
    private const string UnknownUsername = "Unknown";
   // private const string baseUrl = "http://192.168.1.11:5000/api";

    private string chosenReward; // Add this field
    private string chosenResponse; // Add this field
    private string currentUnit; // Add this field
    private string currentLesson; // Add this field
    private int chosenScore; // Add this field for storing the reward score

    public Animator animator;
    public float transitionTime = 1f;
    
    private static readonly string TRIGGER_START = "Start";

 // Base URL for the API
    private string baseUrl => NetworkConfig.BaseUrl; 


    // Start function to initialize
    void Start()
    {
        // Verify that the username object is assigned and has the correct component
        if (usernameObject == null)
        {
            Debug.LogWarning("Username object is not assigned.");
        }
        else if (usernameObject.GetComponent<TMP_Text>() == null)
        {
            Debug.LogWarning("Username object does not have a TMP_Text component.");
        }

        // Set initial alpha for black screen and update raycast status
        if (blackScreen != null)
        {
            SetBlackScreenAlpha(initialBlackScreenAlpha);
        }

        // If animator is not assigned, try to get it
        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }
    }

    private void SetBlackScreenAlpha(float alpha)
    {
        if (blackScreen != null)
        {
            Color color = blackScreen.color;
            color.a = alpha;
            blackScreen.color = color;

            // Disable raycast when fully transparent
            blackScreen.raycastTarget = (alpha > 0);
        }
    }

    // New public method to start the transition without specific scene name
    public void StartTransition()
    {
        if (!isSaving)
        {
            isSaving = true;
            StartCoroutine(SaveProgressAndTransition());
        }
    }

    // Main StartTransition method that handles scene transitions
    public void StartTransition(string nextSceneName)
    {
        sceneToLoad = nextSceneName; // Store the scene name
        Debug.Log($"Starting transition to scene: {sceneToLoad}");
        
        if (!isSaving)
        {
            isSaving = true;
            
            // If we have an animator, play transition animation
            if (animator != null)
            {
                StartCoroutine(LoadSceneWithAnimation());
            }
            else
            {
                // No animation, start the transition directly
                StartCoroutine(SaveProgressAndTransition());
            }
        }
    }

    // Add this new method
    public void StartTransitionWithReward(string nextSceneName, string reward, string response)
    {
        sceneToLoad = nextSceneName;
        chosenReward = reward;
        chosenResponse = response;
        if (!isSaving)
        {
            isSaving = true;
            StartCoroutine(SaveProgressAndTransition());
        }
    }

    public void StartTransitionWithReward(string nextSceneName, string reward, string response, string unit, string lesson)
    {
        sceneToLoad = nextSceneName;
        chosenReward = reward;
        chosenResponse = response;
        currentUnit = unit;
        currentLesson = lesson;
        
        if (!isSaving)
        {
            isSaving = true;
            StartCoroutine(SaveProgressAndTransition());
        }
    }

    // Update StartTransitionWithReward to include score
    public void StartTransitionWithReward(string nextSceneName, string reward, string response, string unit, string lesson, int score = 30)
    {
        sceneToLoad = nextSceneName;
        chosenReward = reward;
        chosenResponse = response;
        currentUnit = unit;
        currentLesson = lesson;
        chosenScore = score; // Store the score
        
        if (!isSaving)
        {
            isSaving = true;
            StartCoroutine(SaveProgressAndTransition());
        }
    }

    // Function that will trigger when the player enters a scene transition area
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player") && !other.isTrigger && !isSaving)
        {
            isSaving = true;  // Prevents multiple saves
            StartCoroutine(SaveProgressAndTransition());
        }
    }

    // Modified coroutine to handle saving progress and transitioning scenes
    private IEnumerator SaveProgressAndTransition()
    {
        yield return StartCoroutine(FadeIn(blackScreen));

        loadingIdleObject.SetActive(true);
        if (loadingIdleVideo != null)
        {
            loadingIdleVideo.Play();
        }

        if (progressText != null)
        {
            progressText.text = "Saving progress...";
            progressText.gameObject.SetActive(true);
        }

        string currentUsername = GetCurrentUsername();

        yield return SaveProgressToMongoDB(currentUsername);

        yield return new WaitForSeconds(2f);

        if (progressText != null)
        {
            progressText.text = "Progress saved!";
        }

        yield return new WaitForSeconds(1.5f);

        if (progressText != null)
        {
            progressText.gameObject.SetActive(false);
        }

        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneToLoad);
        asyncLoad.allowSceneActivation = false;

        while (asyncLoad.progress < 0.9f)
        {
            yield return null;
        }

        loadingIdleObject.SetActive(false);
        if (loadingIdleVideo != null)
        {
            loadingIdleVideo.Stop();
        }

        asyncLoad.allowSceneActivation = true;

        while (!asyncLoad.isDone)
        {
            yield return null;
        }

        yield return StartCoroutine(FadeOut(blackScreen));
    }

    private async Task SaveProgressToMongoDB(string username)
    {
        try {
            var isTutorial = currentLesson?.ToLower() == "tutorial";
            Debug.Log($"Saving progress for: {(isTutorial ? "Tutorial" : $"{currentUnit} {currentLesson}")}");

            // Initialize these variables at the top for wider scope
            string unitKey = currentUnit?.Replace("UNIT ", "Unit").Replace(" ", "");
            string lessonKey = currentLesson?.Replace("Lesson ", "Lesson");
            
            var data = new Dictionary<string, object> {
                ["Username"] = username
            };

            if (isTutorial)
              {
                data["tutorial"] = new Dictionary<string, object> {
                    ["status"] = "Completed",
                    ["reward"] = chosenReward,
                    ["date"] = DateTime.Now.ToString("o")
                };
            }
            else 
            {
                data["units"] = new Dictionary<string, object> {
                    [unitKey] = new Dictionary<string, object> {
                        ["status"] = "In Progress",
                        ["completedLessons"] = 1,
                        ["lessons"] = new Dictionary<string, object> {
                            [lessonKey] = new Dictionary<string, object> {
                                ["status"] = "Completed", 
                                ["reward"] = chosenReward,  // Make sure reward is passed
                                ["score"] = chosenScore,    // Make sure score is passed
                                ["lastAttempt"] = DateTime.Now.ToString("o")
                            }
                        }
                    }
                };

                Debug.Log($"Sending progress update - Lesson: {lessonKey}, Reward: {chosenReward}, Score: {chosenScore}");
            }

            using (var client = new HttpClient())
            {
                var content = new StringContent(
                    JsonConvert.SerializeObject(data), 
                    Encoding.UTF8, 
                    "application/json"
                );

                Debug.Log($"Sending data to server: {await content.ReadAsStringAsync()}");

                var response = await client.PostAsync($"{baseUrl}/game_progress", content);
                var responseText = await response.Content.ReadAsStringAsync();
                Debug.Log($"Server response: {responseText}");

                if (!response.IsSuccessStatusCode)
                {
                    throw new Exception($"Server returned {response.StatusCode}: {responseText}");
                }

                // Verify the saved data
                var verifyResponse = await client.GetAsync($"{baseUrl}/game_progress/{username}");
                var verifyData = await verifyResponse.Content.ReadAsStringAsync();
                Debug.Log($"Progress data loaded: {verifyData}");
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"Failed to save progress: {ex.Message}");
        }
    }

    private string GetPhilippineDateTime()
    {
        // Convert to Philippine Time (UTC+8)
        TimeZoneInfo philippineZone = TimeZoneInfo.CreateCustomTimeZone(
            "Philippine Time",
            new TimeSpan(8, 0, 0),
            "Philippine Time",
            "Philippine Time"
        );

        DateTime philippineTime = TimeZoneInfo.ConvertTime(DateTime.Now, philippineZone);
        return philippineTime.ToString("yyyy-MM-dd hh:mm:ss tt", CultureInfo.InvariantCulture);
    }

    // New method for fading in an image
    private IEnumerator FadeIn(Image image)
    {
        float alpha = initialBlackScreenAlpha;
        while (alpha < 1f)
        {
            alpha += Time.deltaTime;
            SetBlackScreenAlpha(alpha);
            yield return null;
        }
    }

    // New method for fading out an image
    private IEnumerator FadeOut(Image image)
    {
        float alpha = 1f;
        while (alpha > 0f)
        {
            alpha -= Time.deltaTime;
            SetBlackScreenAlpha(alpha);
            yield return null;
        }
    }

    // Helper method to get the current username
    private string GetCurrentUsername()
    {
        // Always use the stored username from PlayerPrefs for API calls
        string Username = PlayerPrefs.GetString("Username", UnknownUsername); //change string to Username? not username???
        Debug.Log($"Using stored username for API call: {Username}"); 
        return Username;
    }

    public void LoadSceneImmediately(string sceneName)
    {
        StartCoroutine(LoadSceneSequence(sceneName));
    }

    private IEnumerator LoadSceneSequence(string sceneName)
    {
        // Get and disable the main camera
        if (Camera.main != null)
        {
            Camera.main.gameObject.SetActive(false);
        }

        // Hide all canvases except loading animation
        var canvases = FindObjectsOfType<Canvas>();
        foreach (var canvas in canvases)
        {
            if (loadingIdleObject != null && !loadingIdleObject.transform.IsChildOf(canvas.transform))
            {
                canvas.gameObject.SetActive(false);
            }
        }
        
        // Show loading animation
        if (loadingIdleObject != null)
        {
            loadingIdleObject.SetActive(true);
            
            if (loadingIdleVideo != null)
            {
                loadingIdleVideo.Play();
                yield return null;
            }
        }

        // Load next scene immediately
        UnityEngine.SceneManagement.SceneManager.LoadScene(sceneName);
    }
    
    private System.Collections.IEnumerator LoadSceneWithAnimation()
    {
        // Trigger the animation
        animator.SetTrigger(TRIGGER_START);
        
        // Wait for animation
        yield return new WaitForSeconds(transitionTime);
        
        // Load the scene
        StartCoroutine(SaveProgressAndTransition());
    }
    
    private void LoadSceneDirectly()
    {
        if (string.IsNullOrEmpty(sceneToLoad))
        {
            Debug.LogError("No scene specified to load!");
            return;
        }
        
        Debug.Log($"Loading scene directly: {sceneToLoad}");
        
        try 
        {
            SceneManager.LoadScene(sceneToLoad);
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Error loading scene '{sceneToLoad}': {ex.Message}");
        }
    }
}