using UnityEngine;
using UnityEngine.UI;

public class DirectionIndicator : MonoBehaviour
{
    public GameObject boyPlayer;
    public GameObject girlPlayer;
    public Transform[] targets; // Changed to array for multiple targets
    public float offset = 1f;
    public Sprite arrowSprite;
    public Button toggleButton;
    public float disappearDistance = 2f;
    public float contactDistance = 0.5f;

    private SpriteRenderer spriteRenderer;
    private bool isVisible = true;
    private bool[] hasContactedTargets; // Array to track contact with each target
    private float alpha = 1f;
    private int currentTargetIndex = 0; // Track current target

    void Start()
    {
        spriteRenderer = gameObject.GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
        }

        spriteRenderer.sprite = arrowSprite;

        if (toggleButton != null)
        {
            toggleButton.onClick.AddListener(ToggleVisibility);
        }
        else
        {
            Debug.LogWarning("Toggle button not assigned!");
        }

        // Initialize contact tracking array
        hasContactedTargets = new bool[targets.Length];
    }

    void Update()
    {
        Transform activePlayer = GetActivePlayer();

        if (activePlayer == null || targets == null || targets.Length == 0 || currentTargetIndex >= targets.Length)
        {
            spriteRenderer.enabled = false;
            return;
        }

        Transform currentTarget = targets[currentTargetIndex];
        if (currentTarget == null)
        {
            spriteRenderer.enabled = false;
            return;
        }

        float distanceToTarget = Vector2.Distance(activePlayer.position, currentTarget.position);

        if (distanceToTarget <= contactDistance)
        {
            hasContactedTargets[currentTargetIndex] = true;
            // Move to next target
            currentTargetIndex++;
            if (currentTargetIndex >= targets.Length)
            {
                spriteRenderer.enabled = false;
                return;
            }
        }

        // Point to current target
        Vector2 direction = (currentTarget.position - activePlayer.position).normalized;
        transform.position = (Vector2)activePlayer.position + direction * offset;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle);

        // Handle fade out when close to target
        if (distanceToTarget < disappearDistance)
        {
            alpha = Mathf.Clamp01((distanceToTarget - contactDistance) / (disappearDistance - contactDistance));
        }
        else
        {
            alpha = 1f;
        }

        Color spriteColor = spriteRenderer.color;
        spriteColor.a = alpha;
        spriteRenderer.color = spriteColor;

        spriteRenderer.enabled = isVisible && alpha > 0;
    }

    public void ToggleVisibility()
    {
        isVisible = !isVisible;
    }

    private Transform GetActivePlayer()
    {
        if (boyPlayer.activeSelf && !girlPlayer.activeSelf)
        {
            return boyPlayer.transform;
        }
        else if (!boyPlayer.activeSelf && girlPlayer.activeSelf)
        {
            return girlPlayer.transform;
        }
        else
        {
            Debug.LogWarning("Invalid player selection. Please ensure only one player is active.");
            return null;
        }
    }

    // Method to set targets programmatically
    public void SetTargets(Transform[] newTargets)
    {
        targets = newTargets;
        hasContactedTargets = new bool[targets.Length];
        currentTargetIndex = 0;
        isVisible = true;
       // spriteRenderer.enabled = true;
    }

    // Method to manually advance to next target
    public void AdvanceToNextTarget()
    {
        if (currentTargetIndex < targets.Length - 1)
        {
            currentTargetIndex++;
            isVisible = true;
            spriteRenderer.enabled = true;
        }
    }
}
