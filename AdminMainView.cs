using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class AdminMainView : MonoBehaviour
{
    public TMP_Text welcomeText;
    public Button logoutButton;

    void Start()
    {
        // Check if user is admin
        string role = PlayerPrefs.GetString("Role");
        if (role != "Admin")
        {
            SceneManager.LoadScene("CreateorLogin");
            return;
        }

        // Setup welcome message
        string username = PlayerPrefs.GetString("Username");
        welcomeText.text = $"Welcome, Admin {username}";

        // Setup logout button
        logoutButton.onClick.AddListener(OnLogoutClick);
    }

    void OnLogoutClick()
    {
        // Clear user data
        PlayerPrefs.DeleteKey("Username");
        PlayerPrefs.DeleteKey("Role");
        PlayerPrefs.Save();

        // Return to login scene
        SceneManager.LoadScene("CreateorLogin");
    }
}
