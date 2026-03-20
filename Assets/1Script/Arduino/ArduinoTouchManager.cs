using System;
using System.Collections;
using System.IO.Ports;
using System.Text.RegularExpressions;
using UnityEngine;
public class ArduinoTouchManager : Singleton<ArduinoTouchManager>
{
    public const int PlayerLedNum = 12;

    public bool[] IsButtonPressed = new bool[PlayerLedNum * 2]; // 1~24번 버튼 상태 저장 (0번 인덱스는 사용 안함)



    bool _useTouchInput = false;

    public bool UseTouchInput
    {
        get => _useTouchInput;
        set
        {
            if (value)
            {
                for (int i = 0; i < IsButtonPressed.Length; i++)
                {
                    IsButtonPressed[i] = false;
                }

            }
            // if (_useTouchInput && value == false)
            // {
            //     OnPlayerLeftTouch?.Invoke(false, false);
            //     OnPlayerRightTouch?.Invoke(false, false);

            //     OnAllPlayerTouchStateChanged?.Invoke(false, false);
            // }

            _useTouchInput = value;
            Debug.Log($"UseTouchInput 상태 변경: {_useTouchInput}");
        }
    }




    public Action<bool, bool> OnPlayerLeftTouch;
    public Action<bool, bool> OnPlayerRightTouch;

    public Action<bool, bool> OnAllPlayerTouchStateChanged;

    public bool _isRunning = false;
    public string SerialPortNames = "COM8";
    int[] pair;

    protected SerialPort stream;

    int countTouch = 0;

    bool[] _isCheckMode = new bool[2]; // 디버그용 체크 모드 (각 플레이어별로)

    public void Start()
    {
        StartCoroutine(CheckTouchError());
    }

    public void SetButtonWait()
    {
        UseTouchInput = false;

        PopupManager.Instance.SetInputType(InputType.Button);

        ArduinoLEDManager.Instance.SendLEDAllOff();
    }


    virtual public void StartArduino()
    {
        Debug.Log("Touch Arduino 시작");
        for (int i = 0; i < IsButtonPressed.Length; i++)
        {
            IsButtonPressed[i] = false;
        }


        _isRunning = true;
        _isCheckMode[0] = false;
        _isCheckMode[1] = false;

        if (stream == null)
        {
            stream = new SerialPort(SerialPortNames, 115200);
            stream.ReadTimeout = 500;
        }
        try
        {
            if (!stream.IsOpen)
            {
                stream.Open();
                Debug.Log("시리얼 포트 열림: " + stream.PortName);
            }
        }
        catch (Exception e)
        {
            Debug.LogError("시리얼 포트를 여는 중 오류 발생: " + e.Message + " / " + stream.PortName);
            return;
        }

        StartCoroutine(ReadMessage());
    }

    public void TouchOff()
    {
        Debug.Log("TouchOff ");
        UseTouchInput = false;
        ArduinoLEDManager.Instance.SendLEDAllOff();
    }

    void Update()
    {
        if (_useTouchInput == false)
            return;
        if (Input.GetKeyDown(KeyCode.C))
        {
            _isCheckMode[0] = !_isCheckMode[0];
            pair = LEDData.Instance.GetPlayerLEDPair();

            IsButtonPressed[pair[0] - 1] = _isCheckMode[0];
            IsButtonPressed[pair[1] - 1] = _isCheckMode[0];

            OnPlayerLeftTouch?.Invoke(IsButtonPressed[pair[0] - 1], IsButtonPressed[pair[1] - 1]);
            OnPlayerRightTouch?.Invoke(IsButtonPressed[pair[0] + PlayerLedNum - 1], IsButtonPressed[pair[1] + PlayerLedNum - 1]);

            OnAllPlayerTouchStateChanged?.Invoke(IsButtonPressed[pair[0] - 1] && IsButtonPressed[pair[1] - 1], IsButtonPressed[pair[0] + PlayerLedNum - 1] && IsButtonPressed[pair[1] + PlayerLedNum - 1]);
            Debug.Log($"체크 모드 상태 변경: Player 1 체크 모드={_isCheckMode[0]}, Player 2 체크 모드={_isCheckMode[1]}");


        }
        if (Input.GetKeyDown(KeyCode.V))
        {
            pair = LEDData.Instance.GetPlayerLEDPair();

            _isCheckMode[1] = !_isCheckMode[1];

            IsButtonPressed[pair[0] + PlayerLedNum - 1] = _isCheckMode[1];
            IsButtonPressed[pair[1] + PlayerLedNum - 1] = _isCheckMode[1];

            OnPlayerLeftTouch?.Invoke(IsButtonPressed[pair[0] - 1], IsButtonPressed[pair[1] - 1]);
            OnPlayerRightTouch?.Invoke(IsButtonPressed[pair[0] + PlayerLedNum - 1], IsButtonPressed[pair[1] + PlayerLedNum - 1]);

            OnAllPlayerTouchStateChanged?.Invoke(IsButtonPressed[pair[0] - 1] && IsButtonPressed[pair[1] - 1], IsButtonPressed[pair[0] + PlayerLedNum - 1] && IsButtonPressed[pair[1] + PlayerLedNum - 1]);
            Debug.Log($"체크 모드 상태 변경: Player 1 체크 모드={_isCheckMode[0]}, Player 2 체크 모드={_isCheckMode[1]}");


        }
    }



    protected void OnApplicationQuit()
    {
        StopArduino();
    }

    protected void OnDisable()
    {
        StopArduino();
    }

    protected void OnDestroy()
    {
        StopArduino();
    }

    virtual public void StopArduino()
    {
        _isRunning = false;
        StopAllCoroutines();
        CloseSerialPort();
    }

    protected void CloseSerialPort()
    {
        if (stream == null)
            return;

        try
        {
            if (stream.IsOpen)
                stream.Close();
        }
        catch (Exception e)
        {
            Debug.LogWarning("시리얼 포트 종료 중 오류 발생:  " + e.Message);
        }
        finally
        {
            stream.Dispose();
            stream = null;
        }
    }

    protected IEnumerator ReadMessage()
    {
        string received;

        while (_isRunning)
        {
            if (stream == null)
            {
                yield return CoroutineReturnManager.GetWaitForSeconds(0.025f);
                continue;
            }

            if (IsReadingMessage())
            {
                if (stream.IsOpen && stream.BytesToRead > 0)
                {
                    //bool isInput = false;
                    received = "";
                    bool isError = false;

                    try
                    {
                        received = stream.ReadLine();
                        // Debug.Log("Received from Arduino: " + received);
                    }
                    catch (TimeoutException)
                    {
                        Debug.LogError("타임아웃 발생 ");
                        isError = true;



                    }
                    catch (Exception e)
                    {
                        // 타임아웃 외의 다른 에러(연결 끊김 등) 처리
                        Debug.LogError("오류 발생: " + e.Message);
                        isError = true;
                        break;
                    }
                    if (isError)
                    {
                        Debug.LogWarning("시리얼 포트 오류 발생, 재시도 중...");
                        if (stream.IsOpen)
                            stream.Close();

                        yield return CoroutineReturnManager.GetWaitForSeconds(1f);
                        if (stream.IsOpen == false)
                        {
                            stream.Open();
                            Debug.LogWarning("부활? 시리얼 포트 열림: " + stream.PortName);
                        }

                    }
                    ReadMessageProcess(received);
                }
            }
            yield return CoroutineReturnManager.GetWaitForSeconds(0.025f);
        }
    }

    protected IEnumerator SendMessage()
    {
        while (_isRunning)
        {
            // 숫자 1키를 누르면 긴 문장을 보냄

            if (IsSendingMessage())
            {
                SendMessageProcess();
            }
            yield return CoroutineReturnManager.GetWaitForSeconds(0.025f);
        }
    }



    /// <summary>
    /// 메세지 전송 지속 조건 
    /// </summary>
    /// <returns></returns>
    virtual protected bool IsSendingMessage()
    {
        return true;
    }
    /// <summary>
    /// 메세지 전송 로직
    /// </summary>
    virtual public void SendMessageProcess()
    {
        ;
    }
    /// <summary>
    /// 메세지 리드 지속 조건 
    /// </summary>
    /// <returns></returns>
    virtual protected bool IsReadingMessage()
    {
        return true;
    }

    /// <summary>
    /// 메세지 리드 로직
    /// </summary>
    /// <param name="received"></param>
    virtual public void ReadMessageProcess(string received)
    {
        if (_useTouchInput == false)
            return;

        if (string.IsNullOrEmpty(received))
            return;

        // Accept both "5Off" and noisy forms like "ff 5 Off".
        Match match = Regex.Match(received, @"\b(\d{1,2})\s*(On|Off)\b", RegexOptions.IgnoreCase);
        if (!match.Success)
            return;

        if (!int.TryParse(match.Groups[1].Value, out int buttonNumber))
            return;

        string state = match.Groups[2].Value;


        if (buttonNumber < 1 || buttonNumber > IsButtonPressed.Length)
            return;
        if (UserDataManager.Instance.IsUser() == false)
            return;

        if (string.Equals(state, "On", StringComparison.OrdinalIgnoreCase))
        {
            IsButtonPressed[buttonNumber - 1] = true;
            ArduinoLEDManager.Instance.SendLEDRedMessage(buttonNumber); // LED 상태도 함께 처리
            DebugButtonPressedArray(buttonNumber, true);
        }

        else if (string.Equals(state, "Off", StringComparison.OrdinalIgnoreCase))
        {
            IsButtonPressed[buttonNumber - 1] = false;
            ArduinoLEDManager.Instance.SendLEDRedOffMessage(buttonNumber); // LED 빨간불 끄는 메시지 예약

            countTouch++;

            DebugButtonPressedArray(buttonNumber, false);
        }
        if (UseTouchInput)
        {
            GameManager.Instance.GoToIdleCheck();

            pair = LEDData.Instance.GetPlayerLEDPair();

            if (_isCheckMode[0])
            {
                IsButtonPressed[pair[0] - 1] = true;
                IsButtonPressed[pair[1] - 1] = true;
            }
            if (_isCheckMode[1])
            {
                IsButtonPressed[pair[0] + PlayerLedNum - 1] = true;
                IsButtonPressed[pair[1] + PlayerLedNum - 1] = true;
            }
            OnPlayerLeftTouch?.Invoke(IsButtonPressed[pair[0] - 1], IsButtonPressed[pair[1] - 1]);
            OnPlayerRightTouch?.Invoke(IsButtonPressed[pair[0] + PlayerLedNum - 1], IsButtonPressed[pair[1] + PlayerLedNum - 1]);
            OnAllPlayerTouchStateChanged?.Invoke(IsButtonPressed[pair[0] - 1] && IsButtonPressed[pair[1] - 1], IsButtonPressed[pair[0] + PlayerLedNum - 1] && IsButtonPressed[pair[1] + PlayerLedNum - 1]);


        }
    }

    IEnumerator CheckTouchError()
    {
        while (true)
        {
            if (countTouch > 50)
            {
                CloseSerialPort();
                yield return CoroutineReturnManager.GetWaitForSeconds(1f);
                StartArduino();
                Debug.LogWarning("터치 입력이 100회 이상 감지되었습니다. 터치 시스템을 점검하세요.");
            }
            countTouch = 0; // 카운트 초기화

            yield return CoroutineReturnManager.GetWaitForSeconds(3f); // 10초마다 체크
        }
    }




    protected void DebugButtonPressedArray(int buttonNumber, bool isPressed)
    {
        // Display all button states in one line to quickly validate Arduino input flow.
        string[] states = new string[IsButtonPressed.Length];
        for (int i = 0; i < IsButtonPressed.Length; i++)
        {
            states[i] = IsButtonPressed[i] ? "1" : "0";
        }

        Debug.Log($"IsButtonPressed[{buttonNumber}]={(isPressed ? "On" : "Off")} ");
    }


}
