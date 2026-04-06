using UnityEngine;
using UnityEngine.UI;

public class MaskingObject : MonoBehaviour
{
    private enum InputReadMode
    {
        GetKeyDown,
        GetKey
    }

    [Header("Targets")]
    [SerializeField] private RawImage[] targetRawImages;

    [Header("Control Keys")]
    [SerializeField] private KeyCode switchTargetKey = KeyCode.B;
    [SerializeField] private KeyCode toggleInputModeKey = KeyCode.T;
    [SerializeField] private KeyCode moveLeftKey = KeyCode.LeftArrow;
    [SerializeField] private KeyCode moveRightKey = KeyCode.RightArrow;
    [SerializeField] private KeyCode rotateUpKey = KeyCode.UpArrow;
    [SerializeField] private KeyCode rotateDownKey = KeyCode.DownArrow;
    [SerializeField] private KeyCode increaseWidthKey = KeyCode.E;
    [SerializeField] private KeyCode decreaseWidthKey = KeyCode.Q;
    [SerializeField] private KeyCode debugAllRawImagesKey = KeyCode.F;

    [Header("Control Values")]
    [SerializeField] private float moveStep = 1f;
    [SerializeField] private float rotateStep = 5f;
    [SerializeField] private float widthStep = 10f;
    [SerializeField] private float minWidth = 20f;
    [SerializeField] private float maxWidth = 2000f;

    [Header("Input Mode")]
    [SerializeField] private InputReadMode inputReadMode = InputReadMode.GetKeyDown;

    private int currentTargetIndex;

    private void Update()
    {
        if (Input.GetKeyDown(debugAllRawImagesKey))
        {
            DebugAllRawImages();
        }

        if (targetRawImages == null || targetRawImages.Length == 0)
        {
            return;
        }

        if (Input.GetKeyDown(toggleInputModeKey))
        {
            inputReadMode = inputReadMode == InputReadMode.GetKeyDown
                ? InputReadMode.GetKey
                : InputReadMode.GetKeyDown;

            Debug.Log($"[MaskingObject] Input mode changed to: {inputReadMode}");
        }

        if (Input.GetKeyDown(switchTargetKey))
        {
            currentTargetIndex = (currentTargetIndex + 1) % targetRawImages.Length;
            Debug.Log($"[MaskingObject] Selected RawImage index: {currentTargetIndex}");
        }

        RawImage selectedRawImage = targetRawImages[currentTargetIndex];
        if (selectedRawImage == null)
        {
            return;
        }

        RectTransform rectTransform = selectedRawImage.rectTransform;

        if (IsControlPressed(moveLeftKey))
        {
            rectTransform.anchoredPosition += Vector2.left * moveStep;
        }

        if (IsControlPressed(moveRightKey))
        {
            rectTransform.anchoredPosition += Vector2.right * moveStep;
        }

        if (IsControlPressed(rotateUpKey))
        {
            rectTransform.Rotate(0f, 0f, rotateStep);
        }

        if (IsControlPressed(rotateDownKey))
        {
            rectTransform.Rotate(0f, 0f, -rotateStep);
        }

        if (IsControlPressed(increaseWidthKey))
        {
            ChangeWidth(rectTransform, widthStep);
        }

        if (IsControlPressed(decreaseWidthKey))
        {
            ChangeWidth(rectTransform, -widthStep);
        }
    }

    private bool IsControlPressed(KeyCode key)
    {
        return inputReadMode == InputReadMode.GetKeyDown
            ? Input.GetKeyDown(key)
            : Input.GetKey(key);
    }

    private void ChangeWidth(RectTransform rectTransform, float delta)
    {
        Vector2 size = rectTransform.sizeDelta;
        size.x = Mathf.Clamp(size.x + delta, minWidth, maxWidth);
        rectTransform.sizeDelta = size;
    }

    private void DebugAllRawImages()
    {
        if (targetRawImages.Length == 0)
        {
            Debug.Log("[MaskingObject] No RawImage found in scene.");
            return;
        }

        Debug.Log($"[MaskingObject] RawImage count: {targetRawImages.Length}");

        for (int i = 0; i < targetRawImages.Length; i++)
        {
            RawImage rawImage = targetRawImages[i];
            if (rawImage == null)
            {
                continue;
            }

            RectTransform rectTransform = rawImage.rectTransform;
            Vector2 anchoredPosition = rectTransform.anchoredPosition;
            Vector2 sizeDelta = rectTransform.sizeDelta;

            Debug.Log(
                $"[MaskingObject] [{i}] {rawImage.name} | active={rawImage.gameObject.activeInHierarchy} | anchoredPos={anchoredPosition} | sizeDelta={sizeDelta}",
                rawImage.gameObject);
        }
    }
}
