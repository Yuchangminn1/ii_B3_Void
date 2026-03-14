using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class PageController : MonoBehaviour
{
    //싱글턴
    #region Singleton
    private static PageController instance;

    public static PageController Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindFirstObjectByType<PageController>();

                if (instance == null)
                {
                    GameObject singletonObject = new GameObject("PageController");
                    instance = singletonObject.AddComponent<PageController>();
                }
            }
            return instance;
        }
    }
    #endregion
    #region default

    [Space]
    [Space]
    [Space]
    [Header("-----------------Debug Mode----------------")]



    [SerializeField] private int openingPage = 0;

    [Header("---------------------------------------------")]
    [SerializeField]
    [Tooltip("Pages 오브젝트 내부에 추가된 모든 Page 오브젝트")]
    public Page[] Pages;




    [SerializeField]
    [Tooltip("VideoPlayers 오브젝트 내부에 추가된 모든 Page 오브젝트")]
    private GameObject[] videos;

    public GameObject[] SettingPages;

    // [SerializeField] private Page currentPage;

    [SerializeField] private int _ncurrentPage = 0;
    //[SerializeField] private bool isTutorial = false;

    // [SerializeField] UITouchSound uiTouchSound;



    #endregion

    [Space]
    [Space]
    [Space]
    [Header("-----------프로그램마다 필요한 설정-----------")]
    [SerializeField]
    public Action<int> OnPageChange; //페이지 변경

    public Action<Action> OnPageRequest; //페이지 변경


    Action _onRestRequest; //변수 리셋
    public Action OnUpdateRequest; //업데이트 (조건확인)

    //처음 초기화 기다리기
    float setupDelayTime = 5f;
    private WaitForSeconds SetupDelay;

    private float _fadeDuration = 0.5f;
    private WaitForSeconds _fadeDelay;

    private WaitForSeconds _waitDelay = new WaitForSeconds(0.05f);

    Coroutine _pageOpenCoroutine = null;


    public int CurrentPage
    {
        get { return _ncurrentPage; }
        set { CheckCondition(value); }
    }

    private void CheckCondition(int value)
    {
        if (_pageOpenCoroutine != null) return;
        if (value >= Pages.Length)
        {
            value = 0;
        }
        if (_ncurrentPage == 0)
        {
            ;
        }


        if (OnPageRequest != null)
        {
            OnPageRequest.Invoke(() => OpenPage(value));
        }

        else
        {
            OpenPage(value);
        }
    }

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }

        SetupDelay = new WaitForSeconds(setupDelayTime);
        //  uiTouchSound = GetComponent<UITouchSound>();
        _fadeDelay = new WaitForSeconds(_fadeDuration);



    }

    public void AddResetAction(Action resetAction)
    {
        _onRestRequest += resetAction;
    }

    void Start()
    {
        GameManager.Instance?.ActionAddProgramStart(StartPrograms);
    }

    public void StartPrograms()
    {
        foreach (GameObject settingPage in SettingPages)
        {
            settingPage.SetActive(true);
        }
        StartCoroutine(WaitForStart());
    }

    IEnumerator WaitForStart()
    {
        yield return SetupDelay;
        Pages = GetComponentsInChildren<Page>();
        int temp = 0;
        foreach (Page page in Pages)
        {
            page.Initialize();
            page.PageNumber = temp;
            temp++;
        }
        CloseAllPages();

        foreach (GameObject settingPage in SettingPages)
        {
            settingPage.SetActive(false);
        }

        if (GameManager.Instance.IsDebugMode)
        {
            CurrentPage = openingPage;
        }
        else
        {
            CurrentPage = 0;
        }

        GameManager.Instance.IsStarted = true;

    }


    void Update()
    {
        //OpenPage - > CurrentPage프로퍼티 호출로 변경

        if (GameManager.Instance.IsDebugMode)
        {
            if (Input.inputString.Length > 0)
            {
                char inputChar = Input.inputString[0];

                if (char.IsDigit(inputChar))
                {
                    CurrentPage = inputChar - '0';
                }
            }
        }
        //페이지 넘기는 조건들 가진 스크립트 업데이트
        if (OnUpdateRequest == null) return;
        OnUpdateRequest?.Invoke();
    }
    public void NextSequence()
    {
        if (Pages[CurrentPage] != null)
        {
            Pages[CurrentPage].CurrentIndex++;
        }
        else
        {
            Debug.Log("pageSequenceController Is Null");
        }
    }
    private void Reset()
    {
        _onRestRequest?.Invoke();
    }

    void OpenPage(int pageNum)
    {
        if (_pageOpenCoroutine == null)
            _pageOpenCoroutine = StartCoroutine(OpenPageCoroutine(pageNum));
    }

    IEnumerator OpenPageCoroutine(int pageNum)
    {
        if (pageNum == 0)
        {
            SoundManager.Instance.MuteBGM();
        }
        if (Pages.Length > pageNum)
        {
            Pages[_ncurrentPage].ClosePage();

            FadeManager.Instance.TargetFade(Pages[_ncurrentPage].Page_CanvasGroup(), 0f, 0.5f);

            yield return _fadeDelay;
            Pages[_ncurrentPage].PageDown();

            _ncurrentPage = pageNum;
            yield return _waitDelay;
            Reset();
            yield return _waitDelay;
            Pages[pageNum].gameObject.SetActive(true);

            Pages[pageNum].OpenPage();
            FadeManager.Instance.TargetFade(Pages[pageNum].Page_CanvasGroup(), 1f, 0.5f);



            if (OnPageChange != null)
                OnPageChange.Invoke(pageNum);

        }
        _pageOpenCoroutine = null;
    }


    private void CloseAllPages()
    {
        for (int i = 0; i < Pages.Length; i++)
        {
            Pages[i].ClosePage();
            Pages[i].PageDown();
        }
    }

    #region PageButton

    public void IdleButton()
    {
        CurrentPage = 0;
    }

    public void ChangePage(int nextPageNum)
    {
        CurrentPage = nextPageNum;
    }

    public Page GetCurrentPage()
    {
        if (GameManager.Instance.IsStarted == false)
        {
            Debug.Log("Game Not Started Yet");
            return default;
        }

        if (Pages == null || Pages.Length < 1 || Pages[CurrentPage] == null)
        {
            Debug.Log("Current Page is Null");
            return default;
        }
        return Pages[CurrentPage];
    }

    #endregion

}