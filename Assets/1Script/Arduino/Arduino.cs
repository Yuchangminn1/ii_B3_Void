using System;
using System.Collections;
using System.IO.Ports;
using UnityEngine;


public interface IPortSetup
{
    public void SetupPort(string portName);

    public void StartArduino();
}

public class Arduino : MonoBehaviour, IPortSetup
{
    protected Action<Arduino> _onArduinoStart;
    protected Action<Arduino> _onArduinoDisconnected;

    public InputType InputType = InputType.Touch;

    public bool _isRunning = false;
    protected bool _isDisconnectedNotified = false;


    public int PlayerIndex = 0;
    protected bool enableSerialDebugLog = false;
    [SerializeField] protected int readTimeoutMs = 1000;

    public string SerialPortName = "COM101";

    public SerialPort stream;
    protected Coroutine readMessageCoroutine = null;


    protected virtual void Start()
    {

    }

    virtual public void StartArduino()
    {

        _isRunning = true;
        _isDisconnectedNotified = false;

        Debug.Log($"{name} StartArduino  ");

        if (!TryOpenPort("StartArduino"))
            return;

        _onArduinoStart?.Invoke(this);
        StartReadMessageLoop();
    }

    public void StartArduinoDelayed(float delaySeconds)
    {
        StartCoroutine(StartArduinoAfterDelay(delaySeconds));
    }

    IEnumerator StartArduinoAfterDelay(float delaySeconds)
    {
        _isRunning = true;
        _isDisconnectedNotified = false;

        Debug.Log($"{name} StartArduino (delayed {delaySeconds}s)");

        if (!TryOpenPort("StartArduinoDelayed"))
            yield break;


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

        string portName = SerialPortName;

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

            Debug.LogWarning($"[{name}] 포트 닫힘: {portName}");
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

    }

    public virtual void StopArduino()
    {
        Debug.LogWarning($"{SerialPortName} StopArduino  ");
        _isRunning = false;



        if (readMessageCoroutine != null)
        {
            StopCoroutine(readMessageCoroutine);
            readMessageCoroutine = null;

        }

        if (stream == null)
            return;

        try
        {
            if (stream.IsOpen)
            {
                stream.Close();
                Debug.LogWarning($"[{name}] 포트 닫힘: {SerialPortName}");
                stream.Dispose();
                stream = null;
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning("시리얼 포트 종료 중 오류 발생: " + e.Message + " / " + SerialPortName);
        }
    }

    // protected string GetConfiguredPortName()
    // {
    //     if (stream != null)
    //         return stream.PortName;

    //     if (SerialPortNames != null && SerialPortNames.Length > 0 && !string.IsNullOrWhiteSpace(SerialPortNames[0]))
    //         return SerialPortNames[0];

    //     return "UNKNOWN";
    // }

    public void SetupPort(string portName)
    {
        TryOpenPort("StartArduino", portName);
        // 기본 구현에서는 포트 설정을 하지 않음
        // 필요에 따라 서브 클래스에서 이 메서드를 오버라이드하여 포트를 설정할 수 있음
    }
    protected bool TryOpenPort(string reason, string receivePortName = "")
    {
        string portName = string.IsNullOrEmpty(receivePortName) ? SerialPortName : receivePortName;



        if (portName == "UNKNOWN")
        {
            Debug.LogError($"[{name}] 포트명이 비어있어 시리얼 오픈 실패 / reason={reason}");
            return false;
        }

        if (stream == null)
        {
            stream = new SerialPort(portName, 9600);
            stream.ReadTimeout = readTimeoutMs;
            stream.DtrEnable = false;
            stream.RtsEnable = false;
        }
        try
        {
            if (!stream.IsOpen)
            {
                stream.Open();
                Debug.Log($"시리얼 포트 열림: {stream.PortName} / {PlayerIndex} / reason={reason}");
                _isRunning = true;
                SerialPortName = stream.PortName;
            }
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"[{name}] 시리얼 포트를 여는 중 오류 발생: {e.Message} / {portName} / reason={reason}");
            return false;
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

            if (IsReadingMessage())
            {
                if (stream.IsOpen && stream.BytesToRead > 0)
                {
                    received = "";
                    try
                    {
                        received = stream.ReadLine();

                        if (enableSerialDebugLog)
                            Debug.Log($"[{name}] Received from Arduino({stream.PortName}): {received}");
                    }
                    catch (TimeoutException)
                    {
                        Debug.LogWarning($"타임아웃 발생 {stream.PortName} ");

                        Debug.LogWarning($"[{name}] 타임아웃 시점 버퍼 길이: {stream.BytesToRead}");
                        continue;

                    }
                    catch (Exception e)
                    {
                        // 타임아웃 외의 다른 에러(연결 끊김 등) 처리
                        Debug.LogError($"오류 발생: {stream.PortName} / {e.Message}");
                        NotifyDisconnected(e);
                        continue;
                    }
                    ReadMessageProcess(received);
                }
            }
            yield return CoroutineReturnManager.GetWaitForSeconds(0.25f);
        }
        readMessageCoroutine = null;
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
