using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SequenceTriggers : SequenceScript
{
    bool triggerA, triggerB, triggerC;
    [SerializeField] float DefaultRemainingTime = 0.1f;
    [SerializeField] float remainingTime = 5f;

    WaitForFixedUpdate waitForFixedUpdate = new WaitForFixedUpdate();

    protected override void AwakeSetup()
    {
        remainingTime = DefaultRemainingTime;
        triggerA = false;
        triggerB = false;
        triggerC = false;
    }

    protected override IEnumerator RunSequence()
    {
        while (remainingTime > 0f)
        {
            remainingTime -= Time.deltaTime;
            yield return waitForFixedUpdate;
        }
    }

    public void TriggerA()
    {
        triggerA = true;
        if (triggerA && triggerB && triggerC)
        {
            isTrigger = true;
        }
    }
    public void TriggerB()
    {
        triggerB = true;
        if (triggerA && triggerB && triggerC)
        {
            isTrigger = true;
        }
    }
    public void TriggerC()
    {
        triggerC = true;
        if (triggerA && triggerB && triggerC)
        {
            isTrigger = true;
        }
    }
}
