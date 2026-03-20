using System;
using System.Collections;
using System.IO.Ports;
using UnityEngine;
using UnityEngine.Events;

public class Arduino_Touch_Page4 : TouchChecker
{


    public Arduino_Touch_Page4 player1_arduino_Touch_Page4;

    int lastLEDIndex = -1;

    public Timer timer2;
    public int LastAnsIndex;

    public Action<int> _onScoreChange;

    Coroutine FlaseCorotuine = null;
    float _deflatFalseTime = 0.1f;

    float _falseTime = 0.1f;

    Coroutine _isTagResetFalseCoroutine = null;

    override protected void OnDisable()
    {
        TagStop();

    }

    public override void TagStop()
    {
        base.TagStop();
        if (_isTagResetFalseCoroutine != null)
            StopCoroutine(_isTagResetFalseCoroutine);
        _isTagResetFalseCoroutine = null;

    }
    protected override void Start()
    {

        if (currentDirection == Direction.Left)
        {
            ArduinoTouchManager.Instance.OnPlayerLeftTouch += SetIsTag;
        }

        else
        {
            ArduinoTouchManager.Instance.OnPlayerRightTouch += SetIsTag;
        }

        LEDData.Instance.onAddPlayerLEDIndex += ClearIsTag;


        if (timer != null)
        {
            timer.onTimerEnd.AddListener(TagSuccess);
        }
    }

    public void AddOnscoreChange(Action<int> onScoreChange)
    {
        _onScoreChange += onScoreChange;
    }

    protected override IEnumerator PlayerTagCheck()
    {

        Debug.Log("태그 대기 시작!");
        FlaseCorotuine = null;
        timer2?.SetTimerText("Start");

        IsTagging = true;

        OnTagWaitStart_ResetValue?.Invoke();

        yield return CoroutineReturnManager.GetWaitForSeconds(0.5f);

        timer2.OffTimer();
        OnTagWaitStart_ResetGraphic?.Invoke();

        LastAnsIndex = -1;
        yield return CoroutineReturnManager.WaitForFixedUpdate;
    }


    public int GetLEDIndex()
    {
        return UserDataManager.Instance.GetPlayer(0).LedTagIndex;
    }

    public override void SetIsTag(bool value1, bool value2)
    {
        if (FlaseCorotuine != null)
        {
            return;
        }
        if (gameObject.activeInHierarchy == false)
        {
            return;
        }
        if (LastAnsIndex == UserDataManager.Instance.GetPlayer(0).LedTagIndex)
            return;

        IsTag = value1 && value2;


        if (IsTag)
        {
            LastAnsIndex = UserDataManager.Instance.GetPlayer(0).LedTagIndex;
            SoundManager.Instance.PlayEffectSound(EffectSoundNum.GreenHandSound);

        }

        if (player1_arduino_Touch_Page4.IsTag && IsTag)
        {
            int q = UserDataManager.Instance.GetPlayer(0).AddScores();
            _onScoreChange?.Invoke(q);
            IsTag = false;
            player1_arduino_Touch_Page4.IsTag = false;
            Debug.Log($"점수 변경: {q}점");
        }

    }

    public void ClearIsTag(int[] index)
    {

        if (gameObject.activeInHierarchy == false)
        {
            return;
        }

        if (FlaseCorotuine == null)

            FlaseCorotuine = StartCoroutine(IsTagFalseCoroutine());

    }

    IEnumerator IsTagFalseCoroutine()
    {
        IsTag = false;

        _falseTime = _deflatFalseTime;

        while (_falseTime > 0)
        {
            _falseTime -= Time.deltaTime;
            yield return null;
        }
        FlaseCorotuine = null;

    }




}

