using UnityEngine;
using System.IO.Ports;
using System.Collections;
using System;

public class ArduinoLEDManager : Singleton<ArduinoLEDManager>
{
    public const int PlayerLedNum = 12;

    public bool[] IsButtonPressed = new bool[PlayerLedNum * 2];


    public bool _isRunning = false;
    public string SerialPortNames = "COM8";

    protected SerialPort stream;

    void Start()
    {
        LEDData.Instance.onAddPlayerLEDIndex += SendLEDOnMessage;
    }

    virtual public void StartArduino()
    {
        Debug.Log("LED Arduino 시작");

        _isRunning = true;

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
            Debug.LogWarning("시리얼 포트 종료 중 오류 발생: " + e.Message);
        }
        finally
        {
            stream.Dispose();
            stream = null;
        }
    }

    public void SendLEDOnMessage(int[] ledIndex)
    {
        int right = -1;

        if (stream == null || !stream.IsOpen)
        {
            Debug.LogWarning("시리얼 포트가 열려 있지 않아 메시지를 보낼 수 없습니다: " + SerialPortNames);
            return;
        }
        foreach (var index in ledIndex)
        {
            right = index + PlayerLedNum;
            stream.WriteLine(index + "White");
            if (right > -0.1f)
            {
                stream.WriteLine(right + "White");
            }
        }
        SoundManager.Instance.PlayEffectSound(EffectSoundNum.WhiteLEDSound);

    }
    public void SendLEDWhiteMessage(int[] ledIndex, Direction direction)
    {
        if (stream == null || !stream.IsOpen)
        {
            Debug.LogWarning("시리얼 포트가 열려 있지 않아 메시지를 보낼 수 없습니다: " + SerialPortNames);
            return;
        }
        if (direction == Direction.Left)
        {
            foreach (var index in ledIndex)
            {
                stream.WriteLine(index + "White");
            }
        }
        else if (direction == Direction.Right)
        {
            foreach (var index in ledIndex)
            {
                stream.WriteLine((index + PlayerLedNum) + "White");
            }
        }
    }
    public void SendLEDWhiteMessage(int index, Direction direction)
    {
        if (stream == null || !stream.IsOpen)
        {
            Debug.LogWarning("시리얼 포트가 열려 있지 않아 메시지를 보낼 수 없습니다: " + SerialPortNames);
            return;
        }
        if (direction == Direction.Left)
        {

            stream.WriteLine(index + "White");
        }
        else if (direction == Direction.Right)
        {

            stream.WriteLine((index + PlayerLedNum) + "White");
        }
    }
    public void SendLEDRedMessage(int ledIndex)
    {
        if (stream == null || !stream.IsOpen)
        {
            Debug.LogWarning("시리얼 포트가 열려 있지 않아 메시지를 보낼 수 없습니다: " + SerialPortNames);
            return;
        }
        int[] q = LEDData.Instance.GetPlayerLEDPair();
        if (ledIndex == q[0] || ledIndex == q[1] || ledIndex == q[0] + PlayerLedNum || ledIndex == q[1] + PlayerLedNum)
            return;
        else
            stream.WriteLine(ledIndex + "Red");

    }
    public void SendLEDRedOffMessage(int ledIndex)
    {
        if (stream == null || !stream.IsOpen)
        {
            Debug.LogWarning("시리얼 포트가 열려 있지 않아 메시지를 보낼 수 없습니다: " + SerialPortNames);
            return;
        }
        int[] q = LEDData.Instance.GetPlayerLEDPair();
        if (ledIndex == q[0] || ledIndex == q[1] || ledIndex == q[0] + PlayerLedNum || ledIndex == q[1] + PlayerLedNum)
            return;
        else
            stream.WriteLine(ledIndex + "Off");


    }
    public void SendLEDRedMessage(int[] ledIndex, Direction direction)
    {
        if (stream == null || !stream.IsOpen)
        {
            Debug.LogWarning("시리얼 포트가 열려 있지 않아 메시지를 보낼 수 없습니다: " + SerialPortNames);
            return;
        }
        if (direction == Direction.Left)
        {
            foreach (var index in ledIndex)
            {
                stream.WriteLine(index + "Red");
            }
        }
        else
        {
            foreach (var index in ledIndex)
            {
                stream.WriteLine((index + PlayerLedNum) + "Red");
            }
        }
    }

    public void SendLEDRedMessage(int index, Direction direction)
    {
        if (stream == null || !stream.IsOpen)
        {
            Debug.LogWarning("시리얼 포트가 열려 있지 않아 메시지를 보낼 수 없습니다: " + SerialPortNames);
            return;
        }
        if (direction == Direction.Left)
        {

            stream.WriteLine(index + "Red");
        }
        else
        {

            stream.WriteLine((index + PlayerLedNum) + "Red");
        }
    }
    public void SendLEDGreenMessage(int[] ledIndex, Direction direction)
    {
        if (stream == null || !stream.IsOpen)
        {
            Debug.LogWarning("시리얼 포트가 열려 있지 않아 메시지를 보낼 수 없습니다: " + SerialPortNames);
            return;
        }
        if (direction == Direction.Left)
        {
            foreach (var index in ledIndex)
            {
                stream.WriteLine(index + "Green");
            }
        }
        else
        {
            foreach (var index in ledIndex)
            {
                stream.WriteLine((index + PlayerLedNum) + "Green");
            }
        }

    }
    public void SendLEDGreenMessage(int index, Direction direction)
    {
        if (stream == null || !stream.IsOpen)
        {
            Debug.LogWarning("시리얼 포트가 열려 있지 않아 메시지를 보낼 수 없습니다: " + SerialPortNames);
            return;
        }
        if (direction == Direction.Left)
        {

            stream.WriteLine(index + "Green");
        }
        else
        {

            stream.WriteLine((index + PlayerLedNum) + "Green");
        }
    }


    public void SendLEDAllOff()
    {
        if (stream == null || !stream.IsOpen)
        {
            Debug.Log("시리얼 포트가 열려 있지 않아 메시지를 보낼 수 없습니다: " + SerialPortNames);
            return;
        }
        Debug.Log("TouchLED LED All Off");
        stream.WriteLine("1AllOff");
        stream.WriteLine("2AllOff");
    }
    public void SendLEDAllOff(Direction direction)
    {
        if (stream == null || !stream.IsOpen)
        {
            Debug.LogWarning("시리얼 포트가 열려 있지 않아 메시지를 보낼 수 없습니다: " + SerialPortNames);
            return;
        }
        if (direction == Direction.Left)
        {
            stream.WriteLine("1AllOff");
            Debug.Log("TouchLED LED Left All Off");
        }
        else if (direction == Direction.Right)
        {
            stream.WriteLine("2AllOff");
            Debug.Log("TouchLED LED Right All Off");
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