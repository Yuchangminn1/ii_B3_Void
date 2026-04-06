using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class AcCheckStep : AcCheck
{

    public UnityEvent onSuccess;
    public UnityEvent onFailure;

    public float setTime = 7f;

    public Timer timer;

    const int GetScorePage = 5;

    int _currentIndex = 0;

    public int CurrentIndex
    {
        get { return _currentIndex; }
        set
        {
            if (_currentIndex != value)
            {
                _currentIndex = value;
            }
        }
    }


    protected override IEnumerator DelayOnClear()
    {
        yield return StartCoroutine(base.DelayOnClear());
        Debug.Log("DelayOnClear");
        if (PageController.Instance.CurrentPage == GetScorePage)
        {
            StepDataManager.Instance.SetSuccess(CurrentDirection, CurrentIndex);
        }
        onSuccess.Invoke();
        CurrentIndex++;

    }

    public override void StartCheck()
    {
        base.StartCheck();

        FadeManager.Instance.SetAlphaZero(outputText);

        if (timer != null)
        {
            if (CurrentDirection == Direction.Left)
            {
                timer.SetDefaultTime(setTime);
                timer.ResetTimer();
                timer.StartTimer();
            }
            Debug.Log($"{name}StartCheck Timer");
            timer.AddOnEndListener(CheckAnswer);
        }
    }


    protected override void Start()
    {
        base.Start();

        protrusionAdjustPercent += 8f;


    }
    public void IsFailed()
    {
        FadeManager.Instance.SetAlphaOne(outputText);
        onFailure.Invoke();
        CurrentIndex++;

    }
    protected override void Update()
    {
        ;
    }

    public void CheckAnswer()
    {
        UpdateColorPercent();
        StartCoroutine(DebugTextShow());

        if (_isClear == false)
        {
            IsFailed();
        }

    }

    IEnumerator DebugTextShow()
    {
        yield return new WaitForSeconds(1f);
        FadeManager.Instance.SetAlphaZero(outputText);


    }
}
