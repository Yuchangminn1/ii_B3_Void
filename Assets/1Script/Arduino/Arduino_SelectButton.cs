using System;
using System.Collections;
using System.Collections.Generic;
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

    [SerializeField] float touchDelayRetrySeconds = 0.3f;
    [SerializeField] float commandAckTimeoutSeconds = 0.8f;

    [SerializeField]
    string[] soundOnAckMessages = {
        "SoundOn",
        "Sound_On",
        "SoundOnAck"
    };

    [SerializeField]
    string[] ledAllOnAckMessages = {
        "LEDAllOn",
        "LEDAllOnAck"
    };
    [SerializeField]
    string[] ledAllOffAckMessages = {
        "LEDAllOff",

        "LEDAllOffAck"
    };
    bool isWaitingCommandAck = false;
    bool isCommandAckReceived = false;
    string[] currentAckMessages = null;
    string currentCommandName = string.Empty;
    bool[] _pressedState = new bool[5];



    override protected bool IsReadingMessage()
    {
        return true;
    }


    override public void ReadMessageProcess(string received)
    {
        if (PopupManager.Instance.currentInputType != InputType.Button && !isReconnecting)
            return;
        if (string.IsNullOrEmpty(received))
            return;

        if (enableButtonDebugLog)
            Debug.Log($"[SelectButton:{ButtonDirection}] Raw message: {received}");

        if (isWaitingCommandAck && currentAckMessages != null && currentAckMessages.Length > 0)
        {
            for (int i = 0; i < currentAckMessages.Length; i++)
            {
                string ackMessage = currentAckMessages[i];
                if (string.IsNullOrEmpty(ackMessage))
                    continue;

                if (received.IndexOf(ackMessage, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    isCommandAckReceived = true;
                    if (enableButtonDebugLog)
                        Debug.Log($"[SelectButton:{ButtonDirection}] ACK 수신: command={currentCommandName}, ack={ackMessage}, raw={received}");
                    return;
                }
            }
        }

        int buttonIndex = -1;
        bool isOnEvent = false;

        for (int i = 0; i < _onMessage.Length; i++)
        {
            if (received.Contains(_onMessage[i]))
            {
                buttonIndex = i;
                isOnEvent = true;
                break;
            }
        }

        if (buttonIndex < 0)
        {
            for (int i = 0; i < _offMessage.Length; i++)
            {
                if (received.Contains(_offMessage[i]))
                {
                    buttonIndex = i;
                    isOnEvent = false;
                    break;
                }
            }
        }

        if (buttonIndex < 0)
        {
            if (enableButtonDebugLog)
                Debug.Log($"[SelectButton:{ButtonDirection}] 매칭 실패 메시지: {received}");
            return;
        }

        if (isOnEvent == false)
        {
            _pressedState[buttonIndex] = false;
            return;
        }

        if (_pressedState[buttonIndex])
        {
            if (enableButtonDebugLog)
                Debug.Log($"[SelectButton:{ButtonDirection}] 중복 ON 입력 무시: index={buttonIndex + 1}");
            return;
        }

        _pressedState[buttonIndex] = true;
        int tmp = buttonIndex + 1;

        if (tmp != 0)
        {

            UserDataManager.Instance.GetPlayer(ButtonDirection).Answers[QuestionManager.Instance.CurrentIndex] = tmp;
            StartCoroutine(UserDataManager.Instance.RequestUserDataUpdate(QuestionManager.Instance.CurrentIndex + 1, tmp, ButtonDirection));
            Debug.Log($"버튼 입력 감지:{ButtonDirection}의 {QuestionManager.Instance.CurrentIndex + 1} 번째 답변이 {tmp}로 설정되었습니다.");



            if (ButtonDirection == Direction.Left)
                _onDebugPlayerLeft?.Invoke();

            else if (ButtonDirection == Direction.Right)
                _onDebugPlayerRight?.Invoke();

            LEDAllOff();


            GameManager.Instance.GoToIdleCheck();

            if (enableButtonDebugLog)
                Debug.Log($"[SelectButton:{ButtonDirection}] 유효 입력 처리 완료: value={tmp}");


        }

    }

    protected void FixedUpdate()
    {
        if (Input.GetKey(KeyCode.Z))
        {
            if (ButtonDirection == Direction.Left)
                DebugF();
        }
        if (Input.GetKey(KeyCode.X))
        {
            if (ButtonDirection == Direction.Right)
                DebugF();
        }
    }

    protected override void Start()
    {
        base.Start();
        // if (ButtonDirection == Direction.Left)
        // {
        //     _onDebugPlayerLeft += DebugTest;
        // }
        // else if (ButtonDirection == Direction.Right)
        // {
        //     _onDebugPlayerRight += DebugTest;
        // }
    }

    public void LEDAllOn()
    {
        if (stream == null || !stream.IsOpen)
        {
            if (!TryOpenPort("LEDAllOn"))
            {
                Debug.LogWarning("시리얼 포트가 열려 있지 않음: " + SerialPortNames[0]);
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

                //ArduinoTouchManager.Instance.UseTouchInput = false;

                PopupManager.Instance.SetInputType(InputType.Button);

                //ArduinoLEDManager.Instance.SendLEDAllOff();


                Debug.Log("LEDAllOn 명령 전송: " + stream.PortName);
            }
            catch (Exception e)
            {
                Debug.LogError("LEDAllOn 명령 전송 중 오류 발생: " + e.Message + " / " + stream.PortName);
            }
        }
    }

    protected override IEnumerator OnReconnectSucceeded()
    {
        if (PopupManager.Instance.currentInputType != InputType.Button)
            yield break;

        if (touchDelayCoroutine != null)
            StopCoroutine(touchDelayCoroutine);

        touchDelayCoroutine = StartCoroutine(TouchDelay());
    }

    IEnumerator TouchDelay()
    {
        while (_isRunning)
        {
            yield return StartCoroutine(SendCommandWithAckRetry("SoundOn", soundOnAckMessages));
            if (!_isRunning)
                break;

            yield return CoroutineReturnManager.GetWaitForSeconds(0.1f);

            yield return StartCoroutine(SendCommandWithAckRetry("LEDAllOn", ledAllOnAckMessages));
            if (!_isRunning)
                break;

            touchDelayCoroutine = null;
            yield break;
        }

        touchDelayCoroutine = null;
    }

    IEnumerator SendCommandWithAckRetry(string commandMessage, string[] ackMessages)
    {
        while (_isRunning)
        {
            if (stream == null || !stream.IsOpen)
            {
                RequestReconnect($"{commandMessage} 전송 전 포트 미오픈");

                while (_isRunning && isReconnecting)
                    yield return CoroutineReturnManager.GetWaitForSeconds(0.05f);

                if (!_isRunning)
                    yield break;

                if (stream == null || !stream.IsOpen)
                {
                    yield return CoroutineReturnManager.GetWaitForSeconds(touchDelayRetrySeconds);
                    continue;
                }
            }

            if (isReconnecting)
            {
                while (_isRunning && isReconnecting)
                    yield return CoroutineReturnManager.GetWaitForSeconds(0.05f);

                if (!_isRunning)
                    yield break;

                if (stream == null || !stream.IsOpen)
                {
                    yield return CoroutineReturnManager.GetWaitForSeconds(touchDelayRetrySeconds);
                    continue;
                }

                // 재연결 직후 장치 수신 버퍼가 안정될 시간을 약간 준 뒤 명령을 다시 보낸다.
                yield return CoroutineReturnManager.GetWaitForSeconds(0.05f);
                continue;
            }

            isCommandAckReceived = false;
            isWaitingCommandAck = true;
            currentAckMessages = ackMessages;
            currentCommandName = commandMessage;

            bool sendFailed = false;

            try
            {
                stream.WriteLine(commandMessage);
                if (enableButtonDebugLog)
                    Debug.Log($"[SelectButton:{ButtonDirection}] 명령 전송: {commandMessage}");
            }
            catch (Exception e)
            {
                sendFailed = true;
                isWaitingCommandAck = false;
                currentAckMessages = null;
                currentCommandName = string.Empty;

                Debug.LogWarning($"{commandMessage} 전송 실패, 재연결 재시도: {e.Message}");
                RequestReconnect($"{commandMessage} 전송 실패");
            }

            if (sendFailed)
            {
                yield return CoroutineReturnManager.GetWaitForSeconds(touchDelayRetrySeconds);
                continue;
            }

            // ACK 응답 대기/타임아웃 재시도 로직 비활성화:
            // 전송이 예외 없이 완료되면 성공으로 간주하고 종료한다.
            isWaitingCommandAck = false;
            isCommandAckReceived = false;
            currentAckMessages = null;
            currentCommandName = string.Empty;
            yield break;

            /*
            float endAt = Time.realtimeSinceStartup + Mathf.Max(0.1f, commandAckTimeoutSeconds);

            while (_isRunning && Time.realtimeSinceStartup < endAt)
            {
                if (isCommandAckReceived)
                    break;

                // ACK 대기 중 재연결이 시작되면 현재 대기를 끊고, 재연결 후 같은 명령을 재전송한다.
                if (isReconnecting)
                    break;

                yield return CoroutineReturnManager.GetWaitForSeconds(0.02f);
            }

            bool ackReceived = isCommandAckReceived;
            isWaitingCommandAck = false;
            isCommandAckReceived = false;
            currentAckMessages = null;
            currentCommandName = string.Empty;

            if (ackReceived)
                yield break;

            if (!_isRunning)
                yield break;

            if (isReconnecting)
            {
                while (_isRunning && isReconnecting)
                    yield return CoroutineReturnManager.GetWaitForSeconds(0.05f);

                if (!_isRunning)
                    yield break;

                yield return CoroutineReturnManager.GetWaitForSeconds(0.05f);
                continue;
            }

            Debug.LogWarning($"{commandMessage} ACK 대기 타임아웃, 재전송 재시도");
            RequestReconnect($"{commandMessage} ACK 타임아웃");

            while (_isRunning && isReconnecting)
                yield return CoroutineReturnManager.GetWaitForSeconds(0.05f);

            if (!_isRunning)
                yield break;

            yield return CoroutineReturnManager.GetWaitForSeconds(0.05f);
            */
        }
    }
    public void LEDAllOff()
    {
        if (stream == null || !stream.IsOpen)
        {
            if (!TryOpenPort("LEDAllOff"))
            {
                Debug.LogWarning("시리얼 포트가 열려 있지 않음: " + SerialPortNames[0]);
                return;
            }
        }
        if (!_isRunning)
        {
            Debug.LogWarning($"[SelectButton:{ButtonDirection}] LEDAllOff 요청 무시: Arduino가 동작 중이 아님");
            return;
        }

        stream.WriteLine("LEDAllOff");
        Debug.Log("LEDAllOff 명령 전송: " + stream.PortName);
        // if (touchDelayCoroutine != null)
        //     StopCoroutine(touchDelayCoroutine);

        //touchDelayCoroutine = StartCoroutine(SendCommandWithAckRetry("LEDAllOff", ledAllOffAckMessages));

        if (stream != null && stream.IsOpen)
            Debug.Log("LEDAllOff ACK 확인 전송 시작: " + stream.PortName);
        else
            Debug.LogWarning($"[SelectButton:{ButtonDirection}] LEDAllOff 전송 전 포트 미오픈, 재연결 후 재시도 예정");
    }


    public void DebugF()
    {

        if (ButtonDirection == Direction.Left)
        {
            if (debugCoroutine == null)
                debugCoroutine = StartCoroutine(TestCoroutine());
        }
        else if (ButtonDirection == Direction.Right)
        {
            if (debugCoroutine == null)
                debugCoroutine = StartCoroutine(TestCoroutine());

        }
    }
    public IEnumerator TestCoroutine()
    {
        if (ButtonDirection == Direction.Left)
        {
            _onDebugPlayerLeft?.Invoke();
        }
        else if (ButtonDirection == Direction.Right)
        {
            _onDebugPlayerRight?.Invoke();
        }
        yield return CoroutineReturnManager.GetWaitForSeconds(0.2f);

        debugCoroutine = null;


    }



    // public void DebugTest()
    // {
    //     int currentAnswerIndex = QuestionManager.Instance.CurrentIndex;

    //     int answerValue = UnityEngine.Random.Range(1, 6);

    //     UserDataManager.Instance.GetPlayer(ButtonDirection).Answers[currentAnswerIndex] = answerValue;

    //     StartCoroutine(UserDataManager.Instance.RequestUserDataUpdate(currentAnswerIndex + 1, answerValue, ButtonDirection));


    //     Debug.Log("버튼 입력 감지: " + _onMessage[0] + $"index = {currentAnswerIndex + 1} - " + ButtonDirection + "의 답변이 " + answerValue + "로 설정되었습니다.");
    // }

    override protected bool IsSendingMessage()
    {
        return false;
    }
    override public void SendMessageProcess()
    {
        ;
    }
}
