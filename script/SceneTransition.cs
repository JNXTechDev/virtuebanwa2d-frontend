using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.UI;
using UnityEngine.Video;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System;

public class SceneTransition : MonoBehaviour
{
    [System.Serializable]
    public class LessonInfo
    {
        public string unit = "Tutorial"; // Change default unit name
        public string lesson = "Introduction"; // Change default lesson name
        public string rewardName = "Star Badge"; // Add this line
        public string rewardMessage = "You've mastered the basics!"; // Add this line
    }

    public GameObject usernameObject;      // GameObject containing the dynamic username
    public string sceneName;               // The name of the current scene
    public string checkpoint;              // The checkpoint or progress indicator in the scene
    public string sceneToLoad;             // The next scene to load after transition
    public TMP_Text progressText;          // TextMeshPro UI text for showing progress messages
    [SerializeField] private LessonInfo currentLessonInfo = new LessonInfo();

    private bool isSaving = false;         // Ensures saving happens only once per trigger

    // New variables for transition effects
    public Image blackScreen;
    public GameObject loadingIdleObject; // Change this to GameObject
    public VideoPlayer loadingIdleVideo; // Add this for video playback

    [Range(0f, 1f)]
    public float initialBlackScreenAlpha = 0f; // Add this line

   // private const string baseUrl = "https://vbdb.onrender.com/api";


 private const string baseUrl = "http://192.168.1.4:5000/api"; // Updated URL

    // Constants for UI messages
    private const string SavingProgressMessage = "Saving progress...";
    private const string ProgressSavedMessage = "Progress saved!";
    private const string UnknownUsername = "Unknown";

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

    // New public method to start the transition
    public void StartTransition()
    {
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
            progressText.text = SavingProgressMessage;
            progressText.gameObject.SetActive(true);
        }

        string currentUsername = GetCurrentUsername();

        yield return SaveProgressToMongoDB(currentUsername, currentLessonInfo.unit, currentLessonInfo.lesson);

        yield return new WaitForSeconds(2f);

        if (progressText != null)
        {
            progressText.text = ProgressSavedMessage;
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

    private async Task SaveProgressToMongoDB(string username, string unit, string lesson)
    {
        Debug.Log($"Saving progress for username: {username}, unit: {unit}, lesson: {lesson}");

        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(unit) || string.IsNullOrEmpty(lesson))
        {
            Debug.LogError("Invalid data. Username, unit, and lesson are required.");
            return;
        }

        var progressData = new GameProgressData
        {
            Username = username, // Changed to match the property name in GameProgressData
            unit = unit,
            lesson = lesson,
            reward = currentLessonInfo.rewardName,
            message = currentLessonInfo.rewardMessage
        };

        using (HttpClient client = new HttpClient())
        {
            try
            {
                string json = JsonUtility.ToJson(progressData);
                Debug.Log($"Sending JSON: {json}");
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                HttpResponseMessage response = await client.PostAsync($"{baseUrl}/game_progress", content);
                string responseContent = await response.Content.ReadAsStringAsync();
                
                if (!response.IsSuccessStatusCode)
                {
                    Debug.LogError($"Failed to save progress: {response.ReasonPhrase}. Response content: {responseContent}");
                }
                else
                {
                    Debug.Log($"Progress saved successfully. Response: {responseContent}");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error saving progress: {ex.Message}");
            }
        }
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

    public void SetCurrentUnit(string unit)
    {
        currentLessonInfo.unit = unit;
    }

    public void SetCurrentLesson(string lesson)
    {
        currentLessonInfo.lesson = lesson;
    }
}