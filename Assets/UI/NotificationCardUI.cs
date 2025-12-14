using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;

public class NotificationCard : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI messageText;
    [SerializeField] private Image iconImage;
    [SerializeField] private Image backgroundImage;
    [SerializeField] private CanvasGroup canvasGroup;

    [Header("Settings")]
    [SerializeField] private float displayDuration = 3f;
    [SerializeField] private float fadeDuration = 0.5f;

    public void Setup(string text, Sprite icon, Color backgroundColor)
    {
        // 1. Set Text
        messageText.text = text;

        // 2. Set Icon (Hide if null)
        if (icon != null)
        {
            iconImage.sprite = icon;
            iconImage.gameObject.SetActive(true);
        }
        else
        {
            iconImage.gameObject.SetActive(false);
        }

        // 3. Set Color
        if (backgroundImage != null)
        {
            backgroundImage.color = backgroundColor;
        }

        // 4. Start the lifecycle
        StartCoroutine(LifecycleRoutine());
    }

    private IEnumerator LifecycleRoutine()
    {
        // --- ANIMATE IN (Fade In) ---
        canvasGroup.alpha = 0f;
        float elapsed = 0f;
        
        // Optional: Slide in effect could be added here by modifying transform.localPosition
        
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(0f, 1f, elapsed / fadeDuration);
            yield return null;
        }
        canvasGroup.alpha = 1f;

        // --- WAIT ---
        yield return new WaitForSeconds(displayDuration);

        // --- ANIMATE OUT (Fade Out) ---
        elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(1f, 0f, elapsed / fadeDuration);
            yield return null;
        }
        canvasGroup.alpha = 0f;

        // --- DESTROY ---
        Destroy(gameObject);
    }
}