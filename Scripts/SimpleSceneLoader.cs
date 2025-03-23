using UnityEngine;
using UnityEngine.SceneManagement;

public class SimpleSceneLoader : MonoBehaviour
{
    public string sceneToLoad;
    
    private void OnTriggerEnter2D(Collider2D other)
    {
        // Check if the collider is the player
        if (other.CompareTag("Player"))
        {
            LoadScene();
        }
    }
    
    public void LoadScene()
    {
        if (!string.IsNullOrEmpty(sceneToLoad))
        {
            SceneManager.LoadScene(sceneToLoad);
        }
        else
        {
            Debug.LogError("Scene name is not set!");
        }
    }
}
