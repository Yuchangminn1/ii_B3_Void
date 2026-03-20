using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RingLEDController_Page4 : RingLEDController, IJsonGenericTarget
{
    JsonGenericUpData _genericData = new JsonGenericUpData();
    bool isShowTime = false;

    float ledTime1 = 2f;
    float ledTime2 = 1.5f;

    Coroutine showTimeCoroutine = null;

    public Timer MainTimer;

    protected override void OnEnable()
    {
        if (GameManager.Instance.IsStarted == false)
            return;

        base.OnEnable();



    }

    protected override void Start()
    {
        LEDData.Instance.onAddPlayerLEDIndex += SetLEDState;

        if (CurrentDirection == Direction.Left)
        {
            ArduinoTouchManager.Instance.OnPlayerLeftTouch += SetLEDState;

        }
        else
        {
            ArduinoTouchManager.Instance.OnPlayerRightTouch += SetLEDState;

        }


    }
    public void ShowTime()
    {
        isShowTime = true;
        if (showTimeCoroutine != null)
        {
            StopCoroutine(showTimeCoroutine);
        }
        showTimeCoroutine = StartCoroutine(ShowTimeCoroutine());

    }


    public void ShowMainGame()
    {

        isShowTime = true;
        if (showTimeCoroutine != null)
        {
            StopCoroutine(showTimeCoroutine);
        }
        showTimeCoroutine = StartCoroutine(ShowTimeMainCoroutine());
    }
    public void StopShow()
    {

        isShowTime = false;
        if (showTimeCoroutine != null)
        {
            StopCoroutine(showTimeCoroutine);
        }

        AllLEDOff();
        ArduinoLEDManager.Instance.SendLEDAllOff();

        showTimeCoroutine = null;
    }
    IEnumerator ShowTimeCoroutine()
    {
        while (isShowTime)
        {

            if (CurrentDirection == Direction.Left)
            {
                ArduinoLEDManager.Instance.SendLEDAllOff();
                yield return CoroutineReturnManager.WaitForFixedUpdate;

                LEDData.Instance.AddPlayerLEDIndex();
            }

            if (MainTimer.Time2 < ledTime1)
            {
                yield break;
            }

            yield return CoroutineReturnManager.GetWaitForSeconds(ledTime2);
        }
    }
    IEnumerator ShowTimeMainCoroutine()
    {
        yield return CoroutineReturnManager.GetWaitForSeconds(2f);

        float starttime = Time.time;
        ArduinoTouchManager.Instance.UseTouchInput = true;
        MainTimer.StartTimer();
        if (CurrentDirection == Direction.Left)
        {
            SoundManager.Instance.PlayEffectSound(EffectSoundNum.TimerSound100);
        }


        while (isShowTime)
        {

            if (CurrentDirection == Direction.Left)
            {
                ArduinoLEDManager.Instance.SendLEDAllOff();

                LEDData.Instance.AddPlayerLEDIndex();
            }

            if (starttime + 19f > Time.time)
                yield return CoroutineReturnManager.GetWaitForSeconds(ledTime1);
            else
            {
                if (ledTime2 < 0.025f)
                    yield return CoroutineReturnManager.WaitForFixedUpdate;
                else
                {
                    if (MainTimer.Time2 < ledTime2)
                    {
                        SoundManager.Instance.StopEffectSound(EffectSoundNum.TimerSound100);

                        yield break;
                    }
                    else
                    {
                        yield return CoroutineReturnManager.GetWaitForSeconds(ledTime2);

                    }
                }
            }

        }
    }

    public void Initialize(JsonGenericUpData data)
    {
        _genericData = data;
        data.floatParams.TryGetValue("ledTime1", out ledTime1);
        data.floatParams.TryGetValue("ledTime2", out ledTime2);
    }
    public JsonGenericUpData Data()
    {
        _genericData.intParams = new Dictionary<string, int>();
        _genericData.floatParams = new Dictionary<string, float>();
        _genericData.boolParams = new Dictionary<string, bool>();
        _genericData.stringParams = new Dictionary<string, string>();


        _genericData.floatParams["ledTime1"] = ledTime1;
        _genericData.floatParams["ledTime2"] = ledTime2;

        return _genericData;
    }
}
