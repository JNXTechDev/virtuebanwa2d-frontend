using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro; // For TextMeshPro UI elements
using System.Collections;

public class LoadingManager : MonoBehaviour
{
    public TMP_Text loadingText; // Reference to the TextMeshPro text for loading message

    // Call this method to start loading a new scene
    public void LoadScene(string sceneName)
    {
        StartCoroutine(LoadSceneAsync(sceneName));
    }

    private IEnumerator LoadSceneAsync(string sceneName)
    {
        // Show loading text
        if (loadingText != null)
        {
            loadingText.text = "Loading...";
        }

        // Start loading the scene asynchronously
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);

        // Wait until the asynchronous scene fully loads
        while (!asyncLoad.isDone)
        {
            yield return null; // Wait for the next frame
        }
    }
}