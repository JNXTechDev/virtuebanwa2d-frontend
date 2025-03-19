using UnityEngine;
using System.Collections;

public class ArrowAnimator : MonoBehaviour
{
    public RectTransform arrow; // Assign your arrow RectTransform here
    public float moveDistance = 10f; // Distance to move up and down
    public float moveSpeed = 20f; // Speed of the movement

    private Vector3 startPos;
    private float timer;

    void Start()
    {
        // If no arrow reference is set, use this object's RectTransform
        if (arrow == null)
        {
            arrow = GetComponent<RectTransform>();
        }

        // Store initial position
        startPos = arrow.anchoredPosition3D;
        timer = 0f;
    }

    void Update()
    {
        if (arrow != null)
        {
            // Update timer
            timer += Time.deltaTime * moveSpeed;

            // Calculate offset using a sine wave
            float offset = Mathf.Sin(timer) * moveDistance;

            // Apply offset to create floating effect
            arrow.anchoredPosition3D = startPos + new Vector3(offset, 0, 0);
        }
    }
}