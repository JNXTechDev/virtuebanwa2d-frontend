using UnityEngine;
using UnityEngine.SceneManagement;

public class LogoutHandler : MonoBehaviour
{
    // Name of the scene to return to after logout
    public string sceneName = "CreateorLogIn";

    // This method will be linked to the Logout button's onClick event
    public void OnLogoutButtonClicked()
    {
        // Clear user data from PlayerPrefs
        PlayerPrefs.DeleteKey("Username");
        PlayerPrefs.DeleteKey("Role");
        PlayerPrefs.DeleteKey("Section");
        PlayerPrefs.Save();

        Debug.Log("UserData cleared from PlayerPrefs on logout.");

        // Load the specified scene
        SceneManager.LoadScene(sceneName);
    }
}