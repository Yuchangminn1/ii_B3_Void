using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;


public class SetUpCoroutine : MonoBehaviour
{


    [Space]
    [Space]
    [Space]
    [Header("-----------------Debug Mode----------------")]





    [Header("---------------------------------------------")]
    [SerializeField]
    [Tooltip("Pages 오브젝트 내부에 추가된 모든 Page 오브젝트")]
    public MainPage[] Pages;

    [SerializeField]
    [Tooltip("VideoPlayers 오브젝트 내부에 추가된 모든 Page 오브젝트")]
    private GameObject[] videos;

    public GameObject[] SettingPages;

    [SerializeField] private int _nCurrentPage = 0;

    WaitForSeconds SetupDelay = CoroutineReturnManager.GetWaitForSeconds(3f);




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


    float _fadeDuration = 0.5f;
    public float FadeDuration { get { return _fadeDuration; } set { _fadeDuration = value; } }

    WaitForSeconds _fadeDelay;

    WaitForSeconds _waitDelay = new WaitForSeconds(0.05f);

    Coroutine _pageResetCoroutine = null;

    Coroutine _pageOpenCoroutine = null;


    public bool IsPairPageOpen = true;





    public int CurrentPage
    {
        get { return _nCurrentPage; }
        set { CheckCondition(value); }
    }

    private void CheckCondition(int value)
    {
        if (_pageOpenCoroutine != null) return;
        if (value >= Pages.Length)
        {
            value = 0;
        }

        if (value == 0)
        {
            _onRestRequest?.Invoke();
            UserDataManager.Instance.Reset();
            //ArduinoLEDManager.Instance.SendLEDAllOff();

        }

        if (IsPairPageOpen)
        {
            PageController.Instance.RequestResetOpenPage(value);
            return;
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

    public void OpenShow(int value)
    {
        if (_pageOpenCoroutine != null) return;
        if (value >= Pages.Length)
        {
            value = 0;
        }

        if (value == 0)
        {
            _onRestRequest?.Invoke();
            UserDataManager.Instance.Reset();

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
        _fadeDelay = new WaitForSeconds(_fadeDuration);
    }


    void Update()
    {

        if (OnUpdateRequest == null) return;
        OnUpdateRequest?.Invoke();
    }


    public void AddResetAction(Action resetAction)
    {
        _onRestRequest += resetAction;
    }

    public void PageSetUp(int openingPage)
    {
        StartCoroutine(PageSetUpCoroutine(openingPage));
    }

    IEnumerator PageSetUpCoroutine(int openingPage)
    {
        yield return SetupDelay;
        Pages = GetComponentsInChildren<MainPage>();

        for (int i = 0; i < Pages.Length; i++)
        {

            yield return CoroutineReturnManager.WaitForFixedUpdate;
            Pages[i].Initialize();

            Pages[i].PageNumber = i;
        }


        foreach (GameObject settingPage in SettingPages)
        {
            settingPage.SetActive(false);
        }

        CloseAllPages();
        OpenShow(openingPage);

        // if (GameManager.Instance.IsDebugMode)
        // {
        //     CurrentPage = openingPage;
        // }
        // else
        // {
        //     CurrentPage = 0;
        // }

        GameManager.Instance.IsStarted = true;

    }

    public void PageReset()
    {
        if (_pageResetCoroutine != null)
        {
            StopCoroutine(_pageResetCoroutine);
            _pageResetCoroutine = null;
        }
        _pageResetCoroutine = StartCoroutine(ResetCoroutine());
    }

    IEnumerator ResetCoroutine()
    {
        CloseAllPages();
        yield return _waitDelay;

        CurrentPage = 0;

        _pageResetCoroutine = null;
    }





    public void DebugTrigger()
    {
        Debug.Log($"{name}Debug Triggered");
        GetCurrentPage().CurrentIndexTriggerON();
    }



    void OpenPage(int pageNum)
    {
        if (_pageOpenCoroutine == null)
            _pageOpenCoroutine = StartCoroutine(OpenPageCoroutine(pageNum));
    }

    IEnumerator OpenPageCoroutine(int pageNum)
    {
        // if (pageNum == 0)
        // {
        //     SoundManager.Instance.MuteBGM();
        // }

        if (Pages.Length > pageNum)
        {
            Pages[_nCurrentPage].ClosePage();
            if (Pages[_nCurrentPage].isCutOut == false)
                yield return _fadeDelay;

            Pages[_nCurrentPage].gameObject.SetActive(false);


            _nCurrentPage = pageNum;

            yield return _waitDelay;


            Pages[pageNum].gameObject.SetActive(true);


            Pages[pageNum].OpenPage();

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

    public MainPage GetCurrentPage()
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