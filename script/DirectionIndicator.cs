using UnityEngine;
using UnityEngine.UI; // Add this for UI components

public class DirectionIndicator : MonoBehaviour
{
    public GameObject boyPlayer;
    public GameObject girlPlayer;
    public Transform target;
    public float offset = 1f;
    public Sprite arrowSprite;
    public Button toggleButton;
    public float disappearDistance = 2f;
    public float contactDistance = 0.5f;

    private SpriteRenderer spriteRenderer;
    private bool isVisible = true;
    private bool hasContactedTarget = false;
    private float alpha = 1f;

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
    }

    void Update()
    {
        Transform activePlayer = GetActivePlayer();

        if (activePlayer == null || target == null)
        {
            spriteRenderer.enabled = false;
            return;
        }

        float distanceToTarget = Vector2.Distance(activePlayer.position, target.position);

        if (distanceToTarget <= contactDistance)
        {
            hasContactedTarget = true;
        }

        if (hasContactedTarget)
        {
            spriteRenderer.enabled = false;
            return;
        }

        Vector2 direction = (target.position - activePlayer.position).normalized;
        transform.position = (Vector2)activePlayer.position + direction * offset;

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle);

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
}
