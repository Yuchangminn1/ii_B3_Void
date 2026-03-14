using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class Slider : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    [SerializeField] SequenceScript _sequenceScript;

    public Image FillImage;

    public RectTransform Handle;

    // 범위를 0.05 ~ 0.95로 설정
    [Range(0f, 1f)] public float MinValue = 0.05f;
    [Range(0f, 1f)] public float MaxValue = 0.95f;

    // 스냅 지속시간(초)
    public float SnapDuration = 0.2f;

    private bool _isDragging;

    private bool _isSnapping;
    private float _snapTarget;
    private float _snapStartValue;
    private float _snapSpeed; // 등속 이동 속도 (fillAmount 단위/sec)

    void Awake()
    {
        // Inspector에 저장된 값이 0.1/0.9라면 0.05/0.95로 강제 보정 (Play 모드 시)
        if (Mathf.Abs(MinValue - 0.1f) < 0.001f) MinValue = 0.05f;
        if (Mathf.Abs(MaxValue - 0.9f) < 0.001f) MaxValue = 0.95f;
    }

    void OnValidate()
    {
        MinValue = Mathf.Clamp01(MinValue);
        MaxValue = Mathf.Clamp01(MaxValue);
        if (MaxValue < MinValue)
        {
            float tmp = MaxValue;
            MaxValue = MinValue;
            MinValue = tmp;
        }
    }

    void OnEnable()
    {
        FillImage.fillAmount = 0.05f;
        UpdateHandlePosition();
    }

    void Update()
    {
        if (_isSnapping)
        {
            // 등속으로 목표로 이동
            FillImage.fillAmount = Mathf.MoveTowards(FillImage.fillAmount, _snapTarget, _snapSpeed * Time.deltaTime);
            UpdateHandlePosition();

            if (Mathf.Approximately(FillImage.fillAmount, _snapTarget) || Mathf.Abs(FillImage.fillAmount - _snapTarget) < 0.0001f)
            {
                FillImage.fillAmount = _snapTarget;

                _isSnapping = false;

                _sequenceScript?.TriggerOn();

            }
        }
    }


    public void OnPointerDown(PointerEventData eventData)
    {
        _isDragging = true;
        _isSnapping = false; // 드래그 시작하면 스냅 중지
        UpdateFillFromPointer(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (_isDragging)
            UpdateFillFromPointer(eventData);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        _isDragging = false;

        // 지정된 4개 구간으로 스냅: 0.05, 0.35, 0.7, 0.95
        float[] targets = new float[] { 0.05f, 0.35f, 0.61f, 0.95f };

        float current = FillImage.fillAmount;
        float closest = targets[0];
        float minDiff = Mathf.Abs(current - closest);

        for (int i = 1; i < targets.Length; i++)
        {
            float diff = Mathf.Abs(current - targets[i]);
            if (diff < minDiff)
            {
                minDiff = diff;
                closest = targets[i];
            }
        }

        _snapTarget = closest;

        _snapStartValue = FillImage.fillAmount;
        float distance = Mathf.Abs(_snapTarget - _snapStartValue);
        _snapSpeed = distance / Mathf.Max(0.0001f, SnapDuration); // 목표까지 등속으로 도달하도록 속도 설정
        _isSnapping = true;
    }

    private void UpdateFillFromPointer(PointerEventData eventData)
    {
        RectTransform rt = FillImage.rectTransform;
        Vector2 localPoint;
        Camera cam = eventData.pressEventCamera; // Overlay 캔버스일 경우 null이어야 정상 동작
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(rt, eventData.position, cam, out localPoint))
        {
            float totalWidth = rt.rect.width;
            float handleWidth = Handle.rect.width;
            float usableWidth = Mathf.Max(0.0001f, totalWidth - handleWidth);

            // 왼쪽 끝 핸들 중심 위치(로컬 좌표 기준)
            float left = -totalWidth * rt.pivot.x + handleWidth * 0.5f;

            float normalized = (localPoint.x - left) / usableWidth;
            normalized = Mathf.Clamp01(normalized);

            // normalized(0..1)를 min..max 범위로 매핑
            FillImage.fillAmount = Mathf.Lerp(MinValue, MaxValue, normalized);
            UpdateHandlePosition();
        }
    }

    private void UpdateHandlePosition()
    {
        RectTransform rt = FillImage.rectTransform;
        float totalWidth = rt.rect.width;
        float handleWidth = Handle.rect.width;
        float usableWidth = Mathf.Max(0.0f, totalWidth - handleWidth);

        // 왼쪽 끝 핸들 중심 위치(로컬 좌표 기준)
        float left = -totalWidth * rt.pivot.x + handleWidth * 0.5f;

        float range = Mathf.Max(0.0001f, MaxValue - MinValue);
        float t = Mathf.Clamp01((FillImage.fillAmount - MinValue) / range);

        float x = left + t * usableWidth;
        Handle.localPosition = new Vector2(x, 0f);
    }

}
