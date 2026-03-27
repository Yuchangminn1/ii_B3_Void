using System.Collections;
using System.Threading;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
public class WaitCheck : MonoBehaviour
{
    public SequenceScript Player1_Trigger;
    public SequenceScript Player2_Trigger;

    public SequenceScript[] Clear_Triggers;


    public UnityEvent ClearTrigger = new UnityEvent();

    readonly float CheckImageTime = 1f;

    public Graphic[] Player1_Graphic;
    public Graphic[] Player1_Graphic2;


    public Graphic[] Player2_Graphic;
    public Graphic[] Player2_Graphic2;


    public Graphic[] Player1_Wait_Graphic;
    public Graphic[] Player2_Wait_Graphic;

    protected bool _isPlayer1On = false;
    public bool IsPlayer1On
    {
        get { return _isPlayer1On; }
        set
        {
            if (value && !_isPlayer1On)
            {
                _isPlayer1On = true;
            }

        }
    }
    protected bool _isPlayer2On = false;
    public bool IsPlayer2On
    {
        get { return _isPlayer2On; }
        set
        {
            if (value && !_isPlayer2On)
            {
                _isPlayer2On = true;
            }
        }
    }


    protected Coroutine checkCoroutine = null;


    protected WaitForSeconds _checkWait = new WaitForSeconds(1f);

    protected Coroutine debugZ = null;
    protected Coroutine debugX = null;

    protected WaitForSeconds debugWait = new WaitForSeconds(0.2f);


    protected bool isTriggerTime = false;

    public void LeftPlayerDebug()
    {
        if (debugZ == null)
            debugZ = StartCoroutine(ChangeZ());

    }
    public void RightPlayerDebug()
    {
        if (debugX == null)
            debugX = StartCoroutine(ChangeX());
    }

    protected virtual void Start()
    {
        Arduino_SelectButton[] buttons = FindObjectsByType<Arduino_SelectButton>(FindObjectsSortMode.None);

        foreach (var button in buttons)
        {
            button._onDebugPlayerLeft += LeftPlayerDebug;
            button._onDebugPlayerRight += RightPlayerDebug;
        }
        PageController.Instance.OnReset += Reset;
    }






    public void Checking(Direction direction)
    {
        if (direction == Direction.Left)
        {
            if (debugZ == null)
            {
                debugZ = StartCoroutine(ChangeZ());
            }
        }
        else
        {
            if (debugX == null)
            {
                debugX = StartCoroutine(ChangeX());
            }
        }

    }

    protected IEnumerator ChangeZ()
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

        }

        debugZ = null;

    }
    protected IEnumerator ChangeX()
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

        }

        debugX = null;

    }
    protected IEnumerator WaitCoroutine()
    {
        bool isAllReady = false;
        int count = 0;
        bool isNext = false;
        while (isAllReady == false)
        {

            if (IsPlayer1On && IsPlayer2On)
            {
                yield return CoroutineReturnManager.GetWaitForSeconds(2f);

                while (isNext == false)
                {
                    foreach (var Clear_Trigger in Clear_Triggers)
                    {
                        if (Clear_Trigger.TriggerOnBool() == false)
                        {
                            continue;
                        }
                    }
                    isNext = true;
                    yield return CoroutineReturnManager.GetWaitForSeconds(0.1f);
                }
                ClearTrigger.Invoke();
                isAllReady = true;
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

    public void SetTriggerTime(bool isOn)
    {
        isTriggerTime = isOn;
    }

    public void StartCheck1()
    {
        debugZ = null;
        debugX = null;
        if (GameManager.Instance.IsStarted == false)
        {
            return;
        }
        Reset();
        if (checkCoroutine != null)
        {
            StopCoroutine(checkCoroutine);
            checkCoroutine = null;
        }

        checkCoroutine = StartCoroutine(WaitCoroutine());
    }
    protected void StopCheck()
    {
        if (checkCoroutine != null)
        {
            StopCoroutine(checkCoroutine);
            checkCoroutine = null;
        }
    }
    protected void OnDisable()
    {
        if (checkCoroutine != null)
        {
            StopCoroutine(checkCoroutine);
            checkCoroutine = null;
        }
    }

    protected void Reset()
    {
        StopCheck();
        _isPlayer1On = false;
        _isPlayer2On = false;
    }
    public void OnClear()
    {
        ClearTrigger.Invoke();
    }

}
