using System;
using System.Collections;
using System.Collections.Generic;
using System.IO.Ports;
using UnityEngine;

public class Arduino_Manager : MonoBehaviour, IJsonGenericTarget
{
    public Arduino_SelectButton[] arduino_SelectButtons;

    string _leftPlayerArduinoButton;
    string _rightPlayerArduinoButton;

    JsonGenericUpData _genericData = new JsonGenericUpData();

    void Start() { }

    public void Initialize(JsonGenericUpData data)
    {
        data.stringParams.TryGetValue("LeftPlayerArduinoButton", out _leftPlayerArduinoButton);
        data.stringParams.TryGetValue("RightPlayerArduinoButton", out _rightPlayerArduinoButton);

        Debug.Log($"[Arduino_Manager] 설정 포트 - Left:{_leftPlayerArduinoButton}, Right:{_rightPlayerArduinoButton}");

        // portMap 키 = 아두이노가 Serial.println()으로 전송하는 ID 문자열
        Dictionary<string, IPortSetup> portMap = new();
        foreach (var arduino in arduino_SelectButtons)
        {
            string configuredId = arduino.ButtonDirection == Direction.Left
                ? _leftPlayerArduinoButton
                : _rightPlayerArduinoButton;

            if (!string.IsNullOrWhiteSpace(configuredId))
                portMap[configuredId] = arduino;
        }

        string[] systemPorts = SerialPort.GetPortNames();
        Debug.Log($"[Arduino_Manager] 시스템 포트: {(systemPorts.Length > 0 ? string.Join(", ", systemPorts) : "없음")}");

        StartCoroutine(ScanPorts(systemPorts, portMap));
    }

    IEnumerator ScanPorts(string[] systemPorts, Dictionary<string, IPortSetup> portMap)
    {
        foreach (string systemPort in systemPorts)
        {
            SerialPort testPort;

            try
            {
                testPort = new SerialPort(systemPort, 9600)
                {
                    ReadTimeout = 500,
                    DtrEnable = false,
                    RtsEnable = false
                };
                testPort.Open();
                Debug.Log($"[Arduino_Manager] {systemPort} 열림, 메세지 대기 중...");
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Arduino_Manager] {systemPort} 열기 실패: {e.Message}");
                continue;
            }

            string receivedMsg = null;
            float waited = 0f;

            while (waited < 3f && receivedMsg == null)
            {
                if (testPort.BytesToRead > 0)
                {
                    try
                    {
                        receivedMsg = testPort.ReadLine();
                    }
                    catch (TimeoutException) { }
                    catch (Exception e)
                    {
                        Debug.LogWarning($"[Arduino_Manager] {systemPort} 읽기 오류: {e.Message}");
                        break;
                    }
                }
                yield return CoroutineReturnManager.GetWaitForSeconds(0.25f);
                waited += 0.25f;
            }

            try { testPort.Close(); testPort.Dispose(); } catch { }

            if (string.IsNullOrWhiteSpace(receivedMsg))
            {
                Debug.LogWarning($"[Arduino_Manager] {systemPort} 응답 없음, 스킵");
                continue;
            }

            string trimmed = receivedMsg.Trim();

            if (!portMap.TryGetValue(trimmed, out IPortSetup matched))
            {
                Debug.Log($"[Arduino_Manager] {systemPort} 수신({trimmed})했으나 설정에 없음, 스킵");
                continue;
            }

            Debug.Log($"[Arduino_Manager] 매칭: {systemPort} →   수신: {trimmed}");
            matched.SetupPort(systemPort);
            matched.StartArduino();
            portMap.Remove(trimmed);
        }

        foreach (var remaining in portMap)
            Debug.LogWarning($"[Arduino_Manager] 포트 연결 불가: {remaining.Key}");
    }

    public void ArduinoStart()
    {
        foreach (var arduino in arduino_SelectButtons)
        {
            if (arduino.stream != null && arduino.stream.IsOpen)
                continue;
            arduino.StartArduino();
        }
    }

    public void ArduinoStop()
    {
        foreach (var arduino in arduino_SelectButtons)
        {
            if (arduino.stream == null || !arduino.stream.IsOpen)
                continue;
            arduino.LEDAllOff();
            arduino.StopArduino();
        }
    }

    public JsonGenericUpData Data()
    {
        _genericData.intParams = new Dictionary<string, int>();
        _genericData.floatParams = new Dictionary<string, float>();
        _genericData.boolParams = new Dictionary<string, bool>();
        _genericData.stringParams = new Dictionary<string, string>();

        _genericData.stringParams["LeftPlayerArduinoButton"] = _leftPlayerArduinoButton;
        _genericData.stringParams["RightPlayerArduinoButton"] = _rightPlayerArduinoButton;

        return _genericData;
    }
}
