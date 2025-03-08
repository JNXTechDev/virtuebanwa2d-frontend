using UnityEngine;
using UnityEngine.Video;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using System.Collections.Generic;

public class VideoManager : MonoBehaviour
{
    private static VideoManager instance;

    [Header("Scene Groups")]
    public VideoClip introVideo;
    public VideoClip mainVideo;
    public List<string> mainVideoScenes = new List<string>();

    [Header("Video Settings")]
    public RenderTexture videoRenderTexture;
    public RawImage backgroundImage;
    

    private VideoPlayer videoPlayer;
    private bool isFading = false;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            SetupComponents();
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }


    private void SetupComponents()
    {
        // Setup Video Player
        videoPlayer = GetComponent<VideoPlayer>();
        if (videoPlayer == null)
            videoPlayer = gameObject.AddComponent<VideoPlayer>();

        videoPlayer.playOnAwake = true;
        videoPlayer.isLooping = true;
        videoPlayer.renderMode = VideoRenderMode.RenderTexture;
        videoPlayer.targetTexture = videoRenderTexture;


        PlayVideoForCurrentScene();
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
        if (!videoPlayer || !videoRenderTexture) return;

        string currentScene = scene.name.ToLower();
        VideoClip clipToPlay = mainVideoScenes.Contains(currentScene) ? mainVideo : introVideo;

        if (clipToPlay != null && clipToPlay != videoPlayer.clip)
        {
            videoPlayer.Stop();
            videoPlayer.clip = clipToPlay;
            videoPlayer.targetTexture = videoRenderTexture;
            videoPlayer.Play();
            Debug.Log($"Switched video for scene: {currentScene} to {clipToPlay.name}");
        }
    }

    private void PlayVideoForCurrentScene()
    {
        string currentScene = SceneManager.GetActiveScene().name;
        VideoClip clipToPlay = mainVideoScenes.Contains(currentScene) ? mainVideo : introVideo;

        if (clipToPlay != null)
        {
            videoPlayer.clip = clipToPlay;
            videoPlayer.targetTexture = videoRenderTexture;
            videoPlayer.Play();
            Debug.Log($"Playing video for scene: {currentScene}");

            // Ensure the background image is showing the video
            if (backgroundImage != null)
            {
                backgroundImage.texture = videoRenderTexture;
            }
        }
    }
}
