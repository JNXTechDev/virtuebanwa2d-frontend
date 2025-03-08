using UnityEngine;
using UnityEngine.SceneManagement; // Required to manage scenes

public class backButtonCreate : MonoBehaviour
{
    // Name of the scene to return to 
    public string sceneName = "CreateorLogIn";

    
    public void OnbackButtonCreateClicked()
    {
        
        // Load the specified scene
        SceneManager.LoadScene(sceneName);
    }
}

