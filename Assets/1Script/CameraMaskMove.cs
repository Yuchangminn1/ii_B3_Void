using UnityEngine;

public class CameraMaskMove : MonoBehaviour
{
    private RectTransform _rectTransform;
    [SerializeField] private float moveStep = 1f;

    private void Awake()
    {
        _rectTransform = GetComponent<RectTransform>();

        if (_rectTransform == null)
        {
            Debug.LogError("CameraMaskMove: RectTransform component not found.");
        }
    }

    private void Update()
    {
        if (_rectTransform == null)
        {
            return;
        }

        Vector2 delta = Vector2.zero;

        if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            delta.y += moveStep;
        }
        else if (Input.GetKeyDown(KeyCode.DownArrow))
        {
            delta.y -= moveStep;
        }
        else if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            delta.x -= moveStep;
        }
        else if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            delta.x += moveStep;
        }

        if (delta != Vector2.zero)
        {
            _rectTransform.anchoredPosition += delta;
            Debug.Log($"Current RectTransform Position: {_rectTransform.anchoredPosition}");
        }
    }
}
