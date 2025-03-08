using UnityEngine;
using System.Collections;

// Rename the class to match the filename
public class ArrowAnimator : MonoBehaviour
{
    public RectTransform arrow; // Assign your arrow RectTransform here
    public float moveDistance = 10f; // Distance to move up and down
    public float moveSpeed = 20f; // Speed of the movement

    private Vector2 originalPosition;
    private bool initialized = false;

    void Start()
    {
        InitializeArrow();
    }
    
    void OnEnable()
    {
        // Re-initialize when enabled to handle cases where the object was disabled
        InitializeArrow();
    }
    
    private void InitializeArrow()
    {
        if (arrow == null)
        {
            // Try to get the RectTransform from this GameObject
            arrow = GetComponent<RectTransform>();
            
            if (arrow == null)
            {
                Debug.LogError("Arrow RectTransform is not assigned or found.");
                return;
            }
        }

        if (!initialized)
        {
            originalPosition = arrow.anchoredPosition;
            StartCoroutine(FloatArrow());
            initialized = true;
        }
    }

    private IEnumerator FloatArrow()
    {
        while (true)
        {
            // Move up
            yield return MoveToPosition(originalPosition + Vector2.up * moveDistance);
            // Move down
            yield return MoveToPosition(originalPosition - Vector2.up * moveDistance);
        }
    }

    private IEnumerator MoveToPosition(Vector2 targetPosition)
    {
        // Exit early if arrow component is missing
        if (arrow == null) yield break;
        
        float startTime = Time.time;
        Vector2 startPosition = arrow.anchoredPosition;
        float journeyLength = Vector2.Distance(startPosition, targetPosition);
        float distanceCovered = 0;
        
        while (distanceCovered < journeyLength)
        {
            float timeElapsed = Time.time - startTime;
            distanceCovered = timeElapsed * moveSpeed;
            float fractionOfJourney = Mathf.Clamp01(distanceCovered / journeyLength);
            
            arrow.anchoredPosition = Vector2.Lerp(startPosition, targetPosition, fractionOfJourney);
            yield return null;
        }
        
        // Ensure we reach the exact target position
        arrow.anchoredPosition = targetPosition;
    }
}