using UnityEngine;
using UnityEngine.SceneManagement;

public class PreTestSceneTransition : MonoBehaviour
{
    public string nextScene;
    public string requiredNPC;
    
    private GameManager gameManager;
    
    private void Start()
    {
        gameManager = FindObjectOfType<GameManager>();
    }
    
    public void GoToNextScene()
    {
        // Check if the required NPC dialogue has been completed 
        if (string.IsNullOrEmpty(requiredNPC) || 
            HasCompletedNPC(requiredNPC))
        {
            Debug.Log($"Transitioning to next pre-test scene: {nextScene}");
            SceneManager.LoadScene(nextScene);
        }
        else
        {
            Debug.LogWarning($"Cannot proceed - you need to complete dialogue with {requiredNPC} first");
        }
    }
    
    private bool HasCompletedNPC(string npcName)
    {
        // Check with gameManager or directly in PlayerPrefs
        string username = PlayerPrefs.GetString("Username", "DefaultPlayer");
        string key = $"{username}_{npcName}_Completed";
        return PlayerPrefs.GetInt(key, 0) == 1;
    }
}
