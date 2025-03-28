using UnityEngine;

public class SchoolEntranceTrigger : MonoBehaviour 
{
    private GameManager gameManager;
    private SceneTransition sceneTransition;
    [Tooltip("The scene to load when entering school")]
    public string schoolSceneName = "Tutorial School";

    void Start()
    {
        gameManager = FindObjectOfType<GameManager>();
        sceneTransition = FindObjectOfType<SceneTransition>();
    }

    private async void OnTriggerEnter2D(Collider2D other)
    {
        if (gameManager != null && 
            ((other.gameObject.name.Contains("BoySprite") && PlayerPrefs.GetString("Character") == "Boy") ||
             (other.gameObject.name.Contains("GirlSprite") && PlayerPrefs.GetString("Character") == "Girl")))
        {
            if (await gameManager.CanEnterSchool())
            {
                Debug.Log("All NPCs completed, transitioning to school scene...");
                if (sceneTransition != null)
                {
                    // Use SceneTransition for smooth transition
                    sceneTransition.sceneToLoad = schoolSceneName;
                    sceneTransition.StartTransition(schoolSceneName);
                }
                else
                {
                    // Fallback to direct scene load
                    gameManager.TryEnterSchool(gameObject);
                }
            }
            else
            {
                Debug.Log("Cannot enter school yet - not all NPCs have been talked to");
            }
        }
    }
}
