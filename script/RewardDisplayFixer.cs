using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

/// <summary>
/// Utility to fix and debug the reward display issues in lessons
/// Add this component to the reward popup/panel GameObject
/// </summary>
public class RewardDisplayFixer : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Reference to the reward image component")]
    public Image rewardImage;
    
    [Tooltip("Reference to the ribbon header image")]
    public Image ribbonImage;
    
    [Tooltip("Reference to the congrats text")]
    public TextMeshProUGUI congratsText;
    
    [Tooltip("Reference to the reward description text")]
    public TextMeshProUGUI rewardText;
    
    [Tooltip("Reference to the close button")]
    public Button closeButton;
    
    [Header("Testing")]
    [Tooltip("Enable to show the reward panel in the editor for testing")]
    public bool showForTesting = false;
    
    [Tooltip("Test reward sprite name to load")]
    public string testRewardSprite = "ThreeStar";
    
    [Tooltip("Test congrats message")]
    public string testCongratsMessage = "Congratulations!";
    
    [Tooltip("Test reward message")]
    public string testRewardMessage = "You earned a reward!";
    
    [Header("Debug Tools")]
    [Tooltip("Enable to log detailed debug info")]
    public bool debugLogging = true;
    
    [Tooltip("Automatically fix common visibility issues")]
    public bool autoFix = true;
    
    void Start()
    {
        // Auto-locate components if not set
        if (rewardImage == null)
            rewardImage = GetComponentInChildren<Image>(true);
            
        if (congratsText == null)
            congratsText = transform.Find("CongratsTextTitle")?.GetComponent<TextMeshProUGUI>();
            
        if (rewardText == null)
            rewardText = transform.Find("RewardText")?.GetComponent<TextMeshProUGUI>();
            
        if (closeButton == null)
            closeButton = GetComponentInChildren<Button>(true);
            
        // Log debug info about components
        if (debugLogging)
        {
            Debug.Log($"[RewardDisplayFixer] Components found: " +
                     $"Image: {rewardImage != null}, " +
                     $"Ribbon: {ribbonImage != null}, " +
                     $"Congrats: {congratsText != null}, " +
                     $"Text: {rewardText != null}, " +
                     $"Button: {closeButton != null}");
        }
        
        // Show for testing in the editor if enabled
        if (showForTesting)
        {
            ShowForTesting();
        }
        else
        {
            // Otherwise hide by default
            gameObject.SetActive(false);
        }
    }
    
    /// <summary>
    /// Show the reward panel with test data for debugging
    /// </summary>
    public void ShowForTesting()
    {
        if (debugLogging)
            Debug.Log("[RewardDisplayFixer] Showing reward for testing");
            
        // Load and set test sprite
        if (rewardImage != null)
        {
            Sprite sprite = Resources.Load<Sprite>($"Rewards/{testRewardSprite}");
            if (sprite != null)
            {
                rewardImage.sprite = sprite;
                rewardImage.preserveAspect = true;
            }
            else
            {
                Debug.LogError($"[RewardDisplayFixer] Could not load sprite: Rewards/{testRewardSprite}");
            }
        }
        
        // Set test texts
        if (congratsText != null)
            congratsText.text = testCongratsMessage;
            
        if (rewardText != null)
            rewardText.text = testRewardMessage;
            
        // Fix canvas visibility
        if (autoFix)
        {
            // Ensure this object and its parents are active
            Transform current = transform;
            while (current != null)
            {
                if (!current.gameObject.activeSelf)
                {
                    current.gameObject.SetActive(true);
                    if (debugLogging)
                        Debug.Log($"[RewardDisplayFixer] Activated {current.name}");
                }
                current = current.parent;
            }
            
            // Ensure canvas is visible with proper sorting
            Canvas parentCanvas = GetComponentInParent<Canvas>();
            if (parentCanvas != null)
            {
                parentCanvas.enabled = true;
                
                // Make sure it's in front of other elements
                if (parentCanvas.sortingOrder < 100)
                    parentCanvas.sortingOrder = 100;
                    
                if (debugLogging)
                    Debug.Log($"[RewardDisplayFixer] Canvas {parentCanvas.name} enabled with sorting order {parentCanvas.sortingOrder}");
            }
            
            // Force this to be in front of siblings
            transform.SetAsLastSibling();
            
            // Force canvas update
            Canvas.ForceUpdateCanvases();
        }
        
        // Make sure this object is active
        gameObject.SetActive(true);
        
        if (debugLogging)
            Debug.Log($"[RewardDisplayFixer] Reward panel is now active: {gameObject.activeSelf}, in hierarchy: {gameObject.activeInHierarchy}");
    }
    
    /// <summary>
    /// Call this method from other scripts to show the reward
    /// </summary>
    public void ShowReward(string spriteName, string congratsMsg, string rewardMsg)
    {
        testRewardSprite = spriteName;
        testCongratsMessage = congratsMsg;
        testRewardMessage = rewardMsg;
        ShowForTesting();
    }
    
    /// <summary>
    /// Method to check for UI rendering issues and fix them
    /// </summary>
    public void DiagnoseVisibilityIssues()
    {
        StartCoroutine(DiagnoseVisibilityCoroutine());
    }
    
    private IEnumerator DiagnoseVisibilityCoroutine()
    {
        Debug.Log("[RewardDisplayFixer] Diagnosing visibility issues...");
        
        // Check if panel is active
        Debug.Log($"Reward panel active: {gameObject.activeSelf}, active in hierarchy: {gameObject.activeInHierarchy}");
        
        // Check canvas
        Canvas parentCanvas = GetComponentInParent<Canvas>();
        if (parentCanvas != null)
        {
            Debug.Log($"Parent canvas: {parentCanvas.name}, enabled: {parentCanvas.enabled}, sorting order: {parentCanvas.sortingOrder}");
            
            // Check if canvas is rendering correctly
            yield return new WaitForEndOfFrame();
            Debug.Log($"Canvas pixel rect: {parentCanvas.pixelRect}");
        }
        else
        {
            Debug.LogError("No parent canvas found!");
        }
        
        // Check raycasting
        GraphicRaycaster raycaster = parentCanvas?.GetComponent<GraphicRaycaster>();
        if (raycaster != null)
        {
            Debug.Log($"Canvas has GraphicRaycaster: {raycaster.enabled}");
        }
        else
        {
            Debug.LogWarning("Canvas has no GraphicRaycaster - UI interactions may not work");
        }
        
        // Check components
        if (rewardImage != null)
        {
            Debug.Log($"Reward image - enabled: {rewardImage.enabled}, color: {rewardImage.color}, raycast target: {rewardImage.raycastTarget}");
        }
        
        if (closeButton != null)
        {
            Debug.Log($"Close button - enabled: {closeButton.enabled}, interactable: {closeButton.interactable}");
        }
        
        // Check for blocking elements
        Canvas[] allCanvases = FindObjectsOfType<Canvas>();
        foreach (Canvas canvas in allCanvases)
        {
            if (canvas != parentCanvas && canvas.sortingOrder >= parentCanvas.sortingOrder)
            {
                Debug.LogWarning($"Potentially blocking canvas: {canvas.name} with sorting order {canvas.sortingOrder}");
            }
        }
    }
    
    void OnEnable()
    {
        // Diagnostic check when enabled
        if (autoFix && debugLogging)
        {
            DiagnoseVisibilityIssues();
        }
    }
}
