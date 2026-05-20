using System;
using System.Collections;
using UnityEngine;

public class Arduino_SelectButton : Arduino
{
    public Direction ButtonDirection;
    bool enableButtonDebugLog = false;

    string[] _onMessage = {
        "1On",
        "2On",
        "3On",
        "4On",
        "5On"
    };

    string[] _offMessage = {
        "1Off",
        "2Off",
        "3Off",
        "4Off",
        "5Off"
    };

    public Action _onDebugPlayerLeft;
    public Action _onDebugPlayerRight;

    Coroutine debugCoroutine = null;
    Coroutine touchDelayCoroutine = null;


    override public void ReadMessageProcess(string received)
    {
        if (PopupManager.Instance.currentInputType != InputType.Button)
            return;
        if (string.IsNullOrEmpty(received))
            return;

        Debug.Log($"[SelectButton:{ButtonDirection}] 메세지 수신 ({stream?.PortName ?? "포트없음"}): {received}");
        if (enableButtonDebugLog)
            Debug.Log($"[SelectButton:{ButtonDirection}] Raw message: {received}");

        int tmp = 0;

        for (int i = 0; i < _onMessage.Length; i++)
        {
            if (received.Contains(_onMessage[i]))
            {
                tmp = i + 1;
                break;
            }
        }

        if (tmp == 0)
        {
            for (int i = 0; i < _offMessage.Length; i++)
            {
                if (received.Contains(_offMessage[i]))
                {
                    tmp = i + 1;
                    break;
                }
            }
        }

        if (tmp == 0)
        {
            if (enableButtonDebugLog)
                Debug.Log($"[SelectButton:{ButtonDirection}] 매칭 실패 메시지: {received}");
            return;
        }

        if (PageController.Instance.CurrentPage == 3)
        {
            if (UserDataManager.Instance.GetPlayer(ButtonDirection).Answers[QuestionManager.Instance.CurrentIndex] == Player.noneAnswer)
            {
                UserDataManager.Instance.GetPlayer(ButtonDirection).Answers[QuestionManager.Instance.CurrentIndex] = tmp;
                StartCoroutine(UserDataManager.Instance.RequestUserDataUpdate(QuestionManager.Instance.CurrentIndex + 1, tmp, ButtonDirection));
                Debug.Log($"버튼 입력 감지:{ButtonDirection}의 {QuestionManager.Instance.CurrentIndex + 1} 번째 답변이 {tmp}로 설정되었습니다.");
            }
        }

        if (ButtonDirection == Direction.Left)
            _onDebugPlayerLeft?.Invoke();
        else if (ButtonDirection == Direction.Right)
            _onDebugPlayerRight?.Invoke();

        GameManager.Instance.GoToIdleCheck();

        if (enableButtonDebugLog)
            Debug.Log($"[SelectButton:{ButtonDirection}] 유효 입력 처리 완료: value={tmp}");
    }

    protected void Update()
    {
        if (Input.GetKeyDown(KeyCode.Z) && ButtonDirection == Direction.Left)
            DebugF();
        if (Input.GetKeyDown(KeyCode.X) && ButtonDirection == Direction.Right)
            DebugF();
    }

    public void LEDAllOn()
    {
        if (stream == null || !stream.IsOpen)
        {
            if (!TryOpenPort("LEDAllOn"))
            {
                Debug.LogWarning("시리얼 포트가 열려 있지 않음: " + SerialPortName);
                return;
            }
        }

        if (stream != null && stream.IsOpen)
        {
            try
            {
                if (touchDelayCoroutine != null)
                    StopCoroutine(touchDelayCoroutine);

                touchDelayCoroutine = StartCoroutine(TouchDelay());
                PopupManager.Instance.SetInputType(InputType.Button);
                Debug.Log("LEDAllOn 명령 전송: " + stream.PortName);
            }
            catch (Exception e)
            {
                Debug.LogError("LEDAllOn 명령 전송 중 오류 발생: " + e.Message + " / " + stream.PortName);
            }
        }
    }

    public void LEDAllOff()
    {
        if (stream == null || !stream.IsOpen)
        {
            Debug.LogWarning($"[SelectButton:{ButtonDirection}] LEDAllOff 요청 무시: 포트가 열려있지 않음");
            return;
        }
        if (!_isRunning)
        {
            Debug.LogWarning($"[SelectButton:{ButtonDirection}] LEDAllOff 요청 무시: Arduino가 동작 중이 아님");
            return;
        }

        stream.WriteLine("LEDAllOff");
        Debug.Log("LEDAllOff 명령 전송: " + stream.PortName);
    }

    IEnumerator TouchDelay()
    {
        if (!_isRunning || stream == null || !stream.IsOpen)
        {
            touchDelayCoroutine = null;
            yield break;
        }

        try
        {
            stream.WriteLine("SoundOn");
            if (enableButtonDebugLog)
                Debug.Log($"[SelectButton:{ButtonDirection}] 명령 전송: SoundOn");
        }
        catch (Exception e)
        {
            Debug.LogWarning($"SoundOn 전송 실패: {e.Message}");
            touchDelayCoroutine = null;
            yield break;
        }

        yield return CoroutineReturnManager.GetWaitForSeconds(0.1f);

        if (!_isRunning || stream == null || !stream.IsOpen)
        {
            touchDelayCoroutine = null;
            yield break;
        }

        try
        {
            stream.WriteLine("LEDAllOn");
            if (enableButtonDebugLog)
                Debug.Log($"[SelectButton:{ButtonDirection}] 명령 전송: LEDAllOn");
        }
        catch (Exception e)
        {
            Debug.LogWarning($"LEDAllOn 전송 실패: {e.Message}");
            touchDelayCoroutine = null;
            yield break;
        }

        touchDelayCoroutine = null;
    }

    public void DebugF()
    {
        debugCoroutine ??= StartCoroutine(TestCoroutine());
    }

    public IEnumerator TestCoroutine()
    {
        if (ButtonDirection == Direction.Left)
            _onDebugPlayerLeft?.Invoke();
        else if (ButtonDirection == Direction.Right)
            _onDebugPlayerRight?.Invoke();

        yield return CoroutineReturnManager.GetWaitForSeconds(0.2f);
        debugCoroutine = null;
    }
}
