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

    // Base URL for the API
    //  private const string baseUrl = "http://192.168.1.11:5000/api"; // Updated URL
    private const string baseUrl = "https://vbdb.onrender.com/api";

    public void OnCreateAccountButtonClicked()
    {
        string username = usernameInput.text.Trim(); // Use Username
        string password = passwordInput.text;
        string selectedRole = roleDropdown.options[roleDropdown.value].text; // Use Role

        if (string.IsNullOrEmpty(username) || username.Contains(" ")) // Check for Username
        {
            feedbackText.text = "Username cannot be empty or contain spaces.";
            return;
        }

        if (string.IsNullOrEmpty(password) || password.Length < 6)
        {
            feedbackText.text = "Password cannot be empty and must be at least 6 characters.";
            return;
        }

        // Create the account
        CreateAccount(username, password, selectedRole);
    }

    private async void CreateAccount(string username, string password, string role)
    {
        using (HttpClient client = new HttpClient())
        {
            try
            {
                // Prepare the request body using the UserData class
                var userData = new UserData { Username = username, Password = password, Role = role };
                string json = JsonUtility.ToJson(userData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                // Log the JSON being sent
                Debug.Log($"Sending JSON: {json}");

                // Send POST request to the create user endpoint
                HttpResponseMessage response = await client.PostAsync($"{baseUrl}/users", content);

                if (response.IsSuccessStatusCode)
                {
                    feedbackText.text = "Account created successfully!";
                    Debug.Log("Account created successfully!");
                }
                else
                {
                    string responseContent = await response.Content.ReadAsStringAsync();
                    feedbackText.text = $"Error creating account: {response.ReasonPhrase}";
                    Debug.LogError($"Account creation failed: {response.ReasonPhrase}. Response: {responseContent}");
                }
            }
            catch (Exception ex)
            {
                feedbackText.text = "Error connecting to the server.";
                Debug.LogError($"Error during account creation: {ex.Message}");
            }
        }
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