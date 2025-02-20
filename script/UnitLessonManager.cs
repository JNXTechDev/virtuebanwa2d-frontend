using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.UI;
using UnityEngine.Video;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using System;

public class UnitLessonManager : MonoBehaviour
{
    [System.Serializable]
    public class LessonData
    {
        public string lessonName;
        public string defaultScene;
        public bool isUnlocked;
        public string lastCheckpoint;
    }

    [System.Serializable]
    public class UnitData
    {
        public string unitName;
        public List<LessonData> lessons = new List<LessonData>();
        public bool isUnlocked;
    }

    [Header("UI References")]
    [SerializeField] private Transform unitButtonContainer;
    [SerializeField] private GameObject unitButtonPrefab;
    [SerializeField] private GameObject lessonSelectionPanel;
    [SerializeField] private Transform lessonButtonContainer;
    [SerializeField] private GameObject lessonButtonPrefab;
    [SerializeField] private TextMeshProUGUI usernameText;
    [SerializeField] private GameObject loadingPanel;
    [SerializeField] private TextMeshProUGUI loadingText;
    [SerializeField] private Button backButton;

    [Header("Unit Data")]
    [SerializeField] private List<UnitData> units = new List<UnitData>();

    private string currentUsername;
    //private const string baseUrl = "http://192.168.1.98:5000/api";
    private const string baseUrl = "https://vbdb.onrender.com/api";


    private void OnEnable()
    {
        StartCoroutine(InitializeAfterDelay());

        if (backButton != null)
        {
            backButton.onClick.AddListener(OnBackButtonClicked);
        }
        else
        {
            Debug.LogError("Back Button reference is missing!");
        }
    }

    private IEnumerator InitializeAfterDelay()
    {
        while (PlayManager.Instance == null || string.IsNullOrEmpty(PlayManager.Instance.GetUsername()))
        {
            yield return new WaitForSeconds(0.1f);
        }

        Initialize();
    }

    private void Initialize()
    {
        try
        {
            Debug.Log("Initializing UnitLessonManager");

            // Retrieve username from PlayManager with sanitization
            string username = PlayManager.Instance.GetUsername();
            currentUsername = SanitizeUsername(username);
            Debug.Log($"Current Username: {currentUsername}");

            if (usernameText != null)
            {
                if (!string.IsNullOrEmpty(currentUsername))
                {
                    usernameText.text = $"Player: {currentUsername}";
                    Debug.Log($"Username set to: {currentUsername}");
                }
                else
                {
                    Debug.LogWarning("Username is not set in PlayManager, using PlayerPrefs");
                    currentUsername = PlayerPrefs.GetString("CurrentUsername", "Unknown");
                    currentUsername = SanitizeUsername(currentUsername);
                    usernameText.text = $"Player: {currentUsername}";
                }
            }
            else
            {
                Debug.LogError("Username Text reference is missing!");
            }

            LoadUserProgress();
            SetupUnitButtons();
            lessonSelectionPanel.SetActive(false);
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error in Initialize: {ex.Message}\n{ex.StackTrace}");
        }
    }

    private void SetupUnitButtons()
    {
        foreach (UnitData unit in units)
        {
            GameObject buttonObj = Instantiate(unitButtonPrefab, unitButtonContainer);
            Button button = buttonObj.GetComponent<Button>();
            TextMeshProUGUI buttonText = buttonObj.GetComponentInChildren<TextMeshProUGUI>();

            buttonText.text = unit.unitName;
            button.onClick.AddListener(() => ShowLessonsForUnit(unit));

            button.interactable = unit.isUnlocked;
        }
    }

    private void ShowLessonsForUnit(UnitData unit)
    {
        foreach (Transform child in lessonButtonContainer)
        {
            Destroy(child.gameObject);
        }

        foreach (LessonData lesson in unit.lessons)
        {
            GameObject buttonObj = Instantiate(lessonButtonPrefab, lessonButtonContainer);
            Button button = buttonObj.GetComponent<Button>();
            TextMeshProUGUI buttonText = buttonObj.GetComponentInChildren<TextMeshProUGUI>();
            RectTransform rectTransform = buttonObj.GetComponent<RectTransform>();

            buttonText.text = lesson.lessonName;
            button.onClick.AddListener(() => LoadLesson(unit.unitName, lesson));
            button.interactable = lesson.isUnlocked;

            LayoutRebuilder.ForceRebuildLayoutImmediate(rectTransform);
        }

        lessonSelectionPanel.SetActive(true);
    }

    private async void LoadLesson(string unitName, LessonData lesson)
    {
        loadingPanel.SetActive(true);
        loadingText.text = $"Loading {unitName} - {lesson.lessonName}...";

        // Save progress before loading the scene
        await SaveSceneTransitionProgress(currentUsername, unitName, lesson.lessonName);

        // Load the scene
        SceneManager.LoadScene(lesson.defaultScene);
    }

    private async void LoadUserProgress()
    {
        loadingPanel.SetActive(true);
        loadingText.text = "Loading progress...";

        using (HttpClient client = new HttpClient())
        {
            try
            {
                HttpResponseMessage response = await client.GetAsync($"{baseUrl}/game_progress/{currentUsername}");
                if (response.IsSuccessStatusCode)
                {
                    string responseContent = await response.Content.ReadAsStringAsync();
                    Debug.Log($"Progress data loaded: {responseContent}");

                    var progressData = JsonUtility.FromJson<UserProgressData>(responseContent);

                    if (progressData != null && progressData.progress != null && progressData.progress.Count > 0)
                    {
                        foreach (var progress in progressData.progress)
                        {
                            Debug.Log($"Unit: {progress.unit}, Lesson: {progress.lesson}, Timestamp: {progress.timestamp}");
                        }
                    }
                    else
                    {
                        Debug.Log("No progress loaded.");
                    }
                }
                else
                {
                    Debug.LogError($"Failed to load progress: {response.ReasonPhrase}");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error loading progress: {ex.Message}");
            }
            finally
            {
                loadingPanel.SetActive(false);
            }
        }
    }

    private async Task SaveSceneTransitionProgress(string username, string unitName, string lessonName)
    {
        if (string.IsNullOrEmpty(username))
        {
            Debug.LogError("Username is null or empty, cannot save progress.");
            return;
        }
        Debug.Log($"Saving progress for username: {username}, unit: {unitName}, lesson: {lessonName}");

        string unit = unitName;
        string lesson = lessonName;

        try
        {
            var progressData = new
            {
                username = username,
                unit = unit,
                lesson = lesson
            };

            string json = JsonUtility.ToJson(progressData);
            Debug.Log($"Sending JSON: {json}");

            var content = new StringContent(json, Encoding.UTF8, "application/json");

            using (HttpClient client = new HttpClient())
            {
                HttpResponseMessage response = await client.PostAsync($"{baseUrl}/game_progress", content);

                if (response.IsSuccessStatusCode)
                {
                    string responseContent = await response.Content.ReadAsStringAsync();
                    Debug.Log($"Progress saved successfully. Response: {responseContent}");

                    var responseData = JsonUtility.FromJson<ProgressResponse>(responseContent);
                    Debug.Log($"Saved progress - Unit: {responseData.progress.unit}, Lesson: {responseData.progress.lesson}, Timestamp: {responseData.progress.timestamp}");
                }
                else
                {
                    string responseContent = await response.Content.ReadAsStringAsync();
                    Debug.LogError($"Failed to save progress: {response.ReasonPhrase}. Response content: {responseContent}");
                }
            }
        }
        catch (HttpRequestException ex)
        {
            Debug.LogError($"HTTP request error: {ex.Message}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error saving progress: {ex.Message}");
        }
    }

    private void OnBackButtonClicked()
    {
        lessonSelectionPanel.SetActive(false);
        foreach (Transform child in lessonButtonContainer)
        {
            Destroy(child.gameObject);
        }
    }

    private void OnDestroy()
    {
        if (backButton != null)
        {
            backButton.onClick.RemoveListener(OnBackButtonClicked);
        }
    }

    // Method to sanitize the username
    private string SanitizeUsername(string username)
    {
        // Basic sanitization, adjust based on your needs
        return username.Replace(" ", "_").Replace(".", "-");
    }
}

[System.Serializable]
public class UserProgressData
{
    public List<GameProgress> progress;
}

[System.Serializable]
public class GameProgress
{
    public string unit;
    public string lesson;
    public string timestamp;
}

[System.Serializable]
class ProgressResponse
{
    public string message;
    public ProgressData progress;
}

[System.Serializable]
class ProgressData
{
    public string unit;
    public string lesson;
    public string timestamp;
}