using System;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;

public class InitialData
{
    public List<string> COLUMNS;
    public List<List<object>> DATA;
}

public class ServerData : MonoBehaviour
{
    private static ServerData instance;
    public static ServerData Instance
    {
        get
        {
            if (instance == null)
                instance = FindAnyObjectByType<ServerData>();
            return instance;
        }
    }

    int deviceNum = 0;
    public int DeviceNum { get { return deviceNum; } set { deviceNum = value; } }
    string code = "B1";
    public string Code { get { return code; } set { code = value; } }
    public event Action onCoroutineEnd;
    public Coroutine severCoroutine;

    private string _nonSerializedData;
    public InitialData initData;


    //  [SerializeField] SetContentsManager setContentsManager;



    void Awake()
    {
        onCoroutineEnd = ResetCoroutine;
        //  if (setContentsManager == null) setContentsManager = GetComponent<SetContentsManager>();


        Cursor.visible = false;
    }

    void Start()
    {
        StartCoroutine(StartProgram());
    }

    // public void StartServerData()
    // {
    //     if (severCoroutine == null)
    //     {
    //         severCoroutine = StartCoroutine(StartProgram());
    //     }
    //     else
    //     {
    //         Debug.Log("severCoroutine Is Working");
    //     }
    // }

    public string FindData(string objectName)
    {

        if (initData == null)
        {
            Debug.Log("initData IS Null ");
            return null;
        }
        if (initData.DATA == null)
        {
            Debug.Log("initData.DATA IS Null ");
            return null;
        }

        foreach (var t in initData.DATA) // 25번째 줄
        {
            if (t[2].Equals(objectName))
            {
                return t[3].ToString();
            }
        }
        Debug.LogError("Cannot Find Data : " + objectName);
        return null;
    }

    public IEnumerator StartProgram()
    {

        // string urlStartApp = $"http://211.110.44.104:8500/api/logApp.cfm?status=run&code={code}&device={deviceNum}&";

        // string urlLoadData = $"http://211.110.44.104:8500/dev/resourceJSON.cfm?code={code}";

        // var www = UnityWebRequest.Get(urlStartApp);

        // www.downloadHandler = new DownloadHandlerBuffer();

        // yield return www.SendWebRequest();

        // string jsonText = www.downloadHandler.text;

        // Debug.Log(jsonText);

        // www = UnityWebRequest.Get(urlLoadData);

        // www.downloadHandler = new DownloadHandlerBuffer();

        // yield return www.SendWebRequest();

        // _nonSerializedData = www.downloadHandler.text;

        // initData = JsonConvert.DeserializeObject<InitialData>(_nonSerializedData);

        // yield return CoroutineReturnManager.GetWaitForSeconds(2f);

        // StartCoroutine(UserDataManager.Instance.RequestInitializeUserDataTest("16357016CB"));

        // yield return CoroutineReturnManager.GetWaitForSeconds(2f);

        // Debug.Log(UserDataManager.Instance.FindValue("RESERVATION_LAST_NAME_RIGHT"));
        // yield return CoroutineReturnManager.GetWaitForSeconds(0.1f);

        // Debug.Log(UserDataManager.Instance.FindValue("RESERVATION_LAST_NAME_LEFT"));

        yield return null;


        //setContentsManager.StartSetting();
    }

    void OnDisable()
    {
        // string urlEndApp = $"http://211.110.44.104:8500/api/logApp.cfm?status=end&code={code}&device={deviceNum}&";

        // var www = UnityWebRequest.Get(urlEndApp);

        // www.downloadHandler = new DownloadHandlerBuffer();

        // www.SendWebRequest();
    }

    public void ResetCoroutine()
    {
        if (severCoroutine != null)
        {
            StopCoroutine(severCoroutine);
            severCoroutine = null;
        }

    }





    // public IEnumerator RequestDataCoroutine(string _url, Action<string> _callback)
    // {
    //     var www = UnityWebRequest.Get(_url);

    //     www.timeout = 10;

    //     www.downloadHandler = new DownloadHandlerBuffer();

    //     float ServerTime = Time.time;

    //     //Debug.Log($"{_url}서버 요청 시작 시간" + TimeSpan.FromSeconds(Time.time).ToString(@"hh\:mm\:ss"));

    //     yield return www.SendWebRequest();
    //     if (Time.time - ServerTime > 2f)
    //     {
    //         Debug.LogWarning($"{_url} / 서버 요청 지연 시간" + TimeSpan.FromSeconds(Time.time - ServerTime).ToString(@"hh\:mm\:ss"));
    //     }
    //     //Debug.Log($"{_url}서버 요청 완료 시간" + TimeSpan.FromSeconds(Time.time).ToString(@"hh\:mm\:ss"));
    //     string jsonText = www.downloadHandler.text;

    //     //Debug.LogWarning(jsonText);

    //     _callback?.Invoke(jsonText);

    //     onCoroutineEnd?.Invoke();


    // }

    public IEnumerator RequestDataCoroutine(string _url, Action<string> _callback)
    {
        const int maxRetryCount = 5;
        string lastError = string.Empty;

        for (int retry = 1; retry <= maxRetryCount; retry++)
        {
            using (var www = UnityWebRequest.Get(_url))
            {
                www.timeout = 10;
                www.downloadHandler = new DownloadHandlerBuffer();

                float serverTime = Time.time;
                yield return www.SendWebRequest();

                if (Time.time - serverTime > 2f)
                {
                    Debug.LogWarning($"{_url} / 서버 요청 지연 시간" + TimeSpan.FromSeconds(Time.time - serverTime).ToString(@"hh\\:mm\\:ss"));
                }

                if (www.result == UnityWebRequest.Result.Success)
                {
                    _callback?.Invoke(www.downloadHandler.text);
                    onCoroutineEnd?.Invoke();
                    yield break;
                }

                lastError = www.error;
                Debug.LogWarning($"요청 실패 재시도 ({retry}/{maxRetryCount}) : {_url} | {www.error}");
            }
        }

        Debug.LogError($"요청 최종 실패 ({maxRetryCount}회) : {_url} | {lastError}");
        _callback?.Invoke(null);
        onCoroutineEnd?.Invoke();


    }



}
