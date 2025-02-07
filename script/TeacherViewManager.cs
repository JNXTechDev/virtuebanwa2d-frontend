using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using System.Collections;

public class TeacherViewManager : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI teacherNameText;
    [SerializeField] private TextMeshProUGUI roleText;

    // private const string baseUrl = "http://192.168.1.11:5000"; // Corrected base URL
    private const string baseUrl = "https://vbdb.onrender.com/api";

    void Start()
    {
        DisplayTeacherInfo(); // Fetch and display teacher information
    }

    private void DisplayTeacherInfo()
    {
        // Retrieve user data from PlayerPrefs
        string username = PlayerPrefs.GetString("Username");
        string role = PlayerPrefs.GetString("Role");

        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(role))
        {
            Debug.LogError("Username or role is not set in PlayerPrefs.");
            teacherNameText.text = "Teacher: Unknown";
            return;
        }

        // Debug log to verify PlayerPrefs data
        Debug.Log($"PlayerPrefs data - Username: {username}, Role: {role}");

        // Update the UI with the retrieved data
        teacherNameText.text = $"Teacher: {username}";
        roleText.text = role;

        // Fetch additional teacher details from MongoDB API (optional)
        StartCoroutine(FetchTeacherDetails(username));
    }

    private IEnumerator FetchTeacherDetails(string username)
    {
        string url = $"{baseUrl}/users/{username}";
        Debug.Log($"Fetching teacher details from: {url}");

        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            request.certificateHandler = new NetworkUtility.BypassCertificateHandler();

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                string responseContent = request.downloadHandler.text;
                Debug.Log($"Received teacher details: {responseContent}");

                // Deserialize the response
                var userData = JsonUtility.FromJson<UserDataResponse>(responseContent);

                // Check if the user data is valid
                if (userData != null && userData.user != null)
                {
                    // Update the UI with the fetched data (optional)
                    teacherNameText.text = $"Teacher: {userData.user.Username}";
                    roleText.text = $"{userData.user.Role}";
                }
                else
                {
                    Debug.LogError("Invalid user data received.");
                }
            }
            else
            {
                // Silently handle the error instead of logging it
                teacherNameText.text = $"Teacher: {username}";
            }
        }
    }

    // Method to handle logout
    public void Logout()
    {
        // Clear user data from PlayerPrefs
        PlayerPrefs.DeleteKey("Username");
        PlayerPrefs.DeleteKey("Role");
        PlayerPrefs.DeleteKey("Section");
        PlayerPrefs.Save();

        Debug.Log("UserData cleared from PlayerPrefs on logout.");

        // Load the CreateorLogIn scene
        SceneManager.LoadScene("CreateorLogIn");
    }
}

// Define a class to match the user data structure returned by the API
[System.Serializable]
public class UserDataResponse
{
    public string message;
    public UserData user; // Reuse the existing UserData class
}