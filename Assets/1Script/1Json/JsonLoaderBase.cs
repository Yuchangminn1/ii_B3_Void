using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Newtonsoft.Json; // ← 반드시 using 추가

public interface IJsonLoader
{
    void Load();
    string JsonPath { get; set; }
}

public abstract class JsonLoaderBase<DataType, TargetType> : IJsonLoader
{
    public string JsonPath { get; set; }

    // JSON을 그대로 Dictionary로 로드
    protected Dictionary<string, DataType> data = new Dictionary<string, DataType>();

    // 등록된 오브젝트 (예: Text, Image, Audio 등)
    protected readonly Dictionary<string, TargetType> _target = new Dictionary<string, TargetType>();

    protected JsonLoaderBase(string jsonPath)
    {
        JsonPath = jsonPath;
    }

    public void Load()
    {
        data = LoadData();
    }

    public virtual void SetJsonPath(string jsonPath)
    {
        JsonPath = jsonPath;
        Load();
    }

    private Dictionary<string, DataType> LoadData()
    {
        string path = Path.Combine(Application.streamingAssetsPath, JsonPath);

#if UNITY_ANDROID && !UNITY_EDITOR
        using (var request = UnityEngine.Networking.UnityWebRequest.Get(path))
        {
            request.SendWebRequest();
            while (!request.isDone) { }

            if (request.result == UnityEngine.Networking.UnityWebRequest.Result.Success)
            {
                string json = request.downloadHandler.text;
                return JsonConvert.DeserializeObject<Dictionary<string, DataType>>(json);
            }
            else
            {
                Debug.LogError($"Failed to load JSON from {path}: {request.error}");
                return new Dictionary<string, DataType>();
            }
        }
#else
        if (!File.Exists(path))
        {
            Debug.LogError($"Json file not found: {path}");
            return new Dictionary<string, DataType>();
        }

        string json = File.ReadAllText(path);
        return JsonConvert.DeserializeObject<Dictionary<string, DataType>>(json);
#endif
    }

    public void Register(string name, TargetType obj)
    {
        if (!_target.ContainsKey(name))
            _target.Add(name, obj);

        ApplyLoadedData(name, obj);
    }

    public bool TryGet(string name, out TargetType obj)
    {
        return _target.TryGetValue(name, out obj);
    }

    public bool TryGetData(string name, out DataType item)
    {
        return data.TryGetValue(name, out item);
    }

    // public void Save()
    // {
    //     string path = Path.Combine(Application.streamingAssetsPath, JsonPath);
    //     var json = JsonConvert.SerializeObject(data, Formatting.Indented);
    //     File.WriteAllText(path, json);
    //     Debug.Log($"Saved JSON → {path}");
    // }
    public void Save()
    {
        string path = Path.Combine(Application.streamingAssetsPath, JsonPath);

        var settings = new JsonSerializerSettings
        {
            Formatting = Formatting.Indented,
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore   // 루프 무시
        };
        var ordered = new SortedDictionary<string, DataType>(data);
        string json = JsonConvert.SerializeObject(ordered, settings);
        File.WriteAllText(path, json);

        Debug.Log($"Saved JSON → {path}");
    }

    public void Sol()
    {
        var ordered = new SortedDictionary<string, DataType>(data);
        data = new Dictionary<string, DataType>(ordered);
        Save();
    }



    public abstract void ApplyLoadedData(string name, TargetType obj);

    public abstract void JsonDataUpdate();

    public void CaptureData()
    {
        var keysToRemove = new List<string>();
        foreach (var kv in data)
        {
            _target.TryGetValue(kv.Key, out var existingTarget);
            if (existingTarget == null)
            {
                Debug.Log($"kv.Key{kv.Key}  Is Null ");
                keysToRemove.Add(kv.Key);
            }
        }

        foreach (var key in keysToRemove)
        {
            data.Remove(key);
        }
        JsonDataUpdate();
    }
}
