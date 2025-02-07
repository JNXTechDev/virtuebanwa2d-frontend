using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

public class LongPressHandler : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    public float longPressThreshold = 1.0f; // Time required to trigger long press
    public UnityEvent OnLongPress;

    private bool isPointerDown = false;
    private float pointerDownTime = 0;
    private bool longPressTriggered = false;

    private void Update()
    {
        if (isPointerDown)
        {
            if (Time.time - pointerDownTime >= longPressThreshold)
            {
                if (!longPressTriggered)
                {
                    longPressTriggered = true;
                    OnLongPress.Invoke();
                }
                isPointerDown = false; // Ensure it only triggers once
            }
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        isPointerDown = true;
        pointerDownTime = Time.time;
        longPressTriggered = false; // Reset on new press
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        isPointerDown = false;
        // If long press was triggered, prevent normal click
        if (longPressTriggered)
        {
            return;
        }

        // Otherwise, trigger normal click
        OnClick();
    }

    private void OnClick()
    {
        // Logic to handle normal click (if needed)
    }
}
