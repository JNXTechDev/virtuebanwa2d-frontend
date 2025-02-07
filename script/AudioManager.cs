using UnityEngine;
using UnityEngine.SceneManagement;

public class AudioManager : MonoBehaviour
{
    private static AudioManager instance;
    private AudioSource audioSource;

    public AudioClip defaultMusic; // Music for general scenes
    public AudioClip neneMainviewMusic; // Music for "nene mainview" scene

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);

            // Check if AudioSource exists, if not, add it
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }

            audioSource.loop = true; // Ensure the audio loops
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Debug.Log($"Scene loaded: {scene.name}"); // Add this line for debugging
        if (scene.name == "nene mainview")
        {
            ChangeMusic(neneMainviewMusic);
        }
        else
        {
            ChangeMusic(defaultMusic);
        }
    }

    private void ChangeMusic(AudioClip newMusic)
    {
        if (newMusic == null)
        {
            Debug.LogError("Attempted to play null AudioClip");
            return;
        }

        audioSource.Stop();
        audioSource.clip = newMusic;
        audioSource.Play();
        Debug.Log($"Changed music to: {newMusic.name}"); // Add this line for debugging
    }
}