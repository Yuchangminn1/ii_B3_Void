using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MaskSave : MonoBehaviour
{
    RectTransform rectTransform;

    [Header("Movement Settings")]
    public float initialSpeed = 10f;   // 초기 정밀 이동 속도
    public float maxSpeed = 300f;      // 최대 이동 속도
    public float acceleration = 500f;  // 가속도

    public bool isLeft = true;         // Left 모드면 Z키로 토글, Right 모드면 X키로 토글
    private bool isMovementEnabled = false; // 움직임 활성화 여부

    private float currentSpeed;

    // Start is called before the first frame update
    void Start()
    {
        rectTransform = GetComponent<RectTransform>();
        currentSpeed = initialSpeed;
        isMovementEnabled = false;
    }

    // Update is called once per frame
    void Update()
    {
        // 토글 키 처리
        if (isLeft)
        {
            if (Input.GetKeyDown(KeyCode.Z))
            {
                isMovementEnabled = !isMovementEnabled;
                Debug.Log($"[MaskSave] Left Movement {(isMovementEnabled ? "Enabled" : "Disabled")}");
            }
        }
        else
        {
            if (Input.GetKeyDown(KeyCode.X))
            {
                isMovementEnabled = !isMovementEnabled;
                Debug.Log($"[MaskSave] Right Movement {(isMovementEnabled ? "Enabled" : "Disabled")}");
            }
        }

        if (!isMovementEnabled) return;

        // WASD 이동
        float h = Input.GetAxisRaw("Horizontal"); // A, D, Left, Right
        float v = Input.GetAxisRaw("Vertical");   // W, S, Up, Down

        Vector2 direction = new Vector2(h, v).normalized;

        if (direction.sqrMagnitude > 0.01f)
        {
            // 키를 누르고 있으면 가속
            currentSpeed += acceleration * Time.deltaTime;
            currentSpeed = Mathf.Min(currentSpeed, maxSpeed);

            if (rectTransform != null)
            {
                rectTransform.anchoredPosition += direction * currentSpeed * Time.deltaTime;
            }
        }
        else
        {
            // 키를 떼면 속도 초기화 (정밀 조정을 위해)
            currentSpeed = initialSpeed;
        }

        // V 키: 현재 위치 출력
        if (Input.GetKeyDown(KeyCode.V))
        {
            if (rectTransform != null)
            {
                Debug.Log($"Current Position (Anchored): {rectTransform.anchoredPosition}");
            }
        }
    }
}
