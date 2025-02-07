using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class SceneSwitcherMobile : MonoBehaviour
{
    public LoadingManager loadingManager; // Reference to the LoadingManager

    private void Update()
    {
        // Check for mouse click or touch using new Input System
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame ||
            Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed)
        {
            // Load the "createorlogin" scene using the LoadingManager
            if (loadingManager != null)
            {
                loadingManager.LoadScene("CreateorLogIn");
            }
        }
    }
}