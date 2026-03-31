using System.Collections;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Video;

public abstract class SequenceScript : MonoBehaviour
{

    int _currentIndex = 0;
    public int CurrentIndex
    {
        get { return _currentIndex; }
        set { _currentIndex = value; }
    }


    public AudioSource audioSource;

    public VideoPlayer nextVedeoPlayer;

    public Coroutine coroutine;
    [Header("시퀀스 리셋 이후 콜백")]

    public UnityEvent OnSequenceStart;
    [Header("시퀀스 트리거 이후 콜백")]
    public UnityEvent triggerCallback;

    [Header("다음 넘어가는 콜백")]
    public UnityEvent nextSequenceCallback;

    [Header("현재 트리거 여부 ")]
    [SerializeField] protected bool isTrigger = true;
    [Header("자동 트리거 여부")]
    [SerializeField] protected bool originTrigger;
    [Header("다음 트리거 넘어가는 딜레이 default -1")]

    [SerializeField] protected float nextDelayTime = -1f;


    WaitForFixedUpdate waitFixedUpdate = new WaitForFixedUpdate();

    WaitForSeconds textUpdateDelay = new WaitForSeconds(0.1f);

    WaitForSeconds waitNextDelay;

    protected bool isInitialize = false;

    bool isWaiting = false;

    Coroutine _triggerForceOnCoroutine = null;





    protected void Awake()
    {
        originTrigger = isTrigger;
        if (nextDelayTime > 0)
        {
            waitNextDelay = new WaitForSeconds(nextDelayTime);
        }

        AwakeSetup();

        isInitialize = true;
        if (audioSource == null)

            audioSource = GetComponent<AudioSource>();

    }

    protected IEnumerator WaitNextDelay()
    {
        if (waitNextDelay == null) yield break;
        yield return waitNextDelay;
    }


    public bool TriggerOnBool()
    {
        // if (isWaiting)
        //     isTrigger = true;
        return isWaiting;
    }
    IEnumerator DelayToWaitTrigger()
    {
        yield return CoroutineReturnManager.GetWaitForSeconds(0.1f);
        isWaiting = true;
    }
    public IEnumerator StartSequence()
    {
        //초기화 전도 대기
        while (!isInitialize)
        {
            yield return textUpdateDelay;
        }
        isTrigger = originTrigger;

        StartCoroutine(DelayToWaitTrigger());
        GameManager.Instance.GoToIdleCheck();
        //특정 트리거 필요하면 대기 
        OnSequenceStart?.Invoke();


        while (!isTrigger)
        {
            //Debug.Log($"{this.name}Wait isTrigger");
            yield return waitFixedUpdate;
        }
        isWaiting = false;

        //Debug.Log($"{this.name} Trigger");
        if (audioSource != null)
            audioSource.Play();

        triggerCallback?.Invoke();

        yield return coroutine = StartCoroutine(RunSequence());


        yield return WaitNextDelay();

        EndPageSequence();
    }

    public void AddTriggerCallback(UnityAction action)
    {
        triggerCallback.AddListener(action);
    }

    // public void AddTrigger(SequenceScript sequenceScript)
    // {
    //     if (isSubPageSequence)
    //         sequenceScript.AddTriggerCallback(TriggerOn);
    // }


    public void TriggerFroceOn()
    {
        if (gameObject.activeInHierarchy)
            return;
        isTrigger = true;
        isWaiting = true;
        if (_triggerForceOnCoroutine != null)
            StopCoroutine(_triggerForceOnCoroutine);
        _triggerForceOnCoroutine = StartCoroutine(TriggerForceOnCoroutine());

    }
    public void TriggerOn()
    {
        isTrigger = true;
    }

    IEnumerator TriggerForceOnCoroutine()
    {

        while (isWaiting && gameObject.activeInHierarchy)
        {
            yield return CoroutineReturnManager.GetWaitForSeconds(0.1f);
            isTrigger = true;
        }
        _triggerForceOnCoroutine = null;
    }
    protected abstract IEnumerator RunSequence();

    protected virtual void AwakeSetup()
    {
        Initialize();
    }

    protected virtual void OnEnable()
    {
        isWaiting = false;
    }




    protected virtual void Initialize()
    {
        ;
    }
    /// <summary>
    /// 디버그용
    /// </summary>


    protected virtual void EndPageSequence()
    {
        if (nextVedeoPlayer != null) nextVedeoPlayer.Prepare();

        isTrigger = originTrigger;
        nextSequenceCallback?.Invoke();
    }

    public void AddNextSequenceCallback(UnityAction action)
    {
        nextSequenceCallback.AddListener(action);
    }

    void OnDisable()
    {
        if (_triggerForceOnCoroutine != null)
        {
            StopCoroutine(_triggerForceOnCoroutine);
            _triggerForceOnCoroutine = null;
        }
        StopAllCoroutines();
    }



}