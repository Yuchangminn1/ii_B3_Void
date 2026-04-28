
/**
 * Ardity (Serial Communication for Arduino + Unity)
 * Author: Daniel Wilches <dwilches@gmail.com>
 *
 * This work is released under the Creative Commons Attributions license.
 * https://creativecommons.org/licenses/by/2.0/
 */

using UnityEngine;
using System.Threading;
using System.Collections;
using System.Collections.Generic;
using System;
using UnityEditor.MPE;

/**
 * This class allows a Unity program to continually check for messages from a
 * serial device.
 *
 * It creates a Thread that communicates with the serial port and continually
 * polls the messages on the wire.
 * That Thread puts all the messages inside a Queue, and this SerialController
 * class polls that queue by means of invoking SerialThread.GetSerialMessage().
 *
 * The serial device must send its messages separated by a newline character.
 * Neither the SerialController nor the SerialThread perform any validation
 * on the integrity of the message. It's up to the one that makes sense of the
 * data.
 */
public class CustomSerialController : MonoBehaviour, IJsonGenericTarget
{
    private static CustomSerialController instance;

    public static CustomSerialController Instance
    {
        get
        {
            if (instance == null)
                instance = FindObjectOfType<CustomSerialController>();
            return instance;
        }
    }
    //[Tooltip("Port name with which the SerialPort object will be created.")]
    //public string portName;

    [Tooltip("Baud rate that the serial device is using to transmit data.")]
    public int baudRate = 9600;

    [Tooltip("Reference to an scene object that will receive the events of connection, " +
             "disconnection and the messages from the serial device.")]
    public GameObject messageListener;

    [Tooltip("After an error in the serial communication, or an unsuccessful " +
             "connect, how many milliseconds we should wait.")]
    public int reconnectionDelay = 1000;

    [Tooltip("Maximum number of unread data messages in the queue. " +
             "New messages will be discarded.")]
    public int maxUnreadMessages = 1;

    // Constants used to mark the start and end of a connection. There is no
    // way you can generate clashing messages from your serial device, as I
    // compare the references of these strings, no their contents. So if you
    // send these same strings from the serial device, upon reconstruction they
    // will have different reference ids.
    public const string SERIAL_DEVICE_CONNECTED = "__Connected__";
    public const string SERIAL_DEVICE_DISCONNECTED = "__Disconnected__";
    public Direction ButtonDirection;
    public Action _onDebugPlayerLeft;
    Coroutine debugCoroutine = null;

    public Action _onDebugPlayerRight;

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
    JsonGenericUpData _genericData = new JsonGenericUpData();


    // Internal reference to the Thread and the object that runs in it.

    string[] ports = new string[2];
    protected Thread[] thread = new Thread[2];
    protected SerialThreadLines[] serialThread = new SerialThreadLines[2];


    public void Initialize(string[] portNames)
    {
        Debug.Log($"포트 초기화: {portNames}");

        for (int i = 0; i < portNames.Length; i++)
        {

            serialThread[i] = new SerialThreadLines(portNames[i], baudRate, reconnectionDelay, maxUnreadMessages);
            thread[i] = new Thread(new ThreadStart(serialThread[i].RunForever));
            thread[i].Start();
        }


        Debug.Log($" Name = {serialThread}");
        // 라이트 1로 
    }

    public void LEDAllOn()
    {
        SendSerialMessage(0, "LEDAllOn");
        SendSerialMessage(1, "LEDAllOn");
    }

    public void LEDAllOff()
    {
        SendSerialMessage(0, "LEDAllOff");
        SendSerialMessage(1, "LEDAllOff");
    }

    public void LeftLEDOff()
    {
        SendSerialMessage(0, "LEDAllOff");
    }
    public void RightLEDOff()
    {
        SendSerialMessage(1, "LEDAllOff");
    }




    // ------------------------------------------------------------------------
    // Invoked whenever the SerialController gameobject is activated.
    // It creates a new thread that tries to connect to the serial device
    // and start reading from it.
    // ------------------------------------------------------------------------
    void OnEnable()
    {

    }

    // ------------------------------------------------------------------------
    // Invoked whenever the SerialController gameobject is deactivated.
    // It stops and destroys the thread that was reading from the serial device.
    // ------------------------------------------------------------------------
    void OnDisable()
    {
        // If there is a user-defined tear-down function, execute it before
        // closing the underlying COM port.
        if (userDefinedTearDownFunction != null)
            userDefinedTearDownFunction();

        // The serialThread reference should never be null at this point,
        // unless an Exception happened in the OnEnable(), in which case I've
        // no idea what face Unity will make.
        if (serialThread != null)
        {
            for (int i = 0; i < serialThread.Length; i++)
            {
                serialThread[i].RequestStop();
                serialThread[i] = null;
            }

        }

        // This reference shouldn't be null at this point anyway.
        if (thread != null)
        {

            for (int i = 0; i < serialThread.Length; i++)
            {
                thread[i].Join();
                thread[i] = null;
            }

        }
    }

    // ------------------------------------------------------------------------
    // Polls messages from the queue that the SerialThread object keeps. Once a
    // message has been polled it is removed from the queue. There are some
    // special messages that mark the start/end of the communication with the
    // device.
    // ------------------------------------------------------------------------


    void Update()
    {
        // If the user prefers to poll the messages instead of receiving them
        // via SendMessage, then the message listener should be null.
        // // Read the next message from the queue
        // Debug.Log(serialThread.Length);
        // if (GameManager.Instance.IsStarted == false)
        //     return;

        if (Input.GetKey(KeyCode.Z))
        {
            _onDebugPlayerLeft?.Invoke();
        }
        if (Input.GetKey(KeyCode.X))
        {
            _onDebugPlayerRight?.Invoke();
        }
        if (serialThread != null)
        {
            for (int i = 0; i < serialThread.Length; i++)
            {
                string message = ReadSerialMessage(i);

                if (message == null)
                    continue;
                CheckMessage(i, message);
                // // Check if the message is plain data or a connect/disconnect event.
                // if (ReferenceEquals(message, SERIAL_DEVICE_CONNECTED))
                //     messageListener.SendMessage("OnConnectionEvent", true);
                // else if (ReferenceEquals(message, SERIAL_DEVICE_DISCONNECTED))
                // {
                //     messageListener.SendMessage("OnConnectionEvent", false);
                //     //Application.Quit();
                // }
                // else
                //     messageListener.SendMessage("OnMessageArrived", message);
            }
        }

    }


    void CheckMessage(int index, string message)
    {
        int selectNum = -1;
        for (int i = 0; i < _onMessage.Length; i++)
        {
            if (message.Contains(_onMessage[i]))
            {
                selectNum = i + 1;
                break;
            }
        }
        for (int i = 0; i < _offMessage.Length; i++)
        {
            if (message.Contains(_offMessage[i]))
            {
                selectNum = i + 1;
                break;
            }
        }
        if (index >= ports.Length)
        {
            Debug.Log($"CheckMessage / Index Out Of Range / index = {index}");
            return;
        }
        Debug.Log($"Left({ports[index]}): {message}  SelectNum: {selectNum}");
        // if (selectNum == 0)
        // {
        //     if (enableButtonDebugLog)
        //         Debug.Log($"[SelectButton:{ButtonDirection}] 매칭 실패 메시지: {received}");
        //     return;
        // }

        if (selectNum != 0)
        {
            if (PageController.Instance.CurrentPage == 3)
            {
                if (UserDataManager.Instance.GetPlayer() != null)
                {
                    if (UserDataManager.Instance.GetPlayer(ButtonDirection).Answers[QuestionManager.Instance.CurrentIndex] == Player.noneAnswer)
                    {
                        UserDataManager.Instance.GetPlayer(ButtonDirection).Answers[QuestionManager.Instance.CurrentIndex] = selectNum;
                        StartCoroutine(UserDataManager.Instance.RequestUserDataUpdate(QuestionManager.Instance.CurrentIndex + 1, selectNum, ButtonDirection));
                        Debug.Log($"버튼 입력 감지:{ButtonDirection}의 {QuestionManager.Instance.CurrentIndex + 1} 번째 답변이 {selectNum}로 설정되었습니다.");
                    }
                }

            }
            GameManager.Instance.GoToIdleCheck();
        }

        if (index == 0)
            _onDebugPlayerLeft?.Invoke();

        else if (index == 1)
            _onDebugPlayerRight?.Invoke();
    }


    // ------------------------------------------------------------------------
    // Returns a new unread message from the serial device. You only need to
    // call this if you don't provide a message listener.
    // ------------------------------------------------------------------------
    public string ReadSerialMessage(int _index)
    {
        // Read the next message from the queue
        if (serialThread[_index] == null)
            return null;
        return (string)serialThread[_index].ReadMessage();
    }

    // ------------------------------------------------------------------------
    // Puts a message in the outgoing queue. The thread object will send the
    // message to the serial device when it considers it's appropriate.
    // ------------------------------------------------------------------------
    public void SendSerialMessage(int _index, string message)
    {

        if (serialThread == null)
        {
            Debug.Log("serialThread Is Null");
            return;

        }
        serialThread[_index].SendMessage(message);
    }

    // ------------------------------------------------------------------------
    // Executes a user-defined function before Unity closes the COM port, so
    // the user can send some tear-down message to the hardware reliably.
    // ------------------------------------------------------------------------
    public delegate void TearDownFunction();
    private TearDownFunction userDefinedTearDownFunction;
    public void SetTearDownFunction(TearDownFunction userFunction)
    {
        this.userDefinedTearDownFunction = userFunction;
    }

    public void Initialize(JsonGenericUpData data)
    {
        _genericData = data;
        data.stringParams.TryGetValue("LeftButton", out ports[0]);
        data.stringParams.TryGetValue("RightButton", out ports[1]);

        Debug.Log($"포트 초기화: {ports[0]}, {ports[1]}");


        Initialize(ports);



    }
    public JsonGenericUpData Data()
    {
        _genericData.intParams = new Dictionary<string, int>();
        _genericData.floatParams = new Dictionary<string, float>();
        _genericData.boolParams = new Dictionary<string, bool>();
        _genericData.stringParams = new Dictionary<string, string>();

        return _genericData;
    }

}
