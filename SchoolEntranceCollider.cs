using UnityEngine;

public class SchoolEntranceCollider : MonoBehaviour 
{
    public SceneTransition sceneTransition;
    public string nextSceneName = "SchoolInside";
    private TutorialChecker tutorialChecker;

    void Start()
    {
        tutorialChecker = FindObjectOfType<TutorialChecker>();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            // First check if the tutorial is explicitly marked as completed
            bool statusCheck = PlayerPrefs.GetString("TutorialStatus", "") == "Completed";
            
            // For more robustness, check individual NPCs - now including Janica
            bool janicaCompleted = PlayerPrefs.GetInt("TutorialJanica", 0) == 1;
            bool markCompleted = PlayerPrefs.GetInt("TutorialMark", 0) == 1;
            bool annieCompleted = PlayerPrefs.GetString("TutorialCheckpoint", "") == "Annie";
            
            // All required NPCs outside the school must be completed
            bool requiredNPCsCompleted = janicaCompleted && markCompleted && annieCompleted;
            
            // Flag to track if we've verified all the necessary checks
            bool shouldAllowEntry = statusCheck || requiredNPCsCompleted;
            
            Debug.Log($"School entrance check: StatusCheck={statusCheck}, " +
                      $"Janica={janicaCompleted}, Mark={markCompleted}, Annie={annieCompleted}, " +
                      $"AllowEntry={shouldAllowEntry}");
            
            // Either we have explicit completion or all required NPCs have been completed
            if (shouldAllowEntry)
            {
                Debug.Log("Initial tutorial complete, proceeding to next scene");
                if (sceneTransition != null)
                {
                    sceneTransition.StartTransitionWithReward(
                        nextSceneName,
                        "TwoStar", // Changed from ThreeStar since Rojan will award the final star
                        "Initial Tutorial Completed!",
                        "Tutorial",
                        "Tutorial",
                        20 // Changed from 30 since Rojan will award more points
                    );
                }
                else
                {
                    Debug.LogError("SceneTransition reference is missing!");
                }
            }
            else
            {
                Debug.Log($"Tutorial not complete yet. Janica: {janicaCompleted}, Mark: {markCompleted}, Annie: {annieCompleted}");
                if (tutorialChecker != null)
                {
                    tutorialChecker.ShowTutorialFeedback();
                }
                else
                {
                    Debug.LogWarning("TutorialChecker not found. Cannot show feedback.");
                    if (!janicaCompleted)
                        Debug.Log("Please talk to Janica first to learn about the game basics.");
                    else if (!markCompleted)
                        Debug.Log("Please talk to Mark near the school entrance.");
                    else
                        Debug.Log("Please talk to Annie near the flowers.");
                }
            }
        }
    }
}
