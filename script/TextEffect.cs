using UnityEngine;
using UnityEngine.UI; // For regular Text
using TMPro; // For TextMeshPro
using System.Collections; // for IEnumerator

public class TextFadeEffect : MonoBehaviour
{
    public Text uiText; // For Unity's built-in Text
    public TextMeshProUGUI tmpText; // For TextMeshPro
    public float fadeDuration = 1f; // Time it takes to fade in/out

    private bool isFadingOut = true; // Whether the text is fading out

    private void Start()
    {
        // Ensure one of the text components is assigned
        if (uiText == null && tmpText == null)
        {
            Debug.LogError("Please assign a Text or TextMeshPro component.");
            return;
        }

        // Start the fade coroutine
        StartCoroutine(FadeText());
    }

    private IEnumerator FadeText()
    {
        while (true)
        {
            float elapsedTime = 0f;
            Color textColor = uiText != null ? uiText.color : tmpText.color;

            // Calculate start and target alpha
            float startAlpha = isFadingOut ? 1f : 0f;
            float targetAlpha = isFadingOut ? 0f : 1f;

            // Perform the fade
            while (elapsedTime < fadeDuration)
            {
                elapsedTime += Time.deltaTime;
                float newAlpha = Mathf.Lerp(startAlpha, targetAlpha, elapsedTime / fadeDuration);

                if (uiText != null)
                {
                    uiText.color = new Color(textColor.r, textColor.g, textColor.b, newAlpha);
                }
                else if (tmpText != null)
                {
                    tmpText.color = new Color(textColor.r, textColor.g, textColor.b, newAlpha);
                }

                yield return null;
            }

            // Switch the fade direction
            isFadingOut = !isFadingOut;

            // Wait for one second before starting the next fade
            yield return new WaitForSeconds(1f);
        }
    }
}
