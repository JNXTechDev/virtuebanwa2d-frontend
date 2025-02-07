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

    public TMP_Text debugConsoleText; // Reference to a TextMeshPro text component for debug output
    private Queue<string> debugLines = new Queue<string>();
    private const int MAX_DEBUG_LINES = 10; // Maximum number of lines to show

    public GameObject debugConsolePanel; // Reference to the debug console panel
    public Button debugToggleButton;     // Reference to a button to toggle the console

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
    private const string baseUrl = "https://vbdb.onrender.com/api";

    private bool isRemovingStudent = false; // Flag to prevent multiple removals
    private bool isUploadInProgress = false; // Flag to track upload state

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

    public async void SendRewards()
    {
        try
        {
            // Check if the database is initialized
            if (!isDatabaseInitialized)
            {
                ShowFeedback("Database is not initialized. Please try again later.");
                return; // Exit if the database is not initialized
            }

            var collection = database.GetCollection<BsonDocument>("users");

            // Fetch the student's full details from the database using FullName
            var filter = Builders<BsonDocument>.Filter.Eq("FullName", selectedStudentName);

            Debug.Log($"Filter used: {filter}");

            // Check if the student exists in the database
            var student = await collection.Find(filter).FirstOrDefaultAsync();
            if (student == null)
            {
                Debug.LogError($"Student not found in database: {selectedStudentName}");
                ShowFeedback("Student not found in the database.");
                return;
            }

            // Extract FirstName and LastName from the database document
            string firstName = student.GetValue("FirstName").AsString;
            string lastName = student.GetValue("LastName").AsString;

            Debug.Log($"Found student - FirstName: {firstName}, LastName: {lastName}");

            // Create a reward document to save in the database with Philippine time
            DateTime utcNow = DateTime.UtcNow;
            DateTime philippineTime = utcNow.AddHours(8);
            string philippineTimeString = philippineTime.ToString("yyyy-MM-dd hh:mm:ss tt");

            var rewardDocument = new BsonDocument
            {
                { "reward", selectedReward.ToString() },
                { "message", messageInputField.text },
                { "date", philippineTimeString } // Use human-readable Philippine Time
            };

            // Update the student's document in the database
            var update = Builders<BsonDocument>.Update.Push("rewards_collected", rewardDocument);
            await collection.UpdateOneAsync(filter, update);

            Debug.Log($"Reward sent to {selectedStudentName}");

            // Show feedback message
            ShowFeedback("Reward sent successfully!");

            // Close the SendRewardsPanel and other panels after 3 seconds
            StartCoroutine(ClosePanelsAfterDelay(3f));
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error sending rewards: {ex.Message}");
            ShowFeedback("Error sending reward. Please try again.");
        }
    }

    private void ShowFeedback(string message)
    {
        if (FeedbackPanel != null && FeedbackText != null)
        {
            FeedbackText.text = message; // Set the feedback message
            FeedbackPanel.SetActive(true); // Activate the FeedbackPanel
        }
        else
        {
            Debug.LogError("FeedbackPanel or FeedbackText reference is missing!");
        }
    }

    private IEnumerator ClosePanelsAfterDelay(float delay)
    {
        // Wait for the specified delay
        yield return new WaitForSeconds(delay);

        // Close the SendRewardsPanel and other panels
        CloseSendRewardsPanel();
        CloseViewRewardsPanel(); // Close the rewards panel if needed

        // Hide the FeedbackPanel after closing panels
        if (FeedbackPanel != null)
        {
            FeedbackPanel.SetActive(false);
        }
    }

    private List<GameObject> rows = new List<GameObject>(); // Store all rows for searching
    private IMongoDatabase database; // MongoDB database instance

    private void Start()
    {
        try
        {
            Application.logMessageReceived += HandleLog;

            // Assign button listeners
            AssignButtonListeners();

            // Ensure proper initialization order
            StartCoroutine(InitializeUIComponents());
        }
        catch (Exception ex)
        {
            Debug.LogError($"Critical error in Start: {ex.Message}\n{ex.StackTrace}");
            LogToScreen("Failed to initialize. Please restart the application.");
        }
    }

    private bool isDatabaseInitialized = false;

    private void InitializeMongoDB()
    {
        try
        {
            var connectionString = "mongodb+srv://vbdb:abcdefghij@cluster0.8i1sn.mongodb.net/Users?retryWrites=true&w=majority";
            var settings = MongoClientSettings.FromConnectionString(connectionString);
            settings.ServerApi = new ServerApi(ServerApiVersion.V1);
            settings.ServerSelectionTimeout = TimeSpan.FromSeconds(10);
            settings.ConnectTimeout = TimeSpan.FromSeconds(15);
            settings.SocketTimeout = TimeSpan.FromSeconds(15);

            var client = new MongoClient(settings);
            database = client.GetDatabase("Users");

            // Verify connection
            var pingTask = database.RunCommandAsync((Command<BsonDocument>)"{ping:1}");
            pingTask.Wait(5000); // Wait up to 5 seconds for ping

            Debug.Log("Attempting to connect to MongoDB...");
            Debug.Log("MongoDB initialization successful");
            isDatabaseInitialized = true; // Set the flag to true
            ShowFeedback("Database connected successfully.");
        }
        catch (MongoConnectionException mongoEx)
        {
            Debug.LogError($"MongoDB connection error: {mongoEx.Message}");
            isDatabaseInitialized = false; // Set the flag to false
            ShowFeedback("MongoDB connection error. Please check your connection settings.");
        }
        catch (Exception ex)
        {
            Debug.LogError($"MongoDB initialization failed: {ex.Message}");
            database = null; // Ensure database is null if initialization fails
            isDatabaseInitialized = false; // Set the flag to false
            ShowFeedback("Database connection failed. Please check your internet connection.");
        }
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
            Debug.LogError($"UI Setup error: {ex.Message}");
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
            SetupDebugConsole();
            AssignButtonListeners();
        }
        catch (Exception ex)
        {
            Debug.LogError($"Initialization error: {ex.Message}");
            LogToScreen("Error during initialization");
        }
    }

    private IEnumerator SafeFetchStudents()
    {
        if (Application.internetReachability == NetworkReachability.NotReachable)
        {
            LogToScreen("No internet connection");
            yield break;
        }

        yield return StartCoroutine(FetchStudentsCoroutine());
    }

    private void HandleLog(string logString, string stackTrace, LogType type)
    {
        if (type == LogType.Exception || type == LogType.Error)
        {
            LogToScreen($"Error: {logString}");
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
            Debug.LogError($"Error in InitializeDataAndListeners: {ex.Message}\nStackTrace: {ex.StackTrace}");
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
            Debug.LogError($"Error clearing rows: {ex.Message}");
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
        // Initialize character dropdown with default options if needed
        if (characterDropdown != null && characterDropdown.options.Count == 0)
        {
            characterDropdown.ClearOptions();
            var characterOptions = new List<string> {
                "Character1",
                "Character2",
                "Character3",
                // Add more character options as needed
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
            Debug.LogError($"Error reading file: {ex.Message}");
            return new FileResult { Success = false, Error = ex.Message };
        }
    }

    public void OnUploadButtonClick()
    {
        Debug.Log("Upload button clicked");

        if (isUploadInProgress)
        {
            Debug.LogWarning("Upload is already in progress. Ignoring this click.");
            return;
        }

        uploadButton.interactable = false;
        isUploadInProgress = true;

        #if UNITY_ANDROID
        if (!Permission.HasUserAuthorizedPermission(Permission.ExternalStorageRead))
        {
            Permission.RequestUserPermission(Permission.ExternalStorageRead);
            Permission.RequestUserPermission(Permission.ExternalStorageWrite);
            StartCoroutine(WaitForPermissionAndPickFile());
            return;
        }
        #endif

        PickAndProcessFile();
    }

    private IEnumerator WaitForPermissionAndPickFile()
    {
        yield return new WaitForSeconds(0.5f);
        PickAndProcessFile();
    }

    private void PickAndProcessFile()
    {
        try
        {
            string[] allowedExtensions = new[] { "csv", "txt" }; // Added txt as fallback
            var mimeTypes = new[] { "text/csv", "text/comma-separated-values", "text/plain" };

            #if UNITY_ANDROID
            // For Android, we need to explicitly check and request permissions
            if (!Permission.HasUserAuthorizedPermission(Permission.ExternalStorageRead))
            {
                Permission.RequestUserPermission(Permission.ExternalStorageRead);
                Permission.RequestUserPermission(Permission.ExternalStorageWrite);
                ShowFeedback("Please grant storage permissions and try again");
                uploadButton.interactable = true;
                isUploadInProgress = false;
                return;
            }
            #endif

            NativeFilePicker.Permission permission = NativeFilePicker.PickFile((path) =>
            {
                if (!string.IsNullOrEmpty(path))
                {
                    Debug.Log($"Selected file path: {path}");
                    if (path.EndsWith(".csv", StringComparison.OrdinalIgnoreCase) || 
                        path.EndsWith(".txt", StringComparison.OrdinalIgnoreCase))
                    {
                        ProcessCSVFileAsync(path);
                    }
                    else
                    {
                        ShowFeedback("Please select a CSV file");
                        uploadButton.interactable = true;
                        isUploadInProgress = false;
                    }
                }
                else
                {
                    Debug.Log("File selection cancelled");
                    ShowFeedback("File selection cancelled");
                    uploadButton.interactable = true;
                    isUploadInProgress = false;
                }
            }, mimeTypes);

            Debug.Log($"File picker permission result: {permission}");
            
            if (permission != NativeFilePicker.Permission.Granted)
            {
                ShowFeedback("Permission not granted. Please check app settings.");
                uploadButton.interactable = true;
                isUploadInProgress = false;
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error in file picker: {ex.Message}");
            ShowFeedback("Error selecting file. Please try again.");
            uploadButton.interactable = true;
            isUploadInProgress = false;
        }
    }

// Fix for ProcessCSVFile coroutine
private async void ProcessCSVFileAsync(string filePath)
{
    Debug.Log("Starting to process CSV file");
    
    // Initialize MongoDB if not already initialized
    if (!isDatabaseInitialized)
    {
        InitializeMongoDB();
        await Task.Delay(1000); // Wait for initialization
        
        if (!isDatabaseInitialized)
        {
            ShowFeedback("Failed to connect to database. Please check your connection.");
            uploadButton.interactable = true;
            isUploadInProgress = false;
            return;
        }
    }

    ShowFeedback("Reading CSV file...");
    
    try
    {
        string content;
        using (var reader = new StreamReader(filePath))
        {
            content = await reader.ReadToEndAsync();
        }
        
        string[] lines = content.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        Debug.Log($"Read {lines.Length} lines from CSV");

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
                        Debug.Log($"Added student: {firstName} {lastName}");
                        
                        ShowFeedback($"Processing... ({successCount} added)");
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"Error adding student {firstName} {lastName}: {ex.Message}");
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
        Debug.LogError($"Error processing CSV: {ex.Message}");
        ShowFeedback($"Error processing CSV: {ex.Message}");
    }
    finally
    {
        uploadButton.interactable = true;
        isUploadInProgress = false;
    }
}


private class FetchResult
{
    public bool Success { get; set; }
    public string ErrorMessage { get; set; }
    public List<UserData> Students { get; set; }
}

private IEnumerator FetchStudentsCoroutine()
{
    Debug.Log("Starting to fetch students...");

    if (Application.internetReachability == NetworkReachability.NotReachable)
    {
        ShowFeedback("No internet connection. Please check your connection and try again.");
        yield break;
    }

    string url = $"{baseUrl}/users";
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
        Debug.LogError($"Error processing response: {ex.Message}");
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
        Debug.Log($"Sending request to: {url}");
        return new RequestResult { Success = true, Request = request };
    }
    catch (Exception ex)
    {
        Debug.LogError($"Error creating request: {ex.Message}");
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
        
        Debug.LogError($"Request failed: {request.error}");
        return new RequestResult 
        { 
            Success = false, 
            ErrorMessage = $"Server request failed: {request.error}" 
        };
    }
    catch (Exception ex)
    {
        Debug.LogError($"Error processing response: {ex.Message}");
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
    Debug.Log($"Raw response: {responseContent}");

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
            Debug.Log($"Attempting to read file from: {filePath}");

            // Check if the file exists
            if (!File.Exists(filePath))
            {
                Debug.LogError($"File does not exist at path: {filePath}");
                return;
            }

            // Register the encoding provider
            System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);

            using (var stream = File.Open(filePath, FileMode.Open, FileAccess.Read))
            {
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
                                Debug.Log($"Uploaded student: {firstName} {lastName} in section {section}");
                            }
                            else
                            {
                                Debug.LogWarning("Skipping row due to missing FirstName or LastName.");
                            }
                        }
                        catch (Exception ex)
                        {
                            Debug.LogError($"Error processing row: {ex.Message}");
                        }
                    }
                }
            }
            Debug.Log("Excel file processed successfully");
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error reading Excel file: {ex.Message}\nStackTrace: {ex.StackTrace}");
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
                Debug.LogError("Database is not initialized. Cannot upload student data.");
                ShowFeedback("Database connection failed. Please check your internet connection.");
                return; // Exit if the database is not initialized
            }

            // Log the values being uploaded
            Debug.Log($"Uploading student data: FirstName: {firstName}, LastName: {lastName}, Section: {section}, Username: {username}, Role: {role}, Character: {character}");

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
            Debug.Log($"Student data uploaded: {username} ({firstName} {lastName})");
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error uploading student data: {ex.Message}");
            ShowFeedback("Error uploading student data. Please try again.");
        }
    }

    public void AddRow(string studentName, string sectionName)
    {
        if (string.IsNullOrEmpty(studentName) || string.IsNullOrEmpty(sectionName))
        {
            Debug.LogError("Invalid student or section name");
            return;
        }

        try
        {
            if (rowPrefab == null || tableParent == null)
            {
                Debug.LogError("Required components missing for AddRow");
                return;
            }

            GameObject newRow = Instantiate(rowPrefab, tableParent);
            if (newRow == null)
            {
                Debug.LogError("Failed to instantiate row");
                return;
            }

            var rowRect = newRow.GetComponent<RectTransform>();
            if (rowRect == null)
            {
                Debug.LogError("Row prefab missing RectTransform");
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
            Debug.LogError($"Error in AddRow: {ex.Message}");
            LogToScreen($"Failed to add row: {ex.Message}");
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
            Debug.LogError($"Error in UpdateLayoutAndScroll: {ex.Message}");
            LogToScreen("Error updating layout");
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
            Debug.LogError($"Error getting student character: {ex.Message}");
        }
        return "";
    }

    private async Task<string> GetUsernameFromMongoDB(string studentName)
    {
        var collection = database.GetCollection<BsonDocument>("users");
        string[] nameParts = studentName.Split(' ');
        if (nameParts.Length < 2)
        {
            Debug.LogError("Invalid student name format");
            return null;
        }

        string firstName = nameParts[0].Trim();
        string lastName = string.Join(" ", nameParts.Skip(1)).Trim();

        Debug.Log($"Searching for - FirstName: {firstName}, LastName: {lastName}");

        var filter = Builders<BsonDocument>.Filter.And(
            Builders<BsonDocument>.Filter.Eq("FirstName", firstName),
            Builders<BsonDocument>.Filter.Eq("LastName", lastName)
        );

        var result = await collection.Find(filter).FirstOrDefaultAsync();

        if (result != null)
        {
            Debug.Log($"Found document with ID: {result["_id"]}");
            return result["Username"].AsString;
        }

        Debug.LogWarning($"No document found for student: {studentName}");
        return null;
    }

    public void OnStudentClick(string studentName, string sectionName)
    {
        Debug.Log($"Clicked on student: {studentName} with section: {sectionName}");

        try
        {
            viewStudentPanel.SetActive(true);

            TMP_Text studentNameText = viewStudentPanel.transform.Find("StudentNameText")?.GetComponent<TMP_Text>();
            TMP_Text sectionText = viewStudentPanel.transform.Find("StudentSectionText")?.GetComponent<TMP_Text>();
            TMP_Text characterText = viewStudentPanel.transform.Find("StudentCharacterText")?.GetComponent<TMP_Text>();

            if (studentNameText != null)
            {
                studentNameText.text = studentName;
                Debug.Log($"Set student name: {studentName}");

                // Set the selectedStudentFullName variable
                selectedStudentFullName = studentName; // Assuming studentName is the full name
            }

            if (sectionText != null)
            {
                sectionText.text = sectionName;
                Debug.Log($"Set section text to: {sectionName}");
            }

            // Get and set character
            if (characterText != null)
            {
                StartCoroutine(SetCharacterText(studentName, characterText));
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error in OnStudentClick: {ex.Message}");
        }
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
            Debug.LogError("No student selected!");
        }
    }

    public async void CreateStudent()
    {
        try
        {
            // Check if the database is initialized
            if (!isDatabaseInitialized)
            {
                ShowFeedback("Database is not initialized. Please try again later.");
                return; // Exit if the database is not initialized
            }

            if (string.IsNullOrEmpty(firstNameInputField.text) || 
                string.IsNullOrEmpty(lastNameInputField.text) || 
                string.IsNullOrEmpty(usernameInputField.text))
            {
                ShowFeedback("Please fill in all required fields");
                return;
            }

            // Ensure sectionDropdown and characterDropdown have valid options
            if (sectionDropdown.options.Count == 0 || characterDropdown.options.Count == 0)
            {
                ShowFeedback("Please ensure all dropdowns have valid options");
                return;
            }

            string firstName = firstNameInputField.text;
            string lastName = lastNameInputField.text;
            string username = usernameInputField.text;
            string section = sectionDropdown.options[sectionDropdown.value].text;
            string character = characterDropdown.options[characterDropdown.value].text;

            await UploadStudentToMongoDB(firstName, lastName, section, username, "Student", character);
            Debug.Log($"Student added: {firstName} {lastName} with character {character}");

            // Clear input fields
            firstNameInputField.text = "";
            lastNameInputField.text = "";
            usernameInputField.text = "";

            // Refresh the student list
            FetchStudents();

            // Refresh the section dropdown
            FetchSections();

            // Close panel and show feedback
            CloseCreateStudentPanel();
            ShowFeedback("Student created successfully!");
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error creating student: {ex.Message}");
            ShowFeedback("Error creating student. Please try again.");
        }
    }

    public async void FetchStudents()
    {
        try
        {
            LogToScreen("Starting to fetch students...");
            string url = $"{baseUrl}/users";
            
            using (UnityWebRequest request = UnityWebRequest.Get(url))
            {
                request.certificateHandler = new NetworkUtility.BypassCertificateHandler();
                
                LogToScreen($"Sending request to: {url}");
                await request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    string responseContent = request.downloadHandler.text;
                    LogToScreen($"Received response: {responseContent}");

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
                                LogToScreen($"Added student: {fullName}");
                            }
                        }
                    }
                    else
                    {
                        LogToScreen("No students found in response");
                    }
                }
                else
                {
                    LogToScreen($"Request failed: {request.error}");
                }
            }
        }
        catch (Exception ex)
        {
            LogToScreen($"Error: {ex.Message}");
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

        LogToScreen($"UI updated with {rows.Count} rows");
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
            LogToScreen($"Error processing student: {ex.Message}");
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
            Debug.LogError("Database is not initialized.");
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
            Debug.LogError($"Student not found in database: {fullName}");
            return (null, null); // Return null if the student is not found
        }
    }
    catch (Exception ex)
    {
        Debug.LogError($"Error fetching student name from database: {ex.Message}");
        return (null, null); // Return null in case of an error
    }
}


public async void OnRemoveStudentButtonClick()
{
    if (isRemovingStudent) return;
    isRemovingStudent = true;

    string fullName = selectedStudentFullName;
    
    Debug.Log($"Attempting to remove student: '{fullName}'");
    
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
                Debug.LogError($"Remove request failed: {request.error}");
                ShowFeedback("Failed to remove student. Please try again.");
            }
        }
    }
    catch (Exception ex)
    {
        Debug.LogError($"Error removing student: {ex.Message}");
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
            var collection = database.GetCollection<BsonDocument>("users");
            var filter = Builders<BsonDocument>.Filter.Exists("Section");

            // Fetch all documents with a Section field
            var documents = await collection.Find(filter).ToListAsync();

            // Use a HashSet to store unique sections
            HashSet<string> uniqueSections = new HashSet<string>();

            foreach (var doc in documents)
            {
                if (doc.Contains("Section"))
                {
                    uniqueSections.Add(doc["Section"].AsString);
                }
            }

            // Clear existing options in the dropdown
            sectionDropdown.ClearOptions();

            // Add unique sections to the dropdown
            sectionDropdown.AddOptions(uniqueSections.ToList());
        }
        catch (Exception ex)
        {
            LogToScreen($"Error fetching sections: {ex.Message}");
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
        sendRewardsButton.onClick.AddListener(ShowViewRewardsPanel);

        closeRewardsPanelButton.onClick.RemoveAllListeners();
        closeRewardsPanelButton.onClick.AddListener(CloseViewRewardsPanel);

        sendRewardsConfirmButton.onClick.RemoveAllListeners();
        sendRewardsConfirmButton.onClick.AddListener(SendRewards);

        closeSendRewardsButton.onClick.RemoveAllListeners();
        closeSendRewardsButton.onClick.AddListener(CloseSendRewardsPanel);

        // Add listener for remove student button
        if (removeStudentButton != null)
        {
            removeStudentButton.onClick.RemoveAllListeners();
            removeStudentButton.onClick.AddListener(OnRemoveStudentButtonClick);
        }

        // Update refresh button listener
        if (refreshButton != null)
        {
            refreshButton.onClick.RemoveAllListeners();
            refreshButton.onClick.AddListener(OnRefreshButtonClick);
        }
        else
        {
            LogToScreen("RefreshButton reference is missing!");
        }

        // Assign debug toggle button listener
        if (debugToggleButton != null)
        {
            debugToggleButton.onClick.RemoveAllListeners();
            debugToggleButton.onClick.AddListener(ToggleDebugConsole);
        }

        // Check and request permissions for Android
        #if UNITY_ANDROID
        if (!Permission.HasUserAuthorizedPermission(Permission.ExternalStorageRead))
        {
            Permission.RequestUserPermission(Permission.ExternalStorageRead);
        }
        if (!Permission.HasUserAuthorizedPermission(Permission.ExternalStorageWrite))
        {
            Permission.RequestUserPermission(Permission.ExternalStorageWrite);
        }
        #endif
    }

    // Add this method to check UI setup
    private void CheckUISetup()
    {
        if (tableParent != null)
        {
            var rectTransform = tableParent.GetComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(rectTransform.sizeDelta.x, 500); // Adjust height as needed

            var contentSizeFitter = tableParent.GetComponent<ContentSizeFitter>();
            var verticalLayoutGroup = tableParent.GetComponent<VerticalLayoutGroup>();

            LogToScreen($"TableParent Setup:" +
                $"\nPosition: {rectTransform.position}" +
                $"\nAnchored Position: {rectTransform.anchoredPosition}" +
                $"\nSize Delta: {rectTransform.sizeDelta}" +
                $"\nContent Size Fitter: {(contentSizeFitter != null ? "Present" : "Missing")}" +
                $"\nVertical Layout Group: {(verticalLayoutGroup != null ? "Present" : "Missing")}" +
                $"\nChild Count: {tableParent.childCount}");
        }
    }

    private void EnsureLayoutComponents()
    {
        if (tableParent != null)
        {
            var vlg = tableParent.GetComponent<VerticalLayoutGroup>();
            if (vlg != null)
            {
                vlg.childForceExpandHeight = false; // Prevents rows from forcing height change
            }

            vlg.childAlignment = TextAnchor.UpperCenter;
            vlg.childControlWidth = true;
            vlg.childForceExpandWidth = true;
            vlg.spacing = 5;

            var csf = tableParent.GetComponent<ContentSizeFitter>();
            if (csf == null) csf = tableParent.gameObject.AddComponent<ContentSizeFitter>();
           // csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            csf.verticalFit = ContentSizeFitter.FitMode.Unconstrained;

        }
    }

    private void LogToScreen(string message)
    {
        if (debugConsoleText != null)
        {
            // Add timestamp to message
            string timeStamp = System.DateTime.Now.ToString("HH:mm:ss");
            string logMessage = $"[{timeStamp}] {message}";

            // Add new line to queue
            debugLines.Enqueue(logMessage);

            // Remove old lines if we exceed the maximum
            while (debugLines.Count > MAX_DEBUG_LINES)
            {
                debugLines.Dequeue();
            }

            // Update the text display
            debugConsoleText.text = string.Join("\n", debugLines.ToArray());
        }
    }

    private void ToggleDebugConsole()
    {
        if (debugConsolePanel != null)
        {
            bool isActive = debugConsolePanel.activeSelf;
            debugConsolePanel.SetActive(!isActive);
        }
    }

    private void LogUISetup()
    {
        Debug.Log("=== UI SETUP CHECK ===");
        Debug.Log($"Screen Resolution: {Screen.width}x{Screen.height}");
        Debug.Log($"TableParent Rect: {tableParent?.GetComponent<RectTransform>().rect}");
    }

    private void RegisterEncoding()
    {
        if (!encodingRegistered)
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            encodingRegistered = true;
            Debug.Log("Encoding provider registered successfully");
        }
    }

    public void SetupDebugConsole()
    {
        if (debugConsolePanel != null)
        {
            debugConsolePanel.SetActive(false);
            if (debugToggleButton != null)
            {
                debugToggleButton.onClick.AddListener(ToggleDebugConsole);
            }
        }
    }

    private void UpdateContentHeight()
    {
        if (tableParent != null)
        {
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(tableParent.GetComponent<RectTransform>());
        }
    }

    private IEnumerator DelayedScrollbarRefresh()
    {
    Canvas.ForceUpdateCanvases(); 
    yield return new WaitForEndOfFrame();

    }
}

// Add these classes to handle the JSON response
[Serializable]
public class StudentListResponse
{
    public UserData[] users;
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


