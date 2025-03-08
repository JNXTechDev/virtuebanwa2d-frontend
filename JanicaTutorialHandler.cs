using UnityEngine;
using UnityEngine.UI;

public class JanicaTutorialHandler : MonoBehaviour
{
    [Header("Arrow References")]
    public GameObject profileArrow; // Reference to the arrow for profile button
    public GameObject menuArrow;    // Reference to the arrow for menu button
    
    [Header("UI Element References")]
    public GameObject profileButton; // Reference to profile button
    public GameObject menuButton;    // Reference to menu button
    
    private NPCscript janicaScript;
    
    void Awake()
    {
        janicaScript = GetComponent<NPCscript>();
        
        // Find arrow references if not assigned
        if (profileArrow == null)
            profileArrow = GameObject.Find("ProfileArrow");
            
        if (menuArrow == null)
            menuArrow = GameObject.Find("MenuArrow");
        
        // Find UI elements if not assigned
        if (profileButton == null)
            profileButton = GameObject.Find("ProfileButton");
            
        if (menuButton == null)
            menuButton = GameObject.Find("MenuButton");
            
        // Hide arrows initially
        HideAllArrows();
        
        // Add ArrowAnimator component to arrows if they don't have it
        AddFloatAnimationIfNeeded(profileArrow);
        AddFloatAnimationIfNeeded(menuArrow);
    }
    
    private void AddFloatAnimationIfNeeded(GameObject arrow)
    {
        if (arrow != null && !arrow.GetComponent<ArrowAnimator>())
        {
            ArrowAnimator floatComponent = arrow.AddComponent<ArrowAnimator>();
            
            // Set the RectTransform reference for the animation
            floatComponent.arrow = arrow.GetComponent<RectTransform>();
            
            // Set reasonable default values
            floatComponent.moveDistance = 10f;
            floatComponent.moveSpeed = 20f;
        }
    }
    
    public void HandleInstructionStep(int step)
    {
        HideAllArrows();
        
        switch(step)
        {
            case 1: // Profile button instruction
                if (profileArrow != null)
                    profileArrow.SetActive(true);
                break;
                
            case 2: // Menu button instruction
                if (menuArrow != null)
                    menuArrow.SetActive(true);
                break;
                
            default:
                // Keep all arrows hidden for other steps
                break;
        }
    }
    
    public void HideAllArrows()
    {
        if (profileArrow != null)
            profileArrow.SetActive(false);
            
        if (menuArrow != null)
            menuArrow.SetActive(false);
    }
    
    private void OnDisable()
    {
        // Make sure arrows are hidden when this component is disabled
        HideAllArrows();
    }
}
