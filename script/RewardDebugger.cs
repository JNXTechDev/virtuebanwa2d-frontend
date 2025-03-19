using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

/// <summary>
/// Helper component to debug reward popup visibility issues.
/// Attach this to the reward popup GameObject.
/// </summary>
public class RewardDebugger : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject rewardPopup;
    [SerializeField] private Button closeButton;
    
    // Reference to parent canvas
    private Canvas parentCanvas;
    
    private void Awake()
    {
        // If rewardPopup is not assigned, use this object
        if (rewardPopup == null)
            rewardPopup = gameObject;
            
        // Get parent canvas
        parentCanvas = GetComponentInParent<Canvas>();
        
        // Add visibility toggle button
        if (closeButton != null)
        {
            // Add a debug listener to the close button
            closeButton.onClick.AddListener(DebugOnClose);
        }
    }
    
    private void OnEnable()
    {
        Debug.Log($"[RewardDebugger] Reward popup enabled. Active: {rewardPopup.activeSelf}, ActiveInHierarchy: {rewardPopup.activeInHierarchy}");
        if (parentCanvas != null)
        {
            Debug.Log($"[RewardDebugger] Parent canvas: {parentCanvas.name}, Enabled: {parentCanvas.enabled}");
        }
        
        // Verify UI components
        VerifyUIComponents();
        
        // Start periodic check for visibility
        StartCoroutine(CheckVisibilityPeriodically());
    }
    
    private void VerifyUIComponents()
    {
        Debug.Log("[RewardDebugger] Verifying UI components:");
        
        // Check Image component
        Image rewardImage = GetComponentInChildren<Image>(true);
        if (rewardImage != null)
        {
            Debug.Log($"- Found reward image: {rewardImage.name}, Has sprite: {rewardImage.sprite != null}, Enabled: {rewardImage.enabled}");
        }
        else
        {
            Debug.LogWarning("- No Image component found in reward popup");
        }
        
        // Check TextMeshProUGUI components
        TextMeshProUGUI[] texts = GetComponentsInChildren<TextMeshProUGUI>(true);
        if (texts != null && texts.Length > 0)
        {
            foreach (var text in texts)
            {
                Debug.Log($"- Found text: {text.name}, Text: \"{text.text}\", Enabled: {text.enabled}");
            }
        }
        else
        {
            Debug.LogWarning("- No TextMeshProUGUI components found in reward popup");
        }
        
        // Check Button components
        Button[] buttons = GetComponentsInChildren<Button>(true);
        if (buttons != null && buttons.Length > 0)
        {
            foreach (var button in buttons)
            {
                Debug.Log($"- Found button: {button.name}, Interactable: {button.interactable}, Enabled: {button.enabled}");
            }
        }
        else
        {
            Debug.LogWarning("- No Button components found in reward popup");
        }
    }
    
    private void DebugOnClose()
    {
        Debug.Log("[RewardDebugger] Close button clicked");
    }
    
    private IEnumerator CheckVisibilityPeriodically()
    {
        int checkCount = 0;
        while (checkCount < 10 && gameObject != null && gameObject.activeInHierarchy)
        {
            yield return new WaitForSeconds(0.5f);
            checkCount++;
            Debug.Log($"[RewardDebugger] Check #{checkCount}: Reward popup active: {rewardPopup.activeSelf}, ActiveInHierarchy: {rewardPopup.activeInHierarchy}");
            
            // Force to front of UI hierarchy
            transform.SetAsLastSibling();
            
            // Force canvas update
            Canvas.ForceUpdateCanvases();
        }
    }
    
    private void OnDisable()
    {
        Debug.Log("[RewardDebugger] Reward popup disabled");
        StopAllCoroutines();
    }
    
    private void OnDestroy()
    {
        if (closeButton != null)
        {
            closeButton.onClick.RemoveListener(DebugOnClose);
        }
    }
    
    // Call this method from the inspector or other scripts to manually test the reward popup
    public void ShowForTesting()
    {
        Debug.Log("[RewardDebugger] Showing reward popup for testing");
        gameObject.SetActive(true);
    }
}
