using UnityEngine;
using System.Collections;

public class MenuSlider : MonoBehaviour
{
    public RectTransform menuPanel; // Assign your menu panel here
    public float slideDuration = 0.5f; // Duration of the slide animation

    // Set these values based on your desired positions
    public float hiddenLeftOffset = 1161.31f; // Off-screen left position
    public float visibleLeftOffset = 802.16f; // On-screen left position

    private bool isMenuVisible = false;

    public void ToggleMenu()
    {
        if (isMenuVisible)
        {
            StartCoroutine(SlideMenu(hiddenLeftOffset));
        }
        else
        {
            StartCoroutine(SlideMenu(visibleLeftOffset));
        }
        isMenuVisible = !isMenuVisible;
    }

    private IEnumerator SlideMenu(float targetLeftOffset)
    {
        float startLeftOffset = menuPanel.offsetMin.x;
        float elapsedTime = 0f;

        while (elapsedTime < slideDuration)
        {
            float newLeftOffset = Mathf.Lerp(startLeftOffset, targetLeftOffset, elapsedTime / slideDuration);
            menuPanel.offsetMin = new Vector2(newLeftOffset, menuPanel.offsetMin.y);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        menuPanel.offsetMin = new Vector2(targetLeftOffset, menuPanel.offsetMin.y);
    }
}