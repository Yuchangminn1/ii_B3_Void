using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Video;

public abstract class SequenceScript : MonoBehaviour
{

    public Coroutine coroutine;

    public int currentIndex = 0;

    public AudioSource audioSource;

    [SerializeField] protected bool isTrigger = true;

    [SerializeField] protected bool originTrigger;

    [SerializeField] protected float nextDelayTime = -1f;


    WaitForFixedUpdate waitFixedUpdate = new WaitForFixedUpdate();

    WaitForSeconds waitNextDelay;
    [Header("OnSequenceEnd")]
    public UnityEvent sequenceCallback;

    public VideoPlayer nextVedeoPlayer;


    protected bool isInitialize = false;





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



    public IEnumerator StartSequence()
    {
        //초기화 전도 대기
        while (!isInitialize)
        {
            yield return waitFixedUpdate;
        }
        isTrigger = originTrigger;
        //특정 트리거 필요하면 대기 
        while (!isTrigger)
        {
            //Debug.Log($"{this.name}Wait isTrigger");
            yield return waitFixedUpdate;
        }
        if (audioSource != null) audioSource.Play();

        yield return coroutine = StartCoroutine(RunSequence());

        if (sequenceCallback != null) sequenceCallback.Invoke();

        yield return WaitNextDelay();

        EndPageSequence();
    }

    public void TriggerOn()
    {
        isTrigger = true;
    }

    protected abstract IEnumerator RunSequence();

    protected abstract void AwakeSetup();
    /// <summary>
    /// 디버그용
    /// </summary>


    protected virtual void EndPageSequence()
    {
        GameManager.Instance.ResetCoroutine();
        if (nextVedeoPlayer != null) nextVedeoPlayer.Prepare();
        PageController.Instance.NextSequence();

    }
}