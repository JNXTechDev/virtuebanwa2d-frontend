using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneSwitcher : MonoBehaviour
{
    // Scene names for login and create account
    public string loginSceneName = "Login";
    public string createAccountSceneName = "Create";

    // This method will be called when the Login button is clicked
    public void OnLoginButtonClicked()
    {
        SceneManager.LoadScene(loginSceneName);
    }

    // This method will be called when the Create Account button is clicked
    public void OnCreateAccountButtonClicked()
    {
        SceneManager.LoadScene(createAccountSceneName);
    }
}
