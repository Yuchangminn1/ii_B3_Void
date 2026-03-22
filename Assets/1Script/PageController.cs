using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PageController : Singleton<PageController>
{
    [SerializeField] private int openingPage = 0;

    SetUpCoroutine[] playerControllers;

    Coroutine pageResetCoroutine = null;

    WaitForSeconds _requestResetCoroutine = CoroutineReturnManager.GetWaitForSeconds(0.5f);

    WaitForSeconds _setupDelay = CoroutineReturnManager.GetWaitForSeconds(3f);

    public Action OnReset;
    override protected void Awake()
    {
        base.Awake();
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
                    foreach (var playerController in playerControllers)
                    {
                        playerController.CurrentPage = inputChar - '0';
                    }

                }
            }


        }

        if (Input.GetKeyDown(KeyCode.N))
        {
            foreach (var playerController in playerControllers)
            {
                playerController.DebugTrigger();
            }
        }

    }

    public int CurrentPage
    {
        get
        {

            if (playerControllers.Length > 0)
            {

                return playerControllers[0].CurrentPage;
            }
            else
            {
                return -1; // No player controllers available
            }
        }
    }

    public void RequestResetOpenPage(int pageNum)
    {
        if (pageResetCoroutine == null)
        {
            if (pageNum == 0)
            {
                OnReset?.Invoke();
                //ArduinoLEDManager.Instance.SendLEDAllOff();

            }




            pageResetCoroutine = StartCoroutine(RequestResetOpenPageCoroutine(pageNum));

        }

    }

    IEnumerator RequestResetOpenPageCoroutine(int pageNum)
    {

        foreach (var playerController in playerControllers)
        {
            playerController.OpenShow(pageNum);
        }
        yield return _requestResetCoroutine;
        pageResetCoroutine = null;

    }
    void Start()
    {
        playerControllers = GetComponentsInChildren<SetUpCoroutine>();
        GameManager.Instance?.AddProgramStart(StartPrograms());
    }


    public bool IsIdle()
    {
        bool isIdle = true;
        foreach (var playerController in playerControllers)
        {
            if (playerController.CurrentPage != 0)
            {
                isIdle = false;
                break;
            }
        }
        return isIdle;
    }

    public IEnumerator StartPrograms()
    {
        Debug.Log("페이지 컨트롤러 프로그램 시작 준비 중...");
        yield return CoroutineReturnManager.GetWaitForSeconds(2f);
        yield return null;

        foreach (var playerController in playerControllers)
        {
            foreach (GameObject settingPage in playerController.SettingPages)
            {
                settingPage.SetActive(true);
            }
        }
        foreach (var playerController in playerControllers)
        {
            playerController.PageSetUp(openingPage);
        }


    }





}
