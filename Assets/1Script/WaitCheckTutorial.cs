using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaitCheckTutorial : WaitCheck
{
    public ShootPieceContainer shootPieceContainer;


    protected override IEnumerator ChangeZ()
    {
        yield return debugWait;
        if (Player1_Trigger == null)
        {
            IsPlayer1On = true;
            debugZ = null;

            yield break;
        }
        if (Player1_Trigger.TriggerOnBool())
        {

            FadeManager.Instance.SetAlphaZero(Player1_Wait_Graphic);
            FadeManager.Instance.TargetFade(Player1_Graphic, 1f);


            if (Player1_Graphic2.Length > 0)
            {
                yield return CoroutineReturnManager.GetWaitForSeconds(CheckImageTime);
                FadeManager.Instance.TargetFade(Player1_Graphic2, 1f);

            }
            IsPlayer1On = true;
            Player1_Trigger.TriggerOn();
            shootPieceContainer.ShowPiece(direction: Direction.Left);

        }

        debugZ = null;

    }
    protected override IEnumerator ChangeX()
    {
        yield return debugWait;
        if (Player2_Trigger == null)
        {
            IsPlayer2On = true;
            debugX = null;
            yield break;
        }
        if (Player2_Trigger.TriggerOnBool())
        {
            FadeManager.Instance.SetAlphaZero(Player2_Wait_Graphic);

            FadeManager.Instance.TargetFade(Player2_Graphic, 1f);

            if (Player2_Graphic2.Length > 0)
            {
                yield return CoroutineReturnManager.GetWaitForSeconds(CheckImageTime);
                FadeManager.Instance.TargetFade(Player2_Graphic2, 1f);

            }
            IsPlayer2On = true;
            Player2_Trigger.TriggerOn();

            shootPieceContainer.ShowPiece(direction: Direction.Right);

        }

        debugX = null;

    }
    protected override IEnumerator WaitCoroutine()
    {
        bool isAllReady = false;
        int count = 0;
        bool isNext = false;
        while (isAllReady == false)
        {

            if (IsPlayer1On && IsPlayer2On)
            {
                //yield return CoroutineReturnManager.GetWaitForSeconds(2f);
                int triggerCount = 0;
                while (isNext == false)
                {


                    triggerCount = 0;

                    foreach (var Clear_Trigger in Clear_Triggers)
                    {
                        if (Clear_Trigger.TriggerOnBool() == false)
                        {
                            yield return CoroutineReturnManager.GetWaitForSeconds(0.1f);

                            continue;
                        }
                        triggerCount++;

                    }
                    if (triggerCount == Clear_Triggers.Length)
                        isNext = true;

                }
                isAllReady = true;
                shootPieceContainer.Shoot();


                yield return CoroutineReturnManager.GetWaitForSeconds(2f);
                foreach (var Clear_Trigger in Clear_Triggers)
                {
                    Clear_Trigger.TriggerOn();
                }

                PopupManager.Instance.SetInputType(InputType.Touch);

            }
            else
            {
                count++;
                if (count > 50)
                {
                    Debug.Log($"IsPlayer1On{IsPlayer1On}, IsPlayer2On{IsPlayer2On}");
                    count = 0;
                }
            }
            yield return CoroutineReturnManager.GetWaitForSeconds(0.1f);

        }
        checkCoroutine = null;
    }

}
