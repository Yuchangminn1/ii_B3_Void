using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class BlinkEyesScript : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private RawImage targetRawImage;

    [Header("Blink")]
    [SerializeField, Min(0.02f)] private float blinkDuration = 0.14f;
    [SerializeField, Range(0f, 1f)] private float closedScaleY = 0.03f;
    [SerializeField] private bool autoBlink = true;
    [SerializeField, Min(0.1f)] private float blinkInterval = 2.5f;

    private RectTransform targetRect;
    private Vector3 originalScale;
    private float blinkTimer;
    private bool isBlinking;

    private void Awake()
    {
        if (targetRawImage == null)
        {
            targetRawImage = GetComponent<RawImage>();
        }

        if (targetRawImage != null)
        {
            targetRect = targetRawImage.rectTransform;
            originalScale = targetRect.localScale;
        }
    }

    private void OnEnable()
    {
        blinkTimer = blinkInterval;
        if (targetRect != null)
        {
            originalScale = targetRect.localScale;
        }
    }

    private void Update()
    {
        if (!autoBlink || isBlinking || targetRect == null)
        {
            return;
        }

        blinkTimer -= Time.deltaTime;
        if (blinkTimer <= 0f)
        {
            TriggerBlink();
            blinkTimer = blinkInterval;
        }
    }

    public void TriggerBlink()
    {
        if (targetRect == null || isBlinking)
        {
            return;
        }

        StartCoroutine(BlinkRoutine());
    }

    private IEnumerator BlinkRoutine()
    {
        isBlinking = true;
        float halfDuration = blinkDuration * 0.5f;
        float t = 0f;



        while (t < halfDuration)
        {
            t += Time.deltaTime;
            float ratio = Mathf.Clamp01(t / halfDuration);
            float y = Mathf.Lerp(originalScale.y, closedScaleY, ratio);
            targetRect.localScale = new Vector3(originalScale.x, y, originalScale.z);
            yield return null;
        }

        t = 0f;
        while (t < halfDuration)
        {
            t += Time.deltaTime;
            float ratio = Mathf.Clamp01(t / halfDuration);
            float y = Mathf.Lerp(closedScaleY, originalScale.y, ratio);
            targetRect.localScale = new Vector3(originalScale.x, y, originalScale.z);
            yield return null;

        }

        targetRect.localScale = originalScale;



        isBlinking = false;
    }
}
