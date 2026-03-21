using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;




public enum GameMode
{
    Playing,
    Stop
}

public class GameManager : MonoBehaviour, IJsonGenericTarget
{
    [Serializable]
    private struct CanvasDisplayBinding
    {
        public Canvas canvas;
    }

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

    Queue<IEnumerator> queueStartCreate = new Queue<IEnumerator>();

    Queue<IEnumerator> queueStartInitialize = new Queue<IEnumerator>();

    Queue<IEnumerator> queueProgramStart = new Queue<IEnumerator>();
    private KeyCode CursorToggleKey = KeyCode.M;
    private bool startHidden = false;

    GameMode _currentGameMode = GameMode.Playing;

    public GameMode CurrentGameMode { get { return _currentGameMode; } set { _currentGameMode = value; } }


    SetUpCoroutine[] _pageControllers;

    [SerializeField] private int[] targetDisplayIndices = new int[] { 1, 0 };
    [SerializeField] private CanvasDisplayBinding[] canvasDisplayBindings;

    //todo 제너릭 제이슨 만들어서 뺴기 
    float _resetTime = 20f;

    WaitForSeconds resetDelay;

    Coroutine resetCoroutine = null;
    JsonGenericUpData _genericData = new JsonGenericUpData();

    bool _isDebugMode = false;
    public bool IsDebugMode { get { return _isDebugMode; } }


    bool isStart = false;

    public bool IsStarted { get { return isStart; } set { isStart = value; } }


    public float Page4TimerDefaultTime = 1f;

    public void SetGameModePlay()
    {
        _currentGameMode = GameMode.Playing;
    }
    public void SetGameModeStop()
    {
        _currentGameMode = GameMode.Stop;
    }

    IEnumerator GoToIdleCoroutine()
    {
        if (resetDelay == null)
        {
            resetDelay = new WaitForSeconds(_resetTime);
        }
        PopupManager.Instance.ClosePopup();
        //Debug.Log("Resetting Page in " + _resetTime + " seconds...");
        yield return resetDelay;

        bool isIdle = true;
        foreach (var pageController in _pageControllers)
        {
            if (PageController.Instance.IsIdle() == false)
            {
                isIdle = false;
                break;
            }
        }
        if (isIdle == false)
        {
            PopupManager.Instance.ResetPopUpOpen();
        }

        resetCoroutine = null;
    }



    public void AddCreate(IEnumerator action)
    {
        queueStartCreate.Enqueue(action);
    }

    public void AddInitialize(IEnumerator action)
    {
        queueStartInitialize.Enqueue(action);
    }
    public void AddProgramStart(IEnumerator action)
    {
        queueProgramStart.Enqueue(action);
    }




    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }

    // displayIndices[display] = canvasIndex
    // 예: [1, 0] => Display0 <- Canvas1, Display1 <- Canvas0
    private void ActivateDisplaysByVariable(int[] displayIndices)
    {
        if (canvasDisplayBindings == null || canvasDisplayBindings.Length == 0)
        {
            Debug.LogWarning("canvasDisplayBindings가 비어있어 디스플레이 매핑을 적용하지 않습니다.");
            return;
        }

        if (displayIndices == null || displayIndices.Length == 0)
        {
            // 매핑이 없으면 기본적으로 같은 인덱스끼리 매핑 (Display0 <- Canvas0)
            int fallbackCount = Mathf.Min(Display.displays.Length, canvasDisplayBindings.Length);
            for (int displayIndex = 0; displayIndex < fallbackCount; displayIndex++)
            {
                if (canvasDisplayBindings[displayIndex].canvas == null)
                {
                    continue;
                }

                Display.displays[displayIndex].Activate();
                canvasDisplayBindings[displayIndex].canvas.targetDisplay = displayIndex;

                if (canvasDisplayBindings[displayIndex].canvas.renderMode == RenderMode.ScreenSpaceCamera &&
                    canvasDisplayBindings[displayIndex].canvas.worldCamera != null)
                {
                    canvasDisplayBindings[displayIndex].canvas.worldCamera.targetDisplay = displayIndex;
                }
            }
            return;
        }

        Debug.Log("디스플레이-캔버스 매핑: " + string.Join(", ", displayIndices));

        int displayCount = Mathf.Min(Display.displays.Length, displayIndices.Length);
        for (int displayIndex = 0; displayIndex < displayCount; displayIndex++)
        {
            int canvasIndex = displayIndices[displayIndex];

            if (canvasIndex < 0 || canvasIndex >= canvasDisplayBindings.Length)
            {
                Debug.LogWarning($"Display {displayIndex}에 대한 canvasIndex {canvasIndex}가 유효하지 않습니다. (범위: 0 ~ {canvasDisplayBindings.Length - 1})");
                continue;
            }

            Canvas targetCanvas = canvasDisplayBindings[canvasIndex].canvas;
            if (targetCanvas == null)
            {
                Debug.LogWarning($"Display {displayIndex}에 연결할 Canvas[{canvasIndex}]가 비어있습니다.");
                continue;
            }

            Display.displays[displayIndex].Activate();
            targetCanvas.targetDisplay = displayIndex;

            // Screen Space - Camera 모드에서는 카메라 display도 같이 맞춰줘야 실제 출력이 바뀝니다.
            if (targetCanvas.renderMode == RenderMode.ScreenSpaceCamera && targetCanvas.worldCamera != null)
            {
                targetCanvas.worldCamera.targetDisplay = displayIndex;
            }

            Debug.Log($"Display {displayIndex} <- Canvas[{canvasIndex}] '{targetCanvas.name}'");
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
            GoToIdleCheck();
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

    public void GoToIdleCheck()
    {
        if (resetCoroutine != null)
        {
            StopCoroutine(resetCoroutine);
        }
        resetCoroutine = StartCoroutine(GoToIdleCoroutine());
    }

    private void Apply(bool show)
    {
        Cursor.visible = show;
    }

    IEnumerator ProgramStart()
    {
        Debug.Log("프로그램 시작 준비 중...");
        yield return CoroutineReturnManager.GetWaitForSeconds(5f);//시작 대기 시간
        while (queueStartCreate.Count > 0)
            yield return StartCoroutine(queueStartCreate.Dequeue());

        while (queueStartInitialize.Count > 0)
            yield return StartCoroutine(queueStartInitialize.Dequeue());

        while (queueProgramStart.Count > 0)
            yield return StartCoroutine(queueProgramStart.Dequeue());



        Apply(startHidden);
        Debug.Log("연결된 모니터 수: " + Display.displays.Length);

        _pageControllers = FindObjectsByType<SetUpCoroutine>(FindObjectsSortMode.None);
#if UNITY_EDITOR == false
        ActivateDisplaysByVariable(targetDisplayIndices);
#endif
    }

    public void Initialize(JsonGenericUpData data)
    {
        _genericData = data;
        data.floatParams.TryGetValue("resetTime", out _resetTime);

        targetDisplayIndices = new int[2];
        targetDisplayIndices[0] = data.intParams.TryGetValue("displayLeft", out int displayValueLeft) ? displayValueLeft : 0;
        targetDisplayIndices[1] = data.intParams.TryGetValue("displayRight", out int displayValueRight) ? displayValueRight : 1;
        data.floatParams.TryGetValue("page4TimerDefaultTime", out Page4TimerDefaultTime);
        if (targetDisplayIndices[0] == targetDisplayIndices[1])
        {
            targetDisplayIndices[0] = 0;
            targetDisplayIndices[1] = 1;
            Debug.LogWarning("왼쪽과 오른쪽 제이슨 디스플레이 인덱스가 동일합니다. 올바른 인덱스를 설정해주세요.");
        }

        if (_resetTime < 1)
        {
            _resetTime = 45f;
        }
        PopupManager.Instance.ResetPopupDelay = _resetTime;
        resetDelay = new WaitForSeconds(_resetTime);

        StartCoroutine(ProgramStart());

    }
    public JsonGenericUpData Data()
    {
        _genericData.intParams = new Dictionary<string, int>();
        _genericData.floatParams = new Dictionary<string, float>();
        _genericData.boolParams = new Dictionary<string, bool>();

        _genericData.floatParams["resetTime"] = _resetTime;
        _genericData.floatParams["page4TimerDefaultTime"] = Page4TimerDefaultTime;
        return _genericData;
    }
}
