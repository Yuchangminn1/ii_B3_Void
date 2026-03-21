using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(CanvasGroup))]

public class PageBase : MonoBehaviour
{

    public int nextPageNumber = -1;

    public int PageNumber { get { return pageNumber; } set { pageNumber = value; } }
    [SerializeField] private int pageNumber;

    public SequenceScript[] sequenceScripts;

    [Header("시작 이벤트")]
    public UnityEvent onStartPage;
    [Header("리셋 이벤트")]
    public UnityEvent onReset;
    [Header("종료 이벤트")]
    public UnityEvent onEndPage;

    SetUpCoroutine _pageController;


    Coroutine _mainCoroutine;


    CanvasGroup _canvasGroup;


    WaitForSeconds _fadeDelay = null;

    WaitForFixedUpdate _waitForFixedUpdate = new WaitForFixedUpdate();


    public bool isCutIn = true;
    public bool isCutOut = true;



    /// <summary>
    /// 현재 순서의 스퀀스 실행 인스펙터에서 시퀀스 상속받는 스크립트 add하여 사용
    /// </summary>
    public int currentindex;
    public virtual int CurrentIndex
    {
        get { return currentindex; }
        set
        {
            if (sequenceScripts.Length == 0) return;

            if (sequenceScripts.Length > value)
            {
                currentindex = value;

                RunSequence();

            }
            else
            {
                if (nextPageNumber == -1)
                {
                    Debug.Log($"페이지 이동: {pageNumber} - > " + (pageNumber + 1));
                    _pageController.ChangePage(pageNumber + 1);

                }

                else
                {
                    Debug.Log($"페이지 이동: {pageNumber} - > " + (nextPageNumber));
                    _pageController.ChangePage(nextPageNumber);

                }
            }

        }
    }

    protected virtual void Awake()
    {
        _canvasGroup = GetComponent<CanvasGroup>();
    }

    protected virtual void Start()
    {
        _pageController = GetComponentInParent<SetUpCoroutine>();
    }


    public virtual void Initialize()
    {
        sequenceScripts = GetComponentsInChildren<SequenceScript>();

        sequenceScripts = sequenceScripts.OrderBy(script => script.CurrentIndex).ToArray();

        for (int i = 0; i < sequenceScripts.Length; i++)
        {
            //트리거 시 다음 시퀀스로 이동 콜백 연결
            sequenceScripts[i].AddNextSequenceCallback(NextSequence);
        }

        if (_pageController.FadeDuration > 0)
            _fadeDelay = new WaitForSeconds(_pageController.FadeDuration);
    }
    public void NextSequence()
    {
        CurrentIndex++;
    }
    public virtual void OpenPage()
    {
        Reset();
        onStartPage?.Invoke();

        if (isCutIn)
            FadeManager.Instance.SetAlphaOne(_canvasGroup);
        else
            CanvasFadeIn();

    }
    public virtual void ClosePage()
    {
        onEndPage?.Invoke();
        if (isCutOut)
            FadeManager.Instance.SetAlphaZero(_canvasGroup);
        else
            CanvasFadeOut();
    }

    public void PageDown()
    {
        gameObject.SetActive(false);
    }

    void OnDisable()
    {
        _canvasGroup.alpha = 0f;
    }

    public CanvasGroup GetCanvasGroup()
    {
        return _canvasGroup;
    }


    public virtual void ResetValue()
    {
        CurrentIndex = 0;
    }




    public void RunSequence()
    {

        if (_mainCoroutine != null)
        {
            StopCoroutine(_mainCoroutine);
            _mainCoroutine = null;
        }

        if (sequenceScripts.Length == 0) return;

        _mainCoroutine = StartCoroutine(sequenceScripts[currentindex].StartSequence());

    }

    public void Reset()
    {
        if (_fadeDelay == null)
            return;
        ResetValue();
    }

    public IEnumerator CurrentResetCoroutine()
    {
        if (isCutOut)
            FadeManager.Instance.SetAlphaZero(_canvasGroup);
        else
            CanvasFadeOut();
        yield return _fadeDelay;

        yield return _waitForFixedUpdate;

        ResetValue();
        if (isCutIn)
            FadeManager.Instance.SetAlphaOne(_canvasGroup);
        else
            CanvasFadeIn();
        yield return _fadeDelay;


    }



    /// <summary>
    /// 디버그용 함수 
    /// </summary>
    public void CurrentIndexTriggerON()
    {
        if (sequenceScripts == null || sequenceScripts.Length < 1) return;
        sequenceScripts[currentindex].TriggerOn();
    }



    public void CanvasFadeIn()
    {
        FadeManager.Instance.TargetFade(GetCanvasGroup(), 1f, _pageController.FadeDuration);
    }

    public void CanvasFadeOut()
    {
        FadeManager.Instance.TargetFade(GetCanvasGroup(), 0f, _pageController.FadeDuration);
    }



}