using UnityEngine;

public class SchoolEntranceTransition : MonoBehaviour
{
    public SceneTransition sceneTransition;
    public string nextScene = "Tutorial Inside"; // The scene to load
    
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            // Complete tutorial with final reward
            sceneTransition.StartTransitionWithReward(
                nextScene,
                "ThreeStar",
                "Tutorial completed!",
                "Tutorial",
                "Tutorial",
                30
            );
        }
    }
}
