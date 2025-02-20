using System.Collections;

using UnityEngine;

public class PopUpManager : MonoBehaviour
{
    [Header("Pop-Up Settings")]
    public GameObject popUpPanel;  // Reference to the panel (assign it in the inspector)
    public float animationDuration = 0.5f;  // Time for the pop-up animation

    private CanvasGroup canvasGroup;

    private void Start()
    {
        canvasGroup = popUpPanel.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            // If the panel doesn't have a CanvasGroup, add one for fade effect
            canvasGroup = popUpPanel.AddComponent<CanvasGroup>();
        }
        popUpPanel.SetActive(false);  // Start with the panel hidden
    }

    public void ShowPopUp()
    {
        popUpPanel.SetActive(true);
        StartCoroutine(FadeInPanel());
    }

    private IEnumerator FadeInPanel()
    {
        float elapsedTime = 0f;

        // Set initial alpha to 0 to start from invisible
        canvasGroup.alpha = 0f;

        while (elapsedTime < animationDuration)
        {
            elapsedTime += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(0f, 1f, elapsedTime / animationDuration);
            yield return null;
        }

        canvasGroup.alpha = 1f;  // Ensure it's fully visible at the end
    }

    public void HidePopUp()
    {
        StartCoroutine(FadeOutPanel());
    }

    private IEnumerator FadeOutPanel()
    {
        float elapsedTime = 0f;

        while (elapsedTime < animationDuration)
        {
            elapsedTime += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(1f, 0f, elapsedTime / animationDuration);
            yield return null;
        }

        canvasGroup.alpha = 0f;
        popUpPanel.SetActive(false);  // Deactivate after fading out
    }
}
