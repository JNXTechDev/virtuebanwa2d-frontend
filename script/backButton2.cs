using UnityEngine;
using UnityEngine.SceneManagement; // Required to manage scenes

public class backButtonLogIn : MonoBehaviour
{
    // Name of the scene to return to 
    public string sceneName = "CreateorLogIn";

    
    public void OnbackButtonLogInClicked()
    {
        
        // Load the specified scene
        SceneManager.LoadScene(sceneName);
    }
}
