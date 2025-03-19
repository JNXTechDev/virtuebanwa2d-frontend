using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Manages the reward panel to ensure it's properly displayed
/// Attach this to the reward panel/popup GameObject
/// </summary>
public class RewardPanelManager : MonoBehaviour
{
    [Header("Panel References")]
    public GameObject rewardPanel;
    public Image rewardImage;
    public TextMeshProUGUI rewardText;
    public TextMeshProUGUI congratsText;
    public Button closeButton;
    
    [Header("Debug Settings")]
    public bool logDebugMessages = true;
    public bool forceActiveOnStart = false;
    
    private Canvas parentCanvas;
    private CanvasGroup canvasGroup;
    
    private void Awake()
    {
        // If rewardPanel is not assigned, use this GameObject
        if (rewardPanel == null)
            rewardPanel = gameObject;
            
        // Get important components
        parentCanvas = GetComponentInParent<Canvas>();
        canvasGroup = GetComponent<CanvasGroup>();
        
        // Add canvas group if missing (allows fading)
        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
            
        // Log component references
        if (logDebugMessages)
        {
            Debug.Log($"[RewardPanelManager] Initialized - Panel: {rewardPanel != null}, " +
                     $"Image: {rewardImage != null}, Text: {rewardText != null}, " +
                     $"Congrats: {congratsText != null}, Button: {closeButton != null}");
        }
    }
    
    private void Start()
    {
        if (forceActiveOnStart)
        {
            // Testing mode - make visible for debugging
            ShowPanel();
        }
        else
        {
            // Normal operation - start hidden
            gameObject.SetActive(false);
        }
    }
    
    /// <summary>
    /// Display the reward panel with the specified reward information
    /// </summary>
    public void ShowReward(string rewardSpriteName, string rewardMessage, string congratsMessage)
    {
        if (logDebugMessages)
            Debug.Log($"[RewardPanelManager] Showing reward: {rewardSpriteName}, Message: {rewardMessage}");
        
        // Set the reward sprite
        if (rewardImage != null)
        {
            Sprite sprite = Resources.Load<Sprite>($"Rewards/{rewardSpriteName}");
            if (sprite != null)
            {
                rewardImage.sprite = sprite;
                rewardImage.preserveAspect = true;
            }
            else
            {
                Debug.LogError($"[RewardPanelManager] Could not load sprite: Rewards/{rewardSpriteName}");
            }
        }
        
        // Set the text elements
        if (rewardText != null)
            rewardText.text = rewardMessage;
            
        if (congratsText != null)
            congratsText.text = congratsMessage;
            
        // Show the panel
        ShowPanel();
    }
    
    /// <summary>
    /// Make the panel visible and ensure it's properly displayed
    /// </summary>
    public void ShowPanel()
    {
        // Step 1: Ensure all parent GameObjects are active
        Transform current = transform;
        while (current != null)
        {
            if (!current.gameObject.activeSelf)
            {
                if (logDebugMessages)
                    Debug.Log($"[RewardPanelManager] Activating parent: {current.name}");
                current.gameObject.SetActive(true);
            }
            current = current.parent;
        }
        
        // Step 2: Ensure canvas is enabled and in front
        if (parentCanvas != null)
        {
            parentCanvas.enabled = true;
            
            if (parentCanvas.sortingOrder < 100)
                parentCanvas.sortingOrder = 100; // Make sure it's in front
                
            if (logDebugMessages)
                Debug.Log($"[RewardPanelManager] Canvas enabled and sorting order set to {parentCanvas.sortingOrder}");
        }
        
        // Step 3: Activate this GameObject
        gameObject.SetActive(true);
        
        // Step 4: Make sure we're the last sibling for proper layering
        transform.SetAsLastSibling();
        
        // Step 5: Make sure canvas group is fully visible
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
        }
        
        // Step 6: Force layout update
        Canvas.ForceUpdateCanvases();
        
        if (logDebugMessages)
            Debug.Log($"[RewardPanelManager] Panel is now active: {gameObject.activeSelf}, " +
                     $"active in hierarchy: {gameObject.activeInHierarchy}");
        
        // Verify the button works
        if (closeButton != null)
        {
            closeButton.interactable = true;
            if (logDebugMessages)
                Debug.Log($"[RewardPanelManager] Close button is interactable: {closeButton.interactable}");
        }
    }
    
    /// <summary>
    /// Hide the panel using a smooth fade
    /// </summary>
    public void HidePanel()
    {
        if (logDebugMessages)
            Debug.Log("[RewardPanelManager] Hiding panel");
            
        // Option 1: Simple deactivate
        gameObject.SetActive(false);
        
        // Option 2: Fade out using canvas group (smoother transition)
        // StartCoroutine(FadeOut(0.5f));
    }
    
    private IEnumerator FadeOut(float duration)
    {
        if (canvasGroup != null)
        {
            float startTime = Time.time;
            float startAlpha = canvasGroup.alpha;
            
            // Disable interaction immediately
            canvasGroup.interactable = false;
            
            // Fade out over time
            while (Time.time < startTime + duration)
            {
                float normalizedTime = (Time.time - startTime) / duration;
                canvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, normalizedTime);
                yield return null;
            }
            
            // Ensure fully invisible
            canvasGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = false;
            
            // Deactivate after fade
            gameObject.SetActive(false);
        }
        else
        {
            // No canvas group, just deactivate
            gameObject.SetActive(false);
        }
    }
    
    /// <summary>
    /// Public method to test the reward panel from inspector
    /// </summary>
    public void TestReward()
    {
        ShowReward("TwoStar", "Test Reward Message", "Congratulations on testing this panel!");
    }

    /// <summary>
    /// Setup the close button with an action to perform on close
    /// </summary>
    public void SetupCloseButton(System.Action onCloseAction)
    {
        if (closeButton != null)
        {
            closeButton.onClick.RemoveAllListeners();
            closeButton.onClick.AddListener(() => {
                if (onCloseAction != null)
                    onCloseAction();
                HidePanel();
            });
            
            if (logDebugMessages)
                Debug.Log("[RewardPanelManager] Close button handler set up");
        }
    }
}
