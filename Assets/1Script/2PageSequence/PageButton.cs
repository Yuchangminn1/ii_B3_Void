using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class PageButton : MonoBehaviour
{

    [Header("트리거 할 시퀀스 스크립트")]
    public SequenceScript sequenceScript;


    [Header("페이지 시작시 보일지")]
    bool isStartActive = false;


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
        _button.interactable = false;
        if (isStartActive)
        {
            StartCoroutine(EnableButtonDelay());
        }


    }

    IEnumerator EnableButtonDelay()
    {
        yield return enableDelay;

        _currentGraphics.raycastTarget = true;
        _button.interactable = true;

        // _currentGraphics.color = Color.white;

        FadeManager.Instance.SetAlphaOne(_currentGraphics);

    }

    void Start()
    {
        _button.onClick.AddListener(SST);
    }

    public void SST()
    {
        sequenceScript.TriggerOn();
    }

    void StartTrigger()
    {
        Debug.Log("Page Button Clicked");
        onClickEvent?.Invoke();
        sequenceScript.TriggerOn();

        _currentGraphics.raycastTarget = false;
        FadeManager.Instance.TargetFade(_currentGraphics, 0f);
        if (text != null)

            FadeManager.Instance.TargetFade(text, 0f);

        SoundManager.Instance.PlayEffectSound(EffectSoundNum.ConfirmSound, 3f);

    }
    IEnumerator SetRayCastTargetToDelay(Graphic graphic)
    {
        yield return delayWaitForSecond;
        FadeManager.Instance.TargetFade(graphic, 1f);
        if (text != null)
            FadeManager.Instance.TargetFade(text, 1f);
        graphic.raycastTarget = true;
        _button.interactable = true;

    }

    public void EnableButton()
    {
        StartCoroutine(SetRayCastTargetToDelay(_currentGraphics));
    }
}
