using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using System;
using System.Threading.Tasks;

public class PlayManager : MonoBehaviour
{
    private static PlayManager instance;
    public static PlayManager Instance
    {
        get => instance;
        private set
        {
            if (instance == null)
                instance = value;
            else if (instance != value)
                Destroy(value.gameObject);
        }
    }

    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI usernameText;
    [SerializeField] private Button playButton;

    [Header("Scene Settings")]
    [SerializeField, Tooltip("The default scene to load for student users")]
    private string defaultStudentScene = "nene mainview";

    [SerializeField, Tooltip("The default scene to load for teacher users")]
    private string defaultTeacherScene = "Teacher.Main.View 1";

    private bool isInitialized = false;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Debug.LogWarning("Multiple PlayManager instances detected. Destroying the new one.");
            Destroy(gameObject);
        }
    }

    private async void Start()
    {
        await Initialize();
    }

    private void ValidateSerializedFields()
    {
        if (usernameText == null) Debug.LogError("Username Text is not assigned!");
        if (playButton == null) Debug.LogError("Play Button is not assigned!");
        if (string.IsNullOrEmpty(defaultStudentScene)) Debug.LogError("Default Student Scene is not set!");
        if (string.IsNullOrEmpty(defaultTeacherScene)) Debug.LogError("Default Teacher Scene is not set!");
    }

    private Task InitializeMongoDB()
    {
        try
        {
            // Retrieve user data from PlayerPrefs
            string username = PlayerPrefs.GetString("Username");
            string role = PlayerPrefs.GetString("Role");
            string section = PlayerPrefs.GetString("Section");

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(role))
            {
                Debug.LogWarning("No user data found, redirecting to login");
                SceneManager.LoadScene("CreateorLogIn");
                return Task.CompletedTask;
            }

            Debug.Log("MongoDB initialization completed");
        }
        catch (Exception ex)
        {
            Debug.LogError($"MongoDB initialization failed: {ex.Message}");
            ShowError("Failed to initialize game services. Please try again later.");
            throw;
        }

        return Task.CompletedTask;
    }

    private void SetupUI()
    {
        // Ensure usernameText is assigned in the Inspector
        if (usernameText == null)
        {
            Debug.LogWarning("Username Text is not assigned in the Inspector!");
            return;
        }

        // Ensure playButton is assigned in the Inspector
        if (playButton == null)
        {
            Debug.LogWarning("Play Button is not assigned in the Inspector!");
            return;
        }

        // Set up the play button click listener
        playButton.onClick.RemoveAllListeners();
        playButton.onClick.AddListener(HandlePlayButtonClick);
        playButton.interactable = true;

        // Update the UI with the current user's information
        UpdateUIWithPlayerInfo();
    }

    private void UpdateUIWithPlayerInfo()
    {
        if (usernameText != null)
        {
            string username = PlayerPrefs.GetString("Username");
            string role = PlayerPrefs.GetString("Role");
            string section = PlayerPrefs.GetString("Section");

            usernameText.text = $"Username: {username}\nRole: {role}\nSection: {section}";
            usernameText.enabled = true;
        }
    }

    private async void HandlePlayButtonClick()
    {
        if (!isInitialized)
        {
            Debug.LogError("PlayManager not properly initialized");
            await Task.Yield();
            ShowError("Please wait for initialization to complete.");
            return;
        }

        try
        {
            string role = PlayerPrefs.GetString("Role");
            string username = PlayerPrefs.GetString("Username");

            if (string.IsNullOrEmpty(role) || string.IsNullOrEmpty(username))
                throw new Exception("User data not set");

            // Redirect based on role
            if (role.Equals("Teacher", StringComparison.OrdinalIgnoreCase))
            {
                SceneManager.LoadScene(defaultTeacherScene);
            }
            else if (role.Equals("Student", StringComparison.OrdinalIgnoreCase))
            {
                SceneManager.LoadScene(defaultStudentScene);
            }
            else
            {
                throw new Exception($"Unknown role: {role}");
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error handling play button click: {ex.Message}");
            await Task.Yield();
            ShowError("Failed to start game. Please try again.");
            if (playButton != null)
                playButton.interactable = true;
        }
    }

    private void ShowError(string message)
    {
        Debug.LogError(message);
        // Implement your error UI display logic here
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnDestroy()
    {
        if (playButton != null)
        {
            playButton.onClick.RemoveAllListeners();
        }
        isInitialized = false;
    }

    private async void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == "Play")
        {
            await Initialize();
        }
    }

    private async Task Initialize()
    {
        try
        {
            ValidateSerializedFields();
            await InitializeMongoDB();
            SetupUI();
            isInitialized = true;

            // Debug log to verify UserData
            string username = PlayerPrefs.GetString("Username");
            string role = PlayerPrefs.GetString("Role");
            string section = PlayerPrefs.GetString("Section");

            Debug.Log($"UserData after login - Username: {username}, Role: {role}, Section: {section}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"Initialization failed: {ex.Message}");
            ShowError("Failed to initialize. Please try again.");
            isInitialized = false;
        }
    }

    // Public method to reset the instance
    public static void ResetInstance()
    {
        if (instance != null)
        {
            Debug.Log("Resetting PlayManager instance.");
            Destroy(instance.gameObject);
            instance = null;
        }
    }

    public string GetUsername()
    {
        return PlayerPrefs.GetString("Username", "");
    }
}