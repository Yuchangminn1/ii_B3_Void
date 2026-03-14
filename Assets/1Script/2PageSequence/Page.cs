using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UIElements;

[RequireComponent(typeof(SetColorA))]
[RequireComponent(typeof(CanvasGroup))]

public class Page : MonoBehaviour
{
    [Header("같이 보여줄 다른 캔버스 또는 오브젝트")]
    public GameObject[] pairObject;
    public int nextPageNumber = -1;

    [Header("시작 이벤트")]
    public UnityEvent onStartPage;
    [Header("리셋 이벤트")]
    public UnityEvent onReset;
    [Header("종료 이벤트")]
    public UnityEvent onEndPage;
    // public SubPageController SubPageController;

    [SerializeField] private int pageNumber;

    public int PageNumber { get { return pageNumber; } set { pageNumber = value; } }

    Coroutine mainCoroutine;

    public SequenceScript[] sequenceScripts;

    SequenceCanvasFade sequenceCanvasFade;

    CanvasGroup _canvasGroup;

    List<CanvasGroup> _pairCanvasGroup = new List<CanvasGroup>();


    public List<CanvasGroup> Page_CanvasGroup()
    {
        return _pairCanvasGroup;
    }




    /// <summary>
    /// 현재 순서의 스퀀스 실행 인스펙터에서 시퀀스 상속받는 스크립트 add하여 사용
    /// </summary>
    public int currentindex;
    public int CurrentIndex
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
                    PageController.Instance.ChangePage(pageNumber + 1);

                }

                else
                {
                    Debug.Log($"페이지 이동: {pageNumber} - > " + (nextPageNumber));
                    PageController.Instance.ChangePage(nextPageNumber);

                }
            }

        }
    }

    void Awake()
    {
        _canvasGroup = GetComponent<CanvasGroup>();
        _pairCanvasGroup.Add(_canvasGroup);

        if (pairObject.Length > 0)
        {
            foreach (var pair in pairObject)
            {
                CanvasGroup cg = pair.GetComponent<CanvasGroup>();
                if (cg != null)
                    _pairCanvasGroup.Add(cg);

            }
        }
    }



    void Start()
    {
    }

    public void Initialize()
    {
        sequenceScripts = GetComponentsInChildren<SequenceScript>();
        sequenceScripts = sequenceScripts.OrderBy(script => script.currentIndex).ToArray();
        sequenceCanvasFade = GetComponent<SequenceCanvasFade>();
    }

    public void PairSetActive(bool _tf)
    {
        foreach (GameObject _object in pairObject)
        {
            _object.SetActive(_tf);
        }
    }
    public void OpenPage()
    {
        PairSetActive(true);
        ResetSequence();
        onStartPage?.Invoke();
    }
    public void ClosePage()
    {
        onEndPage?.Invoke();

    }

    public void PageDown()
    {
        PairSetActive(false);
        gameObject.SetActive(false);
    }

    void OnDisable()
    {
        _canvasGroup.alpha = 0f;
    }

    // IEnumerator IsFadeIn()
    // {
    //     yield return new WaitForSeconds(0.05f);

    //     while (sequenceCanvasFade.isPlaying == false)
    //     {
    //         yield return new WaitForSeconds(0.1f);
    //     }


    //     PairSetActive(false);
    //     gameObject.SetActive(false);
    // }

    public void StopPage()
    {
        StopAllCoroutines();
        PairSetActive(false);
        gameObject.SetActive(false);
    }

    public void ResetSequence()
    {
        CurrentIndex = 0;
    }

    public void RunSequence()
    {
        if (mainCoroutine != null)
        {
            StopCoroutine(mainCoroutine);
            mainCoroutine = null;
        }

        if (sequenceScripts.Length == 0) return;

        mainCoroutine = StartCoroutine(sequenceScripts[currentindex].StartSequence());

    }

    /// <summary>
    /// 디버그용 함수 
    /// </summary>
    public void CurrentIndexTriggerON()
    {
        if (sequenceScripts == null || sequenceScripts.Length < 1) return;
        sequenceScripts[currentindex].TriggerOn();
    }



}