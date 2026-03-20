using System.Collections.Generic;
using System.Diagnostics;
[System.Serializable]
public class JsonGenericUpData
{
    public Dictionary<string, int> intParams;
    public Dictionary<string, float> floatParams;
    public Dictionary<string, bool> boolParams;
    public Dictionary<string, string> stringParams;

}
public interface IJsonGenericTarget
{
    void Initialize(JsonGenericUpData data);
    JsonGenericUpData Data();
}

public class GenericLoader : JsonLoaderBase<JsonGenericUpData, IJsonGenericTarget>
{
    public GenericLoader(string jsonPath) : base(jsonPath) { }

    public override void ApplyLoadedData(string name, IJsonGenericTarget obj)
    {
        if (!data.TryGetValue(name, out var item))
            return;

        obj.Initialize(item);

    }


    public override void JsonDataUpdate()
    {
        foreach (var kv in _target)
        {
            UnityEngine.Debug.Log("Capturing Generic Data: " + kv.Key);
            var target = kv.Value;
            if (target == null) continue;

            JsonGenericUpData upData = new JsonGenericUpData();
            upData.intParams = new Dictionary<string, int>();
            upData.floatParams = new Dictionary<string, float>();
            upData.boolParams = new Dictionary<string, bool>();

            upData = target.Data();

            data[kv.Key] = upData;
        }

        Save();
    }
}