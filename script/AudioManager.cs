using UnityEngine;
using UnityEngine.SceneManagement;

public class AudioManager : MonoBehaviour
{
    private static AudioManager instance;
    private AudioSource audioSource;

    [Header("Background Music")]
    public AudioClip menuMusic; // For intro, createorlogin, create, login scenes
    public AudioClip playSceneMusic; // For play scene only
    public AudioClip gameplayMusic; // For nene mainview scene

    [Header("Audio Settings")]
    [Range(0f, 1f)]
    public float musicVolume = 0.5f;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
            SetupAudioSource();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void SetupAudioSource()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        // Configure AudioSource
        audioSource.loop = true;
        audioSource.volume = musicVolume;
        audioSource.spatialBlend = 0f; // 2D sound
        audioSource.playOnAwake = true;

        // Start with menu music if we're in a menu scene
        string currentScene = SceneManager.GetActiveScene().name.ToLower();
        if (currentScene == "intro" || currentScene == "createorlogin" || 
            currentScene == "create" || currentScene == "login")
        {
            ChangeMusic(menuMusic);
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
        string sceneName = scene.name.ToLower();
        
        // Menu music scenes
        if (sceneName == "intro" || 
            sceneName == "createorlogin" || 
            sceneName == "create" || 
            sceneName == "login")
        {
            ChangeMusic(menuMusic);
        }
        // Play scene music
        else if (sceneName == "play")
        {
            ChangeMusic(playSceneMusic);
        }
        // Gameplay music (nene mainview)
        else if (sceneName == "nene mainview")
        {
            ChangeMusic(gameplayMusic);
        }

        Debug.Log($"Changed music for scene: {sceneName}");
    }

    private void ChangeMusic(AudioClip newMusic)
    {
        if (audioSource == null || newMusic == null)
        {
            Debug.LogError("AudioSource or AudioClip is missing!");
            return;
        }

        // Only change the music if it's different from the current one
        if (audioSource.clip != newMusic)
        {
            audioSource.clip = newMusic;
            audioSource.volume = musicVolume;
            
            if (!audioSource.isPlaying)
            {
                audioSource.Play();
                Debug.Log($"Started playing: {newMusic.name}");
            }
        }
    }

#if UNITY_EDITOR
    // This helps with debugging
    private void OnValidate()
    {
        if (audioSource != null)
        {
            audioSource.volume = musicVolume;
        }
    }
#endif
}