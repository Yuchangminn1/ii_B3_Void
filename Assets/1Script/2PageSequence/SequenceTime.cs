using System.Collections;
using UnityEngine;

public class SequenceTime : SequenceScript
{
    [SerializeField] float DefaultRemainingTime = 0.1f;
    [SerializeField] float remainingTime = 5f;

    WaitForFixedUpdate waitForFixedUpdate = new WaitForFixedUpdate();

    protected override void AwakeSetup()
    {
        remainingTime = DefaultRemainingTime;
    }

    protected override IEnumerator RunSequence()
    {
        while (remainingTime > 0f)
        {
            remainingTime -= Time.deltaTime;
            yield return waitForFixedUpdate;
        }
    }
}
