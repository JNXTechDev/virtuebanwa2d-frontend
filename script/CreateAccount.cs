using UnityEngine;
using TMPro;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System;
using UnityEngine.SceneManagement;

public class CreateAccountHandler : MonoBehaviour
{
    public TMP_InputField usernameInput; // Input for Username
    public TMP_InputField passwordInput; // Input for password
    public TMP_Dropdown roleDropdown; // Dropdown for Role selection
    public TMP_Text feedbackText; // For displaying feedback messages

    public TMP_InputField firstNameInput;
    public TMP_InputField lastNameInput;
    public TMP_InputField employeeIDInput;
    public TMP_InputField teacherUsernameInput;
    public TMP_InputField teacherPasswordInput;
    
    // UI panels
    public GameObject signUpBorder;
    public GameObject noticeMessageBorder;
    public TMP_Text noticeMessageText;

    // Base URL for the API
    //  private const string baseUrl = "http://192.168.1.11:5000/api"; // Updated URL
    private const string baseUrl = "https://vbdb.onrender.com/api";

    public void OnCreateAccountButtonClicked()
    {
        string firstName = firstNameInput.text.Trim();
        string lastName = lastNameInput.text.Trim();
        string employeeID = employeeIDInput.text.Trim();
        string username = teacherUsernameInput.text.Trim();
        string password = teacherPasswordInput.text.Trim();

        if (ValidateInputs(firstName, lastName, employeeID, username, password))
        {
            CreateTeacherAccount(firstName, lastName, employeeID, username, password);
        }
    }

    private bool ValidateInputs(string firstName, string lastName, string employeeID, string username, string password)
    {
        if (string.IsNullOrEmpty(firstName) || string.IsNullOrEmpty(lastName))
        {
            feedbackText.text = "Please enter your full name.";
            return false;
        }

        if (string.IsNullOrEmpty(employeeID))
        {
            feedbackText.text = "Employee ID is required.";
            return false;
        }

        if (string.IsNullOrEmpty(username) || username.Contains(" "))
        {
            feedbackText.text = "Username cannot be empty or contain spaces.";
            return false;
        }

        if (string.IsNullOrEmpty(password) || password.Length < 6)
        {
            feedbackText.text = "Password must be at least 6 characters.";
            return false;
        }

        return true;
    }

    private async void CreateTeacherAccount(string firstName, string lastName, string employeeID, string username, string password)
    {
        using (HttpClient client = new HttpClient())
        {
            try
            {
                var userData = new TeacherData
                {
                    FirstName = firstName,
                    LastName = lastName,
                    EmployeeID = employeeID,
                    Username = username,
                    Password = password,
                    Role = "Teacher",
                    AdminApproval = "Pending",
                    Character = "Teacher"
                };

                string json = JsonUtility.ToJson(userData);
                Debug.Log($"Sending teacher data: {json}");

                var content = new StringContent(json, Encoding.UTF8, "application/json");

                // Make sure we have the correct Content-Type header
                if (!content.Headers.Contains("Content-Type"))
                {
                    content.Headers.Add("Content-Type", "application/json");
                }

                // Add error handling for the URL
                string url = $"{baseUrl}/users/teacher";
                Debug.Log($"Sending request to: {url}");

                HttpResponseMessage response = await client.PostAsync(url, content);
                string responseContent = await response.Content.ReadAsStringAsync();
                Debug.Log($"Response status: {response.StatusCode}");
                Debug.Log($"Response content: {responseContent}");

                if (response.IsSuccessStatusCode)
                {
                    ShowNoticeMessage();
                }
                else
                {
                    feedbackText.text = $"Error: {response.StatusCode} - {responseContent}";
                    Debug.LogError($"Account creation failed: {responseContent}");
                }
            }
            catch (Exception ex)
            {
                feedbackText.text = $"Error: {ex.Message}";
                Debug.LogError($"Network error: {ex}");
            }
        }
    }

    private void ShowNoticeMessage()
    {
        signUpBorder.SetActive(false);
        noticeMessageBorder.SetActive(true);
        noticeMessageText.text = "Your registration request has been submitted! Please wait for admin approval.";
    }

    public void OnExitButtonClick()
    {
        SceneManager.LoadScene("CreateorLogin");
    }

    private void RedirectToSceneBasedOnRole(string role)
    {
        Debug.Log("Selected role: " + role); // Debug log to check the role value

        // Updated to only handle the "Teacher" role
        if (role != "Teacher")
        {
            Debug.LogError("Unknown role: " + role);
            feedbackText.text = "Unknown role.";
            return;
        }

        string sceneName = "Select.Teacher.Char"; // Directly set for Teacher role

        // Check if the scene exists before attempting to load it
        if (IsSceneInBuildSettings(sceneName))
        {
            SceneManager.LoadScene(sceneName);
        }
        else
        {
            Debug.LogError($"Scene '{sceneName}' not found in build settings.");
            feedbackText.text = "Scene not found.";
        }
    }

    private bool IsSceneInBuildSettings(string sceneName)
    {
        for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
        {
            string path = SceneUtility.GetScenePathByBuildIndex(i);
            if (path.EndsWith(sceneName + ".unity"))
            {
                return true;
            }
        }
        return false;
    }
}

[Serializable]
public class TeacherData
{
    public string FirstName;
    public string LastName;
    public string EmployeeID;
    public string Username;
    public string Password;
    public string Role;
    public string AdminApproval;
    public string Character;  // Add this field
}