using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using System.Threading.Tasks;
using System.Net.Http;
using System.Text;
using System;

public class Login : MonoBehaviour
{
    // Input fields
    public TMP_InputField usernameInput;
    public TMP_InputField studentUsernameInput; // For Students
    public TMP_InputField passwordInput;

    // Feedback text
    public TMP_Text feedbackText;

    // Role selection UI
    public GameObject loginRoleSelectionBorder;
    public GameObject loginBorderStudent;
    public GameObject loginBorderTeacherAdmin;

    // Add these UI references
    public GameObject NoticeMessageBorder;
    public TMP_Text NoticeMessageText;

    // Remove hardcoded URLs and use NetworkConfig instead
    private string baseUrl => NetworkConfig.BaseUrl;
    void Start()
    {
        // Hide NoticeMessageBorder initially
        if (NoticeMessageBorder != null)
        {
            NoticeMessageBorder.SetActive(false);
        }

        // Check database configuration first
        if (string.IsNullOrEmpty(PlayerPrefs.GetString("MONGO_URI", "")))
        {
            Debug.LogError("MongoDB not initialized! Make sure MongoDBConfig is in the scene.");
          //  feedbackText.text = "Database configuration error. Please restart the application.";
            return;
        }

        // Hide all login borders initially
        loginBorderStudent.SetActive(false);
        loginBorderTeacherAdmin.SetActive(false);

        // Show the role selection border
        loginRoleSelectionBorder.SetActive(true);
    }

    // Called when the Student button is clicked
    public void OnStudentButtonClicked()
    {
        // Hide role selection and show student login
        loginRoleSelectionBorder.SetActive(false);
        loginBorderStudent.SetActive(true);
    }

    // Called when the Teacher or Admin button is clicked
    public void OnTeacherAdminButtonClicked()
    {
        // Hide role selection and show teacher/admin login
        loginRoleSelectionBorder.SetActive(false);
        loginBorderTeacherAdmin.SetActive(true);
    }

    // Called when the Student Login button is clicked
    public async void OnStudentLoginButtonClicked()
    {
        string username = studentUsernameInput.text.Trim(); // Use studentUsernameInput for students

        // Validate the username
        if (string.IsNullOrEmpty(username) || username.Contains(" "))
        {
            feedbackText.text = "Invalid Username!";
            Debug.Log("Student Login Failed: Invalid Username!");
            return;
        }

        Debug.Log($"Attempting to log in as student with username: {username}");

        // Send login request to the server
        await LoginUser(username, null); // Pass null for password in student login
    }

    // Called when the Teacher/Admin Login button is clicked
    public async void OnTeacherAdminLoginButtonClicked()
    {
        string username = usernameInput.text.Trim();
        string password = passwordInput.text.Trim();

        // Validate the username and password
        if (string.IsNullOrEmpty(username) || username.Contains(" "))
        {
            feedbackText.text = "Invalid Username!";
            Debug.Log("Teacher/Admin Login Failed: Invalid Username!");
            return;
        }

        if (string.IsNullOrEmpty(password))
        {
            feedbackText.text = "Password cannot be empty!";
            Debug.Log("Teacher/Admin Login Failed: Password cannot be empty!");
            return;
        }

        Debug.Log($"Attempting to log in as teacher/admin with username: {username}");

        // Send login request to the server
        await LoginUser(username, password);
    }

    private async Task LoginUser(string username, string password)
    {
        try
        {
            using (var client = new HttpClient())
            {
                // Add timeout
                client.Timeout = TimeSpan.FromSeconds(30);
                
                var loginData = new LoginRequestData { 
                    Username = username, 
                    Password = password 
                };
                
                string json = JsonUtility.ToJson(loginData);
                Debug.Log($"Sending login request to {NetworkConfig.BaseUrl}/login");
                Debug.Log($"Request body: {json}");

                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await client.PostAsync($"{NetworkConfig.BaseUrl}/login", content);
                
                string responseContent = await response.Content.ReadAsStringAsync();
                Debug.Log($"Server response: {responseContent}");

                var errorResponse = JsonUtility.FromJson<ErrorResponse>(responseContent);
                
                // Check for approval status messages first
                if (errorResponse != null && !string.IsNullOrEmpty(errorResponse.message))
                {
                    if (responseContent.Contains("pending approval"))
                    {
                        ShowNoticeMessage("Your account is pending admin approval. Please wait for approval before logging in.");
                        return;
                    }
                    else if (responseContent.Contains("rejected"))
                    {
                        ShowNoticeMessage("Your account registration has been rejected. Please contact an administrator.");
                        return;
                    }
                }

                if (response.IsSuccessStatusCode)
                {
                    // Deserialize the response
                    var responseData = JsonUtility.FromJson<LoginResponseData>(responseContent);

                    // Check if deserialization succeeded
                    if (responseData == null || responseData.user == null)
                    {
                        ShowFeedback("Failed to parse user data.");
                        return;
                    }

                    Debug.Log($"Deserialized user data: Username={responseData.user.Username}, Role={responseData.user.Role}");

                    // Check for teacher approval status
                    if (responseData.user.Role == "Teacher")
                    {
                        if (responseData.user.AdminApproval == "Pending")
                        {
                            ShowNoticeMessage("Your account is pending admin approval. Please wait for approval before logging in.");
                            return;
                        }
                        else if (responseData.user.AdminApproval == "Rejected")
                        {
                            ShowNoticeMessage("Your account registration has been rejected. Please contact an administrator.");
                            return;
                        }
                    }

                    // Check if the user is a student (if logging in as a student)
                    if (password == null && responseData.user.Role != "Student")
                    {
                        feedbackText.text = "Only students can log in here.";
                        Debug.LogError("User is not a student.");
                        return;
                    }

                    // Store user data in PlayerPrefs
                    PlayerPrefs.SetString("Username", responseData.user.Username);
                    PlayerPrefs.SetString("Role", responseData.user.Role);
                    PlayerPrefs.SetString("Section", responseData.user.Section);
                    PlayerPrefs.SetString("FirstName", responseData.user.FirstName);
                    PlayerPrefs.SetString("LastName", responseData.user.LastName);
                    PlayerPrefs.SetString("Character", responseData.user.Character);
                    PlayerPrefs.Save();

                    feedbackText.text = "Login successful!";
                    Debug.Log("Login Successful!");
                    Debug.Log($"Feedback Text: {feedbackText.text}");

                    // Redirect to the "Play" scene
                    SceneManager.LoadScene("Play");
                }
                else
                {
                    feedbackText.text = "Invalid username or password.";
                    Debug.LogError($"Login Failed: {response.ReasonPhrase}. Response: {responseContent}");
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"Login error: {ex.GetType().Name} - {ex.Message}");
            feedbackText.text = "Error connecting to server. Please check your internet connection.";
        }
    }

    private void ShowNoticeMessage(string message)
    {
        Debug.Log($"Attempting to show notice message: {message}");
        
        // Hide all login panels first
        loginBorderStudent?.SetActive(false);
        loginBorderTeacherAdmin?.SetActive(false);
        loginRoleSelectionBorder?.SetActive(false);

        // Show notice message
        if (NoticeMessageBorder != null && NoticeMessageText != null)
        {
            NoticeMessageText.text = message;
            NoticeMessageBorder.SetActive(true);
            Debug.Log($"Notice message panel active: {NoticeMessageBorder.activeSelf}");
        }
        else
        {
            Debug.LogError("NoticeMessageBorder or NoticeMessageText is not assigned!");
        }
    }

    public void OnExitButtonClick()
    {
        SceneManager.LoadScene("CreateorLogin");
    }
    private void ShowFeedback(string message)
    {
        if (feedbackText != null)
        {
            feedbackText.text = message;
        }
        else
        {
            Debug.LogWarning($"FeedbackText is null. Message was: {message}");
        }
    }

    // Add this for error response parsing
    [Serializable]
    private class ErrorResponse
    {
        public string message;
        public string status;
    }
}

// Define the LoginRequestData class for serialization
[System.Serializable]
public class LoginRequestData
{
    public string Username;
    public string Password;
}

// Define the LoginResponseData class for deserialization
[System.Serializable]
public class LoginResponseData
{
    public string message;
    public UserData user;
}