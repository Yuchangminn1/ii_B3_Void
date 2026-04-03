using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class AcCheckStep : AcCheck
{

    public UnityEvent onSuccess;
    public UnityEvent onFailure;

    public float setTime = 7f;

    public Timer timer;


    protected override IEnumerator DelayOnClear()
    {
        yield return StartCoroutine(base.DelayOnClear());
        onSuccess.Invoke();
    }

    public override void StartCheck()
    {
        base.StartCheck();

        if (timer != null)
        {
            if (CurrentDirection == Direction.Left)
            {
                timer.SetDefaultTime(setTime);
                timer.ResetTimer();
                timer.StartTimer();
            }
            timer.AddOnEndListener(CheckAnswer);
        }
    }
    public void IsFailed()
    {
        onFailure.Invoke();
    }
    protected override void Update()
    {
        ;
    }

    public void CheckAnswer()
    {
        UpdateColorPercent();

        if (_isClear == false)
        {
            IsFailed();
        }
        StopCheck();

    }

}
