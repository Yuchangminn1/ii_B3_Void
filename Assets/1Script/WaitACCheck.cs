using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaitACCheck : WaitCheck
{

    public AcCheck LeftACCheck;
    public AcCheck RightACCheck;



    protected override void Start()
    {
        if (LeftACCheck == null || RightACCheck == null)
        {
            Debug.LogError("LeftACCheck or RightACCheck is not assigned in the inspector.");
            return;
        }
        LeftACCheck.AddOnClearListener(LeftPlayerDebug);
        RightACCheck.AddOnClearListener(RightPlayerDebug);
        PageController.Instance.OnReset += Reset;
    }



}
