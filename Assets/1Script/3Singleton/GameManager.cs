using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour, IJsonGenericTarget
{
    public static GameManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindFirstObjectByType<GameManager>();

                if (instance == null)
                {
                    GameObject singletonObject = new GameObject("GameManager");
                    instance = singletonObject.AddComponent<GameManager>();
                }
            }

            return instance;
        }
    }


    static GameManager instance;

    Action actionStartCreate;

    Action actionStartInitialize;

    Action actionProgramStart;
    private KeyCode CursorToggleKey = KeyCode.M;
    private bool startHidden = false;


    WaitForFixedUpdate waitForFixedUpdate = new WaitForFixedUpdate();

    WaitForSeconds startDelay = new WaitForSeconds(2f);

    //todo 제너릭 제이슨 만들어서 뺴기 
    float _resetTime = 45f;

    WaitForSeconds resetDelay;

    Coroutine resetCoroutine = null;
    JsonGenericUpData _genericData = new JsonGenericUpData();

    bool _isDebugMode = false;
    public bool IsDebugMode { get { return _isDebugMode; } }


    bool isStart = false;

    public bool IsStarted { get { return isStart; } set { isStart = value; } }

    IEnumerator ResetPage()
    {
        if (resetDelay == null)
        {
            resetDelay = new WaitForSeconds(_resetTime);
        }
        //Debug.Log("Resetting Page in " + _resetTime + " seconds...");
        yield return resetDelay;
        if (PageController.Instance.CurrentPage != 0 || PageController.Instance.GetCurrentPage().currentindex != 0)
        {
            PageController.Instance.CurrentPage = 0;
            Debug.Log("Page Reset");
        }
        resetCoroutine = null;
    }



    public void ActionAddCreate(Action action)
    {
        actionStartCreate += action;
    }

    public void ActionAddInitialize(Action action)
    {
        actionStartInitialize += action;
    }
    public void ActionAddProgramStart(Action action)
    {
        actionProgramStart += action;
    }




    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        StartCoroutine(StartRiytube());
        Apply(startHidden);
        Debug.Log("연결된 모니터 수: " + Display.displays.Length);

        // Display 1은 기본 활성화
        // Display 2 이상을 활성화하려면 Activate() 호출
        for (int i = 1; i < Display.displays.Length; i++)
        {
            Display.displays[i].Activate();
        }
    }
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

    }
    // Update is called once per frame
    void Update()
    {
        if (Input.GetMouseButtonUp(0))
        {
            ResetCoroutine();
        }
        if (Input.GetKeyDown(CursorToggleKey))
        {
            startHidden = !startHidden;
            Apply(startHidden);
        }
        if (Input.GetKeyDown(KeyCode.D))
        {
            _isDebugMode = !_isDebugMode;
        }
    }

    public void ResetCoroutine()
    {
        if (resetCoroutine != null)
        {
            StopCoroutine(resetCoroutine);
        }
        resetCoroutine = StartCoroutine(ResetPage());
    }

    private void Apply(bool show)
    {
        Cursor.visible = show;
    }

    IEnumerator StartRiytube()
    {
        yield return startDelay;
        actionStartCreate?.Invoke();

        yield return waitForFixedUpdate;
        actionStartInitialize?.Invoke();

        yield return waitForFixedUpdate;
        actionProgramStart?.Invoke();
    }

    public void Initialize(JsonGenericUpData data)
    {
        _genericData = data;
        data.floatParams.TryGetValue("resetTime", out _resetTime);
        if (_resetTime < 1)
        {
            _resetTime = 45f;
        }
        resetDelay = new WaitForSeconds(_resetTime);
    }
    public JsonGenericUpData Data()
    {
        _genericData.intParams = new Dictionary<string, int>();
        _genericData.floatParams = new Dictionary<string, float>();
        _genericData.boolParams = new Dictionary<string, bool>();

        _genericData.floatParams["resetTime"] = _resetTime;
        return _genericData;
    }
}
