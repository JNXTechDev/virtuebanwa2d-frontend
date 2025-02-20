using UnityEngine;
using System.Collections;

public class ArrowFloat : MonoBehaviour
{
    public RectTransform arrow; // Assign your arrow RectTransform here
    public float moveDistance = 10f; // Distance to move up and down
    public float moveSpeed = 1f; // Speed of the movement

    private Vector2 originalPosition;

    void Start()
    {
        if (arrow == null)
        {
            Debug.LogError("Arrow RectTransform is not assigned.");
            return;
        }

        originalPosition = arrow.anchoredPosition;
        StartCoroutine(FloatArrow());
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
        while ((arrow.anchoredPosition - targetPosition).sqrMagnitude > 0.01f)
        {
            arrow.anchoredPosition = Vector2.MoveTowards(arrow.anchoredPosition, targetPosition, moveSpeed * Time.deltaTime);
            yield return null;
        }
    }
}