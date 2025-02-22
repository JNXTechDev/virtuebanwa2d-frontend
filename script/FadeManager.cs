using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class FadeManager : MonoBehaviour
{
    private static FadeManager _instance;
    public static FadeManager Instance
    {
        get
        {
            if (_instance == null)
            {
                var go = new GameObject("FadeManager");
                _instance = go.AddComponent<FadeManager>();
                DontDestroyOnLoad(go);
            }
            return _instance;
        }
    }

    private CanvasGroup fadeCanvasGroup;
    public float fadeDuration = 1f;
    private bool isFading = false;
    private GameObject fadeCanvas;

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);
        CreateFadeCanvas();
    }

    private void CreateFadeCanvas()
    {
        // Clean up any existing canvas
        if (fadeCanvas != null)
        {
            Destroy(fadeCanvas);
        }

        // Create new canvas with correct settings
        fadeCanvas = new GameObject("FadeCanvas");
        fadeCanvas.transform.SetParent(transform);
        DontDestroyOnLoad(fadeCanvas);
        
        var canvas = fadeCanvas.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 999;
        
        // Add CanvasScaler for proper scaling
        var scaler = fadeCanvas.AddComponent<UnityEngine.UI.CanvasScaler>();
        scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.screenMatchMode = UnityEngine.UI.CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        
        fadeCanvas.AddComponent<UnityEngine.UI.GraphicRaycaster>();
        
        // Create black overlay with correct positioning
        var overlay = new GameObject("BlackOverlay");
        overlay.transform.SetParent(fadeCanvas.transform);
        DontDestroyOnLoad(overlay);
        
        var image = overlay.AddComponent<UnityEngine.UI.Image>();
        image.color = Color.black;
        
        // Set up RectTransform properly
        var rect = overlay.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = Vector2.zero;
        rect.anchoredPosition = Vector2.zero;
        rect.localScale = Vector3.one;
        rect.localPosition = Vector3.zero;
        
        fadeCanvasGroup = overlay.AddComponent<CanvasGroup>();
        fadeCanvasGroup.alpha = 0f;
        fadeCanvasGroup.blocksRaycasts = false;

        // Ensure the overlay covers the entire screen
        Canvas.ForceUpdateCanvases();
    }

    public void FadeToScene(string sceneName)
    {
        if (!isFading && fadeCanvasGroup != null)
        {
            StartCoroutine(FadeOutAndLoadScene(sceneName));
        }
    }

    private IEnumerator FadeIn()
    {
        if (fadeCanvasGroup == null)
        {
            CreateFadeCanvas();
            yield return null; // Wait a frame for canvas to initialize
        }

        isFading = true;
        fadeCanvasGroup.alpha = 1f;
        
        while (fadeCanvasGroup != null && fadeCanvasGroup.alpha > 0)
        {
            fadeCanvasGroup.alpha -= Time.deltaTime / fadeDuration;
            yield return null;
        }

        if (fadeCanvasGroup != null)
        {
            fadeCanvasGroup.blocksRaycasts = false;
        }
        
        isFading = false;
    }

    private IEnumerator FadeOutAndLoadScene(string sceneName)
    {
        if (fadeCanvasGroup == null)
        {
            CreateFadeCanvas();
            yield return null;
        }

        isFading = true;
        fadeCanvasGroup.blocksRaycasts = true;
        fadeCanvasGroup.alpha = 0f;
        
        while (fadeCanvasGroup != null && fadeCanvasGroup.alpha < 1)
        {
            fadeCanvasGroup.alpha += Time.deltaTime / fadeDuration;
            yield return null;
        }

        yield return SceneManager.LoadSceneAsync(sceneName);
        StartCoroutine(FadeIn());
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
        if (!isFading)
        {
            StartCoroutine(FadeIn());
        }
    }
}
