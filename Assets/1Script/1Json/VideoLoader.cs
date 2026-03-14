using UnityEngine;
using UnityEngine.Video;
using System.IO;

[System.Serializable]
public class VideoConfig
{
    public string fileName;   // StreamingAssets 기준 상대 경로
    public bool loop;
    public bool playOnAwake;
}

public class VideoLoader : JsonLoaderBase<VideoConfig, VideoPlayer>
{
    public VideoLoader(string jsonPath) : base(jsonPath) { }

    public override void ApplyLoadedData(string name, VideoPlayer obj)
    {
        if (!data.TryGetValue(name, out var item))
            return;

        string filePath = Path.Combine(Application.streamingAssetsPath, item.fileName);

#if UNITY_ANDROID
        filePath = "jar:file://" + filePath;
#elif UNITY_IOS
        filePath = "file://" + filePath;
#else
        filePath = "file:///" + filePath;
#endif

        filePath = filePath.Replace("\\", "/"); ;

        obj.url = filePath;
        Debug.Log($"VideoLoader: Set URL for {name} to {filePath}");
        obj.isLooping = item.loop;
        obj.playOnAwake = item.playOnAwake;

        if (item.playOnAwake)
            obj.Play();
    }



    public override void JsonDataUpdate()
    {
        Save();
    }
}
