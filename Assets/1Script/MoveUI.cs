using System.Collections;
using DG.Tweening;
using UnityEngine;

public class MoveUI : MonoBehaviour
{
    public Vector2 startPos;
    public Vector2 targetPos;
    public float speed = 2f;


    [Header("설정")]
    private float duration = 1.0f;
    [SerializeField] private Ease easeType = Ease.InOutSine;

    // 1. 컴포넌트 캐싱
    private RectTransform _rectTransform;
    // 2. 트윈 객체 캐싱 (제어용)
    private Tween _moveTween;

    void Awake()
    {
        // 미리 참조를 캐싱하여 호출 비용 절감
        _rectTransform = GetComponent<RectTransform>();
    }

    void Start()
    {
        PlayFloatingAnimation();
    }

    void OnEnable()
    {
        _moveTween?.Play();
    }

    public void PlayFloatingAnimation()
    {
        // 기존에 실행 중인 트윈이 있다면 제거 (중첩 방지)
        // 트윈 객체를 캐싱해두면 관리가 매우 쉬워집니다.
        if (_moveTween != null && _moveTween.IsActive())
        {
            _moveTween.Pause();
        }

        // 트윈 생성 및 변수에 할당(캐싱)
        _moveTween = _rectTransform.DOAnchorPos(targetPos - startPos, duration)
            .SetRelative(true)
            .SetEase(easeType)
            .SetLoops(-1, LoopType.Yoyo)
            .SetAutoKill(false); // 재사용 가능성을 위해 자동 삭제 방지 (선택 사항)
    }

    // 오브젝트가 비활성화될 때 트윈 정리 (메모리 누수 방지)
    void OnDisable()
    {
        if (_moveTween != null)
        {
            _moveTween.Pause();
        }
    }
}
