using UnityEngine;
using System.Net.Http;
using System.Threading.Tasks;

public class SchoolExitCollider : MonoBehaviour 
{
    public SceneTransition sceneTransition;
    public string nextSceneName = "Outside";

    private async void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            // Fetch tutorial status from the backend
            bool tutorialCompleted = await IsTutorialCompleted();

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

    private async Task<bool> IsTutorialCompleted()
    {
        string username = PlayerPrefs.GetString("Username");
        if (string.IsNullOrEmpty(username))
        {
            Debug.LogError("Username is not set in PlayerPrefs.");
            return false;
        }

        try
        {
            using (HttpClient client = new HttpClient())
            {
                HttpResponseMessage response = await client.GetAsync($"{NetworkConfig.BaseUrl}/game_progress/{username}");
                if (response.IsSuccessStatusCode)
                {
                    string responseContent = await response.Content.ReadAsStringAsync();
                    Debug.Log($"Progress data: {responseContent}");

                    // Parse the JSON response
                    var progressData = JsonUtility.FromJson<GameProgress>(responseContent);

                    // Check if the tutorial is marked as completed
                    if (progressData?.tutorial?.status == "Completed")
                    {
                        return true;
                    }
                }
                else
                {
                    Debug.LogError($"Error fetching progress: {response.StatusCode}");
                }
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Error fetching tutorial status: {ex.Message}");
        }

        return false;
    }
}
