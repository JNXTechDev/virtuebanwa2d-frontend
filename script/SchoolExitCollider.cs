using UnityEngine;

public class SchoolExitCollider : MonoBehaviour 
{
    public SceneTransition sceneTransition;
    public string nextSceneName = "Outside";
    
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            // Check if the full tutorial is completed (includes Rojan)
            bool tutorialCompleted = PlayerPrefs.GetInt("TutorialRojan", 0) == 1;
            
            if (tutorialCompleted)
            {
                Debug.Log("Full tutorial completed, proceeding to main game");
                if (sceneTransition != null)
                {
                    PlayerPrefs.SetString("TutorialStatus", "Completed");
                    PlayerPrefs.Save();
                    
                    sceneTransition.StartTransitionWithReward(
                        nextSceneName,
                        "ThreeStar",
                        "Tutorial Fully Completed!",
                        "Tutorial",
                        "All Tutorial NPCs",
                        30
                    );
                }
                else
                {
                    Debug.LogError("SceneTransition reference is missing!");
                }
            }
            else
            {
                Debug.Log("Please talk to Rojan before leaving the school.");
            }
        }
    }
}
