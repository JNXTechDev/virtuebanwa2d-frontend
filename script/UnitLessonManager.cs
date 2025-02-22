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
   // private const string baseUrl = "https://vbdb.onrender.com/api";
 private const string baseUrl = "http://192.168.1.4:5000/api"; // Updated URL


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

            currentUsername = PlayerPrefs.GetString("Username", "Unknown");
            Debug.Log($"[UnitLessonManager] Using stored username: {currentUsername}");

            if (usernameText != null)
            {
                usernameText.text = $"Player: {currentUsername}";
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

        await SaveSceneTransitionProgress(currentUsername, unitName, lesson.lessonName);

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

                    var progressData = JsonUtility.FromJson<GameProgress>(responseContent);

                    if (progressData != null)
                    {
                        Debug.Log($"Tutorial Status: {progressData.tutorial.status}, Reward: {progressData.tutorial.reward}, Date: {progressData.tutorial.date}");

                        foreach (var lesson in progressData.lessons)
                        {
                            Debug.Log($"Lesson: {lesson.Key}, Status: {lesson.Value.status}, Reward: {lesson.Value.reward}, Date: {lesson.Value.date}");
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
        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(unitName) || string.IsNullOrEmpty(lessonName))
        {
            Debug.LogError($"[SaveProgress] Invalid data - Username: {username}, Unit: {unitName}, Lesson: {lessonName}");
            return;
        }

        Debug.Log($"[SaveProgress] Saving progress - Username: {username}, Unit: {unitName}, Lesson: {lessonName}");

        var progressData = new GameProgressData
        {
            Username = username, // Changed from username to Username
            unit = unitName,
            lesson = lessonName
        };

        string json = JsonUtility.ToJson(progressData);
        Debug.Log($"[SaveProgress] Sending JSON: {json}");

        var content = new StringContent(json, Encoding.UTF8, "application/json");

        using (HttpClient client = new HttpClient())
        {
            HttpResponseMessage response = await client.PostAsync($"{baseUrl}/game_progress", content);
            
            string responseContent = await response.Content.ReadAsStringAsync();
            if (response.IsSuccessStatusCode)
            {
                Debug.Log($"[SaveProgress] Success. Response: {responseContent}");
            }
            else
            {
                Debug.LogError($"[SaveProgress] Failed - {response.ReasonPhrase}. Response: {responseContent}");
            }
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

    private string SanitizeUsername(string username)
    {
        return username.Replace(" ", "_").Replace(".", "-");
    }
}