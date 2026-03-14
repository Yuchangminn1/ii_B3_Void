using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class PageButton : MonoBehaviour
{

    [Header("트리거 할 시퀀스 스크립트")]
    public SequenceScript sequenceScript;


    [Header("페이지 시작시 보일지")]
    public bool isStartActive;

    float _fadeDuration = 0.5f;

    Graphic _currentGraphics;

    public Button _button;
    //딜레이 안주면 트리거 순서 꼬임
    WaitForSeconds delayWaitForSecond = new WaitForSeconds(0.5f);

    WaitForSeconds enableDelay = new WaitForSeconds(1f);

    Text text;

    public UnityEvent onClickEvent;


    void Awake()
    {
        _currentGraphics = GetComponent<Graphic>();

        _button = GetComponent<Button>();

        text = GetComponentInChildren<Text>();


    }
    void OnEnable()
    {
        _currentGraphics.raycastTarget = false;
        FadeManager.Instance.SetAlphaZero(_currentGraphics);
        if (isStartActive)
        {
            StartCoroutine(EnableButtonDelay());
        }


    }

    IEnumerator EnableButtonDelay()
    {
        yield return enableDelay;

        _currentGraphics.raycastTarget = true;
        // _currentGraphics.color = Color.white;

        FadeManager.Instance.SetAlphaOne(_currentGraphics);

    }

    void Start()
    {
        _button.onClick.AddListener(StartTrigger);
    }

    void StartTrigger()
    {
        Debug.Log("Page Button Clicked");
        onClickEvent?.Invoke();
        sequenceScript?.TriggerOn();

        _currentGraphics.raycastTarget = false;
        FadeManager.Instance.TargetFade(_currentGraphics, 0f, _fadeDuration);
        if (text != null)

            FadeManager.Instance.TargetFade(text, 0f, _fadeDuration);

        SoundManager.Instance.PlayEffectSound(EffectSoundNum.ButtonSound, 3f);

    }
    IEnumerator SetRayCastTargetToDelay(Graphic graphic)
    {
        yield return delayWaitForSecond;
        FadeManager.Instance.TargetFade(graphic, 1f, _fadeDuration);
        if (text != null)
            FadeManager.Instance.TargetFade(text, 1f, _fadeDuration);

        yield return delayWaitForSecond;
        graphic.raycastTarget = true;

    }

    public void EnableButton()
    {
        StartCoroutine(SetRayCastTargetToDelay(_currentGraphics));
    }
}
