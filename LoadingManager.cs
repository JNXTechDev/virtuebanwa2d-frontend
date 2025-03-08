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
        asyncLoad.allowSceneActivation = false; // Prevent immediate activation

        // Wait until the scene is loaded
        while (asyncLoad.progress < 0.9f)
        {
            yield return null;
        }

        // Ensure everything is ready before activating
        yield return new WaitForSeconds(0.1f);
        asyncLoad.allowSceneActivation = true;

        // Wait for scene to fully load
        while (!asyncLoad.isDone)
        {
            yield return null;
        }
    }
}