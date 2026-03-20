using UnityEngine;
using System.Collections;
using System;
using System.Collections.Generic;
using Random = UnityEngine.Random;
using UnityEngine.Events;
using System.IO.Ports;
using Unity.VisualScripting;

public class TouchChecker : MonoBehaviour
{

    //TODO 겟 컴포넌트로 그냥 할당하면 될듯

    //TODO LED도 제어해야함 아두이노 추가로 연결필요

    public bool IsTag = false;

    public Direction currentDirection = Direction.Left;

    protected bool _isCheck = false;

    protected bool IsTagging = false;

    public Timer timer;

    public UnityEvent OnTagWaitStart_ResetValue;

    public UnityEvent OnTagWaitStart_ResetGraphic;

    public UnityEvent OnTagSuccess_ChangeValue;
    public UnityEvent OnTagSuccess_ChangeGraphic;


    Coroutine touchCheckCoroutine = null;

    protected Coroutine _pressButtonUpdate = null;

    protected virtual void Start()
    {
        if (currentDirection == Direction.Left)
            ArduinoTouchManager.Instance.OnPlayerLeftTouch += SetTagLED;

        else
            ArduinoTouchManager.Instance.OnPlayerRightTouch += SetTagLED;



        ArduinoTouchManager.Instance.OnAllPlayerTouchStateChanged += SetIsTag;

        if (timer != null)
        {
            timer.onTimerEnd.AddListener(TagSuccess);
        }

    }


    protected virtual void OnDisable()
    {
        TagStop();
    }


    public void TagStart()
    {
        if (touchCheckCoroutine == null)
            touchCheckCoroutine = StartCoroutine(PlayerTagCheck());

    }
    public virtual void TagStop()
    {
        if (touchCheckCoroutine != null)
            StopCoroutine(touchCheckCoroutine);
        touchCheckCoroutine = null;

        if (_pressButtonUpdate != null)
            StopCoroutine(_pressButtonUpdate);
        _pressButtonUpdate = null;


    }
    protected virtual IEnumerator PlayerTagCheck()
    {
        _isCheck = true;
        IsTag = false;

        Debug.Log("태그 대기 시작!");


        IsTagging = true;


        if (currentDirection == Direction.Left)
            ArduinoLEDManager.Instance.SendLEDAllOff();

        OnTagWaitStart_ResetValue?.Invoke();

        yield return CoroutineReturnManager.GetWaitForSeconds(0.5f);

        OnTagWaitStart_ResetGraphic?.Invoke();

        yield return CoroutineReturnManager.GetWaitForSeconds(0.1f);
        if (currentDirection == Direction.Left)
            LEDData.Instance.AddPlayerLEDIndex();
        float tagStartTime = Time.time;

        timer.SetTextVisible();

        IsTag = false;

        ArduinoTouchManager.Instance.UseTouchInput = true;

        while (IsTagging)
        {

            while (IsTag)  //LED 가 1부터 시작해서 1뺴기
            {
                if (timer.IsCounting == false)
                {
                    timer.StartTimer();
                }
                GameManager.Instance.GoToIdleCheck();

                yield return CoroutineReturnManager.GetWaitForSeconds(0.05f);
            }


            if (IsTag == false && timer.IsCounting)
            {
                timer.ResetTime();
                SoundManager.Instance.StopEffectSound(EffectSoundNum.SecondSound3);
            }

            yield return CoroutineReturnManager.WaitForFixedUpdate;
        }

    }

    public void TagSuccess()
    {
        Debug.Log("태그 성공!");
        if (touchCheckCoroutine != null)
            StopCoroutine(touchCheckCoroutine);
        touchCheckCoroutine = null;
        SoundManager.Instance.StopEffectSound(EffectSoundNum.SecondSound3);

        IsTagging = false;
        OnTagSuccess_ChangeValue?.Invoke();
        ArduinoLEDManager.Instance.SendLEDAllOff();
        OnTagSuccess_ChangeGraphic?.Invoke();

    }



    public virtual void SetIsTag(bool value1, bool value2)
    {
        if (gameObject.activeInHierarchy == false || GameManager.Instance.IsStarted == false)
        {
            return;
        }

        IsTag = value1 && value2;

    }
    public virtual void SetTagLED(bool value1, bool value2)
    {
        if (gameObject.activeInHierarchy == false || GameManager.Instance.IsStarted == false || UserDataManager.Instance.IsUser() == false)
        {
            return;
        }
        if (value1 && value2)
        {
            ArduinoLEDManager.Instance.SendLEDGreenMessage(LEDData.Instance.GetPlayerLEDPair(), currentDirection);
        }

        else if (value1)
        {

            ArduinoLEDManager.Instance.SendLEDGreenMessage(LEDData.Instance.GetPlayerLEDPair()[0], currentDirection);
            ArduinoLEDManager.Instance.SendLEDWhiteMessage(LEDData.Instance.GetPlayerLEDPair()[1], currentDirection);

        }
        else if (value2)
        {

            ArduinoLEDManager.Instance.SendLEDGreenMessage(LEDData.Instance.GetPlayerLEDPair()[1], currentDirection);
            ArduinoLEDManager.Instance.SendLEDWhiteMessage(LEDData.Instance.GetPlayerLEDPair()[0], currentDirection);

        }
        else
        {
            ArduinoLEDManager.Instance.SendLEDWhiteMessage(LEDData.Instance.GetPlayerLEDPair(), currentDirection);
        }

    }


}