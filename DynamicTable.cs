using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;
using MongoDB.Bson;
using MongoDB.Driver;
using System.Threading.Tasks;
using System.Linq;
using System;
using System.IO;
using System.Text;
using System.Collections;
using Unity.VisualScripting;
using UnityEngine.Rendering;
#if UNITY_ANDROID
using UnityEngine.Android;
#endif
using UnityEngine.Networking;
using System.Runtime.CompilerServices;
using ExcelDataReader;
using System.Data; // For DataSet
using Newtonsoft.Json;

public class DynamicTable : MonoBehaviour 
{
    private bool encodingRegistered = false;

    public GameObject rowPrefab;  // Reference to the RowTemplate prefab
    public ScrollRect scrollRect; // Reference to the ScrollRect
    public Transform tableParent; // This is now the Content of the ScrollRect
    public GameObject viewStudentPanel; // Reference to the ViewStudentPanel
    public TMP_InputField searchInputField; // Reference to the Search Input Field
    public Button searchButton; // Reference to the Search Button
    public GameObject createStudentPanel; // Reference to the CreateStudentPanel
    public Button addStudentButton; // Reference to the AddStudentButton
    public Button closeCreateStudentButton; // Reference to the XButton for closing the student panel
    public TMP_InputField firstNameInputField; // Reference to the FirstNameInputField
    public TMP_InputField lastNameInputField; // Reference to the LastNameInputField
    public TMP_InputField usernameInputField; // Reference to the UsernameInputField
    public TMP_Dropdown sectionDropdown; // Reference to the SectionDropdown
    public Button createStudentButton; // Reference to the CreateStudentButton
    public TMP_Dropdown characterDropdown; // Reference to the CharacterDropdown

    public GameObject viewRewardsPanel; // Reference to the ViewRewardsPanel
    public Button sendRewardsButton; // Reference to the SendRewardsButton
    public Button closeRewardsPanelButton; // Reference to the XButton for closing the rewards panel
    public Button removeStudentButton; // Reference to the RemoveStudentButton in ViewStudentPanel
    public Button uploadButton; // Reference to the UploadButton for uploading the Excel file
    public Button refreshButton; // Reference to the RefreshButton for refreshing the table

    //for Sending Rewards
    public GameObject sendRewardsPanel; // Reference to the SendRewardsPanel
    public TMP_Text studentNameText; // Reference to the StudentNameText in SendRewardsPanel
    public TMP_InputField messageInputField; // Reference to the MessageInputField in SendRewardsPanel
    public Image previewRewardsBoxImg; // Reference to the PreviewRewardsBoxImg in SendRewardsPanel
    public Button sendRewardsConfirmButton; // Reference to the SendButton in SendRewardsPanel
    public Button closeSendRewardsButton; // Reference to the CloseButton in SendRewardsPanel
    public TMP_Text FeedbackText;
    public GameObject FeedbackPanel;
    public TMP_Dropdown selectDropdown; // Reference to the dropdown for selecting options
    public TMP_Text statusText; // Reference to status text for progress updates
    public TMP_Text studentusernameText; // Reference to status text for progress updates


    private Queue<string> debugLines = new Queue<string>();
    private const int MAX_DEBUG_LINES = 10; // Maximum number of lines to show

   // public TMP_Text debugConsoleText; // Reference to a TextMeshPro text component for debug output
   // public GameObject debugConsolePanel; // Reference to the debug console panel
   // public Button debugToggleButton;     // Reference to a button to toggle the console

    public enum RewardType
    {
        OneStar,
        TwoStar,
        ThreeStar,
        FourStar,
        FiveStar,
        GoldenStar,
        PinkShirt,
        BlueShirt
    }

    private RewardType selectedReward; // Store the selected reward type
    private string selectedStudentName; // Store the selected student name
    private string selectedStudentFullName; // To store the full name of the selected student

    // Update the base URL to match other files
    private string baseUrl => NetworkConfig.BaseUrl;




    private bool isRemovingStudent = false; // Flag to prevent multiple removals
    private bool isUploadInProgress = false; // Flag to track upload state
    private bool isProcessing = false;

    public void OpenSendRewardsPanel(RewardType reward, string studentName)
    {
        selectedReward = reward;
        selectedStudentName = studentName;

        // Update the UI
        studentNameText.text = studentName; // Set the student name
        messageInputField.text = ""; // Clear the message input field
        UpdatePreviewRewardsBox(reward); // Update the reward image

        sendRewardsPanel.SetActive(true); // Open the SendRewardsPanel
    }

    public void CloseSendRewardsPanel()
    {
        sendRewardsPanel.SetActive(false);
    }

    private void UpdatePreviewRewardsBox(RewardType reward)
    {
        // Load the appropriate image for the selected reward
        string imagePath = $"Rewards/{reward.ToString()}"; // Assuming images are stored in a "Rewards" folder
        Sprite rewardSprite = Resources.Load<Sprite>(imagePath);

        if (rewardSprite != null)
        {
            previewRewardsBoxImg.sprite = rewardSprite; // Update the Image's Sprite
        }
        else
        {
            Debug.LogError($"Reward image not found: {imagePath}");
        }
    }

    public void SendRewards()
    {
        StartCoroutine(SendRewardCoroutine());
    }

    private IEnumerator SendRewardCoroutine()
    {
        if (string.IsNullOrEmpty(selectedStudentName))
        {
            Debug.LogError("[DynamicTable] No student selected for reward!");
            ShowFeedback("No student selected.");
            yield break;
        }

        string fullName = selectedStudentName;
        string reward = selectedReward.ToString();
        string message = messageInputField.text;

        // ✅ Correct JSON Serialization using Newtonsoft.Json
        var rewardData = new RewardData
        {
            fullName = fullName,
            reward = reward,
            message = message
        };

        string jsonData = Newtonsoft.Json.JsonConvert.SerializeObject(rewardData);

        Debug.Log($"[SendReward] JSON Payload: {jsonData}"); // ✅ Log the actual JSON


        using (UnityWebRequest request = new UnityWebRequest(NetworkConfig.BaseUrl + "/api/users/rewards", "POST"))



        {
            request.uploadHandler = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(jsonData));
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                Debug.Log("[DynamicTable] Reward sent successfully: " + request.downloadHandler.text);
                ShowFeedback("Reward sent successfully!");
            }
            else
            {
                Debug.LogError("[DynamicTable] Failed to send reward: " + request.error + " | Response: " + request.downloadHandler.text);
                ShowFeedback("Failed to send reward. Please try again.");
            }
        }
    }

    private IEnumerator VerifyDatabaseConnection()
    {
        if (database == null)
        {
            Debug.LogError("[DynamicTable] Database is null during verification");
            ShowFeedback("Database connection failed.");
            yield break;
        }

        var pingCommand = new BsonDocument("ping", 1);
        var pingTask = database.RunCommandAsync<BsonDocument>(pingCommand);
        
        // Move the yield outside of try-catch
        while (!pingTask.IsCompleted)
        {
            yield return null;
        }

        try
        {
            if (pingTask.Exception != null)
                throw pingTask.Exception;

            Debug.Log("[DynamicTable] MongoDB connection verified successfully");
            ShowFeedback("Database connected successfully.");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[DynamicTable] MongoDB verification failed: {ex.Message}");
            database = null;
            ShowFeedback("Database connection failed. Please check your connection.");
        }
    }

    private List<GameObject> rows = new List<GameObject>(); // Store all rows for searching
    private IMongoDatabase database; // MongoDB database instance

    private void Start()
    {
        try
        {
            Debug.Log("[DynamicTable] Starting initialization...");
            Application.logMessageReceived += HandleLog;

            // ❌ REMOVE this since we don't need MongoDB inside Unity
            // Debug.Log("[DynamicTable] Calling InitializeMongoDB...");
            // await InitializeMongoDB();  

            // ✅ Just proceed with the game logic
            Debug.Log("[DynamicTable] Assigning button listeners...");
            AssignButtonListeners();
            Debug.Log("[DynamicTable] Starting UI component initialization...");
            StartCoroutine(InitializeUIComponents());
        }
        catch (Exception ex)
        {
            Debug.LogError($"[DynamicTable] Critical error in Start: {ex.Message}\nStack trace: {ex.StackTrace}");
        }
    }

    private IEnumerator InitializeUIComponents()
    {
        yield return null; // Wait one frame for all components to be properly initialized

        // Setup UI components with proper layout
        SetupUIComponents();

        // Force layout rebuild
        if (scrollRect != null && scrollRect.content != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(scrollRect.content);
            Canvas.ForceUpdateCanvases();
        }

        // Continue with other initializations
        yield return StartCoroutine(SafeInitialization());
    }

    private bool SetupUIComponentsSafely()
    {
        try
        {
            if (tableParent == null || scrollRect == null)
            {
                Debug.LogError("Essential UI components are missing");
                return false;
            }

            SetupUIComponents();
            return true;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[DynamicTable] UI Setup error: {ex.Message}");
            return false;
        }
    }

    private IEnumerator SafeInitialization()
    {
        yield return new WaitForSeconds(0.5f); // Give time for UI to initialize

        try
        {
            RegisterEncoding();
            InitializeDropdowns();

            // Use coroutine for fetching data
            StartCoroutine(SafeFetchStudents());

            FetchSections();
         //   SetupDebugConsole();
            AssignButtonListeners();
        }
        catch (Exception ex)
        {
            Debug.LogError($"[DynamicTable] Initialization error: {ex.Message}");
        }
    }

    private IEnumerator SafeFetchStudents()
    {
        if (Application.internetReachability == NetworkReachability.NotReachable)
        {
            yield break;
        }

        yield return StartCoroutine(FetchStudentsCoroutine());
    }

    private void HandleLog(string logString, string stackTrace, LogType type)
    {
        if (type == LogType.Exception || type == LogType.Error)
        {
        }
    }

    private void OnDisable()
    {
        Application.logMessageReceived -= HandleLog;
    }

    private void SetupUIComponents()
    {
        if (tableParent != null)
        {
            // Basic VerticalLayoutGroup setup
            var vlg = tableParent.GetComponent<VerticalLayoutGroup>();
            if (vlg == null)
                vlg = tableParent.gameObject.AddComponent<VerticalLayoutGroup>();

            vlg.padding = new RectOffset(5, 5, 5, 5);
            vlg.spacing = 2;
            vlg.childAlignment = TextAnchor.UpperCenter;
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;

            // Basic ScrollRect setup
            if (scrollRect != null)
            {
                scrollRect.horizontal = false;
                scrollRect.vertical = true;
                scrollRect.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.Permanent;
                scrollRect.content = tableParent.GetComponent<RectTransform>();
            }

            // Basic ContentSizeFitter setup
            var contentSizeFitter = tableParent.GetComponent<ContentSizeFitter>();
            if (contentSizeFitter == null)
                contentSizeFitter = tableParent.gameObject.AddComponent<ContentSizeFitter>();

            contentSizeFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            contentSizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        }
    }

    private IEnumerator InitializeDataAndListeners()
    {
        yield return new WaitForSeconds(1);
        yield return StartCoroutine(FetchStudentsCoroutine());
        yield return new WaitForSeconds(0.5f);

        try
        {
            FetchSections();
            AssignButtonListeners();
        }
        catch (Exception ex)
        {
            Debug.LogError($"[DynamicTable] Error in InitializeDataAndListeners: {ex.Message}\nStackTrace: {ex.StackTrace}");
            ShowFeedback("Error loading data. Please check connection.");
        }
    }

    private void ClearExistingRows()
    {
        try
        {
            if (rows != null)
            {
                foreach (GameObject row in rows.Where(r => r != null))
                {
                    Destroy(row);
                }
                rows.Clear();
            }
            else
            {
                rows = new List<GameObject>();
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[DynamicTable] Error clearing rows: {ex.Message}");
        }
    }

    private void ValidateComponents()
    {
        if (searchButton == null) throw new NullReferenceException("Search Button is not assigned");
        if (addStudentButton == null) throw new NullReferenceException("Add Student Button is not assigned");
        if (closeCreateStudentButton == null) throw new NullReferenceException("Close Create Student Button is not assigned");
        if (createStudentButton == null) throw new NullReferenceException("Create Student Button is not assigned");
        if (uploadButton == null) throw new NullReferenceException("Upload Button is not assigned");
        if (sendRewardsButton == null) throw new NullReferenceException("Send Rewards Button is not assigned");
        if (closeRewardsPanelButton == null) throw new NullReferenceException("Close Rewards Panel Button is not assigned");
        if (sendRewardsConfirmButton == null) throw new NullReferenceException("Send Rewards Confirm Button is not assigned");
        if (closeSendRewardsButton == null) throw new NullReferenceException("Close Send Rewards Button is not assigned");
        if (removeStudentButton == null) throw new NullReferenceException("Remove Student Button is not assigned");
        if (refreshButton == null) throw new NullReferenceException("Refresh Button is not assigned");
        if (characterDropdown == null) throw new NullReferenceException("Character Dropdown is not assigned");
        if (sectionDropdown == null) throw new NullReferenceException("Section Dropdown is not assigned");
    }

    private void InitializeDropdowns()
    {
        // Initialize character dropdown with only Gir and Boy options
        if (characterDropdown != null)
        {
            characterDropdown.ClearOptions();
            var characterOptions = new List<string> {
                "Girl",
                "Boy"
            };
            characterDropdown.AddOptions(characterOptions);
        }

        // Fetch and initialize section dropdown
        FetchSections();
    }

    private class FileResult
    {
        public bool Success { get; set; }
        public string[] Lines { get; set; }
        public string Error { get; set; }
    }

    private async Task<FileResult> ReadAndroidFile(string filePath)
    {
        try
        {
            using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            using (var reader = new StreamReader(stream))
            {
                var content = await reader.ReadToEndAsync();
                return new FileResult 
                { 
                    Success = true, 
                    Lines = content.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries) 
                };
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[DynamicTable] Error reading file: {ex.Message}");
            return new FileResult { Success = false, Error = ex.Message };
        }
    }

    public void OnUploadButtonClick()
    {
        Debug.Log("[DynamicTable] Upload button clicked");

        if (isUploadInProgress || isProcessing)
        {
            Debug.LogWarning("[DynamicTable] Process already in progress. Please wait...");
            ShowFeedback("Please wait for current process to complete");
            return;
        }

        isUploadInProgress = true;
        isProcessing = true;
        uploadButton.interactable = false;

        try
        {
            #if UNITY_ANDROID
            if (!Permission.HasUserAuthorizedPermission(Permission.ExternalStorageRead))
            {
                Permission.RequestUserPermission(Permission.ExternalStorageRead);
                Permission.RequestUserPermission(Permission.ExternalStorageWrite);
                StartCoroutine(WaitForPermissionAndPickFile());
                return;
            }
            #endif

            PickFile(); // Changed from PickAndProcessFile() to PickFile()
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[DynamicTable] Error starting upload: {ex.Message}");
            ShowFeedback("Upload failed to start. Please try again.");
            ResetUploadState();
        }
    }

    private void ResetUploadState()
    {
        if (this == null) return;
        
        isUploadInProgress = false;
        isProcessing = false;
        if (uploadButton != null)
        {
            uploadButton.interactable = true;
        }
    }

    private IEnumerator WaitForPermissionAndPickFile()
    {
        yield return new WaitForSeconds(0.5f);
        
        if (Permission.HasUserAuthorizedPermission(Permission.ExternalStorageRead))
        {
            PickFile();
        }
        else
        {
            Debug.LogWarning("[DynamicTable] Storage permission not granted");
            ShowFeedback("Storage permission required to select files");
            ResetUploadState();
        }
    }

    private void PickFile()
    {
        try
        {
            var mimeTypes = new[] { "text/csv", "text/comma-separated-values", "text/plain" };

            NativeFilePicker.Permission permission = NativeFilePicker.PickFile(async (path) =>
            {
                if (string.IsNullOrEmpty(path))
                {
                    ShowFeedback("File selection cancelled");
                    ResetUploadState();
                    return;
                }

                Debug.Log($"[DynamicTable] Selected file path: {path}");
                if (path.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
                {
                    await UploadFileToServer(path);
                }
                else
                {
                    ShowFeedback("Please select a CSV file");
                    ResetUploadState();
                }
            }, mimeTypes);

            if (permission != NativeFilePicker.Permission.Granted)
            {
                ShowFeedback("File picker permission not granted");
                ResetUploadState();
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[DynamicTable] Error in file picker: {ex.Message}");
            ShowFeedback("Error selecting file. Please try again.");
            ResetUploadState();
        }
    }

    private async Task UploadFileToServer(string filePath)
    {
        try
        {
            ShowFeedback("Uploading file...");

            // Create form with file data
            WWWForm form = new WWWForm();
            byte[] fileData = File.ReadAllBytes(filePath);
            form.AddBinaryData("file", fileData, Path.GetFileName(filePath), "text/csv");

            // Add teacher's username to the form
            string teacherUsername = PlayerPrefs.GetString("Username");
            form.AddField("teacherUsername", teacherUsername);

            using (UnityWebRequest request = UnityWebRequest.Post($"{baseUrl}/upload", form))
            {
                request.certificateHandler = new NetworkUtility.BypassCertificateHandler();
                request.timeout = 30; // Increase timeout to 30 seconds
                
                Debug.Log($"[DynamicTable] Sending file: {Path.GetFileName(filePath)}");
                var operation = request.SendWebRequest();
                
                while (!operation.isDone) 
                {
                    ShowFeedback($"Uploading... {request.uploadProgress:P0}");
                    await Task.Yield();
                }

                if (request.result == UnityWebRequest.Result.Success)
                {
                    string responseText = request.downloadHandler.text;
                    Debug.Log($"[DynamicTable] Server response: {responseText}");
                    
                    var response = JsonUtility.FromJson<UploadResponse>(responseText);
                    ShowFeedback($"Successfully uploaded {response.count} students!");
                    await Task.Delay(1000);
                    FetchStudents();
                }
                else
                {
                    Debug.LogError($"[DynamicTable] Upload failed: {request.error}\nResponse: {request.downloadHandler.text}");
                    ShowFeedback("Failed to upload file. Please try again.");
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[DynamicTable] Error uploading file: {ex.Message}");
            ShowFeedback("Error uploading file. Please try again.");
        }
        finally
        {
            ResetUploadState();
        }
    }

    [Serializable]
    private class UploadResponse
    {
        public string message;
        public int count;
    }

    private async Task ProcessCSVFileAsync(string filePath)
    {
        try 
        {
            // Ensure we're not already processing
            if (!isProcessing)
            {
                Debug.LogError("[DynamicTable] Process state invalid");
                return;
            }

            ShowFeedback("Initializing database connection...");
            await EnsureDatabaseInitialized();
            
        

            ShowFeedback("Reading CSV file...");
            
            string content;
            using (var reader = new StreamReader(filePath))
            {
                content = await reader.ReadToEndAsync();
            }
            
            string[] lines = content.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            Debug.Log($"[DynamicTable] Read {lines.Length} lines from CSV");

            if (lines.Length <= 1)
            {
                ShowFeedback("CSV file is empty or contains only headers");
                return;
            }

            var collection = database.GetCollection<BsonDocument>("users");
            int successCount = 0;
            
            // Skip header row
            for (int i = 1; i < lines.Length; i++)
            {
                string[] values = lines[i].Split(',');
                if (values.Length >= 6)
                {
                    string firstName = values[0].Trim();
                    string lastName = values[1].Trim();
                    string role = values[2].Trim();
                    string section = values[3].Trim();
                    string username = values[4].Trim();
                    string character = values[5].Trim();

                    if (!string.IsNullOrEmpty(firstName) && !string.IsNullOrEmpty(lastName))
                    {
                        try
                        {
                            var studentData = new BsonDocument
                            {
                                { "FirstName", firstName },
                                { "LastName", lastName },
                                { "FullName", $"{firstName} {lastName}" },
                                { "Role", role },
                                { "Section", section },
                                { "Username", username },
                                { "Character", character },
                                { "rewards_collected", new BsonArray() }
                            };

                            await collection.InsertOneAsync(studentData);
                            successCount++;
                            Debug.Log($"[DynamicTable] Added student: {firstName} {lastName}");
                            
                            ShowFeedback($"Processing... ({successCount} added)");
                        }
                        catch (Exception ex)
                        {
                            Debug.LogError($"[DynamicTable] Error adding student {firstName} {lastName}: {ex.Message}");
                        }
                    }
                }
            }

            ShowFeedback($"Successfully added {successCount} students");
            await Task.Delay(1000); // Short delay before refresh
            FetchStudents(); // Refresh the list
        }
        catch (Exception ex)
        {
            Debug.LogError($"[DynamicTable] Error processing CSV: {ex.Message}");
            ShowFeedback($"Error processing CSV: {ex.Message}");
        }
        finally
        {
            ResetUploadState();
        }
    }

    private async Task EnsureDatabaseInitialized()
    {

        string connectionString = PlayerPrefs.GetString("MONGO_URI", "");
        if (string.IsNullOrEmpty(connectionString))
        {
            Debug.LogError("[DynamicTable] MongoDB URI not found in PlayerPrefs!");
            ShowFeedback("Database configuration missing. Please restart the application.");
            throw new System.Exception("MongoDB URI not configured");
        }

        int maxRetries = 3;
        int currentTry = 0;

        {
            try
            {
                Debug.Log($"[DynamicTable] Attempting database connection (attempt {currentTry + 1}/{maxRetries})");
                
                var settings = MongoClientSettings.FromUrl(new MongoUrl(connectionString));
                settings.ServerApi = new ServerApi(ServerApiVersion.V1);
                settings.ConnectTimeout = TimeSpan.FromSeconds(30);
                settings.ServerSelectionTimeout = TimeSpan.FromSeconds(30);
                settings.DirectConnection = true;  // Force direct connection

                Debug.Log($"[DynamicTable] Server settings - Host: {settings.Server?.Host}, Port: {settings.Server?.Port}");
                
                var client = new MongoClient(settings);
                database = client.GetDatabase("Users");

                // Test the connection
                var pingCommand = new BsonDocument("ping", 1);
                await database.RunCommandAsync<BsonDocument>(pingCommand);

                Debug.Log("[DynamicTable] Database initialized successfully");
                ShowFeedback("Connected to database");
                return;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[DynamicTable] Database initialization attempt {currentTry + 1} failed: {ex.Message}");
                if (ex.InnerException != null)
                {
                    Debug.LogError($"[DynamicTable] Inner exception: {ex.InnerException.Message}");
                }
                currentTry++;
                if (currentTry < maxRetries)
                {
                    ShowFeedback($"Retrying database connection... ({currentTry}/{maxRetries})");
                    await Task.Delay(1000);
                }
            }
        }

        throw new System.Exception("Failed to initialize database after multiple attempts");
    }

    private class FetchResult
    {
        public bool Success { get; set; }
        public string ErrorMessage { get; set; }
        public List<UserData> Students { get; set; }
    }

    private IEnumerator FetchStudentsCoroutine()
    {
        Debug.Log("[DynamicTable] Starting to fetch students...");

        if (Application.internetReachability == NetworkReachability.NotReachable)
        {
            ShowFeedback("No internet connection. Please check your connection and try again.");
            yield break;
        }

        // Get the logged-in teacher's username
        string teacherUsername = PlayerPrefs.GetString("Username");
        string url = $"{baseUrl}/users?teacherUsername={Uri.EscapeDataString(teacherUsername)}";
        
        UnityWebRequest request = UnityWebRequest.Get(url);
        request.certificateHandler = new NetworkUtility.BypassCertificateHandler();
        request.timeout = 10;

        yield return request.SendWebRequest();

        FetchResult result = ProcessFetchResult(request);
        
        if (!result.Success)
        {
            ShowFeedback(result.ErrorMessage);
            request.Dispose();
            yield break;
        }

        ClearExistingRows();

        if (result.Students != null && result.Students.Count > 0)
        {
            foreach (var user in result.Students)
            {
                if (user.Role == "Student" && 
                    !string.IsNullOrEmpty(user.FirstName) && 
                    !string.IsNullOrEmpty(user.LastName))
                {
                    string fullName = $"{user.FirstName} {user.LastName}";
                    AddRow(fullName, user.Section ?? "Unknown Section");
                    yield return null;
                }
            }
            ShowFeedback("Students loaded successfully");
        }
        else
        {
            ShowFeedback("No students found in the database");
        }

        request.Dispose();
        UpdateContentHeight();
        yield return StartCoroutine(DelayedScrollbarRefresh());
    }

    private FetchResult ProcessFetchResult(UnityWebRequest request)
    {
        if (request.result != UnityWebRequest.Result.Success)
        {
            return new FetchResult 
            { 
                Success = false, 
                ErrorMessage = $"Server request failed: {request.error}" 
            };
        }

        try
        {
            string responseContent = request.downloadHandler.text;
            var students = Newtonsoft.Json.JsonConvert.DeserializeObject<List<UserData>>(responseContent);
            return new FetchResult 
            { 
                Success = true, 
                Students = students 
            };
        }
        catch (Exception ex)
        {
            Debug.LogError($"[DynamicTable] Error processing response: {ex.Message}");
            return new FetchResult 
            { 
                Success = false, 
                ErrorMessage = "Error processing server response" 
            };
        }
    }

    private class RequestResult
    {
        public bool Success { get; set; }
        public string ErrorMessage { get; set; }
        public UnityWebRequest Request { get; set; }
        public string ResponseData { get; set; }
    }

    private RequestResult CreateRequest(string url)
    {
        try
        {
            var request = UnityWebRequest.Get(url);
            request.certificateHandler = new NetworkUtility.BypassCertificateHandler();
            request.timeout = 10;
            Debug.Log($"[DynamicTable] Sending request to: {url}");
            return new RequestResult { Success = true, Request = request };
        }
        catch (Exception ex)
        {
            Debug.LogError($"[DynamicTable] Error creating request: {ex.Message}");
            return new RequestResult { Success = false, ErrorMessage = "Error creating request" };
        }
    }

    private RequestResult ProcessResponse(UnityWebRequest request)
    {
        try
        {
            if (request.result == UnityWebRequest.Result.Success)
            {
                return new RequestResult 
                { 
                    Success = true, 
                    ResponseData = request.downloadHandler.text 
                };
            }
            
            Debug.LogError($"[DynamicTable] Request failed: {request.error}");
            return new RequestResult 
            { 
                Success = false, 
                ErrorMessage = $"Server request failed: {request.error}" 
            };
        }
        catch (Exception ex)
        {
            Debug.LogError($"[DynamicTable] Error processing response: {ex.Message}");
            return new RequestResult 
            { 
                Success = false, 
                ErrorMessage = "Error processing server response" 
            };
        }
    }

    private class TaskWrapper<T>
    {
        public bool Success { get; set; }
        public T Result { get; set; }
        public string ErrorMessage { get; set; }
    }

    private TaskWrapper<T> RunTask<T>(Task<T> task)  // Fixed: Removed the 's' before 'private'
    {
        try
        {
            task.Wait();
            return new TaskWrapper<T> { Success = true, Result = task.Result };
        }
        catch (Exception ex)
        {
            return new TaskWrapper<T> { Success = false, ErrorMessage = ex.Message };
        }
    }

    private IEnumerator SetCharacterForRow(string studentName, TMP_Text characterText)
    {
        if (characterText == null) yield break;

        var task = GetStudentCharacter(studentName);
        while (!task.IsCompleted)
            yield return null;

        var result = RunTask(task);
        
        if (result.Success && result.Result != null)
        {
            characterText.text = result.Result;
        }
    }

    private IEnumerator SetCharacterText(string studentName, TMP_Text characterText)
    {
        if (characterText == null) yield break;

        var task = GetStudentCharacter(studentName);
        while (!task.IsCompleted)
            yield return null;

        var result = RunTask(task);
        
        if (result.Success && result.Result != null)
        {
            characterText.text = result.Result;
        }
    }

    private async Task<List<UserData>> DeserializeUsersAsync(string responseContent)
    {
        return await Task.Run(() =>
        {
            return Newtonsoft.Json.JsonConvert.DeserializeObject<List<UserData>>(responseContent);
        });
    }

    private IEnumerator DisplayStudentData(string responseContent)
    {
        if (string.IsNullOrEmpty(responseContent))
            yield break;

        ClearExistingRows();
        Debug.Log($"[DynamicTable] Raw response: {responseContent}");

        var deserializeTask = DeserializeUsersAsync(responseContent);
        while (!deserializeTask.IsCompleted)
            yield return null;

        var result = RunTask(deserializeTask);

        if (result.Success && result.Result != null)
        {
            var users = result.Result;
            foreach (var user in users)
            {
                if (user.Role == "Student" && 
                    !string.IsNullOrEmpty(user.FirstName) && 
                    !string.IsNullOrEmpty(user.LastName))
                {
                    string fullName = $"{user.FirstName} {user.LastName}";
                    AddRow(fullName, user.Section ?? "Unknown Section");
                    yield return null;
                }
            }
            ShowFeedback("Students loaded successfully");
        }
        else
        {
            ShowFeedback("Error loading students: " + (result.ErrorMessage ?? "Unknown error"));
        }
    }

    private async Task ReadAndUploadExcelFile(string filePath)
    {
        try
        {
            Debug.Log($"[DynamicTable] Attempting to read file from: {filePath}");

            // Check if the file exists
            if (!File.Exists(filePath))
            {
                Debug.LogError($"[DynamicTable] File does not exist at path: {filePath}");
                return;
            }

            // Register the encoding provider
            System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);

            using (var stream = File.Open(filePath, FileMode.Open, FileAccess.Read))
            using (var reader = ExcelReaderFactory.CreateReader(stream))
            {
                // Skip the header row
                reader.Read();

                while (reader.Read())
                {
                    try
                    {
                        // Read values from the Excel file
                        string firstName = reader.GetString(0) ?? "";
                        string lastName = reader.GetString(1) ?? "";
                        string role = reader.GetString(2) ?? "Student"; // Default to "Student" if not provided
                        string section = reader.GetString(3) ?? "";
                        string username = reader.GetString(4) ?? "";
                        string character = reader.GetString(5) ?? "";

                        // Check for valid names before uploading
                        if (!string.IsNullOrEmpty(firstName) && !string.IsNullOrEmpty(lastName))
                        {
                            await UploadStudentToMongoDB(firstName, lastName, section, username, role, character);
                            Debug.Log($"[DynamicTable] Uploaded student: {firstName} {lastName} in section {section}");
                        }
                        else
                        {
                            Debug.LogWarning("[DynamicTable] Skipping row due to missing FirstName or LastName.");
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"[DynamicTable] Error processing row: {ex.Message}");
                    }
                }
            }
            Debug.Log("[DynamicTable] Excel file processed successfully");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[DynamicTable] Error reading Excel file: {ex.Message}\nStackTrace: {ex.StackTrace}");
            throw; // Rethrow the exception to be caught by the caller
        }
    }

    private async Task UploadStudentToMongoDB(string firstName, string lastName, string section, string username, string role = "Student", string character = "")
    {
        try
        {
            // Check if the database is initialized
            if (database == null)
            {
                Debug.LogError("[DynamicTable] Database is not initialized. Cannot upload student data.");
                ShowFeedback("Database connection failed. Please check your internet connection.");
                return; // Exit if the database is not initialized
            }

            // Log the values being uploaded
            Debug.Log($"[DynamicTable] Uploading student data: FirstName: {firstName}, LastName: {lastName}, Section: {section}, Username: {username}, Role: {role}, Character: {character}");

            var collection = database.GetCollection<BsonDocument>("users");

            var studentData = new BsonDocument
        {
            { "FirstName", firstName },
            { "LastName", lastName },
            { "FullName", $"{firstName} {lastName}" },
            { "Section", section },
            { "Username", username },
            { "Role", role },
            { "Character", character },
            { "rewards_collected", new BsonArray() }
        };

            await collection.InsertOneAsync(studentData);
            Debug.Log($"[DynamicTable] Student data uploaded: {username} ({firstName} {lastName})");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[DynamicTable] Error uploading student data: {ex.Message}");
            ShowFeedback("Error uploading student data. Please try again.");
        }
    }

    public void AddRow(string studentName, string sectionName)
    {
        if (string.IsNullOrEmpty(studentName) || string.IsNullOrEmpty(sectionName))
        {
            Debug.LogError("[DynamicTable] Invalid student or section name");
            return;
        }

        try
        {
            if (rowPrefab == null || tableParent == null)
            {
                Debug.LogError("[DynamicTable] Required components missing for AddRow");
                return;
            }

            GameObject newRow = Instantiate(rowPrefab, tableParent);
            if (newRow == null)
            {
                Debug.LogError("[DynamicTable] Failed to instantiate row");
                return;
            }

            var rowRect = newRow.GetComponent<RectTransform>();
            if (rowRect == null)
            {
                Debug.LogError("[DynamicTable] Row prefab missing RectTransform");
                Destroy(newRow);
                return;
            }

            // Setup row transform and components
            ConfigureRowTransform(rowRect);
            SetupRowComponents(newRow, studentName, sectionName);

            rows.Add(newRow);
            UpdateLayoutAndScroll();
        }
        catch (Exception ex)
        {
            Debug.LogError($"[DynamicTable] Error in AddRow: {ex.Message}");
        }
    }

    private void ConfigureRowTransform(RectTransform rowRect)
    {
        rowRect.anchorMin = new Vector2(0, 1);
        rowRect.anchorMax = new Vector2(1, 1);
        rowRect.pivot = new Vector2(0.5f, 1);
        rowRect.sizeDelta = new Vector2(0, 40);

        float yPosition = -rows.Count * (rowRect.sizeDelta.y + 2);
        rowRect.anchoredPosition = new Vector2(0, yPosition);
    }

    private void SetupRowComponents(GameObject row, string studentName, string sectionName)
    {
        var studentButton = row.transform.Find("StudentNameText")?.GetComponent<Button>();
        var studentText = studentButton?.GetComponent<TMP_Text>();
        var sectionText = row.transform.Find("SectionNameText")?.GetComponent<TMP_Text>();

        if (studentText == null || sectionText == null || studentButton == null)
        {
            throw new NullReferenceException("Missing required components in row prefab");
        }

        studentText.text = studentName;
        studentText.fontSize = 14;
        studentText.enableWordWrapping = false;

        sectionText.text = sectionName;
        sectionText.fontSize = 14;
        sectionText.enableWordWrapping = false;

        studentButton.onClick.AddListener(() => OnStudentClick(studentName, sectionName));
    }

    private void UpdateLayoutAndScroll()
    {
        try
        {
            if (tableParent != null)
            {
                // Recalculate positions for remaining rows
                for (int i = 0; i < rows.Count; i++)
                {
                    if (rows[i] != null)
                    {
                        var rowRect = rows[i].GetComponent<RectTransform>();
                        if (rowRect != null)
                        {
                            float yPosition = -i * (rowRect.sizeDelta.y + 2);
                            rowRect.anchoredPosition = new Vector2(0, yPosition);
                        }
                    }
                }

                // Force canvas and layout updates
                Canvas.ForceUpdateCanvases();
                LayoutRebuilder.ForceRebuildLayoutImmediate(tableParent.GetComponent<RectTransform>());
                StartCoroutine(DelayedScrollbarRefresh());
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[DynamicTable] Error in UpdateLayoutAndScroll: {ex.Message}");
        }
    }

    private async Task<string> GetStudentCharacter(string studentName)
    {
        try
        {
            var collection = database.GetCollection<BsonDocument>("users");
            var filter = Builders<BsonDocument>.Filter.Eq("FullName", studentName);
            var student = await collection.Find(filter).FirstOrDefaultAsync();

            if (student != null && student.Contains("Character"))
            {
                return student["Character"].AsString;
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[DynamicTable] Error getting student character: {ex.Message}");
        }
        return "";
    }

    private async Task<string> GetUsernameFromMongoDB(string studentName)
    {
        var collection = database.GetCollection<BsonDocument>("users");
        string[] nameParts = studentName.Split(' ');
        if (nameParts.Length < 2)
        {
            Debug.LogError("[DynamicTable] Invalid student name format");
            return null;
        }

        string firstName = nameParts[0].Trim();
        string lastName = string.Join(" ", nameParts.Skip(1)).Trim();

        Debug.Log($"[DynamicTable] Searching for - FirstName: {firstName}, LastName: {lastName}");

        var filter = Builders<BsonDocument>.Filter.And(
            Builders<BsonDocument>.Filter.Eq("FirstName", firstName),
            Builders<BsonDocument>.Filter.Eq("LastName", lastName)
        );

        var result = await collection.Find(filter).FirstOrDefaultAsync();

        if (result != null)
        {
            Debug.Log($"[DynamicTable] Found document with ID: {result["_id"]}");
            return result["Username"].AsString;
        }

        Debug.LogWarning($"[DynamicTable] No document found for student: {studentName}");
        return null;
    }

    public void OnStudentClick(string studentName, string sectionName)
    {
        Debug.Log($"[DynamicTable] Clicked on student: {studentName} with section: {sectionName}");

        try
        {
            viewStudentPanel.SetActive(true);

            // Get references to text components
            TMP_Text studentNameText = viewStudentPanel.transform.Find("StudentNameText")?.GetComponent<TMP_Text>();
            TMP_Text sectionText = viewStudentPanel.transform.Find("StudentSectionText")?.GetComponent<TMP_Text>();
            TMP_Text characterText = viewStudentPanel.transform.Find("StudentCharacterText")?.GetComponent<TMP_Text>();

            // Get the teacher's username
            string teacherUsername = PlayerPrefs.GetString("Username");
            string url = $"{baseUrl}/users?teacherUsername={Uri.EscapeDataString(teacherUsername)}";
            
            StartCoroutine(FetchStudentData(url, studentName, sectionName));
        }
        catch (Exception ex)
        {
            Debug.LogError($"[DynamicTable] Error in OnStudentClick: {ex.Message}");
            if (studentusernameText != null)
            {
                studentusernameText.text = "Error loading username";
            }
        }
    }

    private IEnumerator FetchStudentData(string url, string studentName, string sectionName)
    {
        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            request.certificateHandler = new NetworkUtility.BypassCertificateHandler();
            request.SetRequestHeader("Content-Type", "application/json");
            
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                string responseText = request.downloadHandler.text;
                Debug.Log($"[DynamicTable] Server response: {responseText}");
                
                try
                {
                    var students = JsonConvert.DeserializeObject<List<UserData>>(responseText);
                    var student = students?.FirstOrDefault(s => 
                        $"{s.FirstName} {s.LastName}".Equals(studentName, StringComparison.OrdinalIgnoreCase));
                    
                    if (student != null)
                    {
                        // Update username text
                        if (studentusernameText != null)
                        {
                            if (!string.IsNullOrEmpty(student.Username))
                            {
                                studentusernameText.text = student.Username;
                                Debug.Log($"[DynamicTable] Set username text to: {student.Username}");

                                // Update other UI elements
                                UpdateStudentPanelUI(studentName, sectionName, student);
                            }
                            else
                            {
                                studentusernameText.text = "No username found";
                                Debug.LogWarning("[DynamicTable] Username is null or empty in response");
                            }
                        }
                        else
                        {
                            Debug.LogError("[DynamicTable] studentusernameText reference is null");
                        }
                    }
                    else
                    {
                        Debug.LogError($"[DynamicTable] Student not found: {studentName}");
                        studentusernameText.text = "Student not found";
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[DynamicTable] Error parsing user data: {ex.Message}");
                    if (studentusernameText != null)
                    {
                        studentusernameText.text = "Error parsing data";
                    }
                }
            }
            else
            {
                Debug.LogError($"[DynamicTable] Request failed: {request.error}\nResponse: {request.downloadHandler.text}");
                if (studentusernameText != null)
                {
                    studentusernameText.text = "Error loading username";
                }
            }
        }
    }

    private void UpdateStudentPanelUI(string studentName, string sectionName, UserData userData)
    {
        var studentNameText = viewStudentPanel.transform.Find("StudentNameText")?.GetComponent<TMP_Text>();
        var sectionText = viewStudentPanel.transform.Find("StudentSectionText")?.GetComponent<TMP_Text>();
        var characterText = viewStudentPanel.transform.Find("StudentCharacterText")?.GetComponent<TMP_Text>();
      

        if (studentNameText != null)
        {
            studentNameText.text = studentName;
            selectedStudentFullName = studentName;
        }

        if (sectionText != null)
        {
            sectionText.text = sectionName;
        }

        if (characterText != null && !string.IsNullOrEmpty(userData?.Character))
        {
            characterText.text = userData.Character;
        }

        // After updating the UI, fetch the game progress using the username
        if (!string.IsNullOrEmpty(userData?.Username))
        {
            StartCoroutine(FetchGameProgress(userData.Username));
        }
    }

    private IEnumerator FetchGameProgress(string username)
    {
        Debug.Log($"[Progress] Fetching progress for username: {username}");
        string url = $"{baseUrl}/game_progress/{Uri.EscapeDataString(username)}";

        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            request.certificateHandler = new NetworkUtility.BypassCertificateHandler();
            request.SetRequestHeader("Content-Type", "application/json");
            
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                string responseText = request.downloadHandler.text;
                Debug.Log($"[Progress] Server response: {responseText}");
                
                try
                {
                    var progressData = JsonConvert.DeserializeObject<GameProgressData>(responseText);
                    DisplayProgressData(progressData);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[Progress] Error parsing progress data: {ex.Message}");
                    if (statusText != null)
                        statusText.text = "Error: Invalid progress data";
                }
            }
            else
            {
                Debug.LogError($"[Progress] Failed to fetch progress: {request.error}");
                if (statusText != null)
                    statusText.text = "Error: Could not fetch progress";
            }
        }
    }

    private void DisplayProgressData(GameProgressData progress)
    {
        if (statusText == null)
        {
            Debug.LogError("[DynamicTable] statusText reference is missing!");
            return;
        }

        if (progress == null)
        {
            statusText.text = "No progress data available";
            return;
        }

        StringBuilder status = new StringBuilder();

        // Display tutorial info
        if (progress.tutorial != null)
        {
            status.AppendLine($"Tutorial:");
            status.AppendLine($"• Status: {progress.tutorial.status}");
            if (!string.IsNullOrEmpty(progress.tutorial.reward))
            {
                status.AppendLine($"• Reward: {progress.tutorial.reward}");
            }
            status.AppendLine();
        }

        // Display unit info
        if (progress.units != null)
        {
            foreach (var unit in progress.units)
            {
                status.AppendLine($"Unit: {unit.Key}");
                status.AppendLine($"• Status: {unit.Value.status}");
                status.AppendLine($"• Completed Lessons: {unit.Value.completedLessons}");
                status.AppendLine($"• Unit Score: {unit.Value.unitScore}%");

                // Display lessons
                if (unit.Value.lessons != null)
                {
                    status.AppendLine("\nLessons:");
                    foreach (var lesson in unit.Value.lessons)
                    {
                        status.AppendLine($"  {lesson.Key}:");
                        status.AppendLine($"  • Status: {lesson.Value.status}");
                        if (!string.IsNullOrEmpty(lesson.Value.reward))
                        {
                            status.AppendLine($"  • Reward: {lesson.Value.reward}");
                        }
                        if (lesson.Value.score > 0)
                        {
                            status.AppendLine($"  • Score: {lesson.Value.score}%");
                        }
                    }
                }

                // Display post-test info
                if (unit.Value.postTest != null)
                {
                    status.AppendLine("\nPost Test:");
                    status.AppendLine($"• Status: {unit.Value.postTest.status}");
                    if (unit.Value.postTest.score > 0)
                    {
                        status.AppendLine($"• Score: {unit.Value.postTest.score}%");
                    }
                    if (!string.IsNullOrEmpty(unit.Value.postTest.reward))
                    {
                        status.AppendLine($"• Reward: {unit.Value.postTest.reward}");
                    }
                }
                
                status.AppendLine("\n-------------------\n");
            }
        }

        Debug.Log($"[Progress] Displaying progress data: {status}");
        statusText.text = status.ToString();
    }

    public void CloseViewStudentPanel()
    {
        viewStudentPanel.SetActive(false);
    }

    public void SearchStudents()
    {
        string searchTerm = searchInputField.text.ToLower();
        string selectedOption = selectDropdown.options[selectDropdown.value].text; // Get selected option

        bool foundResults = false; // Flag to check if any results are found

        foreach (GameObject row in rows)
        {
            TMP_Text studentText = row.transform.Find("StudentNameText")?.GetComponent<TMP_Text>();
            TMP_Text sectionText = row.transform.Find("SectionNameText")?.GetComponent<TMP_Text>();

            bool matchesSearch = false;

            // Check based on selected option
            if (selectedOption == "Name" && studentText != null)
            {
                matchesSearch = studentText.text.ToLower().Contains(searchTerm);
            }
            else if (selectedOption == "Section" && sectionText != null)
            {
                matchesSearch = sectionText.text.ToLower().Contains(searchTerm);
            }

            // Show or hide the row based on the search result
            row.SetActive(matchesSearch);
            if (matchesSearch)
            {
                foundResults = true; // At least one result found
            }
        }

        // Show feedback if no results found
        if (!foundResults)
        {
            ShowFeedback($"No results for '{searchTerm}'");
        }
        else
        {
            ShowFeedback(""); // Clear feedback if results are found
        }
    }

    public void ShowViewRewardsPanel()
    {
        viewRewardsPanel.SetActive(true);
    }

    public void CloseViewRewardsPanel()
    {
        viewRewardsPanel.SetActive(false);
    }


    public void ShowCreateStudentPanel()
    {
        createStudentPanel.SetActive(true);
    }

    public void CloseCreateStudentPanel()
    {
        createStudentPanel.SetActive(false);
    }

    public void OnRewardButtonClick(int rewardType)
    {
        // Get the selected student's name from the ViewStudentPanel
        string studentName = viewStudentPanel.transform.Find("StudentNameText").GetComponent<TMP_Text>().text;

        if (!string.IsNullOrEmpty(studentName))
        {
            OpenSendRewardsPanel((RewardType)rewardType, studentName);
            CloseViewRewardsPanel(); // Close the ViewRewardsPanel when a reward is selected

            // Hide the FeedbackPanel when a reward is selected
            if (FeedbackPanel != null)
            {
                FeedbackPanel.SetActive(false);
            }
        }
        else
        {
            Debug.LogError("[DynamicTable] No student selected!");
        }
    }

    public async void CreateStudent()
    {
        try
        {
            if (string.IsNullOrEmpty(firstNameInputField.text) || 
                string.IsNullOrEmpty(lastNameInputField.text) || 
                string.IsNullOrEmpty(usernameInputField.text))
            {
                ShowFeedback("Please fill in all required fields");
                return;
            }

            string firstName = firstNameInputField.text.Trim();
            string lastName = lastNameInputField.text.Trim();
            string username = usernameInputField.text.Trim(); // Use the actual username input
            string section = sectionDropdown.options[sectionDropdown.value].text;
            string character = characterDropdown.options[characterDropdown.value].text;

            // Get current teacher's username from PlayerPrefs
            string teacherUsername = PlayerPrefs.GetString("Username");

            var userData = new Dictionary<string, string>
            {
                { "FirstName", firstName },
                { "LastName", lastName },
                { "Username", username },
                { "Section", section },
                { "Character", character },
                { "CreatedBy", teacherUsername }, // Add teacher's username
                { "Role", "Student" },
                { "Password", "defaultPassword" }
            };

            Debug.Log($"[DynamicTable] Sending user data: {JsonConvert.SerializeObject(userData)}");

            string jsonData = JsonConvert.SerializeObject(userData);
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);

            using (UnityWebRequest request = new UnityWebRequest($"{baseUrl}/users", "POST"))
            {
                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");
                request.certificateHandler = new NetworkUtility.BypassCertificateHandler();

                await request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    ShowFeedback("Student created successfully!");
                    firstNameInputField.text = "";
                    lastNameInputField.text = "";
                    usernameInputField.text = "";
                    CloseCreateStudentPanel();
                    FetchStudents();
                    FetchSections();
                }
                else
                {
                    Debug.LogError($"[DynamicTable] Failed to create student: {request.downloadHandler.text}");
                    ShowFeedback("Failed to create student. Please try again.");
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[DynamicTable] Error creating student: {ex.Message}");
            ShowFeedback("Error creating student. Please try again.");
        }
    }

    public async void FetchStudents()
    {
        try
        {
            string teacherUsername = PlayerPrefs.GetString("Username");
            string url = $"{baseUrl}/users?teacherUsername={Uri.EscapeDataString(teacherUsername)}";

            using (UnityWebRequest request = UnityWebRequest.Get(url))
            {
                request.certificateHandler = new NetworkUtility.BypassCertificateHandler();
                await request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    string responseContent = request.downloadHandler.text;

                    // Clear existing rows first
                    foreach (GameObject row in rows)
                    {
                        Destroy(row);
                    }
                    rows.Clear();

                    // Deserialize the JSON response
                    var students = JsonUtility.FromJson<StudentListResponse>($"{{\"users\":{responseContent}}}");
                    if (students != null && students.users != null)
                    {
                        foreach (var student in students.users)
                        {
                            if (student.Role == "Student")
                            {
                                string fullName = $"{student.FirstName} {student.LastName}";
                                AddRow(fullName, student.Section);
                            }
                        }
                    }

                }

            }
        }
        catch (Exception ex)

        {
            Debug.LogError($"[DynamicTable] Error in FetchStudents: {ex.Message}");


        }

    }

    private IEnumerator UpdateUI(List<BsonDocument> students)
    {
        // Clear existing rows first
        foreach (GameObject row in rows)
        {
            Destroy(row);
        }
        rows.Clear();

        // Process students one by one with error handling for each
        foreach (var student in students)
        {
            yield return null; // Give time between each student processing

            ProcessStudent(student);
        }

    }

    private void ProcessStudent(BsonDocument student)
    {
        try
        {
            string firstName = student.GetValue("FirstName").AsString;
            string lastName = student.GetValue("LastName").AsString;
            string section = student.GetValue("Section").AsString;

            AddRow($"{firstName} {lastName}", section);
        }
        catch (Exception ex)
        {
            Debug.LogError($"[DynamicTable] Error processing student: {ex.Message}");

        }


    }


private string[] compoundLastNamePrefixes = new string[] { "De", "Del", "Dela", "De La", "San", "Santa", "Santo" };

private (string firstName, string lastName) SplitFullName(string fullName)
{
    // Handle empty or null cases
    if (string.IsNullOrWhiteSpace(fullName))
        return (string.Empty, string.Empty);

    // Split the full name into parts
    string[] parts = fullName.Split(' ');
    
    if (parts.Length < 2)
        return (fullName, string.Empty);

    // Start by assuming the last word is the last name
    int lastNameStartIndex = parts.Length - 1;

    // Look for compound last names by checking backwards
    for (int i = parts.Length - 2; i >= 0; i--)
    {
        string possiblePrefix = parts[i];
        
        // Check if this part is a known prefix for compound last names
        if (compoundLastNamePrefixes.Contains(possiblePrefix, StringComparer.OrdinalIgnoreCase))
        {
            lastNameStartIndex = i;
            break;
        }
    }


    

    // Combine the parts into first name and last name
    string firstName = string.Join(" ", parts.Take(lastNameStartIndex));
    string lastName = string.Join(" ", parts.Skip(lastNameStartIndex));

    return (firstName.Trim(), lastName.Trim());
}

private async Task<(string firstName, string lastName)> GetStudentNameFromDatabase(string fullName)
{
    try
    {
        if (database == null)
        {
            Debug.LogError("[DynamicTable] Database is not initialized.");
            return (null, null);
        }

        var collection = database.GetCollection<BsonDocument>("users");

        // Create a filter to find the student by FullName
        var filter = Builders<BsonDocument>.Filter.Eq("FullName", fullName);

        // Fetch the student document
        var student = await collection.Find(filter).FirstOrDefaultAsync();

        if (student != null)
        {
            // Extract firstName and lastName from the document
            string firstName = student.GetValue("FirstName").AsString;
            string lastName = student.GetValue("LastName").AsString;

            return (firstName, lastName);
        }
        else
        {
            Debug.LogError($"[DynamicTable] Student not found in database: {fullName}");
            return (null, null); // Return null if the student is not found
        }
    }
    catch (Exception ex)
    {
        Debug.LogError($"[DynamicTable] Error fetching student name from database: {ex.Message}");
        return (null, null); // Return null in case of an error
    }
}


public async void OnRemoveStudentButtonClick()
{
    if (isRemovingStudent) return;
    isRemovingStudent = true;

    string fullName = selectedStudentFullName;
    
    Debug.Log($"[DynamicTable] Attempting to remove student: '{fullName}'");
    
    if (string.IsNullOrWhiteSpace(fullName))
    {
        ShowFeedback("Cannot remove student: name is empty.");
        isRemovingStudent = false;
        return;
    }

    try
    {
        // Use the full name directly in the URL
        string url = $"{baseUrl}/users/remove?fullname={Uri.EscapeDataString(fullName)}";
        
        using (UnityWebRequest request = UnityWebRequest.Delete(url))
        {
            request.certificateHandler = new NetworkUtility.BypassCertificateHandler();
            
            var operation = request.SendWebRequest();
            while (!operation.isDone) await Task.Yield();

            if (request.result == UnityWebRequest.Result.Success)
            {
                var rowToRemove = rows.FirstOrDefault(r => 
                    r.transform.Find("StudentNameText")?.GetComponent<TMP_Text>()?.text.Equals(fullName, StringComparison.OrdinalIgnoreCase) ?? false);
                
                if (rowToRemove != null)
                {
                    rows.Remove(rowToRemove);
                    Destroy(rowToRemove);
                    UpdateLayoutAndScroll();
                }

                CloseViewStudentPanel();
                FetchStudents();
                ShowFeedback($"Successfully removed {fullName}");
            }
            else
            {
                Debug.LogError($"[DynamicTable] Remove request failed: {request.error}");
                ShowFeedback("Failed to remove student. Please try again.");
            }
        }
    }
    catch (Exception ex)
    {
        Debug.LogError($"[DynamicTable] Error removing student: {ex.Message}");
        ShowFeedback("Error removing student.");
    }
    finally
    {
        isRemovingStudent = false;
    }
}


    // Add new async versions of FetchSections and FetchStudents
    private async Task FetchSectionsAsync()
    {
        await Task.Run(() => FetchSections());
    }



    private async Task FetchStudentsAsync()
    {
        await Task.Run(() => FetchStudents());
    }

    private Task<bool> ShowConfirmationDialog(string message)
    {
        var tcs = new TaskCompletionSource<bool>();

        // Create confirmation UI elements if they don't exist
        GameObject confirmationPanel = new GameObject("ConfirmationPanel");
        confirmationPanel.AddComponent<RectTransform>();

        // Add your confirmation dialog UI logic here
        // For now, we'll just return true
        tcs.SetResult(true);

        return tcs.Task;
    }

    public async void FetchSections()
    {
        try
        {
            string url = $"{baseUrl}/sections";
            using (UnityWebRequest request = UnityWebRequest.Get(url))
            {
                request.certificateHandler = new NetworkUtility.BypassCertificateHandler();
                request.SetRequestHeader("Content-Type", "application/json");
                
                await request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    string responseText = request.downloadHandler.text;
                    Debug.Log($"[DynamicTable] Raw sections response: {responseText}"); // Debug log

                    // Parse sections directly from JSON array
                    var sections = JsonConvert.DeserializeObject<List<string>>(responseText);
                    
                    if (sections != null && sections.Count > 0)
                    {
                        sections.Sort(); // Sort sections alphabetically
                        sectionDropdown.ClearOptions();
                        var options = new List<string>();
                        
                        // Add default option
                  //      options.Add("Select Section");
                        // Add all sections
                        options.AddRange(sections);
                        
                        sectionDropdown.AddOptions(options);
                        Debug.Log($"[DynamicTable] Loaded {sections.Count} sections");
                        
                        // Select "Select Section" by default
                        sectionDropdown.value = 0;
                        sectionDropdown.RefreshShownValue();
                    }
                    else
                    {
                        sectionDropdown.ClearOptions();
                        sectionDropdown.AddOptions(new List<string> { "No sections available" });
                    }
                }
                else
                {
                    Debug.LogError($"[DynamicTable] Failed to fetch sections: {request.error}");
                    ShowFeedback("Failed to load sections");
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[DynamicTable] Error fetching sections: {ex.Message}");
            ShowFeedback("Error loading sections");
        }
    }

    public void OnRefreshButtonClick()
    {
        FetchStudents(); // Direct call since FetchStudents is now async void
    }

    private void AssignButtonListeners()
    {
        // Remove existing listeners to prevent multiple calls
        searchButton.onClick.RemoveAllListeners();
        searchButton.onClick.AddListener(SearchStudents);

        addStudentButton.onClick.RemoveAllListeners();
        addStudentButton.onClick.AddListener(ShowCreateStudentPanel);

        closeCreateStudentButton.onClick.RemoveAllListeners();
        closeCreateStudentButton.onClick.AddListener(CloseCreateStudentPanel);

        createStudentButton.onClick.RemoveAllListeners();
        createStudentButton.onClick.AddListener(CreateStudent);

        uploadButton.onClick.RemoveAllListeners();
        uploadButton.onClick.AddListener(OnUploadButtonClick);

        // Assign listeners for rewards panel
        sendRewardsButton.onClick.RemoveAllListeners();
        sendRewardsButton.onClick.AddListener(() => ShowFeedback("Sending rewards..."));

        closeRewardsPanelButton.onClick.RemoveAllListeners();
        closeRewardsPanelButton.onClick.AddListener(CloseViewRewardsPanel);

        removeStudentButton.onClick.RemoveAllListeners();
        removeStudentButton.onClick.AddListener(OnRemoveStudentButtonClick);

        refreshButton.onClick.RemoveAllListeners();
        refreshButton.onClick.AddListener(OnRefreshButtonClick);
    }

    private void ShowFeedback(string message)
    {
        if (FeedbackPanel == null)
        {
            Debug.LogError("[DynamicTable] FeedbackPanel reference is missing!");
            return;
        }

        if (FeedbackText == null)
        {
            Debug.LogError("[DynamicTable] FeedbackText reference is missing!");
            return;
        }

        try
        {
            // Make sure the panel is active before setting text
            if (!FeedbackPanel.activeInHierarchy)
            {
                FeedbackPanel.SetActive(true);
            }

            FeedbackText.text = message;

            // Schedule panel to close after 3 seconds
            StartCoroutine(AutoCloseFeedback(3f));
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[DynamicTable] Error showing feedback: {ex.Message}");
        }
    }

    private IEnumerator AutoCloseFeedback(float delay)
    {
        yield return new WaitForSeconds(delay);
        
        if (FeedbackPanel != null && FeedbackPanel.activeInHierarchy)
        {
            FeedbackPanel.SetActive(false);
        }
    }

    private void RegisterEncoding()
    {
        if (!encodingRegistered)
        {
            System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
            encodingRegistered = true;
        }
    }

    private void UpdateContentHeight()
    {
        if (scrollRect != null && scrollRect.content != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(scrollRect.content);
            Canvas.ForceUpdateCanvases();
        }
    }

    private IEnumerator DelayedScrollbarRefresh()
    {
        yield return null; // Wait one frame
        if (scrollRect != null)
        {
            scrollRect.verticalScrollbar.value = 1; // Reset scrollbar to top
        }
    }
}

public static class UnityWebRequestExtensions
{
    public static TaskAwaiter<UnityWebRequest> GetAwaiter(this UnityWebRequestAsyncOperation operation)
    {
        var tcs = new TaskCompletionSource<UnityWebRequest>();
        operation.completed += asyncOp => tcs.TrySetResult(((UnityWebRequestAsyncOperation)asyncOp).webRequest);
        return tcs.Task.GetAwaiter();
    }
}

[System.Serializable]
public class StudentListResponse
{
    public UserData[] users;
}