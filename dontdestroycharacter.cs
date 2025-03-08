using UnityEngine;

public class CharacterPersist : MonoBehaviour
{
    private static CharacterPersist instance;

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);  // This ensures the player persists across scenes
        }
        else
        {
            Destroy(gameObject);  // Prevent duplicates if another instance is found
        }
    }
    void OnDestroy()
{
    Debug.Log("Player is being destroyed!");
}

}


