using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Arduino_Manager : MonoBehaviour, IJsonGenericTarget
{

    public Arduino_SelectButton[] arduino_SelectButtons;


    string _leftPlayerArduinoButton;
    string _rightPlayerArduinoButton;


    JsonGenericUpData _genericData = new JsonGenericUpData();


    void Start()
    {
    }
    public void Initialize(JsonGenericUpData data)
    {

        data.stringParams.TryGetValue("LeftPlayerArduinoButton", out _leftPlayerArduinoButton);
        data.stringParams.TryGetValue("RightPlayerArduinoButton", out _rightPlayerArduinoButton);

        // foreach (var arduino in arduino_SelectButtons)
        // {
        //     if (arduino.ButtonDirection == Direction.Left)
        //         arduino.SerialPortNames[0] = _leftPlayerArduinoButton;
        //     else if (arduino.ButtonDirection == Direction.Right)
        //         arduino.SerialPortNames[0] = _rightPlayerArduinoButton;
        //     arduino.StartArduino();
        // }

        // ArduinoTouchManager.Instance.SerialPortNames = _touchNode;
        // ArduinoTouchManager.Instance.StartArduino();

        // ArduinoLEDManager.Instance.SerialPortNames = _ledNode;
        // ArduinoLEDManager.Instance.StartArduino();


    }

    public void ArduinoStart()
    {
        // foreach (var arduino in arduino_SelectButtons)
        // {
        //     if (arduino.stream.IsOpen)
        //         return;
        //     arduino.StartArduino();
        // }
    }

    public void ArduinoStop()
    {
        // foreach (var arduino in arduino_SelectButtons)
        // {
        //     if (arduino.stream.IsOpen == false)
        //         return;
        //     else
        //     {
        //         arduino.LEDAllOff();
        //     }
        //     arduino.StopArduino();
        // }

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
