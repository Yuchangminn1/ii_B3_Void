using System;
using System.Collections;
using System.IO.Ports;
using TMPro;
using UnityEngine;

public class Arduino : MonoBehaviour
{
    protected Action<Arduino> _onArduinoStart;
    protected Action<Arduino> _onArduinoDisconnected;

    public InputType InputType = InputType.Touch;

    protected bool isStop = false;


    protected bool _isRunning = false;
    protected bool _isDisconnectedNotified = false;


    public int PlayerIndex = 0;
    protected bool enableSerialDebugLog = false;
    [SerializeField] protected int readTimeoutMs = 1000;
    [SerializeField] protected int reconnectAfterTimeoutCount = 3;
    [SerializeField] protected float reconnectDelaySeconds = 0.8f;
    [SerializeField] protected bool enablePingPong = true;
    [SerializeField] protected string pingMessage = "Ping";
    [SerializeField] protected string pongMessage = "Pong";
    [SerializeField] protected float pingIntervalSeconds = 1f;
    [SerializeField] protected int noResponseRestartCount = 3;
    [SerializeField] protected int maxReconnectAttempts = 5;

    public string[] SerialPortNames =
    {
        "COM101"
    };
    public SerialPort stream;
    protected Coroutine readMessageCoroutine;
    protected int timeoutCount = 0;
    protected bool isReconnecting = false;
    protected int noResponseCount = 0;
    protected float nextPingAt = 0f;

    protected void RequestReconnect(string reason)
    {
        if (!_isRunning)
            return;

        if (isReconnecting)
            return;

        Debug.LogWarning($"[{name}] 재연결 요청: {reason}");

        StartCoroutine(ReopenPortAfterTimeout());
    }

    protected virtual IEnumerator OnReconnectSucceeded()
    {
        yield break;
    }

    protected virtual void Start()
    {
    }



    virtual public void StartArduino()
    {
        _isRunning = true;
        _isDisconnectedNotified = false;
        timeoutCount = 0;
        noResponseCount = 0;
        nextPingAt = Time.realtimeSinceStartup + Mathf.Max(0.1f, pingIntervalSeconds);
        Debug.Log($"{name} StartArduino  ");

        if (!TryOpenPort("StartArduino"))
            return;

        _onArduinoStart?.Invoke(this);
        StartReadMessageLoop();

    }







    public void AddOnCheckStartListener(Action<Arduino> listener)
    {
        _onArduinoStart += listener;
    }

    public void AddOnDisconnectedListener(Action<Arduino> listener)
    {
        _onArduinoDisconnected += listener;
    }

    protected void StartReadMessageLoop()
    {
        if (!_isRunning)
            return;

        if (readMessageCoroutine != null)
            return;

        readMessageCoroutine = StartCoroutine(ReadMessage());
    }



    protected void NotifyDisconnected(Exception e = null)
    {
        if (_isDisconnectedNotified)
            return;

        _isDisconnectedNotified = true;

        string portName = GetConfiguredPortName();

        if (e != null)
        {
            Debug.LogWarning($"아두이노 연결 끊김 감지:  {portName} / {e.Message}");
        }
        else
        {
            Debug.LogWarning($"아두이노 연결 끊김 감지:  {portName}");
        }

        _onArduinoDisconnected?.Invoke(this);

        try
        {
            if (stream != null && stream.IsOpen)
                stream.Close();
        }
        catch (Exception closeEx)
        {
            Debug.LogWarning($"[{name}] 연결 끊김 후 포트 닫기 실패: {closeEx.Message} / {portName}");
        }

        try
        {
            stream?.Dispose();
        }
        catch
        {
        }

        stream = null;
        readMessageCoroutine = null;

        RequestReconnect("Disconnected");

    }


    IEnumerator RestartArduino()
    {
        Debug.Log("Arduino 재시작 시도...");

        while (stream != null && stream.IsOpen)
        {
            yield return CoroutineReturnManager.GetWaitForSeconds(2f);
            StartArduino();
        }

    }

    public virtual void StopArduino()
    {
        Debug.Log($"{SerialPortNames} StopArduino  ");
        _isRunning = false;
        StopAllCoroutines();
        readMessageCoroutine = null;
        timeoutCount = 0;
        noResponseCount = 0;
        nextPingAt = 0f;

        if (stream == null)
            return;

        try
        {
            if (stream.IsOpen)
                stream.Close();
        }
        catch (Exception e)
        {
            Debug.LogWarning("시리얼 포트 종료 중 오류 발생: " + e.Message + " / " + stream.PortName);
        }
    }

    protected string GetConfiguredPortName()
    {
        if (stream != null)
            return stream.PortName;

        if (SerialPortNames != null && SerialPortNames.Length > 0 && !string.IsNullOrWhiteSpace(SerialPortNames[0]))
            return SerialPortNames[0];

        return "UNKNOWN";
    }

    protected bool TryOpenPort(string reason)
    {
        string portName = GetConfiguredPortName();

        if (portName == "UNKNOWN")
        {
            Debug.LogError($"[{name}] 포트명이 비어있어 시리얼 오픈 실패 / reason={reason}");
            return false;
        }

        if (stream == null)
        {
            stream = new SerialPort(portName, 9600);
            stream.ReadTimeout = readTimeoutMs;
        }

        try
        {
            if (!stream.IsOpen)
            {
                stream.Open();
                Debug.Log($"시리얼 포트 열림: {stream.PortName} / {PlayerIndex} / reason={reason}");
            }

            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"[{name}] 시리얼 포트를 여는 중 오류 발생: {e.Message} / {portName} / reason={reason}");
            return false;
        }
    }

    protected IEnumerator ReopenPortAfterTimeout()
    {
        if (isReconnecting)
            yield break;

        isReconnecting = true;
        string portName = GetConfiguredPortName();

        Debug.LogWarning($"[{name}] 타임아웃 누적({timeoutCount})으로 포트 재연결 시도: {portName}");

        try
        {
            if (stream != null && stream.IsOpen)
                stream.Close();
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[{name}] 포트 닫기 실패: {e.Message} / {portName}");
        }

        try
        {
            stream?.Dispose();
        }
        catch
        {
        }

        stream = null;


        int reconnectAttempt = 0;
        int maxAttempts = Mathf.Max(1, maxReconnectAttempts);

        while (_isRunning && reconnectAttempt < maxAttempts)
        {
            yield return CoroutineReturnManager.GetWaitForSeconds(reconnectDelaySeconds);
            reconnectAttempt++;
            yield return CoroutineReturnManager.GetWaitForSeconds(1f);


            if (!TryOpenPort("TimeoutReconnect"))
            {
                Debug.LogWarning($"[{name}] 포트 재연결 실패 ({reconnectAttempt}/{maxAttempts}). {reconnectDelaySeconds}초 후 재시도: {portName}");
            }
            else
            {
                timeoutCount = 0;
                noResponseCount = 0;
                nextPingAt = Time.realtimeSinceStartup + Mathf.Max(0.1f, pingIntervalSeconds);
                _isDisconnectedNotified = false;
                Debug.LogWarning($"[{name}] 포트 재연결 성공: {portName}");

                isReconnecting = false;
                StartReadMessageLoop();
                yield return StartCoroutine(OnReconnectSucceeded());
                yield break;
            }
        }

        isReconnecting = false;

        if (_isRunning)
        {
            Debug.LogError($"[{name}] 포트 재연결 {maxAttempts}회 모두 실패. Arduino를 중지합니다: {portName}");
            StopArduino();
        }
    }

    protected void OnApplicationQuit()
    {
        StopArduino();
    }
    protected IEnumerator ReadMessage()
    {
        string received;

        while (_isRunning)
        {
            if (stream == null || !stream.IsOpen)
            {
                NotifyDisconnected();
                yield break;
            }

            // if (enablePingPong && !isReconnecting)
            // {
            //     float now = Time.realtimeSinceStartup;
            //     if (now >= nextPingAt)
            //     {
            //         try
            //         {
            //             stream.WriteLine(pingMessage);
            //         }
            //         catch (Exception e)
            //         {
            //             NotifyDisconnected(e);
            //             yield break;
            //         }

            //         noResponseCount++;
            //         nextPingAt = now + Mathf.Max(0.1f, pingIntervalSeconds);

            //         if (enableSerialDebugLog)
            //             Debug.Log($"[{name}] Ping 전송: {pingMessage} / noResponseCount={noResponseCount}");

            //         if (noResponseCount >= Mathf.Max(1, noResponseRestartCount))
            //         {
            //             Debug.LogWarning($"[{name}] 핑퐁 무응답 누적({noResponseCount})으로 포트 재연결 시도");
            //             yield return StartCoroutine(ReopenPortAfterTimeout());
            //             yield return CoroutineReturnManager.GetWaitForSeconds(0.1f);
            //             continue;
            //         }
            //     }
            // }

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
                        timeoutCount = 0;

                        if (enableSerialDebugLog)
                            Debug.Log($"[{name}] Received from Arduino({stream.PortName}): {received}");
                    }
                    catch (TimeoutException)
                    {
                        timeoutCount++;
                        Debug.LogError($"타임아웃 발생 {stream.PortName} / timeoutCount={timeoutCount}");

                        Debug.LogWarning($"[{name}] 타임아웃 시점 버퍼 길이: {stream.BytesToRead}");

                        isError = true;




                    }
                    catch (Exception e)
                    {
                        // 타임아웃 외의 다른 에러(연결 끊김 등) 처리
                        Debug.LogError($"오류 발생: {stream.PortName} / {e.Message}");
                        isError = true;
                        NotifyDisconnected(e);
                        break;
                    }
                    if (isError)
                    {
                        if (timeoutCount >= reconnectAfterTimeoutCount)
                        {
                            yield return StartCoroutine(ReopenPortAfterTimeout());
                        }

                        yield return CoroutineReturnManager.GetWaitForSeconds(1f);
                        continue;
                    }

                    if (enablePingPong && string.Equals(received?.Trim(), pongMessage, StringComparison.OrdinalIgnoreCase))
                    {
                        noResponseCount = 0;

                        if (enableSerialDebugLog)
                            Debug.Log($"[{name}] Pong 수신: {received}");

                        continue;
                    }

                    ReadMessageProcess(received);
                }
            }
            yield return CoroutineReturnManager.GetWaitForSeconds(0.25f);
        }

        readMessageCoroutine = null;
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
            yield return CoroutineReturnManager.GetWaitForSeconds(0.15f);
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
        ;
    }
}
